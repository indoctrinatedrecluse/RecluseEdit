using System.Windows.Media;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;

namespace RecluseEdit.Core.Services;

/// <summary>
/// Completion data item for AvalonEdit's popup IntelliSense completion window.
/// </summary>
public class WebCompletionData : ICompletionData
{
    public string Text { get; }
    public object Content => Text;
    public object Description { get; }
    public double Priority { get; set; }
    public ImageSource? Image => null;

    public WebCompletionData(string text, string description, double priority = 0)
    {
        Text = text;
        Description = description;
        Priority = priority;
    }

    public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
    {
        textArea.Document.Replace(completionSegment, Text);
    }
}
