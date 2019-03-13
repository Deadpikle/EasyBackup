using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace EasyBackup.Helpers
{
    class Utilities
    {
        // Calculating free space of drive: https://stackoverflow.com/a/13578940/3938401
        // Pinvoke for API function
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetDiskFreeSpaceEx(string lpDirectoryName,
        out ulong lpFreeBytesAvailable,
        out ulong lpTotalNumberOfBytes,
        out ulong lpTotalNumberOfFreeBytes);

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

            ulong freeBytesAvailable = 0;
            ulong totalNumberOfBytes = 0;
            ulong totalNumberOfFreeBytes = 0;

            if (GetDiskFreeSpaceEx(folderName, out freeBytesAvailable, out totalNumberOfBytes, out totalNumberOfFreeBytes))
            {
                return freeBytesAvailable;
            }
            return 0;
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
    }
}
