using Avalonia.Controls;
using YasashiiBackupAvalonia.ViewModels;

namespace YasashiiBackupAvalonia;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel(this);
    }
}