using FileCraft.Models;
using FileCraft.Services.Interfaces;
using FileCraft.Shared.Commands;
using FileCraft.Shared.Helpers;
using FileCraft.ViewModels.Interfaces;
using FileCraft.ViewModels.Shared;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

namespace FileCraft.ViewModels.Functional
{
    public enum FolderContentFullscreenState
    {
        None,
        Folders,
        Details
    }

    public class FolderContentExportViewModel : ExportViewModelBase
    {
        private bool? _areAllColumnsSelected;
        private int _affectedFilesCount;
        private bool _protectSpreadsheetFormulas;
        private readonly IFileQueryService _fileQueryService;

        public FullscreenManager<FolderContentFullscreenState> FullscreenManager { get; }

        public bool? AreAllColumnsSelected
        {
            get => _areAllColumnsSelected;
            set
            {
                OnStateChanging();
                bool selectAll = _areAllColumnsSelected != true;
                SetColumnsSelectionState(selectAll);
            }
        }

        public bool ProtectSpreadsheetFormulas
        {
            get => _protectSpreadsheetFormulas;
            set
            {
                if (_protectSpreadsheetFormulas != value)
                {
                    OnStateChanging();
                    _protectSpreadsheetFormulas = value;
                    OnPropertyChanged();
                }
            }
        }

