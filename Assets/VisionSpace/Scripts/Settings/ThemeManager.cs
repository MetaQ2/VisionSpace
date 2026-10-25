// =============================================================================
// ThemeManager.cs — Dark/Light theme colors
// =============================================================================
using UnityEngine;
using VisionSpace.Core;

namespace VisionSpace.Settings
{
    /// <summary>
    /// Manages color themes. Provides color palettes for glass panels,
    /// text, icons, and backgrounds based on active theme.
    /// </summary>
    public class ThemeManager : MonoBehaviour
    {
        public static ThemeManager Instance { get; private set; }

        public ThemeColors CurrentTheme { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            ApplyTheme(SettingsManager.Instance?.CurrentSettings?.themeMode ?? ThemeMode.Dark);
        }

        public void ApplyTheme(ThemeMode mode)
        {
            CurrentTheme = mode == ThemeMode.Dark ? ThemeColors.Dark() : ThemeColors.Light();
            Debug.Log($"[VisionSpace] Theme: {mode}");
        }
    }

    /// <summary>Color palette for a theme.</summary>
    public class ThemeColors
    {
        public Color glassPrimary;
        public Color glassSecondary;
        public Color glassEdge;
        public Color textPrimary;
        public Color textSecondary;
        public Color accentPrimary;
        public Color accentSecondary;
        public Color dockBackground;
        public Color iconBackground;
        public Color windowFocused;
        public Color windowUnfocused;

        public static ThemeColors Dark() => new ThemeColors
        {
            glassPrimary = new Color(0.08f, 0.08f, 0.12f, 0.85f),
            glassSecondary = new Color(0.12f, 0.12f, 0.18f, 0.75f),
            glassEdge = new Color(0.3f, 0.4f, 0.6f, 0.2f),
            textPrimary = new Color(0.95f, 0.95f, 0.97f),
            textSecondary = new Color(0.6f, 0.6f, 0.65f),
            accentPrimary = new Color(0.3f, 0.55f, 0.95f),
            accentSecondary = new Color(0.5f, 0.3f, 0.8f),
            dockBackground = new Color(0.06f, 0.06f, 0.1f, 0.8f),
            iconBackground = new Color(0.15f, 0.15f, 0.22f, 0.85f),
            windowFocused = new Color(0.12f, 0.12f, 0.18f, 0.9f),
            windowUnfocused = new Color(0.06f, 0.06f, 0.1f, 0.75f),
        };

        public static ThemeColors Light() => new ThemeColors
        {
            glassPrimary = new Color(0.92f, 0.92f, 0.95f, 0.85f),
            glassSecondary = new Color(0.88f, 0.88f, 0.92f, 0.75f),
            glassEdge = new Color(0.7f, 0.7f, 0.8f, 0.3f),
            textPrimary = new Color(0.1f, 0.1f, 0.15f),
            textSecondary = new Color(0.4f, 0.4f, 0.45f),
            accentPrimary = new Color(0.2f, 0.45f, 0.85f),
            accentSecondary = new Color(0.4f, 0.25f, 0.7f),
            dockBackground = new Color(0.85f, 0.85f, 0.9f, 0.8f),
            iconBackground = new Color(0.9f, 0.9f, 0.93f, 0.85f),
            windowFocused = new Color(0.93f, 0.93f, 0.96f, 0.9f),
            windowUnfocused = new Color(0.88f, 0.88f, 0.92f, 0.75f),
        };
    }
}
