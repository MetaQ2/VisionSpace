// =============================================================================
// DockIconController.cs — Individual dock icon interaction
// =============================================================================
using UnityEngine;

namespace VisionSpace.UI
{
    public class DockIconController : MonoBehaviour
    {
        private DockItem _item;
        private float _baseScale;
        private Vector3 _originalScale;
        private bool _isHovered;
        private float _hoverProgress;
        private Renderer _renderer;
        private Color _baseColor;

        public void Setup(DockItem item, float size)
        {
            _item = item;
            _baseScale = size;
            _originalScale = Vector3.one * size;
            _renderer = GetComponent<Renderer>();
            if (_renderer != null) _baseColor = _renderer.material.color;
        }

        private void Update()
        {
            float target = _isHovered ? 1f : 0f;
            _hoverProgress = Mathf.Lerp(_hoverProgress, target, Time.deltaTime * 10f);

            float scale = Mathf.Lerp(1f, 1.25f, _hoverProgress);
            transform.localScale = _originalScale * scale;

            // Brighten on hover
            if (_renderer != null)
            {
                Color c = Color.Lerp(_baseColor, Color.white, _hoverProgress * 0.3f);
                _renderer.material.color = c;
            }
        }

        public void OnHoverEnter() { _isHovered = true; DockController.Instance?.ShowDock(); }
        public void OnHoverExit() { _isHovered = false; }

        public void OnSelect()
        {
            if (_item == null) return;
            Debug.Log($"[VisionSpace] Dock: {_item.label}");
            ExecuteAction(_item.action);
        }

        private void ExecuteAction(DockAction action)
        {
            switch (action)
            {
                case DockAction.GoHome:
                    WindowManagement.WindowManager.Instance?.RecenterAllWindows();
                    break;
                case DockAction.ToggleAppGrid:
                    var grid = FindObjectOfType<Launcher.AppGridController>();
                    if (grid != null) grid.SetVisible(!grid.gameObject.activeSelf);
                    break;
                case DockAction.OpenBrowser:
                    Launcher.AppLauncher.Instance?.Launch(
                        Launcher.AppEntry.CreateInternal("browser", "Browser",
                            new Color(0.2f, 0.5f, 0.9f)));
                    break;
                case DockAction.OpenMedia:
                    Launcher.AppLauncher.Instance?.Launch(
                        Launcher.AppEntry.CreateInternal("media", "Media Player",
                            new Color(0.9f, 0.3f, 0.5f)));
                    break;
                case DockAction.OpenFiles:
                    Launcher.AppLauncher.Instance?.Launch(
                        Launcher.AppEntry.CreateInternal("files", "Files",
                            new Color(0.6f, 0.6f, 0.7f)));
                    break;
                case DockAction.OpenSettings:
                    Launcher.AppLauncher.Instance?.Launch(
                        Launcher.AppEntry.CreateInternal("settings", "Settings",
                            new Color(0.5f, 0.5f, 0.6f)));
                    break;
                case DockAction.ToggleControlCenter:
                    var cc = FindObjectOfType<ControlCenterController>();
                    if (cc != null) cc.Toggle();
                    break;
            }
        }
    }
}
