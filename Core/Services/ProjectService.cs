using System;
using System.IO;
using DeltaAssetEditor.Core.Utils;
using UndertaleModLib;

namespace DeltaAssetEditor.Core.Services;

/// <summary>
/// Сервис управления сессией проекта моддинга Deltarune (загрузка, сохранение, бэкапы).
/// Полностью кроссплатформенный (Linux, Windows, macOS).
/// </summary>
public class ProjectService : IProjectService
{
    private UndertaleData? _currentData;
    private string? _currentFilePath;
    private TextManager? _textManager;
    private bool _isDirty;

    public UndertaleData? CurrentData => _currentData;
    public string? CurrentFilePath => _currentFilePath;
    public bool IsLoaded => _currentData != null;
    public bool IsDirty => _isDirty;
    public ITextManager? TextManager => _textManager;

    public event EventHandler<string>? ProjectLoaded;
    public event EventHandler<string>? ProjectSaved;

    public void Load(string dataFilePath, Action<string, bool>? warningCallback = null)
    {
        string resolvedPath = PathUtils.ResolvePath(dataFilePath);
        if (!File.Exists(resolvedPath))
            throw new FileNotFoundException($"Файл не найден: {resolvedPath}", resolvedPath);

        using (var stream = File.OpenRead(resolvedPath))
        {
            _currentData = UndertaleIO.Read(stream, (warning, isImportant) =>
            {
                warningCallback?.Invoke(warning, isImportant);
            });
        }

        _currentFilePath = resolvedPath;
        _textManager = new TextManager(_currentData);
        _textManager.StringModified += (_, _) => _isDirty = true;
        _isDirty = false;

        ProjectLoaded?.Invoke(this, _currentFilePath);
    }

    public bool EnsureBackup(string? dataFilePath, out string backupPath, out bool created)
    {
        string path = PathUtils.ResolvePath(dataFilePath ?? _currentFilePath ??
            throw new InvalidOperationException("Путь к файлу data.win не задан."));

        backupPath = path + ".bak";

        if (File.Exists(backupPath))
        {
            created = false;
            return true;
        }

        File.Copy(path, backupPath, overwrite: false);
        created = true;
        return true;
    }

    public void Save(string? targetFilePath = null)
    {
        if (_currentData == null)
            throw new InvalidOperationException("Нет загруженных данных data.win для сохранения.");

        string target = PathUtils.ResolvePath(targetFilePath ?? _currentFilePath ??
            throw new InvalidOperationException("Целевой путь для сохранения не указан."));

        string? dir = Path.GetDirectoryName(target);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        // Атомарное сохранение через временный файл: исключает повреждение оригинала при сбое
        string tempFile = target + ".tmp." + Guid.NewGuid().ToString("N")[..8];

        try
        {
            using (var fs = File.Create(tempFile))
            {
                UndertaleIO.Write(fs, _currentData);
            }

            // Перемещение с перезаписью (атомарно на большинстве файловых систем)
            File.Move(tempFile, target, overwrite: true);
            _isDirty = false;

            ProjectSaved?.Invoke(this, target);
        }
        catch
        {
            if (File.Exists(tempFile))
            {
                try { File.Delete(tempFile); } catch { /* ignore */ }
            }
            throw;
        }
    }
}
