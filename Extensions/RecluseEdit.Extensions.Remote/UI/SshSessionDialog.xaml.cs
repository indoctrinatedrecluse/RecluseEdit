using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using RecluseEdit.Extensions.Remote.Models;

namespace RecluseEdit.Extensions.Remote.UI;

public partial class SshSessionDialog : Window
{
    public SshSessionProfile Profile { get; private set; }

    public SshSessionDialog(SshSessionProfile? existingProfile = null, Window? owner = null)
    {
        InitializeComponent();
        if (owner != null) Owner = owner;

        if (existingProfile != null)
        {
            Profile = new SshSessionProfile
            {
                Id = existingProfile.Id,
                Name = existingProfile.Name,
                Group = existingProfile.Group,
                Host = existingProfile.Host,
                Port = existingProfile.Port,
                Username = existingProfile.Username,
                AuthType = existingProfile.AuthType,
                Password = existingProfile.Password,
                PrivateKeyPath = existingProfile.PrivateKeyPath,
                Passphrase = existingProfile.Passphrase,
                InitialDirectory = existingProfile.InitialDirectory,
                Notes = existingProfile.Notes,
                LastConnected = existingProfile.LastConnected
            };

            TxtName.Text = Profile.Name;
            TxtGroup.Text = Profile.Group;
            TxtHost.Text = Profile.Host;
            TxtPort.Text = Profile.Port.ToString();
            TxtUsername.Text = Profile.Username;
            if (Profile.AuthType == SshAuthType.Password)
            {
                RbPassword.IsChecked = true;
                TxtPassword.Password = Profile.Password ?? string.Empty;
            }
            else
            {
                RbPrivateKey.IsChecked = true;
                TxtPrivateKeyPath.Text = Profile.PrivateKeyPath ?? string.Empty;
            }
        }
        else
        {
            Profile = new SshSessionProfile();
            TxtName.Text = "Ubuntu Server";
            TxtHost.Text = "192.168.1.100";
            TxtUsername.Text = "ubuntu";
            TxtPort.Text = "22";
        }

        UpdateAuthPanels();
    }

    private void OnAuthTypeChanged(object sender, RoutedEventArgs e)
    {
        UpdateAuthPanels();
    }

    private void UpdateAuthPanels()
    {
        if (PanelPassword == null || PanelPrivateKey == null) return;

        bool isPassword = RbPassword.IsChecked == true;
        PanelPassword.Visibility = isPassword ? Visibility.Visible : Visibility.Collapsed;
        PanelPrivateKey.Visibility = isPassword ? Visibility.Collapsed : Visibility.Visible;
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
            TxtPrivateKeyPath.Text = dlg.FileName;
        }
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        var name = TxtName.Text.Trim();
        var host = TxtHost.Text.Trim();
        var user = TxtUsername.Text.Trim();
        var portStr = TxtPort.Text.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("Please enter a profile name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtName.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(host))
        {
            MessageBox.Show("Please enter a host address (IP or hostname).", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtHost.Focus();
            return;
        }

        if (!int.TryParse(portStr, out var port) || port <= 0 || port > 65535)
        {
            MessageBox.Show("Please enter a valid TCP port number (1-65535).", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtPort.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(user))
        {
            MessageBox.Show("Please enter a username.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtUsername.Focus();
            return;
        }

        Profile.Name = name;
        Profile.Group = string.IsNullOrWhiteSpace(TxtGroup.Text) ? "Default" : TxtGroup.Text.Trim();
        Profile.Host = host;
        Profile.Port = port;
        Profile.Username = user;
        Profile.AuthType = RbPassword.IsChecked == true ? SshAuthType.Password : SshAuthType.PrivateKey;

        if (Profile.AuthType == SshAuthType.Password)
        {
            Profile.Password = TxtPassword.Password;
        }
        else
        {
            Profile.PrivateKeyPath = TxtPrivateKeyPath.Text.Trim();
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

