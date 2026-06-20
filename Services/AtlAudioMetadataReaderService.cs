using ATL;
using FileCraft.Models;
using FileCraft.Services.Interfaces;
using System.Globalization;
using System.IO;

namespace FileCraft.Services
{
    public sealed class AtlAudioMetadataReaderService : IAudioMetadataReaderService
    {
        private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".aa", ".aac", ".aax", ".ac3", ".aif", ".aifc", ".aiff", ".ape", ".asf", ".bwav", ".bwf",
            ".caf", ".dsd", ".dsf", ".dts", ".flac", ".m4a", ".m4b", ".midi", ".mid", ".mka", ".mp1",
            ".mp2", ".mp3", ".mp4", ".mpc", ".oga", ".ogg", ".opus", ".spx", ".tak", ".tta", ".wav",
            ".webm", ".wma", ".wv"
        };

        public bool IsSupportedAudioFile(string filePath)
        {
            return SupportedExtensions.Contains(Path.GetExtension(filePath));
        }

        public AudioMetadataReadResult Read(string filePath)
        {
            if (!IsSupportedAudioFile(filePath))
            {
                return AudioMetadataReadResult.NotAudio;
            }

            try
            {
                var track = new Track(filePath);

                return new AudioMetadataReadResult
                {
                    IsSupportedAudioFile = true,
                    Success = true,
                    Status = "OK",
                    Metadata = new AudioMetadata
                    {
                        Title = Normalize(track.Title),
                        Artist = Normalize(track.Artist),
                        Album = Normalize(track.Album),
                        AlbumArtist = Normalize(track.AlbumArtist),
                        Year = track.Year > 0 ? Convert.ToString(track.Year, CultureInfo.InvariantCulture) ?? string.Empty : string.Empty,
                        Genre = Normalize(track.Genre),
                        TrackNumber = track.TrackNumber > 0 ? Convert.ToString(track.TrackNumber, CultureInfo.InvariantCulture) ?? string.Empty : string.Empty,
                        Duration = FormatDuration(track.DurationMs)
                    }
                };
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or ArgumentException)
            {
                return CreateError(ex);
            }
            catch (Exception ex)
            {
                return CreateError(ex);
            }
        }

        private static AudioMetadataReadResult CreateError(Exception ex)
        {
            return new AudioMetadataReadResult
            {
                IsSupportedAudioFile = true,
                Success = false,
                Status = "Error",
                ErrorMessage = ex.Message
            };
        }

        private static string Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static string FormatDuration(double durationMs)
        {
            if (durationMs <= 0)
            {
                return string.Empty;
            }

            var duration = TimeSpan.FromMilliseconds(durationMs);
            return duration.TotalHours >= 1
                ? duration.ToString(@"h\:mm\:ss", CultureInfo.InvariantCulture)
                : duration.ToString(@"m\:ss", CultureInfo.InvariantCulture);
        }
    }
}
