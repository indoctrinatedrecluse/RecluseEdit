using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using RecluseEdit.Core.Models;
using RecluseEdit.Core.Services;

namespace RecluseEdit.UI.Controls;

public partial class SourceControlPanel : UserControl
{
    private readonly GitService _gitService = new();
    private string? _workspacePath;
    private GitRepoStatus? _currentStatus;

    public event Action<string>? FileSelected;

    public string? WorkspacePath
    {
        get => _workspacePath;
        set
        {
            _workspacePath = value;
            _ = RefreshAsync();
        }
    }

    public SourceControlPanel()
    {
        InitializeComponent();
    }

    public async Task RefreshAsync()
    {
        if (string.IsNullOrWhiteSpace(_workspacePath))
        {
            TxtNoRepo.Visibility = Visibility.Visible;
            ExpanderStaged.Visibility = Visibility.Collapsed;
            ExpanderChanges.Visibility = Visibility.Collapsed;
            BtnCommit.IsEnabled = false;
            TxtBranch.Text = "No Workspace";
            TxtGitStatus.Text = "No folder opened";
            return;
        }

        TxtGitStatus.Text = "Refreshing status...";
        var status = await _gitService.GetStatusAsync(_workspacePath);
        _currentStatus = status;

        if (!status.IsGitRepository)
        {
            TxtNoRepo.Visibility = Visibility.Visible;
            ExpanderStaged.Visibility = Visibility.Collapsed;
            ExpanderChanges.Visibility = Visibility.Collapsed;
            BtnCommit.IsEnabled = false;
            TxtBranch.Text = "Not a Git Repo";
            TxtGitStatus.Text = "Git not detected in workspace";
            return;
        }

        TxtNoRepo.Visibility = Visibility.Collapsed;
        ExpanderStaged.Visibility = Visibility.Visible;
        ExpanderChanges.Visibility = Visibility.Visible;
        BtnCommit.IsEnabled = true;

        string branchDisplay = $"🌿 {status.Branch}";
        if (status.Ahead > 0 || status.Behind > 0)
        {
            branchDisplay += $" ↑{status.Ahead} ↓{status.Behind}";
        }
        TxtBranch.Text = branchDisplay;

        var stagedList = status.StagedFiles.ToList();
        var changesList = status.UnstagedFiles.ToList();

        ListStaged.ItemsSource = stagedList;
        TxtStagedCount.Text = stagedList.Count.ToString();

        ListChanges.ItemsSource = changesList;
        TxtChangesCount.Text = changesList.Count.ToString();

        TxtGitStatus.Text = $"{stagedList.Count + changesList.Count} changes detected";
    }

    private void OnFileItemClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: GitFileItem item })
        {
            FileSelected?.Invoke(item.FilePath);
        }
    }

    private async void OnStageFileClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: GitFileItem item } && !string.IsNullOrEmpty(_workspacePath))
        {
            TxtGitStatus.Text = $"Staging {item.FileName}...";
            var (success, _) = await _gitService.StageFileAsync(_workspacePath, item.FilePath);
            if (success)
            {
                await RefreshAsync();
            }
            else
            {
                TxtGitStatus.Text = $"Failed to stage {item.FileName}";
            }
        }
    }

    private async void OnUnstageFileClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: GitFileItem item } && !string.IsNullOrEmpty(_workspacePath))
        {
            TxtGitStatus.Text = $"Unstaging {item.FileName}...";
            var (success, _) = await _gitService.UnstageFileAsync(_workspacePath, item.FilePath);
            if (success)
            {
                await RefreshAsync();
            }
            else
            {
                TxtGitStatus.Text = $"Failed to unstage {item.FileName}";
            }
        }
    }

    private async void OnDiscardFileClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: GitFileItem item } && !string.IsNullOrEmpty(_workspacePath))
        {
            var res = MessageBox.Show(
                $"Are you sure you want to discard changes in '{item.FileName}'?\nThis cannot be undone.",
                "Discard Changes",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (res == MessageBoxResult.Yes)
            {
                TxtGitStatus.Text = $"Discarding {item.FileName}...";
                var (success, _) = await _gitService.DiscardChangesAsync(_workspacePath, item.FilePath);
                if (success)
                {
                    await RefreshAsync();
                }
                else
                {
                    TxtGitStatus.Text = $"Failed to discard {item.FileName}";
                }
            }
        }
    }

    private async void OnCommitClick(object sender, RoutedEventArgs e)
    {
        await ExecuteCommitAsync();
    }

    private async void OnCommitMessageKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            e.Handled = true;
            await ExecuteCommitAsync();
        }
    }

    private async Task ExecuteCommitAsync()
    {
        if (string.IsNullOrWhiteSpace(_workspacePath)) return;

        string msg = TxtCommitMessage.Text.Trim();
        if (string.IsNullOrEmpty(msg))
        {
            MessageBox.Show("Please enter a commit message.", "Source Control", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // If no staged files, stage all tracked changes first
        if (_currentStatus != null && !_currentStatus.StagedFiles.Any() && _currentStatus.UnstagedFiles.Any())
        {
            var stagePrompt = MessageBox.Show("There are no staged changes to commit.\nWould you like to stage all changes and commit?", "Source Control", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (stagePrompt == MessageBoxResult.Yes)
            {
                await _gitService.StageFileAsync(_workspacePath, ".");
            }
            else
            {
                return;
            }
        }

        TxtGitStatus.Text = "Committing...";
        BtnCommit.IsEnabled = false;

        var (success, output) = await _gitService.CommitAsync(_workspacePath, msg);
        BtnCommit.IsEnabled = true;

        if (success)
        {
            TxtCommitMessage.Clear();
            TxtGitStatus.Text = "Committed successfully.";
            await RefreshAsync();
        }
        else
        {
            MessageBox.Show($"Commit failed:\n{output}", "Git Error", MessageBoxButton.OK, MessageBoxImage.Error);
            TxtGitStatus.Text = "Commit failed.";
        }
    }

    private async void OnPushClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_workspacePath)) return;
        TxtGitStatus.Text = "Pushing to remote...";
        var (success, output) = await _gitService.PushAsync(_workspacePath);
        if (success)
        {
            TxtGitStatus.Text = "Pushed to remote.";
            await RefreshAsync();
        }
        else
        {
            MessageBox.Show($"Push failed:\n{output}", "Git Error", MessageBoxButton.OK, MessageBoxImage.Error);
            TxtGitStatus.Text = "Push failed.";
        }
    }

    private async void OnPullClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_workspacePath)) return;
        TxtGitStatus.Text = "Pulling from remote...";
        var (success, output) = await _gitService.PullAsync(_workspacePath);
        if (success)
        {
            TxtGitStatus.Text = "Pulled from remote.";
            await RefreshAsync();
        }
        else
        {
            MessageBox.Show($"Pull failed:\n{output}", "Git Error", MessageBoxButton.OK, MessageBoxImage.Error);
            TxtGitStatus.Text = "Pull failed.";
        }
    }

    private void OnRefreshClick(object sender, RoutedEventArgs e)
    {
        _ = RefreshAsync();
    }
}
