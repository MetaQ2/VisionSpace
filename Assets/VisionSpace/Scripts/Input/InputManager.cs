// =============================================================================
// InputManager.cs — Central input management and provider switching
// =============================================================================
using System.Collections.Generic;
using UnityEngine;

namespace VisionSpace.Input
{
    /// <summary>
    /// Manages multiple input providers and exposes the currently active one.
    /// Automatically switches between controller and hand tracking based on availability.
    /// </summary>
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private bool preferHandTracking = false;

        [Header("Ray Visualization")]
        [SerializeField] private LineRenderer rayLineRenderer;
        [SerializeField] private Transform rayHitIndicator;
        [SerializeField] private float maxRayDistance = 10f;
        [SerializeField] private float rayWidth = 0.003f;
        [SerializeField] private Color rayColor = new Color(1f, 1f, 1f, 0.6f);
        [SerializeField] private Color rayHitColor = new Color(0.6f, 0.8f, 1f, 0.8f);

        private List<IInputProvider> _providers = new List<IInputProvider>();
        private IInputProvider _activeProvider;
        private bool _isHovering;

        /// <summary>The currently active input provider.</summary>
        public IInputProvider ActiveProvider => _activeProvider;

        /// <summary>Convenience: ray origin from active provider.</summary>
        public Vector3 RayOrigin => _activeProvider?.RayOrigin ?? Vector3.zero;

        /// <summary>Convenience: ray direction from active provider.</summary>
        public Vector3 RayDirection => _activeProvider?.RayDirection ?? Vector3.forward;

        public bool SelectPressed => _activeProvider?.SelectPressed ?? false;
        public bool SelectHeld => _activeProvider?.SelectHeld ?? false;
        public bool SelectReleased => _activeProvider?.SelectReleased ?? false;
        public bool GrabPressed => _activeProvider?.GrabPressed ?? false;
        public bool GrabHeld => _activeProvider?.GrabHeld ?? false;
        public bool GrabReleased => _activeProvider?.GrabReleased ?? false;
        public Vector2 ScrollInput => _activeProvider?.ScrollInput ?? Vector2.zero;
        public bool MenuPressed => _activeProvider?.MenuPressed ?? false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializeProviders();
            SetupRayVisualization();
        }

        private void InitializeProviders()
        {
            // Add controller provider (always available on Quest 2)
            var controllerProvider = gameObject.AddComponent<ControllerInputProvider>();
            _providers.Add(controllerProvider);

            // Add hand tracking provider
            var handProvider = gameObject.AddComponent<HandTrackingInputProvider>();
            _providers.Add(handProvider);

            // Add head gaze as fallback
            var gazeProvider = gameObject.AddComponent<HeadGazeInputProvider>();
            _providers.Add(gazeProvider);

            // Default to controller
            _activeProvider = controllerProvider;
            Debug.Log("[VisionSpace] InputManager: Providers initialized.");
        }

        private void SetupRayVisualization()
        {
            if (rayLineRenderer == null)
            {
                var rayObj = new GameObject("InputRay");
                rayObj.transform.SetParent(transform);
                rayLineRenderer = rayObj.AddComponent<LineRenderer>();
                rayLineRenderer.positionCount = 2;
                rayLineRenderer.startWidth = rayWidth;
                rayLineRenderer.endWidth = rayWidth * 0.5f;
                rayLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
                rayLineRenderer.startColor = rayColor;
                rayLineRenderer.endColor = new Color(rayColor.r, rayColor.g, rayColor.b, 0.1f);
                rayLineRenderer.useWorldSpace = true;
            }

            if (rayHitIndicator == null)
            {
                var hitObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                hitObj.name = "RayHitIndicator";
                hitObj.transform.SetParent(transform);
                hitObj.transform.localScale = Vector3.one * 0.01f;
                Destroy(hitObj.GetComponent<Collider>());
                var mat = new Material(Shader.Find("Sprites/Default"));
                mat.color = rayHitColor;
                hitObj.GetComponent<Renderer>().material = mat;
                rayHitIndicator = hitObj.transform;
                rayHitIndicator.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            // Update all providers
            foreach (var provider in _providers)
            {
                provider.UpdateInput();
            }

            // Auto-switch: prefer hand tracking if configured and active
            AutoSwitchProvider();

            // Update ray visualization
            UpdateRayVisualization();
        }

        private void AutoSwitchProvider()
        {
            if (preferHandTracking)
            {
                var handProvider = _providers.Find(p => p is HandTrackingInputProvider);
                if (handProvider != null && handProvider.IsActive)
                {
                    _activeProvider = handProvider;
                    return;
                }
            }

            // Default to controller if active
            var controllerProvider = _providers.Find(p => p is ControllerInputProvider);
            if (controllerProvider != null && controllerProvider.IsActive)
            {
                _activeProvider = controllerProvider;
                return;
            }

            // Fallback to head gaze
            var gazeProvider = _providers.Find(p => p is HeadGazeInputProvider);
            if (gazeProvider != null)
            {
                _activeProvider = gazeProvider;
            }
        }

        private void UpdateRayVisualization()
        {
            if (_activeProvider == null || !_activeProvider.IsActive)
            {
                rayLineRenderer.enabled = false;
                rayHitIndicator.gameObject.SetActive(false);
                return;
            }

            rayLineRenderer.enabled = true;
            Vector3 origin = _activeProvider.RayOrigin;
            Vector3 direction = _activeProvider.RayDirection;

            Vector3 endPoint = origin + direction * maxRayDistance;
            _isHovering = false;

            // Raycast for UI interaction
            if (Physics.Raycast(origin, direction, out RaycastHit hit, maxRayDistance))
            {
                endPoint = hit.point;
                _isHovering = true;
                rayHitIndicator.gameObject.SetActive(true);
                rayHitIndicator.position = hit.point;
                rayHitIndicator.rotation = Quaternion.LookRotation(hit.normal);

                // Scale indicator based on distance for consistent visual size
                float scale = Mathf.Max(0.005f, hit.distance * 0.008f);
                rayHitIndicator.localScale = Vector3.one * scale;
            }
            else
            {
                rayHitIndicator.gameObject.SetActive(false);
            }

            rayLineRenderer.SetPosition(0, origin);
            rayLineRenderer.SetPosition(1, endPoint);

            // Adjust ray color when hovering
            rayLineRenderer.startColor = _isHovering ? rayHitColor : rayColor;
            rayLineRenderer.endColor = new Color(
                rayLineRenderer.startColor.r,
                rayLineRenderer.startColor.g,
                rayLineRenderer.startColor.b, 0.1f);
        }

        /// <summary>
        /// Perform a raycast from the active input provider.
        /// </summary>
        public bool Raycast(out RaycastHit hit, float distance = -1)
        {
            if (_activeProvider == null || !_activeProvider.IsActive)
            {
                hit = default;
                return false;
            }

            float dist = distance > 0 ? distance : maxRayDistance;
            return Physics.Raycast(_activeProvider.RayOrigin, _activeProvider.RayDirection, out hit, dist);
        }
    }
}
