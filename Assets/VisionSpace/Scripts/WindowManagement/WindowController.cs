// =============================================================================
// WindowController.cs — Individual window behavior and interaction
// =============================================================================
using System;
using UnityEngine;

namespace VisionSpace.WindowManagement
{
    /// <summary>
    /// Controls a single spatial window — position, rotation, scale, state,
    /// drag interaction, close/minimize, focus visuals.
    /// </summary>
    public class WindowController : MonoBehaviour
    {
        [Header("Window Identity")]
        [SerializeField] private string _title;
        [SerializeField] private int _windowId;
        [SerializeField] private WindowType _windowType;

        [Header("Visual References")]
        [SerializeField] private Transform glassPanel;
        [SerializeField] private Transform contentArea;
        [SerializeField] private Transform titleBar;
        [SerializeField] private Renderer panelRenderer;

        [Header("State")]
        [SerializeField] private WindowState _state = WindowState.Normal;
        [SerializeField] private bool _isFocused;

        [Header("Animation")]
        [SerializeField] private float animationSpeed = 8f;
        [SerializeField] private float hoverScaleMultiplier = 1.02f;

        // Colors for focused/unfocused states
        private Color _focusedColor = new Color(0.15f, 0.15f, 0.22f, 0.9f);
        private Color _unfocusedColor = new Color(0.08f, 0.08f, 0.12f, 0.75f);
        private Color _focusedEdge = new Color(0.4f, 0.6f, 1f, 0.3f);

        // Drag state
        private bool _isDragging;
        private Vector3 _dragOffset;
        private float _dragDistance;

        // Animation targets
        private Vector3 _targetPosition;
        private Vector3 _targetScale;
        private Quaternion _targetRotation;
        private bool _isAnimating;
        private Action _onAnimationComplete;

        // Minimized state
        private Vector3 _preMinimizePosition;
        private Vector3 _preMinimizeScale;

        public string Title => _title;
        public int WindowId => _windowId;
        public WindowType Type => _windowType;
        public WindowState State => _state;
        public bool IsFocused => _isFocused;
        public bool IsDragging => _isDragging;

        public event Action<WindowController> OnCloseRequested;
        public event Action<WindowController> OnFocusRequested;

        /// <summary>
        /// Initialize the window after instantiation.
        /// </summary>
        public void Initialize(int id, string title, WindowType type, Vector2 size)
        {
            _windowId = id;
            _title = title;
            _windowType = type;

            // Set panel size
            if (glassPanel != null)
            {
                glassPanel.localScale = new Vector3(size.x, size.y, 1f);
            }
            else
            {
                // Find glass panel child
                var panelTransform = transform.Find("GlassPanel");
                if (panelTransform != null)
                {
                    panelTransform.localScale = new Vector3(size.x, size.y, 1f);
                    glassPanel = panelTransform;
                    panelRenderer = panelTransform.GetComponent<Renderer>();
                }
            }

            _targetPosition = transform.position;
            _targetScale = transform.localScale;
            _targetRotation = transform.rotation;

            SetFocused(false);
        }

        /// <summary>
        /// Set the focused state with visual feedback.
        /// </summary>
        public void SetFocused(bool focused)
        {
            _isFocused = focused;

            if (panelRenderer != null)
            {
                panelRenderer.material.color = focused ? _focusedColor : _unfocusedColor;
            }
        }

        /// <summary>
        /// Make the window face a point (typically the camera position).
        /// </summary>
        public void FacePoint(Vector3 point)
        {
            Vector3 direction = point - transform.position;
            direction.y = 0; // Keep window upright
            if (direction.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(-direction.normalized);
                _targetRotation = transform.rotation;
            }
        }

        /// <summary>
        /// Smoothly animate to a new position and scale.
        /// </summary>
        public void AnimateToPosition(Vector3 position, Vector3 scale, float duration,
                                       Action onComplete = null)
        {
            _targetPosition = position;
            _targetScale = scale;
            _isAnimating = true;
            _onAnimationComplete = onComplete;
        }

