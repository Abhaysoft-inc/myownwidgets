namespace PostureGuardian
{
    public class PostureSettings
    {
        public double Opacity { get; set; } = 0.95;
        public double WidgetWidth { get; set; } = 220;
        public double WindowX { get; set; } = -1;
        public double WindowY { get; set; } = -1;
        public int CameraIndex { get; set; } = 0;
        /// <summary>How often (seconds) to check posture.</summary>
        public int CheckIntervalSeconds { get; set; } = 10;
        /// <summary>Minutes of coding before a break reminder fires.</summary>
        public int BreakReminderMinutes { get; set; } = 45;
        /// <summary>Deviation threshold (0-1) for yellow warning.</summary>
        public double WarnThreshold { get; set; } = 0.15;
        /// <summary>Deviation threshold (0-1) for red bad posture.</summary>
        public double BadThreshold { get; set; } = 0.30;
        /// <summary>Whether a calibration reference frame has been saved.</summary>
        public bool IsCalibrated { get; set; } = false;
    }
}
