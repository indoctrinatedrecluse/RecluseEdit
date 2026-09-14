using System;
using System.Collections.Generic;

namespace RecluseEdit.Sdk.Providers;

/// <summary>
/// Specifies the positioning alignment of a status bar contribution.
/// </summary>
public enum StatusBarAlignment
{
    /// <summary>
    /// Item is anchored to the left side of the status bar (e.g. next to status messages, branch indicators).
    /// </summary>
    Left,

    /// <summary>
    /// Item is anchored to the right side of the status bar (e.g. next to line/column info, encoding, language).
    /// </summary>
    Right
}

/// <summary>
/// Represents an interactive item displayed in the RecluseEdit status bar.
/// </summary>
public interface IStatusBarItem
{
    /// <summary>
    /// Unique identifier for this status bar item.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Display text shown in the status bar (may include Unicode icons/emojis).
    /// </summary>
    string Text { get; set; }

    /// <summary>
    /// Optional tooltip shown when hovering over the item.
    /// </summary>
    string? Tooltip { get; set; }

    /// <summary>
    /// Optional icon or symbol key.
    /// </summary>
    string? Icon { get; set; }

    /// <summary>
    /// Left or Right alignment inside the status bar.
    /// </summary>
    StatusBarAlignment Alignment { get; set; }

    /// <summary>
    /// Sort priority order (higher priority items appear closer to outer edges).
    /// </summary>
    int Priority { get; set; }

    /// <summary>
    /// Whether the item is currently visible in the status bar.
    /// </summary>
    bool IsVisible { get; set; }

    /// <summary>
    /// Optional callback invoked when the user clicks this status bar item.
    /// </summary>
    Action? OnClick { get; set; }

    /// <summary>
    /// Occurs when any visual property of this item changes.
    /// </summary>
    event EventHandler? Changed;
}

/// <summary>
/// Pluggable provider for supplying one or more status bar items.
/// </summary>
public interface IStatusBarProvider
{
    /// <summary>
    /// Unique identifier of the status bar provider.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets all status bar items contributed by this provider.
    /// </summary>
    IReadOnlyList<IStatusBarItem> GetItems();

    /// <summary>
    /// Occurs when the collection of items changes.
    /// </summary>
    event EventHandler? ItemsChanged;
}

/// <summary>
/// Standard concrete implementation of <see cref="IStatusBarItem"/> with reactive change notifications.
/// </summary>
public class StatusBarItem : IStatusBarItem
{
    private string _text;
    private string? _tooltip;
    private string? _icon;
    private StatusBarAlignment _alignment;
    private int _priority;
    private bool _isVisible = true;
    private Action? _onClick;

    public string Id { get; }

    public string Text
    {
        get => _text;
        set
        {
            if (_text != value)
            {
                _text = value;
                NotifyChanged();
            }
        }
    }

    public string? Tooltip
    {
        get => _tooltip;
        set
        {
            if (_tooltip != value)
            {
                _tooltip = value;
                NotifyChanged();
            }
        }
    }

    public string? Icon
    {
        get => _icon;
        set
        {
            if (_icon != value)
            {
                _icon = value;
                NotifyChanged();
            }
        }
    }

    public StatusBarAlignment Alignment
    {
        get => _alignment;
        set
        {
            if (_alignment != value)
            {
                _alignment = value;
                NotifyChanged();
            }
        }
    }

    public int Priority
    {
        get => _priority;
        set
        {
            if (_priority != value)
            {
                _priority = value;
                NotifyChanged();
            }
        }
    }

    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (_isVisible != value)
            {
                _isVisible = value;
                NotifyChanged();
            }
        }
    }

    public Action? OnClick
    {
        get => _onClick;
        set
        {
            if (_onClick != value)
            {
                _onClick = value;
                NotifyChanged();
            }
        }
    }

    public event EventHandler? Changed;

    public StatusBarItem(string id, string text, StatusBarAlignment alignment = StatusBarAlignment.Right, int priority = 0)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        _text = text ?? string.Empty;
        _alignment = alignment;
        _priority = priority;
    }

    protected virtual void NotifyChanged()
    {
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
