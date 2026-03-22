using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace PadInspector.Views;

public partial class ZoomPanImage : UserControl
{
    private Point _lastMousePos;
    private bool _isPanning;

    public static readonly DependencyProperty SourceProperty =
        DependencyProperty.Register(nameof(Source), typeof(BitmapSource), typeof(ZoomPanImage),
            new PropertyMetadata(null, (d, e) => ((ZoomPanImage)d).Img.Source = (BitmapSource?)e.NewValue));

    public BitmapSource? Source
    {
        get => (BitmapSource?)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public ZoomPanImage()
    {
        InitializeComponent();
        MouseWheel += OnMouseWheel;
        MouseLeftButtonDown += OnMouseDown;
        MouseLeftButtonUp += OnMouseUp;
        MouseMove += OnMouseMove;
        MouseDoubleClick += OnDoubleClick;
    }

    private void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        var pos = e.GetPosition(Img);
        double factor = e.Delta > 0 ? 1.2 : 1 / 1.2;

        double newScale = ScaleT.ScaleX * factor;
        if (newScale < 0.5) newScale = 0.5;
        if (newScale > 20) newScale = 20;

        double dx = pos.X * (ScaleT.ScaleX - newScale);
        double dy = pos.Y * (ScaleT.ScaleY - newScale);

        ScaleT.ScaleX = newScale;
        ScaleT.ScaleY = newScale;
        TranslateT.X += dx;
        TranslateT.Y += dy;
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        _isPanning = true;
        _lastMousePos = e.GetPosition(this);
        CaptureMouse();
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        _isPanning = false;
        ReleaseMouseCapture();
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isPanning) return;
        var pos = e.GetPosition(this);
        TranslateT.X += pos.X - _lastMousePos.X;
        TranslateT.Y += pos.Y - _lastMousePos.Y;
        _lastMousePos = pos;
    }

    private void OnDoubleClick(object sender, MouseButtonEventArgs e)
    {
        ScaleT.ScaleX = 1;
        ScaleT.ScaleY = 1;
        TranslateT.X = 0;
        TranslateT.Y = 0;
    }
}
