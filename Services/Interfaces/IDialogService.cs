using FileCraft.Models;
using FileCraft.ViewModels.Functional;
using FileCraft.ViewModels.Shared;
using System.Collections.Generic;

namespace FileCraft.Services.Interfaces
{
    public interface IDialogService
    {
        string? SelectFolder(string description);
        string? SelectFile(string title, string filter);
        void ShowNotification(string title, string message, DialogIconType iconType);
        bool ShowConfirmation(string title, string message, DialogIconType iconType, int? filesAffected = null);
        bool ShowCopyTreeConfirmation(string title, DialogIconType iconType, CopyTreeConfirmationViewModel contentViewModel);
        ExitConfirmationResult ShowExitConfirmation(string title, string message);
        string? ShowRenamePresetDialog(string currentName, int presetNumber);
        string? ShowEditIgnoredFoldersDialog(string currentFolders);
        bool ShowBulkSearchDialog(IEnumerable<SelectableFile> allFiles);
        IEnumerable<string>? ShowIgnoredCommentsDialog(IEnumerable<SelectableFile> selectedFiles, IEnumerable<string> previouslyIgnoredFiles);
        void ShowPathPresetsManager();
        string? ShowInputStringDialog(string title, string message, string defaultValue = "");
        void ShowTextContentDialog(string title, string content);
        void ShowPresetLoadSummary(PathPresetLoadResult result);
        void ShowPresetDetails(PresetItemViewModel preset);
    }
}
