using System.Collections.Generic;
using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Core.Services.Formatters;

/// <summary>
/// Prettier CLI adapter for JavaScript, TypeScript, JSON, CSS, SCSS, HTML, Markdown, and YAML.
/// </summary>
public class PrettierCliFormatter : CliDocumentFormatter
{
    public override string FormatterId => "cli.prettier";
    public override string DisplayName => "Prettier (CLI)";
    public override string ExecutableName => "prettier";
    public override string ArgumentsPattern => "--stdin-filepath sample.{language}";

    public override IReadOnlyList<string> SupportedLanguages =>
    [
        "javascript", "typescript", "js", "ts", "jsx", "tsx",
        "json", "css", "scss", "less", "html", "htm", "markdown", "md", "yaml", "yml"
    ];

    protected override string FormatArguments(string language, FormattingOptions options)
    {
        string ext = language switch
        {
            "javascript" or "js" => "js",
            "typescript" or "ts" => "ts",
            "jsx" => "jsx",
            "tsx" => "tsx",
            "json" => "json",
            "css" => "css",
            "scss" => "scss",
            "html" or "htm" => "html",
            "markdown" or "md" => "md",
            "yaml" or "yml" => "yaml",
            _ => "txt"
        };

        return $"--stdin-filepath sample.{ext} --tab-width {options.IndentSize} {(!options.InsertSpaces ? "--use-tabs" : "")}";
    }
}

/// <summary>
/// Black CLI adapter for Python.
/// </summary>
public class BlackCliFormatter : CliDocumentFormatter
{
    public override string FormatterId => "cli.black";
    public override string DisplayName => "Black (CLI)";
    public override string ExecutableName => "black";
    public override string ArgumentsPattern => "-q -";

    public override IReadOnlyList<string> SupportedLanguages => ["python", "py", ".py"];
}

/// <summary>
/// Ruff CLI adapter for Python.
/// </summary>
public class RuffCliFormatter : CliDocumentFormatter
{
    public override string FormatterId => "cli.ruff";
    public override string DisplayName => "Ruff (CLI)";
    public override string ExecutableName => "ruff";
    public override string ArgumentsPattern => "format -";

    public override IReadOnlyList<string> SupportedLanguages => ["python", "py", ".py"];
}

/// <summary>
/// GoFmt CLI adapter for Go.
/// </summary>
public class GoFmtCliFormatter : CliDocumentFormatter
{
    public override string FormatterId => "cli.gofmt";
    public override string DisplayName => "GoFmt (CLI)";
    public override string ExecutableName => "gofmt";
    public override string ArgumentsPattern => "";

    public override IReadOnlyList<string> SupportedLanguages => ["go", ".go"];
}

/// <summary>
/// RustFmt CLI adapter for Rust.
/// </summary>
public class RustFmtCliFormatter : CliDocumentFormatter
{
    public override string FormatterId => "cli.rustfmt";
    public override string DisplayName => "RustFmt (CLI)";
    public override string ExecutableName => "rustfmt";
    public override string ArgumentsPattern => "";

    public override IReadOnlyList<string> SupportedLanguages => ["rust", "rs", ".rs"];
}

/// <summary>
/// Dart Format CLI adapter for Dart and Flutter.
/// </summary>
public class DartFormatCliFormatter : CliDocumentFormatter
{
    public override string FormatterId => "cli.dartformat";
    public override string DisplayName => "Dart Format (CLI)";
    public override string ExecutableName => "dart";
    public override string ArgumentsPattern => "format";

    public override IReadOnlyList<string> SupportedLanguages => ["dart", ".dart"];
}
