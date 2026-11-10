using System;
using System.Collections.Generic;
using UnityEngine;
using Nexus7.Core;

namespace Nexus7.Visual
{
    /// <summary>Ruido determinista sencillo (sin depender de Mathf.PerlinNoise, para poder probarlo fuera de Unity).</summary>
    public static class VisualNoise
    {
        public static float Hash(double n)
        {
            double s = Math.Sin(n * 127.1 + 311.7) * 43758.5453;
            return (float)(s - Math.Floor(s));
        }

        /// <summary>Ruido 1D suave entre 0 y 1.</summary>
        public static float Value(double x)
        {
            double i = Math.Floor(x), f = x - i;
            double u = f * f * (3.0 - 2.0 * f);
            return (float)(Hash(i) + (Hash(i + 1.0) - Hash(i)) * u);
        }
    }

    public enum FlickerStyle { Steady, Hum, Faulty, Emergency }

    /// <summary>Factor de intensidad (0–1,2) de luces y pantallas según su estilo de parpadeo.</summary>
    public static class FlickerModel
    {
        public static float Sample(FlickerStyle style, float t, float seed)
        {
            switch (style)
            {
                case FlickerStyle.Hum:   // fluorescente que zumba: casi fijo, con un temblor leve
                    return 0.94f + 0.04f * (float)Math.Sin(t * 2.0 * Math.PI * 7.0 + seed)
                                 + 0.04f * (VisualNoise.Value(t * 1.3 + seed * 10.0) - 0.5f);
                case FlickerStyle.Faulty: // averiada: oscila y, en uno de cada ~8 cuartos de segundo, se corta a tirones
                {
                    double slot = Math.Floor(t * 4.0);
                    if (VisualNoise.Hash(slot * 1.37 + seed * 91.7) < 0.12f)
                        return VisualNoise.Hash(Math.Floor(t * 40.0) + seed * 17.3) < 0.6f ? 0.05f : 0.7f;
                    return 0.85f + 0.15f * VisualNoise.Value(t * 6.0 + seed * 13.0);
                }
                case FlickerStyle.Emergency: // luz roja de emergencia: parpadeo fuerte y apagones cortos
                {
                    double slot = Math.Floor(t * 5.0);
                    if (VisualNoise.Hash(slot * 2.11 + seed * 37.9) < 0.06f) return 0.05f;
                    return 1f - 0.4f * VisualNoise.Value(t * 7.0 + seed * 5.0);
                }
                default: return 1f;
            }
        }
    }

    /// <summary>Estado de un holograma en un instante: fallo digital con saltos, estiramientos y cortes.</summary>
    public struct GlitchState
    {
        public bool active, visible;
        public float offsetX, scaleY, intensity;
    }

    public static class GlitchModel
    {
        public const float GlitchChance = 0.14f;

        public static GlitchState Sample(float t, float seed)
        {
            double slot = Math.Floor(t * 4.0);
            if (VisualNoise.Hash(slot + seed * 53.1) >= GlitchChance)
                return new GlitchState { active = false, visible = true, offsetX = 0f, scaleY = 1f, intensity = 1f };
            bool cut = VisualNoise.Hash(slot * 3.7 + seed) < 0.25f;
            return new GlitchState
            {
                active = true,
                visible = !cut,
                offsetX = (VisualNoise.Hash(Math.Floor(t * 30.0) + seed * 7.0) - 0.5f) * 0.16f,
                scaleY = 0.6f + 0.5f * VisualNoise.Hash(Math.Floor(t * 20.0) + seed * 3.0),
                intensity = 0.4f + 1.2f * VisualNoise.Hash(Math.Floor(t * 25.0) + seed * 11.0),
            };
        }

        /// <summary>Líneas de escaneo que suben por el holograma (v de 0 a 1).</summary>
        public static float ScanlineAlpha(float v, float t) => 0.5f + 0.5f * (float)Math.Sin((v * 40.0 - t * 3.0) * 2.0 * Math.PI);
    }

