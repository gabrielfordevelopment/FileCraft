using FileCraft.Services.Interfaces;
using System.IO;
using System.Text;

namespace FileCraft.Services
{
    public sealed class DelimitedTextWriter : IDelimitedTextWriter
    {
        private readonly char _delimiter;
        private readonly bool _protectSpreadsheetFormulas;

        public DelimitedTextWriter(char delimiter = ';', bool protectSpreadsheetFormulas = false)
        {
            _delimiter = delimiter;
            _protectSpreadsheetFormulas = protectSpreadsheetFormulas;
        }

        public async Task WriteRecordAsync(TextWriter writer, IEnumerable<string> fields, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await writer.WriteLineAsync(string.Join(_delimiter, fields.Select(FormatField)));
        }

        public string FormatField(string? value)
        {
            value ??= string.Empty;

            if (_protectSpreadsheetFormulas && StartsWithFormulaCharacter(value))
            {
                value = "'" + value;
            }

            bool mustQuote = value.Contains(_delimiter)
                || value.Contains('"')
                || value.Contains('\r')
                || value.Contains('\n');

            if (!mustQuote)
            {
                return value;
            }

            var escaped = value.Replace("\"", "\"\"");
            return $"\"{escaped}\"";
        }

        private static bool StartsWithFormulaCharacter(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            return value[0] is '=' or '+' or '-' or '@' or '\t' or '\r' or '\n';
        }
    }
}
