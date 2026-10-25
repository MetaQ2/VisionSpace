// =============================================================================
// AppDiscovery.cs — Android PackageManager bridge for app discovery
// =============================================================================
using System;
using System.Collections.Generic;
using UnityEngine;

namespace VisionSpace.Launcher
{
    /// <summary>
    /// Discovers installed applications on the Quest 2 using Android's PackageManager.
    /// Requires <queries> declarations in AndroidManifest.xml for Android 11+ visibility.
    /// 
    /// PLATFORM NOTE: This only works on actual Android/Quest hardware.
    /// In the Unity editor, returns demo data for development.
    /// </summary>
    public static class AppDiscovery
    {
        /// <summary>
        /// Query installed launchable applications from the Android system.
        /// </summary>
        public static List<AppEntry> DiscoverInstalledApps()
        {
            var apps = new List<AppEntry>();

#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                apps = QueryAndroidPackages();
            }
            catch (Exception e)
            {
                Debug.LogError($"[VisionSpace] AppDiscovery failed: {e.Message}");
                Debug.LogWarning("[VisionSpace] Falling back to empty list. " +
                    "Ensure <queries> is declared in AndroidManifest.xml.");
            }
#else
            Debug.Log("[VisionSpace] AppDiscovery: Not running on Android. " +
                      "Returning demo data for editor testing.");
            apps = GetEditorDemoApps();
#endif

            Debug.Log($"[VisionSpace] AppDiscovery: Found {apps.Count} apps.");
            return apps;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static List<AppEntry> QueryAndroidPackages()
        {
            var results = new List<AppEntry>();

            // Get UnityPlayer.currentActivity
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaObject packageManager = currentActivity.Call<AndroidJavaObject>("getPackageManager");

            // Create Intent for ACTION_MAIN + CATEGORY_LAUNCHER
            AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent", "android.intent.action.MAIN");
            intent.Call<AndroidJavaObject>("addCategory", "android.intent.category.LAUNCHER");

            // Query resolve info list
            AndroidJavaObject resolveInfoList = packageManager.Call<AndroidJavaObject>(
                "queryIntentActivities", intent, 0);

            if (resolveInfoList == null)
            {
                Debug.LogWarning("[VisionSpace] queryIntentActivities returned null.");
                return results;
            }

            int count = resolveInfoList.Call<int>("size");
            string ownPackage = currentActivity.Call<string>("getPackageName");

            for (int i = 0; i < count; i++)
            {
                try
                {
                    AndroidJavaObject resolveInfo = resolveInfoList.Call<AndroidJavaObject>("get", i);
                    AndroidJavaObject activityInfo = resolveInfo.Get<AndroidJavaObject>("activityInfo");

                    string pkgName = activityInfo.Get<string>("packageName");

                    // Skip our own package
                    if (pkgName == ownPackage) continue;

                    // Skip known system packages that aren't useful to show
                    if (IsSystemSkipPackage(pkgName)) continue;

                    string actName = activityInfo.Get<string>("name");

                    // Get display label
                    AndroidJavaObject labelObj = resolveInfo.Call<AndroidJavaObject>("loadLabel", packageManager);
                    string label = labelObj?.Call<string>("toString") ?? pkgName;

                    var entry = new AppEntry
                    {
                        packageName = pkgName,
                        displayName = label,
                        activityName = actName,
                        source = AppSource.Discovered,
                        isVRApp = IsVRPackage(pkgName),
                        isSideloaded = IsSideloadedPackage(packageManager, pkgName)
                    };

                    // Try to extract app icon as Texture2D
                    entry.icon = ExtractAppIcon(packageManager, pkgName);

                    results.Add(entry);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[VisionSpace] Failed to process app at index {i}: {e.Message}");
                }
            }

            return results;
        }

