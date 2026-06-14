using System.Collections.ObjectModel;

namespace FileCraft.Models
{
    public enum CsvInputMode
    {
        File,
        PastedText
    }

    public enum CsvDelimiterMode
    {
        Auto,
        Manual
    }

    public enum CsvWarningSeverity
    {
        Info,
        Warning,
        Error
    }

    public class CsvParseRequest
    {
        public CsvInputMode InputMode { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public CsvDelimiterMode DelimiterMode { get; set; } = CsvDelimiterMode.Auto;
        public char ManualDelimiter { get; set; } = ',';
        public bool HasHeaderRecord { get; set; } = true;
    }

    public class CsvParseOptions
    {
        public char Delimiter { get; set; } = ',';
        public char Quote { get; set; } = '"';
        public bool HasHeaderRecord { get; set; } = true;
        public bool IgnoreBlankLines { get; set; } = true;
    }

    public class CsvParseResult
    {
        public List<CsvColumn> Columns { get; set; } = new();
        public List<CsvRow> Rows { get; set; } = new();
        public List<CsvParseWarning> Warnings { get; set; } = new();
        public char EffectiveDelimiter { get; set; } = ',';
        public int SourceRecordCount { get; set; }
    }

    public class CsvParseWarning
    {
        public CsvWarningSeverity Severity { get; set; } = CsvWarningSeverity.Warning;
        public int? RowNumber { get; set; }
        public int? ColumnNumber { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class CsvColumn
    {
        public int Index { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string OriginalName { get; set; } = string.Empty;
        public double Width { get; set; } = 220;
    }

    public class CsvRow
    {
        public int RowNumber { get; set; }
        public int SourceLineStart { get; set; }
        public int SourceLineEnd { get; set; }
        public ObservableCollection<CsvCell> Cells { get; } = new();
    }

    public class CsvCell
    {
        private const int PreviewLength = 500;

        public int RowNumber { get; set; }
        public int ColumnIndex { get; set; }
        public string Value { get; set; } = string.Empty;
        public int Length => Value.Length;

        public string DisplayValue
        {
            get
            {
                if (Value.Length <= PreviewLength)
                {
                    return Value;
                }

                return Value.Substring(0, PreviewLength);
            }
        }

        public string? ShortPreview
        {
            get
            {
                if (string.IsNullOrEmpty(Value))
                {
                    return null;
                }

                var preview = Value.Replace("\r", " ").Replace("\n", " ");
                return preview.Length <= 160 ? preview : preview.Substring(0, 160) + "...";
            }
        }
    }

    public class CsvViewerSettings
    {
        public CsvDelimiterMode DelimiterMode { get; set; } = CsvDelimiterMode.Auto;
        public char ManualDelimiter { get; set; } = ',';
        public bool HasHeaderRecord { get; set; } = true;
    }

    public class CsvRawRecord
    {
        public List<string> Fields { get; } = new();
        public int StartLine { get; set; }
        public int EndLine { get; set; }
        public bool IsBlank { get; set; }
    }
}
