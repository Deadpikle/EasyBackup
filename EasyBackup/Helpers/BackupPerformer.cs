using EasyBackup.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBackup.Helpers
{
    class BackupPerformer
    {
        // TODO: copy this stuff async and report progress and stuff
        // see: https://stackoverflow.com/questions/33726729/wpf-showing-progress-bar-during-async-file-copy

        // TODO: add option to not always overwrite and use an existing backup (essentially adding new/updated files)

        // from https://docs.microsoft.com/en-us/dotnet/standard/io/how-to-copy-directories
        private static void CopyDirectory(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    CopyDirectory(subdir.FullName, temppath, copySubDirs);
                }
            }
        }

        public static void PerformBackup(List<FolderFileItem> paths, string backupDirectory)
        {
            if (Directory.Exists(backupDirectory))
            {
                backupDirectory = Path.Combine(backupDirectory, "easy-backup", "backup-" + DateTime.Now.ToString("yyyy-MM-dd-H-mm-ss"));
                if (!Directory.Exists(backupDirectory))
                {
                    Directory.CreateDirectory(backupDirectory);
                }
                // ok, start copying the files!
                foreach (FolderFileItem item in paths)
                {
                    var directoryName = Path.GetDirectoryName(item.Path);
                    var pathRoot = Path.GetPathRoot(item.Path);
                    directoryName = directoryName.Replace(pathRoot, "");
                    directoryName = Path.Combine(pathRoot.Replace(":\\", ""), directoryName);
                    var outputDirectoryPath = Path.Combine(backupDirectory, directoryName);
                    if (!Directory.Exists(outputDirectoryPath))
                    {
                        Directory.CreateDirectory(outputDirectoryPath);
                    }
                    if (item.IsDirectory)
                    {
                        var outputPath = Path.Combine(outputDirectoryPath, Path.GetFileName(item.Path));
                        if (Directory.Exists(item.Path))
                        {
                            if (item.OnlyCopiesLatestFile && item.CanEnableOnlyCopiesLatestFile)
                            {
                                // scan directory and copy only the latest file out of it
                            }
                            else
                            {
                                CopyDirectory(item.Path, outputPath, item.IsRecursive);
                            }
                        }
                    }
                    else
                    {
                        var outputPath = Path.Combine(outputDirectoryPath, Path.GetFileName(item.Path));
                        if (File.Exists(item.Path))
                        {
                            File.Copy(item.Path, outputPath);
                        }
                    }
                }
            }
            else
            {
                // todo: error
            }
        }
    }
}
