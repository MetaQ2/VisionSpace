// =============================================================================
// VisionSpaceBootstrap.cs — Application entry point and system initialization
// Part of VISION SPACE — Spatial Computing Interface for Meta Quest 2
// =============================================================================
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VisionSpace.Core
{
    /// <summary>
    /// Entry point for Vision Space. Runs in the Boot scene, initializes all
    /// subsystems, then transitions to the Home scene.
    /// </summary>
    public class VisionSpaceBootstrap : MonoBehaviour
    {
        [Header("Scene Configuration")]
        [SerializeField] private string homeSceneName = "Home";
        [SerializeField] private float minSplashDuration = 2.0f;

        [Header("System References")]
        [SerializeField] private SettingsManager settingsManager;

        private float _bootStartTime;
        private bool _systemsReady;

        private void Awake()
        {
            // Ensure only one instance persists
            DontDestroyOnLoad(gameObject);
            _bootStartTime = Time.realtimeSinceStartup;

            Debug.Log("[VisionSpace] Bootstrap: Initializing...");
            InitializeSystems();
        }

        private void InitializeSystems()
        {
            // Initialize settings first (other systems depend on it)
            if (settingsManager == null)
            {
                settingsManager = gameObject.AddComponent<SettingsManager>();
            }
            settingsManager.Initialize();

            // Apply saved quality settings
            ApplyQualitySettings();

            // Set target frame rate for Quest 2
            Application.targetFrameRate = 90;
            QualitySettings.vSyncCount = 0;

            Debug.Log("[VisionSpace] Bootstrap: Systems initialized.");
            _systemsReady = true;
        }

        private void ApplyQualitySettings()
        {
            var settings = settingsManager.CurrentSettings;

            // Configure rendering for Quest 2
            QualitySettings.antiAliasing = settings.antiAliasing;
            QualitySettings.shadows = settings.enableShadows
                ? ShadowQuality.HardOnly
                : ShadowQuality.Disable;

            // Fixed foveated rendering would be configured via Meta XR SDK at runtime
            Debug.Log($"[VisionSpace] Quality: AA={settings.antiAliasing}, " +
                      $"Shadows={settings.enableShadows}, " +
                      $"RefreshRate={settings.targetRefreshRate}Hz");
        }

        private void Update()
        {
            if (!_systemsReady) return;

            float elapsed = Time.realtimeSinceStartup - _bootStartTime;
            if (elapsed >= minSplashDuration)
            {
                TransitionToHome();
                enabled = false; // Stop updating
            }
        }

        private void TransitionToHome()
        {
            Debug.Log("[VisionSpace] Bootstrap: Transitioning to Home...");
            SceneManager.LoadSceneAsync(homeSceneName, LoadSceneMode.Single);
        }
    }
}
