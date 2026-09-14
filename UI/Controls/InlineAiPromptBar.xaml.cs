using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ICSharpCode.AvalonEdit;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.UI.Controls;

/// <summary>
/// Floating in-editor bar for generating, refactoring, or modifying code inline via AI.
/// </summary>
public partial class InlineAiPromptBar : UserControl
{
    private string _originalText = "";
    private int _targetOffset = 0;
    private int _targetLength = 0;
    private int _generatedLength = 0;
    private CancellationTokenSource? _cts;
    private bool _isGenerating = false;
    private bool _hasGenerated = false;

    public TextEditor? Editor { get; set; }
    public IAiProvider? ActiveProvider { get; set; }
    public Func<string, string, Action<string>, CancellationToken, Task<string>>? CompletionHandler { get; set; }

    public InlineAiPromptBar()
    {
        InitializeComponent();
    }

    public void Show(IAiProvider? provider = null)
    {
        if (Editor == null || Editor.Document == null) return;

        if (provider != null)
        {
            ActiveProvider = provider;
        }

        if (ActiveProvider != null)
        {
            TxtProviderBadge.Text = $"{ActiveProvider.DisplayName}: {ActiveProvider.DefaultModel}";
        }
        else
        {
            TxtProviderBadge.Text = "AI Provider Ready";
        }

        // Capture current editor target
        if (Editor.SelectionLength > 0)
        {
            _targetOffset = Editor.SelectionStart;
            _targetLength = Editor.SelectionLength;
            _originalText = Editor.SelectedText;
            TxtStatus.Text = $"Selected {_originalText.Length} chars to refactor/replace";
        }
        else
        {
            _targetOffset = Editor.CaretOffset;
            _targetLength = 0;
            _originalText = string.Empty;
            TxtStatus.Text = "Generating at current caret position";
        }

        _hasGenerated = false;
        _generatedLength = 0;
        _isGenerating = false;

        ResetButtons();
        Visibility = Visibility.Visible;
        TxtPrompt.Focus();
        TxtPrompt.SelectAll();
    }

    public void Close()
    {
        if (_isGenerating)
        {
            _cts?.Cancel();
            _isGenerating = false;
        }

        if (_hasGenerated && Editor?.Document != null)
        {
            // If closed without explicitly accepting, rollback to original
            Discard();
            return;
        }

        Visibility = Visibility.Collapsed;
        Editor?.Focus();
    }

    private void ResetButtons()
    {
        BtnGenerate.Visibility = Visibility.Visible;
        BtnGenerate.IsEnabled = true;
        BtnStop.Visibility = Visibility.Collapsed;
        BtnAccept.Visibility = Visibility.Collapsed;
        BtnDiscard.Visibility = Visibility.Collapsed;
        PrgGenerating.Visibility = Visibility.Collapsed;
    }

