using FileCraft.Models;
using FileCraft.Services.Interfaces;
using FileCraft.Shared.Helpers;
using FileCraft.ViewModels.Functional;
using FileCraft.ViewModels.Shared;
using FileCraft.Views.Shared;
using System;
using System.Collections.Generic;

namespace FileCraft.Services
{
    public class DialogService : IDialogService
    {
        private readonly IWindowFactory _windowFactory;

        public DialogService(IWindowFactory windowFactory)
        {
            _windowFactory = windowFactory;
        }

        public string? SelectFolder(string description)
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = description
            };

            if (dialog.ShowDialog() == true)
            {
                return dialog.FolderName;
            }

            return null;
        }

        public string? SelectFile(string title, string filter)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = title,
                Filter = filter
            };

            if (dialog.ShowDialog() == true)
            {
                return dialog.FileName;
            }

            return null;
        }

        public void ShowNotification(string title, string message, DialogIconType iconType)
        {
            var iconDef = GetAppIcon(iconType);
            var viewModel = new GenericDialogViewModel
            {
                Title = title,
                Message = message,
                IconGlyph = iconDef.Glyph,
                IconBrush = iconDef.Brush,
                PrimaryButtonText = ResourceHelper.GetString("Common_OkButton"),
                PrimaryButtonStyle = ResourceKeys.PrimaryButton
            };
            var window = new GenericDialogWindow(viewModel);
            window.ShowDialog();
        }

        public bool ShowConfirmation(string title, string message, DialogIconType iconType, int? filesAffected = null)
        {
            var iconDef = GetAppIcon(iconType);
            var viewModel = new GenericDialogViewModel
            {
                Title = title,
                Message = message,
                IconGlyph = iconDef.Glyph,
                IconBrush = iconDef.Brush,
                FilesAffected = filesAffected,
                PrimaryButtonText = ResourceHelper.GetString("Common_YesButton"),
                PrimaryButtonStyle = ResourceKeys.PrimaryButton,
                SecondaryButtonText = ResourceHelper.GetString("Common_CancelButton")
            };
            var window = new GenericDialogWindow(viewModel);
            return window.ShowDialog() ?? false;
        }

        public bool ShowCopyTreeConfirmation(string title, DialogIconType iconType, CopyTreeConfirmationViewModel contentViewModel)
        {
            var iconDef = GetAppIcon(iconType);

            var viewModel = new GenericDialogViewModel
            {
                Title = title,
                IconGlyph = iconDef.Glyph,
                IconBrush = iconDef.Brush,
                CustomContent = contentViewModel,
                PrimaryButtonText = ResourceHelper.GetString("Common_YesButton"),
                PrimaryButtonStyle = ResourceKeys.WarningButton,
                SecondaryButtonText = ResourceHelper.GetString("Common_CancelButton")
            };

            var window = new GenericDialogWindow(viewModel);
            return window.ShowDialog() ?? false;
        }

        public ExitConfirmationResult ShowExitConfirmation(string title, string message)
        {
            var iconDef = AppIcons.Warning;
            var viewModel = new GenericDialogViewModel
            {
                Title = title,
                Message = message,
                IconGlyph = iconDef.Glyph,
                IconBrush = iconDef.Brush,
                PrimaryButtonText = ResourceHelper.GetString("Common_SaveButton"),
                PrimaryButtonStyle = ResourceKeys.PrimaryButton,
                TertiaryButtonText = ResourceHelper.GetString("ExitConfirmation_DontSaveButton"),
                TertiaryButtonStyle = ResourceKeys.DangerButton,
                SecondaryButtonText = ResourceHelper.GetString("Common_CancelButton")
            };

            var window = new GenericDialogWindow(viewModel);
            window.ShowDialog();
            return viewModel.ExitResult;
        }

        public string? ShowRenamePresetDialog(string currentName, int presetNumber)
        {
            var viewModel = new GenericDialogViewModel
            {
                Title = ResourceHelper.GetString("RenamePreset_Title"),
                Message = string.Format(ResourceHelper.GetString("RenamePreset_Prompt"), presetNumber),
                IsInputVisible = true,
                IsMultiline = false,
                InputText = currentName,
                PrimaryButtonText = ResourceHelper.GetString("Common_OkButton"),
                PrimaryButtonStyle = ResourceKeys.PrimaryButton,
                SecondaryButtonText = ResourceHelper.GetString("Common_CancelButton")
            };

            var window = new GenericDialogWindow(viewModel);
            if (window.ShowDialog() == true)
            {
                return viewModel.InputText;
            }
            return null;
        }

        public string? ShowEditIgnoredFoldersDialog(string currentFolders)
        {
            var viewModel = new GenericDialogViewModel
            {
                Title = ResourceHelper.GetString("EditIgnored_Title"),
                Message = ResourceHelper.GetString("EditIgnored_Instruction"),
                IsInputVisible = true,
                IsMultiline = true,
                InputText = currentFolders,
                PrimaryButtonText = ResourceHelper.GetString("Common_OkButton"),
                PrimaryButtonStyle = ResourceKeys.PrimaryButton,
                SecondaryButtonText = ResourceHelper.GetString("Common_CancelButton")
            };

            var window = new GenericDialogWindow(viewModel);
            if (window.ShowDialog() == true)
            {
                return viewModel.InputText;
            }
            return null;
        }

        public bool ShowBulkSearchDialog(IEnumerable<SelectableFile> allFiles)
        {
            var iconDef = AppIcons.Info;
            var viewModel = new BulkSearchViewModel(allFiles, iconDef.Glyph, iconDef.Brush, this);
            var bulkSearchWindow = new BulkSearchWindow(viewModel);
            return bulkSearchWindow.ShowDialog() ?? false;
        }

        public IEnumerable<string>? ShowIgnoredCommentsDialog(IEnumerable<SelectableFile> selectedFiles, IEnumerable<string> previouslyIgnoredFiles)
        {
            var viewModel = new IgnoredCommentsViewModel(selectedFiles, previouslyIgnoredFiles);
            var window = new IgnoredCommentsWindow(viewModel);
            if (window.ShowDialog() == true)
            {
                return window.GetIgnoredFilePaths();
            }
            return null;
        }

        public void ShowPathPresetsManager()
        {
            _windowFactory.ShowPathPresetsManagerDialog();
        }

        public string? ShowInputStringDialog(string title, string message, string defaultValue = "")
        {
            var viewModel = new GenericDialogViewModel
            {
                Title = title,
                Message = message,
                IsInputVisible = true,
                IsMultiline = false,
                InputText = defaultValue,
                PrimaryButtonText = ResourceHelper.GetString("Common_OkButton"),
                PrimaryButtonStyle = ResourceKeys.PrimaryButton,
                SecondaryButtonText = ResourceHelper.GetString("Common_CancelButton")
            };

            var window = new GenericDialogWindow(viewModel);
            if (window.ShowDialog() == true)
            {
                return viewModel.InputText;
            }
            return null;
        }

        public void ShowTextContentDialog(string title, string content)
        {
            var viewModel = new ViewContentViewModel(title, content);
            var window = new ViewContentWindow(viewModel);
            window.ShowDialog();
        }

        public void ShowPresetLoadSummary(PathPresetLoadResult result)
        {
            _windowFactory.ShowPresetLoadSummaryDialog(result);
        }

        public void ShowPresetDetails(PresetItemViewModel preset)
        {
            var viewModel = new PresetDetailsViewModel(preset);
            var window = new PresetDetailsWindow(viewModel);
            window.ShowDialog();
        }

        private IconDefinition GetAppIcon(DialogIconType iconType)
        {
            return iconType switch
            {
                DialogIconType.Info => AppIcons.Info,
                DialogIconType.Warning => AppIcons.Warning,
                DialogIconType.Success => AppIcons.Success,
                DialogIconType.Error => AppIcons.Error,
                _ => AppIcons.Info
            };
        }
    }
}
