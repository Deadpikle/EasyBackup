using ByteSizeLib;
using EasyBackup.Helpers;
using EasyBackup.Interfaces;
using EasyBackup.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

namespace EasyBackup.ViewModels
{
    class BackupInProgressViewModel : BaseViewModel
    {
        #region Private Members

        private BackupPerformer _backupPerformer;
        private string _status;
        private Brush _statusColor;
        private string _finishButtonTitle;
        private ulong _totalBackupSize;
        private ulong _bytesCopiedSoFar;
        private List<FolderFileCopyProgress> _itemProgressData;
        private Dictionary<FolderFileItem, FolderFileCopyProgress> _copyDataToProgressMap; // for easy lookup on progress updates
        private double _currentProgress;

        private bool _playsSounds;
        private SoundPlayer _successSoundPlayer;
        private SoundPlayer _failureSoundPlayer;

        #endregion

        public BackupInProgressViewModel(IChangeViewModel viewModelChanger, List<FolderFileItem> items, 
            string backupLocation, bool compressesOutput = false, SecureString compressedPassword = null) : base(viewModelChanger)
        {
            Items = items;
            BackupLocation = backupLocation;
            _currentProgress = 0;
            _backupPerformer = new BackupPerformer();
            Status = "Running backup";
            StatusColor = new SolidColorBrush(Colors.Black);
            ItemProgressData = new List<FolderFileCopyProgress>();
            _copyDataToProgressMap = new Dictionary<FolderFileItem, FolderFileCopyProgress>();
            foreach (FolderFileItem item in Items)
            {
                var progress = new FolderFileCopyProgress(item.Path);
                _copyDataToProgressMap.Add(item, progress);
                ItemProgressData.Add(progress);
            }
            _backupPerformer.StartedCopyingItem += _backupPerformer_StartedCopyingItem;
            _backupPerformer.FinishedCopyingItem += _backupPerformer_FinishedCopyingItem;
            _backupPerformer.CopiedBytesOfItem += _backupPerformer_CopiedBytesOfItem;
            _backupPerformer.BackupFailed += _backupPerformer_BackupFailed;
            _backupPerformer.CalculatedBytesOfItem += _backupPerformer_CalculatedBytesOfItem;
            _playsSounds = Properties.Settings.Default.PlaySoundsWhenFinished;
            if (_playsSounds)
            {
                _failureSoundPlayer = new SoundPlayer("Sounds/failure-tbone.wav");
                _successSoundPlayer = new SoundPlayer("Sounds/success.wav");
            }
            RunBackup();
        }

        #region Properties

        public List<FolderFileItem> Items { get; set; }

        public string BackupLocation { get; set; }

        public string CurrentProgressString
        {
            // formatting the double without rounding: https://stackoverflow.com/a/2453982/3938401
            get { return string.Format("{0:N2}", _currentProgress); }
        }

        public string Status
        {
            get { return _status; }
            set { _status = value; NotifyPropertyChanged(); }
        }

        public List<FolderFileCopyProgress> ItemProgressData
        {
            get { return _itemProgressData; }
            set { _itemProgressData = value; NotifyPropertyChanged(); }
        }

        public string FinishButtonTitle
        {
            get { return _finishButtonTitle; }
            set { _finishButtonTitle = value; NotifyPropertyChanged(); }
        }

        public Brush StatusColor
        {
            get { return _statusColor; }
            set { _statusColor = value; NotifyPropertyChanged(); }
        }

        #endregion

        #region Running Backup

