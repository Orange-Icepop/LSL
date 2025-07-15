using System;
using System.Collections.Specialized;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using LSL.Common.Collections;

namespace LSL.Components;

public class MyPlot : Control
{
    // properties
    // source
    public static readonly DirectProperty<MyPlot, RangedObservableLinkedList<double>> ItemsSourceProperty =
        AvaloniaProperty.RegisterDirect<MyPlot, RangedObservableLinkedList<double>>(
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
    private RangedObservableLinkedList<double> _itemsSource = new(30);
    public RangedObservableLinkedList<double> ItemsSource
    {
        get => _itemsSource;
        set
        {
            // 取消旧集合的订阅
            if (_isSubscribed && _itemsSource is not null)
            {
                _itemsSource.CollectionChanged -= OnCollectionChanged;
                _isSubscribed = false;
            }
            
            // 设置新集合并订阅
            SetAndRaise(ItemsSourceProperty, ref _itemsSource, value);
            
            if (value is not null)
            {
                value.CollectionChanged += OnCollectionChanged;
                _isSubscribed = true;
            }
            
            InvalidateVisual();
        }
    }

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
        Dispatcher.UIThread.Post(UpdatePoints);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ItemsSourceProperty)
        {
            if (change.OldValue is RangedObservableLinkedList<double> oc) oc.CollectionChanged -= OnCollectionChanged;
            if (change.NewValue is RangedObservableLinkedList<double> nc)
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

    private static double Correct(double value) => value <= 100 ? value : 100;

    // rendering
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (ItemsSource is null || ItemsSource.Count < 2 || 
            _controlSize.Width <= 0 || _controlSize.Height <= 0)
            return;
        var regionGeometry = new StreamGeometry();
        using (var regionContent = regionGeometry.Open())
        {
            regionContent.BeginFigure(new Point(0, _controlSize.Height), true);
            double count = ItemsSource.Count < 0 ? 0 : (double)ItemsSource.Count;
            int i = 0;
            foreach (var item in ItemsSource)
            {
                regionContent.LineTo(CalculatePoint(i, item));
                i++;
            }
            regionContent.LineTo(new Point(_controlSize.Width, _controlSize.Height));
            regionContent.EndFigure(true);
        }
        // line
        var lineGeometry = new StreamGeometry();
        using (var ctx = lineGeometry.Open())
        {
            ctx.BeginFigure(CalculatePoint(0, ItemsSource.FirstOrDefault()), false);
            int i = 1;
            foreach (var item in ItemsSource.Skip(1))
            {
                ctx.LineTo(CalculatePoint(i, item));
                i++;
            }
            ctx.EndFigure(false);
        }

        // 绘制填充、折线与边框
        context.DrawGeometry(null, new Pen(LineColor, 1), lineGeometry);
        context.DrawGeometry(FillColor, null, regionGeometry);
        context.DrawRectangle(null, new Pen(Brushes.DarkGray), new Rect(0, 0, _controlSize.Width, _controlSize.Height), 0, 0, BoxShadows.Parse("0 0 10 -2 LightGray"));
    }
    
    // size control
    protected override Size MeasureOverride(Size availableSize) => new Size(0, 0);
    
    private Size _controlSize;
    protected override Size ArrangeOverride(Size finalSize)
    {
        _controlSize = finalSize;
        return finalSize;
    }

    private Point CalculatePoint(int index, double value)
    {
        double x = _controlSize.Width * index / Math.Max(1, ItemsSource.Count - 1);
        double y = _controlSize.Height * (100 - Correct(value)) / 100.0;
        return new Point(x, y);
    }}