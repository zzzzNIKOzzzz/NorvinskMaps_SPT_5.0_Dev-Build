using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace NorvinskMaps.Utils
{
    public static class TextureUtils
    {
        private static readonly Dictionary<string, Sprite> _spriteCache = new();
        private static readonly HashSet<string> _missingTextures = new();

        // Эти флаги не дают Unity выгрузить текстуры/спрайты при Resources.UnloadUnusedAssets
        // (вызывается игрой при смене сцены) — иначе маркеры станут "белыми квадратами" после смены локации.
        private const HideFlags LoadedAssetFlags =
            HideFlags.DontSave | HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild
            | HideFlags.DontUnloadUnusedAsset;

        private static Texture2D LoadTexture2DFromPath(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath) || !File.Exists(absolutePath))
                return null;

            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!tex.LoadImage(File.ReadAllBytes(absolutePath)))
            {
                Object.Destroy(tex);
                return null;
            }
            return tex;
        }

        public static Sprite GetOrLoadCachedSprite(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;

            if (_spriteCache.TryGetValue(path, out var sprite) && sprite != null && sprite.texture != null)
                return sprite;

            _spriteCache.Remove(path);

            var absolutePath = Path.Combine(NorvinskMapsPlugin.Path, path);
            var texture = LoadTexture2DFromPath(absolutePath);

            if (texture == null)
            {
                if (_missingTextures.Add(path))
                    NorvinskMapsPlugin.Log.LogWarning($"[TextureUtils] failed to load: {absolutePath}");
                return null;
            }

            texture.hideFlags = LoadedAssetFlags;

            sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f));
            sprite.hideFlags = LoadedAssetFlags;

            _spriteCache[path] = sprite;
            return sprite;
        }

        public static void ClearCache()
        {
            _spriteCache.Clear();
            _missingTextures.Clear();
        }
    }
}