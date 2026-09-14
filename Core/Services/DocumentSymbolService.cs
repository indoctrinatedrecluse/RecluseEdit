using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ICSharpCode.AvalonEdit.Document;

namespace RecluseEdit.Core.Services;

public enum SymbolKind
{
    Class,
    Interface,
    Struct,
    Enum,
    Method,
    Function,
    Property,
    Constructor
}

public record DocumentSymbol(
    string Name,
    SymbolKind Kind,
    int LineNumber,
    int ColumnNumber,
    int Offset,
    string? ContainerName = null)
{
    public string DisplayIcon => Kind switch
    {
        SymbolKind.Class => "📦",
        SymbolKind.Interface => "📋",
        SymbolKind.Struct => "🧱",
        SymbolKind.Enum => "🔢",
        SymbolKind.Method => "⚡",
        SymbolKind.Function => "ƒ",
        SymbolKind.Property => "🏷️",
        SymbolKind.Constructor => "⚙️",
        _ => "🔘"
    };

    public string FullName => string.IsNullOrEmpty(ContainerName) ? Name : $"{ContainerName} > {Name}";
}

/// <summary>
/// Extracts code symbols (classes, methods, functions, interfaces, etc.)
/// across all supported languages for quick navigation (@ symbol jump and sticky scope).
/// </summary>
public static class DocumentSymbolService
{
    public static IReadOnlyList<DocumentSymbol> ExtractSymbols(string text, string? languageId)
    {
        return ExtractSymbols(new TextDocument(text ?? string.Empty), languageId);
    }

