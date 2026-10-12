using EasyBackupAvalonia.Models;
using System.Text.Json.Serialization;

namespace EasyBackupAvalonia.Helpers
{
    [JsonSerializable(typeof(BackupTemplate))]
    [JsonSerializable(typeof(SettingsData))]
    internal partial class SourceGenerationContext : JsonSerializerContext { }
}
