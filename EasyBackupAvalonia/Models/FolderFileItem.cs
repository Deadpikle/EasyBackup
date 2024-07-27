using EasyBackupAvalonia.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBackupAvalonia.Models
{
    class FolderFileItem : ChangeNotifier
    {
        private string _path;
        private bool _isDirectory;
        private bool _isRecursive;
        private bool _onlyCopiesLatestFile;
        private ulong _byteSize;

        private List<string> _excludedPaths;

        public FolderFileItem()
        {
            ExcludedPaths = new List<string>();
        }

        public string Path
        {
            get { return _path; }
            set { _path = value; NotifyPropertyChanged(); }
        }

        public bool IsDirectory
        {
            get { return _isDirectory; }
            set
            {
                _isDirectory = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(CanEditFolderExclusions));
            }
        }

        public bool IsRecursive
        {
            get { return _isRecursive; }
            set
            {
                _isRecursive = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(CanEnableOnlyCopiesLatestFile));
                NotifyPropertyChanged(nameof(CanEditFolderExclusions));
            }
        }

        public bool CanEnableOnlyCopiesLatestFile
        {
            get { return _isDirectory && !IsRecursive; }
        }

        public bool OnlyCopiesLatestFile
        {
            get { return _onlyCopiesLatestFile; }
            set
            {
                _onlyCopiesLatestFile = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(CanEditFolderExclusions));
            }
        }

        public ulong ByteSize
        {
            get { return _byteSize; }
            set { _byteSize = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(UserReadableByteSize)); }
        }

        public string UserReadableByteSize
        {
            get
            {
                return Utilities.BytesToString(ByteSize);
            }
        }

        public bool CanEditFolderExclusions
        {
            get
            {
                return IsDirectory && (!OnlyCopiesLatestFile || IsRecursive);
            }
        }

        public List<string> ExcludedPaths
        {
            get
            {
                if (CanEditFolderExclusions)
                {
                    return _excludedPaths;
                }
                else
                {
                    // return empty list; this way we can always access ExcludedPaths when backing up without having to check CanEditFolderExclusions
                    return new List<string>() { }; 
                }
            }
            set { _excludedPaths = value; NotifyPropertyChanged(); }
        }
    }
}
