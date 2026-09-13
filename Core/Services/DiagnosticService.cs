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
        diagnostics.AddRange(AnalyzeBrackets(text, filePath, lang));

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

    public static List<DiagnosticItem> AnalyzeBrackets(string text, string filePath, string language = "")
    {
        if (string.IsNullOrEmpty(text))
            return new List<DiagnosticItem>();

        var lang = language?.ToLowerInvariant() ?? string.Empty;
        var ext = Path.GetExtension(filePath)?.ToLowerInvariant() ?? string.Empty;

        // Plain text / logs / CSV / TSV have no bracket syntax rules
        if (lang is "text" or "plaintext" or "txt" or "log" || ext is ".txt" or ".log" or ".csv" or ".tsv")
        {
            return new List<DiagnosticItem>();
        }

        // Markdown files
        if (lang is "markdown" or "md" || ext is ".md" or ".markdown" or ".mdown" or ".mkd")
        {
            return AnalyzeMarkdownBrackets(text, filePath);
        }

        return AnalyzeCodeBrackets(text, filePath, lang, ext);
    }

    public static List<DiagnosticItem> AnalyzeMarkdownBrackets(string text, string filePath)
    {
        var items = new List<DiagnosticItem>();
        var stack = new Stack<(char bracket, int line, int col)>();

        int line = 1;
        int col = 1;
        bool inFencedCode = false;
        char fenceChar = '\0';
        int fenceCount = 0;
        int fenceStartLine = 1;
        int fenceStartCol = 1;

        int i = 0;
        int len = text.Length;

        while (i < len)
        {
            char c = text[i];

            // 1. Handle Newlines
            if (c == '\n')
            {
                line++;
                col = 1;
                i++;
                continue;
            }
            if (c == '\r')
            {
                i++;
                continue;
            }

            // 2. Check for fenced code blocks at beginning of line
            if (col == 1)
            {
                int spaceCount = 0;
                while (i + spaceCount < len && text[i + spaceCount] == ' ' && spaceCount < 4)
                // Skip any leading whitespace (spaces and tabs)
                int ws = 0;
                while (i + ws < len && (text[i + ws] == ' ' || text[i + ws] == '\t'))
                {
                    spaceCount++;
                    ws++;
                }

                int fenceIdx = i + spaceCount;
                // Optional blockquote prefix '>'
                if (i + ws < len && text[i + ws] == '>')
                {
                    ws++;
                    while (i + ws < len && (text[i + ws] == ' ' || text[i + ws] == '\t'))
                    {
                        ws++;
                    }
                }

                int fenceIdx = i + ws;
                if (fenceIdx < len && (text[fenceIdx] == '`' || text[fenceIdx] == '~'))
                {
                    char fChar = text[fenceIdx];
                    int count = 0;
                    while (fenceIdx + count < len && text[fenceIdx + count] == fChar)
                    {
                        count++;
                    }

                    if (count >= 3)
                    {
                        if (!inFencedCode)
                        // Check what follows the backticks/tildes on the same line
                        int afterFence = fenceIdx + count;
                        int lineEnd = afterFence;
                        while (lineEnd < len && text[lineEnd] != '\n')
                        {
                            inFencedCode = true;
                            fenceChar = fChar;
                            fenceCount = count;
                            fenceStartLine = line;
                            fenceStartCol = col + spaceCount;
                            lineEnd++;
                        }
                        else if (fChar == fenceChar && count >= fenceCount)

                        string restOfLine = text.Substring(afterFence, lineEnd - afterFence).TrimEnd('\r', ' ', '\t');

                        if (inFencedCode)
                        {
                            inFencedCode = false;
                            fenceChar = '\0';
                            fenceCount = 0;
                            // A closing fence: matches if it has 3+ fence characters (matching type or any standard fence)
                            bool isClosing = (fChar == fenceChar || fChar == '`' || fChar == '~') && count >= 3;
                            // and nothing else except whitespace (or a closing comment) on the line
                            bool isClosing = (fChar == fenceChar || fChar == '`' || fChar == '~') && count >= 3 &&
                                (restOfLine.Trim().Length == 0 || restOfLine.Trim().StartsWith("//") || restOfLine.Trim().StartsWith("#"));
                            if (isClosing)
                            {
                                inFencedCode = false;
                                fenceChar = '\0';
                                fenceCount = 0;

                                // Skip to end of line
                                while (i < len && text[i] != '\n')
                                {
                                    col++;
                                    i++;
                                }
                                continue;
                            }
                        }
                        else
                        {
                            // Check if this is a single-line code block: e.g. ``` npm install ```
                            if (restOfLine.TrimStart().Length > 0 &&
                                (restOfLine.EndsWith(new string(fChar, count)) || (count >= 3 && (restOfLine.EndsWith("```") || restOfLine.EndsWith("~~~")))))
                            // Check if this is a single-line code block or inline code at the start of a line:
                            // e.g. ``` npm install ``` or ```code``` is cool
                            if (restOfLine.Contains(new string(fChar, count)) || (count >= 3 && (restOfLine.Contains("```") || restOfLine.Contains("~~~"))))
                            {
                                // Single-line code block! Opens and closes on the same line.
                                // Single-line code block! Opens and closes on the same line, does not toggle inFencedCode.
                                while (i < len && text[i] != '\n')
                                {
                                    col++;
                                    i++;
                                }
                                continue;
                            }

                        // Skip to end of line
                        while (i < len && text[i] != '\n')
                        {
                            col++;
                            i++;
                            // Check if the rest of the line is a prose sentence rather than an info string.
                            // Valid info string: single word like "csharp", "json", "bash", "ts", "python" (no spaces).
                            // Valid info string: single word like "csharp", "json", "bash", "ts", "python", or language with attributes.
                            // But if it starts with space followed by words, or contains multiple spaces like "``` is used to...", it's prose.
                            string info = restOfLine.Trim();
                            bool isProseSentence = info.Contains(' ') && !info.StartsWith("{");
                            bool isProseSentence = (restOfLine.StartsWith(" ") || restOfLine.StartsWith("\t")) && info.Contains(' ') && !info.StartsWith("{");
                            if (!isProseSentence)
                            {
                                inFencedCode = true;
                                fenceChar = fChar;
                                fenceCount = count;
                                fenceStartLine = line;
                                fenceStartCol = col + ws;

                                // Skip to end of line
                                while (i < len && text[i] != '\n')
                                {
                                    col++;
                                    i++;
                                }
                                continue;
                            }
                        }
                        continue;
                    }
                }
            }

            // While inside fenced code block, skip all characters until closing fence
            if (inFencedCode)
            {
                col++;
                i++;
                continue;
            }

            // 3. HTML Comments <!-- ... -->
            if (c == '<' && i + 3 < len && text[i + 1] == '!' && text[i + 2] == '-' && text[i + 3] == '-')
            {
                i += 4;
                col += 4;
                while (i < len)
                {
                    if (text[i] == '-' && i + 2 < len && text[i + 1] == '-' && text[i + 2] == '>')
                    {
                        i += 3;
                        col += 3;
                        break;
                    }
                    if (text[i] == '\n')
                    {
                        line++;
                        col = 1;
                        i++;
                    }
                    else
                    {
                        col++;
                        i++;
                    }
                }
                continue;
            }

            // 4. Inline Code Spans `...` or ``...``
            if (c == '`')
            {
                int backtickCount = 0;
                while (i + backtickCount < len && text[i + backtickCount] == '`')
                {
                    backtickCount++;
                }

                int searchStart = i + backtickCount;
                int closeIdx = -1;
                for (int k = searchStart; k < len; k++)
                {
                    if (text[k] == '\n')
                    {
                        // Check if the following line is blank (paragraph break in LF, CRLF, or whitespace-only lines)
                        int nextNonSpace = k + 1;
                        while (nextNonSpace < len && (text[nextNonSpace] == ' ' || text[nextNonSpace] == '\t' || text[nextNonSpace] == '\r'))
                        {
                            nextNonSpace++;
                        }
                        if (nextNonSpace < len && text[nextNonSpace] == '\n')
                        {
                            // Paragraph break found! Inline code cannot span paragraph breaks.
                            break;
                        }
                    }
                    if (text[k] == '`')
                    {
                        int closingTicks = 0;
                        while (k + closingTicks < len && text[k + closingTicks] == '`')
                        {
                            closingTicks++;
                        }
                        if (closingTicks == backtickCount)
                        {
                            closeIdx = k;
                            break;
                        }
                        k += closingTicks - 1;
                    }
                }

                if (closeIdx != -1)
                {
                    // Advance through inline code
                    while (i < closeIdx + backtickCount)
                    {
                        if (text[i] == '\n')
                        {
                            line++;
                            col = 1;
                        }
                        else
                        {
                            col++;
                        }
                        i++;
                    }
                    continue;
                }
            }

            // 5. Checkboxes: `- [ ] ` or `- [x] ` or `* [ ] `
            if (c == '[' && i + 2 < len && (text[i + 1] == ' ' || text[i + 1] == 'x' || text[i + 1] == 'X') && text[i + 2] == ']')
            {
                // Checkbox brackets match immediately
                i += 3;
                col += 3;
                continue;
            }

            // 6. Markdown Open Brackets: { ( [
            if (c is '{' or '(' or '[')
            {
                // Check if '(' is part of an emoticon: e.g. '(:', '(-:', ':(', ':-('
                bool isEmoticon = false;
                if (c == '(')
                {
                    if (i + 1 < len && (text[i + 1] == ':' || text[i + 1] == ';' || text[i + 1] == '='))
                        isEmoticon = true;
                    else if (i + 2 < len && text[i + 1] == '-' && (text[i + 2] == ':' || text[i + 2] == ';'))
                        isEmoticon = true;
                    else if (i > 0 && (text[i - 1] == ':' || text[i - 1] == ';' || text[i - 1] == '='))
                        isEmoticon = true;
                    else if (i > 1 && text[i - 1] == '-' && (text[i - 2] == ':' || text[i - 2] == ';'))
                        isEmoticon = true;
                }

                if (!isEmoticon)
                {
                    stack.Push((c, line, col));
                }
            }
            // 7. Markdown Close Brackets: } ) ]
            else if (c is '}' or ')' or ']')
            {
                // Check if ')' is part of an emoticon: e.g. ':)', ':-)', ';)', '=)', '):', ')-:'
                bool isEmoticon = false;
                if (c == ')')
                {
                    if (i > 0 && (text[i - 1] == ':' || text[i - 1] == ';' || text[i - 1] == '='))
                        isEmoticon = true;
                    else if (i > 1 && text[i - 1] == '-' && (text[i - 2] == ':' || text[i - 2] == ';'))
                        isEmoticon = true;
                    else if (i + 1 < len && (text[i + 1] == ':' || text[i + 1] == ';' || text[i + 1] == '='))
                        isEmoticon = true;
                    else if (i + 2 < len && text[i + 1] == '-' && (text[i + 2] == ':' || text[i + 2] == ';'))
                        isEmoticon = true;
                }

                // Check if ')' is a list marker: e.g. '1) ', '2) ', 'a) ', 'b) ', 'i) '
                bool isListMarker = false;
                if (c == ')' && (stack.Count == 0 || stack.Peek().bracket != '('))
                {
                    int k = i - 1;
                    while (k >= 0 && text[k] != '\n' && char.IsLetterOrDigit(text[k]))
                    {
                        k--;
                    }
                    int markerLen = (i - 1) - k;
                    if (markerLen is >= 1 and <= 4 && (k < 0 || text[k] == '\n' || char.IsWhiteSpace(text[k])))
                    {
                        if (i + 1 == len || char.IsWhiteSpace(text[i + 1]) || text[i + 1] == '\r' || text[i + 1] == '\n')
                        {
                            isListMarker = true;
                        }
                    }
                }

                if (!isEmoticon && !isListMarker)
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
                            Source = "Markdown Bracket Matcher"
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
                                Source = "Markdown Bracket Matcher"
                            });
                        }
                    }
                }
            }

            col++;
            i++;
        }

        if (inFencedCode)
        {
            items.Add(new DiagnosticItem
            {
                FilePath = filePath,
                LineNumber = fenceStartLine,
                ColumnNumber = fenceStartCol,
                Message = $"Unclosed fenced code block opened with '{fenceChar}{fenceChar}{fenceChar}'.",
                Severity = DiagnosticSeverity.Warning,
                Source = "Markdown Linter"
            });
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
                Source = "Markdown Bracket Matcher"
            });
        }

        return items;
    }

    public static List<DiagnosticItem> AnalyzeCodeBrackets(string text, string filePath, string lang, string ext)
    {
        var items = new List<DiagnosticItem>();
        var stack = new Stack<(char bracket, int line, int col)>();

        bool isHashComment = lang is "python" or "py" or "ruby" or "rb" or "shell" or "bash" or "sh" or "zsh" or "powershell" or "ps1" or "yaml" or "yml" or "r" or "dockerfile" or "toml" ||
                             ext is ".py" or ".rb" or ".sh" or ".bash" or ".zsh" or ".ps1" or ".psm1" or ".psd1" or ".yaml" or ".yml" or ".r" or ".dockerfile" or ".toml";

        bool isDashDashComment = lang is "sql" or "lua" or "haskell" || ext is ".sql" or ".lua" or ".hs";

        int line = 1;
        int col = 1;
        bool inSingleQuote = false;
        bool inDoubleQuote = false;
        bool inBacktickQuote = false;
        bool inLineComment = false;
        bool inBlockComment = false;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            char next = i + 1 < text.Length ? text[i + 1] : '\0';

            if (c == '\n')
            {
                line++;
                col = 1;
                inLineComment = false;
                // Single-line quotes do not cross newlines unless escaped
                if (inSingleQuote && (i == 0 || text[i - 1] != '\\'))
                    inSingleQuote = false;
                if (inDoubleQuote && (i == 0 || text[i - 1] != '\\'))
                    inDoubleQuote = false;
                continue;
            }
            if (c == '\r') continue;

            // Handle block comment continuation
            if (inBlockComment)
            {
                if (isDashDashComment && c == ']' && next == ']')
                {
                    inBlockComment = false;
                    i++;
                    col += 2;
                    continue;
                }
                if (c == '*' && next == '/')
                {
                    inBlockComment = false;
                    i++;
                    col += 2;
                    continue;
                }
                col++;
                continue;
            }

            // Handle line comment continuation
            if (inLineComment)
            {
                col++;
                continue;
            }

            bool inString = inSingleQuote || inDoubleQuote || inBacktickQuote;

            // Check for comments if not in a string
            if (!inString)
            {
                // Hash comment: '#'
                if (isHashComment && c == '#')
                {
                    inLineComment = true;
                    col++;
                    continue;
                }

                // Dash-dash comment: '--'
                if (isDashDashComment && c == '-' && next == '-')
                {
                    // Lua block comment: '--[['
                    if (lang == "lua" || ext == ".lua")
                    {
                        if (i + 3 < text.Length && text[i + 2] == '[' && text[i + 3] == '[')
                        {
                            inBlockComment = true;
                            i += 3;
                            col += 4;
                            continue;
                        }
                    }
                    inLineComment = true;
                    i++;
                    col += 2;
                    continue;
                }

                // C-style block comment: '/*'
                if (!isHashComment && !isDashDashComment && c == '/' && next == '*')
                {
                    inBlockComment = true;
                    i++;
                    col += 2;
                    continue;
                }

                // C-style line comment: '//'
                // Ensure this is NOT part of a URL scheme like 'http://', 'https://', 'file://'
                if (!isHashComment && !isDashDashComment && c == '/' && next == '/')
                {
                    bool isUrl = i > 0 && text[i - 1] == ':';
                    if (!isUrl)
                    {
                        inLineComment = true;
                        i++;
                        col += 2;
                        continue;
                    }
                }
            }

            // Check escaped quotes: count consecutive preceding backslashes
            int backslashes = 0;
            int bi = i - 1;
            while (bi >= 0 && text[bi] == '\\')
            {
                backslashes++;
                bi--;
            }
            bool isEscaped = (backslashes % 2) != 0;

            // Handle double quotes
            if (c == '"' && !isEscaped && !inSingleQuote && !inBacktickQuote)
            {
                inDoubleQuote = !inDoubleQuote;
                col++;
                continue;
            }

            // Handle backtick quotes (JS/TS, Go)
            if (c == '`' && !isEscaped && !inSingleQuote && !inDoubleQuote)
            {
                inBacktickQuote = !inBacktickQuote;
                col++;
                continue;
            }

            // Handle single quotes
            if (c == '\'' && !isEscaped && !inDoubleQuote && !inBacktickQuote)
            {
                // In C#, Rust, C++, Java, check if it's an apostrophe or lifetime:
                bool isFlanked = i > 0 && char.IsLetterOrDigit(text[i - 1]) && i + 1 < text.Length && char.IsLetterOrDigit(text[i + 1]);
                if (!isFlanked)
                {
                    inSingleQuote = !inSingleQuote;
                    col++;
                    continue;
                }
            }

            if (inSingleQuote || inDoubleQuote || inBacktickQuote)
            {
                col++;
                continue;
            }

            // Match brackets
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

