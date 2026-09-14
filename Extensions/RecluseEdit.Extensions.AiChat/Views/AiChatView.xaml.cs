using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using RecluseEdit.Extensions.AiChat.Models;
using RecluseEdit.Extensions.AiChat.Rendering;
using RecluseEdit.Extensions.AiChat.Services;
using RecluseEdit.Sdk;
using RecluseEdit.Sdk.Models;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.Extensions.AiChat.Views;

/// <summary>
/// Interaction logic for AiChatView.xaml
/// Provides unified AI chat across DeepSeek, OpenAI, Gemini, Antigravity, Claude, Ollama, and custom endpoints.
/// Features in-chat quick model selection and inline credential management.
/// </summary>
public partial class AiChatView : UserControl, IAiChatView
{
    public class ModelChoiceItem
    {
        public string ProviderId { get; set; } = "";
        public string ProviderName { get; set; } = "";
        public string ModelId { get; set; } = "";
        public string DisplayText { get; set; } = "";

        public override string ToString() => DisplayText;
    }

    private readonly IWorkspaceContext _workspaceContext;
    private readonly AiChatSettingsService _settingsService;
    private readonly AiChatApiClient _apiClient;

    private readonly List<ChatMessage> _conversationHistory = [];
    private readonly List<UIElement> _currentQueryStatusBadges = [];
    private CancellationTokenSource? _currentCts;
    private bool _isPopulatingHeaderModels;

    public AiChatView(
        IWorkspaceContext workspaceContext,
        AiChatSettingsService? settingsService = null,
        AiChatApiClient? apiClient = null)
    {
        InitializeComponent();

        _workspaceContext = workspaceContext;
        _settingsService = settingsService ?? new AiChatSettingsService();
        _apiClient = apiClient ?? new AiChatApiClient();

        PopulateHeaderModelPicker();
        LoadSettingsToDrawerUI();
        UpdateHeaderAuthBadge();
        UpdateActiveFileBadge();
    }

    #region Model Selection & Header Setup

    private void PopulateHeaderModelPicker()
    {
        _isPopulatingHeaderModels = true;
        try
        {
            var items = new List<ModelChoiceItem>();
            foreach (var provider in AiProviderRegistry.Providers)
            {
                if (provider.RecommendedModels.Count > 0)
                {
                    foreach (var model in provider.RecommendedModels)
                    {
                        items.Add(new ModelChoiceItem
                        {
                            ProviderId = provider.Id,
                            ProviderName = provider.DisplayName,
                            ModelId = model,
                            DisplayText = $"{provider.DisplayName}: {model}"
                        });
                    }
                }
                else
                {
                    items.Add(new ModelChoiceItem
                    {
                        ProviderId = provider.Id,
                        ProviderName = provider.DisplayName,
                        ModelId = provider.DefaultModel,
                        DisplayText = $"{provider.DisplayName}: {provider.DefaultModel}"
                    });
                }
            }

            CmbHeaderModel.ItemsSource = items;

            // Select active model from settings
            var settings = _settingsService.CurrentSettings;
            var currentProvider = settings.Provider;
            var currentModel = settings.Model;

            var match = items.FirstOrDefault(i =>
                string.Equals(i.ProviderId, currentProvider, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(i.ModelId, currentModel, StringComparison.OrdinalIgnoreCase))
                ?? items.FirstOrDefault(i => string.Equals(i.ProviderId, currentProvider, StringComparison.OrdinalIgnoreCase))
                ?? items.FirstOrDefault(i => string.Equals(i.ProviderId, "deepseek", StringComparison.OrdinalIgnoreCase))
                ?? items.FirstOrDefault();

            if (match != null)
            {
                CmbHeaderModel.SelectedItem = match;
            }
        }
        finally
        {
            _isPopulatingHeaderModels = false;
        }
    }

    private void OnHeaderModelSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isPopulatingHeaderModels) return;
        if (CmbHeaderModel.SelectedItem is not ModelChoiceItem selected) return;

        var settings = _settingsService.CurrentSettings;
        var prevProvider = settings.Provider;
        settings.Provider = selected.ProviderId;
        settings.Model = selected.ModelId;

