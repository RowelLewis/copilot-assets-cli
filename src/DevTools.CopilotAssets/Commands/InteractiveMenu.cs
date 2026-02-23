using DevTools.CopilotAssets.Services;

namespace DevTools.CopilotAssets.Commands;

/// <summary>
/// Interactive menu with arrow key navigation for CLI.
/// Uses ANSI escape codes for cross-platform compatibility.
/// </summary>
public static class InteractiveMenu
{
    // ANSI escape codes - these work on macOS, Linux, and Windows 10+
    private const string Esc = "\u001b";
    private const string ClearLine = $"{Esc}[2K";
    private const string CursorUp = $"{Esc}[A";
    private const string HideCursor = $"{Esc}[?25l";
    private const string ShowCursor = $"{Esc}[?25h";
    private const string CarriageReturn = "\r";

    /// <summary>
    /// Display an interactive menu and return the selected index.
    /// </summary>
    /// <param name="title">Menu title</param>
    /// <param name="options">List of options to display</param>
    /// <param name="defaultIndex">Default selected index</param>
    /// <returns>Selected index, or -1 if cancelled</returns>
    public static int Show(string title, IReadOnlyList<string> options, int defaultIndex = 0)
    {
        if (options.Count == 0) return -1;

        var selectedIndex = Math.Clamp(defaultIndex, 0, options.Count - 1);
        var menuHeight = options.Count + 2; // options + blank line + help line

        // Hide cursor and show title
        Console.Write(HideCursor);
        Console.Out.Flush();

        try
        {
            Console.WriteLine();
            Console.WriteLine(title);
            Console.WriteLine();

            // Initial render
            RenderMenu(options, selectedIndex);

            while (true)
            {
                var key = Console.ReadKey(intercept: true);

                switch (key.Key)
                {
                    case ConsoleKey.UpArrow:
                        selectedIndex = selectedIndex > 0 ? selectedIndex - 1 : options.Count - 1;
                        break;

                    case ConsoleKey.DownArrow:
                        selectedIndex = selectedIndex < options.Count - 1 ? selectedIndex + 1 : 0;
                        break;

                    case ConsoleKey.Enter:
                        ClearMenu(menuHeight);
                        return selectedIndex;

                    case ConsoleKey.Escape:
                        ClearMenu(menuHeight);
                        return -1;

                    case ConsoleKey.D1 or ConsoleKey.NumPad1:
                        if (options.Count >= 1) { ClearMenu(menuHeight); return 0; }
                        break;

                    case ConsoleKey.D2 or ConsoleKey.NumPad2:
                        if (options.Count >= 2) { ClearMenu(menuHeight); return 1; }
                        break;

                    case ConsoleKey.D3 or ConsoleKey.NumPad3:
                        if (options.Count >= 3) { ClearMenu(menuHeight); return 2; }
                        break;

                    default:
                        continue; // Don't redraw for unhandled keys
                }

                // Move cursor back up to redraw menu in place
                MoveCursorUp(menuHeight);
                RenderMenu(options, selectedIndex);
            }
        }
        finally
        {
            Console.Write(ShowCursor);
            Console.Out.Flush();
        }
    }

