// =============================================================================
// ImageGalleryController.cs — Spatial image gallery
// =============================================================================
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VisionSpace.Applications
{
    /// <summary>
    /// Displays images in a spatial gallery layout. Supports loading from
    /// local storage (Android Downloads/Pictures folders).
    /// </summary>
    public class ImageGalleryController : MonoBehaviour
    {
        [Header("Layout")]
        [SerializeField] private int columns = 3;
        [SerializeField] private float thumbnailSize = 0.15f;
        [SerializeField] private float spacing = 0.02f;

        [Header("References")]
        [SerializeField] private Transform thumbnailContainer;
        [SerializeField] private Renderer fullImageDisplay;

        private List<string> _imagePaths = new List<string>();
        private List<Texture2D> _thumbnails = new List<Texture2D>();
        private int _selectedIndex = -1;

        /// <summary>Scan for images in accessible directories.</summary>
        public void ScanForImages()
        {
            _imagePaths.Clear();

#if UNITY_ANDROID && !UNITY_EDITOR
            string[] searchPaths = {
                "/sdcard/Download",
                "/sdcard/Pictures",
                "/sdcard/DCIM",
                "/sdcard/Oculus/Screenshots",
            };
#else
            string[] searchPaths = { Application.dataPath };
#endif

            string[] extensions = { "*.jpg", "*.jpeg", "*.png", "*.bmp" };

            foreach (var dir in searchPaths)
            {
                if (!Directory.Exists(dir)) continue;
                foreach (var ext in extensions)
                {
                    try
                    {
                        var files = Directory.GetFiles(dir, ext, SearchOption.TopDirectoryOnly);
                        _imagePaths.AddRange(files);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"[VisionSpace] Gallery: Cannot read {dir}: {e.Message}");
                    }
                }
            }

            Debug.Log($"[VisionSpace] Gallery: Found {_imagePaths.Count} images.");
            LoadThumbnails();
        }

        /// <summary>Load a specific image file.</summary>
        public void LoadImage(string path)
        {
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[VisionSpace] Gallery: File not found: {path}");
                return;
            }

            try
            {
                byte[] data = File.ReadAllBytes(path);
                Texture2D tex = new Texture2D(2, 2);
                tex.LoadImage(data);

                if (fullImageDisplay != null)
                {
                    fullImageDisplay.material.mainTexture = tex;
                    // Adjust aspect ratio
                    float aspect = (float)tex.width / tex.height;
                    fullImageDisplay.transform.localScale = new Vector3(
                        0.5f * aspect, 0.5f, 1f);
                }

                Debug.Log($"[VisionSpace] Gallery: Loaded {Path.GetFileName(path)} ({tex.width}x{tex.height})");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[VisionSpace] Gallery: Failed to load {path}: {e.Message}");
            }
        }

        /// <summary>Select an image by index.</summary>
        public void SelectImage(int index)
        {
            if (index < 0 || index >= _imagePaths.Count) return;
            _selectedIndex = index;
            LoadImage(_imagePaths[index]);
        }

        public void NextImage() => SelectImage(_selectedIndex + 1);
        public void PreviousImage() => SelectImage(_selectedIndex - 1);

        private void LoadThumbnails()
        {
            foreach (var tex in _thumbnails)
                if (tex != null) Destroy(tex);
            _thumbnails.Clear();

            // Load first N thumbnails (performance limit)
            int maxThumbs = Mathf.Min(_imagePaths.Count, 24);
            for (int i = 0; i < maxThumbs; i++)
            {
                try
                {
                    byte[] data = File.ReadAllBytes(_imagePaths[i]);
                    Texture2D tex = new Texture2D(2, 2);
                    tex.LoadImage(data);

                    // Downsample for thumbnail
                    if (tex.width > 128 || tex.height > 128)
                    {
                        // Unity doesn't have built-in resize, so just use the full texture
                        // In production, use RenderTexture blit for downsampling
                    }

                    _thumbnails.Add(tex);
                }
                catch
                {
                    _thumbnails.Add(null);
                }
            }

            BuildThumbnailGrid();
        }

        private void BuildThumbnailGrid()
        {
            if (thumbnailContainer == null) return;

            foreach (Transform child in thumbnailContainer)
                Destroy(child.gameObject);

            float totalWidth = (columns - 1) * (thumbnailSize + spacing);
            float startX = -totalWidth / 2f;

            for (int i = 0; i < _thumbnails.Count; i++)
            {
                int col = i % columns;
                int row = i / columns;

                var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.transform.SetParent(thumbnailContainer);
                quad.transform.localPosition = new Vector3(
                    startX + col * (thumbnailSize + spacing),
                    -row * (thumbnailSize + spacing),
                    0);
                quad.transform.localScale = Vector3.one * thumbnailSize;
                quad.transform.localRotation = Quaternion.identity;
                quad.name = $"Thumb_{i}";

                if (_thumbnails[i] != null)
                {
                    var r = quad.GetComponent<Renderer>();
                    r.material = new Material(Shader.Find("Sprites/Default"));
                    r.material.mainTexture = _thumbnails[i];
                }
            }
        }
    }
}
