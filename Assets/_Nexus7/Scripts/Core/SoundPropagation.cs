using System;
using UnityEngine;

namespace Nexus7.Core
{
    /// <summary>
    /// Cómo se propaga un sonido de NoiseSystem hasta un oyente (sin depender del motor, se prueba fuera de Unity):
    ///   · cada pared o caja entre el sonido y el oyente reduce el radio a la mitad (se cuentan como mucho 2);
    ///   · el oyente no sabe el punto exacto: estima la posición con un error de hasta el 25 % de la distancia.
    /// </summary>
    public static class SoundPropagation
    {
        public const float WallFactor = 0.5f;
        public const int MaxBlockers = 2;
        public const float PositionErrorFraction = 0.25f;

        /// <summary>Radio audible tras atravesar 'blockers' obstáculos.</summary>
        public static float EffectiveRadius(float radius, int blockers)
        {
            int b = Math.Max(0, Math.Min(MaxBlockers, blockers));
            return radius * (float)Math.Pow(WallFactor, b);
        }

        /// <summary>
        /// Cuántos obstáculos distintos hay a lo largo del rayo: impactos a menos de 'mergeDistance' metros
        /// unos de otros cuentan como uno (una pared hecha de varias cajas de colisión). Ordena 'distances' (primeros 'count').
        /// </summary>
        public static int CountObstacles(float[] distances, int count, float mergeDistance)
        {
            if (count <= 0) return 0;
            Array.Sort(distances, 0, count);
            int obstacles = 1;
            for (int i = 1; i < count && obstacles < MaxBlockers; i++)
                if (distances[i] - distances[i - 1] > mergeDistance) obstacles++;
            return obstacles;
        }

        /// <summary>¿Llega el sonido al oyente? 'hearingMultiplier' = oído del robot × alerta de la nave.</summary>
        public static bool CanHear(float distance, float radius, int blockers, float hearingMultiplier) =>
            radius > 0f && distance <= EffectiveRadius(radius, blockers) * hearingMultiplier;

        /// <summary>
        /// Dónde cree el oyente que sonó: el punto real desplazado en horizontal hasta un 25 % de la distancia.
        /// u (0–1) = proporción del error máximo, v (0–1) = dirección. En juego son números aleatorios.
        /// </summary>
        public static Vector3 EstimatePosition(Vector3 source, Vector3 listener, float u, float v)
        {
            float dx = source.x - listener.x, dy = source.y - listener.y, dz = source.z - listener.z;
            float d = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
            float err = d * PositionErrorFraction * Math.Max(0f, Math.Min(1f, u));
            double a = v * 2.0 * Math.PI;
            return new Vector3(source.x + err * (float)Math.Cos(a), source.y, source.z + err * (float)Math.Sin(a));
        }
    }
}
