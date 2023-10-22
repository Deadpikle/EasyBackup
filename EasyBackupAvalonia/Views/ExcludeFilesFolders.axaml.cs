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
    /// Interaction logic for ExcludeFilesFolders.xaml
    /// </summary>
    public partial class ExcludeFilesFolders : UserControl
    {
        public ExcludeFilesFolders()
        {
            InitializeComponent();
            AddHandler(DragDrop.DropEvent, Drop);
        }

        private void Drop(object sender, DragEventArgs e)
        {
            if (e.Data.Contains(DataFormats.Files) && DataContext is ExcludeFilesFoldersViewModel effvm)
            {
                foreach (var fileName in e.Data.GetFiles())
                {
                    effvm.AddPath(fileName.Path.AbsolutePath);
                }
            }
        }
    }
}
