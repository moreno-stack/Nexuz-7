using UnityEngine;

namespace Nexus7.Director
{
    /// <summary>
    /// La tensión del director pasa de una fase a la siguiente (en memoria): si dominas la Fase 2, la Fase 3 no empieza
    /// en el nivel de diseño sino donde lo dejaste. Nueva partida → vuelve a la base de la dificultad elegida.
    /// También guarda el informe de la IA de la fase que acabas de dejar, para enseñarlo al entrar en la siguiente.
    /// </summary>
    public static class DirectorSession
    {
        public static bool HasTension { get; private set; }
        public static float Tension { get; private set; }

        public static void Store(float tension) { Tension = tension; HasTension = true; }
        public static void Clear() { HasTension = false; Tension = 0f; report = null; reportScene = null; HeatmapDirector.ClearAll(); }

        private static PhaseReportModel report;
        private static string reportScene;

        /// <summary>Al salir de una fase (GameDirector.OnDestroy).</summary>
        public static void StoreReport(PhaseReportModel r, string scene) { report = r; reportScene = scene; }

        /// <summary>
        /// El informe pendiente, solo si 'scene' es la misma fase (reinicio) o la siguiente; se consume al leerlo.
        /// Así una partida nueva o un salto de fase no enseña un informe viejo.
        /// </summary>
        public static PhaseReportModel TakeReport(string scene, out bool sameScene)
        {
            var r = report;
            sameScene = reportScene == scene;
            bool next = reportScene != null && Nexus7.Core.GameFlow.PhaseIndex(scene) == Nexus7.Core.GameFlow.PhaseIndex(reportScene) + 1
                        && Nexus7.Core.GameFlow.PhaseIndex(scene) > 0;
            report = null;
            reportScene = null;
            return r != null && (sameScene || next) ? r : null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => Clear();
    }
}
