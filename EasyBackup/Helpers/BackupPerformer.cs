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
        public bool IsRunning { get; set; }
        public bool HasBeenCanceled { get; set; }

        public BackupPerformer()
        {
            IsRunning = false;
            HasBeenCanceled = false;
        }
        
        // TODO: copy this stuff async and report progress and stuff
        // see: https://stackoverflow.com/questions/33726729/wpf-showing-progress-bar-during-async-file-copy

        // TODO: add option to not always overwrite and use an existing backup (essentially adding new/updated files)

        // from https://docs.microsoft.com/en-us/dotnet/standard/io/how-to-copy-directories
        private void CopyDirectory(string sourceDirName, string destDirName, bool copySubDirs)
        {
            if (HasBeenCanceled)
            {
                return;
            }
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
                if (HasBeenCanceled)
                {
                    return;
                }
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    if (HasBeenCanceled)
                    {
                        return;
                    }
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    CopyDirectory(subdir.FullName, temppath, copySubDirs);
                }
            }
        }

        public void Cancel()
        {
            HasBeenCanceled = true;
        }

        public async Task CancelAsync()
        {
            HasBeenCanceled = true;
            while (IsRunning)
            {
                await Task.Delay(100); // wait for cancel to finish
            }
        }

        public void PerformBackup(List<FolderFileItem> paths, string backupDirectory)
        {
            IsRunning = true;
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
                    if (HasBeenCanceled)
                    {
                        break;
                    }
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
                        if (Directory.Exists(item.Path))
                        {
                            if (item.OnlyCopiesLatestFile && item.CanEnableOnlyCopiesLatestFile)
                            {
                                // scan directory and copy only the latest file out of it
                                var directoryInfo = new DirectoryInfo(item.Path);
                                var latestFile = directoryInfo.GetFiles().OrderByDescending(x => x.LastWriteTimeUtc).FirstOrDefault();
                                if (latestFile != null)
                                {
                                    var outputBackupDirectory = Path.Combine(outputDirectoryPath, Path.GetFileName(item.Path));
                                    // create directory if needed in backup path
                                    if (!Directory.Exists(outputBackupDirectory))
                                    {
                                        Directory.CreateDirectory(outputBackupDirectory);
                                    }
                                    var outputPath = Path.Combine(outputBackupDirectory, Path.GetFileName(latestFile.FullName));
                                    if (File.Exists(latestFile.FullName))
                                    {
                                        if (HasBeenCanceled)
                                        {
                                            break;
                                        }
                                        File.Copy(latestFile.FullName, outputPath);
                                    }
                                }
                            }
                            else
                            {
                                var outputPath = Path.Combine(outputDirectoryPath, Path.GetFileName(item.Path));
                                if (HasBeenCanceled)
                                {
                                    break;
                                }
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
            IsRunning = false;
        }
    }
}
