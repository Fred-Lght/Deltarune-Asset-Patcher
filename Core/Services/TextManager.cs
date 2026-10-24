using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using DeltaAssetEditor.Core.Models;
using DeltaAssetEditor.Core.Utils;
using UndertaleModLib;

namespace DeltaAssetEditor.Core.Services;

/// <summary>
/// Реализация менеджера работы со строками Deltarune.
/// Полностью отвязана от консоли (CLI), пригодна для использования в GUI и кроссплатформенных приложениях.
/// </summary>
public class TextManager : ITextManager
{
    private readonly UndertaleData _data;

    public event EventHandler<StringModifiedEventArgs>? StringModified;

    public int Count => _data.Strings?.Count ?? 0;

    public TextManager(UndertaleData data)
    {
        _data = data ?? throw new ArgumentNullException(nameof(data));
    }

    /// <summary>
    /// Получает строку по индексу.
    /// </summary>
    public StringEntry? GetString(int id)
    {
        if (_data.Strings == null || id < 0 || id >= _data.Strings.Count)
            return null;

        string content = _data.Strings[id].Content;
        return new StringEntry(id, content, StringClassifier.IsTechnical(content));
    }

    /// <summary>
    /// Получает список всех строк, опционально фильтруя системные/технические.
    /// </summary>
    public IReadOnlyList<StringEntry> GetAllStrings(DumpOptions? options = null)
    {
        options ??= new DumpOptions();

        if (_data.Strings == null)
            return Array.Empty<StringEntry>();

        var result = new List<StringEntry>(_data.Strings.Count);

        for (int i = 0; i < _data.Strings.Count; i++)
        {
            string content = _data.Strings[i].Content;
            bool isTechnical = StringClassifier.IsTechnical(content);

            if (options.FilterTechnical && isTechnical)
                continue;

            result.Add(new StringEntry(i, content, isTechnical));
        }

        return result;
    }

    /// <summary>
    /// Ищет вхождение подстроки (по умолчанию регистронезависимо) по всем строкам.
    /// </summary>
    public IReadOnlyList<StringSearchResult> SearchStrings(string query, bool caseSensitive = false)
    {
        if (string.IsNullOrEmpty(query) || _data.Strings == null)
            return Array.Empty<StringSearchResult>();

        var comparison = caseSensitive
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;

        var results = new List<StringSearchResult>();

        for (int i = 0; i < _data.Strings.Count; i++)
        {
            string content = _data.Strings[i].Content;
            if (string.IsNullOrEmpty(content))
                continue;

            int matchIndex = content.IndexOf(query, comparison);
            if (matchIndex >= 0)
            {
                bool isTechnical = StringClassifier.IsTechnical(content);
                results.Add(new StringSearchResult(i, content, isTechnical, matchIndex, query.Length));
            }
        }

        return results;
    }

    /// <summary>
    /// Заменяет строку по индексу в памяти.
    /// </summary>
    public bool ReplaceString(int id, string newText, out string? oldText)
    {
        oldText = null;

        if (_data.Strings == null || id < 0 || id >= _data.Strings.Count)
            return false;

        var entry = _data.Strings[id];
        oldText = entry.Content;
        entry.Content = newText;

        StringModified?.Invoke(this, new StringModifiedEventArgs(id, oldText, newText));
        return true;
    }

    /// <summary>
    /// Выгружает строки в JSON-файл.
    /// Поддерживает корректную кодировку UTF-8 (русский текст, японские иероглифы не экранируются).
    /// </summary>
    public int DumpStringsToJson(string outputPath, DumpOptions? options = null)
    {
        options ??= new DumpOptions();

        string resolvedPath = PathUtils.ResolvePath(outputPath);
        string? directory = Path.GetDirectoryName(resolvedPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var entries = GetAllStrings(options);

        var serializerOptions = new JsonSerializerOptions
        {
            WriteIndented = options.Indented,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        using var fs = File.Create(resolvedPath);
        JsonSerializer.Serialize(fs, entries, serializerOptions);

        return entries.Count;
    }
}
