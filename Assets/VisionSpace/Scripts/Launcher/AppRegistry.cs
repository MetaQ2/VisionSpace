// =============================================================================
// AppRegistry.cs — Central app management and favorites
// =============================================================================
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace VisionSpace.Launcher
{
    /// <summary>
    /// Central registry of all known applications. Combines discovered Android apps,
    /// manually registered shortcuts, and internal Vision Space modules.
    /// Persists favorites and launch counts.
    /// </summary>
    public class AppRegistry : MonoBehaviour
    {
        public static AppRegistry Instance { get; private set; }

        private List<AppEntry> _allApps = new List<AppEntry>();
        private string _persistPath;

        public IReadOnlyList<AppEntry> AllApps => _allApps;
        public event Action OnAppsUpdated;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _persistPath = Path.Combine(Application.persistentDataPath, "app_registry.json");
        }

        /// <summary>Discover apps and merge with persisted data.</summary>
        public void Initialize()
        {
            // Add internal modules first
            AddInternalModules();

            // Discover installed Android apps
            var discovered = AppDiscovery.DiscoverInstalledApps();
            foreach (var app in discovered)
            {
                if (!_allApps.Exists(a => a.packageName == app.packageName))
                    _allApps.Add(app);
            }

            // Restore persisted metadata (favorites, launch counts)
            RestorePersistedData();

            Debug.Log($"[VisionSpace] AppRegistry: {_allApps.Count} apps registered.");
            OnAppsUpdated?.Invoke();
        }

        /// <summary>Refresh the app list (re-discover).</summary>
        public void Refresh()
        {
            var discovered = AppDiscovery.DiscoverInstalledApps();
            foreach (var app in discovered)
            {
                if (!_allApps.Exists(a => a.packageName == app.packageName))
                    _allApps.Add(app);
            }
            OnAppsUpdated?.Invoke();
        }

        public List<AppEntry> GetFavorites() =>
            _allApps.Where(a => a.isFavorite).ToList();

        public List<AppEntry> GetRecent(int count = 8) =>
            _allApps.Where(a => a.lastUsedTimestamp > 0)
                    .OrderByDescending(a => a.lastUsedTimestamp)
                    .Take(count).ToList();

        public List<AppEntry> GetByCategory(string category) =>
            string.IsNullOrEmpty(category) || category == "All"
                ? new List<AppEntry>(_allApps)
                : _allApps.Where(a => a.category == category).ToList();

        public List<AppEntry> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return new List<AppEntry>(_allApps);
            string q = query.ToLowerInvariant();
            return _allApps.Where(a =>
                a.displayName.ToLowerInvariant().Contains(q) ||
                a.packageName.ToLowerInvariant().Contains(q)).ToList();
        }

        public void SetFavorite(string packageName, bool favorite)
        {
            var app = _allApps.Find(a => a.packageName == packageName);
            if (app != null) { app.isFavorite = favorite; PersistData(); OnAppsUpdated?.Invoke(); }
        }

        public void RecordLaunch(string packageName)
        {
            var app = _allApps.Find(a => a.packageName == packageName);
            if (app != null)
            {
                app.launchCount++;
                app.lastUsedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                PersistData();
            }
        }

        private void AddInternalModules()
        {
            _allApps.AddRange(new[]
            {
                AppEntry.CreateInternal("browser", "Browser", new Color(0.2f, 0.5f, 0.9f)),
                AppEntry.CreateInternal("media", "Media Player", new Color(0.9f, 0.3f, 0.5f)),
                AppEntry.CreateInternal("gallery", "Gallery", new Color(0.3f, 0.8f, 0.5f)),
                AppEntry.CreateInternal("files", "Files", new Color(0.6f, 0.6f, 0.7f)),
                AppEntry.CreateInternal("settings", "Settings", new Color(0.5f, 0.5f, 0.6f)),
                AppEntry.CreateInternal("about", "About", new Color(0.4f, 0.4f, 0.5f)),
            });
        }

        // -- Persistence --

        [Serializable] private class PersistedApp
        {
            public string packageName;
            public bool isFavorite;
            public int launchCount;
            public long lastUsedTimestamp;
            public string category;
        }

        [Serializable] private class PersistedRegistry
        {
            public List<PersistedApp> apps = new List<PersistedApp>();
        }

        private void PersistData()
        {
            try
            {
                var data = new PersistedRegistry();
                foreach (var a in _allApps)
                {
                    data.apps.Add(new PersistedApp
                    {
                        packageName = a.packageName,
                        isFavorite = a.isFavorite,
                        launchCount = a.launchCount,
                        lastUsedTimestamp = a.lastUsedTimestamp,
                        category = a.category
                    });
                }
                File.WriteAllText(_persistPath, JsonUtility.ToJson(data, true));
            }
            catch (Exception e) { Debug.LogWarning($"[VisionSpace] Registry persist failed: {e.Message}"); }
        }

        private void RestorePersistedData()
        {
            if (!File.Exists(_persistPath)) return;
            try
            {
                var data = JsonUtility.FromJson<PersistedRegistry>(File.ReadAllText(_persistPath));
                foreach (var p in data.apps)
                {
                    var app = _allApps.Find(a => a.packageName == p.packageName);
                    if (app != null)
                    {
                        app.isFavorite = p.isFavorite;
                        app.launchCount = p.launchCount;
                        app.lastUsedTimestamp = p.lastUsedTimestamp;
                        if (!string.IsNullOrEmpty(p.category)) app.category = p.category;
                    }
                }
            }
            catch (Exception e) { Debug.LogWarning($"[VisionSpace] Registry restore failed: {e.Message}"); }
        }
    }
}
