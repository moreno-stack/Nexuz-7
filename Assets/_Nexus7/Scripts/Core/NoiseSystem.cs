using System;
using UnityEngine;

namespace Nexus7.Core
{
    /// <summary>
    /// Bus global de ruido. Cualquier objeto (el jugador al dar un paso, una puerta al abrirse,
    /// un objeto que cae...) puede emitir ruido, y cualquier sistema (en fases futuras: enemigos,
    /// cámaras de seguridad) puede suscribirse para "escucharlo".
    ///
    /// Uso:
    ///   NoiseSystem.Emit(transform.position, 6f, gameObject);
    ///   NoiseSystem.NoiseEmitted += (pos, radio, fuente) => { ... };
    /// </summary>
    public static class NoiseSystem
    {
        /// <summary>Se dispara cada vez que algo hace ruido: (posición, radio audible en metros, objeto que lo emitió).</summary>
        public static event Action<Vector3, float, GameObject> NoiseEmitted;

        public static void Emit(Vector3 position, float radius, GameObject source)
        {
            if (radius <= 0f) return;
            NoiseEmitted?.Invoke(position, radius, source);
        }
    }
}
