using System.Windows;
using System.Windows.Media;
using RecluseEdit.Core.Models;
using RecluseEdit.Core.Services.Licensing;

namespace RecluseEdit.UI.Dialogs;

/// <summary>
/// Code-behind for the License Management dialog.
/// </summary>
public partial class LicenseDialog : Window
{
    private readonly LicenseService _licenseService;
    private LicenseInfo _currentLicense;

    public LicenseDialog(LicenseService licenseService)
    {
        InitializeComponent();
        _licenseService = licenseService;
        _currentLicense = licenseService.CurrentLicense;

        TxtHwid.Text = $"Your HWID: {HwidService.GetHwid()}";
        RefreshUi();
    }

    // ─── UI refresh ──────────────────────────────────────────────────────────

    private void RefreshUi()
    {
        _currentLicense = _licenseService.CurrentLicense;
        UpdateBanner();
        UpdatePanelVisibility();
        TxtActivateError.Visibility = Visibility.Collapsed;
        TxtDeactivateError.Visibility = Visibility.Collapsed;
    }

    private void UpdateBanner()
    {
        var info = _currentLicense;

        TxtBannerIcon.Text = info.BadgeIcon;
        TxtBannerTitle.Text = info.DisplayTierName + " License";

        string detail;
        string bg;

        switch (info.Status)
        {
            case LicenseStatus.Trial:
                detail = $"Trial mode — {info.TrialDaysRemaining} day(s) remaining.\n" +
                         "Premium extensions are locked. Activate a license to unlock everything.";
                bg = "#7F6000";
                break;
            case LicenseStatus.TrialExpired:
                detail = "Your 60-day trial has expired. Activate a paid license to continue using all features.";
                bg = "#7F1F1F";
                break;
            case LicenseStatus.Active:
                detail = info.Tier == LicenseTier.Admin
                    ? $"Admin license — lifetime valid. All extensions unlocked."
                    : $"Active • Username: {info.Username}" +
                      (info.ExpiresAt.HasValue ? $" • Expires: {info.ExpiresAt.Value:yyyy-MM-dd}" : " • No expiry");
                bg = "#1A4731";
                break;
            case LicenseStatus.OfflineGrace:
                detail = $"Offline grace period — {info.GraceDaysLeft} day(s) remaining before an internet check-in is required.";
                bg = "#1A3A47";
                break;
            case LicenseStatus.Expired:
                detail = "Your license has expired. Please renew or activate a new license.";
                bg = "#7F1F1F";
                break;
            case LicenseStatus.Suspended:
                detail = "Your license has been suspended. Please contact support.";
                bg = "#4A2800";
                break;
            default:
                detail = "No license detected.";
                bg = "#3A3A3A";
                break;
        }

        TxtBannerDetail.Text = detail;
        TxtSubheading.Text = info.Tier == LicenseTier.Trial
            ? "RecluseEdit is running in trial mode. Language extensions are always available."
            : $"Managed by Licensor · Product: recluse-edit";

        BannerBorder.Background = new SolidColorBrush(
            (Color)ColorConverter.ConvertFromString(bg));
    }

    private void UpdatePanelVisibility()
    {
        var isPaidLicense = _currentLicense.Tier is LicenseTier.Admin
                                                 or LicenseTier.User
                                                 or LicenseTier.Developer;

        // Show the current-license-actions panel only for active paid licenses
        PanelCurrentLicense.Visibility = isPaidLicense ? Visibility.Visible : Visibility.Collapsed;

        // ADM licenses don't need deactivation password (no HWID binding on server for ADM)
        PanelDeactivatePassword.Visibility =
            _currentLicense.Tier == LicenseTier.Admin ? Visibility.Collapsed : Visibility.Visible;

        // Activate panel: always visible so user can enter a new key
        PanelActivate.Visibility = Visibility.Visible;

        // Credential fields collapse for ADM keys
        UpdateCredentialVisibility();
    }

    private void UpdateCredentialVisibility()
    {
        var keyText = TxtLicenseKey.Text.Trim().ToUpperInvariant();
        var isAdm = keyText.StartsWith("ADM-");
        PanelCredentials.Visibility = isAdm ? Visibility.Collapsed : Visibility.Visible;
    }

    // ─── Event handlers ──────────────────────────────────────────────────────

    private void OnLicenseKeyChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        UpdateCredentialVisibility();
        TxtActivateError.Visibility = Visibility.Collapsed;
    }

    private async void OnActivateClick(object sender, RoutedEventArgs e)
    {
        var key      = TxtLicenseKey.Text.Trim();
        var username = TxtUsername.Text.Trim();
        var password = TxtPassword.Password;

        if (string.IsNullOrEmpty(key))
        {
            ShowActivateError("Please enter a license key.");
            return;
        }

        if (!LicenseService.ValidateKeyFormat(key))
        {
            ShowActivateError("Invalid key format. Expected ADM-XXXX-XXXX-XXXX, USER-XXXX-XXXX-XXXX, or DEV-XXXX-XXXX-XXXX.");
            return;
        }

        BtnActivate.IsEnabled = false;
        BtnActivate.Content = "⏳ Activating…";

        var (ok, error) = await _licenseService.ActivateAsync(key, username, password);

        BtnActivate.IsEnabled = true;
        BtnActivate.Content = "⚡ Activate";

        if (ok)
        {
            TxtLicenseKey.Clear();
            TxtUsername.Clear();
            TxtPassword.Clear();
            RefreshUi();
        }
        else
        {
            ShowActivateError(error ?? "Activation failed. Please try again.");
        }
    }

    private async void OnDeactivateClick(object sender, RoutedEventArgs e)
    {
        var confirm = MessageBox.Show(this,
            "This will release this machine's seat on the licensing server, " +
            "allowing you to activate RecluseEdit on a different computer.\n\n" +
            "You will be reverted to Trial mode on this machine. Continue?",
            "Deactivate Machine Seat", MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes) return;

        BtnDeactivate.IsEnabled = false;
        BtnDeactivate.Content = "⏳ Deactivating…";

        var pw = TxtDeactivatePassword.Password;
        var (ok, error) = await _licenseService.DeactivateAsync(pw);

        BtnDeactivate.IsEnabled = true;
        BtnDeactivate.Content = "🔓 Deactivate Machine Seat";

        if (ok)
            RefreshUi();
        else
            ShowDeactivateError(error ?? "Deactivation failed. Please try again.");
    }

    private void OnRemoveLicenseClick(object sender, RoutedEventArgs e)
    {
        var confirm = MessageBox.Show(this,
            "This will remove the paid license from this machine and revert to Trial mode.\n" +
            "It does NOT free a seat on the server. To transfer to another machine, use 'Deactivate Machine Seat' instead.\n\n" +
            "Continue?",
            "Remove License", MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes) return;

        _licenseService.RemovePaidLicense();
        RefreshUi();
    }

    private void OnCopyHwidClick(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText(HwidService.GetHwid());
        BtnCopyHwid.Content = "✅ Copied!";
        var timer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        timer.Tick += (_, _) => { BtnCopyHwid.Content = "📋 Copy HWID"; timer.Stop(); };
        timer.Start();
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private void ShowActivateError(string msg)
    {
        TxtActivateError.Text = $"⚠️ {msg}";
        TxtActivateError.Visibility = Visibility.Visible;
    }

    private void ShowDeactivateError(string msg)
    {
        TxtDeactivateError.Text = $"⚠️ {msg}";
        TxtDeactivateError.Visibility = Visibility.Visible;
    }
}
