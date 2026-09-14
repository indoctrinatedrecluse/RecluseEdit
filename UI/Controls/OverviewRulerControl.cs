using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
using RecluseEdit.Core.Models;
using RecluseEdit.Core.Services;

namespace RecluseEdit.UI.Controls;

public enum OverviewMarkerKind
{
    FindMatch,
    GitAdded,
    GitModified,
    GitDeleted,
    DiagnosticError,
    DiagnosticWarning,
    DiagnosticInfo
}

public record OverviewMarker(int LineNumber, OverviewMarkerKind Kind);

/// <summary>
/// Renders an overview ruler along the vertical scrollbar track showing colored tick markers
/// for Find matches, Git changes, and compiler/linter diagnostics.
/// Clicking on any marker immediately scrolls the editor to that line.
/// </summary>
public class OverviewRulerControl : Control
{
    private static readonly Brush FindMatchBrush = new SolidColorBrush(Color.FromRgb(229, 160, 13)); // Amber
    private static readonly Brush GitAddedBrush = new SolidColorBrush(Color.FromRgb(56, 138, 52)); // Green
    private static readonly Brush GitModifiedBrush = new SolidColorBrush(Color.FromRgb(0, 122, 204)); // Blue
    private static readonly Brush GitDeletedBrush = new SolidColorBrush(Color.FromRgb(229, 20, 0)); // Red
    private static readonly Brush DiagErrorBrush = new SolidColorBrush(Color.FromRgb(234, 67, 53)); // Red
    private static readonly Brush DiagWarnBrush = new SolidColorBrush(Color.FromRgb(251, 188, 4)); // Yellow
    private static readonly Brush DiagInfoBrush = new SolidColorBrush(Color.FromRgb(66, 133, 244)); // Blue

    static OverviewRulerControl()
    {
        FindMatchBrush.Freeze();
        GitAddedBrush.Freeze();
        GitModifiedBrush.Freeze();
        GitDeletedBrush.Freeze();
        DiagErrorBrush.Freeze();
        DiagWarnBrush.Freeze();
        DiagInfoBrush.Freeze();
    }

    private readonly List<OverviewMarker> _markers = [];
    private readonly List<int> _findLines = [];
    private readonly List<OverviewMarker> _gitMarkers = [];
    private readonly List<OverviewMarker> _diagMarkers = [];

    public TextEditor? Editor { get; set; }

    public OverviewRulerControl()
    {
        Width = 14;
        Cursor = Cursors.Hand;
        ClipToBounds = true;
    }

    public void SetFindMatches(IEnumerable<int> lines)
    {
        _findLines.Clear();
        _findLines.AddRange(lines);
        RebuildMarkers();
    }

    public void SetGitHunks(IEnumerable<DiffHunk> hunks)
    {
        _gitMarkers.Clear();
        foreach (var hunk in hunks)
        {
            var kind = hunk.Type switch
            {
                DiffHunkType.Added => OverviewMarkerKind.GitAdded,
                DiffHunkType.Deleted => OverviewMarkerKind.GitDeleted,
                _ => OverviewMarkerKind.GitModified
            };
            int line = hunk.NewStartLine > 0 ? hunk.NewStartLine : Math.Max(1, hunk.OldStartLine);
            _gitMarkers.Add(new OverviewMarker(line, kind));
        }
        RebuildMarkers();
    }

    public void SetDiagnostics(IEnumerable<DiagnosticItem> diagnostics)
    {
        _diagMarkers.Clear();
        foreach (var d in diagnostics)
        {
            var kind = d.Severity switch
            {
                DiagnosticSeverity.Error => OverviewMarkerKind.DiagnosticError,
                DiagnosticSeverity.Warning => OverviewMarkerKind.DiagnosticWarning,
                _ => OverviewMarkerKind.DiagnosticInfo
            };
            _diagMarkers.Add(new OverviewMarker(d.LineNumber, kind));
        }
        RebuildMarkers();
    }

    public void Clear()
    {
        _findLines.Clear();
        _gitMarkers.Clear();
        _diagMarkers.Clear();
        _markers.Clear();
        InvalidateVisual();
    }

    private void RebuildMarkers()
    {
        _markers.Clear();

        // 1. Git markers (rendered on the left half of the ruler)
        _markers.AddRange(_gitMarkers);

        // 2. Find matches
        foreach (var l in _findLines)
        {
            _markers.Add(new OverviewMarker(l, OverviewMarkerKind.FindMatch));
        }

        // 3. Diagnostics (highest visual priority)
        _markers.AddRange(_diagMarkers);

        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        if (Editor?.Document == null || Editor.Document.LineCount == 0 || ActualHeight <= 0) return;

        int totalLines = Editor.Document.LineCount;
        double height = ActualHeight;
        double width = ActualWidth;

        // Draw subtle background track
        if (Background != null)
        {
            dc.DrawRectangle(Background, null, new Rect(0, 0, width, height));
        }
        else
        {
            dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(30, 0, 0, 0)), null, new Rect(0, 0, width, height));
        }

        foreach (var m in _markers)
        {
            if (m.LineNumber < 1 || m.LineNumber > totalLines) continue;

            double fraction = (double)(m.LineNumber - 1) / Math.Max(1, totalLines);
            double y = fraction * (height - 4);

            Brush brush;
            double x = 2;
            double tickWidth = width - 4;
            double tickHeight = 3;

            switch (m.Kind)
            {
                case OverviewMarkerKind.FindMatch:
                    brush = FindMatchBrush;
                    tickHeight = 3;
                    break;
                case OverviewMarkerKind.GitAdded:
                    brush = GitAddedBrush;
                    tickWidth = 4;
                    break;
                case OverviewMarkerKind.GitModified:
                    brush = GitModifiedBrush;
                    tickWidth = 4;
                    break;
                case OverviewMarkerKind.GitDeleted:
                    brush = GitDeletedBrush;
                    tickWidth = 4;
                    break;
                case OverviewMarkerKind.DiagnosticError:
                    brush = DiagErrorBrush;
                    tickHeight = 4;
                    break;
                case OverviewMarkerKind.DiagnosticWarning:
                    brush = DiagWarnBrush;
                    tickHeight = 4;
                    break;
                default:
                    brush = DiagInfoBrush;
                    tickHeight = 3;
                    break;
            }

            dc.DrawRoundedRectangle(brush, null, new Rect(x, y, tickWidth, tickHeight), 1, 1);
        }
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);

        if (Editor?.Document == null || Editor.Document.LineCount == 0 || ActualHeight <= 0) return;

        var pos = e.GetPosition(this);
        double fraction = Math.Clamp(pos.Y / ActualHeight, 0, 1);
        int targetLine = (int)(fraction * Editor.Document.LineCount) + 1;
        targetLine = Math.Clamp(targetLine, 1, Editor.Document.LineCount);

        Editor.ScrollTo(targetLine, 1);
        Editor.TextArea.Caret.Line = targetLine;
        Editor.Focus();
        e.Handled = true;
    }
}

