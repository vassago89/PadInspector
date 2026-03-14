using System.Collections.Specialized;
using System.Windows;
using PadInspector.ViewModels;

namespace PadInspector;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Closed += (_, _) => viewModel.Dispose();
        StateChanged += OnStateChanged;

        // 로그 자동 스크롤
        ((INotifyCollectionChanged)viewModel.LogMessages).CollectionChanged += (_, _) =>
        {
            if (LogListBox.Items.Count > 0)
                LogListBox.ScrollIntoView(LogListBox.Items[^1]);
        };
    }

    private void OnMinimize(object sender, RoutedEventArgs e)
        => WindowState = WindowState.Minimized;

    private void OnMaximizeRestore(object sender, RoutedEventArgs e)
        => WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;

    private void OnClose(object sender, RoutedEventArgs e)
        => Close();

    private void OnStateChanged(object? sender, EventArgs e)
    {
        BtnMaxRestore.Content = WindowState == WindowState.Maximized
            ? "\uE923"
            : "\uE922";
    }
}
