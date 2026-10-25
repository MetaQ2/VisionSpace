// =============================================================================
// AppRegistryTests.cs — Editor tests for AppRegistry logic
// =============================================================================
using NUnit.Framework;
using VisionSpace.Launcher;
using System.Collections.Generic;

namespace VisionSpace.Tests.Editor
{
    [TestFixture]
    public class AppRegistryTests
    {
        [Test]
        public void CreateManualEntry_SetsCorrectFields()
        {
            var entry = AppEntry.CreateManual("com.test.app", "Test App");

            Assert.AreEqual("com.test.app", entry.packageName);
            Assert.AreEqual("Test App", entry.displayName);
            Assert.AreEqual(AppSource.Manual, entry.source);
            Assert.IsTrue(entry.CanLaunch);
        }

        [Test]
        public void CreateInternalEntry_HasCorrectPackageName()
        {
            var entry = AppEntry.CreateInternal("browser", "Browser",
                UnityEngine.Color.blue);

            Assert.AreEqual("visionspace.internal.browser", entry.packageName);
            Assert.AreEqual(AppSource.Internal, entry.source);
        }

        [Test]
        public void EmptyPackageName_CannotLaunch()
        {
            var entry = new AppEntry { packageName = "" };
            Assert.IsFalse(entry.CanLaunch);
        }

        [Test]
        public void NullPackageName_CannotLaunch()
        {
            var entry = new AppEntry { packageName = null };
            Assert.IsFalse(entry.CanLaunch);
        }

        [Test]
        public void Search_FindsByDisplayName()
        {
            var apps = new List<AppEntry>
            {
                AppEntry.CreateManual("com.a", "Beat Saber"),
                AppEntry.CreateManual("com.b", "SUPERHOT VR"),
                AppEntry.CreateManual("com.c", "Netflix"),
            };

            string query = "beat";
            var results = apps.FindAll(a =>
                a.displayName.ToLowerInvariant().Contains(query));

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("Beat Saber", results[0].displayName);
        }

        [Test]
        public void Search_FindsByPackageName()
        {
            var apps = new List<AppEntry>
            {
                AppEntry.CreateManual("com.beatgames.beatsaber", "Beat Saber"),
                AppEntry.CreateManual("com.netflix", "Netflix"),
            };

            string query = "netflix";
            var results = apps.FindAll(a =>
                a.packageName.ToLowerInvariant().Contains(query));

            Assert.AreEqual(1, results.Count);
        }

        [Test]
        public void Favorites_FilterCorrectly()
        {
            var apps = new List<AppEntry>
            {
                AppEntry.CreateManual("com.a", "App A"),
                AppEntry.CreateManual("com.b", "App B"),
                AppEntry.CreateManual("com.c", "App C"),
            };

            apps[0].isFavorite = true;
            apps[2].isFavorite = true;

            var favorites = apps.FindAll(a => a.isFavorite);
            Assert.AreEqual(2, favorites.Count);
        }
    }
}
