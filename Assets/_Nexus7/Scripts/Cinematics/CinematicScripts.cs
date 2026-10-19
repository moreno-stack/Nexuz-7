using System.Collections.Generic;
using UnityEngine;

namespace Nexus7.Cinematics
{
    /// <summary>
    /// Guiones de las cinemáticas (planos de cámara + subtítulos en español), en coordenadas de cada sala.
    /// Los constructores de escena los copian al CinematicDirector; aquí se pueden probar sin Unity.
    /// </summary>
    public static class CinematicScripts
    {
        private static CinematicShot Shot(Vector3 fromPos, Vector3 toPos, Vector3 fromLook, Vector3 toLook, float duration, float shake,
                                          params SubtitleLine[] lines)
        {
            return new CinematicShot
            {
                fromPos = fromPos, toPos = toPos, fromLook = fromLook, toLook = toLook,
                duration = duration, shake = shake, lines = new List<SubtitleLine>(lines)
            };
        }

        /// <summary>Fase 1 — «Despertar» (19 s): cápsula criogénica, corredor hasta la puerta C-7 y taquilla del traje.</summary>
        public static List<CinematicShot> Despertar()
        {
            return new List<CinematicShot>
            {
                Shot(new Vector3(0.6f, 2.4f, 0.4f), new Vector3(-0.3f, 1.7f, 0.9f), new Vector3(-1.3f, 0.5f, 1.4f), new Vector3(-1.3f, 0.6f, 1.4f), 6f, 0.05f,
                     new SubtitleLine("IA NEXUS-7", "Reanimación de emergencia completada. Tripulante, ¿me recibe?", 0.3f, 2.8f),
                     new SubtitleLine("Tú", "...¿Dónde está todo el mundo?", 3.2f, 2.5f)),
                Shot(new Vector3(0f, 2.2f, 2.2f), new Vector3(0f, 2.0f, 9f), new Vector3(0f, 1.4f, 20f), new Vector3(0f, 1.3f, 20f), 7f, 0f,
                     new SubtitleLine("IA NEXUS-7", "Los robots de mantenimiento se han rebelado. Ahora van armados y patrullan la nave.", 0.2f, 3.3f),
                     new SubtitleLine("IA NEXUS-7", "La puerta C-7 no tiene energía. Hay una terminal de mantenimiento en este corredor.", 3.6f, 3.2f)),
                Shot(new Vector3(0f, 1.7f, 1.6f), new Vector3(0.5f, 1.6f, 2.6f), new Vector3(1.6f, 1.2f, 3.4f), new Vector3(1.6f, 1.1f, 3.4f), 6f, 0f,
                     new SubtitleLine("IA NEXUS-7", "Antes de salir, ponte el traje espacial de esa taquilla: su escudo te protegerá de los golpes.", 0.2f, 3.2f),
                     new SubtitleLine("Tú", "Entendido. Vamos allá.", 3.6f, 2.2f)),
            };
        }

        /// <summary>Fase 2 — «Almacén» (19 s): recorrido aéreo, robot de seguridad con bate y el montacargas al fondo.</summary>
        public static List<CinematicShot> Almacen()
        {
            return new List<CinematicShot>
            {
                Shot(new Vector3(0f, 5.2f, 1f), new Vector3(0f, 5.0f, 13f), new Vector3(0f, 0.5f, 12f), new Vector3(0f, 0.5f, 27f), 7f, 0f,
                     new SubtitleLine("IA NEXUS-7", "Almacén de Carga del Módulo C. El montacargas del muro norte no tiene energía.", 0.3f, 3.2f),
                     new SubtitleLine("IA NEXUS-7", "Necesitará dos baterías de iones. Hay suministros repartidos por todo el almacén.", 3.7f, 3.1f)),
                Shot(new Vector3(-5.2f, 1.7f, 6.2f), new Vector3(-5.6f, 1.5f, 5.5f), new Vector3(-6f, 1.3f, 4f), new Vector3(-6f, 1.2f, 4f), 6f, 0f,
                     new SubtitleLine("IA NEXUS-7", "Cuidado: un robot de seguridad armado con un bate. Si te alcanza, te golpeará.", 0.2f, 3.3f),
                     new SubtitleLine("Tú", "Entonces mejor que no me vea.", 3.8f, 2.0f)),
                Shot(new Vector3(5.5f, 2.6f, 23.5f), new Vector3(7.5f, 2.2f, 26.5f), new Vector3(10f, 1.6f, 30.2f), new Vector3(10f, 1.6f, 30.2f), 6f, 0f,
                     new SubtitleLine("IA NEXUS-7", "Agáchate en las zonas oscuras. En el banco de trabajo puedes fabricar un señuelo.", 0.2f, 3.6f),
                     new SubtitleLine("Tú", "Baterías, montacargas... y nada de correr.", 4.0f, 1.9f)),
            };
        }
    }
}
