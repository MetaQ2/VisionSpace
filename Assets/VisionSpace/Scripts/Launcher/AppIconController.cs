// =============================================================================
// AppIconController.cs — Individual app icon in the grid
// =============================================================================
using UnityEngine;
using TMPro;

namespace VisionSpace.Launcher
{
    /// <summary>
    /// Controls a single app icon in the grid: visual, hover effects, selection.
    /// </summary>
    public class AppIconController : MonoBehaviour
    {
        [SerializeField] private Renderer iconRenderer;
        [SerializeField] private Renderer backgroundRenderer;
        [SerializeField] private TMP_Text labelText;

        private AppEntry _entry;
        private float _baseScale;
        private Vector3 _originalScale;
        private bool _isHovered;
        private float _hoverProgress;
        private float _entranceProgress;
        private float _entranceDelay;
        private float _entranceDuration;
        private bool _isAnimatingEntrance;

        // Hover animation params
        private const float HoverScale = 1.15f;
        private const float HoverSpeed = 8f;
        private const float HoverZOffset = -0.01f;

        public AppEntry Entry => _entry;

        public void Setup(AppEntry entry, float size)
        {
            _entry = entry;
            _baseScale = size;
            _originalScale = Vector3.one * size;
            transform.localScale = Vector3.zero; // Start invisible for entrance animation

            // Set icon texture if available
            if (entry.icon != null && iconRenderer != null)
            {
                iconRenderer.material.mainTexture = entry.icon;
            }

            // Set background color
            if (backgroundRenderer != null)
            {
                backgroundRenderer.material.color = new Color(
                    entry.accentColor.r * 0.3f,
                    entry.accentColor.g * 0.3f,
                    entry.accentColor.b * 0.3f,
                    0.85f);
            }
            else
            {
                // Use first renderer found
                var r = GetComponentInChildren<Renderer>();
                if (r != null)
                {
                    r.material.color = new Color(
                        entry.accentColor.r * 0.4f + 0.1f,
                        entry.accentColor.g * 0.4f + 0.1f,
                        entry.accentColor.b * 0.4f + 0.1f,
                        0.85f);
                }
            }

            // Set label
            if (labelText != null)
            {
                labelText.text = entry.displayName;
            }
            else
            {
                // Create a simple label via 3D text
                CreateLabel(entry.displayName);
            }

            gameObject.name = $"Icon_{entry.displayName}";
        }

        public void AnimateEntrance(float delay, float duration)
        {
            _entranceDelay = delay;
            _entranceDuration = duration;
            _entranceProgress = 0;
            _isAnimatingEntrance = true;
            transform.localScale = Vector3.zero;
        }

        private void Update()
        {
            // Entrance animation
            if (_isAnimatingEntrance)
            {
                _entranceDelay -= Time.deltaTime;
                if (_entranceDelay <= 0)
                {
                    _entranceProgress += Time.deltaTime / _entranceDuration;
                    float t = Mathf.Clamp01(_entranceProgress);
                    // Elastic ease-out
                    float ease = 1f - Mathf.Pow(2f, -10f * t) *
                                 Mathf.Cos(t * Mathf.PI * 2f * 0.75f);
                    transform.localScale = _originalScale * ease;

                    if (t >= 1f) _isAnimatingEntrance = false;
                }
                return;
            }

            // Hover animation
            float targetHover = _isHovered ? 1f : 0f;
            _hoverProgress = Mathf.Lerp(_hoverProgress, targetHover, Time.deltaTime * HoverSpeed);

            float scale = Mathf.Lerp(1f, HoverScale, _hoverProgress);
            float zOffset = Mathf.Lerp(0f, HoverZOffset, _hoverProgress);

            transform.localScale = _originalScale * scale;
            Vector3 pos = transform.localPosition;
            pos.z = zOffset;
            transform.localPosition = pos;
        }

        /// <summary>Called by the input system when hovering over this icon.</summary>
        public void OnHoverEnter()
        {
            _isHovered = true;
        }

        /// <summary>Called by the input system when hover exits.</summary>
        public void OnHoverExit()
        {
            _isHovered = false;
        }

        /// <summary>Called when the icon is selected (trigger/pinch).</summary>
        public void OnSelect()
        {
            if (_entry == null) return;

            Debug.Log($"[VisionSpace] Icon selected: {_entry.displayName}");

            // Pulse animation on select
            StartCoroutine(SelectPulse());

            // Launch the app
            AppLauncher.Instance?.Launch(_entry);
        }

        private System.Collections.IEnumerator SelectPulse()
        {
            float duration = 0.15f;
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float pulse = 1f + 0.1f * Mathf.Sin(t * Mathf.PI);
                transform.localScale = _originalScale * pulse;
                yield return null;
            }

            transform.localScale = _originalScale;
        }

        private void CreateLabel(string text)
        {
            var labelObj = new GameObject("Label");
            labelObj.transform.SetParent(transform);
            labelObj.transform.localPosition = new Vector3(0, -_baseScale * 0.65f, 0);
            labelObj.transform.localRotation = Quaternion.identity;
            labelObj.transform.localScale = Vector3.one * 0.15f;

            var tmp = labelObj.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = 2f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.9f, 0.9f, 0.95f, 0.9f);
            tmp.enableWordWrapping = true;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            tmp.rectTransform.sizeDelta = new Vector2(6f, 2f);
            labelText = tmp;
        }
    }
}
