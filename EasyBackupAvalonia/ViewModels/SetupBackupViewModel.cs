using ByteSizeLib;
using EasyBackupAvalonia.Helpers;
using EasyBackupAvalonia.Interfaces;
using EasyBackupAvalonia.Models;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using NetCoreAudio;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Input;

namespace EasyBackupAvalonia.ViewModels
{
    class SetupBackupViewModel : BaseViewModel
    {

        #region Private Members

        private ObservableCollection<FolderFileItem> _items;
        private FolderFileItem _selectedItem;
        private string _backupLocation;
        private ulong _totalBackupSize;
        private bool _isIncremental;
        private string _backupSizeScanFilePath;

        private string _checkBackupSizeStatus;
        private bool _isCheckBackupSizeStatusVisible;
        private Brush _checkBackupSizeBrush;
        private bool _isCheckingBackupSize;
        private bool _isCancelCheckBackupSizeEnabled;
        private BackupPerformer _backupSizeChecker;

        private SecureString _password;
        private SecureString _confirmPassword;

        private string _lastSaveFilePath;

        #endregion

        public SetupBackupViewModel(IChangeViewModel viewModelChanger) : base(viewModelChanger)
        {
            Items = new ObservableCollection<FolderFileItem>();
            LoadBackupTemplate(Settings.LastUsedBackupTemplatePath);
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

        public bool IsIncremental
        {
            get => _isIncremental;
            set
            {
                _isIncremental = value;
                NotifyPropertyChanged();
            }
        }

        public bool PlaysSoundsOnComplete
        {
            get { return Settings.PlaySoundsOnComplete; }
            set
            {
                Settings.PlaySoundsOnComplete = value;
                NotifyPropertyChanged();
            }
        }

        public bool SavesToCompressedFile
        {
            get { return Settings.SavesToCompressedFile; }
            set
            {
                Settings.SavesToCompressedFile = value;
                NotifyPropertyChanged();
                if (value == false)
                {
                    CompressedFileUsesPassword = false;
                }
            }
        }

        public bool CompressedFileUsesPassword
        {
            get { return Settings.CompressedFileUsesPassword; }
            set
            {
                Settings.CompressedFileUsesPassword = value;
                NotifyPropertyChanged();
                if (value == false)
                {
                    Password.Clear();
                    ConfirmPassword.Clear();
                }
            }
        }

        public SecureString Password
        {
            private get { return _password; }
            set { _password = value; NotifyPropertyChanged(); }
        }

        public SecureString ConfirmPassword
        {
            private get { return _confirmPassword; }
            set { _confirmPassword = value; NotifyPropertyChanged(); }
        }

        public string BackupSizeScanFilePath
        {
            get => _backupSizeScanFilePath;
            set { _backupSizeScanFilePath = value; NotifyPropertyChanged(); }
        }

        #endregion

        public ICommand AddFile => new RelayCommand(o => ChooseFile());
        public ICommand AddFolder => new RelayCommand(o => ChooseFolder());
        public ICommand RemoveItem => new RelayCommand(o => RemoveItemFromList(o));
        public ICommand RemoveAllItems => new RelayCommand(o => CheckAndRemoveAllItems());
        public ICommand ChooseBackupLocation => new RelayCommand(o => PickBackupFolder());
        public ICommand LoadTemplate => new RelayCommand(o => LoadItemsFromDisk());
        public ICommand SaveTemplate => new RelayCommand(o => SaveItemsToDisk());
        public ICommand CheckBackupSize => new RelayCommand(o => ScanBackupAndCheckSize());
        public ICommand PerformBackup => new RelayCommand(o => StartBackup());
        public ICommand EditDirectoryExclusions => new RelayCommand(o => ShowEditDirectoryExclusionsScreen(o as FolderFileItem));
        public ICommand CancelCheckingBackupSize => new RelayCommand(o => StopScanningBackupSize());

        public async void ChooseFolder()
        {
            if (GetTopLevel()?.StorageProvider is IStorageProvider { CanOpen: true } provider)
            {
                var results = await provider.OpenFolderPickerAsync(new FolderPickerOpenOptions()
                {
                    Title = "Select folders...",
                    AllowMultiple = true,
                });
                foreach (IStorageFolder folder in results)
                {
                    AddPath(folder.Path.LocalPath);
                }
            }
        }

        private async void ChooseFile()
        {
            if (GetTopLevel()?.StorageProvider is IStorageProvider { CanOpen: true } provider)
            {
                var results = await provider.OpenFilePickerAsync(new FilePickerOpenOptions()
                {
                    Title = "Select files...",
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
                }
            }
        }

        private async void SaveItemsToDisk()
        {
            if (GetTopLevel()?.StorageProvider is IStorageProvider { CanOpen: true } provider)
            {
                var lastSaveExists = File.Exists(_lastSaveFilePath);
                var lastPath = lastSaveExists ? Path.GetFileName(_lastSaveFilePath) : "";
                var lastFileName = lastSaveExists ? _lastSaveFilePath : "";
                var result = await provider.SaveFilePickerAsync(new FilePickerSaveOptions()
                {
                    Title = "Choose Save Location",
                    SuggestedStartLocation = await provider.TryGetFolderFromPathAsync(lastPath),
                    SuggestedFileName = lastPath,
                    DefaultExtension = "*.ebf",
                    FileTypeChoices = new List<FilePickerFileType>()
                    {
                        new FilePickerFileType("Easy Backup Files | *.ebf")
                        {
                            Patterns = new List<string>() { "*.ebf" }
                        }
                    },
                    ShowOverwritePrompt = true
                });
                if (result != null)
                {
                    var backupTemplate = new BackupTemplate() 
                    { 
                        Paths = Items.ToList(), 
                        BackupLocation = BackupLocation, 
                        IsIncremental = IsIncremental 
                    };
                    using FileStream createStream = File.Create(result.Path.LocalPath);
                    await JsonSerializer.SerializeAsync<BackupTemplate>(createStream, backupTemplate);
                    await createStream.DisposeAsync();
                    UpdateLastUsedBackupPath(result.Path.LocalPath);
                }
            }
        }

        private async void LoadItemsFromDisk()
        {
            if (GetTopLevel()?.StorageProvider is IStorageProvider { CanOpen: true } provider)
            {
                var lastSaveExists = File.Exists(_lastSaveFilePath);
                var lastPath = lastSaveExists ? Path.GetFileName(_lastSaveFilePath) : "";
                var results = await provider.OpenFilePickerAsync(new FilePickerOpenOptions()
                {
                    Title = "Choose EasyBackup File",
                    SuggestedStartLocation = await provider.TryGetFolderFromPathAsync(lastPath),
                    AllowMultiple = false,
                    FileTypeFilter = new List<FilePickerFileType>()
                    {
                        new FilePickerFileType("Easy Backup Files | *.ebf")
                        {
                            Patterns = new List<string>() { "*.ebf" }
                        }
                    }
                });
                foreach (IStorageFile file in results)
                {
                    LoadBackupTemplate(file.Path.LocalPath);
                    UpdateLastUsedBackupPath(file.Path.LocalPath);
                }
            }
        }

        private void UpdateLastUsedBackupPath(string path)
        {
            Settings.LastUsedBackupTemplatePath = path;
            _lastSaveFilePath = path;
        }

        private void LoadBackupTemplate(string path)
        {
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var backupTemplate = JsonSerializer.Deserialize<BackupTemplate>(json);
                if (backupTemplate != null)
                {
                    Items = new ObservableCollection<FolderFileItem>(backupTemplate.Paths);
                    BackupLocation = backupTemplate.BackupLocation;
                    IsIncremental = backupTemplate.IsIncremental;
                }
                _lastSaveFilePath = path;
            }
        }

        private async void PickBackupFolder()
        {
            if (GetTopLevel()?.StorageProvider is IStorageProvider { CanOpen: true } provider)
            {
                var results = await provider.OpenFolderPickerAsync(new FolderPickerOpenOptions()
                {
                    Title = "Choose backup folder...",
                    AllowMultiple = false,
                });
                if (results.Count == 1)
                {
                    BackupLocation = results[0].Path.LocalPath;
                }
            }
        }

        private async void StartBackup()
        {
            if (SavesToCompressedFile && CompressedFileUsesPassword && string.IsNullOrWhiteSpace(Utilities.SecureStringToString(Password)))
            {
                var box = MessageBoxManager.GetMessageBoxStandard(
                    "Error!", 
                    "Your password cannot be blank.", 
                    ButtonEnum.Ok);
                await box.ShowAsync();
            }
            else if (SavesToCompressedFile && CompressedFileUsesPassword && 
                Utilities.SecureStringToString(Password) != Utilities.SecureStringToString(ConfirmPassword))
            {
                var box = MessageBoxManager.GetMessageBoxStandard(
                    "Error!", 
                    "Password and confirm password do not match", 
                    ButtonEnum.Ok);
                await box.ShowAsync();
            }
            else
            {
                bool canProceed = true;
                if (SavesToCompressedFile && CompressedFileUsesPassword)
                {
                    var box = MessageBoxManager.GetMessageBoxStandard(
                        "Warning!", 
                        "This backup will be encrypted. " +
                            "The backup cannot be recovered if you forget your password. " +
                            "Are you sure you want to proceed? ", 
                        ButtonEnum.YesNo);
                    var result = await box.ShowAsync();
                    if (result == ButtonResult.No)
                    {
                        canProceed = false;
                    }
                }
                if (canProceed)
                {
                    PushViewModel(new BackupInProgressViewModel(ViewModelChanger, Items.ToList(), BackupLocation,
                        SavesToCompressedFile, CompressedFileUsesPassword ? Password : null, IsIncremental));
                }
            }
        }

        private void ShowAboutWindowDialog()
        {
            //var aboutWindow = new AboutWindow();
            //aboutWindow.Owner = Application.Current.MainWindow;
            //aboutWindow.Show();
        }

        private async void ScanBackupAndCheckSize()
        {
            IsCheckingBackupSize = true;
            IsCheckBackupSizeStatusVisible = false;
            _totalBackupSize = 0;
            ulong freeDriveBytes = 0;
            _backupSizeChecker = new BackupPerformer();
            _backupSizeChecker.CalculatedBytesOfItem += BackupPerformer_CalculatedBytesOfItem;
            _backupSizeChecker.AboutToProcessFile += BackupPerformer_AboutToProcessFile;
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

        private void BackupPerformer_AboutToProcessFile(string path)
        {
            BackupSizeScanFilePath = path;
        }

        private void StopScanningBackupSize()
        {
            _backupSizeChecker.Cancel();
            IsCancelCheckBackupSizeEnabled = true;
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

        private void ShowEditDirectoryExclusionsScreen(FolderFileItem item)
        {
            PushViewModel(new ExcludeFilesFoldersViewModel(this, item));
        }
    }
}
