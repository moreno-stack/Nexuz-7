using System;
using UnityEngine;

namespace Nexus7.Core
{
    /// <summary>
    /// Avisos de «el jugador ha cumplido algo importante» (hackeo con éxito, terminal de acceso, consola de energía,
    /// acción de la consola de mando, paso de la secuencia de lanzamiento, punto seguro). Los encuentros por oleadas
    /// (EncounterDirector) se activan con ellos. Quien lo cumple no sabe nada de los encuentros.
    /// </summary>
    public static class ObjectiveEvents
    {
        /// <summary>(tipo, posición). Tipos: "hackeo", "acceso", "energia", "consola", "lanzamiento", "punto_seguro".</summary>
        public static event Action<string, Vector3> Completed;

        public static void Raise(string kind, Vector3 position) => Completed?.Invoke(kind, position);
    }
}
