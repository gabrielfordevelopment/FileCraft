using FileCraft.Models;
using FileCraft.Services;
using FileCraft.Services.Interfaces;
using System.ComponentModel;
using System.IO;

var tests = new CsvParserTests();
tests.RunAll();
Console.WriteLine("CSV parser tests passed.");

var folderExportTests = new FolderContentExportTests();
folderExportTests.RunAll();
Console.WriteLine("Folder content export tests passed.");

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

internal sealed class FolderContentExportTests
{
    public void RunAll()
    {
        DelimitedWriterEscapesSemicolonQuoteAndNewLine();
        DelimitedWriterProtectsSpreadsheetFormulas();
        DelimitedWriterHandlesNullEmptyAndWholeRecords();
        FolderExportColumnsUseStableIds();
        FolderExportColumnsDoNotKeepDisplayNameAliases();
        FolderExportSkipsAudioReaderWhenNoAudioColumnsAreSelected();
        FolderExportReadsAudioOncePerFileWhenAudioColumnsAreSelected();
        FolderExportKeepsNonAudioRowsWithEmptyAudioMetadata();
        FolderExportWritesAudioErrorsAsDiagnostics();
        FolderExportRejectsUnknownOnlyColumnSelection();
        FolderExportAppliesFormulaProtectionToMetadataValues();
    }

    private void DelimitedWriterEscapesSemicolonQuoteAndNewLine()
    {
        var writer = new DelimitedTextWriter(';');

        Equal("\"a;b\"", writer.FormatField("a;b"), nameof(DelimitedWriterEscapesSemicolonQuoteAndNewLine));
        Equal("\"a\"\"b\"", writer.FormatField("a\"b"), nameof(DelimitedWriterEscapesSemicolonQuoteAndNewLine));
        Equal("\"a\r\nb\"", writer.FormatField("a\r\nb"), nameof(DelimitedWriterEscapesSemicolonQuoteAndNewLine));
        Equal("abc", writer.FormatField("abc"), nameof(DelimitedWriterEscapesSemicolonQuoteAndNewLine));
    }

    private void DelimitedWriterProtectsSpreadsheetFormulas()
    {
        var writer = new DelimitedTextWriter(';', protectSpreadsheetFormulas: true);

        Equal("'=SUM(A1:A2)", writer.FormatField("=SUM(A1:A2)"), nameof(DelimitedWriterProtectsSpreadsheetFormulas));
        Equal("'+1", writer.FormatField("+1"), nameof(DelimitedWriterProtectsSpreadsheetFormulas));
        Equal("'-1", writer.FormatField("-1"), nameof(DelimitedWriterProtectsSpreadsheetFormulas));
        Equal("'@value", writer.FormatField("@value"), nameof(DelimitedWriterProtectsSpreadsheetFormulas));
        Equal("\"'=1;2\"", writer.FormatField("=1;2"), nameof(DelimitedWriterProtectsSpreadsheetFormulas));
        Equal("safe", writer.FormatField("safe"), nameof(DelimitedWriterProtectsSpreadsheetFormulas));
    }

    private void DelimitedWriterHandlesNullEmptyAndWholeRecords()
    {
        var writer = new DelimitedTextWriter(';');

        Equal(string.Empty, writer.FormatField(null), nameof(DelimitedWriterHandlesNullEmptyAndWholeRecords));
        Equal(string.Empty, writer.FormatField(string.Empty), nameof(DelimitedWriterHandlesNullEmptyAndWholeRecords));

        using var stringWriter = new StringWriter();
        writer.WriteRecordAsync(stringWriter, new[] { "a", "b;c", "d\"e", string.Empty }).GetAwaiter().GetResult();
        Equal($"a;\"b;c\";\"d\"\"e\";{Environment.NewLine}", stringWriter.ToString(), nameof(DelimitedWriterHandlesNullEmptyAndWholeRecords));
    }

