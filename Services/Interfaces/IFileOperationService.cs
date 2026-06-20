using FileCraft.Models;

namespace FileCraft.Services.Interfaces
{
    public interface IFileOperationService
    {
        Task<string> GenerateTreeStructureAsync(string sourcePath, string destinationPath, ISet<string> excludedFolders, string outputFileName, TreeGenerationMode mode);

        Task<string> ExportFolderContentsAsync(string destinationPath, IEnumerable<string> includedFolderPaths, string outputFileName, IEnumerable<string> selectedColumns, FolderContentExportOptions options);

        Task<FileExportResult> ExportSelectedFileContentsAsync(string destinationPath, IEnumerable<SelectableFile> selectedFiles, string outputFileName, ISet<string> filesToIgnoreXmlComments);
    }
}
