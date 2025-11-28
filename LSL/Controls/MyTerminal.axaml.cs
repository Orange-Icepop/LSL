using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using LSL.Models;

namespace LSL.Controls;

public partial class MyTerminal : UserControl
{
    public MyTerminal()
    {
        InitializeComponent();
        TerminalOutput.ItemsSource = ItemsSource;
        TerminalScroll.ScrollChanged += OnScrollChanged;
    }

    public static readonly DirectProperty<MyTerminal, ObservableCollection<ColoredLine>?> ItemsSourceProperty =
        AvaloniaProperty.RegisterDirect<MyTerminal, ObservableCollection<ColoredLine>?>(
            nameof(ItemsSource),
            o => o.ItemsSource,
            (o, v) => o.ItemsSource = v);

    public static readonly StyledProperty<bool> EnableAutoScrollProperty =
        AvaloniaProperty.Register<MyTerminal, bool>(nameof(EnableAutoScroll), true);

    private ObservableCollection<ColoredLine>? _itemsSource;

    public ObservableCollection<ColoredLine>? ItemsSource
    {
        get => _itemsSource;
        set
        {
            if (_itemsSource != null) _itemsSource.CollectionChanged -= OnCollectionChanged;
            if (value != null) value.CollectionChanged += OnCollectionChanged;
            SetAndRaise(ItemsSourceProperty, ref _itemsSource, value);
        }
    }

    public bool EnableAutoScroll
    {
        get => GetValue(EnableAutoScrollProperty);
        set
        {
            OnEnableAutoScrollChanged(value);
            SetValue(EnableAutoScrollProperty, value);
        }
    }

    private bool _isUserScrolling;

    private void ScrollToBottom() => TerminalScroll.ScrollToEnd();

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (EnableAutoScroll && !_isUserScrolling)
        {
            Dispatcher.UIThread.Post(ScrollToBottom, DispatcherPriority.Background);
        }
    }

    private void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        var distanceFromBottom = TerminalScroll.Extent.Height -
                                 (TerminalScroll.Offset.Y + TerminalScroll.Viewport.Height);
        _isUserScrolling = distanceFromBottom > 10;
    }

    private void OnEnableAutoScrollChanged(bool enable)
    {
        if (enable && !_isUserScrolling)
        {
            ScrollToBottom();
        }
    }


    public void ForceScrollToBottom()
    {
        _isUserScrolling = false;
        ScrollToBottom();
    }
}