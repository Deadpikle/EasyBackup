using ByteSizeLib;
using EasyBackupAvalonia.Helpers;
using EasyBackupAvalonia.Interfaces;
using EasyBackupAvalonia.Models;
using Avalonia.Media;
using Avalonia.Threading;
using NetCoreAudio;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace EasyBackupAvalonia.ViewModels
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
        //private SoundPlayer _successSoundPlayer;
        //private SoundPlayer _failureSoundPlayer;
        private Player _soundPlayer;
        private string _successSoundPath;
        private string _failureSoundPath;

        private SolidColorBrush _redBrush = new SolidColorBrush(Colors.Red);
        private SolidColorBrush _greenBrush = new SolidColorBrush(Colors.Green);

        private DispatcherTimer _dispatcherTimer;
        private Stopwatch _stopwatch;
        private string _currentTimeString;
        private string _runningLabel;

        #endregion

        public BackupInProgressViewModel(IChangeViewModel viewModelChanger, List<FolderFileItem> items, 
            string backupLocation, bool compressesOutput = false, SecureString compressedPassword = null,
            bool isIncremental = false) : base(viewModelChanger)
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
            _playsSounds = Settings.PlaySoundsOnComplete;
            if (_playsSounds)
            {
                _soundPlayer = new Player();
                _successSoundPath = "Sounds/success.wav";
                _failureSoundPath = "Sounds/failure-tbone.wav";
            }
            if (compressesOutput)
            {
                _backupPerformer.UsesCompressedFile = true;
                if (compressedPassword != null)
                {
                    _backupPerformer.UsesPasswordForCompressedFile = true;
                    _backupPerformer.CompressedFilePassword = compressedPassword;
                }
            }
            _backupPerformer.IsIncremental = isIncremental;

            _dispatcherTimer = new DispatcherTimer();
            _stopwatch = new Stopwatch();
            CurrentTimeString = "00:00:00";
            RunningLabel = "Running";
            _dispatcherTimer.Tick += new EventHandler(BackupLengthDispatcherTimerTick);
            _dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 1);

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

        public string CurrentTimeString
        {
            get { return _currentTimeString; }
            set { _currentTimeString = value; NotifyPropertyChanged(); }
        }

        public string RunningLabel
        {
            get { return _runningLabel; }
            set { _runningLabel = value; NotifyPropertyChanged(); }
        }

        #endregion

        #region Running Backup

        private void RunBackup()
        {
            _stopwatch.Start();
            _dispatcherTimer.Start();
            Task.Run(() =>
            {
                FinishButtonTitle = "Cancel Backup";
                Status = "Getting backup size...";
                if (!Directory.Exists(BackupLocation))
                {
                    TellUserBackupFailed("Error: Backup directory doesn't exist");
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
                    }
                    else
                    {
                        Status = "Performing backup...";
                        _backupPerformer.PerformBackup(Items, BackupLocation);
                        if (!_backupPerformer.HadError) // if error, message already handled
                        {
                            if (_backupPerformer.HasBeenCanceled)
                            {
                                TellUserBackupFailed("Backup was canceled");
                            }
                            else
                            {
                                TellUserBackupSucceeded("Backup successfully finished");
                            }
                        }
                    }
                }
            });
        }

        void BackupLengthDispatcherTimerTick(object sender, EventArgs e)
        {
            if (_stopwatch.IsRunning)
            {
                TimeSpan ts = _stopwatch.Elapsed;
                CurrentTimeString = String.Format("{0:00}:{1:00}:{2:00}", ts.Hours, ts.Minutes, ts.Seconds);
            }
        }

        private async void TellUserBackupSucceeded(string message)
        {
            RunningLabel = "Ran";
            _stopwatch.Stop();
            _dispatcherTimer.Stop();
            Status = message;
            StatusColor = _greenBrush;
            if (_playsSounds)
            {
                await _soundPlayer.Play(_successSoundPath);
            }
            FinishButtonTitle = "Finish Backup";
        }

        private async void TellUserBackupFailed(string message)
        {
            RunningLabel = "Ran";
            _stopwatch.Stop();
            _dispatcherTimer.Stop();
            Status = message;
            StatusColor = _redBrush;
            if (_playsSounds)
            {
                await _soundPlayer.Play(_failureSoundPath);
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
            TellUserBackupFailed("Backup failed. Error: " + e.Message);
            StatusColor = _redBrush;
        }

        #endregion

        public ICommand CancelBackup => new RelayCommand(o => StopBackup());

        private async void StopBackup()
        {
            if (_backupPerformer.IsRunning)
            {
                await _backupPerformer.CancelAsync();
            }
            else
            {
                PopToSetupView();
            }
        }

        private void PopToSetupView()
        {
            _backupPerformer.StartedCopyingItem -= _backupPerformer_StartedCopyingItem;
            _backupPerformer.FinishedCopyingItem -= _backupPerformer_FinishedCopyingItem;
            _backupPerformer.CopiedBytesOfItem -= _backupPerformer_CopiedBytesOfItem;
            _backupPerformer.BackupFailed -= _backupPerformer_BackupFailed;
            _backupPerformer.CalculatedBytesOfItem -= _backupPerformer_CalculatedBytesOfItem;
            PopViewModel();
        }
    }
}
