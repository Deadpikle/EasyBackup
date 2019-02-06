using ByteSizeLib;
using EasyBackup.Helpers;
using EasyBackup.Interfaces;
using EasyBackup.Models;
using EasyBackup.Views;
using GongSolutions.Wpf.DragDrop;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace EasyBackup.ViewModels
{
    class SetupBackupViewModel : BaseViewModel, IDropTarget
    {

        #region Private Members

        private ObservableCollection<FolderFileItem> _items;
        private FolderFileItem _selectedItem;
        private string _backupLocation;
        private ulong _totalBackupSize;

        private string _checkBackupSizeStatus;
        private bool _isCheckBackupSizeStatusVisible;
        private Brush _checkBackupSizeBrush;
        private bool _isCheckingBackupSize;
        private bool _isCancelCheckBackupSizeEnabled;
        private BackupPerformer _backupSizeChecker;

        #endregion

        public SetupBackupViewModel(IChangeViewModel viewModelChanger) : base(viewModelChanger)
        {
            Items = new ObservableCollection<FolderFileItem>();
            LoadBackupTemplate(Properties.Settings.Default.LastUsedBackupTemplatePath);
            IsCheckBackupSizeStatusVisible = false;
        }

        #region Properties

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

        public string BackupLocation
        {
            get { return _backupLocation; }
            set { _backupLocation = value; NotifyPropertyChanged(); }
        }

        public string CheckBackupSizeStatus
        {
            get { return _checkBackupSizeStatus; }
            set { _checkBackupSizeStatus = value; NotifyPropertyChanged(); }
        }

        public bool IsCheckBackupSizeStatusVisible
        {
            get { return _isCheckBackupSizeStatusVisible; }
            set { _isCheckBackupSizeStatusVisible = value; NotifyPropertyChanged(); }
        }

        public Brush CheckBackupSizeBrush
        {
            get { return _checkBackupSizeBrush; }
            set { _checkBackupSizeBrush = value; NotifyPropertyChanged(); }
        }

        public IDialogCoordinator DialogCoordinator { get; set; }

        public bool IsCheckingBackupSize
        {
            get { return _isCheckingBackupSize; }
            set { _isCheckingBackupSize = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(IsCheckBackupSizeEnabled)); }
        }

        public bool IsCancelCheckBackupSizeEnabled
        {
            get { return _isCancelCheckBackupSizeEnabled; }
            set { _isCancelCheckBackupSizeEnabled = value; NotifyPropertyChanged(); }
        }

        public bool IsCheckBackupSizeEnabled
        {
            get { return !_isCheckingBackupSize; }
        }

        public bool PlaysSoundsOnComplete
        {
            get { return Properties.Settings.Default.PlaySoundsWhenFinished; }
            set
            {
                Properties.Settings.Default.PlaySoundsWhenFinished = value;
                Properties.Settings.Default.Save();
                NotifyPropertyChanged();
            }
        }

        #endregion

        #region ICommands

        public ICommand AddFolder
        {
            get { return new RelayCommand(ChooseFolder); }
        }

        public ICommand AddFile
        {
            get { return new RelayCommand(ChooseFile); }
        }

        public ICommand RemoveItem
        {
            get { return new RelayCommand<object>(list => RemoveItemFromList(list)); }
        }

        public ICommand SaveTemplate
        {
            get { return new RelayCommand(SaveItemsToDisk); }
        }

        public ICommand LoadTemplate
        {
            get { return new RelayCommand(LoadItemsFromDisk); }
        }

        public ICommand ChooseBackupLocation
        {
            get { return new RelayCommand(PickBackupFolder); }
        }

        public ICommand PerformBackup
        {
            get { return new RelayCommand(StartBackup); }
        }

        public ICommand CheckBackupSize
        {
            get { return new RelayCommand(ScanBackupAndCheckSize); }
        }

        public ICommand ShowAboutWindow
        {
            get { return new RelayCommand(ShowAboutWindowDialog); }
        }

        public ICommand CancelCheckingBackupSize
        {
            get { return new RelayCommand(StopScanningBackupSize); }
        }

        public ICommand RemoveAllItems
        {
            get { return new RelayCommand(CheckAndRemoveAllItems); }
        }

        #endregion

        private void ChooseFolder()
        {
            var dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
            dialog.ShowNewFolderButton = true;
            if (dialog.ShowDialog(Application.Current.MainWindow).GetValueOrDefault())
            {
                AddPath(dialog.SelectedPath);
            }
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
            IsCheckBackupSizeStatusVisible = false;
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

        private void SaveItemsToDisk()
        {
            var saveFileDialog = new Ookii.Dialogs.Wpf.VistaSaveFileDialog();
            saveFileDialog.AddExtension = true;
            saveFileDialog.Filter = "Easy Backup Files | *.ebf";
            saveFileDialog.DefaultExt = "ebf";
            saveFileDialog.OverwritePrompt = true;
            saveFileDialog.Title = "Choose Save Location";
            if (saveFileDialog.ShowDialog(Application.Current.MainWindow).GetValueOrDefault())
            {
                var backupTemplate = new BackupTemplate() { Paths = Items.ToList(), BackupLocation = BackupLocation };
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(backupTemplate);
                File.WriteAllText(saveFileDialog.FileName, json);
                UpdateLastUsedBackupPath(saveFileDialog.FileName);
            }
        }

        private void LoadItemsFromDisk()
        {
            var openFileDialog = new Ookii.Dialogs.Wpf.VistaOpenFileDialog();
            openFileDialog.AddExtension = true;
            openFileDialog.Filter = "Easy Backup Files | *.ebf";
            openFileDialog.DefaultExt = "ebf";
            openFileDialog.Title = "Choose Easy Backup File";
            if (openFileDialog.ShowDialog(Application.Current.MainWindow).GetValueOrDefault())
            {
                LoadBackupTemplate(openFileDialog.FileName);
                UpdateLastUsedBackupPath(openFileDialog.FileName);
            }
        }

        private void UpdateLastUsedBackupPath(string path)
        {
            Properties.Settings.Default.LastUsedBackupTemplatePath = path;
            Properties.Settings.Default.Save();
        }

        private void LoadBackupTemplate(string path)
        {
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var backupTemplate = Newtonsoft.Json.JsonConvert.DeserializeObject<BackupTemplate>(json);
                if (backupTemplate != null)
                {
                    Items = new ObservableCollection<FolderFileItem>(backupTemplate.Paths);
                    BackupLocation = backupTemplate.BackupLocation;
                }
            }
        }

        private void PickBackupFolder()
        {
            var dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
            dialog.ShowNewFolderButton = true;
            if (dialog.ShowDialog(Application.Current.MainWindow).GetValueOrDefault())
            {
                BackupLocation = dialog.SelectedPath;
            }
        }

        private void StartBackup()
        {
            PushViewModel(new BackupInProgressViewModel(ViewModelChanger, Items.ToList(), BackupLocation));
        }

        private void ShowAboutWindowDialog()
        {
            var aboutWindow = new AboutWindow();
            aboutWindow.Owner = Application.Current.MainWindow;
            aboutWindow.Show();
        }

        private async void ScanBackupAndCheckSize()
        {
            IsCheckingBackupSize = true;
            IsCheckBackupSizeStatusVisible = false;
            _totalBackupSize = 0;
            ulong freeDriveBytes = 0;
            _backupSizeChecker = new BackupPerformer();
            _backupSizeChecker.CalculatedBytesOfItem += BackupPerformer_CalculatedBytesOfItem;
            IsCancelCheckBackupSizeEnabled = true;
            var redBrush = new SolidColorBrush(Colors.Red);
            bool didFail = false;
            await Task.Run(() =>
            {
                try
                {
                    if (!Directory.Exists(BackupLocation))
                    {
                        CheckBackupSizeBrush = redBrush;
                        CheckBackupSizeStatus = "Backup directory doesn't exist";
                        didFail = true;
                    }
                    else
                    {
                        _backupSizeChecker.CalculateBackupSize(Items.ToList(), BackupLocation);
                        freeDriveBytes = Utilities.DriveFreeBytes(BackupLocation);
                    }
                }
                catch (Exception e)
                {
                    CheckBackupSizeBrush = new SolidColorBrush(Colors.Red);
                    CheckBackupSizeStatus = string.Format("Failed to check size of backup -- {0}", e.Message);
                    didFail = true;
                }
            });
            if (!_backupSizeChecker.HasBeenCanceled && !didFail)
            {
                if (_totalBackupSize > freeDriveBytes)
                {
                    CheckBackupSizeBrush = redBrush;
                    CheckBackupSizeStatus = string.Format("Not enough free space -- need {0} but only have {1}",
                                            ByteSize.FromBytes(_totalBackupSize), ByteSize.FromBytes(freeDriveBytes));
                }
                else
                {
                    CheckBackupSizeBrush = new SolidColorBrush(Colors.Green);
                    CheckBackupSizeStatus = string.Format("There's enough space available! We need {0} and have {1} available.",
                                            ByteSize.FromBytes(_totalBackupSize), ByteSize.FromBytes(freeDriveBytes));
                }
            }
            IsCheckBackupSizeStatusVisible = true;
            _backupSizeChecker.CalculatedBytesOfItem -= BackupPerformer_CalculatedBytesOfItem;
            _backupSizeChecker = null;
            IsCheckingBackupSize = false;
            IsCancelCheckBackupSizeEnabled = false;
        }

        private void BackupPerformer_CalculatedBytesOfItem(FolderFileItem item, ulong bytes)
        {
            _totalBackupSize += bytes;
        }

        private void StopScanningBackupSize()
        {
            _backupSizeChecker.Cancel();
            IsCancelCheckBackupSizeEnabled = true;
        }

        private async void CheckAndRemoveAllItems()
        {
            var result = await DialogCoordinator.ShowMessageAsync(this, "Warning!", "Are you sure you want to remove all items?", 
                MessageDialogStyle.AffirmativeAndNegative, 
                new MetroDialogSettings()
                {
                    AffirmativeButtonText = "Yes",
                    NegativeButtonText = "No",
                    ColorScheme = MetroDialogColorScheme.Theme
                }
            );
            if (result == MessageDialogResult.Affirmative)
            {
                Items.Clear();
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
