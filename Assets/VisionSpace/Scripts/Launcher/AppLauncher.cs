// =============================================================================
// AppLauncher.cs — Launch applications via Android intents
// =============================================================================
using System;
using UnityEngine;

namespace VisionSpace.Launcher
{
    /// <summary>
    /// Launches external Android apps via intents or opens internal Vision Space modules.
    /// 
    /// PLATFORM NOTE: Launching an external app puts it in the foreground.
    /// Vision Space goes to background. The user must re-open Vision Space
    /// from Unknown Sources or the dock to return.
    /// </summary>
    public class AppLauncher : MonoBehaviour
    {
        public static AppLauncher Instance { get; private set; }

        public event Action<AppEntry, bool, string> OnLaunchResult;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        /// <summary>
        /// Launch an application entry.
        /// </summary>
        public void Launch(AppEntry entry)
        {
            if (entry == null)
            {
                OnLaunchResult?.Invoke(null, false, "Null app entry.");
                return;
            }

            // Record the launch
            AppRegistry.Instance?.RecordLaunch(entry.packageName);

            // Handle internal modules
            if (entry.source == AppSource.Internal)
            {
                LaunchInternalModule(entry);
                return;
            }

            // Launch external Android app
            LaunchExternalApp(entry);
        }

        private void LaunchInternalModule(AppEntry entry)
        {
            string moduleId = entry.packageName.Replace("visionspace.internal.", "");

            switch (moduleId)
            {
                case "browser":
                    OpenInternalWindow("Browser", WindowManagement.WindowType.Large);
                    break;
                case "media":
                    OpenInternalWindow("Media Player", WindowManagement.WindowType.Large);
                    break;
                case "gallery":
                    OpenInternalWindow("Gallery", WindowManagement.WindowType.Standard);
                    break;
                case "files":
                    OpenInternalWindow("Files", WindowManagement.WindowType.Standard);
                    break;
                case "settings":
                    OpenInternalWindow("Settings", WindowManagement.WindowType.Modal);
                    break;
                case "about":
                    OpenInternalWindow("About Vision Space", WindowManagement.WindowType.Compact);
                    break;
                default:
                    Debug.LogWarning($"[VisionSpace] Unknown internal module: {moduleId}");
                    OnLaunchResult?.Invoke(entry, false, $"Unknown module: {moduleId}");
                    break;
            }
        }

        private void OpenInternalWindow(string title, WindowManagement.WindowType type)
        {
            var windowManager = WindowManagement.WindowManager.Instance;
            if (windowManager != null)
            {
                windowManager.CreateWindow(type, title);
                OnLaunchResult?.Invoke(null, true, $"Opened {title}");
            }
            else
            {
                Debug.LogError("[VisionSpace] WindowManager not available.");
                OnLaunchResult?.Invoke(null, false, "WindowManager not initialized.");
            }
        }

        private void LaunchExternalApp(AppEntry entry)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                AndroidJavaObject pm = activity.Call<AndroidJavaObject>("getPackageManager");

                AndroidJavaObject launchIntent = pm.Call<AndroidJavaObject>(
                    "getLaunchIntentForPackage", entry.packageName);

                if (launchIntent != null)
                {
                    // Add FLAG_ACTIVITY_NEW_TASK
                    int FLAG_ACTIVITY_NEW_TASK = 0x10000000;
                    launchIntent.Call<AndroidJavaObject>("addFlags", FLAG_ACTIVITY_NEW_TASK);

                    activity.Call("startActivity", launchIntent);

                    Debug.Log($"[VisionSpace] Launched: {entry.displayName} ({entry.packageName})");
                    OnLaunchResult?.Invoke(entry, true,
                        $"Launched {entry.displayName}. Vision Space is now in background.");
                }
                else
                {
                    string msg = $"No launch intent found for {entry.packageName}. " +
                                 "The app may not have a launchable activity.";
                    Debug.LogWarning($"[VisionSpace] {msg}");
                    OnLaunchResult?.Invoke(entry, false, msg);
                }
            }
            catch (Exception e)
            {
                string msg = $"Failed to launch {entry.displayName}: {e.Message}";
                Debug.LogError($"[VisionSpace] {msg}");
                OnLaunchResult?.Invoke(entry, false, msg);
            }
#else
            string msg = $"[Editor] Would launch: {entry.displayName} ({entry.packageName}). " +
                         "External app launching only works on Quest hardware.";
            Debug.Log(msg);
            OnLaunchResult?.Invoke(entry, false, msg);
#endif
        }
    }
}
