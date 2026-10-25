// =============================================================================
// AppEntry.cs — Data model for a discovered or registered application
// =============================================================================
using UnityEngine;
using System;

namespace VisionSpace.Launcher
{
    /// <summary>
    /// Represents a single application entry in the launcher.
    /// Can be populated from Android PackageManager discovery or manually registered.
    /// </summary>
    [Serializable]
    public class AppEntry
    {
        [Header("Identity")]
        public string packageName;
        public string displayName;
        public string activityName; // Fully qualified activity class name

        [Header("Display")]
        public Texture2D icon;
        public Color accentColor = new Color(0.3f, 0.5f, 0.8f);
        public string category = "All";

        [Header("Metadata")]
        public bool isFavorite;
        public bool isVRApp;
        public bool isSideloaded;
        public long lastUsedTimestamp;
        public int launchCount;

        [Header("Source")]
        public AppSource source = AppSource.Discovered;

        /// <summary>
        /// Whether this app can be launched via standard intent.
        /// </summary>
        public bool CanLaunch => !string.IsNullOrEmpty(packageName);

        /// <summary>
        /// Create a manually registered app entry.
        /// </summary>
        public static AppEntry CreateManual(string packageName, string displayName,
                                             Color? accent = null)
        {
            return new AppEntry
            {
                packageName = packageName,
                displayName = displayName,
                accentColor = accent ?? new Color(0.3f, 0.5f, 0.8f),
                source = AppSource.Manual
            };
        }

        /// <summary>
        /// Create an internal Vision Space module entry (not a real Android app).
        /// </summary>
        public static AppEntry CreateInternal(string moduleId, string displayName,
                                               Color accent)
        {
            return new AppEntry
            {
                packageName = $"visionspace.internal.{moduleId}",
                displayName = displayName,
                accentColor = accent,
                source = AppSource.Internal
            };
        }
    }

    public enum AppSource
    {
        Discovered, // Found via Android PackageManager
        Manual,     // Manually registered by user
        Internal    // Built-in Vision Space module
    }
}
