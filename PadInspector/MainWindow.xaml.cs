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

        // 로그 자동 스크롤
        ((INotifyCollectionChanged)viewModel.LogService.Messages).CollectionChanged += (_, _) =>
        {
            if (LogListBox.Items.Count > 0)
                LogListBox.ScrollIntoView(LogListBox.Items[^1]);
        };
    }
}
