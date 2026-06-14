using Fonts;

namespace FileCraft.Shared.Helpers
{
    public class IconDefinition
    {
        public string Glyph { get; }
        public Brush Brush { get; }

        public IconDefinition(string glyph, string colorResourceKey)
        {
            Glyph = glyph;
            Brush = Application.Current.TryFindResource(colorResourceKey) as Brush ?? Brushes.Black;
        }

        public IconDefinition(string glyph, Brush brush)
        {
            Glyph = glyph;
            Brush = brush;
        }
    }

    public static class AppIcons
    {
        public static IconDefinition FileContentExport => new(MaterialIcons.topic, ResourceKeys.FolderIconBrush);
        public static IconDefinition TreeGenerator => new(MaterialIcons.park, ResourceKeys.TreeIconBrush);
        public static IconDefinition FolderContentExport => new(MaterialIcons.dns, ResourceKeys.PrimaryBrush);
        public static IconDefinition CsvViewer => new(MaterialIcons.table_view, ResourceKeys.PrimaryBrush);
        public static IconDefinition Options => new(MaterialIcons.settings, ResourceKeys.GrayTextBrush);

        public static IconDefinition Undo => new(MaterialIcons.keyboard_arrow_left, ResourceKeys.TextBrush);
        public static IconDefinition Redo => new(MaterialIcons.keyboard_arrow_right, ResourceKeys.TextBrush);
        public static IconDefinition Save => new(MaterialIcons.save, ResourceKeys.TextBrush);
        public static IconDefinition Delete => new(MaterialIcons.delete, ResourceKeys.DangerBrush);
        public static IconDefinition Fullscreen => new(MaterialIcons.fullscreen, ResourceKeys.TextBrush);
        public static IconDefinition FullscreenExit => new(MaterialIcons.fullscreen_exit, ResourceKeys.TextBrush);

        public static IconDefinition Warning => new(MaterialIcons.warning, ResourceKeys.DangerBrush);
        public static IconDefinition Error => new(MaterialIcons.error, ResourceKeys.DangerBrush);
        public static IconDefinition Success => new(MaterialIcons.check_circle, ResourceKeys.SuccessBrush);
        public static IconDefinition Info => new(MaterialIcons.info, ResourceKeys.PrimaryBrush);
    }
}
