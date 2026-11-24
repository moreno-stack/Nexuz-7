using System;

namespace Nexus7.Inventory
{
    /// <summary>
    /// Señal del traje hacia los objetos imprescindibles que siguen en el suelo (pura, probada en KeyItemSignalTest).
    /// A los 40 s de empezar la sala y luego cada 60 s, el traje dice a cuántos metros y en qué dirección está el más cercano:
    /// «TRAJE: señal de Batería de iones a 14 m al noreste.» El norte es +Z y el este es +X (como en las guías de las fases).
    /// </summary>
    public static class KeyItemSignal
    {
        public const float FirstDelay = 40f, Repeat = 60f, NearEnough = 3f;

        private static readonly string[] Dirs = { "norte", "noreste", "este", "sureste", "sur", "suroeste", "oeste", "noroeste" };

        /// <summary>Dirección cardinal (8 rumbos) de un desplazamiento en el suelo (dx hacia el este, dz hacia el norte).</summary>
        public static string Direction(float dx, float dz)
        {
            double deg = Math.Atan2(dx, dz) * 180.0 / Math.PI;   // 0 = norte, 90 = este
            if (deg < 0) deg += 360.0;
            int i = (int)Math.Floor((deg + 22.5) / 45.0) % 8;
            return Dirs[i];
        }

        /// <summary>Mensaje del traje, o null si ya está al lado (no hace falta avisar).</summary>
        public static string Message(string itemName, float dx, float dz)
        {
            float d = (float)Math.Sqrt(dx * dx + dz * dz);
            if (d < NearEnough) return null;
            return $"TRAJE: señal de {itemName} a {d:0} m al {Direction(dx, dz)}.";
        }
    }
}
