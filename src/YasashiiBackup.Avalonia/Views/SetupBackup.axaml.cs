using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using YasashiiBackupAvalonia.ViewModels;
using System;
using System.Linq;

namespace YasashiiBackupAvalonia.Views
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
            if (e.DataTransfer.Contains(DataFormat.File) && DataContext is SetupBackupViewModel sbvm)
            {
                foreach (var fileName in e.DataTransfer.Items)
                {
                    var file = fileName.TryGetFile();
                    if (file != null)
                    {
                        sbvm.AddPath(file.Path.LocalPath);
                    }
                }
            }
        }
    }
}
