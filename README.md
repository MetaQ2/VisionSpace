# VISION SPACE

**Spatial Computing Interface for Meta Quest 2**

An Apple Vision Pro–inspired immersive spatial launcher built with Unity for Meta Quest 2.

---

## ⚡ Overview

Vision Space recreates the visual language and interaction philosophy of Apple Vision Pro as a standalone VR application for Meta Quest 2. It provides:

- **Frosted glass spatial UI** — translucent panels with edge highlights, gradients, and depth
- **Floating app grid** — discover and launch installed apps with animated icons
- **Spatial window manager** — create, move, resize, minimize floating windows
- **Vision Pro–style dock** — quick-access shortcuts that follow your view
- **Control center** — environment switching, quality settings, recenter
- **Multiple environments** — Dark Studio, Bright Room, Futuristic
- **Hand tracking + controller support** — pinch to select, grip to drag
- **Built-in modules** — Browser (external), Media Player, Image Gallery, Files, Settings
- **Persistent settings and layouts** — JSON serialization

---

## 🏗 Architecture

```
Option A: Standalone Immersive VR Application ✅ (Selected)
```

Vision Space runs as a normal sideloaded VR app. It **cannot** replace the Horizon OS system launcher on a stock Quest 2 — this is a hardware/OS limitation, not a bug.

### What Works
| Feature | Status |
|---|---|
| Immersive VR environment | ✅ Full |
| Floating glass UI panels | ✅ Full |
| App discovery (PackageManager) | ✅ Requires `<queries>` manifest |
| App launching (intents) | ✅ Launched app takes foreground |
| Controller input | ✅ Full |
| Hand tracking | ✅ Supported (limited FOV) |
| Window management | ✅ Internal windows only |
| Settings persistence | ✅ JSON to persistentDataPath |

### Known Platform Limitations
| Feature | Limitation |
|---|---|
| System launcher replacement | ❌ Impossible on stock device |
| Drawing over other apps | ❌ No SYSTEM_ALERT_WINDOW permission |
| Third-party apps as windows | ❌ No windowed multitasking API |
| Eye tracking | ❌ Quest 2 has no eye tracking hardware |
| Color passthrough | ❌ Quest 2 is monochrome only |
| Embedded browser | ❌ No free solution (Vuplex is commercial) |
| System volume control | ❌ Restricted API |

---

## 🛠 Build Requirements

### Software
| Component | Required Version |
|---|---|
| **Unity** | Unity 6 LTS (6000.0.x) or Unity 2022.3 LTS |
| **Android Build Support** | Install via Unity Hub (includes SDK, NDK, JDK) |
| **Meta XR All-in-One SDK** | Latest from Unity Asset Store |
| **TextMeshPro** | Included with Unity |
| **Render Pipeline** | URP (Universal Render Pipeline) |

### Hardware
| Item | Requirement |
|---|---|
| **Meta Quest 2** | Developer Mode enabled |
| **USB-C cable** | For ADB deployment |
| **Computer** | macOS or Windows |

---

## 🚀 Setup Guide

### 1. Install Unity

```bash
# Install Unity Hub from https://unity.com/download
# In Unity Hub: Install Unity 6 LTS with these modules:
#   ✅ Android Build Support
#   ✅ Android SDK & NDK Tools
#   ✅ OpenJDK
```

### 2. Create Unity Project

```bash
# Open Unity Hub → New Project
# Template: Universal 3D (URP)
# Project Name: VisionSpace
# Location: /path/to/Meta/launcher/
```

### 3. Import Source Files

Copy the entire `Assets/VisionSpace/` directory from this repository into your Unity project's `Assets/` folder.

### 4. Install Meta XR SDK

1. Open Unity → Window → Package Manager
2. Click "+" → "Add package by name"
3. Enter: `com.meta.xr.sdk.all`
4. Or: Unity Asset Store → Search "Meta XR All-in-One SDK" → Import

### 5. Configure Project Settings

#### XR Plug-in Management
```
Edit → Project Settings → XR Plug-in Management
  → Android tab:
    ✅ OpenXR
    → OpenXR → Meta Quest Support Feature Group: ✅ Enabled
```

#### Player Settings
```
Edit → Project Settings → Player → Android:
  Company Name: VisionSpace
  Product Name: Vision Space
  Package Name: com.visionspace.launcher
  Minimum API Level: Android 10.0 (API 29)
  Target API Level: Android 12 (API 32)
  Scripting Backend: IL2CPP
  Target Architectures: ✅ ARM64
  Graphics APIs: Vulkan (primary)
```

#### Quality / URP
```
Edit → Project Settings → Quality:
  Active Quality Level: Medium
  Anti Aliasing: 4x
  VSync: Don't Sync (we set targetFrameRate = 90)
```

### 6. Create Scenes

Create two scenes in `Assets/VisionSpace/Scenes/`:

**Boot.unity:**
1. Create empty GameObject "Bootstrap"
2. Add `VisionSpaceBootstrap` component
3. Add `SettingsManager` component