        private static Texture2D ExtractAppIcon(AndroidJavaObject packageManager, string packageName)
        {
            try
            {
                // Get the app icon as a Drawable
                AndroidJavaObject appInfo = packageManager.Call<AndroidJavaObject>(
                    "getApplicationInfo", packageName, 0);
                AndroidJavaObject drawable = packageManager.Call<AndroidJavaObject>(
                    "getApplicationIcon", appInfo);

                if (drawable == null) return null;

                // Convert Drawable to Bitmap
                // Get intrinsic dimensions
                int width = drawable.Call<int>("getIntrinsicWidth");
                int height = drawable.Call<int>("getIntrinsicHeight");

                if (width <= 0 || height <= 0)
                {
                    width = 128;
                    height = 128;
                }

                // Cap icon size for performance
                width = Mathf.Min(width, 128);
                height = Mathf.Min(height, 128);

                // Create Bitmap
                AndroidJavaClass bitmapClass = new AndroidJavaClass("android.graphics.Bitmap");
                AndroidJavaClass configClass = new AndroidJavaClass("android.graphics.Bitmap$Config");
                AndroidJavaObject config = configClass.GetStatic<AndroidJavaObject>("ARGB_8888");
                AndroidJavaObject bitmap = bitmapClass.CallStatic<AndroidJavaObject>(
                    "createBitmap", width, height, config);

                // Create Canvas and draw
                AndroidJavaObject canvas = new AndroidJavaObject("android.graphics.Canvas", bitmap);
                drawable.Call("setBounds", 0, 0, width, height);
                drawable.Call("draw", canvas);

                // Read pixels
                int[] pixels = new int[width * height];
                bitmap.Call("getPixels", pixels, 0, width, 0, 0, width, height);

                // Convert to Unity Texture2D
                Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
                Color32[] colors = new Color32[pixels.Length];

                for (int i = 0; i < pixels.Length; i++)
                {
                    int pixel = pixels[i];
                    // Android ARGB to Unity RGBA
                    byte a = (byte)((pixel >> 24) & 0xFF);
                    byte r = (byte)((pixel >> 16) & 0xFF);
                    byte g = (byte)((pixel >> 8) & 0xFF);
                    byte b = (byte)(pixel & 0xFF);

                    // Flip vertically (Android and Unity have different Y origins)
                    int flippedIndex = (height - 1 - (i / width)) * width + (i % width);
                    colors[flippedIndex] = new Color32(r, g, b, a);
                }

                tex.SetPixels32(colors);
                tex.Apply();

                bitmap.Call("recycle");
                return tex;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[VisionSpace] Failed to extract icon for {packageName}: {e.Message}");
                return null;
            }
        }

        private static bool IsVRPackage(string packageName)
        {
            return packageName.Contains("oculus") ||
                   packageName.Contains("meta.") ||
                   packageName.Contains(".vr.") ||
                   packageName.Contains("sidequest");
        }

        private static bool IsSideloadedPackage(AndroidJavaObject packageManager, string packageName)
        {
            // Heuristic: if the installer is not the Meta Store, it's likely sideloaded
            try
            {
                string installer = packageManager.Call<string>("getInstallerPackageName", packageName);
                return string.IsNullOrEmpty(installer) ||
                       (!installer.Contains("oculus") && !installer.Contains("meta"));
            }
            catch
            {
                return true; // Assume sideloaded if we can't determine
            }
        }

        private static bool IsSystemSkipPackage(string packageName)
        {
            // Skip known non-useful system packages
            string[] skipPrefixes = {
                "com.android.providers",
                "com.android.inputmethod",
                "com.android.settings",
                "com.android.systemui",
                "com.android.shell",
                "com.android.bluetooth",
                "com.android.nfc",
                "com.android.phone",
                "com.android.server",
                "com.android.keychain",
                "com.android.certinstaller",
                "com.android.packageinstaller",
                "android",
            };

            foreach (var prefix in skipPrefixes)
            {
                if (packageName.StartsWith(prefix)) return true;
            }
            return false;
        }
#endif

        /// <summary>
        /// Demo data for Unity Editor testing.
        /// These are NOT real apps and are clearly marked as demo entries.
        /// </summary>
        private static List<AppEntry> GetEditorDemoApps()
        {
            return new List<AppEntry>
            {
                AppEntry.CreateManual("com.meta.browser", "Meta Browser"),
                AppEntry.CreateManual("com.oculus.vrshell", "Horizon Home"),
                AppEntry.CreateManual("com.beatgames.beatsaber", "Beat Saber"),
                AppEntry.CreateManual("com.resolutiongames.demeo", "Demeo"),
                AppEntry.CreateManual("com.wolvic.browser", "Wolvic Browser"),
                AppEntry.CreateManual("com.superhotteam.superhot.vr", "SUPERHOT VR"),
                AppEntry.CreateManual("com.brave.browser", "Brave Browser"),
                AppEntry.CreateManual("com.netflix.mediaclient", "Netflix"),
            };
        }
    }
}
