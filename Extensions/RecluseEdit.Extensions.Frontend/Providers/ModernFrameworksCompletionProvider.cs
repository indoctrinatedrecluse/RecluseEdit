using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Extensions.Frontend.Providers;

/// <summary>
/// Provides inline completions for SolidJS, Next.js App Router, Remix,
/// and bundler configurations (Vite, Webpack, Rollup, Turbopack).
/// </summary>
public class ModernFrameworksCompletionProvider : IInlineCompletionProvider
{
    public string Id => "frontend.modern.inline";
    public string Name => "SolidJS, Next.js, Remix & Bundler Configs";

    public IReadOnlyList<string> SupportedLanguages =>
    [
        "javascript",
        "typescript",
        "jsx",
        "tsx",
        "json"
    ];

    private static readonly Dictionary<string, string> Completions = new(StringComparer.OrdinalIgnoreCase)
    {
        // SolidJS Reactive Primitives
        {
            "const [count, setCount] = createSignal(",
            "0);"
        },
        {
            "createEffect(",
            "() => {\n  console.log('Signal value:', count());\n});"
        },
        {
            "const double = createMemo(",
            "() => count() * 2);"
        },
        {
            "<For each={",
            "items()}>{(item) => <li>{item.name}</li>}</For>"
        },
        {
            "<Show when={",
            "condition()} fallback={<p>Fallback</p>}>\n  <div>Content</div>\n</Show>"
        },

        // Next.js App Router & Server Actions
        {
            "export default function Layout(",
            "{ children }: { children: React.ReactNode }) {\n  return (\n    <html lang=\"en\">\n      <body>{children}</body>\n    </html>\n  );\n}"
        },
        {
            "export default function Page(",
            "() {\n  return (\n    <main className=\"flex min-h-screen flex-col items-center justify-between p-24\">\n      <h1>Next.js Page</h1>\n    </main>\n  );\n}"
        },
        {
            "'use server'",
            ";\n\nexport async function myServerAction(formData: FormData) {\n  const data = Object.fromEntries(formData);\n  return { success: true, data };\n}"
        },
        {
            "export async function GET(",
            "request: Request) {\n  return Response.json({ success: true, timestamp: Date.now() });\n}"
        },
        {
            "export const metadata: Metadata = ",
            "{\n  title: 'Next.js App',\n  description: 'Built with RecluseEdit',\n};"
        },

        // Remix Route Conventions
        {
            "export const loader: LoaderFunction = ",
            "async ({ request, params }) => {\n  return Response.json({ items: [] });\n};"
        },
        {
            "export const action: ActionFunction = ",
            "async ({ request }) => {\n  const body = await request.formData();\n  return Response.redirect('/success');\n};"
        },

        // Bundler Configs (Vite, Webpack, Rollup)
        {
            "export default defineConfig({",
            "\n  plugins: [],\n  server: {\n    port: 3000,\n    open: true\n  }\n});"
        },
        {
            "module.exports = {",
            "\n  entry: './src/index.js',\n  output: {\n    filename: 'main.js',\n    clean: true\n  }\n};"
        }
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
            if (prefix.EndsWith(trigger, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult<string?>(suggestion);
            }
        }

        return Task.FromResult<string?>(null);
    }
}

