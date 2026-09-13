using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using RecluseEdit.Core.Services;
using RecluseEdit.Sdk.Providers;

namespace RecluseEdit.UI.Dialogs;

public partial class ThemePickerDialog : Window
{
    private readonly ThemeManager _themeManager;
    private readonly IReadOnlyList<IThemeDefinition> _allThemes;
    private bool _confirmed = false;

    public IThemeDefinition? SelectedTheme => ThemesListBox.SelectedItem as IThemeDefinition;

    public ThemePickerDialog(ThemeManager themeManager)
    {
        InitializeComponent();
        _themeManager = themeManager;
        _allThemes = _themeManager.RegisteredThemes;

        FilterThemes();

        // Select currently active theme
        var current = _allThemes.FirstOrDefault(t => t.Id == _themeManager.ActiveTheme.Id);
        if (current != null)
        {
            ThemesListBox.SelectedItem = current;
            ThemesListBox.ScrollIntoView(current);
        }

        Loaded += (_, _) => SearchBox.Focus();
        Closing += (_, _) =>
        {
            if (!_confirmed)
            {
                _themeManager.RollbackPreview();
            }
        };
    }

    private void FilterThemes()
    {
        var query = SearchBox?.Text?.Trim() ?? string.Empty;
        TxtSearchPlaceholder.Visibility = string.IsNullOrEmpty(query) ? Visibility.Visible : Visibility.Collapsed;

        var filtered = _allThemes.Where(t =>
            string.IsNullOrEmpty(query) ||
            t.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            t.Type.ToString().Contains(query, StringComparison.OrdinalIgnoreCase) ||
            (t.Description != null && t.Description.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
            (t.Author != null && t.Author.Contains(query, StringComparison.OrdinalIgnoreCase))
        ).ToList();

        ThemesListBox.ItemsSource = filtered;

        if (filtered.Count > 0 && ThemesListBox.SelectedItem == null)
        {
            ThemesListBox.SelectedIndex = 0;
        }
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        FilterThemes();
    }

    private void OnThemesSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ThemesListBox.SelectedItem is IThemeDefinition selected)
        {
            _themeManager.PreviewTheme(selected);
        }
    }

    private void OnThemesMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        ConfirmSelection();
    }

    private void OnApplyClick(object sender, RoutedEventArgs e)
    {
        ConfirmSelection();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        _confirmed = false;
        _themeManager.RollbackPreview();
        DialogResult = false;
        Close();
    }

    private void ConfirmSelection()
    {
        if (ThemesListBox.SelectedItem is IThemeDefinition selected)
        {
            _confirmed = true;
            _themeManager.ApplyTheme(selected, persist: true);
            DialogResult = true;
            Close();
        }
    }

    private void OnWindowKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            OnCancelClick(this, new RoutedEventArgs());
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter)
        {
            ConfirmSelection();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Down)
        {
            if (ThemesListBox.SelectedIndex < ThemesListBox.Items.Count - 1)
            {
                ThemesListBox.SelectedIndex++;
                ThemesListBox.ScrollIntoView(ThemesListBox.SelectedItem);
            }
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Up)
        {
            if (ThemesListBox.SelectedIndex > 0)
            {
                ThemesListBox.SelectedIndex--;
                ThemesListBox.ScrollIntoView(ThemesListBox.SelectedItem);
            }
            e.Handled = true;
            return;
        }
    }
}
