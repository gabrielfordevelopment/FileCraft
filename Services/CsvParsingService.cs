using FileCraft.Models;
using FileCraft.Services.Interfaces;
using FileCraft.Shared.Helpers;
using System.IO;
using System.Text;

namespace FileCraft.Services
{
    public class CsvParsingService : ICsvParsingService
    {
        private const int LargeFileWarningBytes = 10 * 1024 * 1024;
        private const int LargeCellWarningLength = 100_000;

        private readonly ICsvDelimiterDetector _delimiterDetector;
        private readonly CsvReaderCore _readerCore = new();

        public CsvParsingService(ICsvDelimiterDetector delimiterDetector)
        {
            _delimiterDetector = delimiterDetector;
        }

        public async Task<CsvParseResult> ParseAsync(CsvParseRequest request, CancellationToken cancellationToken = default)
        {
            var warnings = new List<CsvParseWarning>();
            string content = await ReadInputAsync(request, warnings, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            char delimiter = request.DelimiterMode == CsvDelimiterMode.Auto
                ? _delimiterDetector.DetectDelimiter(CreateSample(content), warnings)
                : request.ManualDelimiter;

            var options = new CsvParseOptions
            {
                Delimiter = delimiter,
                HasHeaderRecord = request.HasHeaderRecord,
                IgnoreBlankLines = true
            };

            var records = _readerCore.ReadRecords(new StringReader(content), options, warnings, cancellationToken);
            return BuildResult(records, options, warnings);
        }

        private static async Task<string> ReadInputAsync(CsvParseRequest request, IList<CsvParseWarning> warnings, CancellationToken cancellationToken)
        {
            if (request.InputMode == CsvInputMode.PastedText)
            {
                return request.Text ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(request.FilePath))
            {
                return string.Empty;
            }

            var fileInfo = new FileInfo(request.FilePath);
            if (fileInfo.Exists && fileInfo.Length > LargeFileWarningBytes)
            {
                warnings.Add(new CsvParseWarning
                {
                    Severity = CsvWarningSeverity.Warning,
                    Message = FormatResource("CsvViewer_WarningLargeFile", "Large file detected ({0} MB). Loading the full file may take time.", fileInfo.Length / 1024 / 1024)
                });
            }

            using var reader = new StreamReader(request.FilePath, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), detectEncodingFromByteOrderMarks: true);
            return await reader.ReadToEndAsync(cancellationToken);
        }

        private static string CreateSample(string content)
        {
            const int maxSampleChars = 64_000;
            return content.Length <= maxSampleChars ? content : content.Substring(0, maxSampleChars);
        }

        private static CsvParseResult BuildResult(List<CsvRawRecord> records, CsvParseOptions options, List<CsvParseWarning> warnings)
        {
            var result = new CsvParseResult
            {
                EffectiveDelimiter = options.Delimiter,
                SourceRecordCount = records.Count,
                Warnings = warnings
            };

            if (records.Count == 0)
            {
                return result;
            }

            var dataRecords = records;
            List<string>? header = null;

            if (options.HasHeaderRecord)
            {
                header = records[0].Fields;
                dataRecords = records.Skip(1).ToList();
            }

            int maxColumns = Math.Max(
                header?.Count ?? 0,
                dataRecords.Count == 0 ? 0 : dataRecords.Max(r => r.Fields.Count));

            for (int i = 0; i < maxColumns; i++)
            {
                string originalName = header != null && i < header.Count ? header[i] : string.Empty;
                string displayName = CreateColumnName(originalName, i, result.Columns);
                result.Columns.Add(new CsvColumn
                {
                    Index = i,
                    OriginalName = originalName,
                    DisplayName = displayName
                });
            }

            int expectedColumnCount = result.Columns.Count;
            int rowNumber = 1;
            foreach (var rawRecord in dataRecords)
            {
                if (rawRecord.Fields.Count != expectedColumnCount)
                {
                    warnings.Add(new CsvParseWarning
                    {
                        Severity = CsvWarningSeverity.Warning,
                        RowNumber = rowNumber,
                        Message = FormatResource("CsvViewer_WarningColumnCount", "Row has {0} cell(s), expected {1}.", rawRecord.Fields.Count, expectedColumnCount)
                    });
                }

                var row = new CsvRow
                {
                    RowNumber = rowNumber,
                    SourceLineStart = rawRecord.StartLine,
                    SourceLineEnd = rawRecord.EndLine
                };

                for (int i = 0; i < expectedColumnCount; i++)
                {
                    string value = i < rawRecord.Fields.Count ? rawRecord.Fields[i] : string.Empty;
                    if (value.Length > LargeCellWarningLength)
                    {
                        warnings.Add(new CsvParseWarning
                        {
                            Severity = CsvWarningSeverity.Warning,
                            RowNumber = rowNumber,
                            ColumnNumber = i + 1,
                            Message = FormatResource("CsvViewer_WarningLargeCell", "Cell contains {0} characters. Display is shortened, copy still uses the full value.", value.Length)
                        });
                    }

                    row.Cells.Add(new CsvCell
                    {
                        RowNumber = rowNumber,
                        ColumnIndex = i,
                        Value = value
                    });
                }

                result.Rows.Add(row);
                rowNumber++;
            }

            return result;
        }

        private static string CreateColumnName(string originalName, int index, IList<CsvColumn> existingColumns)
        {
            string baseName = string.IsNullOrWhiteSpace(originalName)
                ? FormatResource("CsvViewer_DefaultColumnName", "Column {0}", index + 1)
                : originalName.Trim();

            string name = baseName;
            int suffix = 2;
            while (existingColumns.Any(c => c.DisplayName.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                name = $"{baseName} ({suffix})";
                suffix++;
            }

            return name;
        }

        private static string FormatResource(string key, string fallbackFormat, params object[] args)
        {
            var format = ResourceHelper.GetString(key);
            if (format == key)
            {
                format = fallbackFormat;
            }

            return string.Format(format, args);
        }
    }
}