    /// <summary>Esperas aleatorias entre un mínimo y un máximo (chispas, vapor). Determinista con su semilla.</summary>
    public class IntermittentTimer
    {
        private readonly float min, max;
        private readonly System.Random rng;
        private float remaining;

        public IntermittentTimer(float min, float max, int seed)
        {
            this.min = Math.Min(min, max); this.max = Math.Max(min, max);
            rng = new System.Random(seed);
            remaining = Next();
        }

        public float Next() => min + (max - min) * (float)rng.NextDouble();

        /// <summary>Avanza el tiempo; devuelve true cuando toca disparar.</summary>
        public bool Tick(float dt)
        {
            remaining -= dt;
            if (remaining > 1e-4f) return false;
            remaining += Next();
            return true;
        }
    }

    /// <summary>Valores del posprocesado que cambian con la tensión.</summary>
    public struct PostFxState
    {
        public float chromatic, vignette, saturation;
        public Color colorFilter;
    }

    /// <summary>Parámetros recomendados del volumen global (ver la guía de gráficos).</summary>
    public struct PostFxDefaults
    {
        public float bloomThreshold, bloomIntensity, bloomScatter, vignette, vignetteSmoothness, temperature, tint,
                     saturation, contrast, postExposure, grain;
        public Vector4 shadows, highlights;
    }

    public static class PostFxModel
    {
        public const float BaseVignette = 0.32f;
        public const float BaseSaturation = -10f;

        public static readonly PostFxDefaults Defaults = new PostFxDefaults
        {
            bloomThreshold = 1f, bloomIntensity = 0.8f, bloomScatter = 0.7f,
            vignette = BaseVignette, vignetteSmoothness = 0.45f,
            temperature = -12f, tint = -2f, saturation = BaseSaturation, contrast = 12f, postExposure = 0.1f,
            grain = 0.12f,
            shadows = new Vector4(0.9f, 0.97f, 1.12f, 0f),     // sombras azuladas
            highlights = new Vector4(1.06f, 1.02f, 0.96f, 0f), // luces algo cálidas
        };

        /// <summary>Estado objetivo según el nivel de alerta, la vida (0–1) y el pulso de un golpe (0–1).</summary>
        public static PostFxState For(AlertLevel level, float health, float hitPulse)
        {
            float chrom = level == AlertLevel.Red ? 0.35f : level == AlertLevel.Yellow ? 0.15f : 0f;
            chrom = Math.Min(1f, chrom + 0.45f * Clamp01(hitPulse));
            float low = Clamp01((0.35f - health) / 0.35f);
            float vig = BaseVignette + (level == AlertLevel.Red ? 0.10f : level == AlertLevel.Yellow ? 0.04f : 0f) + 0.08f * low;
            float sat = BaseSaturation + (level == AlertLevel.Red ? -15f : level == AlertLevel.Yellow ? -5f : 0f);
            Color filter = level == AlertLevel.Red ? new Color(1f, 0.84f, 0.82f) : new Color(1f, 1f, 1f, 1f);
            filter = Lerp(filter, new Color(1f, 0.75f, 0.75f, 1f), low * 0.6f);
            return new PostFxState { chromatic = chrom, vignette = vig, saturation = sat, colorFilter = filter };
        }

        // Matemática propia (no la de Unity) para poder probar el modelo fuera del motor
        private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
        private static Color Lerp(Color a, Color b, float t) =>
            new Color(a.r + (b.r - a.r) * t, a.g + (b.g - a.g) * t, a.b + (b.b - a.b) * t, a.a + (b.a - a.a) * t);

        /// <summary>El pulso del golpe se apaga en 0,4 s.</summary>
        public static float DecayPulse(float pulse, float dt) => Math.Max(0f, pulse - dt * 2.5f);

        /// <summary>Acercamiento exponencial suave (independiente de los FPS).</summary>
        public static float Approach(float current, float target, float dt, float speed) =>
            current + (target - current) * (1f - (float)Math.Exp(-speed * dt));
    }

