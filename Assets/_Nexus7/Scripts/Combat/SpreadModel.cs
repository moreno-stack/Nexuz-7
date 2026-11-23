using System;

namespace Nexus7.Combat
{
    /// <summary>
    /// Dispersión de los disparos (pura, probada): cono en grados según si se apunta, si el jugador se mueve
    /// y lo que se ha abierto el arma disparando seguido. Las direcciones se devuelven como desvío (guiñada, cabeceo)
    /// en grados respecto al centro de la mira, siempre dentro del cono.
    /// </summary>
    public static class SpreadModel
    {
        /// <summary>Moverse sin apuntar abre la dispersión un 40 %; apuntando, solo un 10 %.</summary>
        public const float MovingHipFactor = 1.4f;
        public const float MovingAimFactor = 1.1f;

        public static float Current(PlayerWeaponSpec spec, bool aiming, bool moving, float bloom)
        {
            float b = aiming ? spec.AimSpread : spec.HipSpread;
            if (moving) b *= aiming ? MovingAimFactor : MovingHipFactor;
            return b + Math.Max(0f, bloom);
        }

        /// <summary>
        /// Desvío aleatorio uniforme dentro de un cono de 'spreadDeg' grados. u y v en [0, 1).
        /// (sqrt(u) reparte los disparos por toda el área del círculo, no amontonados en el centro).
        /// </summary>
        public static void Offset(float spreadDeg, double u, double v, out float yawDeg, out float pitchDeg)
        {
            double r = spreadDeg * Math.Sqrt(Math.Max(0.0, Math.Min(1.0, u)));
            double a = 2.0 * Math.PI * v;
            yawDeg = (float)(r * Math.Cos(a));
            pitchDeg = (float)(r * Math.Sin(a));
        }

        /// <summary>
        /// Perdigón 'index' de 'count' (escopeta): uno en el centro y los demás repartidos en anillo con algo de azar,
        /// así el patrón es reconocible y justo (no deja huecos enormes por mala suerte).
        /// </summary>
        public static void PelletOffset(int index, int count, float spreadDeg, double jitterU, double jitterV,
                                        out float yawDeg, out float pitchDeg)
        {
            if (count <= 1 || index == 0) { Offset(spreadDeg * 0.25f, jitterU, jitterV, out yawDeg, out pitchDeg); return; }
            int ring = count - 1;
            double a = 2.0 * Math.PI * ((index - 1) + 0.35 * (jitterV - 0.5)) / ring;
            double r = spreadDeg * (0.65 + 0.35 * Math.Max(0.0, Math.Min(1.0, jitterU)));
            yawDeg = (float)(r * Math.Cos(a));
            pitchDeg = (float)(r * Math.Sin(a));
        }
    }
}