        /// <summary>
        /// Animate window close (scale down, fade out, then invoke callback).
        /// </summary>
        public void AnimateClose(Action onComplete)
        {
            _targetScale = Vector3.zero;
            _isAnimating = true;
            _onAnimationComplete = onComplete;
        }

        /// <summary>
        /// Begin dragging the window.
        /// </summary>
        public void BeginDrag(Vector3 hitPoint)
        {
            _isDragging = true;
            _dragOffset = transform.position - hitPoint;
            _dragDistance = Vector3.Distance(Camera.main.transform.position, transform.position);
            OnFocusRequested?.Invoke(this);
        }

        /// <summary>
        /// Update drag position.
        /// </summary>
        public void UpdateDrag(Vector3 rayOrigin, Vector3 rayDirection)
        {
            if (!_isDragging) return;

            Vector3 targetPos = rayOrigin + rayDirection * _dragDistance + _dragOffset;

            // Constrain distance from user
            Camera cam = Camera.main;
            if (cam != null)
            {
                Vector3 toTarget = targetPos - cam.transform.position;
                float dist = toTarget.magnitude;
                dist = Mathf.Clamp(dist, 0.5f, 5.0f);
                targetPos = cam.transform.position + toTarget.normalized * dist;
            }

            transform.position = Vector3.Lerp(transform.position, targetPos,
                                               Time.deltaTime * animationSpeed);
        }

        /// <summary>
        /// End dragging.
        /// </summary>
        public void EndDrag()
        {
            _isDragging = false;
            _targetPosition = transform.position;

            // Face user after drag
            if (Camera.main != null)
            {
                FacePoint(Camera.main.transform.position);
            }
        }

        /// <summary>
        /// Minimize the window to a small icon.
        /// </summary>
        public void Minimize()
        {
            if (_state == WindowState.Minimized) return;

            _preMinimizePosition = transform.position;
            _preMinimizeScale = transform.localScale;
            _state = WindowState.Minimized;

            // Animate to small scale near the dock area
            _targetScale = Vector3.one * 0.1f;
            _targetPosition = transform.position + Vector3.down * 0.3f;
            _isAnimating = true;
        }

        /// <summary>
        /// Restore from minimized state.
        /// </summary>
        public void Restore()
        {
            if (_state != WindowState.Minimized) return;

            _state = WindowState.Normal;
            _targetScale = _preMinimizeScale;
            _targetPosition = _preMinimizePosition;
            _isAnimating = true;
        }

        /// <summary>
        /// Request this window to close.
        /// </summary>
        public void RequestClose()
        {
            OnCloseRequested?.Invoke(this);
        }

        /// <summary>
        /// Request focus for this window.
        /// </summary>
        public void RequestFocus()
        {
            OnFocusRequested?.Invoke(this);
        }

        private void Update()
        {
            if (_isAnimating)
            {
                float t = Time.deltaTime * animationSpeed;

                transform.position = Vector3.Lerp(transform.position, _targetPosition, t);
                transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, t);
                transform.rotation = Quaternion.Slerp(transform.rotation, _targetRotation, t);

                // Check if animation is complete
                if (Vector3.Distance(transform.position, _targetPosition) < 0.001f &&
                    Vector3.Distance(transform.localScale, _targetScale) < 0.001f)
                {
                    transform.position = _targetPosition;
                    transform.localScale = _targetScale;
                    transform.rotation = _targetRotation;
                    _isAnimating = false;
                    _onAnimationComplete?.Invoke();
                    _onAnimationComplete = null;
                }
            }
        }

        private void OnMouseEnter()
        {
            // Subtle hover scale
            if (!_isDragging)
            {
                _targetScale = transform.localScale * hoverScaleMultiplier;
                _isAnimating = true;
            }
        }

        private void OnMouseExit()
        {
            if (!_isDragging)
            {
                // Restore original scale (approximate)
                _targetScale = transform.localScale / hoverScaleMultiplier;
                _isAnimating = true;
            }
        }
    }

    public enum WindowState
    {
        Normal,
        Minimized,
        Maximized
    }
}
