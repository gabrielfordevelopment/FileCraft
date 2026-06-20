namespace FileCraft.Models
{
    public sealed class AudioMetadata
    {
        public string Title { get; init; } = string.Empty;
        public string Artist { get; init; } = string.Empty;
        public string Album { get; init; } = string.Empty;
        public string AlbumArtist { get; init; } = string.Empty;
        public string Year { get; init; } = string.Empty;
        public string Genre { get; init; } = string.Empty;
        public string TrackNumber { get; init; } = string.Empty;
        public string Duration { get; init; } = string.Empty;
    }

    public sealed class AudioMetadataReadResult
    {
        public bool IsSupportedAudioFile { get; init; }
        public bool Success { get; init; }
        public AudioMetadata Metadata { get; init; } = new();
        public string Status { get; init; } = "Not audio";
        public string ErrorMessage { get; init; } = string.Empty;

        public static AudioMetadataReadResult NotAudio { get; } = new()
        {
            IsSupportedAudioFile = false,
            Success = false,
            Status = "Not audio"
        };
    }
}
