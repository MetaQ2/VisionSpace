// =============================================================================
// GlassPanelAnimator.cs — Reusable glass panel animation utilities
// =============================================================================
using UnityEngine;

namespace VisionSpace.UI
{
    /// <summary>
    /// Utility component for animating glass panels — entrance, exit, 
    /// hover glow, breathing pulse.
    /// </summary>
    public class GlassPanelAnimator : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private float entranceDuration = 0.4f;
        [SerializeField] private float exitDuration = 0.25f;
        [SerializeField] private AnimationCurve entranceCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Breathing")]
        [SerializeField] private bool enableBreathing;
        [SerializeField] private float breathingSpeed = 1.5f;
        [SerializeField] private float breathingAmount = 0.005f;

        private Vector3 _baseScale;
        private float _animProgress;
        private bool _isAnimatingIn;
        private bool _isAnimatingOut;
        private System.Action _onExitComplete;

        private void Start()
        {
            _baseScale = transform.localScale;
        }

        public void AnimateIn()
        {
            _isAnimatingIn = true;
            _isAnimatingOut = false;
            _animProgress = 0;
            transform.localScale = Vector3.zero;
        }

        public void AnimateOut(System.Action onComplete = null)
        {
            _isAnimatingOut = true;
            _isAnimatingIn = false;
            _animProgress = 0;
            _onExitComplete = onComplete;
        }

        private void Update()
        {
            if (_isAnimatingIn)
            {
                _animProgress += Time.deltaTime / entranceDuration;
                float t = entranceCurve.Evaluate(Mathf.Clamp01(_animProgress));
                transform.localScale = _baseScale * t;
                if (_animProgress >= 1f) _isAnimatingIn = false;
            }
            else if (_isAnimatingOut)
            {
                _animProgress += Time.deltaTime / exitDuration;
                float t = 1f - Mathf.Clamp01(_animProgress);
                transform.localScale = _baseScale * t;
                if (_animProgress >= 1f)
                {
                    _isAnimatingOut = false;
                    _onExitComplete?.Invoke();
                }
            }
            else if (enableBreathing)
            {
                float breath = Mathf.Sin(Time.time * breathingSpeed) * breathingAmount;
                transform.localScale = _baseScale * (1f + breath);
            }
        }
    }
}
