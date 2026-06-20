using FileCraft.Models;

namespace FileCraft.Services.Interfaces
{
    public interface IAudioMetadataReaderService
    {
        bool IsSupportedAudioFile(string filePath);

        AudioMetadataReadResult Read(string filePath);
    }
}
