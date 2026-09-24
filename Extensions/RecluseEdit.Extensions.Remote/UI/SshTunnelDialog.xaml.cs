using System;
using System.Windows;
using Microsoft.Win32;
using RecluseEdit.Extensions.Remote.Models;

namespace RecluseEdit.Extensions.Remote.UI;

public partial class SshTunnelDialog : Window
{
    public SshTunnelConfig Config { get; private set; }

    public SshTunnelDialog(SshTunnelConfig? existingConfig = null, Window? owner = null)
    {
        InitializeComponent();
        if (owner != null) Owner = owner;

        if (existingConfig != null)
        {
            Config = new SshTunnelConfig
            {
                Id = existingConfig.Id,
                Name = existingConfig.Name,
                SshHost = existingConfig.SshHost,
                SshPort = existingConfig.SshPort,
                SshUser = existingConfig.SshUser,
                AuthType = existingConfig.AuthType,
                SshPassword = existingConfig.SshPassword,
                SshKeyPath = existingConfig.SshKeyPath,
                LocalPort = existingConfig.LocalPort,
                RemoteHost = existingConfig.RemoteHost,
                RemotePort = existingConfig.RemotePort
            };

            TxtTunnelName.Text = Config.Name;
            TxtLocalPort.Text = Config.LocalPort.ToString();
            TxtRemoteHost.Text = Config.RemoteHost;
            TxtRemotePort.Text = Config.RemotePort.ToString();
            TxtSshHost.Text = Config.SshHost;
            TxtSshPort.Text = Config.SshPort.ToString();
            TxtSshUser.Text = Config.SshUser;

            if (Config.AuthType == SshAuthType.Password)
            {
                RbTunnelPassword.IsChecked = true;
                TxtTunnelPassword.Password = Config.SshPassword ?? string.Empty;
            }
            else
            {
                RbTunnelPrivateKey.IsChecked = true;
                TxtTunnelKeyPath.Text = Config.SshKeyPath ?? string.Empty;
            }
        }
        else
        {
            Config = new SshTunnelConfig();
        }

        UpdateAuthPanels();
    }

    private void OnTunnelAuthTypeChanged(object sender, RoutedEventArgs e)
    {
        UpdateAuthPanels();
    }

    private void UpdateAuthPanels()
    {
        if (PanelTunnelPassword == null || PanelTunnelPrivateKey == null) return;

        bool isPassword = RbTunnelPassword.IsChecked == true;
        PanelTunnelPassword.Visibility = isPassword ? Visibility.Visible : Visibility.Collapsed;
        PanelTunnelPrivateKey.Visibility = isPassword ? Visibility.Collapsed : Visibility.Visible;
    }

    private void OnBrowsePrivateKeyClick(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Select SSH Private Key",
            Filter = "Key Files (*.pem;*.id_rsa;*.key;*)|*.pem;*.id_rsa;*.key;*|All Files (*.*)|*.*"
        };

        if (dlg.ShowDialog() == true)
        {
            TxtTunnelKeyPath.Text = dlg.FileName;
        }
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        var name = TxtTunnelName.Text.Trim();
        var localPortStr = TxtLocalPort.Text.Trim();
        var remoteHost = TxtRemoteHost.Text.Trim();
        var remotePortStr = TxtRemotePort.Text.Trim();
        var sshHost = TxtSshHost.Text.Trim();
        var sshPortStr = TxtSshPort.Text.Trim();
        var sshUser = TxtSshUser.Text.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("Please enter a tunnel name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtTunnelName.Focus();
            return;
        }

        if (!uint.TryParse(localPortStr, out var localPort) || localPort == 0 || localPort > 65535)
        {
            MessageBox.Show("Please enter a valid local port (1-65535).", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtLocalPort.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(remoteHost))
        {
            MessageBox.Show("Please enter a remote host destination.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtRemoteHost.Focus();
            return;
        }

        if (!uint.TryParse(remotePortStr, out var remotePort) || remotePort == 0 || remotePort > 65535)
        {
            MessageBox.Show("Please enter a valid remote destination port (1-65535).", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtRemotePort.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(sshHost))
        {
            MessageBox.Show("Please enter the SSH server address.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtSshHost.Focus();
            return;
        }

        if (!int.TryParse(sshPortStr, out var sshPort) || sshPort <= 0 || sshPort > 65535)
        {
            MessageBox.Show("Please enter a valid SSH port (1-65535).", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtSshPort.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(sshUser))
        {
            MessageBox.Show("Please enter the SSH username.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtSshUser.Focus();
            return;
        }

        Config.Name = name;
        Config.LocalPort = localPort;
        Config.RemoteHost = remoteHost;
        Config.RemotePort = remotePort;
        Config.SshHost = sshHost;
        Config.SshPort = sshPort;
        Config.SshUser = sshUser;
        Config.AuthType = RbTunnelPassword.IsChecked == true ? SshAuthType.Password : SshAuthType.PrivateKey;

        if (Config.AuthType == SshAuthType.Password)
        {
            Config.SshPassword = TxtTunnelPassword.Password;
        }
        else
        {
            Config.SshKeyPath = TxtTunnelKeyPath.Text.Trim();
        }

        DialogResult = true;
        Close();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

