using System;
using UnityEngine;

namespace Nexus7.Core
{
    /// <summary>
    /// Bus global de alarmas. Una cámara de seguridad (u otro sensor) llama a Raise() y
    /// todos los drones dentro del radio acuden corriendo a investigar la posición.
    /// </summary>
    public static class AlarmSystem
    {
        /// <summary>(posición del intruso, radio de alcance de la alarma)</summary>
        public static event Action<Vector3, float> AlarmRaised;

        public static void Raise(Vector3 position, float radius) => AlarmRaised?.Invoke(position, radius);
    }
}
