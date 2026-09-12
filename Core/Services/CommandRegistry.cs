using RecluseEdit.Core.Models;

namespace RecluseEdit.Core.Services;

/// <summary>
/// Central registry indexing commands, actions, navigation targets, and files for the Universal Command Palette.
/// </summary>
public class CommandRegistry
{
    private readonly List<CommandItem> _commands = [];

    /// <summary>
    /// Optional dynamic file provider for Quick Open mode (open tabs + workspace files).
    /// </summary>
    public Func<IEnumerable<CommandItem>>? FileProvider { get; set; }

    /// <summary>
    /// Optional handler for Go to Line navigation.
    /// </summary>
    public Action<int, int>? LineJumpHandler { get; set; }

    public IReadOnlyList<CommandItem> Commands => _commands.AsReadOnly();

    public void Register(CommandItem item)
    {
        _commands.RemoveAll(c => c.Id == item.Id);
        _commands.Add(item);
    }

    public void RegisterRange(IEnumerable<CommandItem> items)
    {
        foreach (var item in items)
        {
            Register(item);
        }
    }

    /// <summary>
    /// Searches commands, files, or line navigation targets matching the given input query.
    /// Supports mode prefixes:
    ///   '>' : Command mode (e.g. '> Terminal')
    ///   ':' : Go to line mode (e.g. ':42' or ':42:10')
    ///   '?' : Help mode
    ///   default : File search + command fallback
    /// </summary>
    public IReadOnlyList<CommandItem> Search(string? query)
    {
        query = (query ?? string.Empty).Trim();

        // 1. Help Mode
        if (query.StartsWith('?'))
        {
            return
            [
                new CommandItem
                {
                    Id = "help.commands",
                    Title = "Type '>' to search commands, settings, extensions & tools",
                    Category = "Help",
                    Icon = "💡",
                    Action = () => { }
                },
                new CommandItem
                {
                    Id = "help.files",
                    Title = "Type filename to Quick Open workspace files & open tabs",
                    Category = "Help",
                    Icon = "📁",
                    Action = () => { }
                },
                new CommandItem
                {
                    Id = "help.line",
                    Title = "Type ':' followed by number to Go to Line (e.g. :42 or :42:10)",
                    Category = "Help",
                    Icon = "📍",
                    Action = () => { }
                }
            ];
        }

        // 2. Go to Line Mode
        if (query.StartsWith(':'))
        {
            var lineSpec = query.TrimStart(':').Trim();
            if (TryParseLineSpec(lineSpec, out int line, out int col))
            {
                return
                [
                    new CommandItem
                    {
                        Id = $"nav.goto.{line}.{col}",
                        Title = $"Go to Line {line}{(col > 1 ? $", Column {col}" : "")}",
                        Category = "Navigation",
                        Icon = "📍",
                        Action = () => LineJumpHandler?.Invoke(line, col)
                    }
                ];
            }

            return
            [
                new CommandItem
                {
                    Id = "nav.goto.prompt",
                    Title = "Type line number (and optional :column) to navigate",
                    Category = "Navigation",
                    Icon = "📍",
                    Action = () => { }
                }
            ];
        }

        // 3. Command Mode (prefixed with '>')
        if (query.StartsWith('>'))
        {
            var filter = query.TrimStart('>').Trim();
            return FilterCommands(_commands, filter);
        }

        // 4. Quick Open File Mode (default when no prefix)
        var results = new List<CommandItem>();

        if (FileProvider != null)
        {
            var files = FileProvider();
            if (!string.IsNullOrEmpty(query))
            {
                files = files.Where(f =>
                    f.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    (f.Description != null && f.Description.Contains(query, StringComparison.OrdinalIgnoreCase)));
            }
            results.AddRange(files.Take(30));
        }

        // Also append matching commands if query is not empty
        if (!string.IsNullOrEmpty(query))
        {
            var matchingCmds = FilterCommands(_commands, query);
            results.AddRange(matchingCmds.Take(20));
        }
        else if (results.Count == 0)
        {
            // If empty query and no files, show all commands
            results.AddRange(_commands.Take(50));
        }

        return results;
    }

    private static List<CommandItem> FilterCommands(IEnumerable<CommandItem> source, string filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            return source.OrderBy(c => c.Category).ThenBy(c => c.Title).ToList();
        }

        var terms = filter.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return source
            .Where(c => MatchesAllTerms(c, terms))
            .OrderBy(c => CalculateScore(c, filter))
            .ToList();
    }

    private static bool MatchesAllTerms(CommandItem c, string[] terms)
    {
        foreach (var term in terms)
        {
            bool match = c.Title.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                         c.Category.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                         (c.InputGestureText != null && c.InputGestureText.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                         (c.Description != null && c.Description.Contains(term, StringComparison.OrdinalIgnoreCase));

            if (!match) return false;
        }
        return true;
    }

    private static int CalculateScore(CommandItem c, string filter)
    {
        // Lower score is ranked higher
        if (c.Title.StartsWith(filter, StringComparison.OrdinalIgnoreCase)) return 0;
        if (c.Title.Contains(filter, StringComparison.OrdinalIgnoreCase)) return 10;
        if (c.Category.Contains(filter, StringComparison.OrdinalIgnoreCase)) return 20;
        return 30;
    }

    private static bool TryParseLineSpec(string spec, out int line, out int col)
    {
        line = 1;
        col = 1;

        if (string.IsNullOrWhiteSpace(spec)) return false;

        var parts = spec.Split(':', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length > 0 && int.TryParse(parts[0], out line) && line > 0)
        {
            if (parts.Length > 1 && int.TryParse(parts[1], out col) && col > 0)
            {
                return true;
            }
            col = 1;
            return true;
        }

        return false;
    }
}
