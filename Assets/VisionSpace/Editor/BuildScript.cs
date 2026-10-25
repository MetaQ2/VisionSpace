// =============================================================================
// BuildScript.cs — Command-line build automation for Vision Space
// =============================================================================
#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace VisionSpace.Editor
{
    public static class BuildScript
    {
        private static readonly string[] Scenes = new[]
        {
            "Assets/VisionSpace/Scenes/Boot.unity",
            "Assets/VisionSpace/Scenes/Home.unity"
        };

        public static void BuildAndroid()
        {
            Debug.Log("[VisionSpace Build] Starting Android build...");

            // Pre-load OpenXR package settings BEFORE BuildPlayer starts
            PreloadOpenXRSettings();

            string outputDir = Path.Combine(Directory.GetCurrentDirectory(), "build");
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            string apkPath = Path.Combine(outputDir, "VisionSpace.apk");

            PlayerSettings.productName = "Vision Space";
            PlayerSettings.companyName = "MetaQ2";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.metaq2.visionspace");
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.Android.bundleVersionCode = 1;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel32;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            BuildPlayerOptions buildOptions = new BuildPlayerOptions
            {
                scenes = Scenes,
                locationPathName = apkPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(buildOptions);
            var summary = report.summary;

            if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.Log($"[VisionSpace Build] SUCCESS! Output: {apkPath} ({summary.totalSize} bytes)");
                EditorApplication.Exit(0);
            }
            else
            {
                Debug.LogError($"[VisionSpace Build] FAILED with {summary.totalErrors} errors.");
                EditorApplication.Exit(1);
            }
        }

        private static void PreloadOpenXRSettings()
        {
            try
            {
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    var type = asm.GetType("UnityEditor.XR.OpenXR.OpenXRPackageSettings");
                    if (type != null)
                    {
                        var prop = type.GetProperty("Instance", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                        var inst = prop?.GetValue(null);
                        if (inst == null)
                        {
                            var method = type.GetMethod("GetOrCreateInstance", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                            inst = method?.Invoke(null, null);
                        }

                        if (inst != null)
                        {
                            EditorBuildSettings.AddConfigObject("com.unity.xr.openxr.settings4", (UnityEngine.Object)inst, true);
                            Debug.Log("[VisionSpace Build] Preloaded OpenXR Settings successfully.");
                        }
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[VisionSpace Build] OpenXR preload notice: {ex.Message}");
            }
        }
    }
}
#endif
