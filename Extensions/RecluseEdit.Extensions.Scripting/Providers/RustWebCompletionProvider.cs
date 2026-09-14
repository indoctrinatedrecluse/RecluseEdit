using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Extensions.Scripting.Providers;

/// <summary>
/// Provides inline completions for Rust web frameworks (Axum, Actix-web),
/// reactive WebAssembly UI frameworks (Leptos, Dioxus), and wasm-bindgen bindings.
/// </summary>
public class RustWebCompletionProvider : IInlineCompletionProvider
{
    public string Id => "scripting.rustweb.inline";
    public string Name => "Rust Web (Axum, Actix, Leptos, Wasm)";

    public IReadOnlyList<string> SupportedLanguages => ["rust", "rs", "toml"];

    private static readonly Dictionary<string, string> Completions = new(StringComparer.OrdinalIgnoreCase)
    {
        // Axum Web Server & Routing
        { "Router::new()", ".route(\"/\", get(root))\n    .route(\"/api/items\", post(create_item))\n    .with_state(state);" },
        { "async fn root(", ") -> &'static str {\n    \"Hello from Axum!\"\n}" },
        { "async fn handle_json(", "Json(payload): Json<CreateRequest>) -> impl IntoResponse {\n    Json(payload)\n}" },
        { "axum::serve(", "listener, app).await.unwrap();" },

        // Actix-Web
        { "HttpServer::new(", "|| {\n    App::new()\n        .route(\"/\", web::get().to(index))\n        .service(web::resource(\"/users\").to(users_handler))\n})\n.bind((\"127.0.0.1\", 8080))?\n.run()\n.await;" },
        { "async fn index(", ") -> impl Responder {\n    HttpResponse::Ok().body(\"Hello from Actix-Web!\")\n}" },

        // Leptos Reactive UI (Wasm)
        { "#[component]", "\npub fn App() -> impl IntoView {\n    let (count, set_count) = create_signal(0);\n\n    view! {\n        <main class=\"container mx-auto p-4\">\n            <h1 class=\"text-2xl font-bold\">\"Leptos Web App\"</h1>\n            <button on:click=move |_| set_count.update(|n| *n += 1)>\n                \"Clicks: \" {count}\n            </button>\n        </main>\n    }\n}" },
        { "view! {", "\n    <div class=\"app\">\n        <h1>\"Hello Leptos\"</h1>\n    </div>\n}" },
        { "let (count, set_count) = create_signal(", "0);" },

        // Dioxus UI (Wasm / Desktop / Mobile)
        { "fn app(cx: Scope) -> Element {", "\n    let mut count = use_signal(cx, || 0);\n\n    cx.render(rsx! {\n        div { class: \"p-4\",\n            h1 { \"Dioxus Counter: {count}\" }\n            button { onclick: move |_| count += 1, \"Increment\" }\n        }\n    })\n}" },

        // WebAssembly & wasm-bindgen
        { "#[wasm_bindgen]", "\npub fn greet(name: &str) {\n    web_sys::console::log_1(&format!(\"Hello, {}!\", name).into());\n}" },
        { "web_sys::window()", ".expect(\"global window does not exist\")" },

        // Cargo.toml Web / Wasm dependencies
        { "axum = ", "\"0.8\"" },
        { "tokio = ", "{ version = \"1\", features = [\"full\"] }" },
        { "leptos = ", "{ version = \"0.7\", features = [\"csr\"] }" },
        { "wasm-bindgen = ", "\"0.2\"" }
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
