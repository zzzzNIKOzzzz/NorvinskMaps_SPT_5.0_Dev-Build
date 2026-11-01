using System;
using NorvinskMaps.Data;
using UnityEngine;

namespace NorvinskMaps.Utils
{
    public static class SvgUtils
    {
        public static Sprite GetOrLoadCachedSprite(MapLayerDef def)
        {
            var pngPath = def.ImagePath;
            if (pngPath.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
                pngPath = pngPath.Substring(0, pngPath.Length - 4) + ".png";

            return TextureUtils.GetOrLoadCachedSprite(pngPath);
        }
    }
}