using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Core.Services;

/// <summary>
/// Service responsible for code prettification and formatting across supported languages.
/// </summary>
public class DocumentFormattingService
{
    private readonly List<IDocumentFormatter> _customFormatters = [];

    public void RegisterFormatter(IDocumentFormatter formatter)
    {
        if (!_customFormatters.Any(f => f.FormatterId == formatter.FormatterId))
        {
            _customFormatters.Add(formatter);
        }
    }

    public bool CanFormat(string language, string filePath)
    {
        var lang = language?.ToLowerInvariant() ?? string.Empty;
        var ext = Path.GetExtension(filePath)?.ToLowerInvariant() ?? string.Empty;

        if (_customFormatters.Any(f => f.CanFormat(lang, filePath)))
            return true;

        return lang switch
        {
            "html" or "htm" or "xhtml" or "vue" or "svelte" or "astro" or "xml" or "xaml" => true,
            "css" or "scss" or "less" => true,
            "json" => true,
            "javascript" or "typescript" or "js" or "ts" or "jsx" or "tsx" => true,
            "sql" => true,
            "markdown" or "md" => true,
            _ => ext switch
            {
                ".html" or ".htm" or ".vue" or ".svelte" or ".astro" or ".xml" or ".xaml" => true,
                ".css" or ".scss" or ".less" => true,
                ".json" => true,
                ".js" or ".ts" or ".jsx" or ".tsx" or ".mjs" or ".cjs" => true,
                ".sql" => true,
                ".md" or ".markdown" => true,
                _ => false
            }
        };
    }

    public string FormatDocument(string sourceCode, string language, string filePath, FormattingOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(sourceCode))
            return sourceCode;

        options ??= new FormattingOptions();
        var lang = language?.ToLowerInvariant() ?? string.Empty;
        var ext = Path.GetExtension(filePath)?.ToLowerInvariant() ?? string.Empty;

        // 1. Check custom registered formatters
        var custom = _customFormatters.FirstOrDefault(f => f.CanFormat(lang, filePath));
        if (custom != null)
        {
            try
            {
                return custom.Format(sourceCode, lang, options);
            }
            catch
            {
                // Fall back to built-in
            }
        }

