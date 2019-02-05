using EasyBackup.ViewModels;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace EasyBackup.Views
{
    /// <summary>
    /// Interaction logic for HomeScreen.xaml
    /// </summary>
    public partial class SetupBackup : UserControl
    {
        public SetupBackup()
        {
            InitializeComponent();
            DataContextChanged += SetupBackup_DataContextChanged;
        }

        private void SetupBackup_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (DataContext is SetupBackupViewModel)
            {
                (DataContext as SetupBackupViewModel).DialogCoordinator = DialogCoordinator.Instance;
            }
        }
    }
}
