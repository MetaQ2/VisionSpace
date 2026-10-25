// =============================================================================
// FileViewerController.cs — File browser using Android document picker
// =============================================================================
using UnityEngine;

namespace VisionSpace.Applications
{
    /// <summary>
    /// Simple file viewer that uses Android's document picker intent.
    /// Does NOT assume unrestricted filesystem access.
    /// </summary>
    public class FileViewerController : MonoBehaviour
    {
        [SerializeField] private TMPro.TMP_Text statusText;

        /// <summary>Open Android file picker.</summary>
        public void OpenFilePicker(string mimeType = "*/*")
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

                AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent", "android.intent.action.OPEN_DOCUMENT");
                intent.Call<AndroidJavaObject>("addCategory", "android.intent.category.OPENABLE");
                intent.Call<AndroidJavaObject>("setType", mimeType);

                activity.Call("startActivity", intent);

                SetStatus("File picker opened. Select a file in the system UI.");
                Debug.Log("[VisionSpace] FileViewer: Opened document picker.");
            }
            catch (System.Exception e)
            {
                SetStatus($"Failed to open file picker: {e.Message}");
                Debug.LogError($"[VisionSpace] FileViewer: {e.Message}");
            }
#else
            SetStatus("File picker only available on Quest hardware.");
            Debug.Log("[VisionSpace] FileViewer: Not available in editor.");
#endif
        }

        /// <summary>Open image picker.</summary>
        public void OpenImagePicker() => OpenFilePicker("image/*");

        /// <summary>Open video picker.</summary>
        public void OpenVideoPicker() => OpenFilePicker("video/*");

        /// <summary>Open text file picker.</summary>
        public void OpenTextPicker() => OpenFilePicker("text/*");

        private void SetStatus(string msg)
        {
            if (statusText != null) statusText.text = msg;
        }
    }
}
