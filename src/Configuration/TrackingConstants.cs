namespace HeadTracking.Configuration
{
    /// <summary>
    /// Constants for head tracking configuration
    /// </summary>
    public static class TrackingConstants
    {
        // Thresholds
        public const int RECENTER_THRESHOLD_FRAMES = 60;
        public const double CONNECTION_TIMEOUT_SECONDS = 2.0;

        // UDP configuration
        public const int DEFAULT_OPENTRACK_PORT = 5252;
        public const int UDP_RECEIVE_TIMEOUT_MS = 100;
        public const int OPENTRACK_PACKET_SIZE = 48;

        // Dialogue mode detection
        public const float DIALOGUE_CAMERA_SPEED_THRESHOLD = 2.0f;
        public const float DIALOGUE_CAMERA_SPEED_RANGE = 8.0f;
        public const float DIALOGUE_MIN_HEAD_TRACKING = 0.15f;
    }
}
