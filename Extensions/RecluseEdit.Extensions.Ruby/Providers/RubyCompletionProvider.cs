using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Extensions.Ruby.Providers;

/// <summary>
/// Provides inline autocomplete suggestions and snippets for core Ruby language constructs,
/// methods, blocks, iterations, and exception handling.
/// </summary>
public class RubyCompletionProvider : IInlineCompletionProvider
{
    public string Id => "ruby.inline.completion";
    public string Name => "Ruby Language & Idiom Completions";

    public IReadOnlyList<string> SupportedLanguages => ["ruby"];

    private static readonly Dictionary<string, string> Completions = new()
    {
        // Method and Class declarations
        {
            "def ",
            "method_name(args)\n  \nend"
        },
        {
            "class ",
            "ClassName\n  def initialize\n    \n  end\nend"
        },
        {
            "module ",
            "ModuleName\n  \nend"
        },

        // Attributes
        { "attr_accessor ", ":attribute_name" },
        { "attr_reader ", ":attribute_name" },
        { "attr_writer ", ":attribute_name" },

        // Block Iteration
        { "each do |", "item|\n  \nend" },
        { "map do |", "item|\n  \nend" },
        { "select do |", "item|\n  \nend" },
        { "reject do |", "item|\n  \nend" },
        { "times do |", "i|\n  \nend" },

        // Control Flow & Exceptions
        {
            "begin",
            "\n  \nrescue StandardError => e\n  warn e.message\nend"
        },
        {
            "case ",
            "variable\nwhen value\n  \nelse\n  \nend"
        },
        { "unless ", "condition\n  \nend" },
        { "lambda {", " |x| x * 2 }" },

        // Requirements
        { "require ", "'json'" },
        { "require_relative ", "'helper'" }
    };

    public Task<string?> GetInlineSuggestionAsync(InlineCompletionContext context, CancellationToken cancellationToken = default)
    {
        var line = context.CurrentLineText;
        var col = context.ColumnNumber - 1;
        if (col < 0 || col > line.Length) col = line.Length;

        var prefix = line[..col];
        if (string.IsNullOrWhiteSpace(prefix)) return Task.FromResult<string?>(null);

        foreach (var (trigger, suggestion) in Completions)
        {
            if (prefix.EndsWith(trigger, StringComparison.Ordinal))
            {
                return Task.FromResult<string?>(suggestion);
            }
        }

        return Task.FromResult<string?>(null);
    }
}