    private void FolderExportColumnsUseStableIds()
    {
        var defaultIds = FolderExportColumns.GetDefaultSelectedColumnIds();

        True(defaultIds.Contains(FolderExportColumnIds.FileName), nameof(FolderExportColumnsUseStableIds));
        True(defaultIds.Contains(FolderExportColumnIds.FileFullPath), nameof(FolderExportColumnsUseStableIds));
        True(!defaultIds.Contains(FolderExportColumnIds.AudioTitle), nameof(FolderExportColumnsUseStableIds));
    }

    private void FolderExportColumnsDoNotKeepDisplayNameAliases()
    {
        True(!FolderExportColumns.ById.ContainsKey("Name"), nameof(FolderExportColumnsDoNotKeepDisplayNameAliases));
        True(!FolderExportColumns.ById.ContainsKey("Size (byte)"), nameof(FolderExportColumnsDoNotKeepDisplayNameAliases));
        True(FolderExportColumns.ById.ContainsKey(FolderExportColumnIds.FileName), nameof(FolderExportColumnsDoNotKeepDisplayNameAliases));
    }

    private void FolderExportSkipsAudioReaderWhenNoAudioColumnsAreSelected()
    {
        using var fixture = FolderExportFixture.Create(("plain.txt", "content"));
        var audioReader = new FakeAudioMetadataReader();
        var service = fixture.CreateService(audioReader);

        var outputPath = service.ExportFolderContentsAsync(
            fixture.OutputDirectory,
            new[] { fixture.SourceDirectory },
            "file-only",
            new[] { FolderExportColumnIds.FileName, FolderExportColumnIds.FileExtension },
            new FolderContentExportOptions()).GetAwaiter().GetResult();

        var lines = File.ReadAllLines(outputPath);
        Equal("Name;Format", lines[0], nameof(FolderExportSkipsAudioReaderWhenNoAudioColumnsAreSelected));
        Equal("plain.txt;.txt", lines[1], nameof(FolderExportSkipsAudioReaderWhenNoAudioColumnsAreSelected));
        Equal(0, audioReader.ReadCalls, nameof(FolderExportSkipsAudioReaderWhenNoAudioColumnsAreSelected));
    }

    private void FolderExportReadsAudioOncePerFileWhenAudioColumnsAreSelected()
    {
        using var fixture = FolderExportFixture.Create(("one.mp3", "fake"), ("two.flac", "fake"));
        var audioReader = new FakeAudioMetadataReader();
        audioReader.Results[fixture.PathFor("one.mp3")] = SuccessfulAudio("Artist A", "Title A");
        audioReader.Results[fixture.PathFor("two.flac")] = SuccessfulAudio("Artist B", "Title B");
        var service = fixture.CreateService(audioReader);

        var outputPath = service.ExportFolderContentsAsync(
            fixture.OutputDirectory,
            new[] { fixture.SourceDirectory },
            "audio",
            new[] { FolderExportColumnIds.FileName, FolderExportColumnIds.AudioArtist, FolderExportColumnIds.AudioTitle },
            new FolderContentExportOptions()).GetAwaiter().GetResult();

        var lines = File.ReadAllLines(outputPath);
        Equal("Name;Audio artist;Audio title", lines[0], nameof(FolderExportReadsAudioOncePerFileWhenAudioColumnsAreSelected));
        True(lines.Contains("one.mp3;Artist A;Title A"), nameof(FolderExportReadsAudioOncePerFileWhenAudioColumnsAreSelected));
        True(lines.Contains("two.flac;Artist B;Title B"), nameof(FolderExportReadsAudioOncePerFileWhenAudioColumnsAreSelected));
        Equal(2, audioReader.ReadCalls, nameof(FolderExportReadsAudioOncePerFileWhenAudioColumnsAreSelected));
    }

