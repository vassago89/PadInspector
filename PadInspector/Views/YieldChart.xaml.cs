using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace PadInspector.Views;

public partial class YieldChart : UserControl
{
    private static readonly Brush GridBrush = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
    private static readonly Brush LabelBrush = new SolidColorBrush(Color.FromRgb(0x6C, 0x70, 0x86));
    private static readonly Brush LineBrush = new SolidColorBrush(Color.FromRgb(0x89, 0xB4, 0xFA));
    private static readonly Brush GoodBrush = new SolidColorBrush(Colors.LimeGreen);
    private static readonly Brush WarnBrush = new SolidColorBrush(Color.FromRgb(0xFA, 0xB3, 0x87));
    private static readonly Brush BadBrush = new SolidColorBrush(Colors.Red);
    private static readonly Pen GridPen;
    private static readonly Pen LinePen;

    static YieldChart()
    {
        GridBrush.Freeze();
        LabelBrush.Freeze();
        LineBrush.Freeze();
        GoodBrush.Freeze();
        WarnBrush.Freeze();
        BadBrush.Freeze();
        GridPen = new Pen(GridBrush, 0.5);
        GridPen.Freeze();
        LinePen = new Pen(LineBrush, 1.5) { LineJoin = PenLineJoin.Round };
        LinePen.Freeze();
    }

    public static readonly DependencyProperty DataProperty =
        DependencyProperty.Register(nameof(Data), typeof(ObservableCollection<double>), typeof(YieldChart),
            new PropertyMetadata(null, OnDataChanged));

    public ObservableCollection<double>? Data
    {
        get => (ObservableCollection<double>?)GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    private bool _redrawPending;

    public YieldChart()
    {
        InitializeComponent();
    }

    private static void OnDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var chart = (YieldChart)d;
        if (e.OldValue is INotifyCollectionChanged old)
            old.CollectionChanged -= chart.OnCollectionChanged;
        if (e.NewValue is INotifyCollectionChanged @new)
            @new.CollectionChanged += chart.OnCollectionChanged;
        chart.RequestRedraw();
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => RequestRedraw();
    private void OnSizeChanged(object sender, SizeChangedEventArgs e) => RequestRedraw();

    private void RequestRedraw()
    {
        if (_redrawPending) return;
        _redrawPending = true;
        Dispatcher.InvokeAsync(() =>
        {
            _redrawPending = false;
            Redraw();
        }, System.Windows.Threading.DispatcherPriority.Render);
    }

    private void Redraw()
    {
        ChartCanvas.Children.Clear();
        var data = Data;
        if (data == null || data.Count < 2) return;

        double w = ChartCanvas.ActualWidth;
        double h = ChartCanvas.ActualHeight;
        if (w <= 0 || h <= 0) return;

        double margin = 4;
        double chartW = w - margin * 2;
        double chartH = h - margin * 2;

        // Grid lines + labels via single DrawingVisual (batch rendering)
        var gridVisual = new DrawingVisual();
        using (var dc = gridVisual.RenderOpen())
        {
            for (int pct = 0; pct <= 100; pct += 25)
            {
                double y = margin + chartH * (1 - pct / 100.0);
                dc.DrawLine(GridPen, new Point(margin, y), new Point(margin + chartW, y));

                if (pct % 50 == 0)
                {
                    var text = new FormattedText($"{pct}%",
                        System.Globalization.CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        new Typeface("Segoe UI"), 9, LabelBrush,
                        VisualTreeHelper.GetDpi(this).PixelsPerDip);
                    dc.DrawText(text, new Point(margin, y - 7));
                }
            }

            // Data line via StreamGeometry (allocation-efficient)
            int count = data.Count;
            double step = chartW / Math.Max(count - 1, 1);

            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open())
            {
                double x0 = margin;
                double y0 = margin + chartH * (1 - Math.Clamp(data[0], 0, 100) / 100.0);
                ctx.BeginFigure(new Point(x0, y0), false, false);

                for (int i = 1; i < count; i++)
                {
                    double x = margin + i * step;
                    double y = margin + chartH * (1 - Math.Clamp(data[i], 0, 100) / 100.0);
                    ctx.LineTo(new Point(x, y), true, true);
                }
            }
            geometry.Freeze();
            dc.DrawGeometry(null, LinePen, geometry);
        }

        var hostCanvas = new VisualHost(gridVisual);
        ChartCanvas.Children.Add(hostCanvas);

        // Last value label
        var lastVal = data[^1];
        var brush = lastVal >= 90 ? GoodBrush : lastVal >= 70 ? WarnBrush : BadBrush;
        var valueTxt = new TextBlock
        {
            Text = $"{lastVal:F1}%", FontSize = 10,
            FontWeight = FontWeights.Bold, Foreground = brush
        };
        Canvas.SetRight(valueTxt, margin);
        Canvas.SetTop(valueTxt, margin);
        ChartCanvas.Children.Add(valueTxt);
    }

    /// <summary>
    /// DrawingVisual을 Canvas에 호스팅하기 위한 FrameworkElement
    /// </summary>
    private class VisualHost : FrameworkElement
    {
        private readonly DrawingVisual _visual;

        public VisualHost(DrawingVisual visual)
        {
            _visual = visual;
            AddVisualChild(visual);
        }

        protected override int VisualChildrenCount => 1;
        protected override Visual GetVisualChild(int index) => _visual;
    }
}
