using EasyBackup.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBackup.Models
{
    class FolderFileItem : ChangeNotifier
    {
        private string _path;
        private bool _isDirectory;
        private bool _isRecursive;
        private bool _onlyCopiesLatestFile;

        public string Path
        {
            get { return _path; }
            set { _path = value; NotifyPropertyChanged(); }
        }

        public bool IsDirectory
        {
            get { return _isDirectory; }
            set { _isDirectory = value; NotifyPropertyChanged(); }
        }

        public bool IsRecursive
        {
            get { return _isRecursive; }
            set
            {
                _isRecursive = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(CanEnableOnlyCopiesLatestFile));
            }
        }

        public bool CanEnableOnlyCopiesLatestFile
        {
            get { return _isDirectory && !IsRecursive; }
        }

        public bool OnlyCopiesLatestFile
        {
            get { return _onlyCopiesLatestFile; }
            set { _onlyCopiesLatestFile = value; NotifyPropertyChanged(); }
        }
    }
}
