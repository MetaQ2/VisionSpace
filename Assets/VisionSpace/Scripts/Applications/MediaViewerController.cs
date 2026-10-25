// =============================================================================
// MediaViewerController.cs — Media playback in a floating window
// =============================================================================
using UnityEngine;
using UnityEngine.Video;

namespace VisionSpace.Applications
{
    /// <summary>
    /// Plays video files in a floating spatial window using Unity VideoPlayer.
    /// Supports local files via Android file picker intent.
    /// </summary>
    public class MediaViewerController : MonoBehaviour
    {
        [Header("Playback")]
        [SerializeField] private VideoPlayer videoPlayer;
        [SerializeField] private Renderer displayRenderer;
        [SerializeField] private RenderTexture renderTexture;

        [Header("Controls")]
        [SerializeField] private bool isPlaying;
        [SerializeField] private float volume = 0.8f;

        private void Awake()
        {
            if (videoPlayer == null)
            {
                videoPlayer = gameObject.AddComponent<VideoPlayer>();
                videoPlayer.playOnAwake = false;
                videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            }

            if (renderTexture == null)
            {
                renderTexture = new RenderTexture(1280, 720, 0);
                videoPlayer.targetTexture = renderTexture;
            }

            if (displayRenderer != null)
                displayRenderer.material.mainTexture = renderTexture;

            videoPlayer.errorReceived += OnVideoError;
        }

        /// <summary>Load and play a video from a file path.</summary>
        public void PlayFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                Debug.LogWarning("[VisionSpace] MediaViewer: No file path provided.");
                return;
            }

            videoPlayer.source = VideoSource.Url;
            videoPlayer.url = filePath;
            videoPlayer.Prepare();
            videoPlayer.prepareCompleted += OnPrepared;

            Debug.Log($"[VisionSpace] MediaViewer: Loading {filePath}");
        }

        /// <summary>Play a URL (streaming).</summary>
        public void PlayUrl(string url)
        {
            videoPlayer.source = VideoSource.Url;
            videoPlayer.url = url;
            videoPlayer.Prepare();
            videoPlayer.prepareCompleted += OnPrepared;
        }

        public void TogglePlayPause()
        {
            if (videoPlayer.isPlaying) { videoPlayer.Pause(); isPlaying = false; }
            else { videoPlayer.Play(); isPlaying = true; }
        }

        public void Stop()
        {
            videoPlayer.Stop();
            isPlaying = false;
        }

        public void SetVolume(float vol)
        {
            volume = Mathf.Clamp01(vol);
            videoPlayer.SetDirectAudioVolume(0, volume);
        }

        public void Seek(float normalizedTime)
        {
            if (videoPlayer.frameCount > 0)
                videoPlayer.frame = (long)(normalizedTime * videoPlayer.frameCount);
        }

        public float GetProgress()
        {
            if (videoPlayer.frameCount == 0) return 0;
            return (float)videoPlayer.frame / videoPlayer.frameCount;
        }

        private void OnPrepared(VideoPlayer vp)
        {
            vp.Play();
            isPlaying = true;
            Debug.Log($"[VisionSpace] MediaViewer: Playing ({vp.width}x{vp.height})");
        }

        private void OnVideoError(VideoPlayer vp, string message)
        {
            Debug.LogError($"[VisionSpace] MediaViewer error: {message}");
        }

        private void OnDestroy()
        {
            if (renderTexture != null) renderTexture.Release();
        }
    }
}
