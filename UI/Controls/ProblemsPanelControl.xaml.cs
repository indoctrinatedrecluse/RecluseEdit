using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Input;
using RecluseEdit.Core.Models;

namespace RecluseEdit.UI.Controls;

public partial class ProblemsPanelControl : UserControl
{
    private readonly ObservableCollection<DiagnosticItem> _problems = [];

    public event Action<DiagnosticItem>? ProblemNavigated;
    public event Action? RefreshRequested;

    public ProblemsPanelControl()
    {
        InitializeComponent();
        ProblemsDataGrid.ItemsSource = _problems;
    }

    public void SetProblems(IEnumerable<DiagnosticItem> items)
    {
        _problems.Clear();
        foreach (var item in items)
        {
            _problems.Add(item);
        }
        ProblemsCountBadge.Text = _problems.Count.ToString();
    }

    public void ClearProblems()
    {
        _problems.Clear();
        ProblemsCountBadge.Text = "0";
    }

    private void OnRowDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ProblemsDataGrid.SelectedItem is DiagnosticItem item)
        {
            ProblemNavigated?.Invoke(item);
        }
    }

    private void OnRefreshClick(object sender, System.Windows.RoutedEventArgs e)
    {
        RefreshRequested?.Invoke();
    }
}
