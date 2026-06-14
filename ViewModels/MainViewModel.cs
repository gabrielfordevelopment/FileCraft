using FileCraft.Models;
using FileCraft.Services.Interfaces;
using FileCraft.Shared.Helpers;
using FileCraft.ViewModels.Functional;
using FileCraft.ViewModels.Shared;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows.Input;

namespace FileCraft.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly ISaveService _saveService;
        private readonly ISharedStateService _sharedStateService;
        private readonly IDialogService _dialogService;
        private readonly IFolderTreeLinkService _folderTreeLinkService;

        public PathSelectionViewModel PathSelectionVM { get; }
        public SessionHistoryViewModel SessionHistoryVM { get; }

        private int _selectedTabIndex;
        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set
            {
                if (_selectedTabIndex != value)
                {
                    _selectedTabIndex = value;
                    OnPropertyChanged();
                }
            }
        }

        public FileContentExportViewModel FileContentExportVM { get; }
        public TreeGeneratorViewModel TreeGeneratorVM { get; }
        public FolderContentExportViewModel FolderContentExportVM { get; }
        public CsvViewerViewModel CsvViewerVM { get; }
        public OptionsViewModel OptionsVM { get; }

        public MainViewModel(
            ISaveService saveService,
            ISharedStateService sharedStateService,
            IDialogService dialogService,
            IFolderTreeLinkService folderTreeLinkService,
            FileContentExportViewModel fileContentExportVM,
            TreeGeneratorViewModel treeGeneratorVM,
            FolderContentExportViewModel folderContentExportVM,
            CsvViewerViewModel csvViewerVM,
            OptionsViewModel optionsVM,
            PathSelectionViewModel pathSelectionVM,
            SessionHistoryViewModel sessionHistoryVM)
        {
            _saveService = saveService;
            _sharedStateService = sharedStateService;
            _dialogService = dialogService;
            _folderTreeLinkService = folderTreeLinkService;

            FileContentExportVM = fileContentExportVM;
            TreeGeneratorVM = treeGeneratorVM;
            FolderContentExportVM = folderContentExportVM;
            CsvViewerVM = csvViewerVM;
            OptionsVM = optionsVM;
            PathSelectionVM = pathSelectionVM;
            SessionHistoryVM = sessionHistoryVM;

            FileContentExportVM.FolderTreeManager.Id = "FileContentExport";
            TreeGeneratorVM.FolderTreeManager.Id = "TreeGenerator";
            FolderContentExportVM.FolderTreeManager.Id = "FolderContentExport";

            _folderTreeLinkService.RegisterManager(FileContentExportVM.FolderTreeManager.Id, FileContentExportVM.FolderTreeManager);
            _folderTreeLinkService.RegisterManager(TreeGeneratorVM.FolderTreeManager.Id, TreeGeneratorVM.FolderTreeManager);
            _folderTreeLinkService.RegisterManager(FolderContentExportVM.FolderTreeManager.Id, FolderContentExportVM.FolderTreeManager);

            _folderTreeLinkService.OnLinksChanged += UpdateLinkedTabsVisuals;

            SessionHistoryVM.GetCurrentStateFunc = GetCurrentSaveData;
            SessionHistoryVM.ApplyStateFuncAsync = ApplyAllData;

            PathSelectionVM.SourcePathUpdated += async (path) => await UpdateAllManagersWithPath(path);
            PathSelectionVM.PathsCleared += async () => await UpdateAllManagersWithPath(string.Empty);

            SubscribeToChanges();

            OptionsVM.IgnoredFoldersChanged += OnIgnoredFoldersChanged;

            OptionsVM.PresetCreateRequested += OnPresetCreateRequested;
            OptionsVM.PresetLoadRequested += OnPresetLoadRequested;
            OptionsVM.PresetOverwriteRequested += OnPresetOverwriteRequested;
            OptionsVM.CurrentSaveDeleteRequested += OnCurrentSaveDeleteRequested;
        }

        public async Task InitializeAsync()
        {
            IsBusy = true;
            SessionHistoryVM.ClearHistory();
            SaveData saveData = _saveService.LoadSaveData();
            await ApplyAllData(saveData);
            SelectedTabIndex = saveData.SelectedTabIndex;
            UpdateLinkedTabsVisuals();
            IsBusy = false;
        }

        private void OnIgnoredFoldersChanged()
        {
            FileContentExportVM.FolderTreeManager.RefreshTree();
            TreeGeneratorVM.FolderTreeManager.RefreshTree();
            FolderContentExportVM.FolderTreeManager.RefreshTree();
        }

        private void SubscribeToChanges()
        {
            FileContentExportVM.StateChanging += OnStateChanging;
            TreeGeneratorVM.StateChanging += OnStateChanging;
            FolderContentExportVM.StateChanging += OnStateChanging;
            CsvViewerVM.StateChanging += OnStateChanging;
            OptionsVM.StateChanging += OnStateChanging;
            PathSelectionVM.StateChanging += OnStateChanging;
        }

        private async Task UpdateAllManagersWithPath(string path)
        {
            var managers = new List<FolderTreeManager>
            {
                FileContentExportVM.FolderTreeManager,
                TreeGeneratorVM.FolderTreeManager,
                FolderContentExportVM.FolderTreeManager
            };

            var handledManagers = new HashSet<string>();

            foreach (var manager in managers)
            {
                if (handledManagers.Contains(manager.Id)) continue;

                await manager.LoadTreeForPathAsync(path);
                handledManagers.Add(manager.Id);

                var peers = _folderTreeLinkService.GetLinkedPeers(manager.Id);
                foreach (var peerId in peers)
                {
                    handledManagers.Add(peerId);
                }
            }
        }

        public void RequestClose(CancelEventArgs e)
        {
            if (!SessionHistoryVM.HasUnsavedChanges)
            {
                return;
            }

            var result = _dialogService.ShowExitConfirmation(
                ResourceHelper.GetString("MainVM_UnsavedChangesTitle"),
                ResourceHelper.GetString("MainVM_UnsavedChangesMessage"));

            switch (result)
            {
                case ExitConfirmationResult.Save:
                    SessionHistoryVM.Save();
                    break;
                case ExitConfirmationResult.DontSave:
                    break;
                case ExitConfirmationResult.Cancel:
                    e.Cancel = true;
                    break;
            }
        }

        private SaveData GetCurrentSaveData()
        {
            return new SaveData
            {
                SourcePath = PathSelectionVM.SourcePath,
                DestinationPath = PathSelectionVM.DestinationPath,
                SelectedTabIndex = this.SelectedTabIndex,
                FileContentExport = new FileContentExportSettings
                {
                    OutputFileName = FileContentExportVM.OutputFileName,
                    AppendTimestamp = FileContentExportVM.AppendTimestamp,
                    SelectedExtensions = FileContentExportVM.GetSelectedExtensions(),
                    SelectedFilePaths = FileContentExportVM.GetSelectedFilePaths(),
                    IgnoredCommentFilePaths = FileContentExportVM.GetIgnoredCommentFilePaths(),
                    FolderTreeState = FileContentExportVM.FolderTreeManager.GetFolderStates()
                },
                FolderContentExport = new FolderContentExportSettings
                {
                    OutputFileName = FolderContentExportVM.OutputFileName,
                    AppendTimestamp = FolderContentExportVM.AppendTimestamp,
                    SelectedColumns = FolderContentExportVM.GetSelectedColumns(),
                    FolderTreeState = FolderContentExportVM.FolderTreeManager.GetFolderStates()
                },
                TreeGenerator = new TreeGeneratorSettings
                {
                    OutputFileName = TreeGeneratorVM.OutputFileName,
                    AppendTimestamp = TreeGeneratorVM.AppendTimestamp,
                    FolderTreeState = TreeGeneratorVM.FolderTreeManager.GetFolderStates(),
                    GenerationMode = TreeGeneratorVM.IsStructuredTree ? TreeGenerationMode.Structured : TreeGenerationMode.PathsOnly
                },
                CsvViewer = CsvViewerVM.GetSettings(),
                SettingsPage = new SettingsPageSettings
                {
                    IgnoredFolders = _sharedStateService.IgnoredFolders,
                    LinkedFolderTreeGroups = _folderTreeLinkService.GetLinkGroups()
                }
            };
        }

        private async Task ApplyAllData(SaveData saveData)
        {
            IsBusy = true;
            _sharedStateService.SourcePath = saveData.SourcePath;
            _sharedStateService.DestinationPath = saveData.DestinationPath;
            _sharedStateService.IgnoredFolders = saveData.SettingsPage.IgnoredFolders ?? new();

            _folderTreeLinkService.LoadLinkGroups(saveData.SettingsPage.LinkedFolderTreeGroups);

            if (FileContentExportVM.FolderTreeManager.RootFolders.Any() || !string.IsNullOrWhiteSpace(PathSelectionVM.SourcePath))
                await ApplyStateToManager(FileContentExportVM.FolderTreeManager, saveData.FileContentExport.FolderTreeState);
            FileContentExportVM.ApplySettings(saveData.FileContentExport);

            if (FolderContentExportVM.FolderTreeManager.RootFolders.Any() || !string.IsNullOrWhiteSpace(PathSelectionVM.SourcePath))
                await ApplyStateToManager(FolderContentExportVM.FolderTreeManager, saveData.FolderContentExport.FolderTreeState);
            FolderContentExportVM.ApplySettings(saveData.FolderContentExport);

            if (TreeGeneratorVM.FolderTreeManager.RootFolders.Any() || !string.IsNullOrWhiteSpace(PathSelectionVM.SourcePath))
                await ApplyStateToManager(TreeGeneratorVM.FolderTreeManager, saveData.TreeGenerator.FolderTreeState);
            TreeGeneratorVM.OutputFileName = saveData.TreeGenerator.OutputFileName;
            TreeGeneratorVM.AppendTimestamp = saveData.TreeGenerator.AppendTimestamp;

            if (saveData.TreeGenerator.GenerationMode == TreeGenerationMode.PathsOnly)
            {
                TreeGeneratorVM.IsPathsOnly = true;
            }
            else
            {
                TreeGeneratorVM.IsStructuredTree = true;
            }

            CsvViewerVM.ApplySettings(saveData.CsvViewer);

            SelectedTabIndex = saveData.SelectedTabIndex;
            IsBusy = false;

            CommandManager.InvalidateRequerySuggested();
        }

        private async Task ApplyStateToManager(FolderTreeManager manager, List<FolderState> state)
        {
            await manager.LoadTreeForPathAsync(PathSelectionVM.SourcePath, state);
        }

        private void OnPresetCreateRequested()
        {
            if (string.IsNullOrWhiteSpace(PathSelectionVM.SourcePath) || !Directory.Exists(PathSelectionVM.SourcePath))
            {
                _dialogService.ShowNotification(
                    ResourceHelper.GetString("Common_WarningTitle"),
                    ResourceHelper.GetString("Preset_SelectSourceFirst"),
                    DialogIconType.Warning);
                return;
            }

            string? presetName = _dialogService.ShowInputStringDialog(
                ResourceHelper.GetString("PathPreset_NewPresetTitle"),
                ResourceHelper.GetString("PathPreset_NamePrompt"));

            if (string.IsNullOrWhiteSpace(presetName)) return;

            if (_saveService.PresetNameExists(presetName))
            {
                bool confirmed = _dialogService.ShowConfirmation(
                    ResourceHelper.GetString("Common_WarningTitle"),
                    "A preset with this name already exists. Do you want to use a different name?",
                    DialogIconType.Warning);
                if (!confirmed) return;
            }

            try
            {
                var currentSaveData = GetCurrentSaveData();
                currentSaveData.PresetName = presetName;

                var relativeSaveData = SaveDataPathConverter.ConvertPaths(currentSaveData, PathSelectionVM.SourcePath, toRelative: true);

                _saveService.SavePreset(presetName, "", relativeSaveData);

                OptionsVM.RefreshPresetList();

                _dialogService.ShowNotification(
                    ResourceHelper.GetString("Common_SuccessTitle"),
                    string.Format(ResourceHelper.GetString("Preset_SavedSuccess"), presetName, "-"),
                    DialogIconType.Success);
            }
            catch (System.Exception ex)
            {
                _dialogService.ShowNotification(
                    ResourceHelper.GetString("Common_ErrorTitle"),
                    string.Format(ResourceHelper.GetString("Preset_SaveError"), "DB", ex.Message),
                    DialogIconType.Error);
            }
        }

        private async void OnPresetLoadRequested(int presetId)
        {
            if (string.IsNullOrWhiteSpace(PathSelectionVM.SourcePath) || !Directory.Exists(PathSelectionVM.SourcePath))
            {
                _dialogService.ShowNotification(
                    ResourceHelper.GetString("Common_WarningTitle"),
                    ResourceHelper.GetString("Preset_LoadSelectSourceFirst"),
                    DialogIconType.Warning);
                return;
            }

            var relativePresetData = _saveService.LoadPresetData(presetId);
            if (relativePresetData == null)
            {
                return;
            }

            string message = string.Format(ResourceHelper.GetString("Preset_LoadConfirmMessage"), "DB", relativePresetData.PresetName);
            bool confirmed = _dialogService.ShowConfirmation(
                title: string.Format(ResourceHelper.GetString("Preset_LoadConfirmTitle"), "DB"),
                message: message,
                iconType: DialogIconType.Info);

            if (!confirmed) return;

            OnStateChanging();
            try
            {
                var absolutePresetData = SaveDataPathConverter.ConvertPaths(relativePresetData, PathSelectionVM.SourcePath, toRelative: false);
                await ApplyAllData(absolutePresetData);
                SessionHistoryVM.HasUnsavedChanges = true;
                _dialogService.ShowNotification(
                    ResourceHelper.GetString("Common_SuccessTitle"),
                    string.Format(ResourceHelper.GetString("Preset_LoadedSuccess"), "DB", relativePresetData.PresetName),
                    DialogIconType.Success);
            }
            catch (System.Exception ex)
            {
                _dialogService.ShowNotification(
                    ResourceHelper.GetString("Common_ErrorTitle"),
                    string.Format(ResourceHelper.GetString("Preset_LoadError"), "DB", ex.Message),
                    DialogIconType.Error);
            }
        }

        private void OnPresetOverwriteRequested(int presetId, string presetName)
        {
            if (string.IsNullOrWhiteSpace(PathSelectionVM.SourcePath) || !Directory.Exists(PathSelectionVM.SourcePath))
            {
                _dialogService.ShowNotification(
                    ResourceHelper.GetString("Common_WarningTitle"),
                    ResourceHelper.GetString("Preset_SelectSourceFirst"),
                    DialogIconType.Warning);
                return;
            }

            bool confirmed = _dialogService.ShowConfirmation(
                ResourceHelper.GetString("Preset_OverwriteCurrentConfirmTitle"),
                string.Format(ResourceHelper.GetString("Preset_OverwriteCurrentConfirmMessage"), presetName),
                DialogIconType.Warning);

            if (!confirmed) return;

            try
            {
                var currentSaveData = GetCurrentSaveData();
                currentSaveData.PresetName = presetName;

                var relativeSaveData = SaveDataPathConverter.ConvertPaths(currentSaveData, PathSelectionVM.SourcePath, toRelative: true);

                _saveService.UpdatePresetData(presetId, relativeSaveData);

                OptionsVM.RefreshPresetList();

                _dialogService.ShowNotification(
                    ResourceHelper.GetString("Common_SuccessTitle"),
                    string.Format(ResourceHelper.GetString("Preset_SavedSuccess"), presetName, "-"),
                    DialogIconType.Success);
            }
            catch (System.Exception ex)
            {
                _dialogService.ShowNotification(
                    ResourceHelper.GetString("Common_ErrorTitle"),
                    string.Format(ResourceHelper.GetString("Preset_SaveError"), "DB", ex.Message),
                    DialogIconType.Error);
            }
        }

        private async void OnCurrentSaveDeleteRequested()
        {
            bool confirmed = _dialogService.ShowConfirmation(
                title: ResourceHelper.GetString("Reset_ConfirmTitle"),
                message: ResourceHelper.GetString("Reset_ConfirmMessage"),
                iconType: DialogIconType.Warning);

            if (confirmed)
            {
                OnStateChanging();
                try
                {
                    _saveService.DeleteSaveData();
                    await ApplyAllData(new SaveData());
                    SessionHistoryVM.HasUnsavedChanges = false;
                    _dialogService.ShowNotification(
                        ResourceHelper.GetString("Common_SuccessTitle"),
                        ResourceHelper.GetString("Reset_SuccessMessage"),
                        DialogIconType.Success);
                }
                catch (System.Exception ex)
                {
                    _dialogService.ShowNotification(
                        ResourceHelper.GetString("Common_ErrorTitle"),
                        string.Format(ResourceHelper.GetString("Reset_ErrorMessage"), ex.Message),
                        DialogIconType.Error);
                }
            }
        }

        private void OnStateChanging()
        {
            if (IsBusy) return;
            SessionHistoryVM.RecordState();
        }

        private void UpdateLinkedTabsVisuals()
        {
            var vms = new[ /*a*/ ]
            {
                new { Id = FileContentExportVM.FolderTreeManager.Id, VM = (ExportViewModelBase)FileContentExportVM },
                new { Id = TreeGeneratorVM.FolderTreeManager.Id, VM = (ExportViewModelBase)TreeGeneratorVM },
                new { Id = FolderContentExportVM.FolderTreeManager.Id, VM = (ExportViewModelBase)FolderContentExportVM }
            };

            foreach (var item in vms)
            {
                item.VM.LinkedTabs.Clear();
                var peerIds = _folderTreeLinkService.GetLinkedPeers(item.Id);
                foreach (var peerId in peerIds)
                {
                    var tabInfo = OptionsVM.TreeToolsVM.AllTabs.FirstOrDefault(t => t.Id == peerId);
                    if (tabInfo != null && tabInfo.Icon != null && tabInfo.IconBrush != null)
                    {
                        item.VM.LinkedTabs.Add(new TabIconViewModel(tabInfo.Name, tabInfo.Icon, tabInfo.IconBrush));
                    }
                }
            }
        }
    }
}
