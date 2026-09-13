using System;

namespace RecluseEdit.Core.Models;

public enum DiagnosticSeverity
{
    Error,
    Warning,
    Information
}

public class DiagnosticItem
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName => string.IsNullOrEmpty(FilePath) ? "Document" : System.IO.Path.GetFileName(FilePath);
    public int LineNumber { get; set; } = 1;
    public int ColumnNumber { get; set; } = 1;
    public string Message { get; set; } = string.Empty;
    public DiagnosticSeverity Severity { get; set; } = DiagnosticSeverity.Error;
    public string Source { get; set; } = "Syntax";

    public string LocationDisplay => $"{FileName} ({LineNumber}, {ColumnNumber})";

    public string SeverityBadge => Severity switch
    {
        DiagnosticSeverity.Error => "❌",
        DiagnosticSeverity.Warning => "⚠️",
        _ => "ℹ️"
    };

    public string SeverityColor => Severity switch
    {
        DiagnosticSeverity.Error => "#F48771",
        DiagnosticSeverity.Warning => "#CCA700",
        _ => "#4EC9B0"
    };
}