        // 2. Built-in Formatters
        try
        {
            string formatted = (lang, ext) switch
            {
                ("json", _) or (_, ".json") => FormatJson(sourceCode, options),
                ("css" or "scss" or "less", _) or (_, ".css" or ".scss" or ".less") => FormatCss(sourceCode, options),
                ("html" or "htm" or "xhtml" or "xml" or "xaml" or "vue" or "svelte" or "astro", _) or
                (_, ".html" or ".htm" or ".xml" or ".xaml" or ".vue" or ".svelte" or ".astro") => FormatHtml(sourceCode, options),
                ("sql", _) or (_, ".sql") => FormatSql(sourceCode, options),
                ("markdown" or "md", _) or (_, ".md" or ".markdown") => FormatMarkdown(sourceCode, options),
                ("javascript" or "typescript" or "js" or "ts" or "jsx" or "tsx", _) or
                (_, ".js" or ".ts" or ".jsx" or ".tsx" or ".mjs" or ".cjs") => FormatJsTs(sourceCode, options),
                _ => sourceCode
            };

            if (options.TrimTrailingWhitespace)
            {
                var lines = formatted.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
                formatted = string.Join(Environment.NewLine, lines.Select(l => l.TrimEnd()));
            }

            if (options.InsertFinalNewline && !formatted.EndsWith(Environment.NewLine))
            {
                formatted += Environment.NewLine;
            }

            return formatted;
        }
        catch
        {
            return sourceCode;
        }
    }

    #region JSON Formatter

    public static string FormatJson(string json, FormattingOptions options)
    {
        try
        {
            using var doc = JsonDocument.Parse(json, new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });
            var jsonWriterOptions = new JsonWriterOptions
            {
                Indented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream, jsonWriterOptions))
            {
                doc.WriteTo(writer);
            }

            var text = Encoding.UTF8.GetString(stream.ToArray());
            if (options.IndentSize != 2 && options.InsertSpaces)
            {
                var customIndent = new string(' ', options.IndentSize);
                text = Regex.Replace(text, @"^(\s{2})+", m =>
                {
                    int levels = m.Value.Length / 2;
                    return string.Concat(Enumerable.Repeat(customIndent, levels));
                }, RegexOptions.Multiline);
            }
            return text;
        }
        catch
        {
            return json;
        }
    }

    #endregion

    #region CSS Formatter

    public static string FormatCss(string css, FormattingOptions options)
    {
        var sb = new StringBuilder();
        int indent = 0;
        bool inComment = false;
        bool inString = false;
        char stringChar = '\0';

        var clean = Regex.Replace(css, @"\s+", " ").Trim();
        for (int i = 0; i < clean.Length; i++)
        {
            char c = clean[i];
            char next = (i + 1 < clean.Length) ? clean[i + 1] : '\0';

            if (!inString && c == '/' && next == '*')
            {
                inComment = true;
                sb.Append("/*");
                i++;
                continue;
            }
            if (inComment && c == '*' && next == '/')
            {
                inComment = false;
                sb.Append("*/\n").Append(options.GetIndentString(indent));
                i++;
                continue;
            }
            if (inComment)
            {
                sb.Append(c);
                continue;
            }

            if ((c == '"' || c == '\'') && (i == 0 || clean[i - 1] != '\\'))
            {
                if (!inString) { inString = true; stringChar = c; }
                else if (stringChar == c) { inString = false; }
                sb.Append(c);
                continue;
            }
            if (inString)
            {
                sb.Append(c);
                continue;
            }

            if (c == '{')
            {
                sb.Append(" {\n");
                indent++;
                sb.Append(options.GetIndentString(indent));
            }
            else if (c == '}')
            {
                sb.Append("\n");
                indent = Math.Max(0, indent - 1);
                sb.Append(options.GetIndentString(indent)).Append("}\n\n");
                if (indent > 0) sb.Append(options.GetIndentString(indent));
            }
            else if (c == ';')
            {
                sb.Append(";\n").Append(options.GetIndentString(indent));
            }
            else if (c == ':' && indent > 0)
            {
                sb.Append(": ");
            }
            else
            {
                if (c == ' ' && sb.Length > 0 && (sb[^1] == ' ' || sb[^1] == '\n'))
                    continue;
                sb.Append(c);
            }
        }

        return sb.ToString().Trim();
    }

    #endregion

    #region HTML / XML Formatter

    private static readonly HashSet<string> VoidElements = new(StringComparer.OrdinalIgnoreCase)
    {
        "area", "base", "br", "col", "embed", "hr", "img", "input", "link", "meta", "param", "source", "track", "wbr"
    };

    public static string FormatHtml(string html, FormattingOptions options)
    {
        var tagRegex = new Regex(@"(<!--.*?-->|<!\[CDATA\[.*?\]\]>|<[^>]+>|[^<]+)", RegexOptions.Singleline);
        var matches = tagRegex.Matches(html);
        var sb = new StringBuilder();
        int indent = 0;
        bool inPre = false;

        foreach (Match match in matches)
        {
            var token = match.Value;
            if (string.IsNullOrWhiteSpace(token))
                continue;

            if (token.StartsWith("<!--") || token.StartsWith("<![CDATA["))
            {
                sb.Append(options.GetIndentString(indent)).Append(token.Trim()).Append("\n");
                continue;
            }

            if (token.StartsWith("<") && token.EndsWith(">"))
            {
                var isClose = token.StartsWith("</");
                var isSelfClose = token.EndsWith("/>");
                var tagMatch = Regex.Match(token, @"^</?([a-zA-Z0-9_\-\:]+)");
                var tagName = tagMatch.Success ? tagMatch.Groups[1].Value : string.Empty;
                var isVoid = VoidElements.Contains(tagName);

                if (string.Equals(tagName, "pre", StringComparison.OrdinalIgnoreCase))
                {
                    inPre = !isClose;
                }

                if (isClose)
                {
                    indent = Math.Max(0, indent - 1);
                }

                sb.Append(options.GetIndentString(indent)).Append(token.Trim()).Append("\n");

                if (!isClose && !isSelfClose && !isVoid && !token.StartsWith("<!"))
                {
                    indent++;
                }
            }
            else
            {
                var text = inPre ? token : token.Trim();
                if (!string.IsNullOrEmpty(text))
                {
                    sb.Append(options.GetIndentString(indent)).Append(text).Append("\n");
                }
            }
        }

        return sb.ToString().Trim();
    }

    #endregion

    #region SQL Formatter

    private static readonly string[] SqlClauses =
    [
        "SELECT", "FROM", "WHERE", "GROUP BY", "HAVING", "ORDER BY",
        "LEFT JOIN", "RIGHT JOIN", "INNER JOIN", "CROSS JOIN", "FULL JOIN", "JOIN",
        "INSERT INTO", "VALUES", "UPDATE", "SET", "DELETE FROM",
        "CREATE TABLE", "ALTER TABLE", "DROP TABLE", "UNION ALL", "UNION",
        "LIMIT", "OFFSET", "BEGIN TRANSACTION", "COMMIT", "ROLLBACK"
    ];

    public static string FormatSql(string sql, FormattingOptions options)
    {
        var text = Regex.Replace(sql, @"\s+", " ").Trim();
        foreach (var clause in SqlClauses.OrderByDescending(c => c.Length))
        {
            text = Regex.Replace(text, $@"\b{Regex.Escape(clause)}\b", clause, RegexOptions.IgnoreCase);
        }

        var lines = new List<string>();
        var pattern = $@"\b({string.Join("|", SqlClauses.Select(Regex.Escape))})\b";
        var parts = Regex.Split(text, pattern, RegexOptions.IgnoreCase);

        var currentClause = string.Empty;
        foreach (var part in parts)
        {
            var p = part.Trim();
            if (string.IsNullOrEmpty(p)) continue;

            if (SqlClauses.Any(c => string.Equals(c, p, StringComparison.OrdinalIgnoreCase)))
            {
                currentClause = p.ToUpperInvariant();
                lines.Add(currentClause);
            }
            else
            {
                if (lines.Count > 0 && !lines[^1].Contains(' ') && SqlClauses.Contains(lines[^1]))
                {
                    lines[^1] = $"{lines[^1]} {p}";
                }
                else
                {
                    lines.Add(options.GetIndentString(1) + p);
                }
            }
        }

        return string.Join(Environment.NewLine, lines).Trim();
    }

    #endregion

    #region JS / TS Formatter

    public static string FormatJsTs(string code, FormattingOptions options)
    {
        var lines = code.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
        var sb = new StringBuilder();
        int indent = 0;

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (string.IsNullOrEmpty(line))
            {
                sb.AppendLine();
                continue;
            }

            int closingCount = line.Count(c => c is '}' or ']');
            int openingCount = line.Count(c => c is '{' or '[');

            if (line.StartsWith("}") || line.StartsWith("]"))
            {
                indent = Math.Max(0, indent - 1);
            }

            sb.Append(options.GetIndentString(indent)).AppendLine(line);

            if (line.StartsWith("}") || line.StartsWith("]"))
            {
                // Already decremented
                indent += Math.Max(0, openingCount - (closingCount - 1));
            }
            else
            {
                indent += Math.Max(0, openingCount - closingCount);
            }
            indent = Math.Max(0, indent);
        }

        return sb.ToString().Trim();
    }

    #endregion

    #region Markdown Formatter

    public static string FormatMarkdown(string markdown, FormattingOptions options)
    {
        var lines = markdown.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
        var sb = new StringBuilder();
        bool inCodeFence = false;

        foreach (var rawLine in lines)
        {
            if (rawLine.TrimStart().StartsWith("```") || rawLine.TrimStart().StartsWith("~~~"))
            {
                inCodeFence = !inCodeFence;
                sb.AppendLine(rawLine);
                continue;
            }

            if (inCodeFence)
            {
                sb.AppendLine(rawLine);
                continue;
            }

            var line = rawLine.TrimEnd();
            sb.AppendLine(line);
        }

        return sb.ToString().Trim();
    }

    #endregion
}
