using EasyBackup.Helpers;
using EasyBackup.Interfaces;
using EasyBackup.Models;
using GongSolutions.Wpf.DragDrop;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace EasyBackup.ViewModels
{
    class HomeScreenViewModel : BaseViewModel, IDropTarget
    {
        private ObservableCollection<FolderFileItem> _items;
        private FolderFileItem _selectedItem;
        private bool _isItemSelected;

        public HomeScreenViewModel(IChangeViewModel viewModelChanger) : base(viewModelChanger)
        {
            Items = new ObservableCollection<FolderFileItem>();
        }

        public ObservableCollection<FolderFileItem> Items
        {
            get { return _items; }
            set { _items = value; NotifyPropertyChanged(); }
        }

        public FolderFileItem SelectedItem
        {
            get { return _selectedItem; }
            set { _selectedItem = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(IsItemSelected)); }
        }

        public bool IsItemSelected
        {
            get { return _selectedItem != null; }
        }

        public ICommand AddFolder
        {
            get { return new RelayCommand(ChooseFolder); }
        }

        private void ChooseFolder()
        {
            var dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
            dialog.ShowNewFolderButton = true;
            if (dialog.ShowDialog(Application.Current.MainWindow).GetValueOrDefault())
            {
                AddPath(dialog.SelectedPath);
            }
        }

        public ICommand AddFile
        {
            get { return new RelayCommand(ChooseFile); }
        }

        private void ChooseFile()
        {
            var dialog = new Ookii.Dialogs.Wpf.VistaOpenFileDialog();
            dialog.Multiselect = true;
            dialog.ShowReadOnly = true;
            dialog.Title = "Choose a file";
            if (dialog.ShowDialog(Application.Current.MainWindow).GetValueOrDefault())
            {
                foreach (string fileName in dialog.FileNames)
                {
                    AddPath(fileName);
                }
            }
        }

        private void AddPath(string path)
        {
            var isDirectory = Directory.Exists(path);
            // if we don't already have this path, add the path
            if (Items.Where(x => x.Path == path).Count() == 0)
            {
                Items.Add(new FolderFileItem() { Path = path, IsDirectory = isDirectory, IsRecursive = isDirectory });
            }
        }

        public ICommand RemoveItem
        {
            get { return new RelayCommand<object>(list => RemoveItemFromList(list)); }
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

        public ICommand SaveTemplate
        {
            get { return new RelayCommand(SaveItemsToDisk); }
        }

        private void SaveItemsToDisk()
        {
            var saveFileDialog = new Ookii.Dialogs.Wpf.VistaSaveFileDialog();
            saveFileDialog.AddExtension = true;
            saveFileDialog.Filter = "Easy Backup Files | *.ebf";
            saveFileDialog.DefaultExt = "ebf";
            saveFileDialog.OverwritePrompt = true;
            saveFileDialog.Title = "Choose save location";
            if (saveFileDialog.ShowDialog(Application.Current.MainWindow).GetValueOrDefault())
            {
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(Items);
                File.WriteAllText(saveFileDialog.FileName, json);
            }
        }

        public ICommand LoadTemplate
        {
            get { return new RelayCommand(LoadItemsFromDisk); }
        }

        private void LoadItemsFromDisk()
        {
            var saveFileDialog = new Ookii.Dialogs.Wpf.VistaOpenFileDialog();
            saveFileDialog.AddExtension = true;
            saveFileDialog.Filter = "Easy Backup Files | *.ebf";
            saveFileDialog.DefaultExt = "ebf";
            saveFileDialog.Title = "Choose Easy Backup File";
            if (saveFileDialog.ShowDialog(Application.Current.MainWindow).GetValueOrDefault())
            {
                var json = File.ReadAllText(saveFileDialog.FileName);
                Items = new ObservableCollection<FolderFileItem>(Newtonsoft.Json.JsonConvert.DeserializeObject<List<FolderFileItem>>(json));
            }
        }

        #region IDropTarget

        public void DragOver(IDropInfo dropInfo)
        {
            if (dropInfo.Data is DataObject && (dropInfo.Data as DataObject).GetFileDropList().Count > 0)
            {
                dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
                dropInfo.Effects = DragDropEffects.Copy;
            }
        }

        public void Drop(IDropInfo dropInfo)
        {
            if (dropInfo.Data is DataObject)
            {
                var stringCollection = (dropInfo.Data as DataObject).GetFileDropList();
                foreach (string path in stringCollection)
                {
                    AddPath(path);
                }
            }
        }

        #endregion
    }
}
