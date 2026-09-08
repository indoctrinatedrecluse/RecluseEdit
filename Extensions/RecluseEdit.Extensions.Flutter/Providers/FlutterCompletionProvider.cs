using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Extensions.Flutter.Providers;

/// <summary>
/// Provides inline autocomplete and snippet expansions for Flutter widgets and Dart language constructs.
/// </summary>
public class FlutterCompletionProvider : IInlineCompletionProvider
{
    public string Id => "flutter.inline.completion";
    public string Name => "Flutter Widgets & Dart Completions";

    public IReadOnlyList<string> SupportedLanguages => ["dart", "flutter"];

    private static readonly Dictionary<string, string> Completions = new()
    {
        // Widget Snippets
        {
            "stless",
            " class MyWidget extends StatelessWidget {\n  const MyWidget({super.key});\n\n  @override\n  Widget build(BuildContext context) {\n    return const Placeholder();\n  }\n}"
        },
        {
            "stful",
            " class MyWidget extends StatefulWidget {\n  const MyWidget({super.key});\n\n  @override\n  State<MyWidget> createState() => _MyWidgetState();\n}\n\nclass _MyWidgetState extends State<MyWidget> {\n  @override\n  Widget build(BuildContext context) {\n    return const Placeholder();\n  }\n}"
        },

        // State & Lifecycle Methods
        { "setState(", "() {\n  \n});" },
        { "initState(): void", " {\n  super.initState();\n  \n}" },
        { "dispose(): void", " {\n  super.dispose();\n}" },

        // Core Flutter Widgets
        {
            "Scaffold(",
            "\n  appBar: AppBar(\n    title: const Text('Title'),\n  ),\n  body: const Center(\n    child: Text('Content'),\n  ),\n);"
        },
        { "Column(", "\n  mainAxisAlignment: MainAxisAlignment.center,\n  children: [\n    \n  ],\n)" },
        { "Row(", "\n  mainAxisAlignment: MainAxisAlignment.spaceBetween,\n  children: [\n    \n  ],\n)" },
        { "Container(", "\n  padding: const EdgeInsets.all(16.0),\n  child: \n)" },
        { "Center(", "\n  child: \n)" },
        { "Padding(", "\n  padding: const EdgeInsets.all(16.0),\n  child: \n)" },
        { "SizedBox(", "height: 16.0)" },
        {
            "ListView.builder(",
            "\n  itemCount: items.length,\n  itemBuilder: (context, index) {\n    return ListTile(\n      title: Text(items[index]),\n    );\n  },\n)"
        },
        {
            "ElevatedButton(",
            "\n  onPressed: () {\n    \n  },\n  child: const Text('Button'),\n)"
        },
        { "Text(", "'Hello, Flutter!', style: Theme.of(context).textTheme.titleLarge)" },
        { "EdgeInsets.all(", "16.0)" },
        { "EdgeInsets.symmetric(", "horizontal: 16.0, vertical: 8.0)" },
        { "Theme.of(", "context)" },
        { "Navigator.of(", "context).push(MaterialPageRoute(builder: (_) => const NextScreen()));" },

        // Dart 3 Patterns & Control Flow
        { "switch (", "state) {\n  Success() => handleSuccess(),\n  Error(:final message) => handleError(message),\n  _ => handleDefault(),\n};" }
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
