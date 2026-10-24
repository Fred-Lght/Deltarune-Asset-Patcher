using System;
using System.IO;
using System.Linq;
using System.Text;
using DeltaAssetEditor.Core.Models;
using DeltaAssetEditor.Core.Services;
using DeltaAssetEditor.Core.Utils;

namespace DeltaAssetEditor.Cli;

/// <summary>
/// Обработчик командной строки и интерактивного режима.
/// Отделяет презентационный слой (консоль) от бизнес-логики (Core).
/// </summary>
public class CommandLineHandler
{
    private readonly IProjectService _projectService;

    public CommandLineHandler(IProjectService projectService)
    {
        _projectService = projectService;
    }

    /// <summary>
    /// Точка входа в обработку CLI аргументов или запуск интерактивной консоли.
    /// </summary>
    public int Run(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        string? dataFilePath = null;
        string? command = null;
        string[] commandArgs = Array.Empty<string>();

        // Разбор аргументов командной строки
        if (args.Length > 0)
        {
            int index = 0;

            // Проверка флага --data <path> или первого позиционного аргумента как пути к файлу
            if (args[0].Equals("--data", StringComparison.OrdinalIgnoreCase) && args.Length > 1)
            {
                dataFilePath = args[1];
                index = 2;
            }
            else if (args[0].EndsWith(".win", StringComparison.OrdinalIgnoreCase) ||
                     File.Exists(PathUtils.ResolvePath(args[0])))
            {
                dataFilePath = args[0];
                index = 1;
            }

            if (index < args.Length)
            {
                command = args[index].ToLowerInvariant();
                commandArgs = args.Skip(index + 1).ToArray();
            }
        }

        // Если путь к файлу не был передан, ищем data.win в текущей папке или запрашиваем
        if (string.IsNullOrEmpty(dataFilePath))
        {
            string defaultPath = Path.Combine(Directory.GetCurrentDirectory(), "data.win");
            if (File.Exists(defaultPath))
            {
                dataFilePath = defaultPath;
            }
            else if (string.IsNullOrEmpty(command))
            {
                Console.Write("Введите путь к data.win: ");
                dataFilePath = Console.ReadLine()?.Trim();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Ошибка: не указан путь к файлу data.win (используйте путь аргументом или положите data.win в текущую папку).");
                Console.ResetColor();
            }
        }

        if (string.IsNullOrWhiteSpace(dataFilePath))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Ошибка: путь к data.win не был указан.");
            Console.ResetColor();
            return 1;
        }

