using System;
using System.IO;

namespace DeltaAssetEditor.Core.Utils;

/// <summary>
/// Кроссплатформенные утилиты для работы с путями (Linux, Windows, macOS).
/// </summary>
public static class PathUtils
{
    /// <summary>
    /// Разрешает путь с поддержкой тильды (~) в Linux/macOS, переменных окружения (%VAR%) в Windows,
    /// очистки от кавычек и нормализации относительных путей.
    /// </summary>
    public static string ResolvePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return path;

        // Удаление обрамляющих кавычек
        path = path.Trim('\'', '\"');

        // Поддержка переменных окружения Windows (например %APPDATA% или %USERPROFILE%)
        path = Environment.ExpandEnvironmentVariables(path);

        // Поддержка домашней директории Unix (~)
        if (path.StartsWith("~"))
        {
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (path == "~")
            {
                path = home;
            }
            else if (path.StartsWith("~/") || path.StartsWith("~\\"))
            {
                path = Path.Combine(home, path.Substring(2));
            }
        }

        return Path.GetFullPath(path);
    }
}
