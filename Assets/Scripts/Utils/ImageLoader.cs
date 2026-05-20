using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace UstAldanQuiz.Utils
{
    public static class ImageLoader
    {
        private static readonly Dictionary<string, Texture2D> _memCache = new Dictionary<string, Texture2D>();

        private static string CacheDir =>
            Path.Combine(Application.persistentDataPath, "ImageCache");

        public static IEnumerator Load(string url, Action<Texture2D> onDone)
        {
            if (string.IsNullOrEmpty(url)) { onDone?.Invoke(null); yield break; }

            // 1. Память
            if (_memCache.TryGetValue(url, out var cached)) { onDone?.Invoke(cached); yield break; }

            // 2. Билд (Assets/Images/Resources/Questions/...)
            var bundled = Resources.Load<Texture2D>(GetResourceName(url));
            if (bundled != null) { _memCache[url] = bundled; onDone?.Invoke(bundled); yield break; }

            // 3. Диск — скачивалось раньше
            string diskPath = GetDiskPath(url);
            if (File.Exists(diskPath))
            {
                var tex = LoadFromDisk(diskPath);
                if (tex != null) { _memCache[url] = tex; onDone?.Invoke(tex); yield break; }
            }

            // 4. Сеть — скачать и сохранить на диск
            using var req = UnityWebRequestTexture.GetTexture(url);
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[ImageLoader] Не удалось загрузить: {url} — {req.error}");
                onDone?.Invoke(null);
                yield break;
            }

            var downloaded = DownloadHandlerTexture.GetContent(req);
            SaveToDisk(diskPath, downloaded);
            _memCache[url] = downloaded;
            onDone?.Invoke(downloaded);
        }

        public static void ClearCache()
        {
            foreach (var tex in _memCache.Values)
                if (tex != null) UnityEngine.Object.Destroy(tex);
            _memCache.Clear();

            if (Directory.Exists(CacheDir))
                Directory.Delete(CacheDir, true);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        // https://.../Questions/History/hist_001.jpg → Questions/History/hist_001
        private static string GetResourceName(string url)
        {
            var uri = new Uri(url);
            string path = uri.AbsolutePath.TrimStart('/');
            int slash = path.IndexOf('/');
            if (slash >= 0) path = path.Substring(slash + 1);
            return Path.ChangeExtension(path, null);
        }

        private static string GetDiskPath(string url)
        {
            string fileName = Path.GetFileName(new Uri(url).LocalPath);
            return Path.Combine(CacheDir, fileName);
        }

        private static Texture2D LoadFromDisk(string path)
        {
            try
            {
                byte[] bytes = File.ReadAllBytes(path);
                var tex = new Texture2D(2, 2);
                if (tex.LoadImage(bytes)) return tex;
                UnityEngine.Object.Destroy(tex);
                return null;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ImageLoader] Ошибка чтения с диска: {e.Message}");
                return null;
            }
        }

        private static void SaveToDisk(string path, Texture2D tex)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllBytes(path, tex.EncodeToJPG(80));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ImageLoader] Ошибка сохранения на диск: {e.Message}");
            }
        }
    }
}
