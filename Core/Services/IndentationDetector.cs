using System;
using System.Linq;
using ICSharpCode.AvalonEdit;

namespace RecluseEdit.Core.Services;

public record IndentationInfo(bool UseTabs, int IndentSize)
{
    public override string ToString() => UseTabs ? $"Tab Size: {IndentSize}" : $"Spaces: {IndentSize}";
}

/// <summary>
/// Detects indentation style (Tabs vs. Spaces, 2 vs. 4) from document content,
/// and provides utilities to convert between spaces and tabs.
/// </summary>
public static class IndentationDetector
{
    public static IndentationInfo Detect(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return new IndentationInfo(UseTabs: false, IndentSize: 4);
        }

        int tabCount = 0;
        int twoSpaceCount = 0;
        int fourSpaceCount = 0;

        var lines = text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        int sampleSize = Math.Min(lines.Length, 200);

        for (int i = 0; i < sampleSize; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            if (line.StartsWith('\t'))
            {
                tabCount++;
            }
            else if (line.StartsWith("  "))
            {
                int leadingSpaces = 0;
                while (leadingSpaces < line.Length && line[leadingSpaces] == ' ')
                {
                    leadingSpaces++;
                }

                if (leadingSpaces % 4 == 0)
                {
                    fourSpaceCount++;
                }
                else if (leadingSpaces % 2 == 0)
                {
                    twoSpaceCount++;
                }
            }
        }

        if (tabCount > fourSpaceCount && tabCount > twoSpaceCount)
        {
            return new IndentationInfo(UseTabs: true, IndentSize: 4);
        }

        if (twoSpaceCount > fourSpaceCount)
        {
            return new IndentationInfo(UseTabs: false, IndentSize: 2);
        }

        return new IndentationInfo(UseTabs: false, IndentSize: 4);
    }

    public static void Apply(TextEditor editor, IndentationInfo info)
    {
        editor.Options.ConvertTabsToSpaces = !info.UseTabs;
        editor.Options.IndentationSize = info.IndentSize;
    }

    public static void ConvertTabsToSpaces(TextEditor editor, int tabSize = 4)
    {
        if (editor.Document == null) return;
        string replacement = new(' ', Math.Max(1, tabSize));

        using (editor.Document.RunUpdate())
        {
            for (int i = 1; i <= editor.Document.LineCount; i++)
            {
                var line = editor.Document.GetLineByNumber(i);
                var text = editor.Document.GetText(line.Offset, line.Length);
                if (text.Contains('\t'))
                {
                    var converted = text.Replace("\t", replacement);
                    editor.Document.Replace(line.Offset, line.Length, converted);
                }
            }
        }
    }

    public static void ConvertSpacesToTabs(TextEditor editor, int tabSize = 4)
    {
        if (editor.Document == null) return;
        string spaces = new(' ', Math.Max(1, tabSize));

        using (editor.Document.RunUpdate())
        {
            for (int i = 1; i <= editor.Document.LineCount; i++)
            {
                var line = editor.Document.GetLineByNumber(i);
                var text = editor.Document.GetText(line.Offset, line.Length);
                if (text.StartsWith(spaces))
                {
                    int leadingSpaces = 0;
                    while (leadingSpaces < text.Length && text[leadingSpaces] == ' ')
                    {
                        leadingSpaces++;
                    }

                    int tabCount = leadingSpaces / tabSize;
                    int remainingSpaces = leadingSpaces % tabSize;
                    var newPrefix = new string('\t', tabCount) + new string(' ', remainingSpaces);
                    var converted = newPrefix + text[leadingSpaces..];
                    editor.Document.Replace(line.Offset, line.Length, converted);
                }
            }
        }
    }
}

