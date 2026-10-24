using System;
using UndertaleModLib;

namespace DeltaAssetEditor.Core.Services;

/// <summary>
/// Интерфейс управления проектом моддинга Deltarune (загрузка, сохранение, бэкапы, доступ к сервисам).
/// Готов для внедрения зависимостей (DI) и использования в GUI.
/// </summary>
public interface IProjectService
{
    /// <summary>
    /// Текущие загруженные данные игры.
    /// </summary>
    UndertaleData? CurrentData { get; }

    /// <summary>
    /// Путь к текущему файлу data.win.
    /// </summary>
    string? CurrentFilePath { get; }

    /// <summary>
    /// Загружен ли файл.
    /// </summary>
    bool IsLoaded { get; }

    /// <summary>
    /// Имеются ли несохранённые изменения.
    /// </summary>
    bool IsDirty { get; }

    /// <summary>
    /// Менеджер работы со строками.
    /// </summary>
    ITextManager? TextManager { get; }

    /// <summary>
    /// Загружает data.win.
    /// </summary>
    void Load(string dataFilePath, Action<string, bool>? warningCallback = null);

    /// <summary>
    /// Гарантирует создание автоматического бэкапа в .bak, если его ещё нет.
    /// </summary>
    bool EnsureBackup(string? dataFilePath, out string backupPath, out bool created);

    /// <summary>
    /// Сохраняет изменения обратно в data.win через UndertaleIO.Write (с атомарной записью).
    /// </summary>
    void Save(string? targetFilePath = null);

    event EventHandler<string>? ProjectLoaded;
    event EventHandler<string>? ProjectSaved;
}
