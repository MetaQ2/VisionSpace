// =============================================================================
// HandTrackingInputProvider.cs — Meta Quest hand tracking input
// =============================================================================
using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

namespace VisionSpace.Input
{
    /// <summary>
    /// Provides hand tracking input via OpenXR hand tracking.
    /// Uses right hand as primary, with pinch gesture for selection
    /// and grab gesture for window manipulation.
    /// 
    /// NOTE: Quest 2 hand tracking has limited FOV and no eye tracking.
    /// Hand rays are approximated from wrist/index direction.
    /// </summary>
    public class HandTrackingInputProvider : MonoBehaviour, IInputProvider
    {
        public string ProviderName => "Hand Tracking";
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

        [Header("Pinch Detection")]
        [SerializeField] private float pinchThreshold = 0.02f;  // Distance in meters
        [SerializeField] private float pinchReleaseThreshold = 0.04f;
        [SerializeField] private float grabThreshold = 0.03f;

        private InputDevice _rightHand;
        private bool _prevPinch;
        private bool _prevGrab;
        private bool _deviceFound;
        private bool _isPinching;

        // Hand joint positions (if using Meta XR Hand Tracking)
        private Vector3 _indexTipPos;
        private Vector3 _thumbTipPos;
        private Vector3 _wristPos;
        private Quaternion _wristRot;
        private Vector3 _middleTipPos;

        public void UpdateInput()
        {
            TryFindHand();

            if (!_deviceFound || !_rightHand.isValid)
            {
                IsActive = false;
                SelectPressed = false;
                SelectReleased = false;
                SelectHeld = false;
                GrabPressed = false;
                GrabReleased = false;
                GrabHeld = false;
                return;
            }

            // Check if hand is actually being tracked
            _rightHand.TryGetFeatureValue(CommonUsages.isTracked, out bool isTracked);
            IsActive = isTracked;

            if (!isTracked)
            {
                SelectPressed = false;
                SelectReleased = false;
                SelectHeld = false;
                GrabPressed = false;
                GrabReleased = false;
                GrabHeld = false;
                return;
            }

            // Get hand position/rotation for ray approximation
            _rightHand.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 handPos);
            _rightHand.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion handRot);

            // Ray from hand — approximate aim direction
            Transform trackingOrigin = Camera.main?.transform.parent;
            if (trackingOrigin != null)
            {
                RayOrigin = trackingOrigin.TransformPoint(handPos);
                RayDirection = (trackingOrigin.rotation * handRot) * Vector3.forward;
            }
            else
            {
                RayOrigin = handPos;
                RayDirection = handRot * Vector3.forward;
            }

            // Pinch detection via trigger analog (Meta maps pinch to trigger on hand devices)
            _rightHand.TryGetFeatureValue(CommonUsages.trigger, out float triggerValue);
            bool currentPinch = triggerValue > 0.7f;

            SelectPressed = currentPinch && !_prevPinch;
            SelectReleased = !currentPinch && _prevPinch;
            SelectHeld = currentPinch;
            _prevPinch = currentPinch;

            // Grab via grip analog
            _rightHand.TryGetFeatureValue(CommonUsages.grip, out float gripValue);
            bool currentGrab = gripValue > 0.7f;

            GrabPressed = currentGrab && !_prevGrab;
            GrabReleased = !currentGrab && _prevGrab;
            GrabHeld = currentGrab;
            _prevGrab = currentGrab;

            // No thumbstick on hands
            ScrollInput = Vector2.zero;
            MenuPressed = false;
        }

        private void TryFindHand()
        {
            if (_deviceFound && _rightHand.isValid) return;

            var devices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(
                InputDeviceCharacteristics.Right |
                InputDeviceCharacteristics.HandTracking,
                devices);

            if (devices.Count > 0)
            {
                _rightHand = devices[0];
                _deviceFound = true;
                Debug.Log($"[VisionSpace] Hand tracking found: {_rightHand.name}");
            }
            else
            {
                _deviceFound = false;
            }
        }
    }
}
