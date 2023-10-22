using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using EasyBackupAvalonia.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBackupAvalonia.Views
{
    /// <summary>
    /// Interaction logic for HomeScreen.xaml
    /// </summary>
    public partial class SetupBackup : UserControl
    {
        public SetupBackup()
        {
            InitializeComponent();
            AddHandler(DragDrop.DropEvent, Drop);
        }

        private void Drop(object sender, DragEventArgs e)
        {
            if (e.Data.Contains(DataFormats.Files) && DataContext is SetupBackupViewModel sbvm)
            {
                foreach (var fileName in e.Data.GetFiles())
                {
                    sbvm.AddPath(fileName.Path.LocalPath);
                }
            }
        }
    }
}
