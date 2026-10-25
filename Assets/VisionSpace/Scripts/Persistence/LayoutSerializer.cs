// =============================================================================
// LayoutSerializer.cs — Save/restore window layouts to JSON
// =============================================================================
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VisionSpace.Persistence
{
    public static class LayoutSerializer
    {
        private static string LayoutDir =>
            Path.Combine(Application.persistentDataPath, "layouts");

        [Serializable]
        public class SavedLayout
        {
            public string name;
            public long timestamp;
            public List<WindowData> windows = new List<WindowData>();
        }

        [Serializable]
        public class WindowData
        {
            public string title;
            public string type;
            public float[] position = new float[3];
            public float[] rotation = new float[4];
            public float[] scale = new float[3];
        }

        public static void SaveCurrentLayout(string name)
        {
            var wm = WindowManagement.WindowManager.Instance;
            if (wm == null) return;

            var layout = new SavedLayout
            {
                name = name,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            foreach (var w in wm.Windows)
            {
                var wd = new WindowData
                {
                    title = w.Title,
                    type = w.Type.ToString(),
                    position = new[] { w.transform.position.x, w.transform.position.y, w.transform.position.z },
                    rotation = new[] { w.transform.rotation.x, w.transform.rotation.y, w.transform.rotation.z, w.transform.rotation.w },
                    scale = new[] { w.transform.localScale.x, w.transform.localScale.y, w.transform.localScale.z }
                };
                layout.windows.Add(wd);
            }

            Directory.CreateDirectory(LayoutDir);
            string path = Path.Combine(LayoutDir, $"{name}.json");
            File.WriteAllText(path, JsonUtility.ToJson(layout, true));
            Debug.Log($"[VisionSpace] Layout saved: {name}");
        }

        public static SavedLayout LoadLayout(string name)
        {
            string path = Path.Combine(LayoutDir, $"{name}.json");
            if (!File.Exists(path)) return null;

            try
            {
                return JsonUtility.FromJson<SavedLayout>(File.ReadAllText(path));
            }
            catch (Exception e)
            {
                Debug.LogError($"[VisionSpace] Layout load failed: {e.Message}");
                return null;
            }
        }

        public static string[] ListSavedLayouts()
        {
            if (!Directory.Exists(LayoutDir)) return Array.Empty<string>();
            var files = Directory.GetFiles(LayoutDir, "*.json");
            var names = new string[files.Length];
            for (int i = 0; i < files.Length; i++)
                names[i] = Path.GetFileNameWithoutExtension(files[i]);
            return names;
        }

        public static void DeleteLayout(string name)
        {
            string path = Path.Combine(LayoutDir, $"{name}.json");
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
