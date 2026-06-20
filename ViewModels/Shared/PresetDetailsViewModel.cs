using FileCraft.Models;
using FileCraft.Shared.Commands;
using FileCraft.Shared.Helpers;
using Fonts;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace FileCraft.ViewModels.Shared
{
    public class DetailItem : BaseViewModel
    {
        private bool _isSelected;

        public string Label { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public ICommand? Command { get; set; }
        public bool IsInteractive { get; set; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }
    }

    public class PresetDetailsViewModel : BaseViewModel
    {
        private bool _isSummaryVisible = true;
        private bool _isContentDetailVisible = false;
        private string _detailTitle = string.Empty;
        private string _detailText = string.Empty;
        private string _summaryText = string.Empty;
        private bool _isTreeMode = false;
        private bool _canToggleView = true;

        private List<string> _currentDetailPaths = new();
        private List<string> _storedFilePaths = new();
        private List<string> _storedFolderPaths = new();

        public string Title { get; }
        public string PresetName { get; }
        public DateTime LastModified { get; }
        public string Description { get; }

        public string DisplayDateString
        {
            get
            {
                var timeSpan = DateTime.Now - LastModified;
                string relativeTime;

                if (timeSpan.TotalMinutes < 1)
                {
                    relativeTime = "Just now";
                }
                else if (timeSpan.TotalHours < 1)
                {
                    int minutes = (int)timeSpan.TotalMinutes;
                    relativeTime = $"{minutes} minute{(minutes != 1 ? "s" : "")} ago";
                }
                else if (timeSpan.TotalDays < 1)
                {
                    int hours = (int)timeSpan.TotalHours;
                    relativeTime = $"{hours} hour{(hours != 1 ? "s" : "")} ago";
                }
                else
                {
                    int days = (int)timeSpan.TotalDays;
                    relativeTime = $"{days} day{(days != 1 ? "s" : "")} ago";
                }

                return $"{LastModified:yyyy.MM.dd HH:mm} • {relativeTime}";
            }
        }

        public bool IsSummaryVisible
        {
            get => _isSummaryVisible;
            set
            {
                if (_isSummaryVisible != value)
                {
                    _isSummaryVisible = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsContentDetailVisible
        {
            get => _isContentDetailVisible;
            set
            {
                if (_isContentDetailVisible != value)
                {
                    _isContentDetailVisible = value;
                    OnPropertyChanged();
                }
            }
        }

        public string DetailTitle
        {
            get => _detailTitle;
            set
            {
                if (_detailTitle != value)
                {
                    _detailTitle = value;
                    OnPropertyChanged();
                }
            }
        }

        public string DetailText
        {
            get => _detailText;
            set
            {
                if (_detailText != value)
                {
                    _detailText = value;
                    OnPropertyChanged();
                }
            }
        }

        public string SummaryText
        {
            get => _summaryText;
            set
            {
                if (_summaryText != value)
                {
                    _summaryText = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsTreeMode
        {
            get => _isTreeMode;
            set
            {
                if (_isTreeMode != value)
                {
                    _isTreeMode = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ToggleViewButtonText));
                    UpdateDetailText();
                }
            }
        }

        public string ToggleViewButtonText => IsTreeMode
            ? ResourceHelper.GetString("Details_SwitchToList")
            : ResourceHelper.GetString("Details_SwitchToTree");

        public ObservableCollection<DetailItem> Statistics { get; } = new();
        public string ContentHeader { get; private set; } = "Content";

        public ICommand CloseCommand { get; }
        public ICommand ShowFoldersCommand { get; }
        public ICommand ShowFilesCommand { get; }
        public ICommand BackCommand { get; }
        public ICommand ToggleViewModeCommand { get; }

        public event Action? RequestClose;

        public PresetDetailsViewModel(PresetItemViewModel presetItem)
        {
            Title = $"Details: {presetItem.Name}";
            PresetName = presetItem.Name;
            LastModified = presetItem.LastModified;
            Description = presetItem.Description;

            CloseCommand = new RelayCommand(_ => RequestClose?.Invoke());
            ShowFoldersCommand = new RelayCommand(_ => ShowFolders());
            ShowFilesCommand = new RelayCommand(_ => ShowFiles());
            BackCommand = new RelayCommand(_ => GoBack());
            ToggleViewModeCommand = new RelayCommand(async _ => await ToggleViewMode(), _ => _canToggleView);

            LoadData(presetItem.RawData);
        }

        private void LoadData(object rawData)
        {
            if (rawData is PathPreset pathPreset)
            {
                LoadPathPreset(pathPreset);
            }
            else if (rawData is PresetEntity savePreset)
            {
                LoadSavePreset(savePreset);
            }
        }

        private void LoadPathPreset(PathPreset preset)
        {
            _storedFilePaths = preset.FilePaths.OrderBy(x => x).ToList();

            Statistics.Add(new DetailItem
            {
                Label = "Total Files",
                Value = preset.FilePaths.Count.ToString(),
                Icon = MaterialIcons.description,
                IsInteractive = true,
                Command = ShowFilesCommand
            });

            ContentHeader = "Included Files";
            SummaryText = string.Join(Environment.NewLine, preset.FilePaths.OrderBy(x => x));
        }

        private void LoadSavePreset(PresetEntity preset)
        {
            var data = preset.Data;
            var stats = preset.Statistics;

            _storedFilePaths = data.FileContentExport.SelectedFilePaths.OrderBy(x => x).ToList();

            var allFolders = new HashSet<string>();
            foreach (var state in data.FileContentExport.FolderTreeState.Where(x => x.IsSelected == true)) allFolders.Add(state.FullPath);
            foreach (var state in data.FolderContentExport.FolderTreeState.Where(x => x.IsSelected == true)) allFolders.Add(state.FullPath);
            foreach (var state in data.TreeGenerator.FolderTreeState.Where(x => x.IsSelected == true)) allFolders.Add(state.FullPath);

            _storedFolderPaths = allFolders.OrderBy(x => x).ToList();

            string displayVersion = preset.AppVersion;
            if (System.Version.TryParse(displayVersion, out var parsedVersion))
            {
                displayVersion = $"{parsedVersion.Major}.{parsedVersion.Minor}.{parsedVersion.Build}";
            }

            Statistics.Add(new DetailItem { Label = "App Version", Value = displayVersion, Icon = MaterialIcons.info, IsInteractive = false });

            Statistics.Add(new DetailItem
            {
                Label = "Folders Configured",
                Value = stats.FolderCount.ToString(),
                Icon = MaterialIcons.folder,
                IsInteractive = true,
                Command = ShowFoldersCommand
            });

            Statistics.Add(new DetailItem
            {
                Label = "Explicit Files",
                Value = stats.FileCount.ToString(),
                Icon = MaterialIcons.description,
                IsInteractive = true,
                Command = ShowFilesCommand
            });

            ContentHeader = "Configuration Summary";

            var sb = new StringBuilder();

            if (data.FileContentExport.SelectedFilePaths.Any() || data.FileContentExport.FolderTreeState.Any(x => x.IsSelected == true))
            {
                sb.AppendLine("--- File Content Export ---");
                sb.AppendLine($"Output: {data.FileContentExport.OutputFileName}");
                if (data.FileContentExport.SelectedExtensions.Any())
                {
                    sb.AppendLine($"Extensions: {string.Join(", ", data.FileContentExport.SelectedExtensions)}");
                }
                sb.AppendLine($"Selected Files: {data.FileContentExport.SelectedFilePaths.Count}");
                sb.AppendLine();
            }

            if (data.TreeGenerator.FolderTreeState.Any(x => x.IsSelected == true))
            {
                sb.AppendLine("--- Tree Generator ---");
                sb.AppendLine($"Output: {data.TreeGenerator.OutputFileName}");
                sb.AppendLine($"Mode: {data.TreeGenerator.GenerationMode}");
                sb.AppendLine();
            }

            if (data.FolderContentExport.FolderTreeState.Any(x => x.IsSelected == true))
            {
                sb.AppendLine("--- Folder Content Export ---");
                sb.AppendLine($"Output: {data.FolderContentExport.OutputFileName}");
                if (data.FolderContentExport.SelectedColumns.Any())
                {
                    var displayColumns = data.FolderContentExport.SelectedColumns.Select(FolderExportColumns.GetDisplayName);
                    sb.AppendLine($"Columns: {string.Join(", ", displayColumns)}");
                }
            }

            SummaryText = sb.ToString();
        }

        private void ShowFolders()
        {
            if (!_storedFolderPaths.Any()) return;
            _currentDetailPaths = _storedFolderPaths;
            DetailTitle = ResourceHelper.GetString("Details_FoldersTitle");
            IsTreeMode = false;
            UpdateBadgeSelection(ShowFoldersCommand);
            SwitchToDetailView();
        }

        private void ShowFiles()
        {
            if (!_storedFilePaths.Any()) return;
            _currentDetailPaths = _storedFilePaths;
            DetailTitle = ResourceHelper.GetString("Details_FilesTitle");
            IsTreeMode = false;
            UpdateBadgeSelection(ShowFilesCommand);
            SwitchToDetailView();
        }

        private void SwitchToDetailView()
        {
            IsSummaryVisible = false;
            IsContentDetailVisible = true;
            UpdateDetailText();
        }

        private void GoBack()
        {
            IsContentDetailVisible = false;
            IsSummaryVisible = true;
            UpdateBadgeSelection(null);
        }

        private void UpdateBadgeSelection(ICommand? activeCommand)
        {
            foreach (var item in Statistics)
            {
                item.IsSelected = item.Command == activeCommand && activeCommand != null;
            }
        }

        private async Task ToggleViewMode()
        {
            if (!_canToggleView) return;

            _canToggleView = false;
            CommandManager.InvalidateRequerySuggested();

            IsTreeMode = !IsTreeMode;

            await Task.Delay(500);
            _canToggleView = true;
            CommandManager.InvalidateRequerySuggested();
        }

        private void UpdateDetailText()
        {
            if (!_currentDetailPaths.Any())
            {
                DetailText = string.Empty;
                return;
            }

            if (IsTreeMode)
            {
                DetailText = GenerateTreeText(_currentDetailPaths);
            }
            else
            {
                DetailText = GenerateListText(_currentDetailPaths);
            }
        }

        private string GenerateListText(List<string> paths)
        {
            return string.Join(Environment.NewLine, paths);
        }

        private string GenerateTreeText(List<string> paths)
        {
            var rootNode = new TreeNode(".");

            foreach (var path in paths)
            {
                var cleanPath = path.Replace('\\', '/');
                var parts = cleanPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                rootNode.AddPath(parts, 0);
            }

            var sb = new StringBuilder();
            sb.AppendLine(".");

            foreach (var child in rootNode.Children)
            {
                PrintTree(child, "", sb, child == rootNode.Children.Last());
            }

            return sb.ToString();
        }

        private void PrintTree(TreeNode node, string indent, StringBuilder sb, bool isLast)
        {
            sb.AppendLine($"{indent}{(isLast ? "└── " : "├── ")}{node.Name}");

            var childIndent = indent + (isLast ? "    " : "│   ");

            for (int i = 0; i < node.Children.Count; i++)
            {
                var child = node.Children[i];
                PrintTree(child, childIndent, sb, i == node.Children.Count - 1);
            }
        }

        private class TreeNode
        {
            public string Name { get; }
            public List<TreeNode> Children { get; } = new();

            public TreeNode(string name)
            {
                Name = name;
            }

            public void AddPath(string[] parts, int index)
            {
                if (index >= parts.Length) return;

                var part = parts[index];

                if (part == "." || string.IsNullOrEmpty(part))
                {
                    AddPath(parts, index + 1);
                    return;
                }

                var child = Children.FirstOrDefault(c => c.Name.Equals(part, StringComparison.OrdinalIgnoreCase));

                if (child == null)
                {
                    child = new TreeNode(part);
                    Children.Add(child);
                }

                child.AddPath(parts, index + 1);
            }
        }
    }
}