    private void OnPromptPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            e.Handled = true;
            if (_hasGenerated)
            {
                Accept();
            }
            else
            {
                OnGenerateClick(sender, e);
            }
            return;
        }

        if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Shift) == 0)
        {
            e.Handled = true;
            OnGenerateClick(sender, e);
            return;
        }

        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            if (_isGenerating)
            {
                Stop();
            }
            else if (_hasGenerated)
            {
                Discard();
            }
            else
            {
                Close();
            }
        }
    }

    private async void OnGenerateClick(object sender, RoutedEventArgs e)
    {
        var prompt = TxtPrompt.Text.Trim();
        if (string.IsNullOrWhiteSpace(prompt)) return;
        if (Editor?.Document == null) return;

        _isGenerating = true;
        _hasGenerated = false;
        _generatedLength = 0;

        BtnGenerate.Visibility = Visibility.Collapsed;
        BtnStop.Visibility = Visibility.Visible;
        BtnAccept.Visibility = Visibility.Collapsed;
        BtnDiscard.Visibility = Visibility.Collapsed;
        PrgGenerating.Visibility = Visibility.Visible;
        TxtStatus.Text = "Streaming AI generation...";

        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        var fullGenerated = new StringBuilder();

        try
        {
            void OnDelta(string delta)
            {
                Dispatcher.Invoke(() =>
                {
                    if (Editor?.Document == null) return;
                    using (Editor.Document.RunUpdate())
                    {
                        if (!_hasGenerated)
                        {
                            // Strip leading markdown fences if model inadvertently outputs them
                            var cleanDelta = delta.StartsWith("```") ? delta.Substring(delta.IndexOf('\n') + 1) : delta;
                            Editor.Document.Replace(_targetOffset, _targetLength, cleanDelta);
                            _targetLength = 0;
                            _generatedLength = cleanDelta.Length;
                            _hasGenerated = true;
                            fullGenerated.Append(cleanDelta);
                        }
                        else
                        {
                            Editor.Document.Insert(_targetOffset + _generatedLength, delta);
                            _generatedLength += delta.Length;
                            fullGenerated.Append(delta);
                        }
                    }
                });
            }

            if (CompletionHandler != null)
            {
                await CompletionHandler(prompt, _originalText, OnDelta, ct);
            }
            else if (ActiveProvider != null)
            {
                var systemPrompt = "You are an expert pair programmer. Return ONLY raw replacement source code. Do NOT output markdown code blocks (no backticks), do NOT provide explanations or commentary.";
                var userPrompt = string.IsNullOrWhiteSpace(_originalText)
                    ? prompt
                    : $"Task: {prompt}\n\nOriginal Code:\n{_originalText}";

                await ActiveProvider.GenerateCompletionAsync(userPrompt, systemPrompt, OnDelta, ct);
            }
            else
            {
                TxtStatus.Text = "No AI provider configured. Configure in AI Chat panel.";
                ResetButtons();
                _isGenerating = false;
                return;
            }

            // Clean any trailing markdown fence if present
            Dispatcher.Invoke(() =>
            {
                if (Editor?.Document != null && _generatedLength > 0)
                {
                    var inserted = Editor.Document.GetText(_targetOffset, _generatedLength);
                    if (inserted.EndsWith("```"))
                    {
                        var trimmed = inserted.TrimEnd('`', '\r', '\n');
                        using (Editor.Document.RunUpdate())
                        {
                            Editor.Document.Replace(_targetOffset, _generatedLength, trimmed);
                            _generatedLength = trimmed.Length;
                        }
                    }
                }

                TxtStatus.Text = $"Generated {_generatedLength} chars. Press Ctrl+Enter to Accept.";
                BtnStop.Visibility = Visibility.Collapsed;
                BtnAccept.Visibility = Visibility.Visible;
                BtnDiscard.Visibility = Visibility.Visible;
                PrgGenerating.Visibility = Visibility.Collapsed;
            });
        }
        catch (OperationCanceledException)
        {
            TxtStatus.Text = "Generation stopped.";
            BtnStop.Visibility = Visibility.Collapsed;
            BtnAccept.Visibility = _hasGenerated ? Visibility.Visible : Visibility.Collapsed;
            BtnDiscard.Visibility = _hasGenerated ? Visibility.Visible : Visibility.Collapsed;
            BtnGenerate.Visibility = _hasGenerated ? Visibility.Collapsed : Visibility.Visible;
            PrgGenerating.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            TxtStatus.Text = $"Error: {ex.Message}";
            ResetButtons();
        }
        finally
        {
            _isGenerating = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    private void OnStopClick(object sender, RoutedEventArgs e) => Stop();

    public void Stop()
    {
        _cts?.Cancel();
    }

    private void OnAcceptClick(object sender, RoutedEventArgs e) => Accept();

    public void Accept()
    {
        _hasGenerated = false; // Mark resolved so Close() won't revert
        Close();
    }

    private void OnDiscardClick(object sender, RoutedEventArgs e) => Discard();

    public void Discard()
    {
        if (_hasGenerated && Editor?.Document != null)
        {
            using (Editor.Document.RunUpdate())
            {
                Editor.Document.Replace(_targetOffset, _generatedLength, _originalText);
            }
            _hasGenerated = false;
        }
        Close();
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
}
