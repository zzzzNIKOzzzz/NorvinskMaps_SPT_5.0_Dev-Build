using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace NorvinskMaps.Utils
{
    public static class TextureUtils
    {
        private static Dictionary<string, Sprite> _spriteCache = new();

        private static Texture2D LoadTexture2DFromPath(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath) || !File.Exists(absolutePath))
                return null;

            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(File.ReadAllBytes(absolutePath));
            return tex;
        }

        public static Sprite GetOrLoadCachedSprite(string path)
        {
            if (_spriteCache.TryGetValue(path, out var sprite))
                return sprite;

            var absolutePath = Path.Combine(NorvinskMapsPlugin.Path, path);
            var texture = LoadTexture2DFromPath(absolutePath);
            
            if (texture == null)
            {
                NorvinskMapsPlugin.Log.LogWarning($"[TextureUtils] failed to load: {absolutePath}");
                return null;
            }

            sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f)); // 0.5f, 0.5f — это центр, а не пиксели!

            _spriteCache[path] = sprite;
            return sprite;
        }
    }
}