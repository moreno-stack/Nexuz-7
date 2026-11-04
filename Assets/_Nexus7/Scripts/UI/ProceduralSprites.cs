using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nexus7.UI
{
    /// <summary>Formas de la interfaz que se generan por código (no hacen falta imágenes en el proyecto).</summary>
    public enum UIShapeKind { Circle, Ring, Star, Vignette, Scanlines, Gradient, GradientH, Glow, Halo }

    /// <summary>
    /// Genera (y guarda en caché) los sprites de las pantallas de inicio y final: círculo, anillo, estrella,
    /// viñeta, líneas de escaneo y degradado. Las funciones *Alpha son lógica pura (probadas sin Unity):
    /// devuelven la opacidad de un punto de la textura (coordenadas en píxeles, origen abajo a la izquierda).
    /// </summary>
    public static class ProceduralSprites
    {
        private static readonly Dictionary<UIShapeKind, Sprite> sprites = new Dictionary<UIShapeKind, Sprite>();
        private static readonly Dictionary<UIShapeKind, Texture2D> textures = new Dictionary<UIShapeKind, Texture2D>();

        /// <summary>Lado de la textura de cada forma (los degradados son tiras de 4 píxeles de grosor).</summary>
        public static int SizeOf(UIShapeKind k)
        {
            switch (k)
            {
                case UIShapeKind.Ring:      return 128;
                case UIShapeKind.Vignette:  return 128;
                case UIShapeKind.Glow:      return 128;
                case UIShapeKind.Halo:      return 128;
                case UIShapeKind.Scanlines: return 4;
                default:                    return 64;
            }
        }

        // ---------------------------------------------------------------- lógica pura
        private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
        private static float Dist(float x, float y, float cx, float cy) => (float)Math.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));

        /// <summary>Disco que ocupa toda la textura, con 1 px de borde suavizado.</summary>
        public static float CircleAlpha(float x, float y, float size)
        {
            float r = size / 2f;
            return Clamp01(r - Dist(x, y, r, r));
        }

        /// <summary>Aro de grosor 'thickness' píxeles pegado al borde de la textura.</summary>
        public static float RingAlpha(float x, float y, float size, float thickness)
        {
            float r = size / 2f, d = Dist(x, y, r, r);
            return Clamp01(r - d) * Clamp01(d - (r - thickness));
        }

        /// <summary>Estrella de 5 puntas con una punta hacia arriba (cobertura de 4×4 submuestras del píxel).</summary>
        public static float StarAlpha(float x, float y, float size)
        {
            var vx = new float[10]; var vy = new float[10];
            float c = size / 2f;
            for (int i = 0; i < 10; i++)   // 10 vértices alternando radio exterior e interior, desde la punta de arriba (90°)
            {
                float a = (float)(Math.PI / 2 + i * Math.PI / 5), r = i % 2 == 0 ? c : c * 0.45f;
                vx[i] = c + r * (float)Math.Cos(a); vy[i] = c + r * (float)Math.Sin(a);
            }
            int inside = 0;
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 4; j++)
                    if (InPolygon(x - 0.5f + (i + 0.5f) / 4f, y - 0.5f + (j + 0.5f) / 4f, vx, vy)) inside++;
            return inside / 16f;
        }

        private static bool InPolygon(float x, float y, float[] vx, float[] vy)
        {
            bool odd = false;
            for (int i = 0, j = vx.Length - 1; i < vx.Length; j = i++)
                if ((vy[i] > y) != (vy[j] > y) && x < (vx[j] - vx[i]) * (y - vy[i]) / (vy[j] - vy[i]) + vx[i]) odd = !odd;
            return odd;
        }

        /// <summary>Viñeta: transparente en el centro y oscura hacia las esquinas (u, v de 0 a 1).</summary>
        public static float VignetteAlpha(float u, float v)
        {
            float dx = (u - 0.5f) * 2f, dy = (v - 0.5f) * 2f;
            float r = (float)Math.Sqrt(dx * dx + dy * dy) / 1.4142135f;
            float k = Clamp01((r - 0.35f) / 0.65f);
            return 0.85f * k * k * (3f - 2f * k);
        }

        /// <summary>Líneas de escaneo: una fila oscura de cada 4.</summary>
        public static float ScanlineAlpha(int row) => row % 4 == 0 ? 0.35f : 0f;

        /// <summary>Resplandor redondo y suave (nebulosas, halo del planeta): 1 en el centro, 0 en el borde.</summary>
        public static float GlowAlpha(float u, float v)
        {
            float dx = (u - 0.5f) * 2f, dy = (v - 0.5f) * 2f;
            float k = 1f - Clamp01((float)Math.Sqrt(dx * dx + dy * dy));
            return k * k;
        }

        /// <summary>Halo de atmósfera: anillo difuso con el máximo a 3/4 del radio (el borde del planeta).</summary>
        public static float HaloAlpha(float u, float v)
        {
            float dx = (u - 0.5f) * 2f, dy = (v - 0.5f) * 2f;
            float r = (float)Math.Sqrt(dx * dx + dy * dy);
            float k = r < 0.75f ? Clamp01((r - 0.5f) / 0.25f) : 1f - Clamp01((r - 0.75f) / 0.25f);
            return k * k * (3f - 2f * k);
        }

        /// <summary>Degradado vertical: 0 abajo, 1 arriba.</summary>
        public static float GradientAlpha(float v) => Clamp01(v);

        /// <summary>Opacidad del píxel (px, py) de la forma 'k'.</summary>
        public static float Alpha(UIShapeKind k, int px, int py)
        {
            int n = SizeOf(k);
            float x = px + 0.5f, y = py + 0.5f;
            switch (k)
            {
                case UIShapeKind.Circle:    return CircleAlpha(x, y, n);
                case UIShapeKind.Ring:      return RingAlpha(x, y, n, 10f);
                case UIShapeKind.Star:      return StarAlpha(x, y, n);
                case UIShapeKind.Vignette:  return VignetteAlpha(x / n, y / n);
                case UIShapeKind.Scanlines: return ScanlineAlpha(py);
                case UIShapeKind.Glow:      return GlowAlpha(x / n, y / n);
                case UIShapeKind.Halo:      return HaloAlpha(x / n, y / n);
                case UIShapeKind.GradientH: return 1f - GradientAlpha(x / n);
                default:                    return GradientAlpha(y / n);
            }
        }

        // ---------------------------------------------------------------- Unity
        /// <summary>Textura blanca con la opacidad de la forma (el color lo pone la Image o la RawImage).</summary>
        public static Texture2D Texture(UIShapeKind k)
        {
            Texture2D tex;
            if (textures.TryGetValue(k, out tex) && tex != null) return tex;
            int n = SizeOf(k);
            int w = k == UIShapeKind.Gradient ? 4 : n, h = k == UIShapeKind.GradientH ? 4 : n;
            tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.name = "Nexus7_" + k;
            tex.hideFlags = HideFlags.DontSave;   // se regenera al cargar: no se guarda dentro de la escena
            tex.wrapMode = k == UIShapeKind.Scanlines ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            tex.filterMode = k == UIShapeKind.Scanlines ? FilterMode.Point : FilterMode.Bilinear;
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    px[y * w + x] = new Color(1f, 1f, 1f, Alpha(k, x, y));
            tex.SetPixels(px);
            tex.Apply();
            textures[k] = tex;
            return tex;
        }

        public static Sprite Get(UIShapeKind k)
        {
            Sprite s;
            if (sprites.TryGetValue(k, out s) && s != null) return s;
            var tex = Texture(k);
            s = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            s.name = tex.name;
            s.hideFlags = HideFlags.DontSave;
            sprites[k] = s;
            return s;
        }
    }
}
