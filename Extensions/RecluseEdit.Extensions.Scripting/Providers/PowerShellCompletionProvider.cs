using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Extensions.Scripting.Providers;

/// <summary>
/// Provides inline ghost-text completions and snippets for PowerShell,
/// including advanced function definitions with [CmdletBinding()], params, loops, and error handling.
/// </summary>
public class PowerShellCompletionProvider : IInlineCompletionProvider
{
    public string Id => "powershell.core.completion";
    public string Name => "PowerShell Core Idioms & Cmdlets";
    public IReadOnlyList<string> SupportedLanguages => ["powershell"];

    private static readonly Dictionary<string, string> Completions = new(StringComparer.OrdinalIgnoreCase)
    {
        { "function ", "Get-ItemDetails {\n\t[CmdletBinding()]\n\tparam(\n\t\t[Parameter(Mandatory = $true, ValueFromPipeline = $true)]\n\t\t[string]$Path\n\t)\n\tprocess {\n\t\tWrite-Verbose \"Processing $Path\"\n\t}\n}" },
        { "[Cmdlet", "Binding()]\nparam(\n\t[Parameter(Mandatory = $true)]\n\t[string]$Name\n)" },
        { "try", " {\n\t$result = Invoke-RestMethod -Uri $endpoint\n}\ncatch {\n\tWrite-Error \"Failed: $($_.Exception.Message)\"\n}" },
        { "foreach ($item in ", "$items) {\n\tWrite-Host \"Item: $item\"\n}" },
        { "switch ($", "status) {\n\t\"Active\" { Write-Host \"Active\" }\n\t\"Pending\" { Write-Host \"Pending\" }\n\tdefault { Write-Host \"Unknown\" }\n}" },
        { "if ($null -eq $", "value) {\n\tthrow \"Value cannot be null.\"\n}" },
        { "Get-ChildI", "tem -Path $PSScriptRoot -Recurse -File" },
        { "New-Obj", "ect -TypeName PSObject -Property @{\n\tId = 1\n\tName = \"Example\"\n}" },
        { "Test-Pa", "th -Path $targetPath" },
        { "Write-Ho", "st \"[INFO] $message\" -ForegroundColor Cyan" }
    };

    public Task<string?> GetInlineSuggestionAsync(InlineCompletionContext context, CancellationToken cancellationToken = default)
    {
        var line = context.CurrentLineText.TrimStart();

        foreach (var (prefix, suggestion) in Completions)
        {
            if (line.EndsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult<string?>(suggestion);
            }
        }

        return Task.FromResult<string?>(null);
    }
}