    private static void RenderMenu(IReadOnlyList<string> options, int selectedIndex)
    {
        for (var i = 0; i < options.Count; i++)
        {
            // Clear line and write option
            Console.Write($"{CarriageReturn}{ClearLine}");
            if (i == selectedIndex)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"  → {options[i]}");
                Console.ResetColor();
            }
            else
            {
                Console.WriteLine($"    {options[i]}");
            }
        }

        // Blank line + help text
        Console.Write($"{CarriageReturn}{ClearLine}");
        Console.WriteLine();
        Console.Write($"{CarriageReturn}{ClearLine}");
        Console.WriteLine("  ↑/↓ to navigate, Enter to select, Esc to cancel");
        Console.Out.Flush();
    }

    private static void MoveCursorUp(int lines)
    {
        for (var i = 0; i < lines; i++)
        {
            Console.Write(CursorUp);
        }
        Console.Out.Flush();
    }

    private static void ClearMenu(int menuHeight)
    {
        // Move up and clear all menu lines
        MoveCursorUp(menuHeight);
        for (var i = 0; i < menuHeight; i++)
        {
            Console.Write($"{CarriageReturn}{ClearLine}");
            Console.WriteLine();
        }
        // Move back up to leave cursor at start
        MoveCursorUp(menuHeight);
        Console.Out.Flush();
    }

    /// <summary>
    /// Prompt for text input with a default value.
    /// </summary>
    public static string PromptWithDefault(string prompt, string? defaultValue)
    {
        if (!string.IsNullOrEmpty(defaultValue))
        {
            Console.Write($"{prompt} [{defaultValue}]: ");
        }
        else
        {
            Console.Write($"{prompt}: ");
        }

        var input = Console.ReadLine()?.Trim();
        return string.IsNullOrEmpty(input) ? (defaultValue ?? "") : input;
    }

    /// <summary>
    /// Prompt for yes/no confirmation using arrow keys.
    /// </summary>
    public static bool Confirm(string prompt, bool defaultYes = true)
    {
        Console.Write($"{prompt} ");
        var options = new List<string> { "Yes", "No" };
        var defaultIndex = defaultYes ? 0 : 1;
        var selection = ShowInline(options, defaultIndex);
        return selection == 0; // Yes selected, or cancelled defaults to No
    }

    /// <summary>
    /// Folder action for interactive mode.
    /// </summary>
    public enum FolderAction { All, OneByOne, Skip }

    /// <summary>
    /// Prompt for folder action using arrow key navigation.
    /// </summary>
    public static FolderAction PromptFolder(string folderName, int fileCount, string statusSummary)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write($"  {folderName}/");
        Console.ResetColor();
        Console.WriteLine($" ({fileCount} files{(string.IsNullOrEmpty(statusSummary) ? "" : $", {statusSummary}")})");

        var options = new List<string>
        {
            "Add all",
            "Review one-by-one",
            "Skip folder"
        };

        var selection = ShowInline(options);

        return selection switch
        {
            0 => FolderAction.All,
            1 => FolderAction.OneByOne,
            _ => FolderAction.Skip // -1 (cancelled) or 2
        };
    }

    /// <summary>
    /// Display an inline menu (no title, compact) for folder selection.
    /// </summary>
    private static int ShowInline(IReadOnlyList<string> options, int defaultIndex = 0, string indent = "  ")
    {
        if (options.Count == 0) return -1;

        var selectedIndex = Math.Clamp(defaultIndex, 0, options.Count - 1);

        Console.Write(HideCursor);
        Console.Out.Flush();

        try
        {
            // Initial render - all on one line with arrows
            RenderInlineMenu(options, selectedIndex, indent);

            while (true)
            {
                var key = Console.ReadKey(intercept: true);

                switch (key.Key)
                {
                    case ConsoleKey.LeftArrow:
                        selectedIndex = selectedIndex > 0 ? selectedIndex - 1 : options.Count - 1;
                        break;

                    case ConsoleKey.RightArrow:
                        selectedIndex = selectedIndex < options.Count - 1 ? selectedIndex + 1 : 0;
                        break;

                    case ConsoleKey.Enter:
                        ClearInlineMenu();
                        return selectedIndex;

                    case ConsoleKey.Escape:
                        ClearInlineMenu();
                        return -1;

                    case ConsoleKey.D1 or ConsoleKey.NumPad1:
                        if (options.Count >= 1) { ClearInlineMenu(); return 0; }
                        break;

                    case ConsoleKey.D2 or ConsoleKey.NumPad2:
                        if (options.Count >= 2) { ClearInlineMenu(); return 1; }
                        break;

                    case ConsoleKey.D3 or ConsoleKey.NumPad3:
                        if (options.Count >= 3) { ClearInlineMenu(); return 2; }
                        break;

                    default:
                        continue;
                }

                // Redraw
                Console.Write($"{CarriageReturn}{ClearLine}");
                RenderInlineMenu(options, selectedIndex, indent);
            }
        }
        finally
        {
            Console.Write(ShowCursor);
            Console.Out.Flush();
        }
    }

    private static void RenderInlineMenu(IReadOnlyList<string> options, int selectedIndex, string indent = "  ")
    {
        Console.Write(indent);
        for (var i = 0; i < options.Count; i++)
        {
            if (i == selectedIndex)
            {
                Console.ForegroundColor = ConsoleColor.Black;
                Console.BackgroundColor = ConsoleColor.Cyan;
                Console.Write($" {options[i]} ");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write($" {options[i]} ");
                Console.ResetColor();
            }

            if (i < options.Count - 1)
            {
                Console.Write(" ");
            }
        }
        Console.Out.Flush();
    }

    private static void ClearInlineMenu()
    {
        Console.Write($"{CarriageReturn}{ClearLine}");
        Console.Out.Flush();
    }

    /// <summary>
    /// Prompt for single file action using arrow keys.
    /// </summary>
    public static bool PromptFile(string fileName, string status, bool isUpdate = false)
    {
        Console.WriteLine();
        var statusColor = status == "new" ? ConsoleColor.Green : ConsoleColor.Yellow;
        Console.Write("    ");
        Console.ForegroundColor = statusColor;
        Console.Write(fileName);
        Console.ResetColor();
        Console.WriteLine($" ({status})");

        var options = new List<string> { isUpdate ? "Update" : "Install", "Skip" };
        var selection = ShowInline(options, defaultIndex: 0, indent: "    ");
        return selection == 0;
    }

    /// <summary>
    /// Print success message for file operation.
    /// </summary>
    public static void PrintFileSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("    ✓ ");
        Console.ResetColor();
        Console.WriteLine(message);
    }

    /// <summary>
    /// Print skip message for file operation.
    /// </summary>
    public static void PrintFileSkipped(string message)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write("    ○ ");
        Console.ResetColor();
        Console.WriteLine(message);
    }

    /// <summary>
    /// Run interactive file selection workflow.
    /// </summary>
    public static List<PendingFile> SelectFiles(
        List<PendingFile> files,
        bool isUpdate,
        out int totalSelected,
        out int totalSkipped)
    {
        // Group files by folder
        var groups = files
            .GroupBy(f => f.Folder)
            .OrderBy(g => g.Key)
            .ToList();

        var selectedFiles = new List<PendingFile>();
        totalSelected = 0;
        totalSkipped = 0;

        foreach (var group in groups)
        {
            var folderFiles = group.ToList();
            var folder = group.Key;

            if (string.IsNullOrEmpty(folder))
            {
                // Root-level files - prompt individually
                foreach (var file in folderFiles)
                {
                    var status = file.Status == PendingFileStatus.New ? "new" : "modified";
                    var isFileUpdate = file.Status == PendingFileStatus.Modified;
                    if (PromptFile(file.RelativePath, status, isFileUpdate))
                    {
                        selectedFiles.Add(file);
                        PrintFileSuccess(isFileUpdate ? "Updated" : "Added");
                        totalSelected++;
                    }
                    else
                    {
                        PrintFileSkipped("Skipped");
                        totalSkipped++;
                    }
                }
            }
            else
            {
                // Folder - show folder prompt
                var newCount = folderFiles.Count(f => f.Status == PendingFileStatus.New);
                var modCount = folderFiles.Count(f => f.Status == PendingFileStatus.Modified);
                var statusParts = new List<string>();
                if (newCount > 0) statusParts.Add($"{newCount} new");
                if (modCount > 0) statusParts.Add($"{modCount} modified");
                var statusSummary = string.Join(", ", statusParts);

                // Get display name (shows "refactor skill" for skills/refactor)
                var displayName = PendingFile.GetFolderDisplayName(folder);
                var action = PromptFolder(displayName, folderFiles.Count, statusSummary);

                switch (action)
                {
                    case FolderAction.All:
                        selectedFiles.AddRange(folderFiles);
                        var actionWord = isUpdate ? "Updated" : "Added";
                        // For skills, show "Added refactor skill" instead of "Added 1 files"
                        if (folder.StartsWith("skills/"))
                        {
                            var skillName = folder.Substring("skills/".Length);
                            PrintFileSuccess($"{actionWord} {skillName} skill");
                        }
                        else
                        {
                            PrintFileSuccess($"{actionWord} {folderFiles.Count} files");
                        }
                        totalSelected += folderFiles.Count;
                        break;

                    case FolderAction.Skip:
                        // For skills, show "Skipped refactor skill" instead of "Skipped 1 files"
                        if (folder.StartsWith("skills/"))
                        {
                            var skillName = folder.Substring("skills/".Length);
                            PrintFileSkipped($"Skipped {skillName} skill");
                        }
                        else
                        {
                            PrintFileSkipped($"Skipped {folderFiles.Count} files");
                        }
                        totalSkipped += folderFiles.Count;
                        break;

                    case FolderAction.OneByOne:
                        foreach (var file in folderFiles)
                        {
                            var fileName = Path.GetFileName(file.RelativePath);
                            var status = file.Status == PendingFileStatus.New ? "new" : "modified";
                            var isFileUpdate = file.Status == PendingFileStatus.Modified;
                            if (PromptFile(fileName, status, isFileUpdate))
                            {
                                selectedFiles.Add(file);
                                PrintFileSuccess(isFileUpdate ? "Updated" : "Added");
                                totalSelected++;
                            }
                            else
                            {
                                PrintFileSkipped("Skipped");
                                totalSkipped++;
                            }
                        }
                        break;
                }
            }
        }

        return selectedFiles;
    }
}
