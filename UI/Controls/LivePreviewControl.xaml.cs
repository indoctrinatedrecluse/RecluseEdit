using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;
using RecluseEdit.Core.Models;
using RecluseEdit.Core.Services;

namespace RecluseEdit.UI.Controls;

public partial class LivePreviewControl : UserControl
{
    private bool _isInitialized;
    private string _currentFilePath = string.Empty;
    private string _pendingContent = string.Empty;
    private bool _pendingIsMarkdown;
    private readonly DispatcherTimer _debounceTimer;

    public event Action<ConsoleLogItem>? ConsoleMessageReceived;
    public event Action? CloseRequested;

    public LivePreviewControl()
    {
        InitializeComponent();

        _debounceTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };
        _debounceTimer.Tick += OnDebounceTick;

        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_isInitialized) return;
        await EnsureWebViewInitializedAsync();
    }

    public async Task EnsureWebViewInitializedAsync()
    {
        if (_isInitialized) return;

        try
        {
            ShowStatus("Initializing Chromium engine...", false);
            var userDataFolder = Path.Combine(Path.GetTempPath(), "RecluseEdit_WebView2");
            Directory.CreateDirectory(userDataFolder);

            var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
            await WebViewControl.EnsureCoreWebView2Async(env);

            WebViewControl.CoreWebView2.Settings.IsStatusBarEnabled = false;
            WebViewControl.CoreWebView2.Settings.AreDevToolsEnabled = true;
            WebViewControl.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

            _isInitialized = true;
            HideStatus();

            if (!string.IsNullOrEmpty(_pendingContent))
            {
                RenderContent(_pendingContent, _currentFilePath, _pendingIsMarkdown);
            }
        }
        catch (Exception ex)
        {
            ShowStatus($"WebView2 Initialization failed: {ex.Message}\nMake sure Microsoft Edge WebView2 runtime is installed.", true);
        }
    }

    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            var rawJson = e.TryGetWebMessageAsString();
            if (string.IsNullOrEmpty(rawJson)) return;

            using var doc = JsonDocument.Parse(rawJson);
            var root = doc.RootElement;
            if (root.TryGetProperty("type", out var typeProp) && typeProp.GetString() == "console")
            {
                var levelStr = root.TryGetProperty("level", out var lProp) ? lProp.GetString() : "Info";
                var msg = root.TryGetProperty("message", out var mProp) ? mProp.GetString() : "";

                var level = levelStr switch
                {
                    "Error" => ConsoleLogLevel.Error,
                    "Warn" => ConsoleLogLevel.Warn,
                    "Debug" => ConsoleLogLevel.Debug,
                    _ => ConsoleLogLevel.Info
                };

                var item = new ConsoleLogItem
                {
                    Level = level,
                    Message = msg ?? "",
                    Timestamp = DateTime.Now,
                    Source = string.IsNullOrEmpty(_currentFilePath) ? "Preview" : Path.GetFileName(_currentFilePath)
                };

                Dispatcher.Invoke(() => ConsoleMessageReceived?.Invoke(item));
            }
        }
        catch
        {
            // Ignore malformed messages
        }
    }

    public void UpdateContent(string content, string filePath, bool isMarkdown)
    {
        _currentFilePath = filePath;
        _pendingContent = content;
        _pendingIsMarkdown = isMarkdown;

        DocTitleText.Text = string.IsNullOrEmpty(filePath) ? "untitled" : Path.GetFileName(filePath);

        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    private void OnDebounceTick(object? sender, EventArgs e)
    {
        _debounceTimer.Stop();
        if (_isInitialized)
        {
            RenderContent(_pendingContent, _currentFilePath, _pendingIsMarkdown);
        }
    }

    private void RenderContent(string content, string filePath, bool isMarkdown)
    {
        try
        {
            if (!_isInitialized || WebViewControl.CoreWebView2 == null) return;

            if (isMarkdown)
            {
                var html = LivePreviewBridge.MarkdownToHtml(content, filePath);
                WebViewControl.NavigateToString(html);
            }
            else
            {
                var html = LivePreviewBridge.PrepareHtmlContent(content, filePath);
                WebViewControl.NavigateToString(html);
            }
        }
        catch
        {
            // Ignore render errors during mid-typing
        }
    }

    private void OnPresetChanged(object sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton btn || btn.Tag is not string tag) return;

        if (tag == "Full")
        {
            ViewportBorder.HorizontalAlignment = HorizontalAlignment.Stretch;
            ViewportBorder.Width = double.NaN;
            ViewportBorder.BorderThickness = new Thickness(0);
        }
        else if (double.TryParse(tag, out var width))
        {
            ViewportBorder.HorizontalAlignment = HorizontalAlignment.Center;
            ViewportBorder.Width = width;
            ViewportBorder.BorderThickness = new Thickness(1);
        }
    }

    private void OnRefreshClick(object sender, RoutedEventArgs e)
    {
        if (_isInitialized)
        {
            RenderContent(_pendingContent, _currentFilePath, _pendingIsMarkdown);
        }
    }

    private void OnExternalBrowserClick(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_currentFilePath) && File.Exists(_currentFilePath))
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = _currentFilePath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open external browser: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        else if (!string.IsNullOrEmpty(_pendingContent))
        {
            try
            {
                var tempFile = Path.Combine(Path.GetTempPath(), "recluse_preview.html");
                var html = _pendingIsMarkdown
                    ? LivePreviewBridge.MarkdownToHtml(_pendingContent, _currentFilePath)
                    : LivePreviewBridge.PrepareHtmlContent(_pendingContent, _currentFilePath);

                File.WriteAllText(tempFile, html);
                Process.Start(new ProcessStartInfo { FileName = tempFile, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to launch preview in browser: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        CloseRequested?.Invoke();
    }

    private async void OnRetryInitClick(object sender, RoutedEventArgs e)
    {
        await EnsureWebViewInitializedAsync();
    }

    private void ShowStatus(string message, bool showError)
    {
        StatusMessageText.Text = message;
        StatusActionBtn.Visibility = showError ? Visibility.Visible : Visibility.Collapsed;
        StatusOverlay.Visibility = Visibility.Visible;
    }

    private void HideStatus()
    {
        StatusOverlay.Visibility = Visibility.Collapsed;
    }
}
