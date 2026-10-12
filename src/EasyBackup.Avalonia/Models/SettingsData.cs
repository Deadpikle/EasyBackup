namespace EasyBackupAvalonia.Models
{
    public class SettingsData
    {
        public int LastSeenVersionNumber { get; set; }
        public string LastUsedBackupTemplatePath { get; set; }
        public bool PlaySoundsOnComplete { get; set; }
        public bool SavesToCompressedFile { get; set; }
        public bool CompressedFileUsesPassword { get; set; }

        public SettingsData()
        {
            LastSeenVersionNumber = 0;
            LastUsedBackupTemplatePath = "";
            PlaySoundsOnComplete = true;
            SavesToCompressedFile = false;
            CompressedFileUsesPassword = false;
        }
    }
}