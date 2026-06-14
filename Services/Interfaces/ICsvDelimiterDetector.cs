using FileCraft.Models;

namespace FileCraft.Services.Interfaces
{
    public interface ICsvDelimiterDetector
    {
        char DetectDelimiter(string sample, IList<CsvParseWarning> warnings);
    }
}
