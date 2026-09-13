using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using RecluseEdit.Core.Models;

namespace RecluseEdit.Core.Services;

/// <summary>
/// Service responsible for validating editor text buffers and discovering syntax diagnostics.
/// </summary>
public class DiagnosticService
{
    public IReadOnlyList<DiagnosticItem> AnalyzeDocument(string text, string language, string filePath)
    {
        var diagnostics = new List<DiagnosticItem>();
        if (string.IsNullOrEmpty(text))
            return diagnostics;

        var lang = language?.ToLowerInvariant() ?? string.Empty;
        var ext = Path.GetExtension(filePath)?.ToLowerInvariant() ?? string.Empty;

        // 1. JSON Diagnostics
        if (lang == "json" || ext == ".json")
        {
            diagnostics.AddRange(AnalyzeJson(text, filePath));
        }

        // 2. Bracket Balance Diagnostics
        diagnostics.AddRange(AnalyzeBrackets(text, filePath));

        // 3. HTML / XML tag matching
        if (lang is "html" or "htm" or "xhtml" or "xml" or "xaml" or "vue" or "svelte" or "astro" ||
            ext is ".html" or ".htm" or ".xml" or ".xaml" or ".vue" or ".svelte" or ".astro")
        {
            diagnostics.AddRange(AnalyzeHtml(text, filePath));
        }

        return diagnostics;
    }

    public static List<DiagnosticItem> AnalyzeJson(string json, string filePath)
    {
        var items = new List<DiagnosticItem>();
        try
        {
            using var doc = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            int line = (int)(ex.LineNumber.HasValue ? ex.LineNumber.Value + 1 : 1);
            int col = (int)(ex.BytePositionInLine.HasValue ? ex.BytePositionInLine.Value + 1 : 1);

            items.Add(new DiagnosticItem
            {
                FilePath = filePath,
                LineNumber = line,
                ColumnNumber = col,
                Message = $"JSON Syntax Error: {ex.Message}",
                Severity = DiagnosticSeverity.Error,
                Source = "JSON Parser"
            });
        }
        return items;
    }

    public static List<DiagnosticItem> AnalyzeBrackets(string text, string filePath)
    {
        var items = new List<DiagnosticItem>();
        var stack = new Stack<(char bracket, int line, int col)>();

        int line = 1;
        int col = 1;
        bool inSingleQuote = false;
        bool inDoubleQuote = false;
        bool inComment = false;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            char next = i + 1 < text.Length ? text[i + 1] : '\0';

            if (c == '\n')
            {
                line++;
                col = 1;
                inComment = false;
                continue;
            }
            if (c == '\r') continue;

            if (!inSingleQuote && !inDoubleQuote && c == '/' && next == '/')
            {
                inComment = true;
            }

            if (inComment)
            {
                col++;
                continue;
            }

            if (c == '"' && (i == 0 || text[i - 1] != '\\') && !inSingleQuote)
            {
                inDoubleQuote = !inDoubleQuote;
                col++;
                continue;
            }

            if (c == '\'' && (i == 0 || text[i - 1] != '\\') && !inDoubleQuote)
            {
                inSingleQuote = !inSingleQuote;
                col++;
                continue;
            }

            if (inSingleQuote || inDoubleQuote)
            {
                col++;
                continue;
            }

            if (c is '{' or '(' or '[')
            {
                stack.Push((c, line, col));
            }
            else if (c is '}' or ')' or ']')
            {
                if (stack.Count == 0)
                {
                    items.Add(new DiagnosticItem
                    {
                        FilePath = filePath,
                        LineNumber = line,
                        ColumnNumber = col,
                        Message = $"Unexpected closing bracket '{c}' without matching opening bracket.",
                        Severity = DiagnosticSeverity.Error,
                        Source = "Bracket Matcher"
                    });
                }
                else
                {
                    var (opening, openLine, openCol) = stack.Pop();
                    bool matches = (opening == '{' && c == '}') ||
                                   (opening == '(' && c == ')') ||
                                   (opening == '[' && c == ']');
                    if (!matches)
                    {
                        items.Add(new DiagnosticItem
                        {
                            FilePath = filePath,
                            LineNumber = line,
                            ColumnNumber = col,
                            Message = $"Mismatched bracket: expected match for '{opening}' from line {openLine}, col {openCol}, but found '{c}'.",
                            Severity = DiagnosticSeverity.Error,
                            Source = "Bracket Matcher"
                        });
                    }
                }
            }

            col++;
        }

