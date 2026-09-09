using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace RecluseEdit.Extensions.DeepSeek.Rendering;

/// <summary>
/// Renders standard Markdown text into styled WPF UI elements for chat message presentation.
/// </summary>
public static class MarkdownBlockRenderer
{
    private static readonly FontFamily CodeFont = new("Consolas, Cascadia Code, Courier New, monospace");
    private static readonly FontFamily MainFont = new("Segoe UI, sans-serif");

    private static readonly Regex BoldRegex = new(@"\*\*(.+?)\*\*|__(.+?)__", RegexOptions.Compiled);
    private static readonly Regex ItalicRegex = new(@"\*(.+?)\*|_(.+?)_", RegexOptions.Compiled);
    private static readonly Regex InlineCodeRegex = new(@"`([^`]+)`", RegexOptions.Compiled);

    /// <summary>
    /// Parses markdown text and populates the target StackPanel with formatted WPF visual blocks.
    /// </summary>
    public static void RenderInto(
        StackPanel targetPanel,
        string markdown,
        Brush textPrimary,
        Brush accentBrush,
        Brush codeBg,
        Brush codeBorder)
    {
        targetPanel.Children.Clear();
        if (string.IsNullOrWhiteSpace(markdown)) return;

        var lines = markdown.Replace("\r\n", "\n").Split('\n');
        var inCodeBlock = false;
        var codeBlockLang = string.Empty;
        var codeBlockLines = new List<string>();

        var i = 0;
        while (i < lines.Length)
        {
            var line = lines[i];

            // 1. Fenced Code Block Detection
            if (line.TrimStart().StartsWith("```"))
            {
                if (!inCodeBlock)
                {
                    inCodeBlock = true;
                    codeBlockLang = line.TrimStart().Substring(3).Trim();
                    codeBlockLines.Clear();
                    i++;
                    continue;
                }
                else
                {
                    inCodeBlock = false;
                    var codeText = string.Join("\n", codeBlockLines);
                    targetPanel.Children.Add(CreateCodeBlockElement(codeText, codeBlockLang, codeBg, codeBorder, textPrimary));
                    codeBlockLines.Clear();
                    i++;
                    continue;
                }
            }

            if (inCodeBlock)
            {
                codeBlockLines.Add(line);
                i++;
                continue;
            }

            var trimmed = line.Trim();

            // 2. Empty Line
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                // Subtle spacer
                targetPanel.Children.Add(new Border { Height = 4 });
                i++;
                continue;
            }

            // 3. Headings
            if (trimmed.StartsWith("### "))
            {
                targetPanel.Children.Add(CreateHeadingElement(trimmed.Substring(4), 13, FontWeights.SemiBold, textPrimary, new Thickness(0, 6, 0, 2)));
                i++;
                continue;
            }
            if (trimmed.StartsWith("## "))
            {
                targetPanel.Children.Add(CreateHeadingElement(trimmed.Substring(3), 14, FontWeights.Bold, textPrimary, new Thickness(0, 8, 0, 3)));
                i++;
                continue;
            }
            if (trimmed.StartsWith("# "))
            {
                targetPanel.Children.Add(CreateHeadingElement(trimmed.Substring(2), 16, FontWeights.Bold, textPrimary, new Thickness(0, 10, 0, 4)));
                i++;
                continue;
            }

            // 4. Blockquotes
            if (trimmed.StartsWith("> "))
            {
                targetPanel.Children.Add(CreateBlockquoteElement(trimmed.Substring(2), accentBrush, textPrimary));
                i++;
                continue;
            }

            // 5. Unordered List Items (- or *)
            if ((trimmed.StartsWith("- ") || trimmed.StartsWith("* ")) && trimmed.Length > 2)
            {
                targetPanel.Children.Add(CreateListItemElement("•", trimmed.Substring(2), accentBrush, textPrimary));
                i++;
                continue;
            }

            // 6. Ordered List Items (1. , 2. )
            var orderedMatch = Regex.Match(trimmed, @"^(\d+)\.\s+(.*)$");
            if (orderedMatch.Success)
            {
                var num = orderedMatch.Groups[1].Value + ".";
                var itemText = orderedMatch.Groups[2].Value;
                targetPanel.Children.Add(CreateListItemElement(num, itemText, accentBrush, textPrimary));
                i++;
                continue;
            }

            // 7. Regular Paragraph Line
            targetPanel.Children.Add(CreateParagraphElement(trimmed, textPrimary));
            i++;
        }

