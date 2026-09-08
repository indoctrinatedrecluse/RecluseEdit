using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Extensions.Angular.Providers;

/// <summary>
/// Provides inline completions and code snippets for Angular templates and TypeScript components,
/// including Angular Signals, modern control flow (@if, @for, @switch), and dependency injection.
/// </summary>
public class AngularCompletionProvider : IInlineCompletionProvider
{
    public string Id => "angular.inline.completion";
    public string Name => "Angular Signals & Control Flow Completions";

    public IReadOnlyList<string> SupportedLanguages =>
    [
        "angular-html",
        "angular-ts",
        "typescript",
        "html",
        "javascript"
    ];

    private static readonly Dictionary<string, string> Completions = new()
    {
        // Angular Signals
        { "signal(", "initialValue);" },
        { "computed(", "() => this.count() * 2);" },
        { "effect(", "() => {\n  console.log(this.count());\n});" },
        { "input(", "<string>('');" },
        { "input.required<", "string>();" },
        { "output<", "string>();" },
        { "model<", "string>('');" },

        // Dependency Injection & Modern Lifecycle
        { "inject(", "MyService);" },
        { "ngOnInit(): void", " {\n  \n}" },
        { "ngOnDestroy(): void", " {\n  \n}" },

        // Component & Class Decorators
        { "@Component({", "\n  selector: 'app-root',\n  standalone: true,\n  imports: [],\n  templateUrl: './app.component.html',\n  styleUrl: './app.component.css'\n})\nexport class AppComponent {\n  \n}" },
        { "@Injectable({", "\n  providedIn: 'root'\n})\nexport class MyService {\n  \n}" },
        { "@Directive({", "\n  selector: '[appDirective]',\n  standalone: true\n})\nexport class MyDirective {\n  \n}" },
        { "@Pipe({", "\n  name: 'myPipe',\n  standalone: true\n})\nexport class MyPipe implements PipeTransform {\n  transform(value: unknown): unknown {\n    return value;\n  }\n}" },

        // Modern Control Flow (@if, @for, @switch, @defer)
        { "@if (", "condition) {\n  \n} @else {\n  \n}" },
        { "@for (", "item of items; track item.id) {\n  \n} @empty {\n  <p>No items found.</p>\n}" },
        { "@switch (", "condition) {\n  @case (value) {\n    \n  }\n  @default {\n    \n  }\n}" },
        { "@defer (", "on viewport) {\n  \n} @placeholder {\n  <p>Loading...</p>\n}" },

        // Classic Directives & Bindings
        { "*ngIf=\"", "condition\"" },
        { "*ngFor=\"let item of ", "items; trackBy: trackById\"" },
        { "[(ngModel)]=\"", "property\"" },
        { "(ngSubmit)=\"", "onSubmit()\"" },
        { "[ngClass]=\"", "{'active': isActive}\"" },
        { "[ngStyle]=\"", "{'color': textColor}\"" }
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

