using EasyBackup.Helpers;

namespace EasyBackup.Models
{
    class FolderFileCopyProgress : ChangeNotifier
    {
        private string _path;
        private bool _isCopyInProgress;
        private bool _isFinishedCopying;
        private long _bytesCopied;

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
            set { _bytesCopied = value; NotifyPropertyChanged(); }
        }
    }
}
