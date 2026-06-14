using FileCraft.Models;

namespace FileCraft.Services.Interfaces
{
    public interface ICsvParsingService
    {
        Task<CsvParseResult> ParseAsync(CsvParseRequest request, CancellationToken cancellationToken = default);
    }
}
