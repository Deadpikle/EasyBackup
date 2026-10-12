using EasyBackup.ViewModels;
using MahApps.Metro.Controls.Dialogs;
using System.Windows;
using System.Windows.Controls;

namespace EasyBackup.Views
{
    /// <summary>
    /// Interaction logic for ExcludeFilesFolders.xaml
    /// </summary>
    public partial class ExcludeFilesFolders : UserControl
    {
        public ExcludeFilesFolders()
        {
            InitializeComponent();
            DataContextChanged += ExcludeFilesFolders_DataContextChanged;
        }

        private void ExcludeFilesFolders_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (DataContext is ExcludeFilesFoldersViewModel)
            {
                (DataContext as ExcludeFilesFoldersViewModel).DialogCoordinator = DialogCoordinator.Instance;
            }
        }
    }
}
