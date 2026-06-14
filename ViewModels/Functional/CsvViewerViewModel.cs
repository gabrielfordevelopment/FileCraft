using FileCraft.Models;
using FileCraft.Services.Interfaces;
using FileCraft.Shared.Commands;
using FileCraft.Shared.Helpers;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace FileCraft.ViewModels.Functional
{
    public class CsvViewerViewModel : BaseViewModel
    {
        private readonly ICsvParsingService _csvParsingService;
        private readonly IClipboardService _clipboardService;
        private readonly IDialogService _dialogService;
        private readonly Debouncer _searchDebouncer;
        private CancellationTokenSource? _parseCts;

        private string _selectedFilePath = string.Empty;
        private string _pastedCsvText = string.Empty;
        private CsvInputMode _inputMode = CsvInputMode.File;
        private CsvDelimiterMode _delimiterMode = CsvDelimiterMode.Auto;
        private char _manualDelimiter = ',';
        private char _effectiveDelimiter = ',';
        private bool _hasHeaderRecord = true;
        private string _searchFilter = string.Empty;
        private string _statusText = string.Empty;
        private string _warningSummary = string.Empty;
        private List<CsvRow> _rows = new();

        public ObservableCollection<CsvColumn> Columns { get; } = new();
        public RangeObservableCollection<CsvRow> FilteredRows { get; } = new();
        public ObservableCollection<CsvParseWarning> Warnings { get; } = new();

        public IEnumerable<CsvDelimiterMode> DelimiterModes { get; } = Enum.GetValues<CsvDelimiterMode>();
        public IEnumerable<CsvInputMode> InputModes { get; } = Enum.GetValues<CsvInputMode>();
        public List<CsvDelimiterOption> DelimiterOptions { get; } = new()
        {
            new CsvDelimiterOption(",", ','),
            new CsvDelimiterOption(";", ';'),
            new CsvDelimiterOption("Tab", '\t'),
            new CsvDelimiterOption("|", '|')
        };

        public string SelectedFilePath
        {
            get => _selectedFilePath;
            set
            {
                if (_selectedFilePath != value)
                {
                    _selectedFilePath = value;
                    OnPropertyChanged();
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public string PastedCsvText
        {
            get => _pastedCsvText;
            set
            {
                if (_pastedCsvText != value)
                {
                    _pastedCsvText = value;
                    OnPropertyChanged();
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public CsvInputMode InputMode
        {
            get => _inputMode;
            set
            {
                if (_inputMode != value)
                {
                    _inputMode = value;
                    OnPropertyChanged();
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public CsvDelimiterMode DelimiterMode
        {
            get => _delimiterMode;
            set
            {
                if (_delimiterMode != value)
                {
                    OnStateChanging();
                    _delimiterMode = value;
                    OnPropertyChanged();
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public char ManualDelimiter
        {
            get => _manualDelimiter;
            set
            {
                if (_manualDelimiter != value)
                {
                    OnStateChanging();
                    _manualDelimiter = value;
                    OnPropertyChanged();
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public char EffectiveDelimiter
        {
            get => _effectiveDelimiter;
            set
            {
                if (_effectiveDelimiter != value)
                {
                    _effectiveDelimiter = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(EffectiveDelimiterText));
                }
            }
        }

        public string EffectiveDelimiterText => EffectiveDelimiter == '\t' ? "Tab" : EffectiveDelimiter.ToString();

        public bool HasHeaderRecord
        {
            get => _hasHeaderRecord;
            set
            {
                if (_hasHeaderRecord != value)
                {
                    OnStateChanging();
                    _hasHeaderRecord = value;
                    OnPropertyChanged();
                }
            }
        }

        public string SearchFilter
        {
            get => _searchFilter;
            set
            {
                if (_searchFilter != value)
                {
                    _searchFilter = value;
                    OnPropertyChanged();
                    _searchDebouncer.Debounce();
                    OnPropertyChanged(nameof(CanClearFilter));
                }
            }
        }

        public string StatusText
        {
            get => _statusText;
            set
            {
                if (_statusText != value)
                {
                    _statusText = value;
                    OnPropertyChanged();
                }
            }
        }

        public string WarningSummary
        {
            get => _warningSummary;
            set
            {
                if (_warningSummary != value)
                {
                    _warningSummary = value;
                    OnPropertyChanged();
                }
            }
        }

        public int TotalRows => _rows.Count;
        public int FilteredRowCount => FilteredRows.Count;
        public int ColumnCount => Columns.Count;
        public bool CanClearFilter => !string.IsNullOrWhiteSpace(SearchFilter);

        public ICommand SelectCsvFileCommand { get; }
        public ICommand LoadCsvCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand CopyCellCommand { get; }
        public ICommand ViewCellCommand { get; }
        public ICommand ClearFilterCommand { get; }

        public CsvViewerViewModel(
            ICsvParsingService csvParsingService,
            IClipboardService clipboardService,
            IDialogService dialogService)
        {
            _csvParsingService = csvParsingService;
            _clipboardService = clipboardService;
            _dialogService = dialogService;
            _searchDebouncer = new Debouncer(ApplyFilter);

            StatusText = ResourceHelper.GetString("CsvViewer_StatusEmpty");
            WarningSummary = ResourceHelper.GetString("CsvViewer_NoWarnings");

            SelectCsvFileCommand = new RelayCommand(_ => SelectCsvFile());
            LoadCsvCommand = new RelayCommand(async _ => await LoadCsvAsync(), _ => CanLoadCsv());
            ClearCommand = new RelayCommand(_ => Clear());
            CopyCellCommand = new RelayCommand(cell => CopyCell(cell as CsvCell), cell => cell is CsvCell);
            ViewCellCommand = new RelayCommand(cell => ViewCell(cell as CsvCell), cell => cell is CsvCell);
            ClearFilterCommand = new RelayCommand(_ => ClearFilter(), _ => CanClearFilter);
        }

        public CsvViewerSettings GetSettings()
        {
            return new CsvViewerSettings
            {
                DelimiterMode = DelimiterMode,
                ManualDelimiter = ManualDelimiter,
                HasHeaderRecord = HasHeaderRecord
            };
        }

        public void ApplySettings(CsvViewerSettings? settings)
        {
            if (settings == null)
            {
                return;
            }

            _delimiterMode = settings.DelimiterMode;
            _manualDelimiter = settings.ManualDelimiter == default ? ',' : settings.ManualDelimiter;
            _hasHeaderRecord = settings.HasHeaderRecord;
            OnPropertyChanged(nameof(DelimiterMode));
            OnPropertyChanged(nameof(ManualDelimiter));
            OnPropertyChanged(nameof(HasHeaderRecord));
        }

        private void SelectCsvFile()
        {
            var path = _dialogService.SelectFile(
                ResourceHelper.GetString("CsvViewer_SelectFileTitle"),
                ResourceHelper.GetString("CsvViewer_FileFilter"));

            if (!string.IsNullOrWhiteSpace(path))
            {
                InputMode = CsvInputMode.File;
                SelectedFilePath = path;
            }
        }

        private bool CanLoadCsv()
        {
            if (IsBusy)
            {
                return false;
            }

            return InputMode == CsvInputMode.File
                ? !string.IsNullOrWhiteSpace(SelectedFilePath)
                : !string.IsNullOrWhiteSpace(PastedCsvText);
        }

        private async Task LoadCsvAsync()
        {
            _parseCts?.Cancel();
            _parseCts = new CancellationTokenSource();
            var token = _parseCts.Token;

            IsBusy = true;
            StatusText = ResourceHelper.GetString("CsvViewer_StatusLoading");

            var request = new CsvParseRequest
            {
                InputMode = InputMode,
                FilePath = SelectedFilePath,
                Text = PastedCsvText,
                DelimiterMode = DelimiterMode,
                ManualDelimiter = ManualDelimiter,
                HasHeaderRecord = HasHeaderRecord
            };

            try
            {
                var result = await Task.Run(() => _csvParsingService.ParseAsync(request, token).GetAwaiter().GetResult(), token);
                if (token.IsCancellationRequested)
                {
                    return;
                }

                ApplyResult(result);
            }
            catch (OperationCanceledException)
            {
                StatusText = ResourceHelper.GetString("CsvViewer_StatusCancelled");
            }
            catch (Exception ex)
            {
                StatusText = string.Format(ResourceHelper.GetString("CsvViewer_StatusError"), ex.Message);
                _dialogService.ShowNotification(
                    ResourceHelper.GetString("Common_ErrorTitle"),
                    StatusText,
                    DialogIconType.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ApplyResult(CsvParseResult result)
        {
            Columns.Clear();
            foreach (var column in result.Columns)
            {
                Columns.Add(column);
            }

            _rows = result.Rows;
            EffectiveDelimiter = result.EffectiveDelimiter;

            Warnings.Clear();
            foreach (var warning in result.Warnings)
            {
                Warnings.Add(warning);
            }

            ApplyFilter();
            UpdateCounts();
            WarningSummary = Warnings.Count == 0
                ? ResourceHelper.GetString("CsvViewer_NoWarnings")
                : string.Format(ResourceHelper.GetString("CsvViewer_WarningSummary"), Warnings.Count);
            StatusText = string.Format(ResourceHelper.GetString("CsvViewer_StatusLoaded"), TotalRows, ColumnCount, EffectiveDelimiterText);
        }

        private void ApplyFilter()
        {
            IEnumerable<CsvRow> filtered = _rows;
            if (!string.IsNullOrWhiteSpace(SearchFilter))
            {
                string filter = SearchFilter.Trim();
                filtered = filtered.Where(row => row.Cells.Any(cell => cell.Value.Contains(filter, StringComparison.OrdinalIgnoreCase)));
            }

            FilteredRows.ReplaceAll(filtered.ToList());
            UpdateCounts();
        }

        private void UpdateCounts()
        {
            OnPropertyChanged(nameof(TotalRows));
            OnPropertyChanged(nameof(FilteredRowCount));
            OnPropertyChanged(nameof(ColumnCount));
            CommandManager.InvalidateRequerySuggested();
        }

        private void CopyCell(CsvCell? cell)
        {
            if (cell == null)
            {
                return;
            }

            try
            {
                _clipboardService.SetText(cell.Value);
                StatusText = ResourceHelper.GetString("CsvViewer_StatusCopied");
            }
            catch (Exception ex)
            {
                StatusText = string.Format(ResourceHelper.GetString("CsvViewer_StatusCopyFailed"), ex.Message);
            }
        }

        private void ViewCell(CsvCell? cell)
        {
            if (cell == null)
            {
                return;
            }

            string title = string.Format(ResourceHelper.GetString("CsvViewer_CellDetailsTitle"), cell.RowNumber, cell.ColumnIndex + 1);
            _dialogService.ShowTextContentDialog(title, cell.Value);
        }

        private void ClearFilter()
        {
            SearchFilter = string.Empty;
            ApplyFilter();
        }

        private void Clear()
        {
            _parseCts?.Cancel();
            SelectedFilePath = string.Empty;
            PastedCsvText = string.Empty;
            SearchFilter = string.Empty;
            Columns.Clear();
            Warnings.Clear();
            _rows = new List<CsvRow>();
            FilteredRows.Clear();
            StatusText = ResourceHelper.GetString("CsvViewer_StatusEmpty");
            WarningSummary = ResourceHelper.GetString("CsvViewer_NoWarnings");
            EffectiveDelimiter = ManualDelimiter;
            UpdateCounts();
        }
    }

    public class CsvDelimiterOption
    {
        public string Label { get; }
        public char Value { get; }

        public CsvDelimiterOption(string label, char value)
        {
            Label = label;
            Value = value;
        }
    }
}
