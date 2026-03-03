using System.Text.Json.Serialization;

namespace GifWidget
{
    public class GifSettings
    {
        public string GifPath { get; set; } = string.Empty;

        /// <summary>Overall window opacity (0.1 – 1.0).</summary>
        public double Opacity { get; set; } = 1.0;

        /// <summary>Rendered width in pixels; height scales proportionally.</summary>
        public double WidgetWidth { get; set; } = 300;

        // Last position (screen pixels). -1 = first-run default (placed below TimetableWidget).
        public double WindowX { get; set; } = -1;
        public double WindowY { get; set; } = -1;

        /// <summary>Whether to show a thin translucent border around the GIF.</summary>
        public bool ShowBorder { get; set; } = false;

        /// <summary>Corner radius for the border / clip.</summary>
        public double CornerRadius { get; set; } = 0;
    }
}
