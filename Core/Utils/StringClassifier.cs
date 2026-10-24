using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace DeltaAssetEditor.Core.Utils;

/// <summary>
/// Утилита для классификации строк GameMaker/Deltarune на диалоговые/пользовательские и технические/системные.
/// </summary>
public static class StringClassifier
{
    private static readonly Regex IdentifierRegex = new(@"^[a-zA-Z_][a-zA-Z0-9_]*$", RegexOptions.Compiled);

    /// <summary>
    /// Определяет, является ли строка технической (пустая строка, идентификатор переменной, функции, скрипта или chunk).
    /// </summary>
    public static bool IsTechnical(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return true;

        // Явные технические префиксы GameMaker
        if (text.StartsWith("gml_", StringComparison.Ordinal) ||
            text.StartsWith("@@", StringComparison.Ordinal))
        {
            return true;
        }

        // Если строка содержит перенос строки, пробел, или не-ASCII символы (кириллица, японский и т.д.) - это текст игры / диалог
        if (text.Contains(' ') || text.Contains('\n') || text.Contains('\r') ||
            text.Any(c => c > 127 || c == '*' || c == '/' || c == '&' || c == '#' || c == '[' || c == ']' || c == '!' || c == '?' || c == '.' || c == ':'))
        {
            return false;
        }

        // Если строка представляет собой одиночный C-style идентификатор (буквы, цифры, подчёркивание),
        // в GameMaker это название переменной, функции или скрипта (например "prototype", "savedata", "ini_open")
        return IdentifierRegex.IsMatch(text);
    }
}
