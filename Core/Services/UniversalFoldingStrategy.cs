using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;

namespace RecluseEdit.Core.Services;

/// <summary>
/// Universal multi-language folding strategy for AvalonEdit.
/// Provides code folding for:
/// 1. Brace-delimited blocks ({}, []) for C#, JS/TS, Go, PHP, Rust, Dart, CSS, JSON, C/C++.
/// 2. Indentation-based blocks for Python and YAML.
/// 3. #region ... #endregion and // #region directives across all languages.
/// 4. Multi-line block comments (/* ... */, <!-- ... -->).
/// 5. XML/HTML tag trees (via XmlFoldingStrategy delegation).
/// </summary>
public class UniversalFoldingStrategy
{
    private readonly XmlFoldingStrategy _xmlFoldingStrategy = new();

    public void UpdateFoldings(FoldingManager manager, TextDocument document, string? languageId)
    {
        var foldings = CreateNewFoldings(document, languageId, out int firstErrorOffset);
        manager.UpdateFoldings(foldings, firstErrorOffset);
    }

    public IEnumerable<NewFolding> CreateNewFoldings(TextDocument document, string? languageId, out int firstErrorOffset)
    {
        firstErrorOffset = -1;
        if (document == null || document.TextLength == 0)
        {
            return [];
        }

        var lang = (languageId ?? "").ToLowerInvariant().Trim().TrimStart('.');
        var foldings = new List<NewFolding>();

        // 1. XML / HTML tag folding delegation
        if (lang is "html" or "xml" or "xaml" or "svg")
        {
            try
            {
                var xmlFoldings = _xmlFoldingStrategy.CreateNewFoldings(document, out _);
                foldings.AddRange(xmlFoldings);
            }
            catch
            {
                // Fallback to brace/comment folding on malformed XML/HTML
            }
        }

        // 2. Region directive folding (#region ... #endregion)
        CreateRegionFoldings(document, foldings);

        // 3. Multi-line comment folding
        CreateCommentFoldings(document, foldings);

        // 4. Language-specific block folding: Indentation (Python/YAML) vs. Braces
        if (lang is "python" or "py" or "yaml" or "yml")
        {
            CreateIndentationFoldings(document, foldings);
        }
        else
        {
            CreateBraceFoldings(document, foldings);
        }

        // Sort by StartOffset (AvalonEdit requirement for UpdateFoldings)
        foldings.Sort((a, b) => a.StartOffset.CompareTo(b.StartOffset));
        return foldings;
    }

    private static void CreateRegionFoldings(TextDocument document, List<NewFolding> foldings)
    {
        var regionStack = new Stack<(int StartOffset, int LineNumber, string Name)>();
        var regionOpenRegex = new Regex(@"^\s*(?:#|//\s*#|/\*\s*#)\s*region(?:\s+(.*))?$", RegexOptions.IgnoreCase);
        var regionCloseRegex = new Regex(@"^\s*(?:#|//\s*#|/\*\s*#)\s*endregion\b", RegexOptions.IgnoreCase);

        for (int i = 1; i <= document.LineCount; i++)
        {
            var line = document.GetLineByNumber(i);
            var text = document.GetText(line.Offset, line.Length);

            var openMatch = regionOpenRegex.Match(text);
            if (openMatch.Success)
            {
                var name = openMatch.Groups[1].Value.Trim();
                if (string.IsNullOrEmpty(name)) name = "#region";
                regionStack.Push((line.Offset, i, name));
                continue;
            }

            if (regionCloseRegex.IsMatch(text) && regionStack.Count > 0)
            {
                var (startOffset, startLine, name) = regionStack.Pop();
                if (i > startLine)
                {
                    foldings.Add(new NewFolding(startOffset, line.EndOffset)
                    {
                        Name = name,
                        DefaultClosed = false
                    });
                }
            }
        }
    }

    private static void CreateCommentFoldings(TextDocument document, List<NewFolding> foldings)
    {
        var text = document.Text;
        int len = text.Length;

        // C-style block comments /* ... */
        int idx = 0;
        while (idx < len)
        {
            int openIdx = text.IndexOf("/*", idx, StringComparison.Ordinal);
            if (openIdx < 0) break;

            int closeIdx = text.IndexOf("*/", openIdx + 2, StringComparison.Ordinal);
            if (closeIdx < 0) break;

            int startLine = document.GetLineByOffset(openIdx).LineNumber;
            int endLine = document.GetLineByOffset(closeIdx + 2).LineNumber;

            if (endLine > startLine)
            {
                foldings.Add(new NewFolding(openIdx, closeIdx + 2)
                {
                    Name = "/* ... */",
                    DefaultClosed = false
                });
            }

            idx = closeIdx + 2;
        }

        // HTML/XML comments <!-- ... -->
        idx = 0;
        while (idx < len)
        {
            int openIdx = text.IndexOf("<!--", idx, StringComparison.Ordinal);
            if (openIdx < 0) break;

            int closeIdx = text.IndexOf("-->", openIdx + 4, StringComparison.Ordinal);
            if (closeIdx < 0) break;

            int startLine = document.GetLineByOffset(openIdx).LineNumber;
            int endLine = document.GetLineByOffset(closeIdx + 3).LineNumber;

            if (endLine > startLine)
            {
                foldings.Add(new NewFolding(openIdx, closeIdx + 3)
                {
                    Name = "<!-- ... -->",
                    DefaultClosed = false
                });
            }

            idx = closeIdx + 3;
        }
    }

