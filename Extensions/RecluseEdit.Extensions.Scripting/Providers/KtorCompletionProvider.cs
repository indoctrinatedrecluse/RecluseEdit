using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Extensions.Scripting.Providers;

/// <summary>
/// Provides inline completions for Kotlin & Ktor asynchronous web framework,
/// including routing, embedded server startup, content negotiation, and coroutines.
/// </summary>
public class KtorCompletionProvider : IInlineCompletionProvider
{
    public string Id => "scripting.ktor.inline";
    public string Name => "Kotlin & Ktor Async Web Framework";

    public IReadOnlyList<string> SupportedLanguages => ["kotlin", "kt", "kts"];

    private static readonly Dictionary<string, string> Completions = new(StringComparer.OrdinalIgnoreCase)
    {
        // Server Setup
        { "fun main() {", "\n    embeddedServer(Netty, port = 8080, host = \"0.0.0.0\") {\n        configureRouting()\n        configureSerialization()\n    }.start(wait = true)\n}" },

        // Routing
        { "routing {", "\n    get(\"/\") {\n        call.respondText(\"Hello from Ktor!\")\n    }\n    route(\"/api/v1\") {\n        get(\"/items\") {\n            call.respond(listOf(\"Item 1\", \"Item 2\"))\n        }\n    }\n}" },
        { "get(\"", "/{id}\") {\n    val id = call.parameters[\"id\"] ?: return@get call.respond(HttpStatusCode.BadRequest)\n    call.respond(mapOf(\"id\" to id))\n}" },
        { "post(\"", "\") {\n    val body = call.receive<CreateRequest>()\n    call.respond(HttpStatusCode.Created, body)\n}" },

        // Plugins / Features
        { "install(ContentNegotiation) {", "\n    json()\n}" },
        { "install(CORS) {", "\n    anyHost()\n    allowHeader(HttpHeaders.ContentType)\n}" },

        // Coroutines
        { "suspend fun fetchRemoteData(", "): String = withContext(Dispatchers.IO) {\n    // Async I/O operations\n    \"data\"\n}" }
    };

    public Task<string?> GetInlineSuggestionAsync(InlineCompletionContext context, CancellationToken cancellationToken = default)
    {
        var prefix = !string.IsNullOrEmpty(context.TextBeforeCaret)
            ? context.TextBeforeCaret
            : context.CurrentLineText;

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
