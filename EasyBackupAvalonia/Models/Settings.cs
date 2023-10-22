using EasyBackupAvalonia.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace EasyBackupAvalonia.Models
{
    class Settings
    {
        public static int VersionNumber => 2;

        private class SettingsData
        {
            public int LastSeenVersionNumber { get; set; }
            public string LastUsedBackupTemplatePath { get; set; }
            public bool PlaySoundsOnComplete { get; set; }
            public bool SavesToCompressedFile { get; set; }
            public bool CompressedFileUsesPassword { get; set; }

            public SettingsData()
            {
                LastSeenVersionNumber = Settings.VersionNumber;
                LastUsedBackupTemplatePath = "";
                PlaySoundsOnComplete = true;
                SavesToCompressedFile = false;
                CompressedFileUsesPassword = false;
            }
        }

        private static SettingsData _settings;

        private Settings()
        {
        }

        public static void Init()
        {
            LoadSettings();
        }

        public static string LastUsedBackupTemplatePath 
        { 
            get => _settings.LastUsedBackupTemplatePath; 
            set
            {
                _settings.LastUsedBackupTemplatePath = value;
                SaveSettings();
            }
        }

        public static bool PlaySoundsOnComplete 
        { 
            get => _settings.PlaySoundsOnComplete; 
            set
            {
                _settings.PlaySoundsOnComplete = value;
                SaveSettings();
            }
        }

        public static bool SavesToCompressedFile 
        { 
            get => _settings.SavesToCompressedFile; 
            set
            {
                _settings.SavesToCompressedFile = value;
                SaveSettings();
            }
        }

        public static bool CompressedFileUsesPassword 
        { 
            get => _settings.CompressedFileUsesPassword; 
            set
            {
                _settings.CompressedFileUsesPassword = value;
                SaveSettings();
            }
        }

        private static void LoadSettings()
        {
            var path = Utilities.SaveFileLocation;
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                _settings = JsonSerializer.Deserialize<SettingsData>(json);
            }
            else
            {
                _settings = new SettingsData();
            }
        }

        public static async void SaveSettings()
        {
            var path = Utilities.SaveFileLocation;
            using FileStream fileStream = File.Create(path);
            await JsonSerializer.SerializeAsync<SettingsData>(fileStream, _settings);
            await fileStream.DisposeAsync();
        }
    }
}