using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace KaleidoscopeEngine.UI
{
    public static class KaleidoscopeFileSelectionUtility
    {
        private static readonly string[] ImageExtensions = { ".png", ".jpg", ".jpeg", ".tga" };
        private static readonly string[] AudioExtensions = { ".wav", ".mp3", ".ogg", ".aiff", ".aif", ".flac" };

        public static bool TrySelectImageFile(out string path, out Texture2D texture, out string message)
        {
            path = string.Empty;
            texture = null;
            message = string.Empty;
#if UNITY_EDITOR
            path = EditorUtility.OpenFilePanel("Select Kaleidoscope Image", string.Empty, "png,jpg,jpeg,tga");
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            return TryLoadTexture(path, out texture, out message);
#else
            message = "File selection is available in the Unity Editor build path first.";
            return false;
#endif
        }

        public static bool TrySelectImageFolder(out string folderPath, out string[] imagePaths, out Texture2D[] textures, out string message)
        {
            folderPath = string.Empty;
            imagePaths = null;
            textures = null;
            message = string.Empty;
#if UNITY_EDITOR
            folderPath = EditorUtility.OpenFolderPanel("Select Kaleidoscope Image Folder", string.Empty, string.Empty);
            if (string.IsNullOrEmpty(folderPath))
            {
                return false;
            }

            imagePaths = EnumerateFiles(folderPath, ImageExtensions);
            if (imagePaths.Length == 0)
            {
                message = "No supported image files found.";
                return false;
            }

            List<Texture2D> loadedTextures = new List<Texture2D>(imagePaths.Length);
            List<string> loadedPaths = new List<string>(imagePaths.Length);
            for (int i = 0; i < imagePaths.Length; i++)
            {
                if (TryLoadTexture(imagePaths[i], out Texture2D texture, out string loadMessage))
                {
                    loadedTextures.Add(texture);
                    loadedPaths.Add(imagePaths[i]);
                    continue;
                }

                message = loadMessage;
            }

            imagePaths = loadedPaths.ToArray();
            textures = loadedTextures.ToArray();
            message = textures.Length > 0 ? $"Loaded {textures.Length} image(s)." : "No image files could be loaded.";
            return textures.Length > 0;
#else
            message = "Folder selection is available in the Unity Editor build path first.";
            return false;
#endif
        }

        public static bool TrySelectAudioFile(out string path, out string message)
        {
            path = string.Empty;
            message = string.Empty;
#if UNITY_EDITOR
            path = EditorUtility.OpenFilePanel("Select Kaleidoscope Audio Track", string.Empty, "wav,mp3,ogg,aiff,aif,flac");
            return !string.IsNullOrEmpty(path);
#else
            message = "Audio file selection is available in the Unity Editor build path first.";
            return false;
#endif
        }

        public static bool TrySelectAudioFolder(out string folderPath, out string[] audioPaths, out string message)
        {
            folderPath = string.Empty;
            audioPaths = null;
            message = string.Empty;
#if UNITY_EDITOR
            folderPath = EditorUtility.OpenFolderPanel("Select Kaleidoscope Audio Folder", string.Empty, string.Empty);
            if (string.IsNullOrEmpty(folderPath))
            {
                return false;
            }

            audioPaths = EnumerateFiles(folderPath, AudioExtensions);
            message = audioPaths.Length > 0 ? $"Found {audioPaths.Length} audio track(s)." : "No supported audio files found.";
            return audioPaths.Length > 0;
#else
            message = "Audio folder selection is available in the Unity Editor build path first.";
            return false;
#endif
        }

        private static bool TryLoadTexture(string path, out Texture2D texture, out string message)
        {
            texture = null;
            message = string.Empty;
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                message = "Image file does not exist.";
                return false;
            }

            try
            {
                byte[] bytes = File.ReadAllBytes(path);
                texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
                {
                    name = Path.GetFileNameWithoutExtension(path),
                    wrapMode = TextureWrapMode.Repeat,
                    filterMode = FilterMode.Trilinear,
                    anisoLevel = 2
                };

                if (!texture.LoadImage(bytes))
                {
                    UnityEngine.Object.Destroy(texture);
                    texture = null;
                    message = $"Image decode failed: {path}";
                    return false;
                }

                texture.wrapMode = TextureWrapMode.Repeat;
                texture.filterMode = FilterMode.Trilinear;
                texture.anisoLevel = 2;
                message = $"Loaded image: {texture.name}";
                return true;
            }
            catch (Exception exception)
            {
                message = $"Image load failed: {exception.Message}";
                return false;
            }
        }

        private static string[] EnumerateFiles(string folderPath, string[] extensions)
        {
            if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
            {
                return Array.Empty<string>();
            }

            List<string> paths = new List<string>();
            string[] files = Directory.GetFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly);
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < files.Length; i++)
            {
                string extension = Path.GetExtension(files[i]);
                if (MatchesExtension(extension, extensions))
                {
                    paths.Add(files[i]);
                }
            }

            return paths.ToArray();
        }

        private static bool MatchesExtension(string extension, string[] supportedExtensions)
        {
            if (string.IsNullOrEmpty(extension))
            {
                return false;
            }

            for (int i = 0; i < supportedExtensions.Length; i++)
            {
                if (string.Equals(extension, supportedExtensions[i], StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
