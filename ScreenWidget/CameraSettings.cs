namespace ScreenWidget
{
    public class CameraSettings
    {
        /// <summary>DirectShow camera index (0 = first/default camera).</summary>
        public int CameraIndex { get; set; } = 0;

        /// <summary>Auto-capture interval in minutes.</summary>
        public int CaptureIntervalMinutes { get; set; } = 30;

        /// <summary>Overall window opacity (0.1 – 1.0).</summary>
        public double Opacity { get; set; } = 1.0;

        /// <summary>Widget width in pixels; height scales with aspect ratio.</summary>
        public double WidgetWidth { get; set; } = 320;

        // Last saved screen position.  -1 = first-run default.
        public double WindowX { get; set; } = -1;
        public double WindowY { get; set; } = -1;

        /// <summary>Animated border thickness in pixels.</summary>
        public double BorderThickness { get; set; } = 4;

        /// <summary>Corner radius of the widget.</summary>
        public double CornerRadius { get; set; } = 12;

        /// <summary>Speed of the spinning border in seconds per full rotation.</summary>
        public double BorderSpeed { get; set; } = 3.0;

        /// <summary>Path where the last captured image was saved.</summary>
        public string LastImagePath { get; set; } = string.Empty;
    }
}
