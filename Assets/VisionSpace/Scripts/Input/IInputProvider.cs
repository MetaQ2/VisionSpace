// =============================================================================
// IInputProvider.cs — Input abstraction interface
// =============================================================================
using UnityEngine;

namespace VisionSpace.Input
{
    /// <summary>
    /// Abstracts all input sources (controller, hand tracking, head gaze)
    /// so that UI and window systems interact with a unified interface.
    /// </summary>
    public interface IInputProvider
    {
        /// <summary>Name of this input provider for diagnostics.</summary>
        string ProviderName { get; }

        /// <summary>Whether this provider is currently active and tracking.</summary>
        bool IsActive { get; }

        /// <summary>World-space origin of the primary interaction ray.</summary>
        Vector3 RayOrigin { get; }

        /// <summary>World-space direction of the primary interaction ray.</summary>
        Vector3 RayDirection { get; }

        /// <summary>True during the frame when the user initiates a selection (trigger pull, pinch start).</summary>
        bool SelectPressed { get; }

        /// <summary>True while the user is holding a selection.</summary>
        bool SelectHeld { get; }

        /// <summary>True during the frame when the user releases a selection.</summary>
        bool SelectReleased { get; }

        /// <summary>True during the frame when the user initiates a grab (grip press, grab gesture).</summary>
        bool GrabPressed { get; }

        /// <summary>True while the user is holding a grab.</summary>
        bool GrabHeld { get; }

        /// <summary>True during the frame when the user releases a grab.</summary>
        bool GrabReleased { get; }

        /// <summary>2D scroll/navigation input (thumbstick or swipe).</summary>
        Vector2 ScrollInput { get; }

        /// <summary>True if a menu/back button was pressed this frame.</summary>
        bool MenuPressed { get; }

        /// <summary>Update the provider state. Called each frame by InputManager.</summary>
        void UpdateInput();
    }
}
