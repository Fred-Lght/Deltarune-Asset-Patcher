using System;
using System.Collections.Generic;
using DeltaAssetEditor.Core.Models;

namespace DeltaAssetEditor.Core.Services;

public class StringModifiedEventArgs : EventArgs
{
    public int Id { get; }
    public string OldText { get; }
    public string NewText { get; }

    public StringModifiedEventArgs(int id, string oldText, string newText)
    {
        Id = id;
        OldText = oldText;
        NewText = newText;
    }
}

/// <summary>
/// Интерфейс менеджера работы с текстом и строковыми ресурсами игры.
/// Подходит как для консоли (CLI), так и для графического интерфейса (GUI).
/// </summary>
public interface ITextManager
{
    /// <summary>
    /// Общее количество строк в файле.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Получает строку по индексу.
    /// </summary>
    StringEntry? GetString(int id);

    /// <summary>
    /// Получает все строки (с возможностью фильтрации технических).
    /// </summary>
    IReadOnlyList<StringEntry> GetAllStrings(DumpOptions? options = null);

    /// <summary>
    /// Ищет вхождение подстроки по всем строкам data.Strings.
    /// </summary>
    IReadOnlyList<StringSearchResult> SearchStrings(string query, bool caseSensitive = false);

    /// <summary>
    /// Заменяет строку по индексу в памяти.
    /// </summary>
    bool ReplaceString(int id, string newText, out string? oldText);

    /// <summary>
    /// Выгружает строки в JSON-файл.
    /// </summary>
    int DumpStringsToJson(string outputPath, DumpOptions? options = null);

    /// <summary>
    /// Событие изменения строки (удобно для обновления GUI в реальном времени).
    /// </summary>
    event EventHandler<StringModifiedEventArgs>? StringModified;
}
