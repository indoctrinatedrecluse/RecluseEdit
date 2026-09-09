using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Extensions.Python.Providers;

/// <summary>
/// Provides inline autocomplete suggestions and snippets for modern Python frontend and UI frameworks:
/// Streamlit, Gradio, and Reflex.
/// </summary>
public class PythonFrontendCompletionProvider : IInlineCompletionProvider
{
    public string Id => "python.frontends.completion";
    public string Name => "Python Frontends (Streamlit, Gradio, Reflex) Completions";

    public IReadOnlyList<string> SupportedLanguages => ["python"];

    private static readonly Dictionary<string, string> Completions = new()
    {
        // Streamlit Snippets
        { "import streamlit as ", "st" },
        { "st.title(", "\"Application Title\")" },
        { "st.header(", "\"Section Header\")" },
        { "st.subheader(", "\"Sub-section\")" },
        { "st.write(", "\"Display message or data\")" },
        { "st.button(", "\"Submit\", type=\"primary\")" },
        { "st.checkbox(", "\"I accept the terms\")" },
        { "st.text_input(", "\"Enter text:\", placeholder=\"Type here...\")" },
        { "st.selectbox(", "\"Select option:\", options=[\"Option A\", \"Option B\"])" },
        { "st.multiselect(", "\"Select choices:\", options=[\"Choice 1\", \"Choice 2\"])" },
        { "st.dataframe(", "df, use_container_width=True)" },
        { "st.sidebar.", "header(\"Sidebar Navigation\")" },
        { "st.columns(", "2)" },
        { "st.plotly_chart(", "fig, use_container_width=True)" },
        { "st.metric(", "label=\"Temperature\", value=\"70 °F\", delta=\"1.2 °F\")" },

        // Gradio Snippets
        { "import gradio as ", "gr" },
        { "gr.Interface(", "fn=predict, inputs=gr.Textbox(label=\"Input\"), outputs=\"text\").launch()" },
        {
            "with gr.Blocks(",
            ") as demo:\n    gr.Markdown(\"# Interactive Dashboard\")\n    with gr.Row():\n        inp = gr.Textbox(label=\"Prompt\")\n        out = gr.Textbox(label=\"Output\")\n    btn = gr.Button(\"Run\")\n    btn.click(fn=process_input, inputs=inp, outputs=out)\ndemo.launch()"
        },
        { "gr.Textbox(", "label=\"Input Prompt\", lines=2, placeholder=\"Enter text...\")" },
        { "gr.Number(", "label=\"Count\", value=0)" },
        { "gr.Image(", "type=\"pil\", label=\"Upload Image\")" },

        // Reflex Snippets
        { "import reflex as ", "rx" },
        {
            "class State(rx.State):",
            "\n    count: int = 0\n\n    def increment(self):\n        self.count += 1"
        },
        {
            "def index() -> rx.Component:",
            "\n    return rx.vstack(\n        rx.heading(\"Welcome to Reflex!\", size=\"9\"),\n        rx.button(\"Click me\", on_click=State.increment),\n        spacing=\"5\",\n        align=\"center\",\n    )"
        },
        { "app = rx.App(", "state=State)" }
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
