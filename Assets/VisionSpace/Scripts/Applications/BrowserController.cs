// =============================================================================
// BrowserController.cs — In-app browser (external launch fallback)
// =============================================================================
using UnityEngine;
using TMPro;

namespace VisionSpace.Applications
{
    /// <summary>
    /// Browser module. Since there is no free embedded browser for Quest 2,
    /// this provides a URL input with external browser launch.
    /// 
    /// For a true embedded browser, integrate Vuplex 3D WebView (commercial)
    /// or build a custom Android WebView AAR plugin.
    /// </summary>
    public class BrowserController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_InputField urlInput;
        [SerializeField] private TMP_Text statusText;

        private string _currentUrl = "https://www.google.com";
        private string[] _bookmarks = {
            "https://www.google.com",
            "https://www.wikipedia.org",
            "https://www.github.com",
        };

        public void NavigateToUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return;
            if (!url.StartsWith("http")) url = "https://" + url;

            _currentUrl = url;

            // Launch in external system browser
            // NOTE: This opens the Meta Quest Browser or Wolvic, putting
            // Vision Space in the background.
            Application.OpenURL(url);

            if (statusText != null)
                statusText.text = $"Opened in system browser: {url}\n" +
                                  "Vision Space is now in background.";

            Debug.Log($"[VisionSpace] Browser: Opened {url} in system browser.");
        }

        public void NavigateToBookmark(int index)
        {
            if (index >= 0 && index < _bookmarks.Length)
                NavigateToUrl(_bookmarks[index]);
        }

        public void SetUrl(string url) { _currentUrl = url; }
        public string GetCurrentUrl() => _currentUrl;
    }
}
