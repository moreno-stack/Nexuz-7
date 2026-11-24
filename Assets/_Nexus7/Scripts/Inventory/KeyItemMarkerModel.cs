using System;

namespace Nexus7.Inventory
{
    /// <summary>
    /// Marcador en pantalla de los objetos imprescindibles (puro, probado en KeyItemMarkerTest). Las salas son muy oscuras
    /// y el botín cambia de sitio en cada partida, así que el traje marca cada objeto imprescindible con un cuadro de su color, su nombre
    /// y los metros, también a través de las paredes. Si está fuera de la vista o a tu espalda, el cuadro se queda en el
    /// borde de la pantalla, del lado hacia el que tienes que girarte.
    /// </summary>
    public static class KeyItemMarkerModel
    {
        public const float Margin = 40f;

        /// <summary>
        /// Coloca el marcador. (sx, sy) es la posición en pantalla con el origen arriba a la izquierda y 'depth' la
        /// distancia por delante de la cámara (negativa si está detrás). Devuelve true si está dentro de la vista.
        /// </summary>
        public static bool Place(float sx, float sy, float depth, float width, float height, out float x, out float y)
        {
            float cx = width * 0.5f, cy = height * 0.5f;
            float dx = sx - cx, dy = sy - cy;
            bool behind = depth <= 0f;
            if (behind) { dx = -dx; dy = -dy; if (Math.Abs(dx) < 0.01f && Math.Abs(dy) < 0.01f) dy = cy; }   // justo detrás: abajo
            bool inside = !behind && sx >= Margin && sx <= width - Margin && sy >= Margin && sy <= height - Margin;
            if (inside) { x = sx; y = sy; return true; }
            float hx = Math.Max(1f, cx - Margin), hy = Math.Max(1f, cy - Margin);
            float scale = Math.Min(Math.Abs(dx) > 0.001f ? hx / Math.Abs(dx) : float.MaxValue,
                                   Math.Abs(dy) > 0.001f ? hy / Math.Abs(dy) : float.MaxValue);
            x = cx + dx * scale;
            y = cy + dy * scale;
            return false;
        }

        public static string Label(string itemName, float distance) => $"{itemName} · {distance:0} m";
    }
}
