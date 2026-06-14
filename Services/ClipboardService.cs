using FileCraft.Services.Interfaces;
using Clipboard = System.Windows.Clipboard;

namespace FileCraft.Services
{
    public class ClipboardService : IClipboardService
    {
        public void SetText(string text)
        {
            Clipboard.SetText(text ?? string.Empty);
        }
    }
}
