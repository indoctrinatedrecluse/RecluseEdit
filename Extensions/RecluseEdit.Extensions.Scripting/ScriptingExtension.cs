using System;
using System.Threading;
using System.Threading.Tasks;
using RecluseEdit.Extensions.Scripting.Providers;
using RecluseEdit.Extensions.Scripting.Syntaxes;
using RecluseEdit.Extensions.Scripting.Toolchains;
using RecluseEdit.Sdk;
using RecluseEdit.Sdk.Models;

namespace RecluseEdit.Extensions.Scripting;

/// <summary>
/// RecluseEdit extension providing language support, Dark+ syntax highlighting,
/// completions, and toolchain checks for Rust, Lua, PowerShell, and Bash.
/// </summary>
public class ScriptingExtension : IExtension
{
    public string Id => "recluse.scripting";
    public string Name => "Scripting & Systems Language Pack";
    public string Version => "1.0.0";
    public string Description => "Comprehensive language support, syntax highlighting, completions, and toolchain checks for Rust, WebAssembly, Cargo/TOML, Java (Spring Boot), Kotlin (Ktor), Elixir (Phoenix), Lua, PowerShell, and Bash.";
    public string Author => "indoctrinatedrecluse";

    public Task InitializeAsync(IExtensionHost host, CancellationToken cancellationToken = default)
    {
        // 1. Register Languages
        host.RegisterLanguage(new LanguageDefinition
        {
            Id = "rust",
            DisplayName = "Rust",
            Extensions = [".rs"],
            HighlightingName = "Rust"
        });

        host.RegisterLanguage(new LanguageDefinition
        {
            Id = "toml",
            DisplayName = "TOML / Cargo",
            Extensions = [".toml"],
            HighlightingName = "TOML"
        });

        host.RegisterLanguage(new LanguageDefinition
        {
            Id = "java",
            DisplayName = "Java / Spring Boot",
            Extensions = [".java"],
            HighlightingName = "Java"
        });

        host.RegisterLanguage(new LanguageDefinition
        {
            Id = "kotlin",
            DisplayName = "Kotlin / Ktor",
            Extensions = [".kt", ".kts"],
            HighlightingName = "Kotlin"
        });

        host.RegisterLanguage(new LanguageDefinition
        {
            Id = "elixir",
            DisplayName = "Elixir / Phoenix LiveView",
            Extensions = [".ex", ".exs", ".heex", ".eex"],
            HighlightingName = "Elixir"
        });

        host.RegisterLanguage(new LanguageDefinition
        {
            Id = "lua",
            DisplayName = "Lua",
            Extensions = [".lua"],
            HighlightingName = "Lua"
        });

        host.RegisterLanguage(new LanguageDefinition
        {
            Id = "powershell",
            DisplayName = "PowerShell",
            Extensions = [".ps1", ".psm1", ".psd1"],
            HighlightingName = "PowerShell"
        });

        host.RegisterLanguage(new LanguageDefinition
        {
            Id = "bash",
            DisplayName = "Bash",
            Extensions = [".sh", ".bash", ".zsh", ".ksh", ".command"],
            HighlightingName = "Bash"
        });

        // 2. Register Syntax Definitions
        RegisterSyntaxSafe(host, "rust", () => RustSyntaxDefinition.CreateDefinition());
        RegisterSyntaxSafe(host, "toml", () => TomlSyntaxDefinition.CreateDefinition());
        RegisterSyntaxSafe(host, "java", () => JavaSyntaxDefinition.CreateDefinition());
        RegisterSyntaxSafe(host, "kotlin", () => KotlinSyntaxDefinition.CreateDefinition());
        RegisterSyntaxSafe(host, "elixir", () => ElixirSyntaxDefinition.CreateDefinition());
        RegisterSyntaxSafe(host, "lua", () => LuaSyntaxDefinition.CreateDefinition());
        RegisterSyntaxSafe(host, "powershell", () => PowerShellSyntaxDefinition.CreateDefinition());
        RegisterSyntaxSafe(host, "bash", () => BashSyntaxDefinition.CreateDefinition());

        // 3. Register Completion Providers
        host.RegisterInlineCompletion(new RustCompletionProvider());
        host.RegisterInlineCompletion(new RustWebCompletionProvider());
        host.RegisterInlineCompletion(new SpringBootCompletionProvider());
        host.RegisterInlineCompletion(new KtorCompletionProvider());
        host.RegisterInlineCompletion(new PhoenixCompletionProvider());
        host.RegisterInlineCompletion(new LuaCompletionProvider());
        host.RegisterInlineCompletion(new PowerShellCompletionProvider());
        host.RegisterInlineCompletion(new BashCompletionProvider());

        // 4. Register Toolchain Checks
        host.RegisterToolchainCheck(new RustToolchainCheck());
        host.RegisterToolchainCheck(new TrunkToolchainCheck());
        host.RegisterToolchainCheck(new WasmPackToolchainCheck());
        host.RegisterToolchainCheck(new JavaToolchainCheck());
        host.RegisterToolchainCheck(new MavenToolchainCheck());
        host.RegisterToolchainCheck(new GradleToolchainCheck());
        host.RegisterToolchainCheck(new ElixirToolchainCheck());
        host.RegisterToolchainCheck(new LuaToolchainCheck());
        host.RegisterToolchainCheck(new PowerShellToolchainCheck());
        host.RegisterToolchainCheck(new BashToolchainCheck());

        // 5. Register Side Panel
        host.RegisterSidePanel(new RegexWorkbenchSidePanelProvider());

        host.Log("Scripting & Systems Language Pack initialized.");
        return Task.CompletedTask;
    }

    private static void RegisterSyntaxSafe(IExtensionHost host, string langId, Func<ICSharpCode.AvalonEdit.Highlighting.IHighlightingDefinition> factory)
    {
        try
        {
            host.RegisterSyntaxHighlighting(langId, factory());
        }
        catch (Exception ex)
        {
            host.Log($"Failed to register {langId} syntax definition: {ex.Message}");
        }
    }

    public Task DeinitializeAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

