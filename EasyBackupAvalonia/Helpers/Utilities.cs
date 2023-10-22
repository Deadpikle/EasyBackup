using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace EasyBackupAvalonia.Helpers
{
    class Utilities
    {
        public static ulong DriveFreeBytes(string folderName)
        {
            if (string.IsNullOrEmpty(folderName))
            {
                return 0;
            }

            if (!folderName.EndsWith("\\"))
            {
                folderName += '\\';
            }
            FileInfo file = new FileInfo(folderName);
            DriveInfo drive = new DriveInfo(file.Directory.Root.FullName);

            // Calculating free space of drive: https://stackoverflow.com/a/6815482/3938401
            // and https://stackoverflow.com/a/3066845/3938401
            return (ulong)drive.AvailableFreeSpace;
        }

        public static string BytesToString(ulong bytes)
        {
            if (bytes == 0)
            {
                return "-";
            }
            var byteSize = ByteSizeLib.ByteSize.FromBytes(bytes);
            return byteSize.ToString();
        }

        // https://stackoverflow.com/a/12169099/3938401
        public static bool Is64BitOS()
        {
            return Environment.Is64BitOperatingSystem;
        }

        // https://stackoverflow.com/a/819705
        public static string SecureStringToString(SecureString value)
        {
            IntPtr valuePtr = IntPtr.Zero;
            try
            {
                valuePtr = Marshal.SecureStringToGlobalAllocUnicode(value);
                return Marshal.PtrToStringUni(valuePtr);
            }
            catch { }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(valuePtr);
            }
            return "";
        }

        public static string SaveFileLocation
        {
            get
            {
                var folder = Path.Combine(Environment.GetFolderPath(
                        Environment.SpecialFolder.ApplicationData, 
                        Environment.SpecialFolderOption.DoNotVerify), 
                    "EasyBackup");
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }
                return Path.Combine(folder, "settings.json");
            }
        }

        public static bool IsSymbolicLink(string path)
        {
            if (!File.Exists(path) && !Directory.Exists(path))
            {
                return false;
            }
            FileInfo file = new FileInfo(path);
            return file.LinkTarget != null;
        }

    }
}
