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

        ((INotifyCollectionChanged)viewModel.LogService.Messages).CollectionChanged += (_, _) =>
        {
            if (LogListBox.Items.Count > 0)
                LogListBox.ScrollIntoView(LogListBox.Items[^1]);
        };
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
