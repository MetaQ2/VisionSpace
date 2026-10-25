// =============================================================================
// ControlCenterController.cs — Floating control center panel
// =============================================================================
using UnityEngine;

namespace VisionSpace.UI
{
    /// <summary>
    /// Vision Pro-inspired control center with brightness, environment,
    /// volume proxy, recenter, and quality settings.
    /// 
    /// NOTE: Controls that say "App Only" only affect Vision Space, not the
    /// system. System-wide controls (volume, mic) require system APIs that
    /// are restricted on sideloaded Quest apps.
    /// </summary>
    public class ControlCenterController : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private float panelWidth = 0.5f;
        [SerializeField] private float panelHeight = 0.4f;
        [SerializeField] private float distanceFromUser = 1.0f;

        private bool _isVisible;
        private Vector3 _targetScale;

        private void Awake()
        {
            _targetScale = transform.localScale;
            transform.localScale = Vector3.zero;
            _isVisible = false;
        }

        public void Toggle()
        {
            _isVisible = !_isVisible;
            _targetScale = _isVisible ? Vector3.one : Vector3.zero;

            if (_isVisible) PositionInFront();
        }

        public void Show()
        {
            _isVisible = true;
            _targetScale = Vector3.one;
            PositionInFront();
        }

        public void Hide()
        {
            _isVisible = false;
            _targetScale = Vector3.zero;
        }

        private void PositionInFront()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 forward = cam.transform.forward;
            forward.y = 0;
            forward.Normalize();

            transform.position = cam.transform.position +
                                 forward * distanceFromUser +
                                 Vector3.up * 0.1f;

            Vector3 lookDir = cam.transform.position - transform.position;
            lookDir.y = 0;
            if (lookDir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(-lookDir);
        }

        private void Update()
        {
            transform.localScale = Vector3.Lerp(transform.localScale, _targetScale,
                                                 Time.deltaTime * 10f);
        }

        // -- Control Actions --

        /// <summary>Adjust app interface brightness (not system brightness).</summary>
        public void SetInterfaceBrightness(float value)
        {
            if (Core.SettingsManager.Instance == null) return;
            var s = Core.SettingsManager.Instance.CurrentSettings;
            s.interfaceBrightness = Mathf.Clamp01(value);
            Core.SettingsManager.Instance.SaveSettings();
            Debug.Log($"[VisionSpace] Interface brightness: {value:F2} (app only)");
        }

        /// <summary>Cycle environment.</summary>
        public void CycleEnvironment()
        {
            Environment.EnvironmentManager.Instance?.NextEnvironment();
        }

        /// <summary>Switch environment by ID.</summary>
        public void SetEnvironment(string envId)
        {
            Environment.EnvironmentManager.Instance?.SwitchEnvironment(envId);
        }

        /// <summary>Recenter all windows in front of user.</summary>
        public void RecenterInterface()
        {
            WindowManagement.WindowManager.Instance?.RecenterAllWindows();
            Debug.Log("[VisionSpace] Interface recentered.");
        }

        /// <summary>Toggle FPS counter.</summary>
        public void ToggleFPSCounter()
        {
            if (Core.SettingsManager.Instance == null) return;
            var s = Core.SettingsManager.Instance.CurrentSettings;
            s.showFPSCounter = !s.showFPSCounter;
            Core.SettingsManager.Instance.SaveSettings();
        }

        /// <summary>Set quality level.</summary>
        public void SetQuality(Core.QualityLevel level)
        {
            if (Core.SettingsManager.Instance == null) return;
            var s = Core.SettingsManager.Instance.CurrentSettings;
            s.qualityLevel = level;
            Core.SettingsManager.Instance.ApplyAndSave(s);
        }

        /// <summary>Toggle hand tracking preference.</summary>
        public void ToggleHandTrackingPreference()
        {
            if (Core.SettingsManager.Instance == null) return;
            var s = Core.SettingsManager.Instance.CurrentSettings;
            s.preferHandTracking = !s.preferHandTracking;
            Core.SettingsManager.Instance.ApplyAndSave(s);
            Debug.Log($"[VisionSpace] Hand tracking preferred: {s.preferHandTracking}");
        }
    }
}
