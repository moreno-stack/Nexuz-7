using UnityEngine;

namespace Nexus7.Director
{
    /// <summary>
    /// La tensión del director pasa de una fase a la siguiente (en memoria): si dominas la Fase 2, la Fase 3 no empieza
    /// en el nivel de diseño sino donde lo dejaste. Nueva partida → vuelve a la base de la dificultad elegida.
    /// </summary>
    public static class DirectorSession
    {
        public static bool HasTension { get; private set; }
        public static float Tension { get; private set; }

        public static void Store(float tension) { Tension = tension; HasTension = true; }
        public static void Clear() { HasTension = false; Tension = 0f; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => Clear();
    }
}
