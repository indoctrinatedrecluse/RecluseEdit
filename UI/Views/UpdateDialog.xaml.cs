using System.Diagnostics;
using System.Windows;
using RecluseEdit.Core.Models;
using RecluseEdit.Core.Services;

namespace RecluseEdit.UI.Views;

public partial class UpdateDialog : Window
{
    private enum DialogState
    {
        Checking,
        UpToDate,
        Available,
        Downloading,
        ReadyToInstall,
        Error
    }

    private readonly UpdateService _updateService;
    private UpdateCheckResult? _checkResult;
    private CancellationTokenSource? _cts;
    private string? _downloadedFilePath;
    private DialogState _currentState = DialogState.Checking;

    public UpdateDialog(UpdateService updateService, UpdateCheckResult? preloadedResult = null)
    {
        InitializeComponent();
        _updateService = updateService;
        _checkResult = preloadedResult;

        Loaded += OnWindowLoaded;
    }

    private async void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        if (_checkResult != null)
        {
            ApplyCheckResult(_checkResult);
        }
        else
        {
            await RunUpdateCheckAsync();
        }
    }

    private async Task RunUpdateCheckAsync()
    {
        SetState(DialogState.Checking);
        _cts = new CancellationTokenSource();

        try
        {
            var result = await _updateService.CheckForUpdatesAsync(isManual: true, _cts.Token);
            _checkResult = result;
            ApplyCheckResult(result);
        }
        catch (Exception ex)
        {
            ShowError($"Failed to complete update check: {ex.Message}");
        }
    }

    private void ApplyCheckResult(UpdateCheckResult result)
    {
        if (!result.IsSuccess)
        {
            ShowError(result.ErrorMessage ?? "Unknown update check error.");
            return;
        }

        if (result.IsUpdateAvailable && result.Asset != null)
        {
            TxtReleaseTitle.Text = string.IsNullOrWhiteSpace(result.ReleaseTitle)
                ? $"RecluseEdit v{result.LatestVersion}"
                : result.ReleaseTitle;

            TxtCurrentVersionBadge.Text = $"v{result.CurrentVersion}";
            TxtLatestVersionBadge.Text = $"v{result.LatestVersion}";
            TxtAssetSize.Text = result.Asset.FormattedSize;
            TxtReleaseNotes.Text = string.IsNullOrWhiteSpace(result.ReleaseNotes)
                ? "No detailed release notes provided for this release."
                : result.ReleaseNotes;

            SetState(DialogState.Available);
        }
        else
        {
            TxtUpToDateVersion.Text = $"RecluseEdit v{result.CurrentVersion} is currently the newest version available.";
            SetState(DialogState.UpToDate);
        }
    }

    private async void OnPrimaryButtonClick(object sender, RoutedEventArgs e)
    {
        if (_currentState == DialogState.Available)
        {
            if (_checkResult?.Asset == null) return;

            SetState(DialogState.Downloading);
            _cts = new CancellationTokenSource();

            var progress = new Progress<UpdateProgressReport>(report =>
            {
                PbDownloadProgress.Value = report.Percentage;
                TxtDownloadStatus.Text = report.StatusMessage;
                TxtDownloadPercentage.Text = $"{report.Percentage:0}%";
            });

            try
            {
                _downloadedFilePath = await _updateService.DownloadUpdateAsync(_checkResult.Asset, progress, _cts.Token);
                SetState(DialogState.ReadyToInstall);
            }
            catch (OperationCanceledException)
            {
                ApplyCheckResult(_checkResult);
            }
            catch (Exception ex)
            {
                ShowError($"Download failed: {ex.Message}");
            }
        }
        else if (_currentState == DialogState.ReadyToInstall)
        {
            if (string.IsNullOrEmpty(_downloadedFilePath) || _checkResult?.Asset == null)
            {
                ShowError("Update package not found.");
                return;
            }

            try
            {
                _updateService.PrepareAndApplyUpdate(_downloadedFilePath, _checkResult.Asset);
            }
            catch (Exception ex)
            {
                ShowError($"Failed to launch installer/updater: {ex.Message}");
            }
        }
        else if (_currentState == DialogState.Error)
        {
            await RunUpdateCheckAsync();
        }
    }

    private void OnSecondaryButtonClick(object sender, RoutedEventArgs e)
    {
        if (_currentState == DialogState.Downloading)
        {
            _cts?.Cancel();
            SetState(DialogState.Available);
        }
        else
        {
            Close();
        }
    }

    private void OnViewOnGitHubClick(object sender, RoutedEventArgs e)
    {
        var url = _checkResult?.HtmlUrl;
        if (!string.IsNullOrWhiteSpace(url))
        {
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Unable to open browser: {ex.Message}", "RecluseEdit Updater", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

    private void ShowError(string message)
    {
        TxtErrorMessage.Text = message;
        SetState(DialogState.Error);
    }

    private void SetState(DialogState state)
    {
        _currentState = state;

        PanelChecking.Visibility = state == DialogState.Checking ? Visibility.Visible : Visibility.Collapsed;
        PanelUpToDate.Visibility = state == DialogState.UpToDate ? Visibility.Visible : Visibility.Collapsed;
        PanelAvailable.Visibility = state == DialogState.Available ? Visibility.Visible : Visibility.Collapsed;
        PanelDownloading.Visibility = state == DialogState.Downloading ? Visibility.Visible : Visibility.Collapsed;
        PanelReadyToInstall.Visibility = state == DialogState.ReadyToInstall ? Visibility.Visible : Visibility.Collapsed;
        PanelError.Visibility = state == DialogState.Error ? Visibility.Visible : Visibility.Collapsed;

        switch (state)
        {
            case DialogState.Checking:
                BtnViewOnGitHub.Visibility = Visibility.Collapsed;
                BtnSecondary.Content = "Cancel";
                BtnSecondary.Visibility = Visibility.Visible;
                BtnPrimary.Visibility = Visibility.Collapsed;
                break;

            case DialogState.UpToDate:
                BtnViewOnGitHub.Visibility = Visibility.Collapsed;
                BtnSecondary.Content = "Close";
                BtnSecondary.Visibility = Visibility.Visible;
                BtnPrimary.Visibility = Visibility.Collapsed;
                break;

            case DialogState.Available:
                BtnViewOnGitHub.Visibility = Visibility.Visible;
                BtnSecondary.Content = "Remind Me Later";
                BtnSecondary.Visibility = Visibility.Visible;
                BtnPrimary.Content = "Download & Install";
                BtnPrimary.Visibility = Visibility.Visible;
                break;

            case DialogState.Downloading:
                BtnViewOnGitHub.Visibility = Visibility.Collapsed;
                BtnSecondary.Content = "Cancel";
                BtnSecondary.Visibility = Visibility.Visible;
                BtnPrimary.Visibility = Visibility.Collapsed;
                break;

            case DialogState.ReadyToInstall:
                BtnViewOnGitHub.Visibility = Visibility.Collapsed;
                BtnSecondary.Content = "Cancel";
                BtnSecondary.Visibility = Visibility.Visible;
                BtnPrimary.Content = "Restart & Install";
                BtnPrimary.Visibility = Visibility.Visible;
                break;

            case DialogState.Error:
                BtnViewOnGitHub.Visibility = Visibility.Collapsed;
                BtnSecondary.Content = "Close";
                BtnSecondary.Visibility = Visibility.Visible;
                BtnPrimary.Content = "Retry";
                BtnPrimary.Visibility = Visibility.Visible;
                break;
        }
    }
}

