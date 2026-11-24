using System;

namespace Nexus7.Combat
{
    /// <summary>Dónde ha dado un disparo a un robot.</summary>
    public enum HitZone { Body, Optic, Core, Arm }

    /// <summary>
    /// Puntos débiles de los robots (puro, probado):
    ///   · ÓPTICA (cabeza, todos): daño ×2 y la deslumbra: 2 s con +6° de dispersión y 0,8 s sin poder empezar ráfagas.
    ///   · NÚCLEO TRASERO (solo élite): daño ×3 si el disparo entra por la espalda (dirección del disparo a menos de 60°
    ///     de hacia donde mira el robot) a la altura del torso (0,7–1,5 m). Premia flanquear a la élite.
    ///   · BRAZO DEL ARMA (robots tiradores): impactos a menos de 0,25 m del antebrazo o la mano derechos.
    ///     Daño normal al robot y además al brazo (WeaponArmModel): con 30 de daño queda desarmado.
    /// Prioridad: óptica &gt; brazo &gt; núcleo &gt; cuerpo.
    /// </summary>
    public static class WeakPointModel
    {
        public const float OpticMultiplier = HealthModel.CriticalMultiplier, CoreMultiplier = 3f;
        public const float OpticMargin = 0.2f, CoreMinHeight = 0.7f, CoreMaxHeight = 1.5f, CoreMinDot = 0.5f;
        public const float DazzleSeconds = 2f, DazzleNoBurstSeconds = 0.8f;
        public const float ArmRadius = 0.25f;

        /// <param name="distanceToArm">Distancia del impacto al antebrazo/mano del arma (m). Se ignora si no tiene brazo armado.</param>
        public static HitZone Classify(bool hasCore, bool hasArm, float distanceToArm, float heightAboveFeet, float headHeight,
                                       float dirX, float dirZ, float fwdX, float fwdZ)
        {
            var zone = Classify(hasCore, heightAboveFeet, headHeight, dirX, dirZ, fwdX, fwdZ);
            if (zone == HitZone.Optic || !hasArm || distanceToArm > ArmRadius) return zone;
            return HitZone.Arm;
        }

        /// <param name="heightAboveFeet">Altura del impacto sobre los pies del robot (m).</param>
        /// <param name="headHeight">Altura de la cabeza sobre los pies (m); ≤ 0 si no tiene.</param>
        /// <param name="dirX">Dirección del disparo (no hace falta normalizar).</param>
        /// <param name="fwdX">Hacia dónde mira el robot.</param>
        public static HitZone Classify(bool hasCore, float heightAboveFeet, float headHeight,
                                       float dirX, float dirZ, float fwdX, float fwdZ)
        {
            if (headHeight > 0f && heightAboveFeet >= headHeight - OpticMargin) return HitZone.Optic;
            if (!hasCore || heightAboveFeet < CoreMinHeight || heightAboveFeet > CoreMaxHeight) return HitZone.Body;
            double dl = Math.Sqrt(dirX * dirX + dirZ * dirZ), fl = Math.Sqrt(fwdX * fwdX + fwdZ * fwdZ);
            if (dl < 1e-4 || fl < 1e-4) return HitZone.Body;
            double dot = (dirX * fwdX + dirZ * fwdZ) / (dl * fl);   // el disparo va en el mismo sentido que su mirada = por detrás
            return dot >= CoreMinDot ? HitZone.Core : HitZone.Body;
        }

        public static float Multiplier(HitZone zone) =>
            zone == HitZone.Optic ? OpticMultiplier : zone == HitZone.Core ? CoreMultiplier : 1f;
    }
}
