using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ICSharpCode.AvalonEdit.Highlighting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RecluseEdit.Extensions.Scripting;
using RecluseEdit.Extensions.Scripting.Providers;
using RecluseEdit.Extensions.Scripting.Syntaxes;
using RecluseEdit.Extensions.Scripting.Toolchains;
using RecluseEdit.Sdk;
using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Tests;

[TestClass]
public sealed class ScriptingExtensionTests
{
    private class MockExtensionHost : IExtensionHost
    {
        public List<LanguageDefinition> Languages { get; } = [];
        public List<IInlineCompletionProvider> InlineProviders { get; } = [];
        public List<IIntelliSenseProvider> IntelliSenseProviders { get; } = [];
        public List<IToolchainCheck> ToolchainChecks { get; } = [];
        public Dictionary<string, IHighlightingDefinition> Syntaxes { get; } = new(StringComparer.OrdinalIgnoreCase);
        public List<string> Logs { get; } = [];

        public void RegisterLanguage(LanguageDefinition language) => Languages.Add(language);
        public void RegisterInlineCompletion(IInlineCompletionProvider provider) => InlineProviders.Add(provider);
        public void RegisterIntelliSense(IIntelliSenseProvider provider) => IntelliSenseProviders.Add(provider);
        public void RegisterToolchainCheck(IToolchainCheck toolchainCheck) => ToolchainChecks.Add(toolchainCheck);
        public void RegisterSyntaxHighlighting(string languageId, IHighlightingDefinition definition) => Syntaxes[languageId] = definition;
        public IReadOnlyList<LanguageDefinition> GetRegisteredLanguages() => Languages;
        public void Log(string message) => Logs.Add(message);
    }

    private static InlineCompletionContext CreateContext(string lineText, string lang) => new()
    {
        CurrentLineText = lineText,
        TextBeforeCaret = lineText,
        CaretOffset = lineText.Length,
        LineNumber = 1,
        ColumnNumber = lineText.Length,
        LanguageId = lang,
        FullText = lineText
    };

    [TestMethod]
    public void TestScriptingMetadata()
    {
        var ext = new ScriptingExtension();
        Assert.AreEqual("recluse.scripting", ext.Id);
        Assert.AreEqual("Scripting & Systems Language Pack", ext.Name);
        Assert.AreEqual("1.0.0", ext.Version);
        Assert.AreEqual("indoctrinatedrecluse", ext.Author);
        Assert.Contains("Rust", ext.Description);
        Assert.Contains("Lua", ext.Description);
        Assert.Contains("PowerShell", ext.Description);
        Assert.Contains("Bash", ext.Description);
    }

    [TestMethod]
    public async Task TestScriptingRegistration()
    {
        var ext = new ScriptingExtension();
        var host = new MockExtensionHost();

        await ext.InitializeAsync(host);

        // 1. Verify all 4 Languages are registered
        Assert.IsTrue(host.Languages.Any(l => l.Id == "rust" && l.Extensions.Contains(".rs")));
        Assert.IsTrue(host.Languages.Any(l => l.Id == "lua" && l.Extensions.Contains(".lua")));
        Assert.IsTrue(host.Languages.Any(l => l.Id == "powershell" && l.Extensions.Contains(".ps1")));
        Assert.IsTrue(host.Languages.Any(l => l.Id == "bash" && l.Extensions.Contains(".sh")));

        // 2. Verify all 4 Syntax Highlighting definitions
        Assert.IsTrue(host.Syntaxes.ContainsKey("rust"));
        Assert.AreEqual("Rust", host.Syntaxes["rust"].Name);

        Assert.IsTrue(host.Syntaxes.ContainsKey("lua"));
        Assert.AreEqual("Lua", host.Syntaxes["lua"].Name);

        Assert.IsTrue(host.Syntaxes.ContainsKey("powershell"));
        Assert.AreEqual("PowerShell", host.Syntaxes["powershell"].Name);

        Assert.IsTrue(host.Syntaxes.ContainsKey("bash"));
        Assert.AreEqual("Bash", host.Syntaxes["bash"].Name);

        // 3. Verify all 4 Inline Completion Providers
        Assert.HasCount(4, host.InlineProviders);
        Assert.IsTrue(host.InlineProviders.Any(p => p.Id == "rust.core.completion"));
        Assert.IsTrue(host.InlineProviders.Any(p => p.Id == "lua.core.completion"));
        Assert.IsTrue(host.InlineProviders.Any(p => p.Id == "powershell.core.completion"));
        Assert.IsTrue(host.InlineProviders.Any(p => p.Id == "bash.core.completion"));

        // 4. Verify all 4 Toolchain Checks
        Assert.HasCount(4, host.ToolchainChecks);
        Assert.IsTrue(host.ToolchainChecks.Any(c => c.Command == "rustc"));
        Assert.IsTrue(host.ToolchainChecks.Any(c => c.Command == "lua"));
        Assert.IsTrue(host.ToolchainChecks.Any(c => c.Command == "pwsh"));
        Assert.IsTrue(host.ToolchainChecks.Any(c => c.Command == "bash"));
    }

