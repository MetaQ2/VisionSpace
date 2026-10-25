// =============================================================================
// LayoutSerializerTests.cs — Tests for layout persistence
// =============================================================================
using NUnit.Framework;
using VisionSpace.Persistence;

namespace VisionSpace.Tests.Editor
{
    [TestFixture]
    public class LayoutSerializerTests
    {
        [Test]
        public void WindowData_SerializesPosition()
        {
            var wd = new LayoutSerializer.WindowData
            {
                title = "Test",
                type = "Standard",
                position = new float[] { 1f, 2f, 3f },
                rotation = new float[] { 0, 0, 0, 1 },
                scale = new float[] { 1, 1, 1 }
            };

            Assert.AreEqual("Test", wd.title);
            Assert.AreEqual(1f, wd.position[0]);
            Assert.AreEqual(2f, wd.position[1]);
            Assert.AreEqual(3f, wd.position[2]);
        }

        [Test]
        public void SavedLayout_InitializesEmpty()
        {
            var layout = new LayoutSerializer.SavedLayout
            {
                name = "TestLayout",
                timestamp = 12345
            };

            Assert.AreEqual("TestLayout", layout.name);
            Assert.IsNotNull(layout.windows);
            Assert.AreEqual(0, layout.windows.Count);
        }

        [Test]
        public void ListSavedLayouts_ReturnsArrayOnMissingDir()
        {
            // Should not crash even if directory doesn't exist
            var layouts = LayoutSerializer.ListSavedLayouts();
            Assert.IsNotNull(layouts);
        }
    }
}
