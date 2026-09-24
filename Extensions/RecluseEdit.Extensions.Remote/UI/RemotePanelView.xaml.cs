using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using RecluseEdit.Extensions.Remote.Models;
using RecluseEdit.Extensions.Remote.Services;
using RecluseEdit.Sdk;

namespace RecluseEdit.Extensions.Remote.UI;

public partial class RemotePanelView : UserControl, IDisposable
{
    private readonly IWorkspaceContext? _workspaceContext;
    private readonly SshProfileStore _profileStore;
    private readonly SftpBrowserService _sftpService;
    private readonly SshTunnelService _tunnelService;
    private readonly NetworkToolsService _networkTools;

    private SshSessionProfile? _activeSftpProfile;
    private string _currentSftpPath = ".";
    private GeneratedKeyPair? _latestKeyPair;

    public RemotePanelView(IWorkspaceContext? workspaceContext = null)
    {
        InitializeComponent();
        _workspaceContext = workspaceContext;

        _profileStore = new SshProfileStore();
        _sftpService = new SftpBrowserService();
        _tunnelService = new SshTunnelService();
        _networkTools = new NetworkToolsService();

        _sftpService.FileSynchronized += (remote, local) =>
        {
            Dispatcher.BeginInvoke(() =>
            {
                TxtSftpStatus.Text = $"Synced {Path.GetFileName(remote)} to remote server at {DateTime.Now:HH:mm:ss}";
            });
        };

        _tunnelService.TunnelStatusChanged += (cfg, isRunning) =>
        {
            Dispatcher.BeginInvoke(() =>
            {
                cfg.IsActive = isRunning;
                RefreshTunnelsList();
            });
        };

        LoadProfiles();
        LoadTunnels();
    }

