// =============================================================================
// DockController.cs — Floating Vision Pro-style dock
// =============================================================================
using System.Collections.Generic;
using UnityEngine;

namespace VisionSpace.UI
{
    /// <summary>
    /// Floating dock positioned at the bottom of the user's view.
    /// Contains configurable shortcuts with hover animations and glass background.
    /// </summary>
    public class DockController : MonoBehaviour
    {
        public static DockController Instance { get; private set; }

        [Header("Layout")]
        [SerializeField] private float iconSpacing = 0.06f;
        [SerializeField] private float iconSize = 0.04f;
        [SerializeField] private float distanceFromUser = 1.2f;
        [SerializeField] private float heightOffset = -0.35f;

        [Header("Behavior")]
        [SerializeField] private bool autoHide;
        [SerializeField] private float autoHideDelay = 3f;
        [SerializeField] private bool followUser = true;
        [SerializeField] private float followSpeed = 3f;

        [Header("Visual")]
        [SerializeField] private Transform dockBackground;
        [SerializeField] private Transform iconContainer;

        private List<DockItem> _items = new List<DockItem>();
        private float _hideTimer;
        private bool _isVisible = true;
        private float _targetAlpha = 1f;
        private Renderer _bgRenderer;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            if (iconContainer == null)
            {
                iconContainer = new GameObject("DockIcons").transform;
                iconContainer.SetParent(transform);
                iconContainer.localPosition = Vector3.zero;
            }

            CreateDefaultDockItems();
            BuildDock();
        }

        private void CreateDefaultDockItems()
        {
            _items.Add(new DockItem("Home", "home", DockAction.GoHome));
            _items.Add(new DockItem("Apps", "apps", DockAction.ToggleAppGrid));
            _items.Add(new DockItem("Browser", "browser", DockAction.OpenBrowser));
            _items.Add(new DockItem("Media", "media", DockAction.OpenMedia));
            _items.Add(new DockItem("Files", "files", DockAction.OpenFiles));
            _items.Add(new DockItem("Settings", "settings", DockAction.OpenSettings));
            _items.Add(new DockItem("Controls", "controls", DockAction.ToggleControlCenter));
        }

        private void BuildDock()
        {
            // Clear existing
            foreach (Transform child in iconContainer)
                Destroy(child.gameObject);

            float totalWidth = (_items.Count - 1) * iconSpacing;
            float startX = -totalWidth / 2f;

            // Create glass background
            if (dockBackground == null)
            {
                var bgObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bgObj.name = "DockBackground";
                bgObj.transform.SetParent(transform);
                bgObj.transform.localPosition = new Vector3(0, 0, 0.005f);
                bgObj.transform.localScale = new Vector3(
                    totalWidth + iconSpacing * 2f, iconSize * 1.5f, 0.008f);

                _bgRenderer = bgObj.GetComponent<Renderer>();
                var mat = new Material(Shader.Find("Sprites/Default"));
                mat.color = new Color(0.1f, 0.1f, 0.15f, 0.7f);
                _bgRenderer.material = mat;

                Destroy(bgObj.GetComponent<Collider>());
                dockBackground = bgObj.transform;
            }

            // Create icons
            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                var iconObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
                iconObj.name = $"DockIcon_{item.label}";
                iconObj.transform.SetParent(iconContainer);
                iconObj.transform.localPosition = new Vector3(startX + i * iconSpacing, 0, 0);
                iconObj.transform.localScale = Vector3.one * iconSize;
                iconObj.transform.localRotation = Quaternion.identity;

                var renderer = iconObj.GetComponent<Renderer>();
                var mat = new Material(Shader.Find("Sprites/Default"));
                mat.color = item.iconColor;
                renderer.material = mat;

                // Add interaction
                var dockIcon = iconObj.AddComponent<DockIconController>();
                dockIcon.Setup(item, iconSize);
            }
        }

        private void Update()
        {
            if (followUser) FollowUserView();

            if (autoHide)
            {
                // Simple auto-hide: fade after delay
                _hideTimer += Time.deltaTime;
                if (_hideTimer > autoHideDelay && _isVisible)
                {
                    _targetAlpha = 0.1f;
                }
            }

            // Smooth alpha
            if (_bgRenderer != null)
            {
                Color c = _bgRenderer.material.color;
                c.a = Mathf.Lerp(c.a, _targetAlpha * 0.7f, Time.deltaTime * 5f);
                _bgRenderer.material.color = c;
            }
        }

        private void FollowUserView()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            // Position dock below the user's forward view
            Vector3 forward = cam.transform.forward;
            forward.y = 0;
            forward.Normalize();

            Vector3 targetPos = cam.transform.position +
                                forward * distanceFromUser +
                                Vector3.up * heightOffset;

            transform.position = Vector3.Lerp(transform.position, targetPos,
                                               Time.deltaTime * followSpeed);

            // Face the user
            Vector3 lookDir = cam.transform.position - transform.position;
            lookDir.y = 0;
            if (lookDir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(-lookDir);
        }

        /// <summary>Show dock and reset auto-hide timer.</summary>
        public void ShowDock()
        {
            _isVisible = true;
            _targetAlpha = 1f;
            _hideTimer = 0;
        }

        /// <summary>Toggle dock visibility.</summary>
        public void ToggleDock()
        {
            _isVisible = !_isVisible;
            _targetAlpha = _isVisible ? 1f : 0.1f;
        }
    }

    /// <summary>Dock item data.</summary>
    [System.Serializable]
    public class DockItem
    {
        public string label;
        public string iconId;
        public DockAction action;
        public Color iconColor;

        public DockItem(string label, string iconId, DockAction action)
        {
            this.label = label;
            this.iconId = iconId;
            this.action = action;

            // Assign distinct colors per action type
            this.iconColor = action switch
            {
                DockAction.GoHome => new Color(0.3f, 0.5f, 0.8f, 0.9f),
                DockAction.ToggleAppGrid => new Color(0.4f, 0.6f, 0.9f, 0.9f),
                DockAction.OpenBrowser => new Color(0.2f, 0.6f, 0.8f, 0.9f),
                DockAction.OpenMedia => new Color(0.8f, 0.3f, 0.5f, 0.9f),
                DockAction.OpenFiles => new Color(0.5f, 0.5f, 0.6f, 0.9f),
                DockAction.OpenSettings => new Color(0.4f, 0.4f, 0.5f, 0.9f),
                DockAction.ToggleControlCenter => new Color(0.3f, 0.3f, 0.4f, 0.9f),
                _ => new Color(0.5f, 0.5f, 0.5f, 0.9f)
            };
        }
    }

    public enum DockAction
    {
        GoHome,
        ToggleAppGrid,
        OpenBrowser,
        OpenMedia,
        OpenFiles,
        OpenSettings,
        ToggleControlCenter,
        OpenRecent,
        Custom
    }
}
