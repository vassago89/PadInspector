using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace PadInspector.Views;

public partial class YieldChart : UserControl
{
    public static readonly DependencyProperty DataProperty =
        DependencyProperty.Register(nameof(Data), typeof(ObservableCollection<double>), typeof(YieldChart),
            new PropertyMetadata(null, OnDataChanged));

    public ObservableCollection<double>? Data
    {
        get => (ObservableCollection<double>?)GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

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
        chart.Redraw();
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => Redraw();
    private void OnSizeChanged(object sender, SizeChangedEventArgs e) => Redraw();

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

        // 기준선 (목표 수율 등)
        var gridBrush = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
        for (int pct = 0; pct <= 100; pct += 25)
        {
            double y = margin + chartH * (1 - pct / 100.0);
            var line = new Line
            {
                X1 = margin, X2 = margin + chartW,
                Y1 = y, Y2 = y,
                Stroke = gridBrush, StrokeThickness = 0.5
            };
            ChartCanvas.Children.Add(line);

            if (pct % 50 == 0)
            {
                var txt = new TextBlock
                {
                    Text = $"{pct}%", FontSize = 9,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x6C, 0x70, 0x86))
                };
                Canvas.SetLeft(txt, margin);
                Canvas.SetTop(txt, y - 7);
                ChartCanvas.Children.Add(txt);
            }
        }

        // 데이터 라인
        int count = data.Count;
        double step = chartW / Math.Max(count - 1, 1);

        var polyline = new Polyline
        {
            Stroke = new SolidColorBrush(Color.FromRgb(0x89, 0xB4, 0xFA)),
            StrokeThickness = 1.5,
            StrokeLineJoin = PenLineJoin.Round
        };

        for (int i = 0; i < count; i++)
        {
            double x = margin + i * step;
            double y = margin + chartH * (1 - Math.Clamp(data[i], 0, 100) / 100.0);
            polyline.Points.Add(new Point(x, y));
        }

        ChartCanvas.Children.Add(polyline);

        // 마지막 값 표시
        if (count > 0)
        {
            var lastVal = data[^1];
            var brush = lastVal >= 90
                ? new SolidColorBrush(Colors.LimeGreen)
                : lastVal >= 70
                    ? new SolidColorBrush(Color.FromRgb(0xFA, 0xB3, 0x87))
                    : new SolidColorBrush(Colors.Red);

            var txt = new TextBlock
            {
                Text = $"{lastVal:F1}%", FontSize = 10,
                FontWeight = FontWeights.Bold, Foreground = brush
            };
            Canvas.SetRight(txt, margin);
            Canvas.SetTop(txt, margin);
            ChartCanvas.Children.Add(txt);
        }
    }
}
