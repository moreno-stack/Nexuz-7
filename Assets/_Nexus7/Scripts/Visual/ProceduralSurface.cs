using System;
using UnityEngine;

namespace Nexus7.Visual
{
    /// <summary>
    /// Texturas de superficie generadas por código (lógica pura): mapa de altura, color, mapa normal y máscara
    /// (metal en R, suavidad en A, como espera _MetallicGlossMap de URP). Todas se repiten sin costura.
    /// Las usa el editor (VisualUpgrade) para guardar PNG en Assets/_Nexus7/Data/Textures.
    /// </summary>
    public static class ProceduralSurface
    {
        private static float Mix(float a, float b, float t) => a + (b - a) * t;
        private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
        private static float Smooth(float a, float b, float x) { float k = Clamp01((x - a) / (b - a)); return k * k * (3f - 2f * k); }

        private static float Hash2(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + seed * 1442695041);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / (float)0x1000000;
            }
        }

        private static int Wrap(int a, int period) { int m = a % period; return m < 0 ? m + period : m; }

        /// <summary>Ruido de valor 2D que se repite cada 'period' celdas (u, v de 0 a 1).</summary>
        private static float Tileable(float u, float v, int period, int seed)
        {
            float x = u * period, y = v * period;
            int x0 = (int)Math.Floor(x), y0 = (int)Math.Floor(y);
            float fx = x - x0, fy = y - y0;
            fx = fx * fx * (3f - 2f * fx); fy = fy * fy * (3f - 2f * fy);
            float a00 = Hash2(Wrap(x0, period), Wrap(y0, period), seed), a10 = Hash2(Wrap(x0 + 1, period), Wrap(y0, period), seed);
            float a01 = Hash2(Wrap(x0, period), Wrap(y0 + 1, period), seed), a11 = Hash2(Wrap(x0 + 1, period), Wrap(y0 + 1, period), seed);
            float top = a00 + (a10 - a00) * fx, bot = a01 + (a11 - a01) * fx;
            return top + (bot - top) * fy;
        }

        private static float Fbm(float u, float v, int basePeriod, int seed)
        {
            float sum = 0f, amp = 0.5f, norm = 0f; int p = basePeriod;
            for (int o = 0; o < 4; o++) { sum += amp * Tileable(u, v, p, seed + o * 31); norm += amp; amp *= 0.5f; p *= 2; }
            return sum / norm;
        }

        /// <summary>Distancia (en fracción de celda) a la línea de rejilla más cercana, con 'cells' celdas por lado.</summary>
        private static float GridDist(float u, int cells)
        {
            float x = u * cells; x -= (float)Math.Floor(x);
            return Math.Min(x, 1f - x);
        }

        /// <summary>Arañazos: líneas finas en diagonal que se repiten sin costura.</summary>
        private static float Scratches(float u, float v, int seed)
        {
            float best = 0f;
            for (int i = 0; i < 6; i++)
            {
                // líneas u·a + v·b = c con a, b enteros: periódicas en 1
                int a = 1 + (i % 3), b = (i % 2 == 0 ? 1 : -1) * (1 + (i / 3));
                float c = Hash2(i, seed, 7);
                float d = u * a + v * b - c; d -= (float)Math.Floor(d); d = Math.Min(d, 1f - d);
                float mask = Tileable(u, v, 4, seed + i * 13) > 0.62f ? 1f : 0f;   // solo en algunos tramos
                best = Math.Max(best, mask * (1f - Smooth(0f, 0.004f, d)));
            }
            return best;
        }

        public static float[] HeightMap(SurfaceKind kind, int n)
        {
            var h = new float[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = (x + 0.5f) / n, v = (y + 0.5f) / n, val;
                    switch (kind)
                    {
                        case SurfaceKind.Floor:
                        {
                            float seam = Math.Min(GridDist(u, 2), GridDist(v, 2));             // 2×2 placas
                            float groove = Smooth(0.015f, 0.04f, seam);
                            // dibujo antideslizante: relieves en diagonal
                            float du = GridDist(u + v, 16), dv = GridDist(u - v + 1f, 16);
                            float tread = Math.Min(du, dv) < 0.12f ? 0.12f : 0f;
                            val = 0.25f + 0.45f * groove + tread * groove + 0.08f * Fbm(u, v, 8, 3);
                            break;
                        }
                        case SurfaceKind.Wall:
                        {
                            float seam = Math.Min(GridDist(u, 2), GridDist(v, 1));             // 2 paneles por cara
                            float groove = Smooth(0.01f, 0.03f, seam);
                            // remaches cerca de las esquinas de cada panel
                            float cu = GridDist(u, 2), cv = GridDist(v, 1);
                            float rx = cu - 0.06f, ry = cv - 0.06f;
                            float rivet = rx * rx + ry * ry < 0.0006f ? 0.25f : 0f;
                            val = 0.3f + 0.4f * groove + rivet + 0.06f * Fbm(u, v, 4, 5);
                            break;
                        }
                        case SurfaceKind.Crate:
                        {
                            float edge = Math.Min(Math.Min(u, 1f - u), Math.Min(v, 1f - v));
                            float frame = edge < 0.08f ? 0.85f : 0.5f;
                            float diag = Math.Abs(u - v) < 0.035f || Math.Abs(u + v - 1f) < 0.035f ? 0.15f : 0f;
                            val = frame + diag + 0.08f * Fbm(u, v, 8, 9) - 0.04f;
                            break;
                        }
                        default: // óxido: picaduras y arañazos
                        {
                            float f = Fbm(u, v, 4, 11);
                            val = 0.55f + 0.3f * (f - 0.5f) - 0.15f * Smooth(0.55f, 0.75f, Fbm(u, v, 8, 17)) + 0.1f * Scratches(u, v, 23);
                            break;
                        }
                    }
                    h[y * n + x] = Clamp01(val);
                }
            return h;
        }

        /// <summary>Mapa normal (espacio tangente, convención de Unity: verde hacia arriba) desde la altura.</summary>
        public static Color[] NormalMap(float[] h, int n, float strength)
        {
            var c = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    int xl = (x - 1 + n) % n, xr = (x + 1) % n, yd = (y - 1 + n) % n, yu = (y + 1) % n;
                    float dx = (h[y * n + xr] - h[y * n + xl]) * strength;
                    float dy = (h[yu * n + x] - h[yd * n + x]) * strength;
                    float len = (float)Math.Sqrt(dx * dx + dy * dy + 1f);
                    float nx = -dx / len, ny = -dy / len, nz = 1f / len;
                    c[y * n + x] = new Color(nx * 0.5f + 0.5f, ny * 0.5f + 0.5f, nz * 0.5f + 0.5f, 1f);
                }
            return c;
        }

        /// <summary>Óxido (0–1) en cada punto: se usa en el color y en la máscara del metal oxidado.</summary>
        private static float RustAmount(float u, float v) => Smooth(0.52f, 0.7f, 0.7f * Fbm(u, v, 8, 41) + 0.3f * Fbm(u, v, 2, 43));

        /// <summary>Color en escala casi gris: se multiplica por el color propio de cada material.</summary>
        public static Color[] Albedo(SurfaceKind kind, float[] h, int n)
        {
            var c = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = (x + 0.5f) / n, v = (y + 0.5f) / n, hv = h[y * n + x];
                    float grime = Fbm(u, v, 4, 51);
                    Color col;
                    switch (kind)
                    {
                        case SurfaceKind.Floor: { float g = 0.55f + 0.45f * hv - 0.12f * grime; col = new Color(g, g, g * 1.02f); break; }
                        case SurfaceKind.Wall:  { float g = 0.72f + 0.3f * hv - 0.1f * grime;  col = new Color(g, g, g * 1.03f); break; }
                        case SurfaceKind.Crate: { float wear = hv > 0.8f ? 0.12f : 0f; float g = 0.8f + wear - 0.15f * grime; col = new Color(g, g * 0.98f, g * 0.95f); break; }
                        default:
                        {
                            float rust = RustAmount(u, v);
                            float scratch = Scratches(u, v, 23);
                            float m = 0.9f + 0.1f * grime, rk = 0.8f + 0.3f * grime, sk = scratch * 0.35f;
                            float r = Mix(0.86f * m, 0.66f * rk, rust), g = Mix(0.87f * m, 0.42f * rk, rust), b = Mix(0.9f * m, 0.27f * rk, rust);
                            col = new Color(Mix(r, 1f, sk), Mix(g, 1f, sk), Mix(b, 1f, sk));
                            break;
                        }
                    }
                    c[y * n + x] = new Color(Clamp01(col.r), Clamp01(col.g), Clamp01(col.b), 1f);
                }
            return c;
        }

        /// <summary>Máscara de URP: R = metal, A = suavidad (1 − rugosidad).</summary>
        public static Color[] Mask(SurfaceKind kind, float[] h, int n)
        {
            var c = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = (x + 0.5f) / n, v = (y + 0.5f) / n, hv = h[y * n + x];
                    float metal, smooth;
                    switch (kind)
                    {
                        case SurfaceKind.Floor: metal = 0.85f; smooth = 0.2f + 0.3f * hv; break;
                        case SurfaceKind.Wall:  metal = 0.7f;  smooth = 0.3f + 0.25f * hv; break;
                        case SurfaceKind.Crate: metal = 0.35f; smooth = 0.25f + 0.15f * hv; break;
                        default:
                        {
                            float rust = RustAmount(u, v), scratch = Scratches(u, v, 23);
                            metal = 0.1f + 0.85f * (1f - rust);
                            smooth = 0.08f + 0.42f * (1f - rust) + 0.2f * scratch;
                            break;
                        }
                    }
                    c[y * n + x] = new Color(Clamp01(metal), 0f, 0f, Clamp01(smooth));
                }
            return c;
        }

        /// <summary>
        /// Textura de pantalla holográfica (blanca, la tiñe el material): marco con esquinas, líneas de escaneo,
        /// barras de datos y renglones de «texto». El alfa lleva el dibujo; se usa con mezcla aditiva.
        /// </summary>
        public static Color[] HoloPattern(int n)
        {
            var c = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = (x + 0.5f) / n, v = (y + 0.5f) / n, a = 0.06f;
                    float edge = Math.Min(Math.Min(u, 1f - u), Math.Min(v, 1f - v));
                    if (edge < 0.015f) a = 1f;                                                    // marco
                    else if (edge < 0.05f && (Math.Min(u, 1f - u) < 0.12f && Math.Min(v, 1f - v) < 0.12f)) a = 0.9f; // esquinas
                    else
                    {
                        if (y % 4 == 0) a += 0.12f;                                              // líneas de escaneo
                        if (v > 0.74f && v < 0.82f && u > 0.08f && u < 0.08f + 0.84f * (0.35f + 0.5f * Hash2(3, 0, 5))) a = 0.75f; // barra 1
                        if (v > 0.64f && v < 0.70f && u > 0.08f && u < 0.08f + 0.84f * (0.2f + 0.6f * Hash2(4, 0, 5))) a = 0.55f;  // barra 2
                        // renglones de «texto»: guiones de distinta longitud
                        int row = (int)Math.Floor(v * 18f);
                        float inRow = v * 18f - row;
                        if (row >= 2 && row <= 9 && inRow > 0.3f && inRow < 0.7f && u > 0.08f && u < 0.92f)
                        {
                            int word = (int)Math.Floor(u * 14f);
                            if (Hash2(word, row, 21) > 0.3f && (u * 14f - word) < 0.8f) a = 0.6f;
                        }
                    }
                    c[y * n + x] = new Color(1f, 1f, 1f, Clamp01(a));
                }
            return c;
        }
    }
}