    private void ToolBar_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is ToolBar toolBar && toolBar.Template.FindName("OverflowGrid", toolBar) is FrameworkElement overflowGrid)
        {
            overflowGrid.Visibility = Visibility.Collapsed;
        }
    }

    #region SSH Sessions Tab

    private void LoadProfiles()
    {
        var profiles = _profileStore.GetProfiles();
        ListSessions.ItemsSource = profiles;

        CmbSftpProfiles.ItemsSource = profiles;
        if (profiles.Count > 0 && CmbSftpProfiles.SelectedIndex < 0)
        {
            CmbSftpProfiles.SelectedIndex = 0;
        }
    }

    private void OnNewSessionClick(object sender, RoutedEventArgs e)
    {
        var owner = Window.GetWindow(this);
        var dlg = new SshSessionDialog(null, owner);
        if (dlg.ShowDialog() == true)
        {
            _profileStore.SaveProfile(dlg.Profile);
            LoadProfiles();
            ListSessions.SelectedItem = dlg.Profile;
        }
    }

    private void OnEditSessionClick(object sender, RoutedEventArgs e)
    {
        if (ListSessions.SelectedItem is not SshSessionProfile selected)
        {
            MessageBox.Show("Please select an SSH profile to edit.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var owner = Window.GetWindow(this);
        var dlg = new SshSessionDialog(selected, owner);
        if (dlg.ShowDialog() == true)
        {
            _profileStore.SaveProfile(dlg.Profile);
            LoadProfiles();
        }
    }

    private void OnDeleteSessionClick(object sender, RoutedEventArgs e)
    {
        if (ListSessions.SelectedItem is not SshSessionProfile selected) return;

        var res = MessageBox.Show($"Delete SSH profile '{selected.Name}'?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (res == MessageBoxResult.Yes)
        {
            _profileStore.DeleteProfile(selected.Id);
            LoadProfiles();
        }
    }

    private void OnConnectTerminalClick(object sender, RoutedEventArgs e)
    {
        if (ListSessions.SelectedItem is not SshSessionProfile selected)
        {
            MessageBox.Show("Please select a session to connect.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        ConnectTerminalForProfile(selected);
    }

    private void OnSessionDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ListSessions.SelectedItem is SshSessionProfile selected)
        {
            ConnectTerminalForProfile(selected);
        }
    }

    private void ConnectTerminalForProfile(SshSessionProfile profile)
    {
        profile.LastConnected = DateTime.UtcNow;
        _profileStore.SaveProfile(profile);

        var args = $"-p {profile.Port} {profile.Username}@{profile.Host}";
        var title = $"SSH: {profile.Name}";

        if (_workspaceContext != null)
        {
            _workspaceContext.OpenTerminal(title, "ssh.exe", args, null);
        }
        else
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/k title {title} & ssh {args}",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to launch SSH terminal: {ex.Message}", "Launch Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void OnBrowseSftpClick(object sender, RoutedEventArgs e)
    {
        if (ListSessions.SelectedItem is not SshSessionProfile selected) return;

        TabSftp.IsSelected = true;
        CmbSftpProfiles.SelectedItem = selected;
        ConnectSftp(selected);
    }

    #endregion

    #region SFTP Browser Tab

    private void OnSftpConnectToggleClick(object sender, RoutedEventArgs e)
    {
        if (_activeSftpProfile != null)
        {
            // Disconnect
            _activeSftpProfile = null;
            BtnSftpConnect.Content = "Connect";
            BtnSftpConnect.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(14, 99, 156));
            ListSftpFiles.ItemsSource = null;
            TxtSftpStatus.Text = "Disconnected";
            TxtSftpPath.Text = "";
            return;
        }

        if (CmbSftpProfiles.SelectedItem is not SshSessionProfile selected)
        {
            MessageBox.Show("Please select a session profile to connect SFTP.", "Select Profile", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        ConnectSftp(selected);
    }

    private async void ConnectSftp(SshSessionProfile profile)
    {
        _activeSftpProfile = profile;
        BtnSftpConnect.Content = "Disconnect";
        BtnSftpConnect.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(180, 50, 50));
        TxtSftpStatus.Text = $"Connecting to {profile.Host}:{profile.Port}...";

        _currentSftpPath = !string.IsNullOrWhiteSpace(profile.InitialDirectory) ? profile.InitialDirectory : ".";
        await NavigateSftpDirectoryAsync(_currentSftpPath);
    }

    private async Task NavigateSftpDirectoryAsync(string remotePath)
    {
        if (_activeSftpProfile == null) return;

        TxtSftpStatus.Text = $"Loading {remotePath}...";
        try
        {
            var files = await _sftpService.ListDirectoryAsync(_activeSftpProfile, remotePath);
            _currentSftpPath = remotePath;
            TxtSftpPath.Text = remotePath;
            ListSftpFiles.ItemsSource = files;
            TxtSftpStatus.Text = $"{files.Count} items in {remotePath}";
        }
        catch (Exception ex)
        {
            TxtSftpStatus.Text = $"Error: {ex.Message}";
            MessageBox.Show($"SFTP Error: {ex.Message}", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void OnSftpUpClick(object sender, RoutedEventArgs e)
    {
        if (_activeSftpProfile == null) return;

        if (_currentSftpPath is "." or "/" or "")
        {
            return;
        }

        var trimmed = _currentSftpPath.TrimEnd('/');
        var lastSlash = trimmed.LastIndexOf('/');
        var parent = lastSlash <= 0 ? "/" : trimmed[..lastSlash];
        await NavigateSftpDirectoryAsync(parent);
    }

    private async void OnSftpRefreshClick(object sender, RoutedEventArgs e)
    {
        if (_activeSftpProfile == null) return;
        await NavigateSftpDirectoryAsync(_currentSftpPath);
    }

    private async void OnSftpPathKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && _activeSftpProfile != null)
        {
            var path = TxtSftpPath.Text.Trim();
            if (!string.IsNullOrEmpty(path))
            {
                await NavigateSftpDirectoryAsync(path);
            }
        }
    }

    private async void OnSftpFileDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ListSftpFiles.SelectedItem is not RemoteFileItem selected || _activeSftpProfile == null) return;

        if (selected.IsDirectory)
        {
            await NavigateSftpDirectoryAsync(selected.FullPath);
        }
        else
        {
            await EditSelectedFileInEditor(selected);
        }
    }

    private async void OnSftpEditInEditorClick(object sender, RoutedEventArgs e)
    {
        if (ListSftpFiles.SelectedItem is not RemoteFileItem selected || _activeSftpProfile == null)
        {
            MessageBox.Show("Please select a file to edit.", "Edit File", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (selected.IsDirectory)
        {
            await NavigateSftpDirectoryAsync(selected.FullPath);
            return;
        }

        await EditSelectedFileInEditor(selected);
    }

    private async Task EditSelectedFileInEditor(RemoteFileItem item)
    {
        if (_activeSftpProfile == null) return;

        TxtSftpStatus.Text = $"Downloading {item.Name} for editing...";
        try
        {
            var localPath = await _sftpService.FetchForEditingAsync(_activeSftpProfile, item.FullPath);
            _workspaceContext?.OpenFile(localPath);
            TxtSftpStatus.Text = $"Editing {item.Name} (Changes will auto-upload to remote server on save)";
        }
        catch (Exception ex)
        {
            TxtSftpStatus.Text = $"Failed to open file: {ex.Message}";
            MessageBox.Show($"Failed to download file: {ex.Message}", "SFTP Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void OnSftpDownloadClick(object sender, RoutedEventArgs e)
    {
        if (ListSftpFiles.SelectedItem is not RemoteFileItem selected || _activeSftpProfile == null) return;
        if (selected.IsDirectory)
        {
            MessageBox.Show("Directory downloading is not supported yet.", "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dlg = new SaveFileDialog
        {
            FileName = selected.Name,
            Title = $"Save Remote File '{selected.Name}' As..."
        };

        if (dlg.ShowDialog() == true)
        {
            TxtSftpStatus.Text = $"Downloading {selected.Name}...";
            try
            {
                await _sftpService.DownloadFileAsync(_activeSftpProfile, selected.FullPath, dlg.FileName);
                TxtSftpStatus.Text = $"Downloaded {selected.Name} successfully.";
            }
            catch (Exception ex)
            {
                TxtSftpStatus.Text = $"Download error: {ex.Message}";
                MessageBox.Show($"Download failed: {ex.Message}", "SFTP Download Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async void OnSftpUploadClick(object sender, RoutedEventArgs e)
    {
        if (_activeSftpProfile == null)
        {
            MessageBox.Show("Please connect to an SFTP server first.", "Not Connected", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dlg = new OpenFileDialog
        {
            Title = "Select File to Upload to Remote Server"
        };

        if (dlg.ShowDialog() == true)
        {
            var localPath = dlg.FileName;
            var fileName = Path.GetFileName(localPath);
            var remoteTarget = _currentSftpPath.TrimEnd('/') + "/" + fileName;

            TxtSftpStatus.Text = $"Uploading {fileName}...";
            try
            {
                await _sftpService.UploadFileAsync(_activeSftpProfile, localPath, remoteTarget);
                TxtSftpStatus.Text = $"Uploaded {fileName} successfully.";
                await NavigateSftpDirectoryAsync(_currentSftpPath);
            }
            catch (Exception ex)
            {
                TxtSftpStatus.Text = $"Upload error: {ex.Message}";
                MessageBox.Show($"Upload failed: {ex.Message}", "SFTP Upload Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async void OnSftpNewFolderClick(object sender, RoutedEventArgs e)
    {
        if (_activeSftpProfile == null) return;

        var name = Microsoft.VisualBasic.Interaction.InputBox("Enter directory name:", "Create Remote Directory", "new_folder");
        if (string.IsNullOrWhiteSpace(name)) return;

        var target = _currentSftpPath.TrimEnd('/') + "/" + name.Trim();
        try
        {
            await _sftpService.CreateDirectoryAsync(_activeSftpProfile, target);
            await NavigateSftpDirectoryAsync(_currentSftpPath);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to create directory: {ex.Message}", "SFTP Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void OnSftpDeleteClick(object sender, RoutedEventArgs e)
    {
        if (ListSftpFiles.SelectedItem is not RemoteFileItem selected || _activeSftpProfile == null) return;

        var type = selected.IsDirectory ? "directory" : "file";
        var res = MessageBox.Show($"Are you sure you want to delete remote {type} '{selected.Name}'?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (res == MessageBoxResult.Yes)
        {
            try
            {
                await _sftpService.DeleteAsync(_activeSftpProfile, selected.FullPath, selected.IsDirectory);
                await NavigateSftpDirectoryAsync(_currentSftpPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Delete failed: {ex.Message}", "SFTP Delete Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    #endregion

    #region SSH Tunnels Tab

    private void LoadTunnels()
    {
        var tunnels = _profileStore.GetTunnels();
        foreach (var t in tunnels)
        {
            t.IsActive = _tunnelService.IsTunnelRunning(t.Id);
        }
        ListTunnels.ItemsSource = tunnels;
    }

    private void RefreshTunnelsList()
    {
        if (ListTunnels.ItemsSource is System.Collections.IEnumerable items)
        {
            ListTunnels.Items.Refresh();
        }
        UpdateTunnelButtonState();
    }

    private void OnNewTunnelClick(object sender, RoutedEventArgs e)
    {
        var owner = Window.GetWindow(this);
        var dlg = new SshTunnelDialog(null, owner);
        if (dlg.ShowDialog() == true)
        {
            _profileStore.SaveTunnel(dlg.Config);
            LoadTunnels();
        }
    }

    private void OnEditTunnelClick(object sender, RoutedEventArgs e)
    {
        if (ListTunnels.SelectedItem is not SshTunnelConfig selected)
        {
            MessageBox.Show("Please select a tunnel to edit.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var owner = Window.GetWindow(this);
        var dlg = new SshTunnelDialog(selected, owner);
        if (dlg.ShowDialog() == true)
        {
            _profileStore.SaveTunnel(dlg.Config);
            LoadTunnels();
        }
    }

    private void OnDeleteTunnelClick(object sender, RoutedEventArgs e)
    {
        if (ListTunnels.SelectedItem is not SshTunnelConfig selected) return;

        if (_tunnelService.IsTunnelRunning(selected.Id))
        {
            _tunnelService.StopTunnel(selected.Id);
        }

        _profileStore.DeleteTunnel(selected.Id);
        LoadTunnels();
    }

    private async void OnToggleTunnelClick(object sender, RoutedEventArgs e)
    {
        if (ListTunnels.SelectedItem is not SshTunnelConfig selected)
        {
            MessageBox.Show("Please select a tunnel to start or stop.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (_tunnelService.IsTunnelRunning(selected.Id))
        {
            _tunnelService.StopTunnel(selected.Id);
            selected.IsActive = false;
            RefreshTunnelsList();
        }
        else
        {
            BtnToggleTunnel.IsEnabled = false;
            try
            {
                await _tunnelService.StartTunnelAsync(selected);
                selected.IsActive = true;
                RefreshTunnelsList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to establish SSH tunnel: {ex.Message}", "Tunnel Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnToggleTunnel.IsEnabled = true;
            }
        }
    }

    private void UpdateTunnelButtonState()
    {
        if (ListTunnels.SelectedItem is SshTunnelConfig selected && selected.IsActive)
        {
            BtnToggleTunnel.Content = "⏹️ Stop";
            BtnToggleTunnel.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(180, 50, 50));
        }
        else
        {
            BtnToggleTunnel.Content = "▶️ Start";
            BtnToggleTunnel.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(14, 99, 156));
        }
    }

    #endregion

    #region Network Tools Tab

    private async void OnPingClick(object sender, RoutedEventArgs e)
    {
        var host = TxtPingHost.Text.Trim();
        if (string.IsNullOrWhiteSpace(host)) return;

        BtnPing.IsEnabled = false;
        TxtPingResult.Text = $"Pinging {host} (4 attempts)...";

        try
        {
            var report = await _networkTools.PingHostAsync(host);
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Ping results for {report.Host} [{report.ResolvedAddress}]:");
            sb.AppendLine($"Packets: Sent = {report.SentCount}, Received = {report.ReceivedCount}, Lost = {report.LostCount} ({report.PacketLossPercent:F0}% loss)");
            if (report.ReceivedCount > 0)
            {
                sb.AppendLine($"Approximate round trip times: Minimum = {report.MinRoundtripTimeMs}ms, Maximum = {report.MaxRoundtripTimeMs}ms, Average = {report.AvgRoundtripTimeMs:F1}ms");
            }
            TxtPingResult.Text = sb.ToString();
        }
        catch (Exception ex)
        {
            TxtPingResult.Text = $"Ping failed: {ex.Message}";
        }
        finally
        {
            BtnPing.IsEnabled = true;
        }
    }

    private async void OnScanPortsClick(object sender, RoutedEventArgs e)
    {
        var host = TxtScanHost.Text.Trim();
        if (string.IsNullOrWhiteSpace(host)) return;

        BtnScanPorts.IsEnabled = false;
        TxtScanResult.Text = $"Scanning common ports on {host}...";

        try
        {
            var results = await _networkTools.ScanCommonPortsAsync(host);
            var open = results.Where(r => r.IsOpen).ToList();
            var closed = results.Where(r => !r.IsOpen).ToList();

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Scan results for {host}: {open.Count} open, {closed.Count} closed");
            foreach (var r in open)
            {
                sb.AppendLine($"  🟢 Port {r.Port} ({r.ServiceName}): OPEN ({r.LatencyMs}ms)");
            }
            if (open.Count == 0)
            {
                sb.AppendLine("  No open ports detected in standard list.");
            }
            TxtScanResult.Text = sb.ToString();
        }
        catch (Exception ex)
        {
            TxtScanResult.Text = $"Scan failed: {ex.Message}";
        }
        finally
        {
            BtnScanPorts.IsEnabled = true;
        }
    }

    private async void OnDnsLookupClick(object sender, RoutedEventArgs e)
    {
        var host = TxtDnsHost.Text.Trim();
        if (string.IsNullOrWhiteSpace(host)) return;

        BtnDnsLookup.IsEnabled = false;
        TxtDnsResult.Text = $"Resolving {host}...";

        try
        {
            var report = await _networkTools.ResolveDnsAsync(host);
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Hostname: {report.Hostname}");
            if (report.IPv4Addresses.Count > 0)
            {
                sb.AppendLine($"IPv4: {string.Join(", ", report.IPv4Addresses)}");
            }
            if (report.IPv6Addresses.Count > 0)
            {
                sb.AppendLine($"IPv6: {string.Join(", ", report.IPv6Addresses)}");
            }
            if (report.Aliases.Count > 0)
            {
                sb.AppendLine($"Aliases: {string.Join(", ", report.Aliases)}");
            }
            TxtDnsResult.Text = sb.ToString();
        }
        catch (Exception ex)
        {
            TxtDnsResult.Text = $"DNS lookup failed: {ex.Message}";
        }
        finally
        {
            BtnDnsLookup.IsEnabled = true;
        }
    }

    private void OnGenerateKeyClick(object sender, RoutedEventArgs e)
    {
        int bits = CmbKeyBits.SelectedIndex == 1 ? 4096 : 2048;
        var comment = $"{Environment.UserName}@{Environment.MachineName}";

        _latestKeyPair = SshKeyGenService.GenerateRsaKey(bits, comment);
        TxtPublicKey.Text = _latestKeyPair.PublicKeyOpenSsh;
    }

    private void OnCopyPublicKeyClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtPublicKey.Text))
        {
            MessageBox.Show("Please generate an SSH key pair first.", "No Key Generated", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        Clipboard.SetText(TxtPublicKey.Text);
        MessageBox.Show("Public key copied to clipboard! You can paste it into ~/.ssh/authorized_keys on your remote server.", "Copied", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void OnSavePrivateKeyClick(object sender, RoutedEventArgs e)
    {
        if (_latestKeyPair == null)
        {
            MessageBox.Show("Please generate an SSH key pair first.", "No Key Generated", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dlg = new SaveFileDialog
        {
            Title = "Save Private Key PEM",
            FileName = "id_rsa",
            Filter = "PEM Key (*.pem)|*.pem|All Files (*.*)|*.*"
        };

        if (dlg.ShowDialog() == true)
        {
            File.WriteAllText(dlg.FileName, _latestKeyPair.PrivateKeyPem);
            MessageBox.Show($"Private key successfully saved to:\n{dlg.FileName}", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    #endregion

    public void Dispose()
    {
        _sftpService.Dispose();
        _tunnelService.Dispose();
    }
}

