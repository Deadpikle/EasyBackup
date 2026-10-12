using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBackupAvalonia.Models
{
    class BackupTemplate
    {
        public static int LATEST_VERSION_CODE = 2;

        public List<FolderFileItem> Paths { get; set; }
        public string BackupLocation { get; set; }
        public int VersionCode { get; set; }
        public bool IsIncremental { get; set; }

        public BackupTemplate()
        {
            Paths = new List<FolderFileItem>();
            BackupLocation = "";
            VersionCode = LATEST_VERSION_CODE;
            IsIncremental = false;
        }
    }
}
