using EasyBackupAvalonia.Helpers;
using EasyBackupAvalonia.Interfaces;
using EasyBackupAvalonia.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace EasyBackupAvalonia.ViewModels
{
    class ExcludeFilesFoldersViewModel : BaseViewModel/*, IDropTarget*/
    {
        private FolderFileItem _itemBeingEdited;
        private ObservableCollection<FolderFileItem> _items;

        public ExcludeFilesFoldersViewModel(IChangeViewModel viewModelChanger, FolderFileItem itemBeingEdited) : base(viewModelChanger)
        {
            _itemBeingEdited = itemBeingEdited;
            _items = new ObservableCollection<FolderFileItem>();
            foreach (var path in itemBeingEdited.ExcludedPaths)
            {
                Items.Add(new FolderFileItem()
                {
                    Path = path,
                    IsDirectory = Directory.Exists(path)
                });
            }
        }

        public ObservableCollection<FolderFileItem> Items
        {
            get { return _items; }
            set { _items = value; NotifyPropertyChanged(); }
        }

        public string DirectoryPath
        {
            get { return _itemBeingEdited.Path; }
        }

        private void CancelChangeAndPopView()
        {
            PopViewModel();
        }

        private void SaveExclusionsAndPopView()
        {
            _itemBeingEdited.ExcludedPaths = new List<string>();
            foreach (var folderFileItem in _items)
            {
                _itemBeingEdited.ExcludedPaths.Add(folderFileItem.Path);
            }
            PopViewModel();
        }

        private void ChooseFolder()
        {
            //var dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
            //dialog.ShowNewFolderButton = true;
            //dialog.SelectedPath = DirectoryPath + "\\";
            //if (dialog.ShowDialog(Application.Current.MainWindow).GetValueOrDefault())
            //{
            //    AddPath(dialog.SelectedPath);
            //}
        }

        private void ChooseFile()
        {
            //var dialog = new Ookii.Dialogs.Wpf.VistaOpenFileDialog();
            //dialog.Reset();
            //dialog.Multiselect = true;
            //dialog.ShowReadOnly = true;
            //dialog.Title = "Choose a file";
            //dialog.RestoreDirectory = false;
            //dialog.FileName = DirectoryPath + "\\";
            //if (dialog.ShowDialog(Application.Current.MainWindow).GetValueOrDefault())
            //{
            //    foreach (string fileName in dialog.FileNames)
            //    {
            //        AddPath(fileName);
            //    }
            //}
        }

        private void AddPath(string path)
        {
            var isDirectory = Directory.Exists(path);
            // if we don't already have this path, add the path
            var excluded = new char[] { '/', '/' };
            if (Items.Where(x => x.Path == path).Count() == 0 && path.Trim(excluded).Contains(DirectoryPath.Trim(excluded)))
            {
                Items.Add(new FolderFileItem() { Path = path, IsDirectory = isDirectory, IsRecursive = false });
            }
        }

        private async void CheckAndRemoveAllItems()
        {
            //var result = await DialogCoordinator.ShowMessageAsync(this, "Warning!", "Are you sure you want to remove all items?",
            //    MessageDialogStyle.AffirmativeAndNegative,
            //    new MetroDialogSettings()
            //    {
            //        AffirmativeButtonText = "Yes",
            //        NegativeButtonText = "No",
            //        ColorScheme = MetroDialogColorScheme.Theme
            //    }
            //);
            //if (result == MessageDialogResult.Affirmative)
            //{
            //    Items.Clear();
            //}
        }

        private void RemoveItemFromList(object items)
        {
            if (items != null)
            {
                System.Collections.IList list = (System.Collections.IList)items;
                var selection = list?.Cast<FolderFileItem>();
                for (int i = 0; i < selection.Count(); i++)
                {
                    Items.Remove(selection.ElementAt(i));
                    i--; // have to do this as selection array is modified when we do the remove O_o
                }
            }
        }

        #region IDropTarget

        //public void DragOver(IDropInfo dropInfo)
        //{
        //    if (dropInfo.Data is DataObject && (dropInfo.Data as DataObject).GetFileDropList().Count > 0)
        //    {
        //        dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
        //        dropInfo.Effects = DragDropEffects.Copy;
        //    }
        //}
//
        //public void Drop(IDropInfo dropInfo)
        //{
        //    if (dropInfo.Data is DataObject)
        //    {
        //        var stringCollection = (dropInfo.Data as DataObject).GetFileDropList();
        //        foreach (string path in stringCollection)
        //        {
        //            AddPath(path);
        //        }
        //    }
        //}

        #endregion
    }
}
