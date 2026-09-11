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
    public string Description => "Comprehensive language support, syntax highlighting, completions, and toolchain checks for Rust, Lua, PowerShell, and Bash.";
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
        try
        {
            host.RegisterSyntaxHighlighting("rust", RustSyntaxDefinition.CreateDefinition());
        }
        catch (Exception ex)
        {
            host.Log($"Failed to register Rust syntax definition: {ex.Message}");
        }

        try
        {
            host.RegisterSyntaxHighlighting("lua", LuaSyntaxDefinition.CreateDefinition());
        }
        catch (Exception ex)
        {
            host.Log($"Failed to register Lua syntax definition: {ex.Message}");
        }

        try
        {
            host.RegisterSyntaxHighlighting("powershell", PowerShellSyntaxDefinition.CreateDefinition());
        }
        catch (Exception ex)
        {
            host.Log($"Failed to register PowerShell syntax definition: {ex.Message}");
        }

        try
        {
            host.RegisterSyntaxHighlighting("bash", BashSyntaxDefinition.CreateDefinition());
        }
        catch (Exception ex)
        {
            host.Log($"Failed to register Bash syntax definition: {ex.Message}");
        }

        // 3. Register Completion Providers
        host.RegisterInlineCompletion(new RustCompletionProvider());
        host.RegisterInlineCompletion(new LuaCompletionProvider());
        host.RegisterInlineCompletion(new PowerShellCompletionProvider());
        host.RegisterInlineCompletion(new BashCompletionProvider());

        // 4. Register Toolchain Checks
        host.RegisterToolchainCheck(new RustToolchainCheck());
        host.RegisterToolchainCheck(new LuaToolchainCheck());
        host.RegisterToolchainCheck(new PowerShellToolchainCheck());
        host.RegisterToolchainCheck(new BashToolchainCheck());

        host.Log("Scripting & Systems Language Pack initialized.");
        return Task.CompletedTask;
    }

    public Task DeinitializeAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

