// =============================================================================
// HeadGazeInputProvider.cs — Fallback head-directed input
// =============================================================================
using UnityEngine;

namespace VisionSpace.Input
{
    /// <summary>
    /// Fallback input provider using head direction for pointing.
    /// Selection is triggered by a dwell timer or physical button press.
    /// Used when neither controllers nor hand tracking are available.
    /// </summary>
    public class HeadGazeInputProvider : MonoBehaviour, IInputProvider
    {
        public string ProviderName => "Head Gaze";
        public bool IsActive => true; // Always available as fallback
        public Vector3 RayOrigin { get; private set; }
        public Vector3 RayDirection { get; private set; }
        public bool SelectPressed { get; private set; }
        public bool SelectHeld { get; private set; }
        public bool SelectReleased { get; private set; }
        public bool GrabPressed => false;
        public bool GrabHeld => false;
        public bool GrabReleased => false;
        public Vector2 ScrollInput => Vector2.zero;
        public bool MenuPressed => false;

        [Header("Dwell Selection")]
        [SerializeField] private float dwellDuration = 1.5f;
        [SerializeField] private float dwellResetDistance = 0.05f;

        private float _dwellTimer;
        private Vector3 _lastGazeDirection;
        private bool _dwellTriggered;
        private bool _prevDwell;

        public void UpdateInput()
        {
            Camera mainCam = Camera.main;
            if (mainCam == null) return;

            RayOrigin = mainCam.transform.position;
            RayDirection = mainCam.transform.forward;

            // Check if gaze has moved significantly (reset dwell)
            float angleDelta = Vector3.Angle(_lastGazeDirection, RayDirection);
            if (angleDelta > dwellResetDistance * 100f)
            {
                _dwellTimer = 0f;
                _dwellTriggered = false;
            }
            _lastGazeDirection = RayDirection;

            // Dwell timer
            if (Physics.Raycast(RayOrigin, RayDirection, out _, 10f))
            {
                _dwellTimer += Time.deltaTime;
                if (_dwellTimer >= dwellDuration && !_dwellTriggered)
                {
                    _dwellTriggered = true;
                }
            }
            else
            {
                _dwellTimer = 0f;
                _dwellTriggered = false;
            }

            SelectPressed = _dwellTriggered && !_prevDwell;
            SelectHeld = _dwellTriggered;
            SelectReleased = !_dwellTriggered && _prevDwell;
            _prevDwell = _dwellTriggered;
        }
    }
}
