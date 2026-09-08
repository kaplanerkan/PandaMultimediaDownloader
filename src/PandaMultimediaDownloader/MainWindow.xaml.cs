using System.Windows;
using System.Windows.Controls;
using PandaMultimediaDownloader.ViewModels;

namespace PandaMultimediaDownloader;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }

    private void LogBox_TextChanged(object sender, TextChangedEventArgs e) => LogBox.ScrollToEnd();
}
