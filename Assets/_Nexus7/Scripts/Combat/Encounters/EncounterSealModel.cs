using System;

namespace Nexus7.Combat
{
    /// <summary>
    /// Sello de la salida durante un encuentro (puro, probado en EncounterSealTest): mientras hay oleadas, la IA bloquea la
    /// salida de la fase para que no se pueda huir a la siguiente con los refuerzos detrás.
    ///   · Se sella en la cuenta atrás y durante las oleadas; se libera al despejar o al caer (el encuentro se reinicia).
    ///   · No sella en Fácil ni en la última vida (director clemente).
    ///   · Como mucho MaxSeal segundos por intento: si te quedas sin poder acabar, la salida se abre igual.
    /// Solo afecta a la zona de salida (SceneExitTrigger): las puertas de las salas no se tocan, porque cerrarlas
    /// cortaría el camino de los robots de la oleada.
    /// </summary>
    public static class EncounterSealModel
    {
        public const float MaxSeal = 150f;

        /// <summary>¿Este encuentro sella la salida? (no en Fácil ni con la última vida).</summary>
        public static bool ShouldSeal(int difficulty, bool lastLife) => difficulty > 0 && !lastLife;

        /// <summary>¿La salida está sellada ahora? 'since' es cuándo se activó el encuentro en este intento.</summary>
        public static bool IsSealed(EncounterState state, bool sealsThisAttempt, float since, float now)
        {
            if (!sealsThisAttempt) return false;
            if (state != EncounterState.Countdown && state != EncounterState.Active) return false;
            return now - since < MaxSeal;
        }

        /// <summary>Segundos que quedan de sello (0 si ya no está sellada).</summary>
        public static float Remaining(float since, float now) => Math.Max(0f, MaxSeal - (now - since));

        /// <summary>Lo que se dice al tocar la salida sellada.</summary>
        public static string Message(int wave, int waveCount, float remaining)
        {
            string w = wave >= 1 && waveCount >= 1 ? $" (oleada {wave}/{waveCount})" : "";
            return $"SALIDA SELLADA: la IA ha bloqueado la salida durante el encuentro. Acaba con los refuerzos{w}; si no, se abre sola en {remaining:0} s.";
        }
    }
}
