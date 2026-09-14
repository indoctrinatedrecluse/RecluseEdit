using System;
using ICSharpCode.AvalonEdit;

namespace RecluseEdit.Core.Services;

public enum DocumentLineEnding
{
    Crlf,
    Lf
}

/// <summary>
/// Detects line ending sequence (CRLF vs. LF) and provides conversion tools.
/// </summary>
public static class LineEndingDetector
{
    public static DocumentLineEnding Detect(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return Environment.OSVersion.Platform == PlatformID.Win32NT
                ? DocumentLineEnding.Crlf
                : DocumentLineEnding.Lf;
        }

        int crlfCount = 0;
        int lfCount = 0;

        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
            {
                crlfCount++;
                i++;
            }
            else if (text[i] == '\n')
            {
                lfCount++;
            }
        }

        return crlfCount >= lfCount ? DocumentLineEnding.Crlf : DocumentLineEnding.Lf;
    }

    public static void ConvertLineEndings(TextEditor editor, DocumentLineEnding target)
    {
        if (editor.Document == null) return;

        var currentText = editor.Document.Text;
        string normalized;

        if (target == DocumentLineEnding.Crlf)
        {
            // Normalize all newlines to CRLF
            normalized = currentText.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\r\n");
        }
        else
        {
            // Normalize all newlines to LF
            normalized = currentText.Replace("\r\n", "\n").Replace("\r", "\n");
        }

        if (normalized != currentText)
        {
            int caret = Math.Min(editor.CaretOffset, normalized.Length);
            editor.Document.Text = normalized;
            editor.CaretOffset = caret;
        }
    }
}

