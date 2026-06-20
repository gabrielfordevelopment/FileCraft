using FileCraft.Shared.Helpers;

namespace FileCraft.Models
{
    public enum FolderExportColumnGroup
    {
        FileDetails,
        AudioTags,
        AudioProperties,
        Diagnostics
    }

    public sealed class FolderExportColumnDefinition
    {
        public required string Id { get; init; }
        public required string DisplayNameResourceKey { get; init; }
        public required string DefaultDisplayName { get; init; }
        public FolderExportColumnGroup Group { get; init; }
        public bool IsDefaultSelected { get; init; }
        public bool RequiresAudioMetadata { get; init; }

        public string DisplayName
        {
            get
            {
                var value = ResourceHelper.GetString(DisplayNameResourceKey);
                return value == DisplayNameResourceKey ? DefaultDisplayName : value;
            }
        }
    }

    public static class FolderExportColumnIds
    {
        public const string FileName = "file.name";
        public const string FileSizeBytes = "file.sizeBytes";
        public const string FileCreationTime = "file.creationTime";
        public const string FileLastWriteTime = "file.lastWriteTime";
        public const string FileLastAccessTime = "file.lastAccessTime";
        public const string FileIsReadOnly = "file.isReadOnly";
        public const string FileAttributes = "file.attributes";
        public const string FileFullPath = "file.fullPath";
        public const string FileParent = "file.parent";
        public const string FileExtension = "file.extension";

        public const string AudioTitle = "audio.title";
        public const string AudioArtist = "audio.artist";
        public const string AudioAlbum = "audio.album";
        public const string AudioAlbumArtist = "audio.albumArtist";
        public const string AudioYear = "audio.year";
        public const string AudioGenre = "audio.genre";
        public const string AudioTrackNumber = "audio.trackNumber";
        public const string AudioDuration = "audio.duration";

        public const string AudioIsAudioFile = "audio.isAudioFile";
        public const string AudioMetadataStatus = "audio.metadataStatus";
        public const string AudioMetadataError = "audio.metadataError";
    }

    public static class FolderExportColumns
    {
        private static readonly IReadOnlyList<FolderExportColumnDefinition> _all = new List<FolderExportColumnDefinition>
        {
            File(FolderExportColumnIds.FileName, "FolderContent_Column_FileName", "Name", true),
            File(FolderExportColumnIds.FileSizeBytes, "FolderContent_Column_FileSizeBytes", "Size (byte)", true),
            File(FolderExportColumnIds.FileCreationTime, "FolderContent_Column_FileCreationTime", "Creation time", true),
            File(FolderExportColumnIds.FileLastWriteTime, "FolderContent_Column_FileLastWriteTime", "Last write time", true),
            File(FolderExportColumnIds.FileLastAccessTime, "FolderContent_Column_FileLastAccessTime", "Last access time", true),
            File(FolderExportColumnIds.FileIsReadOnly, "FolderContent_Column_FileIsReadOnly", "Is read-only", true),
            File(FolderExportColumnIds.FileAttributes, "FolderContent_Column_FileAttributes", "Attributes", true),
            File(FolderExportColumnIds.FileFullPath, "FolderContent_Column_FileFullPath", "Full path", true),
            File(FolderExportColumnIds.FileParent, "FolderContent_Column_FileParent", "Parent", true),
            File(FolderExportColumnIds.FileExtension, "FolderContent_Column_FileExtension", "Format", true),

            AudioTag(FolderExportColumnIds.AudioTitle, "FolderContent_Column_AudioTitle", "Audio title"),
            AudioTag(FolderExportColumnIds.AudioArtist, "FolderContent_Column_AudioArtist", "Audio artist"),
            AudioTag(FolderExportColumnIds.AudioAlbum, "FolderContent_Column_AudioAlbum", "Album"),
            AudioTag(FolderExportColumnIds.AudioAlbumArtist, "FolderContent_Column_AudioAlbumArtist", "Album artist"),
            AudioTag(FolderExportColumnIds.AudioYear, "FolderContent_Column_AudioYear", "Year"),
            AudioTag(FolderExportColumnIds.AudioGenre, "FolderContent_Column_AudioGenre", "Genre"),
            AudioTag(FolderExportColumnIds.AudioTrackNumber, "FolderContent_Column_AudioTrackNumber", "Track number"),
            AudioProperty(FolderExportColumnIds.AudioDuration, "FolderContent_Column_AudioDuration", "Duration"),

            Diagnostic(FolderExportColumnIds.AudioIsAudioFile, "FolderContent_Column_AudioIsAudioFile", "Is audio file"),
            Diagnostic(FolderExportColumnIds.AudioMetadataStatus, "FolderContent_Column_AudioMetadataStatus", "Metadata status"),
            Diagnostic(FolderExportColumnIds.AudioMetadataError, "FolderContent_Column_AudioMetadataError", "Metadata error")
        };

        public static IReadOnlyList<FolderExportColumnDefinition> All => _all;

        public static IReadOnlyDictionary<string, FolderExportColumnDefinition> ById { get; } =
            _all.ToDictionary(column => column.Id, StringComparer.Ordinal);

        public static List<string> GetDefaultSelectedColumnIds()
        {
            return _all.Where(column => column.IsDefaultSelected).Select(column => column.Id).ToList();
        }

        public static string GetDisplayName(string columnId)
        {
            return ById.TryGetValue(columnId, out var definition) ? definition.DisplayName : columnId;
        }

        private static FolderExportColumnDefinition File(string id, string resourceKey, string defaultDisplayName, bool isDefaultSelected)
        {
            return new FolderExportColumnDefinition
            {
                Id = id,
                DisplayNameResourceKey = resourceKey,
                DefaultDisplayName = defaultDisplayName,
                Group = FolderExportColumnGroup.FileDetails,
                IsDefaultSelected = isDefaultSelected
            };
        }

        private static FolderExportColumnDefinition AudioTag(string id, string resourceKey, string defaultDisplayName)
        {
            return new FolderExportColumnDefinition
            {
                Id = id,
                DisplayNameResourceKey = resourceKey,
                DefaultDisplayName = defaultDisplayName,
                Group = FolderExportColumnGroup.AudioTags,
                RequiresAudioMetadata = true
            };
        }

        private static FolderExportColumnDefinition AudioProperty(string id, string resourceKey, string defaultDisplayName)
        {
            return new FolderExportColumnDefinition
            {
                Id = id,
                DisplayNameResourceKey = resourceKey,
                DefaultDisplayName = defaultDisplayName,
                Group = FolderExportColumnGroup.AudioProperties,
                RequiresAudioMetadata = true
            };
        }

        private static FolderExportColumnDefinition Diagnostic(string id, string resourceKey, string defaultDisplayName)
        {
            return new FolderExportColumnDefinition
            {
                Id = id,
                DisplayNameResourceKey = resourceKey,
                DefaultDisplayName = defaultDisplayName,
                Group = FolderExportColumnGroup.Diagnostics,
                RequiresAudioMetadata = true
            };
        }
    }

    public sealed class FolderContentExportOptions
    {
        public bool ProtectSpreadsheetFormulas { get; init; }
    }
}
