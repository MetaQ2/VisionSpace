// =============================================================================
// EnvironmentManager.cs — Spatial environment switching
// =============================================================================
using System.Collections.Generic;
using UnityEngine;

namespace VisionSpace.Environment
{
    /// <summary>
    /// Manages immersive VR environments. Handles skybox, ambient lighting,
    /// environment geometry, and smooth transitions between environments.
    /// </summary>
    public class EnvironmentManager : MonoBehaviour
    {
        public static EnvironmentManager Instance { get; private set; }

        [Header("Available Environments")]
        [SerializeField] private List<EnvironmentConfig> environments = new List<EnvironmentConfig>();

        [Header("Transition")]
        [SerializeField] private float transitionDuration = 1.0f;

        private EnvironmentConfig _activeEnvironment;
        private int _activeIndex;
        private bool _isTransitioning;
        private float _transitionProgress;
        private Color _fromAmbient, _toAmbient;
        private float _fromIntensity, _toIntensity;

        public EnvironmentConfig ActiveEnvironment => _activeEnvironment;
        public IReadOnlyList<EnvironmentConfig> Environments => environments;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            if (environments.Count == 0) CreateDefaultEnvironments();
        }

        private void Start()
        {
            // Load saved environment or default
            string savedId = Core.SettingsManager.Instance?.CurrentSettings?.activeEnvironmentId;
            var target = environments.Find(e => e.id == savedId);
            if (target == null && environments.Count > 0)
                target = environments[0];

            if (target != null)
                ApplyEnvironment(target, instant: true);
        }

        /// <summary>Switch to environment by ID.</summary>
        public void SwitchEnvironment(string environmentId)
        {
            var target = environments.Find(e => e.id == environmentId);
            if (target == null)
            {
                Debug.LogWarning($"[VisionSpace] Environment not found: {environmentId}");
                return;
            }
            ApplyEnvironment(target, instant: false);

            // Save preference
            if (Core.SettingsManager.Instance != null)
            {
                var settings = Core.SettingsManager.Instance.CurrentSettings;
                settings.activeEnvironmentId = environmentId;
                Core.SettingsManager.Instance.SaveSettings();
            }
        }

        /// <summary>Cycle to next environment.</summary>
        public void NextEnvironment()
        {
            if (environments.Count == 0) return;
            _activeIndex = (_activeIndex + 1) % environments.Count;
            ApplyEnvironment(environments[_activeIndex], instant: false);
        }

        private void ApplyEnvironment(EnvironmentConfig config, bool instant)
        {
            if (_isTransitioning) return;

            _fromAmbient = RenderSettings.ambientSkyColor;
            _fromIntensity = RenderSettings.ambientIntensity;
            _toAmbient = config.ambientColor;
            _toIntensity = config.ambientIntensity;

            if (instant)
            {
                SetEnvironmentImmediate(config);
            }
            else
            {
                _isTransitioning = true;
                _transitionProgress = 0;
            }

            _activeEnvironment = config;
            _activeIndex = environments.IndexOf(config);
            Debug.Log($"[VisionSpace] Environment: {config.displayName}");
        }

        private void SetEnvironmentImmediate(EnvironmentConfig config)
        {
            if (config.skyboxMaterial != null)
                RenderSettings.skybox = config.skyboxMaterial;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientSkyColor = config.ambientColor;
            RenderSettings.ambientIntensity = config.ambientIntensity;

            if (config.fogEnabled)
            {
                RenderSettings.fog = true;
                RenderSettings.fogColor = config.fogColor;
                RenderSettings.fogDensity = config.fogDensity;
                RenderSettings.fogMode = FogMode.ExponentialSquared;
            }
            else
            {
                RenderSettings.fog = false;
            }

            // Activate/deactivate environment geometry
            ActivateEnvironmentGeometry(config);
        }

        private void ActivateEnvironmentGeometry(EnvironmentConfig config)
        {
            foreach (var env in environments)
            {
                if (env.environmentRoot != null)
                    env.environmentRoot.SetActive(env == config);
            }
        }

        private void Update()
        {
            if (!_isTransitioning) return;

            _transitionProgress += Time.deltaTime / transitionDuration;
            float t = Mathf.SmoothStep(0, 1, _transitionProgress);

            RenderSettings.ambientSkyColor = Color.Lerp(_fromAmbient, _toAmbient, t);
            RenderSettings.ambientIntensity = Mathf.Lerp(_fromIntensity, _toIntensity, t);

            if (_transitionProgress >= 1f)
            {
                _isTransitioning = false;
                SetEnvironmentImmediate(_activeEnvironment);
            }
        }

        private void CreateDefaultEnvironments()
        {
            environments.Add(new EnvironmentConfig
            {
                id = "dark_studio",
                displayName = "Dark Studio",
                ambientColor = new Color(0.05f, 0.05f, 0.08f),
                ambientIntensity = 0.3f,
                fogEnabled = true,
                fogColor = new Color(0.02f, 0.02f, 0.04f),
                fogDensity = 0.05f
            });

            environments.Add(new EnvironmentConfig
            {
                id = "bright_room",
                displayName = "Bright Room",
                ambientColor = new Color(0.6f, 0.6f, 0.65f),
                ambientIntensity = 0.8f,
                fogEnabled = false
            });

            environments.Add(new EnvironmentConfig
            {
                id = "futuristic",
                displayName = "Futuristic",
                ambientColor = new Color(0.1f, 0.08f, 0.15f),
                ambientIntensity = 0.4f,
                fogEnabled = true,
                fogColor = new Color(0.05f, 0.03f, 0.08f),
                fogDensity = 0.03f
            });
        }
    }

    /// <summary>
    /// Configuration for a single environment.
    /// </summary>
    [System.Serializable]
    public class EnvironmentConfig
    {
        public string id;
        public string displayName;
        public Material skyboxMaterial;
        public GameObject environmentRoot;

        [Header("Lighting")]
        public Color ambientColor = new Color(0.1f, 0.1f, 0.15f);
        public float ambientIntensity = 0.5f;

        [Header("Fog")]
        public bool fogEnabled = true;
        public Color fogColor = new Color(0.02f, 0.02f, 0.04f);
        public float fogDensity = 0.05f;
    }
}