    private void FolderExportKeepsNonAudioRowsWithEmptyAudioMetadata()
    {
        using var fixture = FolderExportFixture.Create(("plain.txt", "content"));
        var audioReader = new FakeAudioMetadataReader();
        audioReader.Results[fixture.PathFor("plain.txt")] = AudioMetadataReadResult.NotAudio;
        var service = fixture.CreateService(audioReader);

        var outputPath = service.ExportFolderContentsAsync(
            fixture.OutputDirectory,
            new[] { fixture.SourceDirectory },
            "not-audio",
            new[] { FolderExportColumnIds.FileName, FolderExportColumnIds.AudioArtist, FolderExportColumnIds.AudioIsAudioFile, FolderExportColumnIds.AudioMetadataStatus },
            new FolderContentExportOptions()).GetAwaiter().GetResult();

        var lines = File.ReadAllLines(outputPath);
        Equal("plain.txt;;False;Not audio", lines[1], nameof(FolderExportKeepsNonAudioRowsWithEmptyAudioMetadata));
        Equal(1, audioReader.ReadCalls, nameof(FolderExportKeepsNonAudioRowsWithEmptyAudioMetadata));
    }

    private void FolderExportWritesAudioErrorsAsDiagnostics()
    {
        using var fixture = FolderExportFixture.Create(("broken.mp3", "fake"));
        var audioReader = new FakeAudioMetadataReader();
        audioReader.Results[fixture.PathFor("broken.mp3")] = new AudioMetadataReadResult
        {
            IsSupportedAudioFile = true,
            Success = false,
            Status = "Error",
            ErrorMessage = "bad;tag"
        };
        var service = fixture.CreateService(audioReader);

        var outputPath = service.ExportFolderContentsAsync(
            fixture.OutputDirectory,
            new[] { fixture.SourceDirectory },
            "error",
            new[] { FolderExportColumnIds.FileName, FolderExportColumnIds.AudioMetadataStatus, FolderExportColumnIds.AudioMetadataError },
            new FolderContentExportOptions()).GetAwaiter().GetResult();

        var lines = File.ReadAllLines(outputPath);
        Equal("broken.mp3;Error;\"bad;tag\"", lines[1], nameof(FolderExportWritesAudioErrorsAsDiagnostics));
    }

    private void FolderExportRejectsUnknownOnlyColumnSelection()
    {
        using var fixture = FolderExportFixture.Create(("plain.txt", "content"));
        var service = fixture.CreateService(new FakeAudioMetadataReader());

        Throws<ArgumentException>(() => service.ExportFolderContentsAsync(
            fixture.OutputDirectory,
            new[] { fixture.SourceDirectory },
            "unknown",
            new[] { "Name", "unknown.column" },
            new FolderContentExportOptions()).GetAwaiter().GetResult(), nameof(FolderExportRejectsUnknownOnlyColumnSelection));
    }

    private void FolderExportAppliesFormulaProtectionToMetadataValues()
    {
        using var fixture = FolderExportFixture.Create(("formula.mp3", "fake"));
        var audioReader = new FakeAudioMetadataReader();
        audioReader.Results[fixture.PathFor("formula.mp3")] = SuccessfulAudio("=Danger", "+Title");
        var service = fixture.CreateService(audioReader);

        var outputPath = service.ExportFolderContentsAsync(
            fixture.OutputDirectory,
            new[] { fixture.SourceDirectory },
            "formula",
            new[] { FolderExportColumnIds.AudioArtist, FolderExportColumnIds.AudioTitle },
            new FolderContentExportOptions { ProtectSpreadsheetFormulas = true }).GetAwaiter().GetResult();

        var lines = File.ReadAllLines(outputPath);
        Equal("'=Danger;'+Title", lines[1], nameof(FolderExportAppliesFormulaProtectionToMetadataValues));
    }

