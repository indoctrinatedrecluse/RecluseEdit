using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Extensions.Laravel.Providers;

/// <summary>
/// Provides inline autocomplete suggestions and snippets for Laravel routing (Route::get, Route::resource),
/// route middleware groups, controller actions, request validation, and HTTP responses.
/// </summary>
public class LaravelRouteCompletionProvider : IInlineCompletionProvider
{
    public string Id => "laravel.routes.completion";
    public string Name => "Laravel Routing, Middleware & Controller Completions";

    public IReadOnlyList<string> SupportedLanguages => ["php"];

    private static readonly Dictionary<string, string> Completions = new()
    {
        // Route Definitions
        {
            "Route::get(",
            "'/resource', [ResourceController::class, 'index'])->name('resource.index');"
        },
        {
            "Route::post(",
            "'/resource', [ResourceController::class, 'store'])->name('resource.store');"
        },
        {
            "Route::put(",
            "'/resource/{id}', [ResourceController::class, 'update'])->name('resource.update');"
        },
        {
            "Route::delete(",
            "'/resource/{id}', [ResourceController::class, 'destroy'])->name('resource.destroy');"
        },
        {
            "Route::resource(",
            "'resources', ResourceController::class);"
        },
        {
            "Route::apiResource(",
            "'resources', ResourceController::class);"
        },
        {
            "Route::middleware([",
            "'auth'])->group(function () {\n    \n});"
        },

        // Controller Actions & Handlers
        {
            "public function index(Request $request",
            "): View\n    {\n        $items = Item::latest()->paginate(15);\n        return view('items.index', compact('items'));\n    }"
        },
        {
            "public function store(Request $request",
            "): RedirectResponse\n    {\n        $validated = $request->validate([\n            'title' => 'required|string|max:255',\n        ]);\n\n        Item::create($validated);\n        return redirect()->route('items.index')->with('success', 'Created successfully!');\n    }"
        },

        // Controller Responses
        {
            "return view(",
            "'items.index', compact('items'));"
        },
        {
            "return response()->json(",
            "['status' => 'success', 'data' => $data]);"
        },
        {
            "return redirect()->route(",
            "'dashboard')->with('success', 'Operation succeeded!');"
        },
        {
            "abort_if(",
            "!$user->isAdmin(), 403, 'Unauthorized access.');"
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
            if (prefix.EndsWith(trigger, StringComparison.Ordinal))
            {
                return Task.FromResult<string?>(suggestion);
            }
        }

        return Task.FromResult<string?>(null);
    }
}
