using ByteSizeLib;
using EasyBackup.Helpers;

namespace EasyBackup.Models
{
    class FolderFileCopyProgress : ChangeNotifier
    {
        private string _path;
        private bool _isCopyInProgress;
        private bool _isFinishedCopying;
        private ulong _bytesCopied;
        private ulong _totalBytesToCopy;

        public FolderFileCopyProgress(string path)
        {
            Path = path;
            IsCopyInProgress = false;
            IsFinishedCopying = false;
            BytesCopied = 0;
        }

        public string Path
        {
            get { return _path; }
            set { _path = value; NotifyPropertyChanged(); }
        }

        public bool IsCopyInProgress
        {
            get { return _isCopyInProgress; }
            set { _isCopyInProgress = value; NotifyPropertyChanged(); }
        }

        public bool IsFinishedCopying
        {
            get { return _isFinishedCopying; }
            set { _isFinishedCopying = value; NotifyPropertyChanged(); }
        }

        public ulong BytesCopied
        {
            get { return _bytesCopied; }
            set
            {
                _bytesCopied = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(UserReadableBytesCopied));
                NotifyPropertyChanged(nameof(Progress));
                NotifyPropertyChanged(nameof(ProgressString));
            }
        }

        public string UserReadableBytesCopied
        {
            get
            {
                return Utilities.BytesToString(BytesCopied);
            }
        }

        public ulong TotalBytesToCopy
        {
            get { return _totalBytesToCopy; }
            set
            {
                _totalBytesToCopy = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(UserReadableBytesToCopy));
                NotifyPropertyChanged(nameof(Progress));
                NotifyPropertyChanged(nameof(ProgressString));
            }
        }

        public string UserReadableBytesToCopy
        {
            get
            {
                return Utilities.BytesToString(TotalBytesToCopy);
            }
        }

        public double Progress
        {
            get { return (double)BytesCopied / (double)TotalBytesToCopy * 100; }
        }

        public string ProgressString
        {
            get
            {
                if (double.IsNaN(Progress))
                {
                    return "0.00";
                }
                return string.Format("{0:N2}", Progress);
            }
        }
    }
}
