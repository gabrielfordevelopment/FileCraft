using FileCraft.Models;
using FileCraft.Services.Interfaces;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;

namespace FileCraft.Services
{
    public sealed class ShellAudioMetadataReaderService : IAudioMetadataReaderService
    {
        private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".aac", ".aif", ".aifc", ".aiff", ".flac", ".m4a", ".m4b", ".m4p", ".mp3", ".mp4",
            ".oga", ".ogg", ".opus", ".wav", ".wma"
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

            object? shell = null;
            object? folder = null;
            object? folderItem = null;

            try
            {
                var directoryPath = Path.GetDirectoryName(filePath);
                var fileName = Path.GetFileName(filePath);

                if (string.IsNullOrWhiteSpace(directoryPath) || string.IsNullOrWhiteSpace(fileName))
                {
                    return CreateError("The audio file path is invalid.");
                }

                var shellType = Type.GetTypeFromProgID("Shell.Application");
                if (shellType == null)
                {
                    return CreateError("Windows Shell metadata services are not available.");
                }

                shell = Activator.CreateInstance(shellType);
                if (shell == null)
                {
                    return CreateError("Windows Shell metadata services could not be started.");
                }

                folder = Invoke(shell, "NameSpace", directoryPath);
                if (folder == null)
                {
                    return CreateError("The audio file folder could not be opened.");
                }

                folderItem = Invoke(folder, "ParseName", fileName);
                if (folderItem == null)
                {
                    return CreateError("The audio file could not be opened.");
                }

                return new AudioMetadataReadResult
                {
                    IsSupportedAudioFile = true,
                    Success = true,
                    Status = "OK",
                    Metadata = new AudioMetadata
                    {
                        Title = ReadString(folderItem, "System.Title"),
                        Artist = ReadString(folderItem, "System.Music.Artist"),
                        Album = ReadString(folderItem, "System.Music.AlbumTitle"),
                        AlbumArtist = ReadString(folderItem, "System.Music.AlbumArtist"),
                        Year = ReadString(folderItem, "System.Media.Year"),
                        Genre = ReadString(folderItem, "System.Music.Genre"),
                        TrackNumber = ReadString(folderItem, "System.Music.TrackNumber"),
                        Duration = FormatDuration(ReadValue(folderItem, "System.Media.Duration"))
                    }
                };
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or ArgumentException or COMException)
            {
                return CreateError(ex.Message);
            }
            catch (Exception ex)
            {
                return CreateError(ex.Message);
            }
            finally
            {
                ReleaseComObject(folderItem);
                ReleaseComObject(folder);
                ReleaseComObject(shell);
            }
        }

        private static object? Invoke(object target, string methodName, params object[] args)
        {
            return target.GetType().InvokeMember(
                methodName,
                System.Reflection.BindingFlags.InvokeMethod,
                binder: null,
                target,
                args,
                CultureInfo.InvariantCulture);
        }

        private static object? ReadValue(object folderItem, string propertyName)
        {
            return Invoke(folderItem, "ExtendedProperty", propertyName);
        }

        private static string ReadString(object folderItem, string propertyName)
        {
            return Normalize(ReadValue(folderItem, propertyName));
        }

        private static AudioMetadataReadResult CreateError(string message)
        {
            return new AudioMetadataReadResult
            {
                IsSupportedAudioFile = true,
                Success = false,
                Status = "Error",
                ErrorMessage = message
            };
        }

        private static string Normalize(object? value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            if (value is string[] values)
            {
                return string.Join(", ", values.Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v.Trim()));
            }

            return Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
        }

        private static string FormatDuration(object? value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            if (!long.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out var durationValue))
            {
                return string.Empty;
            }

            if (durationValue <= 0)
            {
                return string.Empty;
            }

            var duration = TimeSpan.FromTicks(durationValue);
            return duration.TotalHours >= 1
                ? duration.ToString(@"h\:mm\:ss", CultureInfo.InvariantCulture)
                : duration.ToString(@"m\:ss", CultureInfo.InvariantCulture);
        }

        private static void ReleaseComObject(object? value)
        {
            if (value != null && Marshal.IsComObject(value))
            {
                Marshal.FinalReleaseComObject(value);
            }
        }
    }
}
