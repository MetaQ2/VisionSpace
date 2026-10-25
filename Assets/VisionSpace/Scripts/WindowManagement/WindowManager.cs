// =============================================================================
// WindowManager.cs — Spatial window management system
// =============================================================================
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VisionSpace.WindowManagement
{
    /// <summary>
    /// Manages all floating windows in the spatial environment.
    /// Handles creation, destruction, focus, layering, layout saving/restoring,
    /// and ensures windows stay within comfortable viewing bounds.
    /// </summary>
    public class WindowManager : MonoBehaviour
    {
        public static WindowManager Instance { get; private set; }

        [Header("Window Constraints")]
        [SerializeField] private float minDistance = 0.5f;
        [SerializeField] private float maxDistance = 5.0f;
        [SerializeField] private float defaultDistance = 1.5f;
        [SerializeField] private float maxVerticalAngle = 60f;
        [SerializeField] private float maxHorizontalAngle = 120f;

        [Header("Window Prefabs")]
        [SerializeField] private GameObject standardWindowPrefab;
        [SerializeField] private GameObject compactPanelPrefab;
        [SerializeField] private GameObject largeWindowPrefab;
        [SerializeField] private GameObject modalWindowPrefab;

        private List<WindowController> _windows = new List<WindowController>();
        private WindowController _focusedWindow;
        private int _nextWindowId;

        public IReadOnlyList<WindowController> Windows => _windows;
        public WindowController FocusedWindow => _focusedWindow;

        public event Action<WindowController> OnWindowCreated;
        public event Action<WindowController> OnWindowDestroyed;
        public event Action<WindowController> OnWindowFocused;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Create a new window of the specified type at a comfortable position.
        /// </summary>
        public WindowController CreateWindow(
            WindowType type,
            string title,
            Vector3? position = null,
            Vector2? size = null)
        {
            GameObject prefab = GetPrefab(type);
            if (prefab == null)
            {
                Debug.LogError($"[VisionSpace] WindowManager: No prefab for {type}. " +
                               "Creating default window.");
                prefab = CreateDefaultWindowPrefab();
            }

            Vector3 spawnPos = position ?? CalculateSpawnPosition();
            spawnPos = ClampToViewBounds(spawnPos);

            GameObject windowObj = Instantiate(prefab, spawnPos, Quaternion.identity, transform);
            windowObj.name = $"Window_{_nextWindowId}_{title.Replace(" ", "_")}";

            WindowController controller = windowObj.GetComponent<WindowController>();
            if (controller == null)
            {
                controller = windowObj.AddComponent<WindowController>();
            }

            controller.Initialize(_nextWindowId++, title, type, size ?? GetDefaultSize(type));
            FaceUser(controller);

            _windows.Add(controller);
            controller.OnCloseRequested += HandleWindowClose;
            controller.OnFocusRequested += HandleWindowFocus;

            OnWindowCreated?.Invoke(controller);
            SetFocus(controller);

            Debug.Log($"[VisionSpace] Window created: {title} ({type})");
            return controller;
        }

        /// <summary>
        /// Close and destroy a window.
        /// </summary>
        public void CloseWindow(WindowController window)
        {
            if (window == null) return;

            window.OnCloseRequested -= HandleWindowClose;
            window.OnFocusRequested -= HandleWindowFocus;
            _windows.Remove(window);

            if (_focusedWindow == window)
            {
                _focusedWindow = _windows.LastOrDefault();
            }

            OnWindowDestroyed?.Invoke(window);
            window.AnimateClose(() => Destroy(window.gameObject));

            Debug.Log($"[VisionSpace] Window closed: {window.Title}");
        }

        /// <summary>
        /// Set focus to a specific window, bringing it forward.
        /// </summary>
        public void SetFocus(WindowController window)
        {
            if (window == null || window == _focusedWindow) return;

            if (_focusedWindow != null)
            {
                _focusedWindow.SetFocused(false);
            }

            _focusedWindow = window;
            _focusedWindow.SetFocused(true);

            // Adjust depth ordering — focused window slightly closer
            ReorderWindows();
            OnWindowFocused?.Invoke(window);
        }

        /// <summary>
        /// Close all windows.
        /// </summary>
        public void CloseAllWindows()
        {
            var windowsCopy = new List<WindowController>(_windows);
            foreach (var w in windowsCopy)
            {
                CloseWindow(w);
            }
        }

        /// <summary>
        /// Apply a layout preset to arrange windows.
        /// </summary>
        public void ApplyLayout(WindowLayoutPreset preset)
        {
            if (preset == null) return;

            for (int i = 0; i < _windows.Count && i < preset.positions.Length; i++)
            {
                _windows[i].AnimateToPosition(
                    CalculateWorldPosition(preset.positions[i]),
                    preset.scales.Length > i ? preset.scales[i] : Vector3.one,
                    0.5f);
            }

            Debug.Log($"[VisionSpace] Layout applied: {preset.layoutName}");
        }

        /// <summary>
        /// Recenter all windows in front of the user.
        /// </summary>
        public void RecenterAllWindows()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            float angleStep = 30f;
            float startAngle = -((float)(_windows.Count - 1) / 2f) * angleStep;

            for (int i = 0; i < _windows.Count; i++)
            {
                float angle = startAngle + i * angleStep;
                Vector3 dir = Quaternion.Euler(0, angle, 0) * cam.transform.forward;
                Vector3 pos = cam.transform.position + dir * defaultDistance;
                pos.y = cam.transform.position.y; // Eye height

                _windows[i].AnimateToPosition(pos, _windows[i].transform.localScale, 0.4f);
                _windows[i].FacePoint(cam.transform.position);
            }
        }

        // -- Private Helpers --

        private Vector3 CalculateSpawnPosition()
        {
            Camera cam = Camera.main;
            if (cam == null) return Vector3.forward * defaultDistance;

            // Spawn in front of user, offset slightly based on existing window count
            float horizontalOffset = (_windows.Count % 3 - 1) * 0.4f;
            float verticalOffset = (_windows.Count / 3) * -0.3f;

            Vector3 forward = cam.transform.forward;
            forward.y = 0;
            forward.Normalize();

            Vector3 right = cam.transform.right;
            right.y = 0;
            right.Normalize();

            return cam.transform.position +
                   forward * defaultDistance +
                   right * horizontalOffset +
                   Vector3.up * verticalOffset;
        }

        private Vector3 ClampToViewBounds(Vector3 position)
        {
            Camera cam = Camera.main;
            if (cam == null) return position;

            Vector3 toWindow = position - cam.transform.position;
            float distance = toWindow.magnitude;

            // Clamp distance
            distance = Mathf.Clamp(distance, minDistance, maxDistance);

            // Clamp angles
            Vector3 flatForward = cam.transform.forward;
            flatForward.y = 0;
            flatForward.Normalize();

            float horizontalAngle = Vector3.SignedAngle(flatForward,
                new Vector3(toWindow.x, 0, toWindow.z).normalized, Vector3.up);
            horizontalAngle = Mathf.Clamp(horizontalAngle, -maxHorizontalAngle / 2, maxHorizontalAngle / 2);

            float verticalAngle = Vector3.Angle(flatForward, toWindow.normalized) *
                                  Mathf.Sign(toWindow.y);
            verticalAngle = Mathf.Clamp(verticalAngle, -maxVerticalAngle / 2, maxVerticalAngle / 2);

            Vector3 dir = Quaternion.Euler(verticalAngle, horizontalAngle, 0) * flatForward;
            return cam.transform.position + dir * distance;
        }

        private void FaceUser(WindowController window)
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            window.FacePoint(cam.transform.position);
        }

        private void ReorderWindows()
        {
            // No z-fighting management needed — windows are physically separated in 3D space
            // Focus is indicated visually via brightness/outline changes
        }

        private Vector3 CalculateWorldPosition(Vector3 normalizedPos)
        {
            Camera cam = Camera.main;
            if (cam == null) return normalizedPos;

            Vector3 forward = cam.transform.forward;
            forward.y = 0;
            forward.Normalize();

            return cam.transform.position +
                   Quaternion.Euler(normalizedPos.y * 30f, normalizedPos.x * 60f, 0) *
                   forward * (defaultDistance + normalizedPos.z);
        }

        private GameObject GetPrefab(WindowType type)
        {
            return type switch
            {
                WindowType.Standard => standardWindowPrefab,
                WindowType.Compact => compactPanelPrefab,
                WindowType.Large => largeWindowPrefab,
                WindowType.Modal => modalWindowPrefab,
                _ => standardWindowPrefab
            };
        }

        private Vector2 GetDefaultSize(WindowType type)
        {
            return type switch
            {
                WindowType.Standard => new Vector2(0.6f, 0.4f),
                WindowType.Compact => new Vector2(0.3f, 0.2f),
                WindowType.Large => new Vector2(0.9f, 0.6f),
                WindowType.Modal => new Vector2(0.4f, 0.3f),
                _ => new Vector2(0.6f, 0.4f)
            };
        }

        private void HandleWindowClose(WindowController window) => CloseWindow(window);
        private void HandleWindowFocus(WindowController window) => SetFocus(window);

        /// <summary>
        /// Creates a default runtime window prefab when no serialized prefab is assigned.
        /// </summary>
        private GameObject CreateDefaultWindowPrefab()
        {
            var obj = new GameObject("DefaultWindow");

            // Glass panel background
            var panelObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            panelObj.transform.SetParent(obj.transform);
            panelObj.transform.localPosition = Vector3.zero;
            panelObj.transform.localScale = new Vector3(0.6f, 0.4f, 1f);
            panelObj.name = "GlassPanel";

            // Apply translucent material
            var renderer = panelObj.GetComponent<Renderer>();
            var mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = new Color(0.1f, 0.1f, 0.15f, 0.85f);
            renderer.material = mat;

            // Add WindowController
            obj.AddComponent<WindowController>();

            // Add collider for interaction
            var col = obj.AddComponent<BoxCollider>();
            col.size = new Vector3(0.6f, 0.4f, 0.02f);

            return obj;
        }
    }

    public enum WindowType
    {
        Standard,
        Compact,
        Large,
        Modal
    }
}
