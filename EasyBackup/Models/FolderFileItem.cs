using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBackup.Models
{
    class FolderFileItem
    {
        public string Path { get; set; }
        public bool IsDirectory { get; set; }
        public bool IsRecursive { get; set; }
        public bool OnlyCopiesLatestFile { get; set; }
    }
}
