using ByteSizeLib;
using EasyBackup.Helpers;
using EasyBackup.Interfaces;
using EasyBackup.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace EasyBackup.ViewModels
{
    class BackupInProgressViewModel : BaseViewModel
    {
        private BackupPerformer _backupPerformer;
        private string _status;
        private string _finishButtonTitle;
        private ulong _backupSize;
        private ulong _bytesCopiedSoFar;
        private List<FolderFileCopyProgress> _itemProgressData;
        private Dictionary<FolderFileItem, FolderFileCopyProgress> _copyDataToProgressMap; // for easy lookup on progress updates
        private double _currentProgress;

        public BackupInProgressViewModel(IChangeViewModel viewModelChanger, List<FolderFileItem> items, string backupLocation) : base(viewModelChanger)
        {
            Items = items;
            BackupLocation = backupLocation;
            _currentProgress = 0;
            _backupPerformer = new BackupPerformer();
            Status = "Running backup";
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
            RunBackup();
        }

        public List<FolderFileItem> Items { get; set; }

        public string BackupLocation { get; set; }

        public string CurrentProgressString
        {
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

        private void RunBackup()
        {
            Task.Run(() =>
            {
                FinishButtonTitle = "Cancel Backup";
                Status = "Getting backup size...";
                _backupPerformer.CalculateBackupSize(Items, BackupLocation);
                ulong freeDriveBytes = Utilities.DriveFreeBytes(BackupLocation);
                if (_backupSize > freeDriveBytes)
                {
                    Status = string.Format("Can't perform backup: not enough free space -- need {0} but only have {1}",
                                            ByteSize.FromBytes(_backupSize), ByteSize.FromBytes(freeDriveBytes));
                }
                else
                {
                    Status = "Performing backup...";
                    _backupPerformer.PerformBackup(Items, BackupLocation);
                    if (_backupPerformer.HasBeenCanceled)
                    {
                        Status = "Backup was canceled";
                    }
                    else
                    {
                        Status = "Backup successfully finished";
                    }
                    FinishButtonTitle = "Finish Backup";
                }
            });
        }

        private void _backupPerformer_CalculatedBytesOfItem(FolderFileItem item, ulong bytes)
        {
            if (_copyDataToProgressMap.ContainsKey(item))
            {
                // we use += in case it's a directory
                item.ByteSize += bytes;
                var progressInfo = _copyDataToProgressMap[item];
                progressInfo.TotalBytesToCopy += bytes;
                _backupSize += bytes;
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
                _currentProgress = Math.Min((double)_bytesCopiedSoFar / (double)_backupSize * 100, 100);
                NotifyPropertyChanged(nameof(CurrentProgressString));
            }
        }

        private void _backupPerformer_BackupFailed(Exception e)
        {
            Status = "Backup failed. Error: " + e.Message;
            FinishButtonTitle = "Finish Backup";
        }

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
            PopViewModel();
        }
    }
}
