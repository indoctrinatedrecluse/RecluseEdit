using System;
using System.Collections.Generic;
using System.Linq;
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
        new("Command Palette", "Go to Symbol in File...", "Ctrl+Shift+O / Ctrl+T"),
        new("Command Palette", "Go to Line / Column", "Ctrl+G"),
        new("Command Palette", "Focus Explorer Activity Bar", "Ctrl+Shift+E"),
        new("Command Palette", "Focus Source Control Activity Bar", "Ctrl+Shift+G"),

        // 📄 File Operations
        new("File Operations", "New File", "Ctrl+N"),
        new("File Operations", "New Project (Scaffolding Wizard)", "Ctrl+Shift+N"),
        new("File Operations", "Open File...", "Ctrl+O"),
        new("File Operations", "Open Folder / Workspace...", "Ctrl+K, Ctrl+O"),
        new("File Operations", "Save Active File", "Ctrl+S"),
        new("File Operations", "Save As...", "Ctrl+Shift+S"),
        new("File Operations", "Close Active Tab", "Ctrl+W"),

        // ✏️ Core Editing & Line Operations
        new("Line & Text Operations", "Format Document (Prettify)", "Shift+Alt+F"),
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
        new("Multiline & Selection", "Add Next Occurrence (Multi-Caret)", "Ctrl+D"),
        new("Multiline & Selection", "Select All Occurrences", "Ctrl+Shift+L"),
        new("Multiline & Selection", "Expand Semantic Selection", "Shift+Alt+Right"),
        new("Multiline & Selection", "Shrink Semantic Selection", "Shift+Alt+Left"),
        new("Multiline & Selection", "Rectangular / Column Selection (Mouse)", "Alt + Drag"),
        new("Multiline & Selection", "Add Cursor / Column Below", "Ctrl+Alt+Down"),
        new("Multiline & Selection", "Add Cursor / Column Above", "Ctrl+Alt+Up"),
        new("Multiline & Selection", "Select All Text", "Ctrl+A"),
        new("Multiline & Selection", "Indent Selection", "Tab"),
        new("Multiline & Selection", "Outdent Selection", "Shift+Tab"),

        // 📁 Code Folding
        new("Code Folding", "Fold All Blocks", "Ctrl+K, Ctrl+0"),
        new("Code Folding", "Unfold All Blocks", "Ctrl+K, Ctrl+J"),
        new("Code Folding", "Toggle Fold at Caret", "Ctrl+M"),
        new("Code Folding", "Fold Current Block", "Ctrl+Shift+["),
        new("Code Folding", "Unfold Current Block", "Ctrl+Shift+]"),

        // 🔍 Search & Replace
        new("Search & Replace", "Find in Active File", "Ctrl+F"),
        new("Search & Replace", "Replace in Active File", "Ctrl+H"),
        new("Search & Replace", "Find Next Occurrence", "Enter"),
        new("Search & Replace", "Find Previous Occurrence", "Shift+Enter"),

        // ⚡ IntelliSense & AI Assistant
        new("IntelliSense & AI", "AI Code Review (Audit)", "Ctrl+Shift+R"),
        new("IntelliSense & AI", "AI Inline Prompt & Generation", "Ctrl+I"),
        new("IntelliSense & AI", "Trigger IntelliSense Popup", "Ctrl+Space"),
        new("IntelliSense & AI", "Accept Ghost-Text Completion", "Tab"),
        new("IntelliSense & AI", "Dismiss Ghost-Text / Popup", "Esc"),
        new("IntelliSense & AI", "Toggle AI Chat Panel (DeepSeek, Gemini, OpenAI, Claude, Ollama)", "Ctrl+Alt+A"),
        new("IntelliSense & AI", "Toggle Integrated Terminal", "Ctrl+`"),

        // 🖥️ View & Display
        new("View & Workspace", "Toggle Zen Mode (Distraction-Free)", "Ctrl+K, Z"),
        new("View & Workspace", "Toggle Live Chromium & Markdown Preview", "Ctrl+Shift+V"),
        new("View & Workspace", "Color Theme Picker (Live Preview)", "Ctrl+K, Ctrl+T"),
        new("View & Workspace", "Quick Color Theme Selector", "Ctrl+Alt+T"),
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
