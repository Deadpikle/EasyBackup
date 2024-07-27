using Avalonia.Controls;
using EasyBackupAvalonia.ViewModels;

namespace EasyBackupAvalonia;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel(this);
    }
}