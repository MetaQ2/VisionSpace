// =============================================================================
// PerformanceMonitor.cs — FPS counter and diagnostics
// =============================================================================
using UnityEngine;
using TMPro;

namespace VisionSpace.Performance
{
    /// <summary>
    /// Monitors and displays framerate. Essential for Quest 2 optimization.
    /// Toggle via Settings or Control Center.
    /// </summary>
    public class PerformanceMonitor : MonoBehaviour
    {
        public static PerformanceMonitor Instance { get; private set; }

        [SerializeField] private TMP_Text fpsText;
        [SerializeField] private float updateInterval = 0.5f;

        private float _accum;
        private int _frames;
        private float _timeLeft;
        private float _currentFps;
        private bool _isVisible;

        public float CurrentFPS => _currentFps;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            _timeLeft = updateInterval;

            if (fpsText == null)
            {
                var obj = new GameObject("FPSCounter");
                obj.transform.SetParent(transform);
                fpsText = obj.AddComponent<TextMeshPro>();
                fpsText.fontSize = 1.5f;
                fpsText.alignment = TextAlignmentOptions.TopRight;
                fpsText.color = new Color(0.5f, 0.8f, 0.5f, 0.8f);
                fpsText.rectTransform.sizeDelta = new Vector2(3f, 1f);
            }

            // Position in corner of view
            UpdatePosition();
            SetVisible(Core.SettingsManager.Instance?.CurrentSettings?.showFPSCounter ?? false);
        }

        private void Update()
        {
            if (!_isVisible) return;

            _timeLeft -= Time.deltaTime;
            _accum += Time.timeScale / Time.deltaTime;
            _frames++;

            if (_timeLeft <= 0f)
            {
                _currentFps = _accum / _frames;
                _timeLeft = updateInterval;
                _accum = 0;
                _frames = 0;

                if (fpsText != null)
                {
                    Color color;
                    if (_currentFps >= 72f) color = new Color(0.4f, 0.9f, 0.4f, 0.8f);
                    else if (_currentFps >= 60f) color = new Color(0.9f, 0.9f, 0.3f, 0.8f);
                    else color = new Color(0.9f, 0.3f, 0.3f, 0.8f);

                    fpsText.text = $"{_currentFps:F0} FPS";
                    fpsText.color = color;
                }
            }

            UpdatePosition();
        }

        private void UpdatePosition()
        {
            Camera cam = Camera.main;
            if (cam == null || fpsText == null) return;

            // Fixed position in upper right of view
            transform.position = cam.transform.position +
                                 cam.transform.forward * 2f +
                                 cam.transform.right * 0.8f +
                                 cam.transform.up * 0.6f;
            transform.rotation = cam.transform.rotation;
        }

        public void SetVisible(bool visible)
        {
            _isVisible = visible;
            if (fpsText != null) fpsText.gameObject.SetActive(visible);
        }

        public void Toggle() => SetVisible(!_isVisible);
    }
}
