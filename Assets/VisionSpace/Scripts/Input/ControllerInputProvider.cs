// =============================================================================
// ControllerInputProvider.cs — Meta Quest Touch controller input
// =============================================================================
using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

namespace VisionSpace.Input
{
    /// <summary>
    /// Reads input from Meta Quest Touch controllers via Unity XR Input.
    /// Uses the right controller as primary pointer by default.
    /// </summary>
    public class ControllerInputProvider : MonoBehaviour, IInputProvider
    {
        public string ProviderName => "Quest Controller";
        public bool IsActive { get; private set; }
        public Vector3 RayOrigin { get; private set; }
        public Vector3 RayDirection { get; private set; }
        public bool SelectPressed { get; private set; }
        public bool SelectHeld { get; private set; }
        public bool SelectReleased { get; private set; }
        public bool GrabPressed { get; private set; }
        public bool GrabHeld { get; private set; }
        public bool GrabReleased { get; private set; }
        public Vector2 ScrollInput { get; private set; }
        public bool MenuPressed { get; private set; }

        private InputDevice _rightController;
        private bool _prevTrigger;
        private bool _prevGrip;
        private bool _prevMenu;
        private bool _deviceFound;

        public void UpdateInput()
        {
            TryFindController();

            if (!_deviceFound || !_rightController.isValid)
            {
                IsActive = false;
                return;
            }

            IsActive = true;

            // Position and rotation
            _rightController.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 pos);
            _rightController.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rot);

            // Transform from tracking space to world space
            Transform trackingOrigin = Camera.main?.transform.parent;
            if (trackingOrigin != null)
            {
                RayOrigin = trackingOrigin.TransformPoint(pos);
                RayDirection = (trackingOrigin.rotation * rot) * Vector3.forward;
            }
            else
            {
                RayOrigin = pos;
                RayDirection = rot * Vector3.forward;
            }

            // Trigger (index finger) — Select
            _rightController.TryGetFeatureValue(CommonUsages.triggerButton, out bool triggerPressed);
            SelectPressed = triggerPressed && !_prevTrigger;
            SelectReleased = !triggerPressed && _prevTrigger;
            SelectHeld = triggerPressed;
            _prevTrigger = triggerPressed;

            // Grip — Grab
            _rightController.TryGetFeatureValue(CommonUsages.gripButton, out bool gripPressed);
            GrabPressed = gripPressed && !_prevGrip;
            GrabReleased = !gripPressed && _prevGrip;
            GrabHeld = gripPressed;
            _prevGrip = gripPressed;

            // Thumbstick — Scroll
            _rightController.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 thumbstick);
            ScrollInput = thumbstick;

            // Menu button (B or Y button on Quest controllers)
            _rightController.TryGetFeatureValue(CommonUsages.secondaryButton, out bool menuBtn);
            MenuPressed = menuBtn && !_prevMenu;
            _prevMenu = menuBtn;
        }

        private void TryFindController()
        {
            if (_deviceFound && _rightController.isValid) return;

            var devices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(
                InputDeviceCharacteristics.Right |
                InputDeviceCharacteristics.Controller |
                InputDeviceCharacteristics.HeldInHand,
                devices);

            if (devices.Count > 0)
            {
                _rightController = devices[0];
                _deviceFound = true;
                Debug.Log($"[VisionSpace] Controller found: {_rightController.name}");
            }
            else
            {
                _deviceFound = false;
            }
        }
    }
}
