using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using RecluseEdit.Extensions.DeepSeek.Models;
using RecluseEdit.Extensions.DeepSeek.Services;
using RecluseEdit.Sdk;

namespace RecluseEdit.Extensions.DeepSeek.Views;

/// <summary>
/// Interaction logic for DeepSeekChatView.xaml
/// </summary>
public partial class DeepSeekChatView : UserControl
{
    private readonly IWorkspaceContext _workspaceContext;
    private readonly DeepSeekSettingsService _settingsService;
    private readonly DeepSeekApiClient _apiClient;

    private readonly List<ChatMessage> _conversationHistory = [];
    private CancellationTokenSource? _currentCts;

    public DeepSeekChatView(
        IWorkspaceContext workspaceContext,
        DeepSeekSettingsService? settingsService = null,
        DeepSeekApiClient? apiClient = null)
    {
        InitializeComponent();

        _workspaceContext = workspaceContext;
        _settingsService = settingsService ?? new DeepSeekSettingsService();
        _apiClient = apiClient ?? new DeepSeekApiClient();

        LoadSettingsToUI();
        UpdateActiveFileBadge();
    }

    private void LoadSettingsToUI()
    {
        var settings = _settingsService.CurrentSettings;
        TxtApiEndpoint.Text = settings.ApiEndpoint;
        TxtApiKey.Password = settings.ApiKey;
        TxtApiKeyVisible.Text = settings.ApiKey;
        TxtModelName.Text = settings.Model;
        TxtModelBadge.Text = string.IsNullOrWhiteSpace(settings.Model) ? "deepseek-chat" : settings.Model;
    }

    private void UpdateActiveFileBadge()
    {
        var activePath = _workspaceContext.ActiveFilePath;
        if (!string.IsNullOrEmpty(activePath))
        {
            var fileName = Path.GetFileName(activePath);
            TxtActiveFileBadge.Text = $"📄 {fileName}";
            TxtActiveFileBadge.ToolTip = $"Active: {activePath}";
        }
        else
        {
            TxtActiveFileBadge.Text = "📄 No file";
            TxtActiveFileBadge.ToolTip = "No active document tab open";
        }
    }

    #region Settings Drawer Handlers