    private static void CreateBraceFoldings(TextDocument document, List<NewFolding> foldings)
    {
        var braceStack = new Stack<(char Kind, int OpenOffset, int LineNumber)>();
        var bracketStack = new Stack<(char Kind, int OpenOffset, int LineNumber)>();

        var text = document.Text;
        int len = text.Length;
        int i = 0;

        while (i < len)
        {
            char c = text[i];

            // 1. Skip single-line comments
            if (c == '/' && i + 1 < len && text[i + 1] == '/')
            {
                var line = document.GetLineByOffset(i);
                i = line.EndOffset;
                continue;
            }
            if (c == '#' || (c == '-' && i + 1 < len && text[i + 1] == '-'))
            {
                var line = document.GetLineByOffset(i);
                i = line.EndOffset;
                continue;
            }

            // 2. Skip block comments
            if (c == '/' && i + 1 < len && text[i + 1] == '*')
            {
                int close = text.IndexOf("*/", i + 2, StringComparison.Ordinal);
                i = close >= 0 ? close + 2 : len;
                continue;
            }

            // 3. Skip string literals
            if (c is '"' or '\'' or '`')
            {
                char quote = c;
                i++;
                while (i < len)
                {
                    if (text[i] == '\\')
                    {
                        i += 2;
                        continue;
                    }
                    if (text[i] == quote)
                    {
                        i++;
                        break;
                    }
                    i++;
                }
                continue;
            }

            // 4. Track curly braces { }
            if (c == '{')
            {
                int lineNum = document.GetLineByOffset(i).LineNumber;
                braceStack.Push(('{', i, lineNum));
            }
            else if (c == '}' && braceStack.Count > 0)
            {
                var (_, openOffset, startLine) = braceStack.Pop();
                int endLine = document.GetLineByOffset(i).LineNumber;
                if (endLine > startLine)
                {
                    foldings.Add(new NewFolding(openOffset, i + 1)
                    {
                        Name = "{...}",
                        DefaultClosed = false
                    });
                }
            }

            // 5. Track square brackets [ ]
            else if (c == '[')
            {
                int lineNum = document.GetLineByOffset(i).LineNumber;
                bracketStack.Push(('[', i, lineNum));
            }
            else if (c == ']' && bracketStack.Count > 0)
            {
                var (_, openOffset, startLine) = bracketStack.Pop();
                int endLine = document.GetLineByOffset(i).LineNumber;
                if (endLine > startLine)
                {
                    foldings.Add(new NewFolding(openOffset, i + 1)
                    {
                        Name = "[...]",
                        DefaultClosed = false
                    });
                }
            }

            i++;
        }
    }

    private static void CreateIndentationFoldings(TextDocument document, List<NewFolding> foldings)
    {
        var stack = new Stack<(int Indent, int LineNumber, int StartOffset)>();
        int lastNonEmptyLine = 0;

        for (int i = 1; i <= document.LineCount; i++)
        {
            var line = document.GetLineByNumber(i);
            var text = document.GetText(line.Offset, line.Length);
            var trimmed = text.Trim();

            // Skip empty or comment-only lines
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#'))
            {
                continue;
            }

            int indent = GetIndentLevel(text);

            while (stack.Count > 0 && stack.Peek().Indent >= indent)
            {
                var block = stack.Pop();
                if (lastNonEmptyLine > block.LineNumber)
                {
                    var endLine = document.GetLineByNumber(lastNonEmptyLine);
                    foldings.Add(new NewFolding(block.StartOffset, endLine.EndOffset)
                    {
                        Name = "...",
                        DefaultClosed = false
                    });
                }
            }

            // If line ends with ':' (Python function, class, if, loop, try, etc.), start fold block
            if (trimmed.EndsWith(':'))
            {
                stack.Push((indent, i, line.Offset));
            }

            lastNonEmptyLine = i;
        }

        // Close any trailing blocks at end of document
        while (stack.Count > 0)
        {
            var block = stack.Pop();
            if (lastNonEmptyLine > block.LineNumber)
            {
                var endLine = document.GetLineByNumber(lastNonEmptyLine);
                foldings.Add(new NewFolding(block.StartOffset, endLine.EndOffset)
                {
                    Name = "...",
                    DefaultClosed = false
                });
            }
        }
    }

    private static int GetIndentLevel(string line)
    {
        int count = 0;
        foreach (char ch in line)
        {
            if (ch == ' ') count++;
            else if (ch == '\t') count += 4;
            else break;
        }
        return count;
    }
}

