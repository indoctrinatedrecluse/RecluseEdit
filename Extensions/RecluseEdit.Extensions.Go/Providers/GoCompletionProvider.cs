using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Extensions.Go.Providers;

/// <summary>
/// Provides inline ghost-text completions for idiomatic Go code, including functions,
/// methods, structs, interfaces, goroutines, channels, error handling, and concurrency primitives.
/// </summary>
public class GoCompletionProvider : IInlineCompletionProvider
{
    public string Id => "go.core.completion";
    public string Name => "Go Core Idioms & Concurrency";

    public IReadOnlyList<string> SupportedLanguages => ["go"];

    private static readonly Dictionary<string, string> Completions = new()
    {
        // Packages & Entrypoints
        {
            "package mai",
            "n\n\nimport (\n\t\"fmt\"\n)\n\nfunc main() {\n\tfmt.Println(\"Hello, World!\")\n}"
        },
        {
            "func mai",
            "n() {\n\tfmt.Println(\"Hello, World!\")\n}"
        },
        {
            "func ini",
            "t() {\n\t// Package initialization logic\n}"
        },

        // Function & Method Declarations
        {
            "func (",
            "r *Receiver) MethodName(ctx context.Context) error {\n\treturn nil\n}"
        },

        // Error Handling Idioms
        {
            "if err !=",
            " nil {\n\treturn fmt.Errorf(\"operation failed: %w\", err)\n}"
        },
        {
            "if err :=",
            " doWork(); err != nil {\n\treturn err\n}"
        },
        {
            "var Err",
            "NotFound = errors.New(\"resource not found\")"
        },

        // Loops & Range
        {
            "for _,",
            " item := range items {\n\t\n}"
        },
        {
            "for k,",
            " v := range mapping {\n\t\n}"
        },
        {
            "for i :=",
            " 0; i < len(items); i++ {\n\t\n}"
        },
        {
            "for {",
            "\n\tselect {\n\tcase <-ctx.Done():\n\t\treturn ctx.Err()\n\tdefault:\n\t\t// polling / worker loop\n\t}\n}"
        },

        // Data Types: Structs & Interfaces
        {
            "type User s",
            "truct {\n\tID        int64     `json:\"id\" db:\"id\"`\n\tName      string    `json:\"name\" binding:\"required\"`\n\tEmail     string    `json:\"email\" binding:\"required,email\"`\n\tCreatedAt time.Time `json:\"created_at\"`\n}"
        },
        {
            "type Config s",
            "truct {\n\tPort string `env:\"PORT\" envDefault:\"8080\"`\n\tHost string `env:\"HOST\" envDefault:\"localhost\"`\n}"
        },
        {
            "type Repository i",
            "nterface {\n\tFindByID(ctx context.Context, id int64) (*Entity, error)\n\tSave(ctx context.Context, entity *Entity) error\n\tDelete(ctx context.Context, id int64) error\n}"
        },

        // Concurrency: Goroutines, Channels, Select, Mutexes, WaitGroups
        {
            "go func",
            "() {\n\t// Asynchronous task\n}()"
        },
        {
            "select {",
            "\ncase msg := <-ch:\n\tfmt.Println(\"Received:\", msg)\ncase <-ctx.Done():\n\treturn ctx.Err()\ndefault:\n\t// Non-blocking fallback\n}"
        },
        {
            "var wg s",
            "ync.WaitGroup\nwg.Add(1)\ngo func() {\n\tdefer wg.Done()\n\t// background worker task\n}()\nwg.Wait()"
        },
        {
            "var mu s",
            "ync.RWMutex\nmu.Lock()\ndefer mu.Unlock()"
        },
        {
            "ch :=",
            " make(chan string, 10)"
        },

        // Context & Cleanup
        {
            "ctx, can",
            "cel := context.WithTimeout(context.Background(), 5*time.Second)\ndefer cancel()"
        },
        {
            "defer res",
            "p.Body.Close()"
        },
        {
            "defer fil",
            "e.Close()"
        },

        // Collections & Memory
        {
            "make([]s",
            "tring, 0, 10)"
        },
        {
            "make(map[s",
            "tring]any)"
        },

        // Type Switches
        {
            "switch v :=",
            " val.(type) {\ncase string:\n\tfmt.Println(\"string:\", v)\ncase int:\n\tfmt.Println(\"int:\", v)\ndefault:\n\tfmt.Println(\"unknown type:\", v)\n}"
        }
    };

    public Task<string?> GetInlineSuggestionAsync(InlineCompletionContext context, CancellationToken cancellationToken = default)
    {
        var line = context.CurrentLineText.TrimStart();

        foreach (var (prefix, suggestion) in Completions)
        {
            if (line.EndsWith(prefix, StringComparison.Ordinal))
            {
                return Task.FromResult<string?>(suggestion);
            }
        }

        return Task.FromResult<string?>(null);
    }
}
