using EasyBackupAvalonia.Helpers;
using EasyBackupAvalonia.Interfaces;
using EasyBackupAvalonia.Models;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
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
        private FolderFileItem _selectedItem;
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

        public FolderFileItem SelectedItem
        {
            get { return _selectedItem; }
            set { _selectedItem = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(IsItemSelected)); }
        }

        public bool IsItemSelected
        {
            get { return _selectedItem != null; }
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

        public ICommand CancelChangeExclusions => new RelayCommand(o => CancelChangeAndPopView());
        public ICommand SaveExclusions => new RelayCommand(o => SaveExclusionsAndPopView());
        public ICommand AddFile => new RelayCommand(o => ChooseFiles());
        public ICommand AddFolder => new RelayCommand(o => ChooseFolders());
        public ICommand RemoveItem => new RelayCommand(o => RemoveItemFromList(o));
        public ICommand RemoveAllItems => new RelayCommand(o => CheckAndRemoveAllItems());

        private async void ChooseFolders()
        {
            if (GetTopLevel()?.StorageProvider is IStorageProvider { CanOpen: true } provider)
            {
                var results = await provider.OpenFolderPickerAsync(new FolderPickerOpenOptions()
                {
                    Title = "Select folders...",
                    SuggestedStartLocation = await provider.TryGetFolderFromPathAsync(_itemBeingEdited.Path),
                    AllowMultiple = true,
                });
                foreach (IStorageFolder folder in results)
                {
                    AddPath(folder.Path.LocalPath);
                }
            }
        }

        private async void ChooseFiles()
        {
            if (GetTopLevel()?.StorageProvider is IStorageProvider { CanOpen: true } provider)
            {
                var results = await provider.OpenFilePickerAsync(new FilePickerOpenOptions()
                {
                    Title = "Select files...",
                    SuggestedStartLocation = await provider.TryGetFolderFromPathAsync(_itemBeingEdited.Path),
                    AllowMultiple = true,
                });
                foreach (IStorageFile file in results)
                {
                    AddPath(file.Path.LocalPath);
                }
            }
        }

        public void AddPath(string path)
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
            // eventually change to https://github.com/AvaloniaUtils/DialogHost.Avalonia as that's much nicer,
            // but it is more complex to implement with MMVM (aka more time-consuming)
            var box = MessageBoxManager.GetMessageBoxStandard(
                "Warning!", 
                "Are you sure you want to remove all items?", 
                ButtonEnum.YesNo);
            var result = await box.ShowAsync();
            if (result == ButtonResult.Yes)
            {
                Items.Clear();
            }
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
                }
            }
        }
    }
}
