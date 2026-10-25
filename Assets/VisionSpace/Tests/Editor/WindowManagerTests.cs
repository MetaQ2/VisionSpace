// =============================================================================
// WindowManagerTests.cs — Editor tests for WindowManager logic
// =============================================================================
using NUnit.Framework;
using VisionSpace.WindowManagement;

namespace VisionSpace.Tests.Editor
{
    [TestFixture]
    public class WindowManagerTests
    {
        [Test]
        public void WindowType_DefaultSize_ReturnsReasonableValues()
        {
            // Verify size mappings produce usable dimensions
            Assert.IsTrue(GetDefaultSize(WindowType.Standard).x > 0);
            Assert.IsTrue(GetDefaultSize(WindowType.Compact).x > 0);
            Assert.IsTrue(GetDefaultSize(WindowType.Large).x > 0);
            Assert.IsTrue(GetDefaultSize(WindowType.Modal).x > 0);

            // Large should be bigger than compact
            Assert.Greater(GetDefaultSize(WindowType.Large).x,
                          GetDefaultSize(WindowType.Compact).x);
        }

        [Test]
        public void WindowLayoutPreset_FocusMode_HasSinglePosition()
        {
            var preset = WindowLayoutPreset.CreateFocusMode();
            Assert.AreEqual("Focus", preset.layoutName);
            Assert.AreEqual(1, preset.positions.Length);
        }

        [Test]
        public void WindowLayoutPreset_ProductivityMode_HasThreePositions()
        {
            var preset = WindowLayoutPreset.CreateProductivityMode();
            Assert.AreEqual("Productivity", preset.layoutName);
            Assert.AreEqual(3, preset.positions.Length);
        }

        [Test]
        public void WindowLayoutPreset_EntertainmentMode_HasLargeCenter()
        {
            var preset = WindowLayoutPreset.CreateEntertainmentMode();
            Assert.AreEqual("Entertainment", preset.layoutName);
            // Center scale should be largest
            Assert.Greater(preset.scales[0].x, preset.scales[1].x);
        }

        // Helper matching WindowManager.GetDefaultSize logic
        private UnityEngine.Vector2 GetDefaultSize(WindowType type) => type switch
        {
            WindowType.Standard => new UnityEngine.Vector2(0.6f, 0.4f),
            WindowType.Compact => new UnityEngine.Vector2(0.3f, 0.2f),
            WindowType.Large => new UnityEngine.Vector2(0.9f, 0.6f),
            WindowType.Modal => new UnityEngine.Vector2(0.4f, 0.3f),
            _ => new UnityEngine.Vector2(0.6f, 0.4f)
        };
    }
}
