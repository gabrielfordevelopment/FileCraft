using FileCraft.Models;
using System.IO;

namespace FileCraft.Shared.Helpers
{
    public static class SaveDataPathConverter
    {
        public static SaveData ConvertPaths(SaveData originalData, string basePath, bool toRelative)
        {
            Func<string, string> convertPath = path =>
            {
                if (string.IsNullOrWhiteSpace(path)) return string.Empty;
                return toRelative
                    ? Path.GetRelativePath(basePath, path)
                    : Path.GetFullPath(Path.Combine(basePath, path));
            };

            return new SaveData
            {
                PresetName = originalData.PresetName,
                LastModified = originalData.LastModified,
                SourcePath = toRelative ? "." : basePath,
                DestinationPath = convertPath(originalData.DestinationPath),
                SelectedTabIndex = originalData.SelectedTabIndex,

                FileContentExport = new FileContentExportSettings
                {
                    OutputFileName = originalData.FileContentExport.OutputFileName,
                    AppendTimestamp = originalData.FileContentExport.AppendTimestamp,
                    SelectedExtensions = originalData.FileContentExport.SelectedExtensions?.ToList() ?? new List<string>(),
                    SelectedFilePaths = originalData.FileContentExport.SelectedFilePaths?.Select(convertPath).ToList() ?? new List<string>(),
                    IgnoredCommentFilePaths = originalData.FileContentExport.IgnoredCommentFilePaths?.Select(convertPath).ToList() ?? new List<string>(),
                    FolderTreeState = originalData.FileContentExport.FolderTreeState?.Select(s => new FolderState
                    {
                        FullPath = convertPath(s.FullPath),
                        IsSelected = s.IsSelected,
                        IsExpanded = s.IsExpanded
                    }).ToList() ?? new List<FolderState>()
                },

                FolderContentExport = new FolderContentExportSettings
                {
                    OutputFileName = originalData.FolderContentExport.OutputFileName,
                    AppendTimestamp = originalData.FolderContentExport.AppendTimestamp,
                    SelectedColumns = originalData.FolderContentExport.SelectedColumns?.ToList() ?? new List<string>(),
                    ProtectSpreadsheetFormulas = originalData.FolderContentExport.ProtectSpreadsheetFormulas,
                    FolderTreeState = originalData.FolderContentExport.FolderTreeState?.Select(s => new FolderState
                    {
                        FullPath = convertPath(s.FullPath),
                        IsSelected = s.IsSelected,
                        IsExpanded = s.IsExpanded
                    }).ToList() ?? new List<FolderState>()
                },

                TreeGenerator = new TreeGeneratorSettings
                {
                    OutputFileName = originalData.TreeGenerator.OutputFileName,
                    AppendTimestamp = originalData.TreeGenerator.AppendTimestamp,
                    GenerationMode = originalData.TreeGenerator.GenerationMode,
                    FolderTreeState = originalData.TreeGenerator.FolderTreeState?.Select(s => new FolderState
                    {
                        FullPath = convertPath(s.FullPath),
                        IsSelected = s.IsSelected,
                        IsExpanded = s.IsExpanded
                    }).ToList() ?? new List<FolderState>()
                },

                SettingsPage = new SettingsPageSettings
                {
                    IgnoredFolders = originalData.SettingsPage.IgnoredFolders?.ToList() ?? new List<string>(),
                    LinkedFolderTreeGroups = originalData.SettingsPage.LinkedFolderTreeGroups?.Select(g => g.ToList()).ToList() ?? new List<List<string>>()
                },

                PathPresetSortBy = originalData.PathPresetSortBy,
                PathPresetIsDescending = originalData.PathPresetIsDescending,
                SavePresetSortBy = originalData.SavePresetSortBy,
                SavePresetIsDescending = originalData.SavePresetIsDescending
            };
        }
    }
}
