using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RecluseEdit.Extensions.DeepSeek.Rendering;

namespace RecluseEdit.Tests;

[TestClass]
public class DeepSeekMarkdownTests
{
    [STATestMethod]
    public void TestMarkdownBlockRenderer_RendersHeadings()
    {
        var panel = new StackPanel();
        var markdown = """
        # Big Title
        ## Subtitle
        ### Section Header
        """;

        MarkdownBlockRenderer.RenderInto(
            panel,
            markdown,
            Brushes.White,
            Brushes.Cyan,
            Brushes.DarkGray,
            Brushes.Gray);

        var textBlocks = panel.Children.OfType<TextBlock>().ToList();
        Assert.HasCount(3, textBlocks);

        Assert.AreEqual(16, textBlocks[0].FontSize);
        Assert.AreEqual(FontWeights.Bold, textBlocks[0].FontWeight);

        Assert.AreEqual(14, textBlocks[1].FontSize);
        Assert.AreEqual(FontWeights.Bold, textBlocks[1].FontWeight);

        Assert.AreEqual(13, textBlocks[2].FontSize);
        Assert.AreEqual(FontWeights.SemiBold, textBlocks[2].FontWeight);
    }

    [STATestMethod]
    public void TestMarkdownBlockRenderer_RendersCodeBlocks()
    {
        var panel = new StackPanel();
        var markdown = """
        Here is some code:
        ```csharp
        var msg = "Hello, World!";
        Console.WriteLine(msg);
        ```
        And text after.
        """;

        MarkdownBlockRenderer.RenderInto(
            panel,
            markdown,
            Brushes.White,
            Brushes.Cyan,
            Brushes.Black,
            Brushes.DarkGray);

        var borders = panel.Children.OfType<Border>().ToList();
        Assert.IsNotEmpty(borders);

        var codeBorder = borders.FirstOrDefault(b => b.Child is StackPanel s && s.Children.OfType<TextBox>().Any());
        Assert.IsNotNull(codeBorder);

        var stack = (StackPanel)codeBorder.Child;
        var textBox = stack.Children.OfType<TextBox>().First();
        Assert.Contains("Console.WriteLine(msg);", textBox.Text);
        Assert.IsTrue(textBox.IsReadOnly);
    }

    [STATestMethod]
    public void TestMarkdownBlockRenderer_RendersListsAndQuotes()
    {
        var panel = new StackPanel();
        var markdown = """
        - First bullet item
        - Second bullet item
        1. Numbered item
        > Quoted message text
        """;

        MarkdownBlockRenderer.RenderInto(
            panel,
            markdown,
            Brushes.White,
            Brushes.Cyan,
            Brushes.Black,
            Brushes.DarkGray);

        var grids = panel.Children.OfType<Grid>().ToList();
        Assert.HasCount(3, grids); // 2 bullet items + 1 numbered item

        var quoteBorders = panel.Children.OfType<Border>().Where(b => b.Child is TextBlock tb && tb.FontStyle == FontStyles.Italic).ToList();
        Assert.HasCount(1, quoteBorders);
    }

    [STATestMethod]
    public void TestMarkdownBlockRenderer_InlineFormatting()
    {
        var tb = new TextBlock();
        MarkdownBlockRenderer.AppendFormattedInlines(tb, "This is **bold** and *italic* and `code` formatting", Brushes.White);

        var boldInlines = tb.Inlines.OfType<Bold>().ToList();
        Assert.HasCount(1, boldInlines);

        var italicInlines = tb.Inlines.OfType<Italic>().ToList();
        Assert.HasCount(1, italicInlines);

        var rawSpanInlines = tb.Inlines.OfType<Span>().Where(s => s is not Bold && s is not Italic).ToList();
        Assert.HasCount(1, rawSpanInlines);
    }
}