    public static IReadOnlyList<DocumentSymbol> ExtractSymbols(TextDocument document, string? languageId)
    {
        if (document == null || document.TextLength == 0) return [];

        var lang = (languageId ?? "").ToLowerInvariant().Trim();
        if (lang.Contains('.'))
        {
            lang = System.IO.Path.GetExtension(lang).TrimStart('.');
        }
        else
        {
            lang = lang.TrimStart('.');
        }

        var symbols = new List<DocumentSymbol>();
        string? currentContainer = null;
        int braceDepth = 0;

        for (int i = 1; i <= document.LineCount; i++)
        {
            var line = document.GetLineByNumber(i);
            var lineText = document.GetText(line.Offset, line.Length);
            var trimmed = lineText.Trim();

            if (trimmed.Length == 0 || trimmed.StartsWith("//") || trimmed.StartsWith('#') || trimmed.StartsWith("/*") || trimmed.StartsWith('*'))
                continue;

            int openBraces = trimmed.Count(c => c == '{');
            int closeBraces = trimmed.Count(c => c == '}');

            if (trimmed == "}")
            {
                braceDepth = Math.Max(0, braceDepth - 1);
                if (braceDepth <= 0)
                {
                    currentContainer = null;
                    braceDepth = 0;
                }
                continue;
            }

            void OnLineProcessed()
            {
                braceDepth += (openBraces - closeBraces);
                if (braceDepth <= 0)
                {
                    currentContainer = null;
                    braceDepth = 0;
                }
            }

            // 1. Python
            if (lang is "py" or "python")
            {
                if (!lineText.StartsWith(' ') && !lineText.StartsWith('\t') && !trimmed.StartsWith("class"))
                {
                    currentContainer = null;
                }
                var matchClass = Regex.Match(trimmed, @"^class\s+([a-zA-Z0-9_]+)");
                if (matchClass.Success)
                {
                    currentContainer = matchClass.Groups[1].Value;
                    symbols.Add(new DocumentSymbol(currentContainer, SymbolKind.Class, i, lineText.IndexOf(currentContainer) + 1, line.Offset));
                    OnLineProcessed();
                    continue;
                }

                var matchDef = Regex.Match(trimmed, @"^def\s+([a-zA-Z0-9_]+)\s*\(");
                if (matchDef.Success)
                {
                    var name = matchDef.Groups[1].Value;
                    var kind = name == "__init__" ? SymbolKind.Constructor : (lineText.StartsWith(" ") || lineText.StartsWith("\t") ? SymbolKind.Method : SymbolKind.Function);
                    symbols.Add(new DocumentSymbol(name, kind, i, lineText.IndexOf(name) + 1, line.Offset, currentContainer));
                    OnLineProcessed();
                    continue;
                }
            }
            // 2. Go
            else if (lang is "go")
            {
                var matchType = Regex.Match(trimmed, @"^type\s+([a-zA-Z0-9_]+)\s+(struct|interface)");
                if (matchType.Success)
                {
                    var name = matchType.Groups[1].Value;
                    var kind = matchType.Groups[2].Value == "interface" ? SymbolKind.Interface : SymbolKind.Struct;
                    currentContainer = name;
                    symbols.Add(new DocumentSymbol(name, kind, i, lineText.IndexOf(name) + 1, line.Offset));
                    OnLineProcessed();
                    continue;
                }

                var matchMethod = Regex.Match(trimmed, @"^func\s*\(\s*(?:[a-zA-Z0-9_]+\s+)?\*?([a-zA-Z0-9_]+)\s*\)\s*([a-zA-Z0-9_]+)\s*\(");
                if (matchMethod.Success)
                {
                    var receiver = matchMethod.Groups[1].Value;
                    var name = matchMethod.Groups[2].Value;
                    symbols.Add(new DocumentSymbol(name, SymbolKind.Method, i, lineText.IndexOf(name) + 1, line.Offset, receiver));
                    OnLineProcessed();
                    continue;
                }

                var matchFunc = Regex.Match(trimmed, @"^func\s+([a-zA-Z0-9_]+)\s*\(");
                if (matchFunc.Success)
                {
                    var name = matchFunc.Groups[1].Value;
                    symbols.Add(new DocumentSymbol(name, SymbolKind.Function, i, lineText.IndexOf(name) + 1, line.Offset));
                    OnLineProcessed();
                    continue;
                }
            }
            // 3. Rust
            else if (lang is "rust" or "rs")
            {
                var matchImpl = Regex.Match(trimmed, @"^(?:pub\s+)?impl(?:\s*<[^>]+>)?\s+([a-zA-Z0-9_]+)");
                if (matchImpl.Success)
                {
                    currentContainer = matchImpl.Groups[1].Value;
                    OnLineProcessed();
                    continue;
                }

                var matchType = Regex.Match(trimmed, @"^(?:pub\s+)?(?:struct|enum|trait)\s+([a-zA-Z0-9_]+)");
                if (matchType.Success)
                {
                    var name = matchType.Groups[1].Value;
                    currentContainer = name;
                    symbols.Add(new DocumentSymbol(name, SymbolKind.Struct, i, lineText.IndexOf(name) + 1, line.Offset));
                    OnLineProcessed();
                    continue;
                }

                var matchFn = Regex.Match(trimmed, @"^(?:pub\s+)?(?:async\s+)?fn\s+([a-zA-Z0-9_]+)\s*\(");
                if (matchFn.Success)
                {
                    var name = matchFn.Groups[1].Value;
                    var kind = currentContainer != null ? SymbolKind.Method : SymbolKind.Function;
                    symbols.Add(new DocumentSymbol(name, kind, i, lineText.IndexOf(name) + 1, line.Offset, currentContainer));
                    OnLineProcessed();
                    continue;
                }
            }
            // 4. JavaScript & TypeScript
            else if (lang is "js" or "javascript" or "ts" or "typescript" or "jsx" or "tsx")
            {
                var matchClass = Regex.Match(trimmed, @"^(?:export\s+)?(?:default\s+)?class\s+([a-zA-Z0-9_]+)");
                if (matchClass.Success)
                {
                    var name = matchClass.Groups[1].Value;
                    currentContainer = name;
                    symbols.Add(new DocumentSymbol(name, SymbolKind.Class, i, lineText.IndexOf(name) + 1, line.Offset));
                    OnLineProcessed();
                    continue;
                }

                var matchJsFunc = Regex.Match(trimmed, @"^(?:export\s+)?(?:default\s+)?(?:async\s+)?function\s+([a-zA-Z0-9_]+)\s*\(");
                if (matchJsFunc.Success)
                {
                    var name = matchJsFunc.Groups[1].Value;
                    symbols.Add(new DocumentSymbol(name, SymbolKind.Function, i, lineText.IndexOf(name) + 1, line.Offset));
                    OnLineProcessed();
                    continue;
                }

                var matchArrow = Regex.Match(trimmed, @"^(?:export\s+)?(?:const|let|var)\s+([a-zA-Z0-9_]+)\s*=\s*(?:async\s*)?(?:\([^)]*\)|[a-zA-Z0-9_]+)\s*=>");
                if (matchArrow.Success)
                {
                    var name = matchArrow.Groups[1].Value;
                    symbols.Add(new DocumentSymbol(name, SymbolKind.Function, i, lineText.IndexOf(name) + 1, line.Offset));
                    OnLineProcessed();
                    continue;
                }

                var matchClassMethod = Regex.Match(trimmed, @"^(?:(?:async|get|set|static)\s+)?([a-zA-Z0-9_]+)\s*\([^)]*\)\s*\{?");
                if (matchClassMethod.Success && currentContainer != null)
                {
                    var name = matchClassMethod.Groups[1].Value;
                    if (name is not "if" and not "while" and not "for" and not "switch" and not "catch")
                    {
                        var kind = name == "constructor" ? SymbolKind.Constructor : SymbolKind.Method;
                        symbols.Add(new DocumentSymbol(name, kind, i, lineText.IndexOf(name) + 1, line.Offset, currentContainer));
                        OnLineProcessed();
                        continue;
                    }
                }
            }
            // 5. C# / C-Family / PHP / general
            else
            {
                var matchClass = Regex.Match(trimmed, @"\b(?:class|interface|struct|enum|record|trait)\s+([a-zA-Z0-9_]+)");
                if (matchClass.Success)
                {
                    var name = matchClass.Groups[1].Value;
                    currentContainer = name;
                    var kind = trimmed.Contains("interface") ? SymbolKind.Interface :
                               trimmed.Contains("enum") ? SymbolKind.Enum :
                               trimmed.Contains("struct") ? SymbolKind.Struct : SymbolKind.Class;
                    symbols.Add(new DocumentSymbol(name, kind, i, lineText.IndexOf(name) + 1, line.Offset));
                    OnLineProcessed();
                    continue;
                }

                // General method / function match: ReturnType MethodName(...)
                var matchMethod = Regex.Match(trimmed, @"^(?:(?:public|private|protected|internal|static|async|override|virtual|final|abstract)\s+)*[a-zA-Z0-9_<>[\]?, ]+\s+([a-zA-Z0-9_]+)\s*\(");
                if (matchMethod.Success)
                {
                    var name = matchMethod.Groups[1].Value;
                    if (name is not "if" and not "while" and not "for" and not "switch" and not "catch")
                    {
                        var kind = (currentContainer != null && name == currentContainer) ? SymbolKind.Constructor : SymbolKind.Method;
                        symbols.Add(new DocumentSymbol(name, kind, i, lineText.IndexOf(name) + 1, line.Offset, currentContainer));
                        OnLineProcessed();
                        continue;
                    }
                }

                // PHP function
                var matchPhpFunc = Regex.Match(trimmed, @"^(?:(?:public|private|protected|static)\s+)*function\s+([a-zA-Z0-9_]+)\s*\(");
                if (matchPhpFunc.Success)
                {
                    var name = matchPhpFunc.Groups[1].Value;
                    var kind = name == "__construct" ? SymbolKind.Constructor : SymbolKind.Method;
                    symbols.Add(new DocumentSymbol(name, kind, i, lineText.IndexOf(name) + 1, line.Offset, currentContainer));
                    OnLineProcessed();
                    continue;
                }
            }

            OnLineProcessed();
        }

        return symbols;
    }

    /// <summary>
    /// Returns the enclosing symbol at or immediately preceding the given line.
    /// </summary>
    public static DocumentSymbol? GetEnclosingSymbol(IReadOnlyList<DocumentSymbol> symbols, int caretLine)
    {
        if (symbols == null || symbols.Count == 0) return null;
        var preceding = symbols.Where(s => s.LineNumber <= caretLine).OrderBy(s => s.LineNumber).ToList();
        return preceding.LastOrDefault();
    }

    /// <summary>
    /// Returns the enclosing symbol name at or immediately preceding the given line.
    /// </summary>
    public static string? GetEnclosingScope(IReadOnlyList<DocumentSymbol> symbols, int caretLine)
    {
        return GetEnclosingSymbol(symbols, caretLine)?.FullName;
    }
}