    [TestMethod]
    public void TestRustSyntaxDefinition()
    {
        var def = RustSyntaxDefinition.CreateDefinition();
        Assert.IsNotNull(def);
        Assert.AreEqual("Rust", def.Name);

        var colors = def.NamedHighlightingColors.Select(c => c.Name).ToList();
        CollectionAssert.Contains(colors, "Keywords");
        CollectionAssert.Contains(colors, "ControlFlow");
        CollectionAssert.Contains(colors, "Types");
        CollectionAssert.Contains(colors, "Macros");
        CollectionAssert.Contains(colors, "Attributes");
        CollectionAssert.Contains(colors, "Lifetimes");
        CollectionAssert.Contains(colors, "Comment");
        CollectionAssert.Contains(colors, "DocComment");
        CollectionAssert.Contains(colors, "String");
        CollectionAssert.Contains(colors, "Char");
        CollectionAssert.Contains(colors, "Digits");
        CollectionAssert.Contains(colors, "Operators");
    }

    [TestMethod]
    public void TestLuaSyntaxDefinition()
    {
        var def = LuaSyntaxDefinition.CreateDefinition();
        Assert.IsNotNull(def);
        Assert.AreEqual("Lua", def.Name);

        var colors = def.NamedHighlightingColors.Select(c => c.Name).ToList();
        CollectionAssert.Contains(colors, "Keywords");
        CollectionAssert.Contains(colors, "ControlFlow");
        CollectionAssert.Contains(colors, "Builtins");
        CollectionAssert.Contains(colors, "SpecialVars");
        CollectionAssert.Contains(colors, "Comment");
        CollectionAssert.Contains(colors, "String");
        CollectionAssert.Contains(colors, "Digits");
        CollectionAssert.Contains(colors, "Operators");
    }

    [TestMethod]
    public void TestPowerShellSyntaxDefinition()
    {
        var def = PowerShellSyntaxDefinition.CreateDefinition();
        Assert.IsNotNull(def);
        Assert.AreEqual("PowerShell", def.Name);

        var colors = def.NamedHighlightingColors.Select(c => c.Name).ToList();
        CollectionAssert.Contains(colors, "Keywords");
        CollectionAssert.Contains(colors, "ControlFlow");
        CollectionAssert.Contains(colors, "Cmdlets");
        CollectionAssert.Contains(colors, "Variables");
        CollectionAssert.Contains(colors, "Parameters");
        CollectionAssert.Contains(colors, "Types");
        CollectionAssert.Contains(colors, "Operators");
        CollectionAssert.Contains(colors, "Comment");
        CollectionAssert.Contains(colors, "String");
        CollectionAssert.Contains(colors, "Digits");
    }

    [TestMethod]
    public void TestBashSyntaxDefinition()
    {
        var def = BashSyntaxDefinition.CreateDefinition();
        Assert.IsNotNull(def);
        Assert.AreEqual("Bash", def.Name);

        var colors = def.NamedHighlightingColors.Select(c => c.Name).ToList();
        CollectionAssert.Contains(colors, "Keywords");
        CollectionAssert.Contains(colors, "ControlFlow");
        CollectionAssert.Contains(colors, "Builtins");
        CollectionAssert.Contains(colors, "Commands");
        CollectionAssert.Contains(colors, "Shebang");
        CollectionAssert.Contains(colors, "Variables");
        CollectionAssert.Contains(colors, "Comment");
        CollectionAssert.Contains(colors, "String");
        CollectionAssert.Contains(colors, "Digits");
        CollectionAssert.Contains(colors, "Operators");
    }

