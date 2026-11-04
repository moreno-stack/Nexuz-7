using System;
using UnityEngine;
using Nexus7.Core;
using Nexus7.Gameplay;

namespace Nexus7.UI
{
    /// <summary>
    /// Lógica pura de las animaciones y colores de las pantallas de inicio y final
    /// (entradas, barras escalonadas, pulsos, estrellas del rango, vaivén de la cámara del menú).
    /// </summary>
    public static class ScreenFx
    {
        private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;

        /// <summary>Progreso de una entrada (0 → 1) con retraso y frenada suave al final.</summary>
        public static float Intro(float t, float delay, float duration)
        {
            if (duration <= 0f) return t >= delay ? 1f : 0f;
            float k = Clamp01((t - delay) / duration);
            float r = 1f - k;
            return 1f - r * r * r;
        }

        /// <summary>Desplazamiento que queda por recorrer en una entrada deslizante.</summary>
        public static float Slide(float t, float delay, float duration, float distance) => distance * (1f - Intro(t, delay, duration));

        public const float StaggerStart = 0.4f, StaggerStep = 0.25f, FillDuration = 0.9f;

        /// <summary>Relleno de la barra 'index' en el instante t: se llenan una tras otra hasta su valor (0–1).</summary>
        public static float StaggeredFill(float t, int index, float target) =>
            Clamp01(target) * Intro(t, StaggerStart + index * StaggerStep, FillDuration);

        /// <summary>Instante en que termina de llenarse la última de 'count' barras.</summary>
        public static float FillsEnd(int count) => StaggerStart + (count - 1) * StaggerStep + FillDuration;

        /// <summary>¿Han terminado de llenarse las 'count' barras?</summary>
        public static bool FillsDone(float t, int count) => t >= FillsEnd(count);

        /// <summary>Pulso suave entre 0 y 1 ('speed' ciclos por segundo).</summary>
        public static float Pulse(float t, float speed) => 0.5f + 0.5f * (float)Math.Cos(t * speed * 2.0 * Math.PI);

        /// <summary>Opacidad del destello que recorre la línea del título: 0 en los extremos, 1 en el centro.</summary>
        public static float SweepAlpha(float k) => (float)Math.Sin(Clamp01(k) * Math.PI);

        /// <summary>Estrellas del rango: S 3 · A 2 · B 1 · C y D ninguna.</summary>
        public static int GradeStars(string grade) =>
            grade == "S" ? 3 : grade == "A" ? 2 : grade == "B" ? 1 : 0;

        public static Color GradeColor(string grade) =>
            grade == "S" ? new Color(1f, 0.85f, 0.3f) : grade == "A" ? new Color(0.4f, 1f, 0.7f)
          : grade == "B" ? new Color(0.4f, 0.8f, 1f) : grade == "C" ? new Color(1f, 0.7f, 0.3f) : new Color(1f, 0.35f, 0.3f);

        /// <summary>Color de la banda de la cabecera según el final.</summary>
        public static Color EndingColor(EndingType e) =>
            e == EndingType.Optimal ? new Color(0.4f, 1f, 0.69f) : e == EndingType.Tactical ? new Color(1f, 0.76f, 0.23f) : new Color(1f, 0.31f, 0.25f);

        public static string EndingTag(EndingType e) =>
            e == EndingType.Optimal ? "ÓPTIMO" : e == EndingType.Tactical ? "TÁCTICO" : "CRÍTICO";

        /// <summary>Marcas de «nivel de peligro» en la tarjeta de cada dificultad.</summary>
        public static int DangerPips(Difficulty d) => (int)d + 1;

        public static float CardScale(bool selected) => selected ? 1.06f : 1f;

        /// <summary>Vaivén de la cámara del menú: empieza quieto y crece en 3 s hasta unos 25 cm.</summary>
        public static Vector3 MenuCameraOffset(float t)
        {
            float a = 0.25f * Intro(t, 0f, 3f);
            return new Vector3((float)Math.Sin(t * 0.21 * 2 * Math.PI) * a,
                               (float)Math.Sin(t * 0.13 * 2 * Math.PI + 1.0) * a * 0.5f,
                               (float)Math.Sin(t * 0.08 * 2 * Math.PI + 2.0) * a * 0.4f);
        }

        /// <summary>Giro lento (grados) de la cámara del menú.</summary>
        public static float MenuCameraYaw(float t) => 2f * (float)Math.Sin(t * 0.05 * 2 * Math.PI) * Intro(t, 0f, 3f);

        /// <summary>Luces de navegación de la nave: encendidas 0,2 s de cada segundo.</summary>
        public static float NavBlink(float t)
        {
            float r = t - (float)Math.Floor(t);
            return r < 0.2f ? 1f : 0f;
        }
    }
}
