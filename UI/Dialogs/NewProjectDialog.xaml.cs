using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using RecluseEdit.Core.Models;
using RecluseEdit.Core.Services;

namespace RecluseEdit.UI.Dialogs;

public partial class NewProjectDialog : Window
{
    private readonly ProjectScaffoldingService _scaffoldingService;

    public string? CreatedProjectPath { get; private set; }
    public string? EntrypointFile { get; private set; }

    public NewProjectDialog(ProjectScaffoldingService scaffoldingService, string? defaultParentDir = null)
    {
        InitializeComponent();
        _scaffoldingService = scaffoldingService;

        var templates = _scaffoldingService.GetTemplates();
        TemplatesListBox.ItemsSource = templates;
        TemplatesListBox.SelectedIndex = 0;

        if (string.IsNullOrEmpty(defaultParentDir) || !Directory.Exists(defaultParentDir))
        {
            defaultParentDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Projects");
            if (!Directory.Exists(defaultParentDir))
            {
                defaultParentDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }
        }

        LocationBox.Text = defaultParentDir;
    }

    private void OnTemplateSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TemplatesListBox.SelectedItem is ProjectTemplate template)
        {
            if (string.IsNullOrWhiteSpace(ProjectNameBox.Text) || ProjectNameBox.Text.StartsWith("my-"))
            {
                ProjectNameBox.Text = template.Id switch
                {
                    "vite-react-ts" => "my-react-app",
                    "vite-vue-ts" => "my-vue-app",
                    "vite-svelte-ts" => "my-svelte-app",
                    "vite-solid-ts" => "my-solid-app",
                    "fastify-api" => "my-fastify-api",
                    "markdown-docs" => "my-docs-site",
                    _ => "my-web-app"
                };
            }
        }
    }

    private void OnProjectNameChanged(object sender, TextChangedEventArgs e)
    {
        ValidateInputs();
    }

    private void OnBrowseLocationClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select Project Location",
            InitialDirectory = LocationBox.Text
        };

        if (dialog.ShowDialog(this) == true)
        {
            LocationBox.Text = dialog.FolderName;
        }
    }

    private void ValidateInputs()
    {
        if (CreateBtn == null) return;
        var name = ProjectNameBox.Text?.Trim();
        var loc = LocationBox.Text?.Trim();

        bool isValid = !string.IsNullOrEmpty(name) &&
                       !name.Any(c => Path.GetInvalidFileNameChars().Contains(c)) &&
                       !string.IsNullOrEmpty(loc);

        CreateBtn.IsEnabled = isValid;
    }

    private async void OnCreateClick(object sender, RoutedEventArgs e)
    {
        var template = TemplatesListBox.SelectedItem as ProjectTemplate;
        if (template == null) return;

        var name = ProjectNameBox.Text.Trim();
        var location = LocationBox.Text.Trim();

        try
        {
            Directory.CreateDirectory(location);
            var targetDir = Path.Combine(location, name);
            if (Directory.Exists(targetDir) && Directory.EnumerateFileSystemEntries(targetDir).Any())
            {
                var result = MessageBox.Show(this,
                    $"The directory '{name}' already exists and is not empty. Do you want to scaffold into it anyway?",
                    "Directory Exists", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result != MessageBoxResult.Yes) return;
            }

            CreateBtn.IsEnabled = false;
            CreateBtn.Content = "Creating...";

            var projectDir = await _scaffoldingService.ScaffoldProjectAsync(
                template.Id, location, name, InitGitCheckBox.IsChecked == true);

            CreatedProjectPath = projectDir;
            EntrypointFile = Path.Combine(projectDir, template.EntrypointRelativePath);

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Failed to create project: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            CreateBtn.IsEnabled = true;
            CreateBtn.Content = "Create Project";
        }
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

