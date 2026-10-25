// =============================================================================
// HomeSceneController.cs — Main Home scene orchestrator
// =============================================================================
using UnityEngine;
using VisionSpace.Launcher;
using VisionSpace.WindowManagement;
using VisionSpace.UI;
using VisionSpace.Environment;
using VisionSpace.Input;

namespace VisionSpace.Core
{
    /// <summary>
    /// Orchestrates the Home scene: initializes all subsystems, wires up
    /// the app grid, dock, control center, window manager, and input.
    /// Attach to a root GameObject in the Home scene.
    /// </summary>
    public class HomeSceneController : MonoBehaviour
    {
        [Header("System Objects (auto-created if null)")]
        [SerializeField] private InputManager inputManager;
        [SerializeField] private WindowManager windowManager;
        [SerializeField] private AppRegistry appRegistry;
        [SerializeField] private AppLauncher appLauncher;
        [SerializeField] private EnvironmentManager environmentManager;

        [Header("UI Objects")]
        [SerializeField] private AppGridController appGrid;
        [SerializeField] private DockController dock;
        [SerializeField] private ControlCenterController controlCenter;

        [Header("Interaction")]
        [SerializeField] private LayerMask interactableLayer = ~0;
        [SerializeField] private float maxInteractionDistance = 10f;

        private WindowController _draggedWindow;
        private AppIconController _hoveredIcon;
        private DockIconController _hoveredDockIcon;

        private void Awake()
        {
            EnsureSubsystems();
        }

        private void Start()
        {
            // Initialize app registry and discover apps
            appRegistry.Initialize();

            // Wire up launch result feedback
            appLauncher.OnLaunchResult += HandleLaunchResult;

            Debug.Log("[VisionSpace] Home scene ready.");
        }

        private void EnsureSubsystems()
        {
            if (inputManager == null)
                inputManager = FindOrCreate<InputManager>("InputManager");
            if (windowManager == null)
                windowManager = FindOrCreate<WindowManager>("WindowManager");
            if (appRegistry == null)
                appRegistry = FindOrCreate<AppRegistry>("AppRegistry");
            if (appLauncher == null)
                appLauncher = FindOrCreate<AppLauncher>("AppLauncher");
            if (environmentManager == null)
                environmentManager = FindOrCreate<EnvironmentManager>("EnvironmentManager");
            if (appGrid == null)
            {
                appGrid = FindOrCreate<AppGridController>("AppGrid");
                appGrid.transform.localPosition = new Vector3(0, 0, 1.4f);
            }
            if (dock == null)
                dock = FindOrCreate<DockController>("Dock");
            if (controlCenter == null)
            {
                controlCenter = FindOrCreate<ControlCenterController>("ControlCenter");
                controlCenter.transform.localPosition = new Vector3(0, 0.35f, 1.2f);
            }
        }

        private T FindOrCreate<T>(string name) where T : MonoBehaviour
        {
            var existing = FindObjectOfType<T>();
            if (existing != null) return existing;

            var obj = new GameObject(name);
            obj.transform.SetParent(transform);
            return obj.AddComponent<T>();
        }

        private void Update()
        {
            HandleInteraction();
        }

        /// <summary>
        /// Process input interaction with windows, icons, and dock.
        /// </summary>
        private void HandleInteraction()
        {
            if (inputManager == null || inputManager.ActiveProvider == null) return;

            var provider = inputManager.ActiveProvider;
            if (!provider.IsActive) return;

            // Raycast
            if (Physics.Raycast(provider.RayOrigin, provider.RayDirection,
                                out RaycastHit hit, maxInteractionDistance, interactableLayer))
            {
                // Check what we hit
                var iconCtrl = hit.collider.GetComponent<AppIconController>();
                var dockIconCtrl = hit.collider.GetComponent<DockIconController>();
                var windowCtrl = hit.collider.GetComponentInParent<WindowController>();

                // App icon hover
                if (iconCtrl != null)
                {
                    if (_hoveredIcon != iconCtrl)
                    {
                        _hoveredIcon?.OnHoverExit();
                        _hoveredIcon = iconCtrl;
                        _hoveredIcon.OnHoverEnter();
                    }

                    if (provider.SelectPressed)
                        iconCtrl.OnSelect();
                }
                else if (_hoveredIcon != null)
                {
                    _hoveredIcon.OnHoverExit();
                    _hoveredIcon = null;
                }

                // Dock icon hover
                if (dockIconCtrl != null)
                {
                    if (_hoveredDockIcon != dockIconCtrl)
                    {
                        _hoveredDockIcon?.OnHoverExit();
                        _hoveredDockIcon = dockIconCtrl;
                        _hoveredDockIcon.OnHoverEnter();
                    }

                    if (provider.SelectPressed)
                        dockIconCtrl.OnSelect();
                }
                else if (_hoveredDockIcon != null)
                {
                    _hoveredDockIcon.OnHoverExit();
                    _hoveredDockIcon = null;
                }

                // Window grab/drag
                if (windowCtrl != null)
                {
                    if (provider.GrabPressed)
                    {
                        _draggedWindow = windowCtrl;
                        _draggedWindow.BeginDrag(hit.point);
                    }

                    if (provider.SelectPressed)
                        windowCtrl.RequestFocus();
                }
            }
            else
            {
                // Clear hovers
                if (_hoveredIcon != null) { _hoveredIcon.OnHoverExit(); _hoveredIcon = null; }
                if (_hoveredDockIcon != null) { _hoveredDockIcon.OnHoverExit(); _hoveredDockIcon = null; }
            }

            // Continue drag
            if (_draggedWindow != null)
            {
                if (provider.GrabHeld)
                {
                    _draggedWindow.UpdateDrag(provider.RayOrigin, provider.RayDirection);
                }
                else if (provider.GrabReleased)
                {
                    _draggedWindow.EndDrag();
                    _draggedWindow = null;
                }
            }

            // Menu button — toggle control center
            if (provider.MenuPressed)
            {
                controlCenter?.Toggle();
            }
        }

        private void HandleLaunchResult(AppEntry entry, bool success, string message)
        {
            if (!success)
            {
                Debug.LogWarning($"[VisionSpace] Launch failed: {message}");
                // Could show a notification window here
            }
        }

        private void OnDestroy()
        {
            if (appLauncher != null)
                appLauncher.OnLaunchResult -= HandleLaunchResult;
        }
    }
}
