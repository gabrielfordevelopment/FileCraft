using FileCraft.Models;
using System.IO;
using System.Text;

namespace FileCraft.Services
{
    public class CsvReaderCore
    {
        private enum ParserState
        {
            StartField,
            UnquotedField,
            QuotedField,
            QuoteInQuotedField,
            AfterQuotedField
        }

        public List<CsvRawRecord> ReadRecords(TextReader reader, CsvParseOptions options, IList<CsvParseWarning> warnings, CancellationToken cancellationToken = default)
        {
            var text = reader.ReadToEnd();
            return ReadRecords(text, options, warnings, cancellationToken);
        }

        public List<CsvRawRecord> ReadRecords(string text, CsvParseOptions options, IList<CsvParseWarning> warnings, CancellationToken cancellationToken = default)
        {
            var records = new List<CsvRawRecord>();
            if (string.IsNullOrEmpty(text))
            {
                return records;
            }

            var state = ParserState.StartField;
            var record = new CsvRawRecord { StartLine = 1, EndLine = 1 };
            var field = new StringBuilder();
            int line = 1;
            int fieldStartLine = 1;
            bool recordHasContent = false;
            bool lastTokenWasDelimiter = false;
            bool hasPendingRecord = false;

            for (int i = 0; i < text.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                char c = text[i];
                bool isNewLine = IsNewLineStart(text, i);

                switch (state)
                {
                    case ParserState.StartField:
                        hasPendingRecord = true;
                        fieldStartLine = line;
                        if (c == options.Quote)
                        {
                            state = ParserState.QuotedField;
                            recordHasContent = true;
                            lastTokenWasDelimiter = false;
                        }
                        else if (c == options.Delimiter)
                        {
                            AddField(record, field);
                            recordHasContent = true;
                            lastTokenWasDelimiter = true;
                        }
                        else if (isNewLine)
                        {
                            AddField(record, field);
                            record.IsBlank = !recordHasContent && record.Fields.Count == 1 && record.Fields[0].Length == 0;
                            record.EndLine = line;
                            records.Add(record);
                            ConsumeNewLine(text, ref i, ref line);
                            record = new CsvRawRecord { StartLine = line, EndLine = line };
                            recordHasContent = false;
                            lastTokenWasDelimiter = false;
                            hasPendingRecord = false;
                        }
                        else
                        {
                            field.Append(c);
                            if (!char.IsWhiteSpace(c))
                            {
                                recordHasContent = true;
                            }
                            state = ParserState.UnquotedField;
                            lastTokenWasDelimiter = false;
                        }
                        break;

                    case ParserState.UnquotedField:
                        if (c == options.Delimiter)
                        {
                            AddField(record, field);
                            state = ParserState.StartField;
                            lastTokenWasDelimiter = true;
                        }
                        else if (isNewLine)
                        {
                            AddField(record, field);
                            record.EndLine = line;
                            record.IsBlank = !recordHasContent && record.Fields.All(string.IsNullOrEmpty);
                            records.Add(record);
                            ConsumeNewLine(text, ref i, ref line);
                            record = new CsvRawRecord { StartLine = line, EndLine = line };
                            recordHasContent = false;
                            lastTokenWasDelimiter = false;
                            hasPendingRecord = false;
                            state = ParserState.StartField;
                        }
                        else
                        {
                            if (c == options.Quote)
                            {
                                warnings.Add(new CsvParseWarning
                                {
                                    RowNumber = records.Count + 1,
                                    ColumnNumber = record.Fields.Count + 1,
                                    Message = "Quote found inside an unquoted field. The quote was kept as data."
                                });
                            }

                            field.Append(c);
                            if (!char.IsWhiteSpace(c))
                            {
                                recordHasContent = true;
                            }
                        }
                        break;

                    case ParserState.QuotedField:
                        if (c == options.Quote)
                        {
                            state = ParserState.QuoteInQuotedField;
                        }
                        else if (isNewLine)
                        {
                            field.Append(GetNewLineText(text, i));
                            ConsumeNewLine(text, ref i, ref line);
                            record.EndLine = line;
                        }
                        else
                        {
                            field.Append(c);
                        }
                        break;

                    case ParserState.QuoteInQuotedField:
                        if (c == options.Quote)
                        {
                            field.Append(options.Quote);
                            state = ParserState.QuotedField;
                        }
                        else if (c == options.Delimiter)
                        {
                            AddField(record, field);
                            state = ParserState.StartField;
                            lastTokenWasDelimiter = true;
                        }
                        else if (isNewLine)
                        {
                            AddField(record, field);
                            record.EndLine = line;
                            records.Add(record);
                            ConsumeNewLine(text, ref i, ref line);
                            record = new CsvRawRecord { StartLine = line, EndLine = line };
                            recordHasContent = false;
                            lastTokenWasDelimiter = false;
                            hasPendingRecord = false;
                            state = ParserState.StartField;
                        }
                        else
                        {
                            warnings.Add(new CsvParseWarning
                            {
                                RowNumber = records.Count + 1,
                                ColumnNumber = record.Fields.Count + 1,
                                Message = "Unexpected character after a closing quote. The character was kept as data."
                            });
                            field.Append(c);
                            state = ParserState.AfterQuotedField;
                        }
                        break;

                    case ParserState.AfterQuotedField:
                        if (c == options.Delimiter)
                        {
                            AddField(record, field);
                            state = ParserState.StartField;
                            lastTokenWasDelimiter = true;
                        }
                        else if (isNewLine)
                        {
                            AddField(record, field);
                            record.EndLine = line;
                            records.Add(record);
                            ConsumeNewLine(text, ref i, ref line);
                            record = new CsvRawRecord { StartLine = line, EndLine = line };
                            recordHasContent = false;
                            lastTokenWasDelimiter = false;
                            hasPendingRecord = false;
                            state = ParserState.StartField;
                        }
                        else
                        {
                            field.Append(c);
                        }
                        break;
                }
            }

            if (state == ParserState.QuotedField)
            {
                warnings.Add(new CsvParseWarning
                {
                    RowNumber = records.Count + 1,
                    ColumnNumber = record.Fields.Count + 1,
                    Message = "Quoted field was not closed before the end of input. The partial field was kept."
                });
            }

            if (state == ParserState.QuoteInQuotedField || state == ParserState.AfterQuotedField)
            {
                AddField(record, field);
                hasPendingRecord = true;
            }
            else if (field.Length > 0 || hasPendingRecord || lastTokenWasDelimiter)
            {
                AddField(record, field);
                hasPendingRecord = true;
            }

            if (hasPendingRecord && record.Fields.Count > 0)
            {
                record.EndLine = line;
                record.IsBlank = !recordHasContent && record.Fields.All(string.IsNullOrEmpty);
                records.Add(record);
            }

            if (options.IgnoreBlankLines)
            {
                records = records.Where(r => !r.IsBlank).ToList();
            }

            return records;
        }

        private static void AddField(CsvRawRecord record, StringBuilder field)
        {
            record.Fields.Add(field.ToString());
            field.Clear();
        }

        private static bool IsNewLineStart(string text, int index)
        {
            return text[index] == '\r' || text[index] == '\n';
        }

        private static string GetNewLineText(string text, int index)
        {
            if (text[index] == '\r' && index + 1 < text.Length && text[index + 1] == '\n')
            {
                return "\r\n";
            }

            return text[index].ToString();
        }

        private static void ConsumeNewLine(string text, ref int index, ref int line)
        {
            if (text[index] == '\r' && index + 1 < text.Length && text[index + 1] == '\n')
            {
                index++;
            }

            line++;
        }
    }
}
