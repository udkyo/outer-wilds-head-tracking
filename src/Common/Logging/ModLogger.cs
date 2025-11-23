extern alias OWMLCommon;
using System;
using MessageType = OWMLCommon::OWML.Common.MessageType;

namespace HeadTracking.Common.Logging
{
    /// <summary>
    /// Centralized logging utility for the mod with lazy evaluation
    /// </summary>
    public static class ModLogger
    {
        public static bool EnableDebugLogging = false;

        public static void LogError(string message)
        {
            var mod = HeadTrackingMod.Instance;
            mod?.ModHelper?.Console.WriteLine($"[HeadTracking] ERROR: {message}", MessageType.Error);
        }

        public static void LogWarning(string message)
        {
            var mod = HeadTrackingMod.Instance;
            mod?.ModHelper?.Console.WriteLine($"[HeadTracking] WARNING: {message}", MessageType.Warning);
        }

        public static void LogSuccess(string message)
        {
            var mod = HeadTrackingMod.Instance;
            mod?.ModHelper?.Console.WriteLine($"[HeadTracking] {message}", MessageType.Success);
        }

        public static void LogInfo(string message)
        {
            var mod = HeadTrackingMod.Instance;
            mod?.ModHelper?.Console.WriteLine($"[HeadTracking] {message}", MessageType.Info);
        }

        public static void LogInfo(Func<string> messageFactory)
        {
            var mod = HeadTrackingMod.Instance;
            if (mod?.ModHelper?.Console != null)
            {
                mod.ModHelper.Console.WriteLine($"[HeadTracking] {messageFactory()}", MessageType.Info);
            }
        }

        public static void LogDebug(string message)
        {
            if (!EnableDebugLogging) return;
            var mod = HeadTrackingMod.Instance;
            mod?.ModHelper?.Console.WriteLine($"[HeadTracking] {message}", MessageType.Debug);
        }

        public static void LogDebug(Func<string> messageFactory)
        {
            if (!EnableDebugLogging) return;
            var mod = HeadTrackingMod.Instance;
            if (mod?.ModHelper?.Console != null)
            {
                mod.ModHelper.Console.WriteLine($"[HeadTracking] {messageFactory()}", MessageType.Debug);
            }
        }
    }
}
