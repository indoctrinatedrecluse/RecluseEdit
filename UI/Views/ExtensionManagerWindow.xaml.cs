using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using Microsoft.Win32;
using RecluseEdit.Core.Services;
using RecluseEdit.Extensions;

namespace RecluseEdit.UI.Views;

/// <summary>
/// Interaction logic for ExtensionManagerWindow.xaml
/// </summary>
public partial class ExtensionManagerWindow : Window
{
    private readonly ExtensionManager _extensionManager;

    public ExtensionManagerWindow(ExtensionManager extensionManager)
    {
        InitializeComponent();
        _extensionManager = extensionManager;
        RefreshList();
    }

    private void RefreshList()
    {
        ListExtensions.ItemsSource = null;
        ListExtensions.ItemsSource = _extensionManager.LoadedExtensions;
    }

    private void OnInstallDllClick(object sender, RoutedEventArgs e)
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
                        _extensionManager.LoadExtension(ext);
                    }
                }

                RefreshList();
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
}

