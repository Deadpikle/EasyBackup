using ByteSizeLib;
using EasyBackup.Helpers;

namespace EasyBackup.Models
{
    class FolderFileCopyProgress : ChangeNotifier
    {
        private string _path;
        private bool _isCopyInProgress;
        private bool _isFinishedCopying;
        private long _bytesCopied;
        private long _totalBytesToCopy;

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

        public long BytesCopied
        {
            get { return _bytesCopied; }
            set { _bytesCopied = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(UserReadableBytesCopied)); }
        }

        public string UserReadableBytesCopied
        {
            get
            {
                var byteSize = ByteSize.FromBytes(BytesCopied);
                return byteSize.ToString();
            }
        }

        public long TotalBytesToCopy
        {
            get { return _totalBytesToCopy; }
            set { _totalBytesToCopy = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(UserReadableBytesToCopy)); }
        }

        public string UserReadableBytesToCopy
        {
            get
            {
                var byteSize = ByteSizeLib.ByteSize.FromBytes(TotalBytesToCopy);
                return byteSize.ToString();
            }
        }
    }
}
