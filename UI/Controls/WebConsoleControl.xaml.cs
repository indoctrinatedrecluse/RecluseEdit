using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using RecluseEdit.Core.Models;

namespace RecluseEdit.UI.Controls;

public partial class WebConsoleControl : UserControl
{
    private readonly ObservableCollection<ConsoleLogItem> _allLogs = [];
    private readonly ObservableCollection<ConsoleLogItem> _filteredLogs = [];
    private string _currentFilter = "All";
    private string _searchQuery = string.Empty;

    public WebConsoleControl()
    {
        InitializeComponent();
        LogListBox.ItemsSource = _filteredLogs;
    }

    public void AddLog(ConsoleLogItem item)
    {
        _allLogs.Add(item);
        if (MatchesFilter(item))
        {
            _filteredLogs.Add(item);
            LogListBox.ScrollIntoView(item);
        }
        UpdateCount();
    }

    private bool MatchesFilter(ConsoleLogItem item)
    {
        if (_currentFilter == "Info" && item.Level != ConsoleLogLevel.Info) return false;
        if (_currentFilter == "Warn" && item.Level != ConsoleLogLevel.Warn) return false;
        if (_currentFilter == "Error" && item.Level != ConsoleLogLevel.Error) return false;

        if (!string.IsNullOrWhiteSpace(_searchQuery) &&
            !item.Message.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private void RefreshFilteredList()
    {
        _filteredLogs.Clear();
        foreach (var item in _allLogs.Where(MatchesFilter))
        {
            _filteredLogs.Add(item);
        }
        UpdateCount();
    }

    private void UpdateCount()
    {
        CountLabel.Text = $"{_filteredLogs.Count} item{(_filteredLogs.Count == 1 ? "" : "s")}";
    }

    private void OnFilterChanged(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb && rb.Tag is string filter)
        {
            _currentFilter = filter;
            RefreshFilteredList();
        }
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _searchQuery = SearchBox.Text;
        RefreshFilteredList();
    }

    private void OnClearClick(object sender, RoutedEventArgs e)
    {
        _allLogs.Clear();
        _filteredLogs.Clear();
        UpdateCount();
    }

    private void OnCopyClick(object sender, RoutedEventArgs e)
    {
        if (_filteredLogs.Count == 0) return;

        var sb = new StringBuilder();
        foreach (var item in _filteredLogs)
        {
            sb.AppendLine($"[{item.FormattedTime}] [{item.BadgeText}] {item.Message}");
        }

        Clipboard.SetText(sb.ToString());
    }
}

