// =============================================================================
// WindowLayoutPreset.cs — Saved window arrangement data
// =============================================================================
using UnityEngine;

namespace VisionSpace.WindowManagement
{
    /// <summary>
    /// Defines a saved window layout arrangement.
    /// Can be used as a ScriptableObject in the editor or serialized at runtime.
    /// </summary>
    [CreateAssetMenu(fileName = "NewLayout", menuName = "VisionSpace/Window Layout Preset")]
    public class WindowLayoutPreset : ScriptableObject
    {
        [Header("Layout Info")]
        public string layoutName = "Default";
        public string description = "";

        [Header("Positions (normalized: x=horizontal angle, y=vertical angle, z=depth offset)")]
        public Vector3[] positions = new Vector3[]
        {
            new Vector3(0, 0, 0),       // Center
            new Vector3(-0.5f, 0, 0),   // Left
            new Vector3(0.5f, 0, 0),    // Right
        };

        [Header("Scales")]
        public Vector3[] scales = new Vector3[]
        {
            Vector3.one,
            Vector3.one,
            Vector3.one,
        };

        /// <summary>
        /// Create a Focus Mode preset (single centered window).
        /// </summary>
        public static WindowLayoutPreset CreateFocusMode()
        {
            var preset = CreateInstance<WindowLayoutPreset>();
            preset.layoutName = "Focus";
            preset.description = "Single centered window for focused work";
            preset.positions = new[] { new Vector3(0, 0, 0) };
            preset.scales = new[] { Vector3.one * 1.2f };
            return preset;
        }

        /// <summary>
        /// Create a Productivity preset (three windows side by side).
        /// </summary>
        public static WindowLayoutPreset CreateProductivityMode()
        {
            var preset = CreateInstance<WindowLayoutPreset>();
            preset.layoutName = "Productivity";
            preset.description = "Three windows arranged side by side";
            preset.positions = new[]
            {
                new Vector3(-0.6f, 0, 0),
                new Vector3(0, 0, 0),
                new Vector3(0.6f, 0, 0),
            };
            preset.scales = new[] { Vector3.one, Vector3.one, Vector3.one };
            return preset;
        }

        /// <summary>
        /// Create an Entertainment preset (large center with small side panels).
        /// </summary>
        public static WindowLayoutPreset CreateEntertainmentMode()
        {
            var preset = CreateInstance<WindowLayoutPreset>();
            preset.layoutName = "Entertainment";
            preset.description = "Large center with small side panels";
            preset.positions = new[]
            {
                new Vector3(0, 0, -0.3f),
                new Vector3(-0.8f, 0, 0.2f),
                new Vector3(0.8f, 0, 0.2f),
            };
            preset.scales = new[]
            {
                Vector3.one * 1.5f,
                Vector3.one * 0.7f,
                Vector3.one * 0.7f,
            };
            return preset;
        }
    }
}
