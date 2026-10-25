// =============================================================================
// AboutPanelController.cs — About / credits panel
// =============================================================================
using UnityEngine;
using TMPro;

namespace VisionSpace.Applications
{
    public class AboutPanelController : MonoBehaviour
    {
        [SerializeField] private TMP_Text aboutText;

        private void Start()
        {
            if (aboutText == null)
            {
                var obj = new GameObject("AboutText");
                obj.transform.SetParent(transform);
                obj.transform.localPosition = Vector3.zero;
                aboutText = obj.AddComponent<TextMeshPro>();
            }

            aboutText.text =
                "<size=4><b>VISION SPACE</b></size>\n" +
                "<size=2.5>Spatial Computing Interface for Meta Quest 2</size>\n\n" +
                $"<size=2>Version: {Application.version}\n" +
                $"Unity: {Application.unityVersion}\n" +
                $"Platform: {Application.platform}\n" +
                $"Device: {SystemInfo.deviceModel}\n" +
                $"GPU: {SystemInfo.graphicsDeviceName}\n" +
                $"RAM: {SystemInfo.systemMemorySize} MB</size>\n\n" +
                "<size=2><b>Architecture</b>\n" +
                "• Standalone VR application (not a system launcher)\n" +
                "• Cannot replace Horizon OS Home\n" +
                "• External app launches put Vision Space in background\n" +
                "• Hand tracking: supported (limited FOV)\n" +
                "• Eye tracking: not available on Quest 2\n" +
                "• Passthrough: monochrome only on Quest 2\n" +
                "• Browser: opens in system browser (no embedded WebView)</size>\n\n" +
                "<size=2><b>Inspired by</b> Apple Vision Pro's spatial design language.\n" +
                "Original implementation. No Apple code or assets used.</size>\n\n" +
                "<size=1.5>MIT License</size>";

            aboutText.fontSize = 2f;
            aboutText.alignment = TextAlignmentOptions.Center;
            aboutText.color = new Color(0.85f, 0.85f, 0.9f);
            aboutText.enableWordWrapping = true;
            aboutText.rectTransform.sizeDelta = new Vector2(8f, 12f);
        }
    }
}
