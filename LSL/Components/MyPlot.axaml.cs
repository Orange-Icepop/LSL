using System;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using LSL.Common.Contracts;

namespace LSL.Components;

public class MyPlot : Control
{
    // properties
    // source
    public static readonly DirectProperty<MyPlot, RangedObservableCollection<uint>> ItemsSourceProperty =
        AvaloniaProperty.RegisterDirect<MyPlot, RangedObservableCollection<uint>>(
            nameof(ItemsSource),
            obj => obj.ItemsSource,
            (obj, val) => obj.ItemsSource = val);
    // fill color
    public static readonly StyledProperty<IBrush> FillColorProperty =
        AvaloniaProperty.Register<MyPlot, IBrush>(nameof(FillColor), new SolidColorBrush(Color.FromArgb(127, 0, 0, 255)));
    // line color
    public static readonly StyledProperty<IBrush> LineColorProperty =
        AvaloniaProperty.Register<MyPlot, IBrush>(nameof(LineColor), Brushes.Blue);

    private bool _isSubscribed;
    private RangedObservableCollection<uint> _itemsSource = new(30);
    public RangedObservableCollection<uint> ItemsSource
    {
        get => _itemsSource;
        set
        {
            // 取消旧集合的订阅
            if (_isSubscribed && _itemsSource != null)
            {
                _itemsSource.CollectionChanged -= OnCollectionChanged;
                _isSubscribed = false;
            }
            
            // 设置新集合并订阅
            SetAndRaise(ItemsSourceProperty, ref _itemsSource, value);
            
            if (value != null)
            {
                value.CollectionChanged += OnCollectionChanged;
                _isSubscribed = true;
            }
            
            InvalidateVisual();
        }    }

    public IBrush FillColor
    {
        get => GetValue(FillColorProperty);
        set => SetValue(FillColorProperty, value);
    }
    public IBrush LineColor
    {
        get => GetValue(LineColorProperty);
        set => SetValue(LineColorProperty, value);
    }

    // logics
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (ItemsSource is not null)
        {
            ItemsSource.CollectionChanged += OnCollectionChanged;
        }
        InvalidateVisual();
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdatePoints();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ItemsSourceProperty)
        {
            if (change.OldValue is RangedObservableCollection<uint> oc) oc.CollectionChanged -= OnCollectionChanged;
            if (change.NewValue is RangedObservableCollection<uint> nc)
            {
                nc.CollectionChanged += OnCollectionChanged;
                InvalidateVisual();
            }
        }
        else if (change.Property == FillColorProperty) InvalidateVisual();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (ItemsSource is not null)
        {
            ItemsSource.CollectionChanged -= OnCollectionChanged;
        }
    }

    private void UpdatePoints()
    {
        InvalidateVisual();
    }

    private uint Correct(uint value) => value <= 100 ? value : 100;

    // rendering
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (ItemsSource is null || ItemsSource.Count < 2 || 
            _controlSize.Width <= 0 || _controlSize.Height <= 0)
            return;        // region
        var regionGeometry = new StreamGeometry();
        using (var regionContent = regionGeometry.Open())
        {
            regionContent.BeginFigure(new Point(0, _controlSize.Height), true);
            uint count = ItemsSource.Count < 0 ? 0 : (uint)ItemsSource.Count;
            for (int i = 0; i < count; i++)
            {
                regionContent.LineTo(CalculatePoint(i, ItemsSource[i]));
            }
            regionContent.LineTo(new Point(_controlSize.Width, _controlSize.Height));
            regionContent.EndFigure(true);
        }
        // line
        var lineGeometry = new StreamGeometry();
        using (var ctx = lineGeometry.Open())
        {
            ctx.BeginFigure(CalculatePoint(0, ItemsSource[0]), false);
            for (int i = 1; i < ItemsSource.Count; i++)
            {
                ctx.LineTo(CalculatePoint(i, ItemsSource[i]));
            }
            ctx.EndFigure(false);
        }

        // 绘制填充和折线
        context.DrawGeometry(null, new Pen(LineColor, 1), lineGeometry);
        context.DrawGeometry(FillColor, null, regionGeometry);
    }
    
    // size control
    protected override Size MeasureOverride(Size availableSize) => new Size(0, 0);
    
    private Size _controlSize;
    protected override Size ArrangeOverride(Size finalSize)
    {
        _controlSize = finalSize;
        return finalSize;
    }

    private Point CalculatePoint(int index, uint value)
    {
        double x = _controlSize.Width * index / Math.Max(1, ItemsSource.Count - 1);
        double y = _controlSize.Height * (100 - Correct(value)) / 100.0;
        return new Point(x, y);
    }}