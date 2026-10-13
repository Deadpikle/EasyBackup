using YasashiiBackupAvalonia.Models;
using System.Text.Json.Serialization;

namespace YasashiiBackupAvalonia.Helpers;

[JsonSerializable(typeof(BackupTemplate))]
[JsonSerializable(typeof(SettingsData))]
internal partial class SourceGenerationContext : JsonSerializerContext { }
