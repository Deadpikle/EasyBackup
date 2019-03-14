using EasyBackup.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace EasyBackup.Helpers
{
    // TODO: add option to not always overwrite and use an existing backup (essentially adding new/updated files)
    class BackupPerformer
    {
        // // // Events and Delegates // // //

        public delegate void StartedFinishedCopyingItemHandler(FolderFileItem item);
        public event StartedFinishedCopyingItemHandler StartedCopyingItem;
        public event StartedFinishedCopyingItemHandler FinishedCopyingItem;

        public delegate void CopiedBytesOfItemHandler(FolderFileItem item, ulong bytes);
        public event CopiedBytesOfItemHandler CopiedBytesOfItem;

        public delegate void BackupFailedHandler(Exception e);
        public event BackupFailedHandler BackupFailed;

        public delegate void CalculatedBytesOfItemHandler(FolderFileItem item, ulong bytes);
        public event CopiedBytesOfItemHandler CalculatedBytesOfItem;

        // // // // // // // // // // // // //

        public bool IsRunning;
        public bool HadError;
        public bool HasBeenCanceled;

        public bool UsesCompressedFile;
        public bool UsesPasswordForCompressedFile;
        public SecureString CompressedFilePassword;

        private List<string> _directoryPathsSeen;
        private bool _isCalculatingFileSize;
        private ulong _currentDirectorySize;

        public BackupPerformer()
        {
            IsRunning = false;
            HasBeenCanceled = false;
            _isCalculatingFileSize = false;
            _currentDirectorySize = 0;
            UsesCompressedFile = false;
            HadError = false;
            UsesPasswordForCompressedFile = false;
            CompressedFilePassword = null;
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

        // Modified from https://docs.microsoft.com/en-us/dotnet/standard/io/how-to-copy-directories
        private void CopyDirectory(FolderFileItem itemBeingCopied, string sourceDirName, string destDirName, bool copySubDirs)
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
            _directoryPathsSeen.Add(sourceDirName);

            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName) && !_isCalculatingFileSize)
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
                if (_isCalculatingFileSize)
                {
                    _currentDirectorySize += (ulong)file.Length;
                }
                else
                {
                    string tempPath = Path.Combine(destDirName, file.Name);
                    CopySingleFile(itemBeingCopied, file.FullName, tempPath);
                }
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                DirectoryInfo[] dirs = dir.GetDirectories();
                foreach (DirectoryInfo subDir in dirs)
                {
                    if (HasBeenCanceled)
                    {
                        return;
                    }
                    if (!_directoryPathsSeen.Contains(subDir.FullName))
                    {
                        string temppath = Path.Combine(destDirName, subDir.Name);
                        CopyDirectory(itemBeingCopied, subDir.FullName, temppath, copySubDirs);
                        _directoryPathsSeen.Add(subDir.FullName);
                    }
                }
            }
        }

        private Dictionary<string, ulong> GetFilePathsAndSizesInDirectory(string sourceDirName, bool searchSubDirs)
        {
            var fileList = new Dictionary<string, ulong>();
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);
            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }
            _directoryPathsSeen.Add(sourceDirName);
            // Get the files in the current directory
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                if (HasBeenCanceled)
                {
                    return fileList;
                }
                fileList.Add(file.FullName, (ulong)file.Length);
            }

            // If copying subdirectories, search them and their contents 
            if (searchSubDirs)
            {
                DirectoryInfo[] dirs = dir.GetDirectories();
                foreach (DirectoryInfo subDir in dirs)
                {
                    if (HasBeenCanceled)
                    {
                        return fileList;
                    }
                    if (!_directoryPathsSeen.Contains(subDir.FullName))
                    {
                        var additionalFileInfo = GetFilePathsAndSizesInDirectory(subDir.FullName, searchSubDirs);
                        foreach (KeyValuePair<string, ulong> entry in additionalFileInfo)
                        {
                            fileList.Add(entry.Key, entry.Value);
                        }
                        _directoryPathsSeen.Add(subDir.FullName);
                    }
                }
            }
            return fileList;
        }

        // Modified from https://stackoverflow.com/a/33726939/3938401
        private void CopySingleFile(FolderFileItem itemBeingCopied, string source, string destination)
        {
            if (File.Exists(source))
            {
                if (HasBeenCanceled)
                {
                    return;
                }
                if (UsesCompressedFile)
                {
                }
                else
                {
                    using (var outStream = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
                    {
                        using (var inStream = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            int buffer_size = 10240;
                            byte[] buffer = new byte[buffer_size];
                            long total_read = 0;
                            while (total_read < inStream.Length)
                            {
                                if (HasBeenCanceled)
                                {
                                    break;
                                }
                                int read = inStream.Read(buffer, 0, buffer_size);
                                outStream.Write(buffer, 0, read);
                                CopiedBytesOfItem(itemBeingCopied, (ulong)read);
                                total_read += read;
                            }
                        }
                    }
                }
            }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool GenerateConsoleCtrlEvent(ConsoleCtrlEvent sigevent, int dwProcessGroupId);
        public enum ConsoleCtrlEvent
        {
            CTRL_C = 0,
            CTRL_BREAK = 1,
            CTRL_CLOSE = 2,
            CTRL_LOGOFF = 5,
            CTRL_SHUTDOWN = 6
        }
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool AttachConsole(uint dwProcessId);
        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        internal static extern bool FreeConsole();
        [DllImport("kernel32.dll")]
        static extern bool SetConsoleCtrlHandler(ConsoleCtrlDelegate HandlerRoutine, bool Add);
        // Delegate type to be used as the Handler Routine for SCCH
        delegate Boolean ConsoleCtrlDelegate(uint CtrlType);

        private void BackupToCompressedFile(string destination, List<string> filePaths, 
            Dictionary<string, FolderFileItem> pathsToFolderFileItem, Dictionary<string, ulong> pathsToFileSize)
        {
            var quotedFilePaths = new List<string>();
            foreach (string filePath in filePaths)
            {
                quotedFilePaths.Add("\"" + filePath + "\"");
            }
            var is64BitOS = Utilities.Is64BitOS();
            Process process = new Process();
            var currentDir = Directory.GetParent(Assembly.GetExecutingAssembly().Location).FullName;
            var exePath = is64BitOS ? currentDir + "/tools/x64/7za.exe" : currentDir + "/tools/x86/7za.exe";
            process.StartInfo.FileName = exePath;
            process.StartInfo.WorkingDirectory = Directory.GetParent(Assembly.GetExecutingAssembly().Location).FullName;

            // https://stackoverflow.com/a/6522928/3938401
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardOutput = true;
            Console.OutputEncoding = System.Text.Encoding.UTF8; // so we get all filenames properly
            //process.StartInfo.RedirectStandardError = true;
            process.EnableRaisingEvents = true;
            var didError = false;
            var sizeInBytes = 0UL;
            var remainingBytes = 0UL;
            var didStartCompressingFile = false;
            var lastPercent = 0.0;
            string currentFilePath = "";
            FolderFileItem currentItem = null;
            var bytesCopiedForItem = new Dictionary<FolderFileItem, ulong>();
            var didFinishCancel = false;
            var nextMessageIsError = false;
            string errorMessage = "";
            process.OutputDataReceived += new DataReceivedEventHandler(delegate (object sender, DataReceivedEventArgs e) {
                if (string.IsNullOrWhiteSpace(e.Data) || didFinishCancel) // in case more events come through
                {
                    return;
                }
                if (HasBeenCanceled || didError)
                {
                    // ONLY WORKS IF YOU AREN'T ALREADY SHOWING A CONSOLE!
                    // https://stackoverflow.com/a/29274238/3938401
                    if (AttachConsole((uint)process.Id))
                    {
                        SetConsoleCtrlHandler(null, true);
                        try
                        {
                            GenerateConsoleCtrlEvent(ConsoleCtrlEvent.CTRL_C, process.SessionId); // ends the process
                            process.Kill(); // process is canned, so OK to kill
                        }
                        catch { }
                        finally
                        {
                            FreeConsole();
                            SetConsoleCtrlHandler(null, false);
                            didFinishCancel = true;
                        }
                        return;
                    }
                }
                if (e.Data.StartsWith("+ ") && e.Data.Trim() != "+")
                {
                    didStartCompressingFile = true;
                    currentFilePath = e.Data.Substring(2);
                    if (currentItem != null && remainingBytes > 0)
                    {
                        CopiedBytesOfItem(currentItem, remainingBytes);
                        bytesCopiedForItem[currentItem] += remainingBytes;
                        if (!currentItem.IsDirectory)
                        {
                            FinishedCopyingItem?.Invoke(currentItem);
                        }
                        else
                        {
                            if (bytesCopiedForItem[currentItem] == currentItem.ByteSize)
                            {
                                FinishedCopyingItem?.Invoke(currentItem);
                            }
                        }
                    }
                    if (pathsToFolderFileItem.ContainsKey(currentFilePath))
                    {
                        currentItem = pathsToFolderFileItem[currentFilePath];
                    }
                    else
                    {
                        currentItem = null;
                    }
                    if (currentItem != null && !bytesCopiedForItem.ContainsKey(currentItem))
                    {
                        bytesCopiedForItem[currentItem] = 0;
                    }
                    if (pathsToFileSize.ContainsKey(currentFilePath))
                    {
                        sizeInBytes = remainingBytes = pathsToFileSize[currentFilePath];
                    }
                    else
                    {
                        sizeInBytes = remainingBytes = 0;
                    }
                }
                else if (e.Data.Contains("%") && didStartCompressingFile)
                {
                    var percent = double.Parse(e.Data.Trim().Split('%')[0]);
                    var actualPercent = percent - lastPercent;
                    lastPercent = percent;
                    var copiedBytes = Math.Floor((actualPercent / 100.0) * sizeInBytes); // floor -- would rather underestimate than overestimate
                    if (currentItem != null)
                    {
                        CopiedBytesOfItem(currentItem, (ulong)copiedBytes);
                        bytesCopiedForItem[currentItem] += (ulong)copiedBytes;
                    }
                    remainingBytes -= (ulong)copiedBytes;
                }
                else if (e.Data.Contains("Error:"))
                {
                    nextMessageIsError = true;
                }
                else if (nextMessageIsError)
                {
                    errorMessage = e.Data;
                    didError = true;
                    nextMessageIsError = false;
                }
            });
            /**
             * Command line params:
             * -y (yes to prompts)
             * -ssw (Compresses files open for writing by another applications)
             * -bsp1 (output for progress to stdout)
             * -bse1 (output for errors to stdout)
             * -bb1 (log level 1)
             * -spf (Use fully qualified file paths)
             * -mx1 (compression level to fastest)
             * -v2g (split into 2 gb volumes -- https://superuser.com/a/184601)
             * -sccUTF-8 (set console output to UTF-8)
             * -p (set password for file)
             * */
            var args = "-y -ssw -bsp1 -bse1 -bb1 -spf -mx1 -v2g -sccUTF-8";
            if (UsesPasswordForCompressedFile)
            {
                var pass = Utilities.SecureStringToString(CompressedFilePassword);
                if (!string.IsNullOrWhiteSpace(pass))
                {
                    args = "-p" + pass + " " + args; // add password flag
                }
            }
            string inputPaths = string.Join("\n", quotedFilePaths);
            // to circumvent issue where inputPaths is too long for command line, need to write them to a file
            // and then load the file into 7z via command line params (@fileName as last param -- https://superuser.com/a/940894)
            var tmpFileName = Path.GetTempFileName();
            using (StreamWriter sw = new StreamWriter(tmpFileName))
            {
                sw.Write(inputPaths);
            }
            args = "a " + args + " \"" + destination + "\" @\"" + tmpFileName + "\""; // a = add file
            process.StartInfo.Arguments = args;
            process.Start();
            process.BeginOutputReadLine();
            //process.BeginErrorReadLine();
            process.WaitForExit();
            // make sure last item is handled properly
            if (!HasBeenCanceled && currentItem != null && remainingBytes > 0)
            {
                CopiedBytesOfItem(currentItem, remainingBytes);
                FinishedCopyingItem?.Invoke(currentItem);
            }
            if (HasBeenCanceled)
            {
                File.Delete(destination);
            }
            if (didError)
            {
                if (string.IsNullOrWhiteSpace(errorMessage))
                {
                    errorMessage = "Compression operation failed";
                }
                throw new Exception(errorMessage);
            }
        }

        private void IterateOverFiles(List<FolderFileItem> paths, string backupDirectory)
        {
            try
            {
                HadError = false;
                IsRunning = true;
                if (Directory.Exists(backupDirectory) || _isCalculatingFileSize)
                {
                    var backupName = "backup-" + DateTime.Now.ToString("yyyy-MM-dd-H-mm-ss");
                    backupDirectory = Path.Combine(backupDirectory, "easy-backup", backupName);
                    if (!Directory.Exists(backupDirectory) && !_isCalculatingFileSize)
                    {
                        Directory.CreateDirectory(backupDirectory);
                    }
                    else if (!_isCalculatingFileSize)
                    {
                        // ok, somehow they started two backups within the same second >_> wait 1 second and start again
                        Task.Delay(1000);
                        backupName = "backup-" + DateTime.Now.ToString("yyyy-MM-dd-H-mm-ss");
                        backupDirectory = Path.Combine(backupDirectory, "easy-backup", backupName);
                        if (!Directory.Exists(backupDirectory))
                        {
                            Directory.CreateDirectory(backupDirectory);
                        }
                        else
                        {
                            throw new Exception("Couldn't create backup directory (directory already exists)");
                        }
                    }
                    // ok, start copying the files if not using compressed file.
                    if (!UsesCompressedFile || _isCalculatingFileSize)
                    {
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
                            if (!Directory.Exists(outputDirectoryPath) && !_isCalculatingFileSize)
                            {
                                Directory.CreateDirectory(outputDirectoryPath);
                            }
                            if (!_isCalculatingFileSize)
                            {
                                StartedCopyingItem?.Invoke(item);
                            }
                            if (item.IsDirectory && Directory.Exists(item.Path))
                            {
                                if (item.OnlyCopiesLatestFile && item.CanEnableOnlyCopiesLatestFile)
                                {
                                    // scan directory and copy only the latest file out of it
                                    var directoryInfo = new DirectoryInfo(item.Path);
                                    var latestFile = directoryInfo.GetFiles().OrderByDescending(x => x.LastWriteTimeUtc).FirstOrDefault();
                                    if (latestFile != null)
                                    {
                                        if (_isCalculatingFileSize)
                                        {
                                            CalculatedBytesOfItem?.Invoke(item, (ulong)new FileInfo(latestFile.FullName).Length);
                                        }
                                        else
                                        {
                                            var outputBackupDirectory = Path.Combine(outputDirectoryPath, Path.GetFileName(item.Path));
                                            // create directory if needed in backup path
                                            if (!Directory.Exists(outputBackupDirectory))
                                            {
                                                Directory.CreateDirectory(outputBackupDirectory);
                                            }
                                            if (HasBeenCanceled)
                                            {
                                                break;
                                            }
                                            var outputPath = Path.Combine(outputBackupDirectory, Path.GetFileName(latestFile.FullName));
                                            CopySingleFile(item, latestFile.FullName, outputPath);
                                        }
                                    }
                                }
                                else
                                {
                                    if (HasBeenCanceled)
                                    {
                                        break;
                                    }
                                    _currentDirectorySize = 0;
                                    var outputPath = Path.Combine(outputDirectoryPath, Path.GetFileName(item.Path));
                                    CopyDirectory(item, item.Path, outputPath, item.IsRecursive);
                                    if (_isCalculatingFileSize)
                                    {
                                        CalculatedBytesOfItem?.Invoke(item, _currentDirectorySize);
                                    }
                                }
                            }
                            else
                            {
                                if (_isCalculatingFileSize)
                                {
                                    CalculatedBytesOfItem?.Invoke(item, (ulong)new FileInfo(item.Path).Length);
                                }
                                else
                                {
                                    var outputPath = Path.Combine(outputDirectoryPath, Path.GetFileName(item.Path));
                                    CopySingleFile(item, item.Path, outputPath);
                                }
                            }
                            if (!HasBeenCanceled && !_isCalculatingFileSize)
                            {
                                FinishedCopyingItem?.Invoke(item);
                            }
                        }
                    }
                    else
                    {
                        // first, figure out each file that needs to be copied into the 7z file. this way we can optimize 
                        // the copy to 1 single Process start.
                        var filePaths = new List<string>();
                        var pathsToFolderFileItem = new Dictionary<string, FolderFileItem>();
                        var pathToFileSize = new Dictionary<string, ulong>();
                        foreach (FolderFileItem item in paths)
                        {
                            if (HasBeenCanceled)
                            {
                                break;
                            }
                            if (item.IsDirectory && Directory.Exists(item.Path))
                            {
                                if (item.OnlyCopiesLatestFile && item.CanEnableOnlyCopiesLatestFile)
                                {
                                    // scan directory and copy only the latest file out of it
                                    var directoryInfo = new DirectoryInfo(item.Path);
                                    var latestFile = directoryInfo.GetFiles().OrderByDescending(x => x.LastWriteTimeUtc).FirstOrDefault();
                                    if (latestFile != null)
                                    {
                                        if (HasBeenCanceled)
                                        {
                                            break;
                                        }
                                        pathsToFolderFileItem.Add(latestFile.FullName, item);
                                        filePaths.Add(latestFile.FullName);
                                        pathToFileSize.Add(item.Path, (ulong)new FileInfo(latestFile.FullName).Length);
                                    }
                                }
                                else
                                {
                                    if (HasBeenCanceled)
                                    {
                                        break;
                                    }
                                    var filesWithSizesInDirectory = GetFilePathsAndSizesInDirectory(item.Path, item.IsRecursive);
                                    foreach (KeyValuePair<string, ulong> entry in filesWithSizesInDirectory)
                                    {
                                        pathsToFolderFileItem.Add(entry.Key, item);
                                        pathToFileSize.Add(entry.Key, entry.Value);
                                        filePaths.Add(entry.Key);
                                    }
                                }
                            }
                            else
                            {
                                pathsToFolderFileItem.Add(item.Path, item);
                                pathToFileSize.Add(item.Path, (ulong)new FileInfo(item.Path).Length);
                                filePaths.Add(item.Path);
                            }
                        }
                        _directoryPathsSeen.Clear();
                        if (!HasBeenCanceled)
                        {
                            // ok, we can do le copy now
                            BackupToCompressedFile(Path.Combine(backupDirectory, backupName + ".7z"), filePaths, pathsToFolderFileItem, pathToFileSize);
                            if (HasBeenCanceled)
                            {
                                try
                                {
                                    // not a huge deal if this fails
                                    Directory.Delete(backupDirectory);
                                }
                                catch (Exception) { }
                            }
                        }
                    }
                }
                else
                {
                    throw new Exception("Backup directory doesn't exist");
                }
                IsRunning = false;
            }
            catch (Exception e)
            {
                HadError = true;
                BackupFailed?.Invoke(e);
            }
            finally
            {
                IsRunning = false;
            }
        }

        public void CalculateBackupSize(List<FolderFileItem> paths, string backupDirectory)
        {
            _directoryPathsSeen = new List<string>();
            _isCalculatingFileSize = true;
            IterateOverFiles(paths, backupDirectory);
            _isCalculatingFileSize = false;
        }

        public void PerformBackup(List<FolderFileItem> paths, string backupDirectory)
        {
            _directoryPathsSeen = new List<string>();
            IterateOverFiles(paths, backupDirectory);
        }
    }
}
