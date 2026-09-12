using System.Windows;
using System.Windows.Controls;

namespace RecluseEdit.UI.Views;

/// <summary>
/// Interaction logic for KeyboardShortcutsWindow.xaml
/// </summary>
public partial class KeyboardShortcutsWindow : Window
{
    public record ShortcutEntry(string Category, string Name, string Shortcut);

    private static readonly IReadOnlyList<ShortcutEntry> AllShortcuts =
    [
        // 🚀 Command Palette & Navigation
        new("Command Palette", "Open Command Palette (Commands)", "Ctrl+Shift+P / F1"),
        new("Command Palette", "Quick Open Workspace Files & Tabs", "Ctrl+P"),
        new("Command Palette", "Go to Line / Column", "Ctrl+G"),

        // 📄 File Operations
        new("File Operations", "New File", "Ctrl+N"),
        new("File Operations", "Open File...", "Ctrl+O"),
        new("File Operations", "Open Folder / Workspace...", "Ctrl+Shift+O"),
        new("File Operations", "Save Active File", "Ctrl+S"),
        new("File Operations", "Save As...", "Ctrl+Shift+S"),
        new("File Operations", "Close Active Tab", "Ctrl+W"),

        // ✏️ Core Editing & Line Operations
        new("Line & Text Operations", "Toggle Line Comment", "Ctrl+/"),
        new("Line & Text Operations", "Move Line(s) Up", "Alt+Up"),
        new("Line & Text Operations", "Move Line(s) Down", "Alt+Down"),
        new("Line & Text Operations", "Duplicate Line(s) Down", "Shift+Alt+Down"),
        new("Line & Text Operations", "Duplicate Line(s) Up", "Shift+Alt+Up"),
        new("Line & Text Operations", "Delete Entire Line(s)", "Ctrl+Shift+K"),
        new("Line & Text Operations", "Join Next Line", "Ctrl+J"),
        new("Line & Text Operations", "Transform to UPPERCASE", "Ctrl+Shift+U"),
        new("Line & Text Operations", "Transform to lowercase", "Ctrl+U"),
        new("Line & Text Operations", "Undo Last Change", "Ctrl+Z"),
        new("Line & Text Operations", "Redo Change", "Ctrl+Y"),
        new("Line & Text Operations", "Cut Line / Selection", "Ctrl+X"),
        new("Line & Text Operations", "Copy Line / Selection", "Ctrl+C"),
        new("Line & Text Operations", "Paste Clipboard Content", "Ctrl+V"),

        // 🔤 Multiline & Selection
        new("Multiline & Selection", "Rectangular / Column Selection (Mouse)", "Alt + Drag"),
        new("Multiline & Selection", "Add Cursor / Column Below", "Ctrl+Alt+Down"),
        new("Multiline & Selection", "Add Cursor / Column Above", "Ctrl+Alt+Up"),
        new("Multiline & Selection", "Select All Text", "Ctrl+A"),
        new("Multiline & Selection", "Indent Selection", "Tab"),
        new("Multiline & Selection", "Outdent Selection", "Shift+Tab"),

        // 🔍 Search & Replace
        new("Search & Replace", "Find in Active File", "Ctrl+F"),
        new("Search & Replace", "Replace in Active File", "Ctrl+H"),
        new("Search & Replace", "Find Next Occurrence", "Enter"),
        new("Search & Replace", "Find Previous Occurrence", "Shift+Enter"),

        // ⚡ IntelliSense & AI Assistant
        new("IntelliSense & AI", "Trigger IntelliSense Popup", "Ctrl+Space"),
        new("IntelliSense & AI", "Accept Ghost-Text Completion", "Tab"),
        new("IntelliSense & AI", "Dismiss Ghost-Text / Popup", "Esc"),
        new("IntelliSense & AI", "Toggle DeepSeek AI Chat Panel", "Ctrl+Alt+A"),
        new("IntelliSense & AI", "Toggle Integrated Terminal", "Ctrl+`"),

        // 🖥️ View & Display
        new("View & Workspace", "Toggle Workspace Explorer Sidebar", "Ctrl+B"),
        new("View & Workspace", "Zoom In / Out Editor Font", "Ctrl + MouseWheel"),
        new("View & Workspace", "Keyboard Shortcuts Reference", "Ctrl+K, Ctrl+S")
    ];

    public KeyboardShortcutsWindow()
    {
        InitializeComponent();
        ItemsShortcuts.ItemsSource = AllShortcuts;
    }

    private void OnFilterTextChanged(object sender, TextChangedEventArgs e)
    {
        var filter = TxtFilter.Text.Trim();
        TxtPlaceholder.Visibility = string.IsNullOrEmpty(filter) ? Visibility.Visible : Visibility.Collapsed;

        if (string.IsNullOrEmpty(filter))
        {
            ItemsShortcuts.ItemsSource = AllShortcuts;
            return;
        }

        var terms = filter.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        ItemsShortcuts.ItemsSource = AllShortcuts.Where(s =>
            terms.All(term =>
                s.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                s.Category.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                s.Shortcut.Contains(term, StringComparison.OrdinalIgnoreCase))).ToList();
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