        // If provider switched, update endpoint if default was in use
        var provider = AiProviderRegistry.GetProvider(selected.ProviderId);
        if (prevProvider != selected.ProviderId)
        {
            settings.ApiEndpoint = provider.DefaultEndpoint;
            settings.AuthMode = provider.DefaultAuthMode switch
            {
                AiAuthMode.LocalNoAuth => "local_no_auth",
                AiAuthMode.AccountToken => "account_token",
                _ => "api_key"
            };
        }

        _settingsService.SaveSettings(settings);
        UpdateHeaderAuthBadge();
        SyncDrawerWithCurrentSettings();

        // If key is missing for non-local auth, automatically open inline auth banner
        if (!settings.IsLocalNoAuth && string.IsNullOrWhiteSpace(settings.GetEffectiveToken()))
        {
            OpenInlineAuthBanner();
        }
        else
        {
            InlineAuthBanner.Visibility = Visibility.Collapsed;
        }
    }

    private void UpdateHeaderAuthBadge()
    {
        var settings = _settingsService.CurrentSettings;
        var provider = AiProviderRegistry.GetProvider(settings.Provider);

        if (settings.IsLocalNoAuth)
        {
            TxtHeaderAuthIcon.Text = "🟢";
            TxtHeaderAuthLabel.Text = "Offline";
            TxtHeaderAuthLabel.Foreground = new SolidColorBrush(Color.FromRgb(137, 209, 133));
            BtnHeaderAuth.ToolTip = $"{provider.DisplayName}: Local model, no authentication required";
            TxtWelcomeAuthHint.Text = $"Active model: {provider.DisplayName} ({settings.Model}) - Offline local execution.";
        }
        else if (!string.IsNullOrWhiteSpace(settings.GetEffectiveToken()))
        {
            TxtHeaderAuthIcon.Text = "🔑";
            TxtHeaderAuthLabel.Text = "Ready";
            TxtHeaderAuthLabel.Foreground = new SolidColorBrush(Color.FromRgb(137, 209, 133));
            BtnHeaderAuth.ToolTip = $"{provider.DisplayName}: Key configured. Click to view or change.";
            TxtWelcomeAuthHint.Text = $"Active model: {provider.DisplayName} ({settings.Model}) - Credentials configured.";
        }
        else
        {
            TxtHeaderAuthIcon.Text = "⚠️";
            TxtHeaderAuthLabel.Text = "No Key";
            TxtHeaderAuthLabel.Foreground = new SolidColorBrush(Color.FromRgb(224, 108, 117));
            BtnHeaderAuth.ToolTip = $"{provider.DisplayName}: Missing API Key or Token. Click to authenticate.";
            TxtWelcomeAuthHint.Text = $"Active model: {provider.DisplayName} ({settings.Model}) - Click 'No Key' above to authenticate.";
        }
    }

    #endregion

    #region Inline Authentication Banner

    private void OnToggleInlineAuthClick(object sender, RoutedEventArgs e)
    {
        if (InlineAuthBanner.Visibility == Visibility.Visible)
        {
            InlineAuthBanner.Visibility = Visibility.Collapsed;
        }
        else
        {
            OpenInlineAuthBanner();
        }
    }

    private void OpenInlineAuthBanner()
    {
        var settings = _settingsService.CurrentSettings;
        var provider = AiProviderRegistry.GetProvider(settings.Provider);

        TxtInlineAuthTitle.Text = $"🔑 Credentials for {provider.DisplayName} ({settings.Model})";
        var currentToken = settings.GetEffectiveToken();
        TxtInlineApiKey.Password = currentToken;
        TxtInlineApiKeyVisible.Text = currentToken;
        TxtInlineStatus.Visibility = Visibility.Collapsed;

        if (settings.IsLocalNoAuth)
        {
            TxtInlineApiKey.IsEnabled = false;
            TxtInlineApiKeyVisible.IsEnabled = false;
            TxtInlineStatus.Text = "This local provider requires no API key.";
            TxtInlineStatus.Foreground = new SolidColorBrush(Color.FromRgb(137, 209, 133));
            TxtInlineStatus.Visibility = Visibility.Visible;
        }
        else
        {
            TxtInlineApiKey.IsEnabled = true;
            TxtInlineApiKeyVisible.IsEnabled = true;
        }

        InlineAuthBanner.Visibility = Visibility.Visible;
        if (TxtInlineApiKey.IsEnabled)
        {
            TxtInlineApiKey.Focus();
        }
    }

    private void OnCloseInlineAuthClick(object sender, RoutedEventArgs e)
    {
        InlineAuthBanner.Visibility = Visibility.Collapsed;
    }

    private void OnToggleInlineShowKey(object sender, RoutedEventArgs e)
    {
        if (ChkInlineShowKey.IsChecked == true)
        {
            TxtInlineApiKeyVisible.Text = TxtInlineApiKey.Password;
            TxtInlineApiKey.Visibility = Visibility.Collapsed;
            TxtInlineApiKeyVisible.Visibility = Visibility.Visible;
        }
        else
        {
            TxtInlineApiKey.Password = TxtInlineApiKeyVisible.Text;
            TxtInlineApiKeyVisible.Visibility = Visibility.Collapsed;
            TxtInlineApiKey.Visibility = Visibility.Visible;
        }
    }

    private void OnInlineKeyKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            OnSaveInlineAuthClick(sender, e);
        }
    }

    private async void OnInlineAutoDetectClick(object sender, RoutedEventArgs e)
    {
        var settings = _settingsService.CurrentSettings;
        var provider = AiProviderRegistry.GetProvider(settings.Provider);

        BtnInlineAutoDetect.IsEnabled = false;
        TxtInlineStatus.Visibility = Visibility.Visible;
        TxtInlineStatus.Text = $"Detecting credentials for {provider.DisplayName}...";
        TxtInlineStatus.Foreground = (Brush)FindResource("TextSecondary");

        var result = await AiProviderRegistry.AutoDetectCredentialsAsync(provider.Id);
        BtnInlineAutoDetect.IsEnabled = true;

        if (result.Found)
        {
            if (!string.IsNullOrEmpty(result.Token))
            {
                TxtInlineApiKey.Password = result.Token;
                TxtInlineApiKeyVisible.Text = result.Token;
            }

            TxtInlineStatus.Text = $"✔ {result.Info}";
            TxtInlineStatus.Foreground = new SolidColorBrush(Color.FromRgb(137, 209, 133));
        }
        else
        {
            TxtInlineStatus.Text = $"ℹ {result.Info}";
            TxtInlineStatus.Foreground = new SolidColorBrush(Color.FromRgb(224, 108, 117));
        }
    }

    private void OnSaveInlineAuthClick(object sender, RoutedEventArgs e)
    {
        var settings = _settingsService.CurrentSettings;
        var token = (ChkInlineShowKey.IsChecked == true ? TxtInlineApiKeyVisible.Text : TxtInlineApiKey.Password).Trim();

        if (settings.AuthMode == "account_token")
        {
            settings.AccountToken = token;
            settings.ApiKey = token;
        }
        else
        {
            settings.ApiKey = token;
            settings.AccountToken = null;
        }

        _settingsService.SaveSettings(settings);
        UpdateHeaderAuthBadge();
        SyncDrawerWithCurrentSettings();
        InlineAuthBanner.Visibility = Visibility.Collapsed;
    }

    #endregion

    #region Drawer Settings (Advanced)

    private void LoadSettingsToDrawerUI()
    {
        CmbProvider.ItemsSource = AiProviderRegistry.Providers;
        CmbProvider.DisplayMemberPath = "DisplayName";
        CmbProvider.SelectedValuePath = "Id";

        CmbAuthMode.ItemsSource = new[]
        {
            new { Id = "api_key", Name = "Standard API Key" },
            new { Id = "account_token", Name = "Account Bearer / Session Token" },
            new { Id = "local_no_auth", Name = "Local Offline (No Auth)" }
        };
        CmbAuthMode.DisplayMemberPath = "Name";
        CmbAuthMode.SelectedValuePath = "Id";

        SyncDrawerWithCurrentSettings();
    }

    private void SyncDrawerWithCurrentSettings()
    {
        var settings = _settingsService.CurrentSettings;
        var currentProvider = AiProviderRegistry.GetProvider(settings.Provider);

        CmbProvider.SelectedValue = currentProvider.Id;
        CmbAuthMode.SelectedValue = string.IsNullOrWhiteSpace(settings.AuthMode) ? "api_key" : settings.AuthMode;

        UpdateDrawerModelList(currentProvider, settings.Model);

        TxtApiEndpoint.Text = settings.ApiEndpoint;
        var effectiveKey = settings.GetEffectiveToken();
        TxtApiKey.Password = effectiveKey;
        TxtApiKeyVisible.Text = effectiveKey;
    }

    private void UpdateDrawerModelList(AiProviderDescriptor provider, string? selectedModel)
    {
        CmbModel.ItemsSource = provider.RecommendedModels;
        CmbModel.Text = string.IsNullOrWhiteSpace(selectedModel) ? provider.DefaultModel : selectedModel;
    }

    private void OnToggleSettingsClick(object sender, RoutedEventArgs e)
    {
        SettingsDrawer.Visibility = SettingsDrawer.Visibility == Visibility.Visible
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    private void OnProviderSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbProvider.SelectedItem is AiProviderDescriptor provider)
        {
            TxtApiEndpoint.Text = provider.DefaultEndpoint;
            UpdateDrawerModelList(provider, provider.DefaultModel);
            CmbAuthMode.SelectedValue = provider.DefaultAuthMode switch
            {
                AiAuthMode.LocalNoAuth => "local_no_auth",
                AiAuthMode.AccountToken => "account_token",
                _ => "api_key"
            };
        }
    }

    private void OnAuthModeSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var authMode = CmbAuthMode.SelectedValue as string ?? "api_key";
        if (authMode == "local_no_auth")
        {
            LblSecretKey.Text = "No Authentication Required (Local)";
            TxtApiKey.IsEnabled = false;
            TxtApiKeyVisible.IsEnabled = false;
            BtnDrawerAutoDetect.IsEnabled = true;
        }
        else if (authMode == "account_token")
        {
            LblSecretKey.Text = "Account Bearer / Session Token:";
            TxtApiKey.IsEnabled = true;
            TxtApiKeyVisible.IsEnabled = true;
            BtnDrawerAutoDetect.IsEnabled = true;
        }
        else
        {
            LblSecretKey.Text = "API Key / Secret:";
            TxtApiKey.IsEnabled = true;
            TxtApiKeyVisible.IsEnabled = true;
            BtnDrawerAutoDetect.IsEnabled = true;
        }
    }

    private async void OnDrawerAutoDetectClick(object sender, RoutedEventArgs e)
    {
        if (CmbProvider.SelectedItem is not AiProviderDescriptor provider) return;

        BtnDrawerAutoDetect.IsEnabled = false;
        var result = await AiProviderRegistry.AutoDetectCredentialsAsync(provider.Id);
        BtnDrawerAutoDetect.IsEnabled = true;

        if (result.Found)
        {
            if (!string.IsNullOrEmpty(result.Token))
            {
                TxtApiKey.Password = result.Token;
                TxtApiKeyVisible.Text = result.Token;
            }

            if (!string.IsNullOrEmpty(result.SuggestedModel))
            {
                CmbModel.Text = result.SuggestedModel;
            }

            MessageBox.Show(result.Info, "Credentials Detected", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            MessageBox.Show(result.Info, "Auto-Detect Status", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
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
        var settings = _settingsService.CurrentSettings;
        var selectedProvider = CmbProvider.SelectedItem as AiProviderDescriptor ?? AiProviderRegistry.GetProvider("deepseek");
        var authMode = CmbAuthMode.SelectedValue as string ?? "api_key";
        var keyOrToken = (ChkShowKey.IsChecked == true ? TxtApiKeyVisible.Text : TxtApiKey.Password).Trim();
        var model = CmbModel.Text.Trim();

        settings.Provider = selectedProvider.Id;
        settings.AuthMode = authMode;
        settings.ApiEndpoint = TxtApiEndpoint.Text.Trim();
        if (authMode == "account_token")
        {
            settings.AccountToken = keyOrToken;
            settings.ApiKey = keyOrToken;
        }
        else
        {
            settings.ApiKey = keyOrToken;
            settings.AccountToken = null;
        }
        settings.Model = string.IsNullOrWhiteSpace(model) ? selectedProvider.DefaultModel : model;

        _settingsService.SaveSettings(settings);
        PopulateHeaderModelPicker();
        UpdateHeaderAuthBadge();

        SettingsDrawer.Visibility = Visibility.Collapsed;
    }

    #endregion

    #region Workspace Context & Active File Badge

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

    #endregion

    #region IAiChatView Implementation

    public void SubmitPrompt(string prompt)
    {
        Dispatcher.Invoke(() =>
        {
            TxtPromptInput.Text = prompt;
            OnSendPromptClick(this, new RoutedEventArgs());
        });
    }

    public void StartCodeReview(string code, string? fileName = null)
    {
        var fn = !string.IsNullOrEmpty(fileName) ? fileName : "Active File";
        var ext = Path.GetExtension(fn).TrimStart('.');
        var sb = new StringBuilder();
        sb.AppendLine($"Please perform a thorough AI Code Review for the following code snippet from `{fn}`:");
        sb.AppendLine();
        sb.AppendLine($"```{ext}");
        sb.AppendLine(code);
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("Please structure your review as follows:");
        sb.AppendLine("1. 🐛 Potential Bugs & Edge Cases (null/undefined safety, race conditions, boundary conditions)");
        sb.AppendLine("2. ⚡ Performance & Efficiency (algorithmic complexity, memory overhead, unnecessary allocations)");
        sb.AppendLine("3. 🛡️ Security Vulnerabilities (injection hazards, input validation, sensitive data exposure)");
        sb.AppendLine("4. 🧼 Clean Code & Idiomatic Best Practices (readability, modern language features, architecture)");
        sb.AppendLine("5. 💡 Actionable Improvement Suggestions (provide concrete refactored code snippets)");

        SubmitPrompt(sb.ToString());
    }

    #endregion

    #region Chat Lifecycle & Actions

    private void OnClearChatClick(object sender, RoutedEventArgs e)
    {
        if (_currentCts != null)
        {
            _currentCts.Cancel();
        }

        _currentQueryStatusBadges.Clear();
        _conversationHistory.Clear();
        MessagePanel.Children.Clear();
        WelcomeBorder.Visibility = Visibility.Visible;
        MessagePanel.Children.Add(WelcomeBorder);
        UpdateHeaderAuthBadge();
    }

    private void OnPromptInputPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Shift) == 0)
        {
            e.Handled = true;
            OnSendPromptClick(sender, e);
        }
    }

    private async void OnSendPromptClick(object sender, RoutedEventArgs e)
    {
        var input = TxtPromptInput.Text.Trim();
        if (string.IsNullOrWhiteSpace(input)) return;

        var settings = _settingsService.CurrentSettings;
        var effectiveToken = settings.GetEffectiveToken();
        if (!settings.IsLocalNoAuth && string.IsNullOrWhiteSpace(effectiveToken))
        {
            OpenInlineAuthBanner();
            return;
        }

        // Clean any leftover tool execution badges from previous queries
        foreach (var badge in _currentQueryStatusBadges)
        {
            MessagePanel.Children.Remove(badge);
        }
        _currentQueryStatusBadges.Clear();

        // Prepare prompt with optional active file context
        var promptToSend = input;
        if (ChkIncludeActiveFile.IsChecked == true && !string.IsNullOrEmpty(_workspaceContext.ActiveFilePath))
        {
            var activePath = _workspaceContext.ActiveFilePath;
            var content = _workspaceContext.ActiveDocumentContent ?? "";
            var truncated = content.Length > 8000 ? content[..8000] + "\n...[truncated]" : content;
            promptToSend = $"[Active File: {activePath}]\n```\n{truncated}\n```\n\n{input}";
        }

        // Hide welcome banner
        if (MessagePanel.Children.Contains(WelcomeBorder))
        {
            MessagePanel.Children.Remove(WelcomeBorder);
        }

        // Add user bubble
        AddMessageBubble("user", input);
        TxtPromptInput.Text = "";

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

        // Add assistant response container
        var assistantMsg = AddAssistantMessageContainer();

        // Set busy state
        BtnSendPrompt.IsEnabled = false;
        BtnStopGeneration.Visibility = Visibility.Visible;
        var provider = AiProviderRegistry.GetProvider(settings.Provider);
        TxtStatusIndicator.Text = $"{provider.DisplayName} is thinking...";

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
                        assistantMsg.FullText.Append(delta);
                        assistantMsg.LiveBox.Text = assistantMsg.FullText.ToString();
                        ScrollToBottom();
                    });
                },
                status =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        TxtStatusIndicator.Text = status;
                        AddStatusBadge(status);
                    });
                },
                ct);

            // Record assistant message
            _conversationHistory.Add(new ChatMessage
            {
                Role = "assistant",
                Content = assistantMsg.FullText.ToString()
            });
        }
        catch (OperationCanceledException)
        {
            assistantMsg.FullText.Append("\n\n*(Generation stopped by user)*");
        }
        catch (Exception ex)
        {
            assistantMsg.FullText.Append($"\n\n⚠️ Error: {ex.Message}");
        }
        finally
        {
            _currentCts?.Dispose();
            _currentCts = null;

            // Finalize: delete intermediate tool run logs & render Markdown output
            FinalizeAssistantMessage(assistantMsg);

            BtnSendPrompt.IsEnabled = true;
            BtnStopGeneration.Visibility = Visibility.Collapsed;
            TxtStatusIndicator.Text = "Idle";
            ScrollToBottom();
            UpdateActiveFileBadge();
        }
    }

    private void OnStopGenerationClick(object sender, RoutedEventArgs e)
    {
        _currentCts?.Cancel();
    }

    #endregion

    #region Message Rendering Helpers

    private class AssistantMessageContainer
    {
        public StackPanel ContentPanel { get; set; } = null!;
        public TextBox LiveBox { get; set; } = null!;
        public StringBuilder FullText { get; } = new();
    }

    private AssistantMessageContainer AddAssistantMessageContainer()
    {
        var container = new Grid
        {
            Margin = new Thickness(0, 4, 0, 4),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var border = new Border
        {
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(10, 8, 10, 8),
            Background = (Brush)FindResource("AssistantBubbleBg"),
            BorderBrush = (Brush)FindResource("BorderBrushColor"),
            BorderThickness = new Thickness(1)
        };

        var stack = new StackPanel();

        var liveBox = new TextBox
        {
            Text = "",
            TextWrapping = TextWrapping.Wrap,
            IsReadOnly = true,
            Background = Brushes.Transparent,
            Foreground = (Brush)FindResource("TextPrimary"),
            BorderThickness = new Thickness(0),
            FontSize = 12,
            FontFamily = new FontFamily("Segoe UI")
        };

        stack.Children.Add(liveBox);
        border.Child = stack;
        container.Children.Add(border);
        MessagePanel.Children.Add(container);

        ScrollToBottom();

        return new AssistantMessageContainer
        {
            ContentPanel = stack,
            LiveBox = liveBox
        };
    }

    private void FinalizeAssistantMessage(AssistantMessageContainer assistant)
    {
        // 1. Delete intermediate tool run logs
        foreach (var badge in _currentQueryStatusBadges)
        {
            MessagePanel.Children.Remove(badge);
        }
        _currentQueryStatusBadges.Clear();

        // 2. Render response as rich Markdown
        var textPrimary = (Brush)FindResource("TextPrimary");
        var accent = (Brush)FindResource("AccentColor");
        var codeBg = (Brush)FindResource("SectionBg");
        var codeBorder = (Brush)FindResource("BorderBrushColor");

        MarkdownBlockRenderer.RenderInto(
            assistant.ContentPanel,
            assistant.FullText.ToString(),
            textPrimary,
            accent,
            codeBg,
            codeBorder);

        ScrollToBottom();
    }

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
            FontFamily = new FontFamily("Segoe UI")
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
        _currentQueryStatusBadges.Add(border);
        ScrollToBottom();
    }

    private void ScrollToBottom()
    {
        ChatScrollViewer.ScrollToEnd();
    }

    #endregion
}