**Home.unity:**
1. Create empty GameObject "HomeController"
2. Add `HomeSceneController` component
3. Add XR Origin (from Meta XR SDK building blocks)
4. Add Directional Light
5. Add `PerformanceMonitor` to a child object

**Build Settings:**
```
File → Build Settings:
  Scene 0: Boot
  Scene 1: Home
  Platform: Android
```

### 7. Run Meta Project Setup Tool

```
Oculus → Tools → Project Setup Tool
→ Fix All (resolves manifest, rendering, and plugin issues)
```

### 8. Build APK

```
File → Build Settings → Build
Output: VisionSpace.apk
```

### 9. Install on Quest 2

```bash
# Connect Quest 2 via USB
adb devices  # Verify connection

# Install
adb install -r VisionSpace.apk

# Launch: Unknown Sources → Vision Space
```

---

## 📁 Project Structure

```
Assets/VisionSpace/
├── Art/
│   ├── Shaders/
│   │   ├── GlassPanel.shader      # Frosted glass (URP + fallback)
│   │   └── RoundedRect.shader     # Rounded icon mask
│   ├── Materials/
│   ├── Models/
│   ├── Icons/
│   ├── Environments/
│   └── Fonts/
├── Plugins/Android/
│   └── AndroidManifest.xml        # Package visibility + VR metadata
├── Scripts/
│   ├── Core/
│   │   ├── VisionSpaceBootstrap.cs
│   │   └── HomeSceneController.cs
│   ├── Input/
│   │   ├── IInputProvider.cs
│   │   ├── InputManager.cs
│   │   ├── ControllerInputProvider.cs
│   │   ├── HandTrackingInputProvider.cs
│   │   └── HeadGazeInputProvider.cs
│   ├── WindowManagement/
│   │   ├── WindowManager.cs
│   │   ├── WindowController.cs
│   │   └── WindowLayoutPreset.cs
│   ├── Launcher/
│   │   ├── AppEntry.cs
│   │   ├── AppRegistry.cs
│   │   ├── AppDiscovery.cs
│   │   ├── AppLauncher.cs
│   │   ├── AppGridController.cs
│   │   ├── AppIconController.cs
│   │   └── SearchController.cs
│   ├── Environment/
│   │   └── EnvironmentManager.cs
│   ├── UI/
│   │   ├── DockController.cs
│   │   ├── DockIconController.cs
│   │   ├── ControlCenterController.cs
│   │   └── GlassPanelAnimator.cs
│   ├── Applications/
│   │   ├── BrowserController.cs
│   │   ├── MediaViewerController.cs
│   │   ├── ImageGalleryController.cs
│   │   ├── FileViewerController.cs
│   │   └── AboutPanelController.cs
│   ├── Settings/
│   │   ├── SettingsManager.cs
│   │   └── ThemeManager.cs
│   ├── Performance/
│   │   └── PerformanceMonitor.cs
│   └── Persistence/
│       └── LayoutSerializer.cs
├── Tests/
│   └── Editor/
│       ├── AppRegistryTests.cs
│       ├── WindowManagerTests.cs
│       └── LayoutSerializerTests.cs
├── Scenes/
│   ├── Boot.unity
│   └── Home.unity
└── Resources/
```

---

## 🎮 Controls

### Quest Touch Controllers
| Input | Action |
|---|---|
| Trigger (index finger) | Select / click |
| Grip | Grab and drag windows |
| Thumbstick | Scroll / navigate pages |
| B button | Toggle control center |

### Hand Tracking
| Gesture | Action |
|---|---|
| Point | Aim ray |
| Pinch (index + thumb) | Select |
| Grab (full hand close) | Grab and drag window |

---

## 🧪 Testing Checklist

### Quest 2 Hardware Test
- [ ] Clean install via `adb install`
- [ ] First launch → Boot scene → Home transition
- [ ] Environment renders (Dark Studio default)
- [ ] App grid shows discovered apps
- [ ] Controller ray + trigger selection works
- [ ] Icon hover animation plays
- [ ] App icon selection launches external app
- [ ] Internal module (Settings/About) opens window
- [ ] Dock follows user view
- [ ] Dock icon selection triggers actions
- [ ] Window drag via grip
- [ ] Window close
- [ ] Control center toggle (B button)
- [ ] Environment switching
- [ ] Recenter interface
- [ ] Hand tracking (if enabled in Quest settings)
- [ ] FPS counter toggle
- [ ] Settings persist after restart
- [ ] Performance: stable refresh rate
- [ ] 30+ minute session stability

### Profiling
```
# Unity Profiler via USB:
1. Build with Development Build + Autoconnect Profiler
2. Window → Analysis → Profiler
3. Connect to Quest device
4. Monitor: Frame Time, Draw Calls, GC Alloc

# OVR Metrics Tool:
adb shell setprop debug.oculus.gpuLevel 3
adb shell am start -n com.oculus.ovrmonitormetricsservice/.MainActivity
```

---

## ⚖️ License

MIT License. Created for the MetaQ2 organization.

**Vision Space** is an original implementation inspired by Apple Vision Pro's publicly available design language. It does not use Apple's proprietary source code, assets, trademarks, or system software.
