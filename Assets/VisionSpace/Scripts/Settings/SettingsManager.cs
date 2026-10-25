// =============================================================================
// SettingsManager.cs — Persistent settings system
// =============================================================================
using UnityEngine;
using System;
using System.IO;

namespace VisionSpace.Core
{
    /// <summary>
    /// Manages application settings with JSON persistence.
    /// </summary>
    public class SettingsManager : MonoBehaviour
    {
        public static SettingsManager Instance { get; private set; }

        public SettingsData CurrentSettings { get; private set; }
        public event Action<SettingsData> OnSettingsChanged;

        private string _settingsPath;

        public void Initialize()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _settingsPath = Path.Combine(Application.persistentDataPath, "settings.json");
            LoadSettings();
        }

        public void LoadSettings()
        {
            if (File.Exists(_settingsPath))
            {
                try
                {
                    string json = File.ReadAllText(_settingsPath);
                    CurrentSettings = JsonUtility.FromJson<SettingsData>(json);
                    Debug.Log("[VisionSpace] Settings loaded from disk.");
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[VisionSpace] Failed to load settings: {e.Message}. Using defaults.");
                    CurrentSettings = SettingsData.CreateDefault();
                }
            }
            else
            {
                CurrentSettings = SettingsData.CreateDefault();
                Debug.Log("[VisionSpace] Using default settings.");
            }
        }

        public void SaveSettings()
        {
            try
            {
                string json = JsonUtility.ToJson(CurrentSettings, true);
                File.WriteAllText(_settingsPath, json);
                Debug.Log("[VisionSpace] Settings saved.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[VisionSpace] Failed to save settings: {e.Message}");
            }
        }

        public void ApplyAndSave(SettingsData newSettings)
        {
            CurrentSettings = newSettings;
            SaveSettings();
            OnSettingsChanged?.Invoke(CurrentSettings);
        }

        public void ResetToDefaults()
        {
            ApplyAndSave(SettingsData.CreateDefault());
        }
    }

    /// <summary>
    /// Serializable settings data structure.
    /// </summary>
    [Serializable]
    public class SettingsData
    {
        [Header("Display")]
        public int targetRefreshRate = 90;
        public int antiAliasing = 4;
        public bool enableShadows = true;
        public float interfaceScale = 1.0f;
        public float interfaceBrightness = 1.0f;

        [Header("Theme")]
        public ThemeMode themeMode = ThemeMode.Dark;

        [Header("Environment")]
        public string activeEnvironmentId = "dark_studio";

        [Header("Interaction")]
        public bool enableHandTracking = true;
        public bool preferHandTracking = false;
        public float hoverDelaySeconds = 0.3f;
        public float windowMinDistance = 0.5f;
        public float windowMaxDistance = 5.0f;
        public float windowDefaultDistance = 1.5f;

        [Header("Dock")]
        public bool dockAutoHide = false;
        public DockPosition dockPosition = DockPosition.Bottom;

        [Header("Performance")]
        public QualityLevel qualityLevel = QualityLevel.Medium;
        public bool showFPSCounter = false;

        public static SettingsData CreateDefault()
        {
            return new SettingsData();
        }
    }

    public enum ThemeMode
    {
        Dark,
        Light
    }

    public enum DockPosition
    {
        Bottom,
        Left,
        Right
    }

    public enum QualityLevel
    {
        Low,
        Medium,
        High
    }
}
