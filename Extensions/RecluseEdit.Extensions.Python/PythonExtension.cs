using RecluseEdit.Extensions.Python.Providers;
using RecluseEdit.Extensions.Python.Syntaxes;
using RecluseEdit.Extensions.Python.Toolchains;
using RecluseEdit.Sdk;
using RecluseEdit.Sdk.Models;

namespace RecluseEdit.Extensions.Python;

/// <summary>
/// RecluseEdit extension providing language support, syntax highlighting, completions, and compiler checks
/// for modern Python, Python frontends (Streamlit, Gradio, Reflex), web frameworks (Flask, Django, FastAPI),
/// Express.js, and Jinja2 / Django templates.
/// </summary>
public class PythonExtension : IExtension
{
    public string Id => "recluse.python";
    public string Name => "Python & Full-Stack Web Pack";
    public string Version => "1.0.0";
    public string Description => "Comprehensive Python, Python frontends (Streamlit, Gradio, Reflex), web frameworks (Flask, Django, FastAPI), Express.js, Jinja templates, and runtime toolchain checks.";
    public string Author => "indoctrinatedrecluse";

    public Task InitializeAsync(IExtensionHost host, CancellationToken cancellationToken = default)
    {
        // 1. Register Languages
        host.RegisterLanguage(new LanguageDefinition
        {
            Id = "python",
            DisplayName = "Python",
            Extensions = [".py", ".pyw", ".pyi", ".pyd"],
            HighlightingName = "Python"
        });

        host.RegisterLanguage(new LanguageDefinition
        {
            Id = "jinja",
            DisplayName = "Jinja2 / Django Template",
            Extensions = [".jinja", ".jinja2", ".j2", ".html.jinja", ".djhtml"],
            HighlightingName = "Jinja"
        });

        // 2. Register Custom Syntax Highlighting Definitions
        try
        {
            var pythonDef = PythonSyntaxDefinition.CreateDefinition();
            host.RegisterSyntaxHighlighting("python", pythonDef);
        }
        catch (Exception ex)
        {
            host.Log($"Failed to register Python syntax definition: {ex.Message}");
        }

        try
        {
            var jinjaDef = JinjaSyntaxDefinition.CreateDefinition();
            host.RegisterSyntaxHighlighting("jinja", jinjaDef);
        }
        catch (Exception ex)
        {
            host.Log($"Failed to register Jinja syntax definition: {ex.Message}");
        }

        // 3. Register Inline Autocomplete & Snippet Providers
        host.RegisterInlineCompletion(new PythonCompletionProvider());
        host.RegisterInlineCompletion(new FlaskDjangoCompletionProvider());
        host.RegisterInlineCompletion(new PythonFrontendCompletionProvider());
        host.RegisterInlineCompletion(new ExpressCompletionProvider());

        // 4. Register Toolchain Checks
        host.RegisterToolchainCheck(new PythonToolchainCheck());
        host.RegisterToolchainCheck(new PipToolchainCheck());
        host.RegisterToolchainCheck(new DjangoToolchainCheck());

        host.Log("Python & Full-Stack Web Pack initialized.");
        return Task.CompletedTask;
    }

    public Task DeinitializeAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

