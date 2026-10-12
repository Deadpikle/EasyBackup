using EasyBackupAvalonia.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EasyBackupAvalonia.Helpers;

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

    public delegate void ProcessingFileHandler(string path);
    public event ProcessingFileHandler AboutToProcessFile;

    // // // // // // // // // // // // //

    public bool IsRunning;
    public bool HadError;
    public bool HasBeenCanceled;

    public bool IsIncremental;

    public bool UsesCompressedFile;
    public bool UsesPasswordForCompressedFile;
    public SecureString CompressedFilePassword;

    private List<string> _directoryPathsSeen;
    private bool _isCalculatingFileSize;
    private ulong _currentDirectorySize;
    private Dictionary<string, bool> _pathsCopiedTo;

    private EnumerationOptions _enumerationOptions;

    private string _backupDirectory;

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
        IsIncremental = false;
        _enumerationOptions = new EnumerationOptions()
        {
            IgnoreInaccessible = true,
            MatchType = MatchType.Win32,
            AttributesToSkip = 0,
        };
        _pathsCopiedTo = new Dictionary<string, bool>();
        _backupDirectory = "";
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

    private bool ShouldAllowPath(List<string> excludedPaths, string path)
    {
        if (excludedPaths == null)
        {
            return true;
        }
        var excluded = new char[] { '/', '/' };
        foreach (string excludedPath in excludedPaths)
        {
            if (path.Trim(excluded).Contains(excludedPath.Trim(excluded)))
            {
                return false;
            }
        }
        if (path.Contains(".imovielibrary/.fcpcache"))
        {
            // TODO: fix infinite syncing with iMovie; need to add a better fix for this 
            // somewhere and/or disallow alias as option for folder 
            return false; 
        }
        return true;
    }

    // Modified from https://docs.microsoft.com/en-us/dotnet/standard/io/how-to-copy-directories
    private async Task CopyDirectory(FolderFileItem itemBeingCopied, string sourceDirName, string destDirName, bool copySubDirs, List<string> excludedPaths = null)
    {
        if (HasBeenCanceled)
        {
            return;
        }
        // Get the subdirectories for the specified directory.
        var dir = new DirectoryInfo(sourceDirName);

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
        FileInfo[] files = dir.GetFiles("*", enumerationOptions: _enumerationOptions);
        foreach (FileInfo file in files)
        {
            if (HasBeenCanceled)
            {
                return;
            }
            string tempPath = Path.Combine(destDirName, file.Name);
            bool shouldAllow = ShouldAllowPath(excludedPaths, file.FullName);
            if (_isCalculatingFileSize && shouldAllow)
            {
                AboutToProcessFile?.Invoke(file.FullName);
                _currentDirectorySize += (ulong)file.Length;
            }
            else if (shouldAllow)
            {
                await CopySingleFile(itemBeingCopied, file.FullName, tempPath);
            }
        }

        // If copying subdirectories, copy them and their contents to new location.
        if (copySubDirs)
        {
            DirectoryInfo[] dirs = dir.GetDirectories("*", enumerationOptions: _enumerationOptions);
            foreach (DirectoryInfo subDir in dirs)
            {
                if (HasBeenCanceled)
                {
                    return;
                }
                if (!_directoryPathsSeen.Contains(subDir.FullName))
                {
                    if (!string.IsNullOrWhiteSpace(_backupDirectory) && subDir.FullName.StartsWith(_backupDirectory))
                    {
                        continue; // don't back up backup directory
                    }
                    string temppath = Path.Combine(destDirName, subDir.Name);
                    if (ShouldAllowPath(excludedPaths, subDir.FullName))
                    {
                        await CopyDirectory(itemBeingCopied, subDir.FullName, temppath, copySubDirs, excludedPaths);
                    }
                    _directoryPathsSeen.Add(subDir.FullName);
                }
            }
        }
    }

    private Dictionary<string, ulong> GetFilePathsAndSizesInDirectory(string sourceDirName, bool searchSubDirs, List<string> excludedPaths = null)
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
        FileInfo[] files = dir.GetFiles("*", enumerationOptions: _enumerationOptions);
        foreach (FileInfo file in files)
        {
            if (HasBeenCanceled)
            {
                return fileList;
            }
            if (ShouldAllowPath(excludedPaths, file.FullName))
            {
                fileList.Add(file.FullName, (ulong)file.Length);
            }
        }

        // If copying subdirectories, search them and their contents 
        if (searchSubDirs)
        {
            DirectoryInfo[] dirs = dir.GetDirectories("*", enumerationOptions: _enumerationOptions);
            foreach (DirectoryInfo subDir in dirs)
            {
                if (HasBeenCanceled)
                {
                    return fileList;
                }
                if (!_directoryPathsSeen.Contains(subDir.FullName))
                {
                    if (!string.IsNullOrWhiteSpace(_backupDirectory) && subDir.FullName.StartsWith(_backupDirectory))
                    {
                        continue; // don't back up backup directory
                    }
                    if (ShouldAllowPath(excludedPaths, subDir.FullName))
                    {
                        var additionalFileInfo = GetFilePathsAndSizesInDirectory(subDir.FullName, searchSubDirs);
                        foreach (KeyValuePair<string, ulong> entry in additionalFileInfo)
                        {
                            fileList.Add(entry.Key, entry.Value);
                        }
                    }
                    _directoryPathsSeen.Add(subDir.FullName);
                }
            }
        }
        return fileList;
    }

    public string HashFile(string filePath)
    {
        try 
        {
            if (File.Exists(filePath))
            {
                if (Utilities.IsSymbolicLink(filePath) && !Utilities.SymbolicLinkFileExists(filePath))
                {
                    return "";
                }
                using SHA256 sha256 = SHA256.Create();
                using FileStream fileStream = File.OpenRead(filePath);
                return BitConverter.ToString(sha256.ComputeHash(fileStream)).Replace("-", "");
            }
        }
        catch {}
        return "";
    }

    // Modified from https://stackoverflow.com/a/33726939/3938401
    private async Task CopySingleFile(FolderFileItem itemBeingCopied, string source, string destination)
    {
        if (File.Exists(source))
        {
            if (Utilities.IsSymbolicLink(source) && !Utilities.SymbolicLinkFileExists(source))
            {
                _pathsCopiedTo.Add(destination, false);
                return;
            }
            if (HasBeenCanceled)
            {
                _pathsCopiedTo.Add(destination, false);
                return;
            }
            if (UsesCompressedFile)
            {
                _pathsCopiedTo.Add(destination, false);
            }
            else
            {
                AboutToProcessFile?.Invoke(source);
                var needsToCopy = true;
                if (IsIncremental && File.Exists(destination))
                {
                    var sourceHash = HashFile(source);
                    var outputHash = HashFile(destination);
                    needsToCopy = sourceHash != outputHash;
                }
                if (_pathsCopiedTo.ContainsKey(destination))
                {
                    needsToCopy = false; // have already processed this file (fixes any symlink fun)
                }
                if (needsToCopy)
                {
                    if (IsIncremental && File.Exists(destination))
                    {
                        // output file already exists. don't get rid of file in case
                        // something goes wrong during copy (that way we don't lose anything).
                        // remove old one after the fact.
                        // TODO: one improvement/setting could be to retain old files for X number of days.
                        File.Move(destination, destination + ".old");                            
                    }
                    // TODO: increase buffer size
                    // todo: see if better way to copy w/cancel
                    // https://stackoverflow.com/questions/7680640/how-to-copy-a-file-with-the-ability-to-cancel-the-copy
                    // https://stackoverflow.com/questions/882686/asynchronous-file-copy-move-in-c-sharp
                    var fileOptions = /*FileOptions.Asynchronous | */ FileOptions.SequentialScan;
                    var bufferSize = 0; // no buffer
                    using (var inStream = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, fileOptions))
                    using (var outStream = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.Read, bufferSize, fileOptions))
                    {
                        int buffer_size = 5 * 1024 * 1024; // 5 MB
                        byte[] buffer = new byte[buffer_size];
                        long total_read = 0;
                        while (total_read < inStream.Length)
                        {
                            if (HasBeenCanceled)
                            {
                                // stop copying file and erase anything in progress
                                // TODO: test
                                inStream.Close();
                                outStream.Close();
                                if (File.Exists(destination))
                                {
                                    File.Delete(destination);
                                }
                                break;
                            }
                            int read = await inStream.ReadAsync(buffer.AsMemory(0, buffer_size));
                            await outStream.WriteAsync(buffer.AsMemory(0, read));
                            CopiedBytesOfItem(itemBeingCopied, (ulong)read);
                            total_read += read;
                        }
                    }
                    if (IsIncremental && File.Exists(destination + ".old"))
                    {
                        File.Delete(destination + ".old"); // remove old file now that new one is in place
                    }
                    _pathsCopiedTo.Add(destination, true);
                }
                else
                {
                    CopiedBytesOfItem(itemBeingCopied, (ulong)new FileInfo(source).Length);
                    _pathsCopiedTo.Add(destination, true);
                }
            }
        }
        else
        {
            _pathsCopiedTo.Add(destination, false);
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

    private string GetBackupToolPath(string processPath)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            if (RuntimeInformation.OSArchitecture == Architecture.X86)
            {
                return Path.Combine(processPath, "Assets/tools/win-x86/7za.exe");
            }
            else if (RuntimeInformation.OSArchitecture == Architecture.X64)
            {
                return Path.Combine(processPath, "Assets/tools/win-x64/7za.exe");
            }
            else if (RuntimeInformation.OSArchitecture == Architecture.Arm64)
            {
                return Path.Combine(processPath, "Assets/tools/win-arm64/7za.exe");
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            if (Directory.Exists(Path.Combine(processPath, "Assets")))
            {
                return Path.Combine(processPath, "Assets/tools/macos/7zz");
            }
            return Path.Combine(processPath, "../Resources/Assets/tools/macos/7zz");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            if (RuntimeInformation.OSArchitecture == Architecture.X86)
            {
                return Path.Combine(processPath, "Assets/tools/linux-x86/7zzs");
            }
            else if (RuntimeInformation.OSArchitecture == Architecture.X64)
            {
                return Path.Combine(processPath, "Assets/tools/linux-x64/7zzs");
            }
            else if (RuntimeInformation.OSArchitecture == Architecture.Arm64)
            {
                return Path.Combine(processPath, "Assets/tools/linux-arm64/7zzs");
            }
        }
        return null;
    }

    private async Task BackupToCompressedFile(string destination, List<string> filePaths, 
        Dictionary<string, FolderFileItem> pathsToFolderFileItem, Dictionary<string, ulong> pathsToFileSize)
    {
        var quotedFilePaths = new List<string>();
        foreach (string filePath in filePaths)
        {
            quotedFilePaths.Add("\"" + filePath + "\"");
        }
        var is64BitOS = Utilities.Is64BitOS();
        Process process = new Process();
        var currentDir = AppContext.BaseDirectory;
        var exePath = GetBackupToolPath(Path.GetDirectoryName(Environment.ProcessPath));
        process.StartInfo.FileName = exePath;
        process.StartInfo.WorkingDirectory = Path.GetDirectoryName(Environment.ProcessPath);//AppContext.BaseDirectory;

        // https://stackoverflow.com/a/6522928/3938401
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.StandardOutputEncoding = Encoding.UTF8;

        //process.StartInfo.RedirectStandardError = true;
        process.EnableRaisingEvents = true;
        var didError = false;
        var itemSizeInBytes = 0UL;
        var remainingBytes = 0UL;
        var totalBytesToAdd = 0UL;
        var didStartCompressingFile = false;
        var lastPercent = 0.0;
        string currentFilePath = "";
        FolderFileItem currentItem = null;
        var bytesCopiedForItem = new Dictionary<FolderFileItem, ulong>();
        var didFinishCancel = false;
        var nextMessageIsError = false;
        string errorMessage = "";
        ulong currentFileNumber = 0;
        void HandleLine(string line)
        {
            // Console.WriteLine("Handling data: " + line);
            if (string.IsNullOrWhiteSpace(line) || didFinishCancel) // in case more events come through
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
            if (totalBytesToAdd == 0 && line.Contains("files") && line.Contains("bytes"))
            {
                // line format:  13 files, 158632881 bytes (152 MiB)
                var regex = @"(\d+)\s*(?:files,\s*)(\d+)\s*(?:bytes)\s*\((\d+)(?:\s*\S+)";
                // group 1: files, group 2: bytes, group 3: MiB (or probably other sizing, unused by this code)
                var match = Regex.Match(line, regex);
                if (match.Groups.Count >= 3)
                {
                    ulong.TryParse(match.Groups[2].ToString(), out totalBytesToAdd);
                    // Console.WriteLine("total bytes to add is: {0}", totalBytesToAdd);
                }
            }
            else if (line.Contains('+') && line.Trim() != "+")
            {
                didStartCompressingFile = true;
                currentFilePath = line.Substring(line.IndexOf('+')).Substring(2).Trim();
                AboutToProcessFile?.Invoke(currentFilePath);
                //Console.WriteLine("Handling line with + in it");
                //Console.WriteLine("Curr file path {0}", currentFilePath);
                //Console.WriteLine("Curr item {0}", currentItem?.Path);
                //Console.WriteLine("Curr item is dir {0}", currentItem?.IsDirectory);
                //Console.WriteLine("Curr item byte size {0}", currentItem?.ByteSize);
                if (currentItem != null && remainingBytes > 0)
                {
                    // cleanup previous item since we've moved on to something else (previous one is done)
                    CopiedBytesOfItem(currentItem, remainingBytes);
                    bytesCopiedForItem[currentItem] += remainingBytes;
                    if (!currentItem.IsDirectory)
                    {
                        FinishedCopyingItem?.Invoke(currentItem);
                    }
                    else
                    {
                        // ... >= just in case....paranoia...should be fine...
                        if (bytesCopiedForItem[currentItem] >= currentItem.ByteSize)
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
                    itemSizeInBytes = remainingBytes = pathsToFileSize[currentFilePath];
                }
                else
                {
                    itemSizeInBytes = remainingBytes = 0;
                }
            }
            else if (line.Contains('%') && didStartCompressingFile)
            {
                var percentAndFileNumberParts = line.Split('%');
                var prevCurrFileNumber = currentFileNumber;
                if (percentAndFileNumberParts.Length > 1 && !string.IsNullOrWhiteSpace(percentAndFileNumberParts[1]))
                {
                    currentFileNumber = ulong.Parse(percentAndFileNumberParts[1].Trim());
                }
                else
                {
                    currentFileNumber = 0;
                }
                var percent = double.Parse(percentAndFileNumberParts[0].Trim());
                // percent is an overall percentage of all bytes to copy, not % of individual item
                // (at least, so it seems!)
                var percentCopiedSinceLastUpdate = percent - lastPercent;
                // Console.WriteLine("Processing % with currentFileNumber {0}; percent copied = {1}",
                //     currentFileNumber, percentCopiedSinceLastUpdate);
                lastPercent = percent;
                 // use floor for number of copied bytes -- would rather underestimate than overestimate
                var copiedBytes = Math.Floor(percentCopiedSinceLastUpdate / 100.0 * totalBytesToAdd);
                if (currentItem != null)
                {
                    CopiedBytesOfItem(currentItem, (ulong)copiedBytes);
                    bytesCopiedForItem[currentItem] += (ulong)copiedBytes;
                }
                remainingBytes -= (ulong)copiedBytes;
            }
            else if (line.Contains("Error:"))
            {
                nextMessageIsError = true;
            }
            else if (nextMessageIsError)
            {
                errorMessage = line;
                didError = true;
                nextMessageIsError = false;
            }
        }
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

        // because 7zip writes output without newlines for file-based progress percentages, 
        // we must stream and handle all data ourselves.
        // see also: https://stackoverflow.com/q/33716580/3938401
        var outputStream = process.StandardOutput;
        char[] buffer = new char[256];
        int cch;

        var wipLine = "";
        var receivingDeleteChars = false;
        // TODO: async this somehow so UI updates work
        while ((cch = await outputStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            // use file numbers to see if this is the start of a new file or continuing progress on a file we have
            // already started looking at.
            // that will let us know what progress is.
            // for getting lines, see also: https://stackoverflow.com/a/34055661/3938401
            var str = new string(buffer, 0, cch);
            str = str.Replace( "\r\r\n", "\r\n");
            // str = str.Replace("\b", "").Trim();

            char? prev = null;
            foreach (var ch in str)
            {
                // TODO: handle newlines on other OS
                if (receivingDeleteChars && ch != '\b')
                {
                    receivingDeleteChars = false;
                    // Console.WriteLine("stopped receiving del chars; line is: " + wipLine);
                    HandleLine(wipLine.Trim());
                    wipLine = "";
                }
                //if (ch == '\r')
                //{
                //    // Console.WriteLine("return carraige");
                //}
                if (ch == '\b' && !receivingDeleteChars)
                {
                    // Console.WriteLine("delete char, line is : {0}", wipLine);
                    receivingDeleteChars = true;
                    // check current line. are we just a % and a file number?
                    // if so, get our file number and see if we are working
                    // on a file we've already seen (we saw the + symbol and the path),
                    // aka progress on a large file,
                    // or if it's info on a new file.
                    // for progress on file, looks like:
                    // 18% 11       + /MyBigFile.pdf
                    // (builds string...)
                    // deleting characters found with existing string: 23% 12
                    // (note the increment of the file number!)
                    // continue;

                    // if (wipLine.Contains('%'))
                    // {
                    //     var parts = wipLine.Split('%');
                    //     var percent = parts[0].Trim();
                    //     var fileNumber = parts[1].Trim();
                    // }
                }
                if (ch == '\n')
                {
                    // Console.WriteLine("line feed");
                    // this is a newline, cut off string and send to processor
                    HandleLine(wipLine.Trim());
                    wipLine = "";
                }
                else if (ch != '\b')
                {
                    wipLine += ch; // build up current string line
                }
                prev = ch;
            }
        }
        // handle final line/any remaining text
        HandleLine(wipLine);

        // process.BeginOutputReadLine();
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

    private async Task IterateOverFiles(List<FolderFileItem> paths, string backupDirectory)
    {
        try
        {
            HadError = false;
            IsRunning = true;
            if (Directory.Exists(backupDirectory) || _isCalculatingFileSize)
            {
                var backupName = "backup-" + DateTime.Now.ToString("yyyy-MM-dd-H-mm-ss");
                var originalBackupDir = backupDirectory;
                backupDirectory = Path.Combine(backupDirectory, "easy-backup", backupName);
                if (IsIncremental)
                {
                    backupName = "backup-incremental";
                    backupDirectory = Path.Combine(originalBackupDir, "easy-backup", backupName);
                }
                if (!Directory.Exists(backupDirectory) && !_isCalculatingFileSize)
                {
                    Directory.CreateDirectory(backupDirectory);
                }
                else if (!_isCalculatingFileSize && !IsIncremental)
                {
                    // ok, somehow they started two backups within the same second >_> wait 1 second and start again
                    await Task.Delay(1000);
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
                _backupDirectory = backupDirectory;
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
                        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                        {
                            // Do something
                            directoryName = directoryName.Replace(pathRoot, "");
                            directoryName = Path.Combine(pathRoot.Replace(":\\", ""), directoryName);
                        }
                        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || 
                                 RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                        {
                            if (directoryName.StartsWith(pathRoot))
                            {
                                directoryName = directoryName.Substring(1);
                            }
                        }
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
                                var latestFile = directoryInfo.GetFiles("*", enumerationOptions: _enumerationOptions)
                                    .OrderByDescending(x => x.LastWriteTimeUtc).FirstOrDefault();
                                if (latestFile != null)
                                {
                                    if (_isCalculatingFileSize)
                                    {
                                        var fileInfo = new FileInfo(latestFile.FullName);
                                        AboutToProcessFile?.Invoke(fileInfo.FullName);
                                        CalculatedBytesOfItem?.Invoke(item, (ulong)fileInfo.Length);
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
                                        await CopySingleFile(item, latestFile.FullName, outputPath);
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
                                await CopyDirectory(item, item.Path, outputPath, item.IsRecursive, item.ExcludedPaths);
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
                                await CopySingleFile(item, item.Path, outputPath);
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
                                var latestFile = directoryInfo.GetFiles("*", enumerationOptions: _enumerationOptions)
                                    .OrderByDescending(x => x.LastWriteTimeUtc).FirstOrDefault();
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
                                var filesWithSizesInDirectory = GetFilePathsAndSizesInDirectory(item.Path, item.IsRecursive, item.ExcludedPaths);
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
                        await BackupToCompressedFile(Path.Combine(backupDirectory, backupName + ".7z"), filePaths, pathsToFolderFileItem, pathToFileSize);
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

    public async Task CalculateBackupSize(List<FolderFileItem> paths, string backupDirectory)
    {
        _directoryPathsSeen = new List<string>();
        _isCalculatingFileSize = true;
        await IterateOverFiles(paths, backupDirectory);
        _isCalculatingFileSize = false;
    }

    public async Task PerformBackup(List<FolderFileItem> paths, string backupDirectory)
    {
        _directoryPathsSeen = new List<string>();
        await IterateOverFiles(paths, backupDirectory);
    }
}