    /// <summary>Encender o apagar efectos según la distancia a la cámara, con un 10 % de margen.</summary>
    public static class VisualCull
    {
        public static bool ShouldRun(float distance, float radius, bool wasRunning) =>
            distance <= (wasRunning ? radius * 1.1f : radius);
    }

    /// <summary>Elige qué luces del techo proyectan sombra: las que tienen más estanterías y cajas debajo.</summary>
    public static class ShadowLightPicker
    {
        public static List<int> Pick(IList<Vector3> lights, IList<Vector3> occluders, int max, float radius)
        {
            var scored = new List<KeyValuePair<int, int>>();
            for (int i = 0; i < lights.Count; i++)
            {
                int count = 0;
                foreach (var o in occluders)
                {
                    float dx = o.x - lights[i].x, dz = o.z - lights[i].z;
                    if (dx * dx + dz * dz <= radius * radius) count++;
                }
                if (count > 0) scored.Add(new KeyValuePair<int, int>(i, count));
            }
            scored.Sort((a, b) => b.Value != a.Value ? b.Value.CompareTo(a.Value) : a.Key.CompareTo(b.Key));
            var result = new List<int>();
            for (int i = 0; i < scored.Count && i < max; i++) result.Add(scored[i].Key);
            return result;
        }
    }

    /// <summary>Cómo repartir pocos efectos entre muchos sitios posibles.</summary>
    public static class VisualPlacement
    {
        /// <summary>Toma hasta 'max' elementos repartidos a lo largo de la lista (no todos juntos al principio).</summary>
        public static List<T> Spread<T>(IList<T> items, int max)
        {
            var result = new List<T>();
            if (items == null || items.Count == 0 || max <= 0) return result;
            if (items.Count <= max) { result.AddRange(items); return result; }
            for (int i = 0; i < max; i++) result.Add(items[(int)((i + 0.5f) * items.Count / max)]);
            return result;
        }
    }

    public enum SurfaceKind { Skip, Floor, Wall, Rust, Crate }

    /// <summary>Qué textura le toca a cada material del juego, según su nombre.</summary>
    public static class SurfaceClassifier
    {
        private static readonly string[] SkipWords =
        {
            "pantalla", "holo", "lampara", "luz", "brillo", "mj_", "mr_", "mm_", "mb_", "planeta", "estrella", "explos", "cristal",
            "visor", "espacio", "indicador", "led", "aviso", "cartel", "papel", "liquido", "quemad", "propulsor", "capsulacrio",
            "fluorescente", "nucleo", "laser", "ventana", "monitor", "consola", "dron", "cable", "bateria", "circuito",
            "franja", "etiqueta",
        };

        public static SurfaceKind Classify(string materialName)
        {
            if (string.IsNullOrEmpty(materialName)) return SurfaceKind.Skip;
            string n = materialName.ToLowerInvariant();
            foreach (var w in SkipWords) if (n.Contains(w)) return SurfaceKind.Skip;
            if (n.Contains("suelo")) return SurfaceKind.Floor;
            if (n.Contains("pared") || n.Contains("muro") || n.Contains("panel")) return SurfaceKind.Wall;
            if (n.Contains("caja") || n.Contains("bidon") || n.Contains("contenedor")) return SurfaceKind.Crate;
            if (n.Contains("metal") || n.Contains("estanteria") || n.Contains("tuberia") || n.Contains("estructura") || n.Contains("transformador")
                || n.Contains("casco") || n.Contains("marco") || n.Contains("puerta") || n.Contains("mesa") || n.Contains("chatarra")
                || n.Contains("herramienta") || n.Contains("camara") || n.Contains("capsula"))
                return SurfaceKind.Rust;
            return SurfaceKind.Skip;
        }

        /// <summary>Repeticiones de la textura por cara de objeto.</summary>
        public static float Tiling(SurfaceKind k) =>
            k == SurfaceKind.Floor ? 4f : k == SurfaceKind.Wall ? 2f : k == SurfaceKind.Rust ? 1.5f : 1f;
    }
}
