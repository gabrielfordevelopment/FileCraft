using FileCraft.Models;
using FileCraft.Services.Interfaces;
using System.IO;

namespace FileCraft.Services
{
    public class CsvDelimiterDetector : ICsvDelimiterDetector
    {
        private static readonly char[] Candidates = { ',', ';', '\t', '|' };
        private readonly CsvReaderCore _readerCore = new();

        public char DetectDelimiter(string sample, IList<CsvParseWarning> warnings)
        {
            if (string.IsNullOrWhiteSpace(sample))
            {
                warnings.Add(CreateWarning("Delimiter detection had no content to inspect. Comma was used."));
                return ',';
            }

            var scores = new List<(char Delimiter, int Score, int ConsistentRows, int MaxColumns)>();

            foreach (var candidate in Candidates)
            {
                var candidateWarnings = new List<CsvParseWarning>();
                var options = new CsvParseOptions { Delimiter = candidate, HasHeaderRecord = false };
                var records = _readerCore.ReadRecords(new StringReader(sample), options, candidateWarnings)
                    .Where(r => r.Fields.Count > 0)
                    .Take(10)
                    .ToList();

                if (records.Count == 0)
                {
                    scores.Add((candidate, 0, 0, 0));
                    continue;
                }

                var fieldCounts = records.Select(r => r.Fields.Count).ToList();
                int maxColumns = fieldCounts.Max();
                int mostCommonCount = fieldCounts
                    .GroupBy(c => c)
                    .OrderByDescending(g => g.Count())
                    .ThenByDescending(g => g.Key)
                    .First()
                    .Key;
                int consistentRows = fieldCounts.Count(c => c == mostCommonCount);
                int score = (maxColumns - 1) * 100 + consistentRows * 10 - fieldCounts.Distinct().Count();

                scores.Add((candidate, score, consistentRows, maxColumns));
            }

            var best = scores.OrderByDescending(s => s.Score).ThenByDescending(s => s.MaxColumns).First();
            var tied = scores.Count(s => s.Score == best.Score && s.MaxColumns == best.MaxColumns);

            if (best.MaxColumns <= 1)
            {
                warnings.Add(CreateWarning("Delimiter detection found only one column. Comma was used."));
                return ',';
            }

            if (tied > 1)
            {
                warnings.Add(CreateWarning($"Delimiter detection was ambiguous. '{FormatDelimiter(best.Delimiter)}' was selected."));
            }

            return best.Delimiter;
        }

        private static CsvParseWarning CreateWarning(string message)
        {
            return new CsvParseWarning
            {
                Severity = CsvWarningSeverity.Warning,
                Message = message
            };
        }

        private static string FormatDelimiter(char delimiter)
        {
            return delimiter == '\t' ? "tab" : delimiter.ToString();
        }
    }
}