        public int AffectedFilesCount
        {
            get => _affectedFilesCount;
            set
            {
                _affectedFilesCount = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<SelectableColumnViewModel> AvailableColumns { get; } = new();

        public ObservableCollection<FolderExportColumnGroupViewModel> ColumnGroups { get; } = new();

        public ICommand ExportFolderContentsCommand { get; }

        public FolderContentExportViewModel(
            ISharedStateService sharedStateService,
            IFileOperationService fileOperationService,
            IDialogService dialogService,
            FolderTreeManager folderTreeManager,
            IFileQueryService fileQueryService)
            : base(sharedStateService, fileOperationService, dialogService, folderTreeManager)
        {
            _fileQueryService = fileQueryService;
            FullscreenManager = new FullscreenManager<FolderContentFullscreenState>(FolderContentFullscreenState.None);

            FolderTreeManager.FolderSelectionChanged += UpdateAffectedFilesCount;
            FolderTreeManager.StateChanging += OnStateChanging;

            ExportFolderContentsCommand = new RelayCommand(async (_) => await ExportFolderContents(), (_) => CanExecuteOperation(this.OutputFileName) && AvailableColumns.Any(c => c.IsSelected));

            foreach (var group in FolderExportColumns.All.GroupBy(column => column.Group))
            {
                var groupViewModel = new FolderExportColumnGroupViewModel(group.Key, this.OnStateChanging, UpdateSelectAllColumnsState);

                foreach (var definition in group)
                {
                    var item = new SelectableColumnViewModel(definition, definition.IsDefaultSelected, this.OnStateChanging);
                    item.PropertyChanged += OnColumnSelectionChanged;
                    groupViewModel.Columns.Add(item);
                    AvailableColumns.Add(item);
                }

                groupViewModel.UpdateSelectAllState();
                ColumnGroups.Add(groupViewModel);
            }

            UpdateSelectAllColumnsState();
            UpdateAffectedFilesCount();
        }

        protected override void OnFolderTreeManagerPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            base.OnFolderTreeManagerPropertyChanged(sender, e);
            if (e.PropertyName == nameof(FolderTreeManager.RootFolders))
            {
                UpdateAffectedFilesCount();
            }
        }

        private void OnColumnSelectionChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SelectableColumnViewModel.IsSelected))
            {
                foreach (var group in ColumnGroups)
                {
                    group.UpdateSelectAllState();
                }
                UpdateSelectAllColumnsState();
            }
        }

        private void SetColumnsSelectionState(bool isSelected)
        {
            SelectionHelper.SetSelectionState(AvailableColumns, isSelected);
            foreach (var group in ColumnGroups)
            {
                group.UpdateSelectAllState();
            }
            UpdateSelectAllColumnsState();
        }

        private void UpdateSelectAllColumnsState()
        {
            bool? newSelectionState = SelectionHelper.GetMasterSelectionState(AvailableColumns);

            if (_areAllColumnsSelected != newSelectionState)
            {
                _areAllColumnsSelected = newSelectionState;
                OnPropertyChanged(nameof(AreAllColumnsSelected));
            }
            CommandManager.InvalidateRequerySuggested();
        }

        public void ApplySettings(FolderContentExportSettings settings)
        {
            OutputFileName = settings.OutputFileName;
            AppendTimestamp = settings.AppendTimestamp;
            ProtectSpreadsheetFormulas = settings.ProtectSpreadsheetFormulas;
            var loadedSelectedColumns = new HashSet<string>(settings.SelectedColumns ?? new List<string>(), StringComparer.Ordinal);

            foreach (var column in AvailableColumns)
            {
                column.IsSelected = loadedSelectedColumns.Contains(column.Id);
            }

            foreach (var group in ColumnGroups)
            {
                group.UpdateSelectAllState();
            }

            UpdateSelectAllColumnsState();
        }

        public List<string> GetSelectedColumns()
        {
            return AvailableColumns.Where(c => c.IsSelected).Select(c => c.Id).ToList();
        }

        public FolderContentExportOptions GetExportOptions()
        {
            return new FolderContentExportOptions
            {
                ProtectSpreadsheetFormulas = ProtectSpreadsheetFormulas
            };
        }

        private void UpdateAffectedFilesCount()
        {
            var includedFolderPaths = GetSelectedFolderPaths();

            if (includedFolderPaths.Any())
            {
                var ignoredFolders = new HashSet<string>(_sharedStateService.IgnoredFolders, StringComparer.OrdinalIgnoreCase);
                var allFiles = _fileQueryService.GetAllFiles(includedFolderPaths, ignoredFolders);
                AffectedFilesCount = allFiles.Count();
            }
            else
            {
                AffectedFilesCount = 0;
            }
            CommandManager.InvalidateRequerySuggested();
        }

        private List<string> GetSelectedFolderPaths()
        {
            var paths = new List<string>();
            if (RootFolders.Any())
            {
                CollectSelectedPaths(RootFolders[0], paths);
            }
            return paths;
        }

        private void CollectSelectedPaths(FolderViewModel node, List<string> paths)
        {
            if (node.IsSelected == true)
            {
                paths.Add(node.FullPath);
            }
            else if (node.IsSelected == null)
            {
                foreach (var child in node.Children)
                {
                    CollectSelectedPaths(child, paths);
                }
            }
        }

        private async Task ExportFolderContents()
        {
            IsBusy = true;
            try
            {
                var includedFolderPaths = GetSelectedFolderPaths();

                if (!includedFolderPaths.Any())
                {
                    _dialogService.ShowNotification(
                        ResourceHelper.GetString("Common_InfoTitle"),
                        ResourceHelper.GetString("FolderContent_NoFoldersSelected"),
                        DialogIconType.Info);
                    return;
                }

                var selectedColumns = GetSelectedColumns();
                if (!selectedColumns.Any())
                {
                    _dialogService.ShowNotification(
                        ResourceHelper.GetString("Common_InfoTitle"),
                        ResourceHelper.GetString("FolderContent_NoColumnsSelected"),
                        DialogIconType.Info);
                    return;
                }

                string messageFormat = ResourceHelper.GetString("FolderContent_ConfirmExportMessage");
                string message = $"{messageFormat}\n{_sharedStateService.DestinationPath}";

                bool confirmed = _dialogService.ShowConfirmation(
                    title: ResourceHelper.GetString("FolderContent_ExportTitle"),
                    message: message,
                    iconType: DialogIconType.Info,
                    filesAffected: AffectedFilesCount);

                if (!confirmed)
                {
                    return;
                }

                string finalFileName = GetFinalFileName(OutputFileName, AppendTimestamp);

                string outputFilePath = await _fileOperationService.ExportFolderContentsAsync(_sharedStateService.DestinationPath, includedFolderPaths, finalFileName, selectedColumns, GetExportOptions());

                string successMsg = ResourceHelper.GetString("FolderContent_SuccessMessage");
                string savedToMsg = string.Format(ResourceHelper.GetString("Common_SavedTo"), outputFilePath);

                _dialogService.ShowNotification(
                    ResourceHelper.GetString("Common_SuccessTitle"),
                    $"{successMsg}\n\n{savedToMsg}",
                    DialogIconType.Success);
            }
            catch (Exception ex)
            {
                string errorMsg = string.Format(ResourceHelper.GetString("FolderContent_ErrorMessage"), ex.Message);
                _dialogService.ShowNotification(
                    ResourceHelper.GetString("Common_ErrorTitle"),
                    errorMsg,
                    DialogIconType.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }

    public sealed class SelectableColumnViewModel : BaseViewModel, ISelectable
    {
        private bool _isSelected;
        private readonly Action? _onStateChanging;

        public string Id { get; }
        public string DisplayName { get; }
        public FolderExportColumnGroup Group { get; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _onStateChanging?.Invoke();
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public SelectableColumnViewModel(FolderExportColumnDefinition definition, bool isSelected, Action? onStateChanging)
        {
            Id = definition.Id;
            DisplayName = definition.DisplayName;
            Group = definition.Group;
            _isSelected = isSelected;
            _onStateChanging = onStateChanging;
        }
    }

    public sealed class FolderExportColumnGroupViewModel : BaseViewModel
    {
        private bool? _areAllColumnsSelected;
        private readonly Action? _onStateChanging;
        private readonly Action? _onSelectionChanged;
        private bool _isUpdatingSelectionState;

        public string DisplayName { get; }
        public bool IsExpanded { get; set; }
        public ObservableCollection<SelectableColumnViewModel> Columns { get; } = new();

        public bool? AreAllColumnsSelected
        {
            get => _areAllColumnsSelected;
            set
            {
                if (_isUpdatingSelectionState)
                {
                    _areAllColumnsSelected = value;
                    OnPropertyChanged();
                    return;
                }

                bool selectAll = _areAllColumnsSelected != true;
                _onStateChanging?.Invoke();
                SetColumnsSelectionState(selectAll);
                _onSelectionChanged?.Invoke();

                if (_areAllColumnsSelected != value)
                {
                    OnPropertyChanged();
                }
            }
        }

        public FolderExportColumnGroupViewModel(FolderExportColumnGroup group, Action? onStateChanging, Action? onSelectionChanged)
        {
            DisplayName = GetDisplayName(group);
            IsExpanded = group != FolderExportColumnGroup.Diagnostics;
            _onStateChanging = onStateChanging;
            _onSelectionChanged = onSelectionChanged;
        }

        public void SetColumnsSelectionState(bool isSelected)
        {
            SelectionHelper.SetSelectionState(Columns, isSelected);
            UpdateSelectAllState();
        }

        public void UpdateSelectAllState()
        {
            var newState = SelectionHelper.GetMasterSelectionState(Columns);
            if (_areAllColumnsSelected != newState)
            {
                _isUpdatingSelectionState = true;
                _areAllColumnsSelected = newState;
                OnPropertyChanged(nameof(AreAllColumnsSelected));
                _isUpdatingSelectionState = false;
            }
        }

        private static string GetDisplayName(FolderExportColumnGroup group)
        {
            string resourceKey = group switch
            {
                FolderExportColumnGroup.FileDetails => "FolderContent_Group_FileDetails",
                FolderExportColumnGroup.AudioTags => "FolderContent_Group_AudioTags",
                FolderExportColumnGroup.AudioProperties => "FolderContent_Group_AudioProperties",
                FolderExportColumnGroup.Diagnostics => "FolderContent_Group_Diagnostics",
                _ => group.ToString()
            };

            var value = ResourceHelper.GetString(resourceKey);
            return value == resourceKey ? group.ToString() : value;
        }
    }
}