    private static AudioMetadataReadResult SuccessfulAudio(string artist, string title)
    {
        return new AudioMetadataReadResult
        {
            IsSupportedAudioFile = true,
            Success = true,
            Status = "OK",
            Metadata = new AudioMetadata
            {
                Artist = artist,
                Title = title,
                Album = "Album",
                AlbumArtist = artist,
                Year = "2024",
                Genre = "Genre",
                TrackNumber = "1",
                Duration = "3:10"
            }
        };
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

    private static void Throws<TException>(Action action, string testName) where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"{testName} failed. Expected {typeof(TException).Name}, got {ex.GetType().Name}.", ex);
        }

        throw new InvalidOperationException($"{testName} failed. Expected {typeof(TException).Name}, got no exception.");
    }

    private sealed class FolderExportFixture : IDisposable
    {
        public string RootDirectory { get; }
        public string SourceDirectory { get; }
        public string OutputDirectory { get; }
        public List<FileInfo> Files { get; } = new();

        private FolderExportFixture(string rootDirectory)
        {
            RootDirectory = rootDirectory;
            SourceDirectory = Path.Combine(rootDirectory, "source");
            OutputDirectory = Path.Combine(rootDirectory, "output");
            Directory.CreateDirectory(SourceDirectory);
            Directory.CreateDirectory(OutputDirectory);
        }

        public static FolderExportFixture Create(params (string Name, string Content)[] files)
        {
            var fixture = new FolderExportFixture(Path.Combine(Path.GetTempPath(), "FileCraftTests", Guid.NewGuid().ToString("N")));

            foreach (var file in files)
            {
                var path = Path.Combine(fixture.SourceDirectory, file.Name);
                File.WriteAllText(path, file.Content);
                fixture.Files.Add(new FileInfo(path));
            }

            return fixture;
        }

        public string PathFor(string fileName)
        {
            return Path.Combine(SourceDirectory, fileName);
        }

        public FileOperationService CreateService(FakeAudioMetadataReader audioReader)
        {
            return new FileOperationService(
                new FakeSharedStateService(),
                new FakeFileQueryService(Files),
                audioReader);
        }

        public void Dispose()
        {
            if (Directory.Exists(RootDirectory))
            {
                Directory.Delete(RootDirectory, recursive: true);
            }
        }
    }

    private sealed class FakeSharedStateService : ISharedStateService
    {
        public string SourcePath { get; set; } = string.Empty;
        public string DestinationPath { get; set; } = string.Empty;
        public List<string> IgnoredFolders { get; set; } = new();

        event PropertyChangedEventHandler? INotifyPropertyChanged.PropertyChanged
        {
            add { }
            remove { }
        }
    }

    private sealed class FakeFileQueryService : IFileQueryService
    {
        private readonly List<FileInfo> _files;

        public FakeFileQueryService(IEnumerable<FileInfo> files)
        {
            _files = files.ToList();
        }

        public HashSet<string> GetAvailableExtensions(IEnumerable<(string Path, bool Recursive)> folderConfigs, ISet<string> ignoredFolderNames)
        {
            return _files.Select(file => file.Extension).ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        public IEnumerable<SelectableFile> GetFilesByExtensions(string basePath, IEnumerable<(string Path, bool Recursive)> folderConfigs, ISet<string> selectedExtensions, ISet<string> ignoredFolderNames)
        {
            return _files
                .Where(file => selectedExtensions.Contains(file.Extension))
                .Select(file => new SelectableFile
                {
                    FileName = file.Name,
                    FullPath = file.FullName,
                    RelativePath = Path.GetRelativePath(basePath, file.FullName)
                });
        }

        public IEnumerable<FileInfo> GetAllFiles(IEnumerable<string> folderPaths, ISet<string> ignoredFolderNames)
        {
            return _files;
        }
    }

    private sealed class FakeAudioMetadataReader : IAudioMetadataReaderService
    {
        public Dictionary<string, AudioMetadataReadResult> Results { get; } = new(StringComparer.OrdinalIgnoreCase);
        public int ReadCalls { get; private set; }

        public bool IsSupportedAudioFile(string filePath)
        {
            return Results.TryGetValue(filePath, out var result) && result.IsSupportedAudioFile;
        }

        public AudioMetadataReadResult Read(string filePath)
        {
            ReadCalls++;
            return Results.TryGetValue(filePath, out var result) ? result : AudioMetadataReadResult.NotAudio;
        }
    }
}
