// =============================================================================
// BuildScript.cs — Command-line build automation for Vision Space
// =============================================================================
#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEditor.XR.OpenXR.Features;

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

            // 1. Configure scenes first
            SetupScenes();

            // 2. Configure XR Management & OpenXR loaders
            ConfigureXR();

            // 3. Preload OpenXR package settings for batchmode
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
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.stripEngineCode = false;
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

        private static void ConfigureXR()
        {
            try
            {
                Debug.Log("[VisionSpace Build] Configuring XR settings for Android...");

                XRGeneralSettingsPerBuildTarget buildTargetSettings = null;
                if (!EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out buildTargetSettings) || buildTargetSettings == null)
                {
                    var assets = AssetDatabase.FindAssets("t:XRGeneralSettingsPerBuildTarget");
                    if (assets.Length > 0)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(assets[0]);
                        buildTargetSettings = AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(path);
                    }
                }

                if (buildTargetSettings == null)
                {
                    Debug.LogError("[VisionSpace Build] Could not find XRGeneralSettingsPerBuildTarget!");
                    return;
                }

                EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, buildTargetSettings, true);

                if (!buildTargetSettings.HasSettingsForBuildTarget(BuildTargetGroup.Android))
                {
                    buildTargetSettings.CreateDefaultSettingsForBuildTarget(BuildTargetGroup.Android);
                }

                if (!buildTargetSettings.HasManagerSettingsForBuildTarget(BuildTargetGroup.Android))
                {
                    buildTargetSettings.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.Android);
                }

                var generalSettings = buildTargetSettings.SettingsForBuildTarget(BuildTargetGroup.Android);
                if (generalSettings != null)
                {
                    generalSettings.InitManagerOnStart = true;
                    EditorUtility.SetDirty(generalSettings);
                }

                var manager = buildTargetSettings.ManagerSettingsForBuildTarget(BuildTargetGroup.Android);
                if (manager != null)
                {
                    manager.automaticLoading = true;
                    manager.automaticRunning = true;

                    string openXRLoaderName = typeof(UnityEngine.XR.OpenXR.OpenXRLoader).FullName;
                    if (!XRPackageMetadataStore.IsLoaderAssigned(openXRLoaderName, BuildTargetGroup.Android))
                    {
                        bool assigned = XRPackageMetadataStore.AssignLoader(manager, openXRLoaderName, BuildTargetGroup.Android);
                        Debug.Log($"[VisionSpace Build] AssignLoader {openXRLoaderName} result: {assigned}");
                    }
                    EditorUtility.SetDirty(manager);
                }

                EditorUtility.SetDirty(buildTargetSettings);

                // Configure OpenXR Features for Android
                var openXRSettings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
                if (openXRSettings != null)
                {
                    openXRSettings.renderMode = OpenXRSettings.RenderMode.MultiPass;
                    EditorUtility.SetDirty(openXRSettings);
                }

                // Disable deprecated Oculus Quest feature
                var deprecatedOculusQuest = FeatureHelpers.GetFeatureWithIdForBuildTarget(BuildTargetGroup.Android, "com.unity.openxr.feature.oculusquest");
                if (deprecatedOculusQuest != null)
                {
                    deprecatedOculusQuest.enabled = false;
                    EditorUtility.SetDirty(deprecatedOculusQuest);
                    Debug.Log("[VisionSpace Build] Disabled deprecated feature: com.unity.openxr.feature.oculusquest");
                }

                string[] targetFeatureIds = new[]
                {
                    "com.unity.openxr.feature.metaquest",
                    "com.unity.openxr.feature.input.oculustouch",
                    "com.unity.openxr.feature.input.handinteraction",
                    "com.unity.openxr.feature.input.handtracking",
                    "com.unity.openxr.feature.input.palmpose",
                    "com.unity.openxr.feature.input.metaquestplus"
                };

                foreach (var id in targetFeatureIds)
                {
                    var feat = FeatureHelpers.GetFeatureWithIdForBuildTarget(BuildTargetGroup.Android, id);
                    if (feat != null)
                    {
                        feat.enabled = true;
                        EditorUtility.SetDirty(feat);
                        Debug.Log($"[VisionSpace Build] Enabled feature: {id}");
                    }
                }

                AssetDatabase.SaveAssets();
                Debug.Log("[VisionSpace Build] XR settings configured successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[VisionSpace Build] Error configuring XR: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private static void SetupScenes()
        {
            try
            {
                Debug.Log("[VisionSpace Build] Verifying and setting up scenes...");

                // --- 1. BOOT SCENE ---
                string bootScenePath = "Assets/VisionSpace/Scenes/Boot.unity";
                var bootScene = EditorSceneManager.OpenScene(bootScenePath, OpenSceneMode.Single);

                var mainCamObj = GameObject.FindWithTag("MainCamera") ?? GameObject.Find("Main Camera");
                if (mainCamObj != null)
                {
                    var cam = mainCamObj.GetComponent<Camera>();
                    if (cam != null)
                    {
                        cam.clearFlags = CameraClearFlags.SolidColor;
                        cam.backgroundColor = new Color(0.04f, 0.05f, 0.09f, 1f);
                    }
                }

                var bootObj = GameObject.Find("Bootstrap");
                if (bootObj == null)
                {
                    bootObj = new GameObject("Bootstrap");
                }
                if (bootObj.GetComponent<VisionSpace.Core.VisionSpaceBootstrap>() == null)
                {
                    bootObj.AddComponent<VisionSpace.Core.VisionSpaceBootstrap>();
                    Debug.Log("[VisionSpace Build] Added VisionSpaceBootstrap to Boot scene.");
                }

                if (GameObject.Find("SplashCard") == null)
                {
                    var splash = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    splash.name = "SplashCard";
                    splash.transform.position = new Vector3(0, 0, 1.5f);
                    splash.transform.localScale = new Vector3(0.7f, 0.4f, 1f);
                    var splashRenderer = splash.GetComponent<Renderer>();
                    var mat = new Material(Shader.Find("Sprites/Default"));
                    mat.color = new Color(0.1f, 0.12f, 0.22f, 0.95f);
                    splashRenderer.material = mat;
                    var col = splash.GetComponent<Collider>();
                    if (col != null) UnityEngine.Object.DestroyImmediate(col);

                    var textObj = new GameObject("SplashText");
                    textObj.transform.SetParent(splash.transform);
                    textObj.transform.localPosition = new Vector3(0, 0, -0.01f);
                    textObj.transform.localScale = Vector3.one * 0.035f;
                    var textMesh = textObj.AddComponent<TextMesh>();
                    textMesh.text = "VISION SPACE\nStarting spatial desktop...";
                    textMesh.alignment = TextAlignment.Center;
                    textMesh.anchor = TextAnchor.MiddleCenter;
                    textMesh.fontSize = 28;
                    textMesh.color = Color.white;
                }

                EditorSceneManager.MarkSceneDirty(bootScene);
                EditorSceneManager.SaveScene(bootScene);

                // --- 2. HOME SCENE ---
                string homeScenePath = "Assets/VisionSpace/Scenes/Home.unity";
                var homeScene = EditorSceneManager.OpenScene(homeScenePath, OpenSceneMode.Single);

                var homeCamObj = GameObject.FindWithTag("MainCamera") ?? GameObject.Find("Main Camera");
                if (homeCamObj != null)
                {
                    var cam = homeCamObj.GetComponent<Camera>();
                    if (cam != null)
                    {
                        cam.clearFlags = CameraClearFlags.SolidColor;
                        cam.backgroundColor = new Color(0.04f, 0.05f, 0.08f, 1f);
                    }
                }

                var lightObj = GameObject.Find("Directional Light");
                if (lightObj == null)
                {
                    lightObj = new GameObject("Directional Light");
                    var light = lightObj.AddComponent<Light>();
                    light.type = LightType.Directional;
                    light.color = new Color(0.95f, 0.95f, 1f);
                    light.intensity = 1.0f;
                    lightObj.transform.rotation = Quaternion.Euler(50, -30, 0);
                }

                var homeCtrlObj = GameObject.Find("HomeSceneController");
                if (homeCtrlObj == null)
                {
                    homeCtrlObj = new GameObject("HomeSceneController");
                }
                if (homeCtrlObj.GetComponent<VisionSpace.Core.HomeSceneController>() == null)
                {
                    homeCtrlObj.AddComponent<VisionSpace.Core.HomeSceneController>();
                    Debug.Log("[VisionSpace Build] Added HomeSceneController to Home scene.");
                }

                EditorSceneManager.MarkSceneDirty(homeScene);
                EditorSceneManager.SaveScene(homeScene);

                Debug.Log("[VisionSpace Build] Scenes verified and saved.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[VisionSpace Build] Error setting up scenes: {ex.Message}\n{ex.StackTrace}");
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
