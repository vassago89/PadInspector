using System.Collections.Specialized;
using System.Windows;
using System.Windows.Threading;
using PadInspector.ViewModels;

namespace PadInspector;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer _clock;
    private readonly NotifyCollectionChangedEventHandler _logScrollHandler;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        _logScrollHandler = (_, _) =>
        {
            if (LogListBox.Items.Count > 0)
                LogListBox.ScrollIntoView(LogListBox.Items[^1]);
        };

        ((INotifyCollectionChanged)viewModel.LogService.Messages).CollectionChanged += _logScrollHandler;

        Closed += (_, _) =>
        {
            _clock.Stop();
            ((INotifyCollectionChanged)viewModel.LogService.Messages).CollectionChanged -= _logScrollHandler;
            viewModel.Dispose();
        };

        _clock = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clock.Tick += (_, _) => ClockText.Text = DateTime.Now.ToString("yyyy-MM-dd  HH:mm:ss");
        _clock.Start();
        ClockText.Text = DateTime.Now.ToString("yyyy-MM-dd  HH:mm:ss");
    }

    private void OnKoreanClick(object sender, RoutedEventArgs e) => App.SetLanguage("ko");
    private void OnEnglishClick(object sender, RoutedEventArgs e) => App.SetLanguage("en");

    private void OnToggleThemeClick(object sender, RoutedEventArgs e)
    {
#pragma warning disable WPF0001
        ThemeMode = ThemeMode == ThemeMode.Dark ? ThemeMode.Light : ThemeMode.Dark;
#pragma warning restore WPF0001
    }
}