        while (stack.Count > 0)
        {
            var (unclosed, uLine, uCol) = stack.Pop();
            items.Add(new DiagnosticItem
            {
                FilePath = filePath,
                LineNumber = uLine,
                ColumnNumber = uCol,
                Message = $"Unclosed bracket '{unclosed}'.",
                Severity = DiagnosticSeverity.Warning,
                Source = "Bracket Matcher"
            });
        }

        return items;
    }

    private static readonly HashSet<string> VoidHtmlTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "area", "base", "br", "col", "embed", "hr", "img", "input", "link", "meta", "param", "source", "track", "wbr", "!doctype"
    };

    public static List<DiagnosticItem> AnalyzeHtml(string text, string filePath)
    {
        var items = new List<DiagnosticItem>();
        var tagPattern = new Regex(@"<(/?)([a-zA-Z0-9_\-\:]+)[^>]*?(/?)>", RegexOptions.Singleline);
        var matches = tagPattern.Matches(text);
        var tagStack = new Stack<(string tag, int line, int col)>();

        int GetLine(int index) => text.AsSpan(0, index).Count('\n') + 1;
        int GetCol(int index)
        {
            int lastNl = text.LastIndexOf('\n', index);
            return lastNl < 0 ? index + 1 : index - lastNl;
        }

        foreach (Match m in matches)
        {
            bool isClose = m.Groups[1].Value == "/";
            string tagName = m.Groups[2].Value.ToLowerInvariant();
            bool isSelfClose = m.Groups[3].Value == "/" || m.Value.EndsWith("/>");

            if (VoidHtmlTags.Contains(tagName) || isSelfClose || tagName.StartsWith("!--"))
                continue;

            int line = GetLine(m.Index);
            int col = GetCol(m.Index);

            if (!isClose)
            {
                tagStack.Push((tagName, line, col));
            }
            else
            {
                if (tagStack.Count == 0)
                {
                    items.Add(new DiagnosticItem
                    {
                        FilePath = filePath,
                        LineNumber = line,
                        ColumnNumber = col,
                        Message = $"Unexpected closing tag '</{tagName}>' with no matching opening tag.",
                        Severity = DiagnosticSeverity.Warning,
                        Source = "HTML Validator"
                    });
                }
                else
                {
                    var (opened, oLine, oCol) = tagStack.Pop();
                    if (!string.Equals(opened, tagName, StringComparison.OrdinalIgnoreCase))
                    {
                        items.Add(new DiagnosticItem
                        {
                            FilePath = filePath,
                            LineNumber = line,
                            ColumnNumber = col,
                            Message = $"Mismatched tag: opening '<{opened}>' at line {oLine} closed by '</{tagName}>'.",
                            Severity = DiagnosticSeverity.Warning,
                            Source = "HTML Validator"
                        });
                    }
                }
            }
        }

        while (tagStack.Count > 0)
        {
            var (unclosed, uLine, uCol) = tagStack.Pop();
            items.Add(new DiagnosticItem
            {
                FilePath = filePath,
                LineNumber = uLine,
                ColumnNumber = uCol,
                Message = $"Unclosed HTML tag '<{unclosed}>'.",
                Severity = DiagnosticSeverity.Information,
                Source = "HTML Validator"
            });
        }

        return items;
    }
}
