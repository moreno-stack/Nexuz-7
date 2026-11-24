using System.Collections.Generic;
using UnityEngine;
using Nexus7.Devices;
using Nexus7.Inventory;
using Nexus7.Visual.Models;

namespace Nexus7.UI
{
    /// <summary>
    /// Iconos de los objetos para la interfaz: se dibujan una sola vez a partir de su modelo (IconRasterizer) y se
    /// guardan en caché por Id. No hace falta ninguna imagen en el proyecto.
    /// </summary>
    public static class ItemIcons
    {
        public const int Size = 64;
        private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        public static Sprite For(ItemData item)
        {
            if (item == null) return null;
            var device = item as DeviceItemData;
            string key = item.Id + (device != null ? "|" + device.Type : "");
            if (cache.TryGetValue(key, out var sprite) && sprite != null) return sprite;

            float[] px = IconRasterizer.Render(ItemModels.For(item), Size);
            var colors = new Color[Size * Size];
            for (int i = 0; i < colors.Length; i++)
                colors[i] = new Color(px[i * 4], px[i * 4 + 1], px[i * 4 + 2], px[i * 4 + 3]);
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            tex.SetPixels(colors);
            tex.Apply();
            sprite = Sprite.Create(tex, new Rect(0f, 0f, Size, Size), new Vector2(0.5f, 0.5f), 100f);
            cache[key] = sprite;
            return sprite;
        }
    }
}