        // Unclosed code block fallback
        if (inCodeBlock && codeBlockLines.Count > 0)
        {
            var codeText = string.Join("\n", codeBlockLines);
            targetPanel.Children.Add(CreateCodeBlockElement(codeText, codeBlockLang, codeBg, codeBorder, textPrimary));
        }
    }

    private static UIElement CreateHeadingElement(string text, double fontSize, FontWeight weight, Brush foreground, Thickness margin)
    {
        var tb = new TextBlock
        {
            FontSize = fontSize,
            FontWeight = weight,
            Foreground = foreground,
            FontFamily = MainFont,
            TextWrapping = TextWrapping.Wrap,
            Margin = margin
        };
        AppendFormattedInlines(tb, text, foreground);
        return tb;
    }

    private static UIElement CreateBlockquoteElement(string text, Brush accentBrush, Brush textPrimary)
    {
        var border = new Border
        {
            BorderBrush = accentBrush,
            BorderThickness = new Thickness(3, 0, 0, 0),
            Padding = new Thickness(8, 2, 0, 2),
            Margin = new Thickness(0, 3, 0, 3)
        };

        var tb = new TextBlock
        {
            FontStyle = FontStyles.Italic,
            Foreground = textPrimary,
            FontFamily = MainFont,
            TextWrapping = TextWrapping.Wrap,
            FontSize = 12
        };
        AppendFormattedInlines(tb, text, textPrimary);

        border.Child = tb;
        return border;
    }

    private static UIElement CreateListItemElement(string bullet, string text, Brush bulletBrush, Brush textPrimary)
    {
        var grid = new Grid
        {
            Margin = new Thickness(4, 1, 0, 1)
        };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var bulletBlock = new TextBlock
        {
            Text = bullet + " ",
            Foreground = bulletBrush,
            FontWeight = FontWeights.Bold,
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Top
        };
        Grid.SetColumn(bulletBlock, 0);

        var contentBlock = new TextBlock
        {
            Foreground = textPrimary,
            FontFamily = MainFont,
            TextWrapping = TextWrapping.Wrap,
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Top
        };
        AppendFormattedInlines(contentBlock, text, textPrimary);
        Grid.SetColumn(contentBlock, 1);

        grid.Children.Add(bulletBlock);
        grid.Children.Add(contentBlock);
        return grid;
    }

    private static UIElement CreateParagraphElement(string text, Brush textPrimary)
    {
        var tb = new TextBlock
        {
            Foreground = textPrimary,
            FontFamily = MainFont,
            TextWrapping = TextWrapping.Wrap,
            FontSize = 12,
            Margin = new Thickness(0, 1, 0, 2)
        };
        AppendFormattedInlines(tb, text, textPrimary);
        return tb;
    }

    private static UIElement CreateCodeBlockElement(string code, string language, Brush codeBg, Brush codeBorder, Brush textPrimary)
    {
        var container = new Border
        {
            Background = codeBg,
            BorderBrush = codeBorder,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Margin = new Thickness(0, 4, 0, 6)
        };

        var stack = new StackPanel();

        // Optional Language Header
        if (!string.IsNullOrWhiteSpace(language))
        {
            var header = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                Padding = new Thickness(8, 2, 8, 2),
                BorderBrush = codeBorder,
                BorderThickness = new Thickness(0, 0, 0, 1)
            };
            var langText = new TextBlock
            {
                Text = language.ToLowerInvariant(),
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 150)),
                FontFamily = CodeFont
            };
            header.Child = langText;
            stack.Children.Add(header);
        }

        // Code Body Box (selectable monospace)
        var textBox = new TextBox
        {
            Text = code,
            IsReadOnly = true,
            Background = Brushes.Transparent,
            Foreground = new SolidColorBrush(Color.FromRgb(212, 212, 212)),
            BorderThickness = new Thickness(0),
            FontFamily = CodeFont,
            FontSize = 11.5,
            Padding = new Thickness(8, 6, 8, 6),
            TextWrapping = TextWrapping.NoWrap,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled
        };

        stack.Children.Add(textBox);
        container.Child = stack;
        return container;
    }

    /// <summary>
    /// Parses inline formatting: **bold**, *italic*, and `inline code`.
    /// </summary>
    public static void AppendFormattedInlines(TextBlock textBlock, string text, Brush defaultBrush)
    {
        if (string.IsNullOrEmpty(text)) return;

        // Tokenize by `code` or **bold** or *italic*
        var pattern = @"(`[^`]+`|\*\*[^*]+\*\*|\*[^*]+\*)";
        var parts = Regex.Split(text, pattern);

        foreach (var part in parts)
        {
            if (string.IsNullOrEmpty(part)) continue;

            if (part.StartsWith("`") && part.EndsWith("`") && part.Length >= 2)
            {
                // Inline Code
                var codeText = part.Substring(1, part.Length - 2);
                var span = new Span
                {
                    FontFamily = CodeFont,
                    Foreground = new SolidColorBrush(Color.FromRgb(156, 220, 254)), // VS Code blue
                    Background = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255))
                };
                span.Inlines.Add(new Run(" " + codeText + " "));
                textBlock.Inlines.Add(span);
            }
            else if (part.StartsWith("**") && part.EndsWith("**") && part.Length >= 4)
            {
                // Bold
                var boldText = part.Substring(2, part.Length - 4);
                var bold = new Bold(new Run(boldText)) { Foreground = defaultBrush };
                textBlock.Inlines.Add(bold);
            }
            else if (part.StartsWith("*") && part.EndsWith("*") && part.Length >= 2)
            {
                // Italic
                var italicText = part.Substring(1, part.Length - 2);
                var italic = new Italic(new Run(italicText)) { Foreground = defaultBrush };
                textBlock.Inlines.Add(italic);
            }
            else
            {
                textBlock.Inlines.Add(new Run(part) { Foreground = defaultBrush });
            }
        }
    }
}
