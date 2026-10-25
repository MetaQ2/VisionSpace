// =============================================================================
// AppGridController.cs — Spatial app grid panel (Vision Pro-style home screen)
// =============================================================================
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace VisionSpace.Launcher
{
    /// <summary>
    /// Controls the floating glass app grid panel. Displays app icons in a
    /// grid layout with categories, search, favorites, and smooth animations.
    /// Attach to the AppGrid panel prefab in the Home scene.
    /// </summary>
    public class AppGridController : MonoBehaviour
    {
        [Header("Grid Layout")]
        [SerializeField] private int columns = 4;
        [SerializeField] private int rows = 3;
        [SerializeField] private float iconSpacing = 0.12f;
        [SerializeField] private float iconSize = 0.08f;

        [Header("References")]
        [SerializeField] private Transform iconContainer;
        [SerializeField] private GameObject appIconPrefab;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text pageIndicatorText;

        [Header("Categories")]
        [SerializeField] private Transform categoryBar;

        [Header("Animation")]
        [SerializeField] private float staggerDelay = 0.05f;
        [SerializeField] private float animationDuration = 0.3f;

        private List<AppEntry> _displayedApps = new List<AppEntry>();
        private List<AppIconController> _iconControllers = new List<AppIconController>();
        private int _currentPage;
        private int _totalPages;
        private string _currentCategory = "All";
        private string _searchQuery = "";

        private int AppsPerPage => columns * rows;

        private void Start()
        {
            if (iconContainer == null)
            {
                iconContainer = new GameObject("IconContainer").transform;
                iconContainer.SetParent(transform);
                iconContainer.localPosition = Vector3.zero;
            }

            RefreshGrid();
        }

        /// <summary>Refresh the grid with current filters.</summary>
        public void RefreshGrid()
        {
            var registry = AppRegistry.Instance;
            if (registry == null) return;

            // Apply filters
            if (!string.IsNullOrEmpty(_searchQuery))
                _displayedApps = registry.Search(_searchQuery);
            else if (_currentCategory == "Favorites")
                _displayedApps = registry.GetFavorites();
            else if (_currentCategory == "Recent")
                _displayedApps = registry.GetRecent(AppsPerPage);
            else
                _displayedApps = registry.GetByCategory(_currentCategory);

            _totalPages = Mathf.Max(1, Mathf.CeilToInt((float)_displayedApps.Count / AppsPerPage));
            _currentPage = Mathf.Clamp(_currentPage, 0, _totalPages - 1);

            RebuildIcons();
            UpdatePageIndicator();
        }

        /// <summary>Navigate to next page.</summary>
        public void NextPage()
        {
            if (_currentPage < _totalPages - 1)
            {
                _currentPage++;
                RebuildIcons();
                UpdatePageIndicator();
            }
        }

        /// <summary>Navigate to previous page.</summary>
        public void PreviousPage()
        {
            if (_currentPage > 0)
            {
                _currentPage--;
                RebuildIcons();
                UpdatePageIndicator();
            }
        }

        /// <summary>Set category filter.</summary>
        public void SetCategory(string category)
        {
            _currentCategory = category;
            _currentPage = 0;
            RefreshGrid();
        }

        /// <summary>Set search query.</summary>
        public void SetSearchQuery(string query)
        {
            _searchQuery = query;
            _currentPage = 0;
            RefreshGrid();
        }

        /// <summary>Show/hide the grid with animation.</summary>
        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
            if (visible) RefreshGrid();
        }

        private void RebuildIcons()
        {
            // Clear existing icons
            foreach (var ic in _iconControllers)
            {
                if (ic != null && ic.gameObject != null)
                    Destroy(ic.gameObject);
            }
            _iconControllers.Clear();

            // Calculate page slice
            int startIndex = _currentPage * AppsPerPage;
            int endIndex = Mathf.Min(startIndex + AppsPerPage, _displayedApps.Count);

            // Grid origin (centered)
            float gridWidth = (columns - 1) * iconSpacing;
            float gridHeight = (rows - 1) * iconSpacing;
            Vector3 gridOrigin = new Vector3(-gridWidth / 2f, gridHeight / 2f, 0);

            for (int i = startIndex; i < endIndex; i++)
            {
                int gridIndex = i - startIndex;
                int col = gridIndex % columns;
                int row = gridIndex / columns;

                Vector3 localPos = gridOrigin + new Vector3(
                    col * iconSpacing,
                    -row * iconSpacing,
                    0);

                GameObject iconObj;
                if (appIconPrefab != null)
                {
                    iconObj = Instantiate(appIconPrefab, iconContainer);
                }
                else
                {
                    iconObj = CreateDefaultIconObject();
                    iconObj.transform.SetParent(iconContainer);
                }

                iconObj.transform.localPosition = localPos;
                iconObj.transform.localRotation = Quaternion.identity;

                var controller = iconObj.GetComponent<AppIconController>();
                if (controller == null)
                    controller = iconObj.AddComponent<AppIconController>();

                controller.Setup(_displayedApps[i], iconSize);
                _iconControllers.Add(controller);

                // Staggered entrance animation
                float delay = gridIndex * staggerDelay;
                controller.AnimateEntrance(delay, animationDuration);
            }

            if (titleText != null)
                titleText.text = _currentCategory == "All" ? "Apps" : _currentCategory;
        }

        private void UpdatePageIndicator()
        {
            if (pageIndicatorText != null)
            {
                pageIndicatorText.text = _totalPages > 1
                    ? $"{_currentPage + 1} / {_totalPages}"
                    : "";
            }
        }

        private GameObject CreateDefaultIconObject()
        {
            var obj = new GameObject("AppIcon");

            // Background quad with rounded appearance
            var bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
            bg.transform.SetParent(obj.transform);
            bg.transform.localPosition = Vector3.zero;
            bg.transform.localScale = Vector3.one * iconSize;
            bg.name = "IconBackground";

            var renderer = bg.GetComponent<Renderer>();
            var mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = new Color(0.2f, 0.2f, 0.3f, 0.85f);
            renderer.material = mat;

            // Collider for interaction
            var col = obj.AddComponent<BoxCollider>();
            col.size = new Vector3(iconSize, iconSize, 0.01f);

            return obj;
        }

        private void Update()
        {
            // Handle scroll input for pagination
            var input = Input.InputManager.Instance;
            if (input == null) return;

            Vector2 scroll = input.ScrollInput;
            if (Mathf.Abs(scroll.x) > 0.8f)
            {
                if (scroll.x > 0) NextPage();
                else PreviousPage();
            }
        }
    }
}