    [TestMethod]
    public async Task TestRustCompletionProvider()
    {
        var provider = new RustCompletionProvider();
        Assert.Contains("rust", provider.SupportedLanguages);

        var resMain = await provider.GetInlineSuggestionAsync(CreateContext("fn mai", "rust"));
        Assert.IsNotNull(resMain);
        Assert.Contains("println!", resMain);

        var resDerive = await provider.GetInlineSuggestionAsync(CreateContext("#[der", "rust"));
        Assert.IsNotNull(resDerive);
        Assert.Contains("Debug", resDerive);

        var resMatch = await provider.GetInlineSuggestionAsync(CreateContext("    match ", "rust"));
        Assert.IsNotNull(resMatch);
        Assert.Contains("result", resMatch);

        var resNone = await provider.GetInlineSuggestionAsync(CreateContext("unrelated_code", "rust"));
        Assert.IsNull(resNone);
    }

    [TestMethod]
    public async Task TestLuaCompletionProvider()
    {
        var provider = new LuaCompletionProvider();
        Assert.Contains("lua", provider.SupportedLanguages);

        var resFunc = await provider.GetInlineSuggestionAsync(CreateContext("local function ", "lua"));
        Assert.IsNotNull(resFunc);
        Assert.Contains("end", resFunc);

        var resPairs = await provider.GetInlineSuggestionAsync(CreateContext("    for k, v in pair", "lua"));
        Assert.IsNotNull(resPairs);
        Assert.Contains("s(tbl)", resPairs);

        var resNone = await provider.GetInlineSuggestionAsync(CreateContext("x = 10", "lua"));
        Assert.IsNull(resNone);
    }

    [TestMethod]
    public async Task TestPowerShellCompletionProvider()
    {
        var provider = new PowerShellCompletionProvider();
        Assert.Contains("powershell", provider.SupportedLanguages);

        var resFunc = await provider.GetInlineSuggestionAsync(CreateContext("function ", "powershell"));
        Assert.IsNotNull(resFunc);
        Assert.Contains("CmdletBinding", resFunc);

        var resTry = await provider.GetInlineSuggestionAsync(CreateContext("try", "powershell"));
        Assert.IsNotNull(resTry);
        Assert.Contains("catch", resTry);

        var resNone = await provider.GetInlineSuggestionAsync(CreateContext("Write-Output 'hello'", "powershell"));
        Assert.IsNull(resNone);
    }

    [TestMethod]
    public async Task TestBashCompletionProvider()
    {
        var provider = new BashCompletionProvider();
        Assert.Contains("bash", provider.SupportedLanguages);

        var resShebang = await provider.GetInlineSuggestionAsync(CreateContext("#!/", "bash"));
        Assert.IsNotNull(resShebang);
        Assert.Contains("usr/bin/env bash", resShebang);

        var resIf = await provider.GetInlineSuggestionAsync(CreateContext("if [[ -f ", "bash"));
        Assert.IsNotNull(resIf);
        Assert.Contains("fi", resIf);

        var resNone = await provider.GetInlineSuggestionAsync(CreateContext("ls -la", "bash"));
        Assert.IsNull(resNone);
    }

    [TestMethod]
    public async Task TestScriptingToolchains()
    {
        var rust = new RustToolchainCheck();
        Assert.AreEqual("Rust Compiler", rust.ToolName);
        Assert.AreEqual("rustc", rust.Command);
        var rustReport = await rust.CheckAsync();
        Assert.IsNotNull(rustReport);
        Assert.IsTrue(rustReport.Status is ToolchainStatus.Available or ToolchainStatus.Warning or ToolchainStatus.Missing);

        var lua = new LuaToolchainCheck();
        Assert.AreEqual("Lua Runtime", lua.ToolName);
        Assert.AreEqual("lua", lua.Command);
        var luaReport = await lua.CheckAsync();
        Assert.IsNotNull(luaReport);
        Assert.IsTrue(luaReport.Status is ToolchainStatus.Available or ToolchainStatus.Warning or ToolchainStatus.Missing);

        var pwsh = new PowerShellToolchainCheck();
        Assert.AreEqual("PowerShell Engine", pwsh.ToolName);
        Assert.AreEqual("pwsh", pwsh.Command);
        var pwshReport = await pwsh.CheckAsync();
        Assert.IsNotNull(pwshReport);
        Assert.IsTrue(pwshReport.Status is ToolchainStatus.Available or ToolchainStatus.Warning or ToolchainStatus.Missing);

        var bash = new BashToolchainCheck();
        Assert.AreEqual("Bash Shell", bash.ToolName);
        Assert.AreEqual("bash", bash.Command);
        var bashReport = await bash.CheckAsync();
        Assert.IsNotNull(bashReport);
        Assert.IsTrue(bashReport.Status is ToolchainStatus.Available or ToolchainStatus.Warning or ToolchainStatus.Missing);
    }
}
