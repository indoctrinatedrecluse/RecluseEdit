using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using Microsoft.Win32;
using RecluseEdit.Core.Services;
using RecluseEdit.Sdk;

namespace RecluseEdit.UI.Views;

/// <summary>
/// Interaction logic for ExtensionManagerWindow.xaml
/// </summary>
public partial class ExtensionManagerWindow : Window
{
    private readonly ExtensionManager _extensionManager;
    private readonly ToolchainManager _toolchainManager;

    public ExtensionManagerWindow(ExtensionManager extensionManager, ToolchainManager toolchainManager)
    {
        InitializeComponent();
        _extensionManager = extensionManager;
        _toolchainManager = toolchainManager;

        _toolchainManager.ToolchainStatusChanged += OnToolchainStatusChanged;

        RefreshLists();
    }

    private void RefreshLists()
    {
        ListExtensions.ItemsSource = null;
        ListExtensions.ItemsSource = _extensionManager.LoadedExtensions;

        ListToolchains.ItemsSource = null;
        ListToolchains.ItemsSource = _toolchainManager.Reports;
    }

    private void OnToolchainStatusChanged()
    {
        Dispatcher.Invoke(RefreshLists);
    }

    private async void OnRefreshToolchainsClick(object sender, RoutedEventArgs e)
    {
        await _toolchainManager.RunAllChecksAsync();
        RefreshLists();
    }

    private async void OnInstallDllClick(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Select Extension Assembly",
            Filter = "Extension Assemblies (*.dll)|*.dll|All Files (*.*)|*.*"
        };

        if (dlg.ShowDialog(this) == true)
        {
            try
            {
                var asm = Assembly.LoadFrom(dlg.FileName);
                var extensionTypes = asm.GetTypes()
                    .Where(t => typeof(IExtension).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                    .ToList();

                if (extensionTypes.Count == 0)
                {
                    MessageBox.Show(this, "The selected assembly does not contain any types implementing IExtension.", "Extension Installer", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                foreach (var type in extensionTypes)
                {
                    if (Activator.CreateInstance(type) is IExtension ext)
                    {
                        await _extensionManager.LoadExtensionAsync(ext);
                    }
                }

                RefreshLists();
                MessageBox.Show(this, $"Successfully installed and activated extension(s) from:\n{dlg.SafeFileName}", "Extension Installed", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Failed to load extension:\n{ex.Message}", "Extension Installer", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void OnOpenFolderClick(object sender, RoutedEventArgs e)
    {
        var extDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Extensions");
        if (!Directory.Exists(extDir))
        {
            Directory.CreateDirectory(extDir);
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = extDir,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Unable to open directory:\n{ex.Message}", "RecluseEdit", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        _toolchainManager.ToolchainStatusChanged -= OnToolchainStatusChanged;
        base.OnClosed(e);
    }
}
