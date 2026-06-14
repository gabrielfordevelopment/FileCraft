using FileCraft.Models;
using FileCraft.Services;
using System.IO;

var tests = new CsvParserTests();
tests.RunAll();
Console.WriteLine("CSV parser tests passed.");

internal sealed class CsvParserTests
{
    public void RunAll()
    {
        SimpleCsv();
        QuotedDelimiter();
        QuotedNewLine();
        EscapedQuote();
        EmptyAndTrailingFields();
        HeaderAndDuplicateColumns();
        MissingAndExtraColumnsCreateWarnings();
        UnclosedQuoteKeepsDataAndWarns();
        DetectsSemicolonDelimiter();
        DetectsTabDelimiter();
        ManualDelimiterOverride();
        SearchValueUsesFullCellValueShape();
        EmptyCellHasNoTooltipPreview();
    }

    private static CsvParseResult Parse(string content, bool hasHeader = false, CsvDelimiterMode mode = CsvDelimiterMode.Manual, char delimiter = ',')
    {
        var service = new CsvParsingService(new CsvDelimiterDetector());
        return service.ParseAsync(new CsvParseRequest
        {
            InputMode = CsvInputMode.PastedText,
            Text = content,
            HasHeaderRecord = hasHeader,
            DelimiterMode = mode,
            ManualDelimiter = delimiter
        }).GetAwaiter().GetResult();
    }

    private static List<CsvRawRecord> ReadCore(string content, char delimiter = ',')
    {
        var warnings = new List<CsvParseWarning>();
        return new CsvReaderCore().ReadRecords(new StringReader(content), new CsvParseOptions
        {
            Delimiter = delimiter,
            HasHeaderRecord = false
        }, warnings);
    }

    private void SimpleCsv()
    {
        var records = ReadCore("a,b,c\r\n1,2,3");
        Equal(2, records.Count, nameof(SimpleCsv));
        Equal("b", records[0].Fields[1], nameof(SimpleCsv));
        Equal("3", records[1].Fields[2], nameof(SimpleCsv));
    }

    private void QuotedDelimiter()
    {
        var records = ReadCore("\"a,b\",c");
        Equal("a,b", records[0].Fields[0], nameof(QuotedDelimiter));
        Equal("c", records[0].Fields[1], nameof(QuotedDelimiter));
    }

    private void QuotedNewLine()
    {
        var records = ReadCore("\"first\r\nsecond\",tail");
        Equal("first\r\nsecond", records[0].Fields[0], nameof(QuotedNewLine));
        Equal(1, records.Count, nameof(QuotedNewLine));
    }

    private void EscapedQuote()
    {
        var records = ReadCore("\"a \"\"quoted\"\" value\"");
        Equal("a \"quoted\" value", records[0].Fields[0], nameof(EscapedQuote));
    }

    private void EmptyAndTrailingFields()
    {
        var records = ReadCore("a,,");
        Equal(3, records[0].Fields.Count, nameof(EmptyAndTrailingFields));
        Equal(string.Empty, records[0].Fields[1], nameof(EmptyAndTrailingFields));
        Equal(string.Empty, records[0].Fields[2], nameof(EmptyAndTrailingFields));
    }

    private void HeaderAndDuplicateColumns()
    {
        var result = Parse("Name,Name,\r\nA,B,C", hasHeader: true);
        Equal("Name", result.Columns[0].DisplayName, nameof(HeaderAndDuplicateColumns));
        Equal("Name (2)", result.Columns[1].DisplayName, nameof(HeaderAndDuplicateColumns));
        Equal("Column 3", result.Columns[2].DisplayName, nameof(HeaderAndDuplicateColumns));
    }

    private void MissingAndExtraColumnsCreateWarnings()
    {
        var result = Parse("A,B\r\n1\r\n1,2,3", hasHeader: true);
        Equal(3, result.Columns.Count, nameof(MissingAndExtraColumnsCreateWarnings));
        True(result.Warnings.Count > 0, nameof(MissingAndExtraColumnsCreateWarnings));
        Equal(string.Empty, result.Rows[0].Cells[1].Value, nameof(MissingAndExtraColumnsCreateWarnings));
        Equal("3", result.Rows[1].Cells[2].Value, nameof(MissingAndExtraColumnsCreateWarnings));
    }

    private void UnclosedQuoteKeepsDataAndWarns()
    {
        var warnings = new List<CsvParseWarning>();
        var records = new CsvReaderCore().ReadRecords(new StringReader("\"abc"), new CsvParseOptions(), warnings);
        Equal("abc", records[0].Fields[0], nameof(UnclosedQuoteKeepsDataAndWarns));
        True(warnings.Count > 0, nameof(UnclosedQuoteKeepsDataAndWarns));
    }

    private void DetectsSemicolonDelimiter()
    {
        var result = Parse("A;B;C\r\n1;2;3", hasHeader: true, mode: CsvDelimiterMode.Auto);
        Equal(';', result.EffectiveDelimiter, nameof(DetectsSemicolonDelimiter));
        Equal("2", result.Rows[0].Cells[1].Value, nameof(DetectsSemicolonDelimiter));
    }

    private void DetectsTabDelimiter()
    {
        var result = Parse("A\tB\tC\r\n1\t2\t3", hasHeader: true, mode: CsvDelimiterMode.Auto);
        Equal('\t', result.EffectiveDelimiter, nameof(DetectsTabDelimiter));
        Equal("3", result.Rows[0].Cells[2].Value, nameof(DetectsTabDelimiter));
    }

    private void ManualDelimiterOverride()
    {
        var result = Parse("A|B\r\n1|2", hasHeader: true, mode: CsvDelimiterMode.Manual, delimiter: '|');
        Equal('|', result.EffectiveDelimiter, nameof(ManualDelimiterOverride));
        Equal("2", result.Rows[0].Cells[1].Value, nameof(ManualDelimiterOverride));
    }

    private void SearchValueUsesFullCellValueShape()
    {
        var result = Parse("A\r\n" + new string('x', 520) + "needle", hasHeader: true);
        True(result.Rows[0].Cells[0].DisplayValue.Length == 500, nameof(SearchValueUsesFullCellValueShape));
        True(result.Rows[0].Cells[0].Value.Contains("needle", StringComparison.Ordinal), nameof(SearchValueUsesFullCellValueShape));
    }

    private void EmptyCellHasNoTooltipPreview()
    {
        var result = Parse("A,B\r\n1,", hasHeader: true);
        True(result.Rows[0].Cells[1].ShortPreview == null, nameof(EmptyCellHasNoTooltipPreview));
    }

    private static void Equal<T>(T expected, T actual, string testName)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"{testName} failed. Expected '{expected}', got '{actual}'.");
        }
    }

    private static void True(bool condition, string testName)
    {
        if (!condition)
        {
            throw new InvalidOperationException($"{testName} failed.");
        }
    }
}