        dataFilePath = PathUtils.ResolvePath(dataFilePath);
        if (!File.Exists(dataFilePath))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Ошибка: файл не найден: {dataFilePath}");
            Console.ResetColor();
            return 1;
        }

        // Загрузка проекта и автоматический бэкап
        try
        {
            EnsureBackupAndLoad(dataFilePath);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Ошибка загрузки файла {dataFilePath}: {ex.Message}");
            Console.ResetColor();
            return 1;
        }

        // Если была передана конкретная команда из CLI, выполняем её и завершаем
        if (!string.IsNullOrEmpty(command))
        {
            return ExecuteCommand(command, commandArgs);
        }

        // Иначе запускаем интерактивную оболочку (REPL)
        return RunInteractiveLoop();
    }

    private void EnsureBackupAndLoad(string dataFilePath)
    {
        _projectService.EnsureBackup(dataFilePath, out string backupPath, out bool created);
        if (created)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[✓] Автоматический бэкап создан: {backupPath}");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"[i] Резервная копия уже существует: {backupPath}");
            Console.ResetColor();
        }

        Console.WriteLine($"[LOAD] Загрузка {Path.GetFileName(dataFilePath)}...");
        _projectService.Load(dataFilePath, (warning, isImportant) =>
        {
            Console.ForegroundColor = isImportant ? ConsoleColor.Red : ConsoleColor.DarkYellow;
            Console.WriteLine($"[UTMT {(isImportant ? "ВНИМАНИЕ" : "Инфо")}]: {warning}");
            Console.ResetColor();
        });

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[✓] Файл успешно загружен. Всего строк: {_projectService.TextManager?.Count ?? 0}\n");
        Console.ResetColor();
    }

    private int ExecuteCommand(string command, string[] args)
    {
        var textManager = _projectService.TextManager;
        if (textManager == null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Ошибка: TextManager не инициализирован.");
            Console.ResetColor();
            return 1;
        }

        switch (command)
        {
            case "dump-strings":
            case "dump":
                return ExecuteDumpStrings(textManager, args);

            case "search-strings":
            case "search":
                return ExecuteSearchStrings(textManager, args);

            case "replace-string":
            case "replace":
                return ExecuteReplaceString(textManager, args);

            case "info":
                PrintInfo();
                return 0;

            case "help":
                PrintHelp();
                return 0;

            default:
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Неизвестная команда: '{command}'. Введите 'help' для списка доступных команд.");
                Console.ResetColor();
                return 1;
        }
    }

    private int ExecuteDumpStrings(ITextManager textManager, string[] args)
    {
        if (args.Length == 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Использование: dump-strings <output.json> [--all]");
            Console.ResetColor();
            return 1;
        }

        string outputFile = args[0];
        bool includeAll = args.Any(a => a.Equals("--all", StringComparison.OrdinalIgnoreCase));

        var options = new DumpOptions
        {
            FilterTechnical = !includeAll,
            Indented = true,
            IncludeTechnicalFlag = true
        };

        try
        {
            int count = textManager.DumpStringsToJson(outputFile, options);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[✓] Успешно экспортировано {count} строк в {PathUtils.ResolvePath(outputFile)}");
            if (!includeAll)
            {
                int filteredOut = textManager.Count - count;
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"    (Отфильтровано {filteredOut} технических/системных строк. Используйте флаг --all для выгрузки всех).");
            }
            Console.ResetColor();
            return 0;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ОШИБКА] Не удалось выполнить дамп строк: {ex.Message}");
            Console.ResetColor();
            return 1;
        }
    }

    private int ExecuteSearchStrings(ITextManager textManager, string[] args)
    {
        if (args.Length == 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Использование: search-strings <текст_для_поиска>");
            Console.ResetColor();
            return 1;
        }

        string query = string.Join(" ", args);
        Console.WriteLine($"[SEARCH] Поиск подстроки: \"{query}\" (регистронезависимо)...\n");

        var results = textManager.SearchStrings(query, caseSensitive: false);

        if (results.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Совпадений не найдено.");
            Console.ResetColor();
            return 0;
        }

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"Найдено совпадений: {results.Count}\n");
        Console.ResetColor();

        foreach (var res in results)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"[{res.Id,5}] ");

            if (res.IsTechnical)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write("[tech] ");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("[text] ");
            }

            Console.ResetColor();
            string escaped = res.Text.Replace("\n", "\\n").Replace("\r", "\\r");
            Console.WriteLine(escaped);
        }

        return 0;
    }

    private int ExecuteReplaceString(ITextManager textManager, string[] args)
    {
        if (args.Length < 2)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Использование: replace-string <id> <новый_текст>");
            Console.ResetColor();
            return 1;
        }

        if (!int.TryParse(args[0], out int id))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Ошибка: '{args[0]}' не является корректным числовым индексом (id).");
            Console.ResetColor();
            return 1;
        }

        // Если текст передан несколькими словами без кавычек, объединяем их
        string newText = string.Join(" ", args.Skip(1));

        // Раскрытие escape-последовательностей (\n, \r, \t) для удобства ввода диалогов
        newText = UnescapeInput(newText);

        if (!textManager.ReplaceString(id, newText, out string? oldText))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Ошибка: строка с индексом {id} не найдена (допустимый диапазон: 0..{textManager.Count - 1}).");
            Console.ResetColor();
            return 1;
        }

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"[✓] Строка #{id} заменена в памяти:");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"    Было : {oldText?.Replace("\n", "\\n")}");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"    Стало: {newText.Replace("\n", "\\n")}");
        Console.ResetColor();

        try
        {
            Console.WriteLine($"[SAVE] Сохранение изменений в {_projectService.CurrentFilePath}...");
            _projectService.Save();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[✓] Файл data.win успешно обновлён на диске!");
            Console.ResetColor();
            return 0;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ОШИБКА] Сбой при сохранении файла: {ex.Message}");
            Console.ResetColor();
            return 1;
        }
    }

    private int RunInteractiveLoop()
    {
        PrintHelp();

        while (true)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("delta-mod > ");
            Console.ResetColor();

            string? line = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(line))
                continue;

            string[] tokens = SplitArguments(line);
            if (tokens.Length == 0)
                continue;

            string cmd = tokens[0].ToLowerInvariant();
            if (cmd is "exit" or "quit" or "q")
            {
                if (_projectService.IsDirty)
                {
                    Console.Write("Есть несохранённые изменения. Сохранить перед выходом? (y/n): ");
                    var answer = Console.ReadLine()?.Trim().ToLowerInvariant();
                    if (answer is "y" or "yes" or "да")
                    {
                        _projectService.Save();
                        Console.WriteLine("[✓] Изменения сохранены.");
                    }
                }
                Console.WriteLine("Завершение работы.");
                break;
            }

            string[] subArgs = tokens.Skip(1).ToArray();
            ExecuteCommand(cmd, subArgs);
            Console.WriteLine();
        }

        return 0;
    }

    private void PrintInfo()
    {
        var data = _projectService.CurrentData;
        if (data == null)
            return;

        Console.WriteLine("---------------- Сводка по data.win -------------");
        Console.WriteLine($"  Файл                                    : {_projectService.CurrentFilePath}");
        Console.WriteLine($"  Общее количество строк (Strings.Count)   : {data.Strings?.Count ?? 0}");
        Console.WriteLine($"  Общее количество спрайтов (Sprites.Count): {data.Sprites?.Count ?? 0}");
        Console.WriteLine($"  Количество скриптов кода (Code.Count)   : {data.Code?.Count ?? 0}");
        Console.WriteLine($"  Количество комнат (Rooms.Count)         : {data.Rooms?.Count ?? 0}");
        Console.WriteLine($"  Количество аудио (Sounds.Count)         : {data.Sounds?.Count ?? 0}");
        if (data.GeneralInfo != null)
        {
            string gameName = data.GeneralInfo.DisplayName?.Content ?? data.GeneralInfo.Name?.Content ?? "Unknown";
            Console.WriteLine($"  Название игры (GeneralInfo)             : {gameName}");
            Console.WriteLine($"  Версия байткода                         : {data.GeneralInfo.BytecodeVersion}");
        }
        Console.WriteLine("-------------------------------------------------");
    }

    private void PrintHelp()
    {
        Console.WriteLine("Доступные команды:");
        Console.WriteLine("  dump-strings <output.json> [--all]  - выгрузить строки в JSON (--all для полного дампа)");
        Console.WriteLine("  search-strings <query>              - найти строки по подстроке (case-insensitive)");
        Console.WriteLine("  replace-string <id> <new_text>      - заменить строку по индексу и сохранить в data.win");
        Console.WriteLine("  info                                - вывести сводку по загруженному файлу");
        Console.WriteLine("  help                                - справка по командам");
        Console.WriteLine("  exit / quit                         - выйти из программы\n");
    }

    private static string UnescapeInput(string input)
    {
        return input
            .Replace("\\n", "\n")
            .Replace("\\r", "\r")
            .Replace("\\t", "\t");
    }

    private static string[] SplitArguments(string commandLine)
    {
        var parts = new System.Collections.Generic.List<string>();
        bool inQuotes = false;
        var current = new StringBuilder();

        for (int i = 0; i < commandLine.Length; i++)
        {
            char c = commandLine[i];
            if (c == '\"' || c == '\'')
            {
                inQuotes = !inQuotes;
            }
            else if (char.IsWhiteSpace(c) && !inQuotes)
            {
                if (current.Length > 0)
                {
                    parts.Add(current.ToString());
                    current.Clear();
                }
            }
            else
            {
                current.Append(c);
            }
        }

        if (current.Length > 0)
        {
            parts.Add(current.ToString());
        }

        return parts.ToArray();
    }
}