        private void RunBackup()
        {
            var redBrush = new SolidColorBrush(Colors.Red);
            var greenBrush = new SolidColorBrush(Colors.Green);
            Task.Run(() =>
            {
                FinishButtonTitle = "Cancel Backup";
                Status = "Getting backup size...";
                if (!Directory.Exists(BackupLocation))
                {
                    TellUserBackupFailed("Error: Backup directory doesn't exist");
                    StatusColor = redBrush;
                }
                else
                {
                    _backupPerformer.CalculateBackupSize(Items, BackupLocation);
                    ulong freeDriveBytes = Utilities.DriveFreeBytes(BackupLocation);
                    if (_totalBackupSize > freeDriveBytes)
                    {
                        var error = string.Format("Can't perform backup: not enough free space -- need {0} but only have {1}",
                                                ByteSize.FromBytes(_totalBackupSize), ByteSize.FromBytes(freeDriveBytes)); ;
                        TellUserBackupFailed(error);
                        StatusColor = redBrush;
                    }
                    else
                    {
                        Status = "Performing backup...";
                        _backupPerformer.PerformBackup(Items, BackupLocation);
                        if (_backupPerformer.HasBeenCanceled)
                        {
                            TellUserBackupFailed("Backup was canceled");
                            StatusColor = redBrush;
                        }
                        else
                        {
                            TellUserBackupSucceeded("Backup successfully finished");
                            StatusColor = greenBrush;
                        }
                    }
                }
            });
        }

        private void TellUserBackupSucceeded(string message)
        {
            Status = message;
            if (_playsSounds)
            {
                _successSoundPlayer.Play();
            }
            FinishButtonTitle = "Finish Backup";
        }

        private void TellUserBackupFailed(string message)
        {
            Status = message;
            if (_playsSounds)
            {
                _failureSoundPlayer.Play();
            }
            FinishButtonTitle = "Finish Backup";
        }

        #endregion

        #region Backup Performer -- Callbacks

        private void _backupPerformer_CalculatedBytesOfItem(FolderFileItem item, ulong bytes)
        {
            if (_copyDataToProgressMap.ContainsKey(item))
            {
                item.ByteSize = bytes;
                var progressInfo = _copyDataToProgressMap[item];
                progressInfo.TotalBytesToCopy = bytes;
                _totalBackupSize += bytes;
            }
        }

        private void _backupPerformer_StartedCopyingItem(FolderFileItem item)
        {
            if (_copyDataToProgressMap.ContainsKey(item))
            {
                var progressInfo = _copyDataToProgressMap[item];
                progressInfo.IsCopyInProgress = true;
            }
        }

        private void _backupPerformer_FinishedCopyingItem(FolderFileItem item)
        {
            if (_copyDataToProgressMap.ContainsKey(item))
            {
                var progressInfo = _copyDataToProgressMap[item];
                progressInfo.IsCopyInProgress = false;
                progressInfo.IsFinishedCopying = true;
            }
        }

        private void _backupPerformer_CopiedBytesOfItem(FolderFileItem item, ulong bytes)
        {
            if (_copyDataToProgressMap.ContainsKey(item))
            {
                var progressInfo = _copyDataToProgressMap[item];
                progressInfo.BytesCopied += bytes;
                _bytesCopiedSoFar += bytes;
                _currentProgress = Math.Min((double)_bytesCopiedSoFar / (double)_totalBackupSize * 100, 100);
                NotifyPropertyChanged(nameof(CurrentProgressString));
            }
        }

        private void _backupPerformer_BackupFailed(Exception e)
        {
            Status = "Backup failed. Error: " + e.Message;
            FinishButtonTitle = "Finish Backup";
            StatusColor = new SolidColorBrush(Colors.Red);
        }

        #endregion

        public ICommand CancelBackup
        {
            get { return new RelayCommand(PopToSetupView); }
        }

        private async void PopToSetupView()
        {
            if (_backupPerformer.IsRunning)
            {
                await _backupPerformer.CancelAsync();
            }
            _backupPerformer.StartedCopyingItem -= _backupPerformer_StartedCopyingItem;
            _backupPerformer.FinishedCopyingItem -= _backupPerformer_FinishedCopyingItem;
            _backupPerformer.CopiedBytesOfItem -= _backupPerformer_CopiedBytesOfItem;
            _backupPerformer.BackupFailed -= _backupPerformer_BackupFailed;
            _backupPerformer.CalculatedBytesOfItem -= _backupPerformer_CalculatedBytesOfItem;
            PopViewModel();
        }
    }
}
