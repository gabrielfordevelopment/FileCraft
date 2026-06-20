using System.IO;

namespace FileCraft.Services.Interfaces
{
    public interface IDelimitedTextWriter
    {
        Task WriteRecordAsync(TextWriter writer, IEnumerable<string> fields, CancellationToken cancellationToken = default);

        string FormatField(string? value);
    }
}
