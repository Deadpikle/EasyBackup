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
        private List<FolderFileCopyProgress> _itemProgressData;

        public BackupInProgressViewModel(IChangeViewModel viewModelChanger, List<FolderFileItem> items, string backupLocation) : base(viewModelChanger)
        {
            Items = items;
            BackupLocation = backupLocation;
            _backupPerformer = new BackupPerformer();
            Status = "Running backup";
            ItemProgressData = new List<FolderFileCopyProgress>();
            foreach (FolderFileItem item in Items)
            {
                ItemProgressData.Add(new FolderFileCopyProgress(item.Path));
            }
            RunBackup();
        }

        public List<FolderFileItem> Items { get; set; }

        public string BackupLocation { get; set; }

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

        private void RunBackup()
        {
            // TODO: show progress!
            Task.Run(() =>
            {
                // setup ItemProgressData
                try
                {
                    _backupPerformer.PerformBackup(Items, BackupLocation);
                    if (_backupPerformer.HasBeenCanceled)
                    {
                        Status = "CANCELED!";
                    }
                    else
                    {
                        Status = "FINISHED!";
                    }
                }
                catch (Exception e)
                {
                    Status = "Backup failed. Error: " + e.Message;
                }
            });
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
