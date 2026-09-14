using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Extensions.Scripting.Providers;

/// <summary>
/// Provides inline completions for Elixir & Phoenix LiveView reactive server-rendered web applications.
/// </summary>
public class PhoenixCompletionProvider : IInlineCompletionProvider
{
    public string Id => "scripting.phoenix.inline";
    public string Name => "Elixir & Phoenix LiveView Framework";

    public IReadOnlyList<string> SupportedLanguages => ["elixir", "ex", "exs", "heex", "eex"];

    private static readonly Dictionary<string, string> Completions = new(StringComparer.OrdinalIgnoreCase)
    {
        // LiveView Module Skeleton
        { "use Phoenix.LiveView", "\n\n  @impl true\n  def mount(_params, _session, socket) {\n    {:ok, assign(socket, count: 0)}\n  }\n\n  @impl true\n  def handle_event(\"increment\", _params, socket) {\n    {:noreply, update(socket, :count, &(&1 + 1))}\n  }\n\n  @impl true\n  def render(assigns) {\n    ~H\"\"\"\n    <div class=\"p-4\">\n      <h1>Counter: <%= @count %></h1>\n      <button phx-click=\"increment\">+1</button>\n    </div>\n    \"\"\"\n  }" },

        // Phoenix LiveView Hooks
        { "def mount(_params, ", "_session, socket) do\n    {:ok, assign(socket, :items, [])}\n  end" },
        { "def handle_event(\"", "save\", %{\"item\" => params}, socket) do\n    {:noreply, assign(socket, :status, :saved)}\n  end" },
        { "def handle_info(", "{:item_updated, item}, socket) do\n    {:noreply, assign(socket, :item, item)}\n  end" },

        // HEEx templates
        { "~H\"\"\"", "\n    <div class=\"container mx-auto\">\n      <.header>\n        Listing Items\n        <:actions>\n          <.link patch={~p\"/items/new\"}>\n            <.button>New Item</.button>\n          </.link>\n        </:actions>\n      </.header>\n    </div>\n    \"\"\"" },
        { "<.table id=\"items\" ", "rows={@items}>\n      <:col :let={item} label=\"Name\"><%= item.name %></:col>\n    </.table>" }
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