    private void OnToggleSettingsClick(object sender, RoutedEventArgs e)
    {
        SettingsDrawer.Visibility = SettingsDrawer.Visibility == Visibility.Visible
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    private void OnToggleShowKey(object sender, RoutedEventArgs e)
    {
        if (ChkShowKey.IsChecked == true)
        {
            TxtApiKeyVisible.Text = TxtApiKey.Password;
            TxtApiKey.Visibility = Visibility.Collapsed;
            TxtApiKeyVisible.Visibility = Visibility.Visible;
        }
        else
        {
            TxtApiKey.Password = TxtApiKeyVisible.Text;
            TxtApiKeyVisible.Visibility = Visibility.Collapsed;
            TxtApiKey.Visibility = Visibility.Visible;
        }
    }

    private void OnSaveSettingsClick(object sender, RoutedEventArgs e)
    {
        var key = ChkShowKey.IsChecked == true ? TxtApiKeyVisible.Text : TxtApiKey.Password;
        var settings = _settingsService.CurrentSettings;
        settings.ApiEndpoint = TxtApiEndpoint.Text.Trim();
        settings.ApiKey = key.Trim();
        settings.Model = TxtModelName.Text.Trim();

        _settingsService.SaveSettings(settings);
        TxtModelBadge.Text = string.IsNullOrWhiteSpace(settings.Model) ? "deepseek-chat" : settings.Model;

        SettingsDrawer.Visibility = Visibility.Collapsed;
    }

    #endregion

    #region Chat Lifecycle & Actions

    private void OnClearChatClick(object sender, RoutedEventArgs e)
    {
        if (_currentCts != null)
        {
            _currentCts.Cancel();
        }

        _conversationHistory.Clear();
        MessagePanel.Children.Clear();
        WelcomeBorder.Visibility = Visibility.Visible;
        MessagePanel.Children.Add(WelcomeBorder);
    }

    private void OnInputPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Shift) == 0)
        {
            e.Handled = true;
            OnSendClick(sender, e);
        }
    }

    private async void OnSendClick(object sender, RoutedEventArgs e)
    {
        var input = TxtInput.Text.Trim();
        if (string.IsNullOrWhiteSpace(input)) return;

        var settings = _settingsService.CurrentSettings;
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            SettingsDrawer.Visibility = Visibility.Visible;
            MessageBox.Show("Please provide your DeepSeek API key in the configuration drawer above.",
                "DeepSeek AI Configuration Required", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // Prepare prompt with optional active file context
        var promptToSend = input;
        if (ChkIncludeActiveFile.IsChecked == true && !string.IsNullOrEmpty(_workspaceContext.ActiveFilePath))
        {
            var activePath = _workspaceContext.ActiveFilePath;
            var content = _workspaceContext.ActiveDocumentContent ?? "";
            var truncated = content.Length > 8000 ? content.Substring(0, 8000) + "\n...[truncated]" : content;
            promptToSend = $"[Active File: {activePath}]\n```\n{truncated}\n```\n\n{input}";
        }

        // Hide welcome banner
        if (MessagePanel.Children.Contains(WelcomeBorder))
        {
            MessagePanel.Children.Remove(WelcomeBorder);
        }

        // Add user bubble
        AddMessageBubble("user", input);
        TxtInput.Text = "";

        // Add to history
        if (_conversationHistory.Count == 0 && !string.IsNullOrWhiteSpace(settings.SystemPrompt))
        {
            _conversationHistory.Add(new ChatMessage
            {
                Role = "system",
                Content = settings.SystemPrompt
            });
        }
        _conversationHistory.Add(new ChatMessage
        {
            Role = "user",
            Content = promptToSend
        });

        // Add assistant response bubble
        var assistantBubble = AddMessageBubble("assistant", "");

        // Set busy state
        BtnSend.IsEnabled = false;
        StatusBarBorder.Visibility = Visibility.Visible;
        TxtStatus.Text = "DeepSeek is thinking...";

        _currentCts = new CancellationTokenSource();
        var ct = _currentCts.Token;

        try
        {
            await _apiClient.SendChatStreamAsync(
                settings,
                _conversationHistory,
                _workspaceContext,
                delta =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        assistantBubble.Text += delta;
                        ScrollToBottom();
                    });
                },
                status =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        TxtStatus.Text = status;
                        AddStatusBadge(status);
                    });
                },
                ct);

            // Record assistant message
            _conversationHistory.Add(new ChatMessage
            {
                Role = "assistant",
                Content = assistantBubble.Text
            });
        }
        catch (OperationCanceledException)
        {
            assistantBubble.Text += "\n\n*(Generation stopped by user)*";
        }
        catch (Exception ex)
        {
            assistantBubble.Text += $"\n\n⚠️ Error: {ex.Message}";
        }
        finally
        {
            _currentCts?.Dispose();
            _currentCts = null;

            BtnSend.IsEnabled = true;
            StatusBarBorder.Visibility = Visibility.Collapsed;
            ScrollToBottom();
            UpdateActiveFileBadge();
        }
    }

    private void OnStopClick(object sender, RoutedEventArgs e)
    {
        _currentCts?.Cancel();
    }

    #endregion

    #region Message Rendering Helpers

    private TextBox AddMessageBubble(string role, string initialContent)
    {
        var isUser = role == "user";

        var container = new Grid
        {
            Margin = new Thickness(0, 4, 0, 4),
            HorizontalAlignment = isUser ? HorizontalAlignment.Right : HorizontalAlignment.Stretch
        };

        var border = new Border
        {
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(10, 8, 10, 8),
            Background = (Brush)FindResource(isUser ? "UserBubbleBg" : "AssistantBubbleBg"),
            BorderBrush = isUser ? null : (Brush)FindResource("BorderBrushColor"),
            BorderThickness = isUser ? new Thickness(0) : new Thickness(1),
            MaxWidth = isUser ? 300 : double.PositiveInfinity
        };

        var textBox = new TextBox
        {
            Text = initialContent,
            TextWrapping = TextWrapping.Wrap,
            IsReadOnly = true,
            Background = Brushes.Transparent,
            Foreground = isUser ? Brushes.White : (Brush)FindResource("TextPrimary"),
            BorderThickness = new Thickness(0),
            FontSize = 12,
            FontFamily = isUser ? new FontFamily("Segoe UI") : new FontFamily("Consolas, Segoe UI")
        };

        border.Child = textBox;
        container.Children.Add(border);
        MessagePanel.Children.Add(container);

        ScrollToBottom();
        return textBox;
    }

    private void AddStatusBadge(string message)
    {
        var border = new Border
        {
            CornerRadius = new CornerRadius(4),
            Background = (Brush)FindResource("ToolBubbleBg"),
            Padding = new Thickness(8, 4, 8, 4),
            Margin = new Thickness(0, 2, 0, 2),
            HorizontalAlignment = HorizontalAlignment.Center
        };

        var tb = new TextBlock
        {
            Text = $"⚡ {message}",
            Foreground = (Brush)FindResource("TextSecondary"),
            FontSize = 10
        };

        border.Child = tb;
        MessagePanel.Children.Add(border);
        ScrollToBottom();
    }

    private void ScrollToBottom()
    {
        ChatScrollViewer.ScrollToEnd();
    }

    #endregion
}

