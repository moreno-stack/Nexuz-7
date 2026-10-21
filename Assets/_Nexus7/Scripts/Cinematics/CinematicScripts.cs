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

        /// <summary>Fase 3 — «Energía» (19 s): el reactor apagado, un guardia con llave inglesa y la barrera.</summary>
        public static List<CinematicShot> Energia()
        {
            return new List<CinematicShot>
            {
                Shot(new Vector3(0f, 3.2f, 1.5f), new Vector3(0f, 3.6f, 8f), new Vector3(0f, 1.5f, 30f), new Vector3(0f, 1.5f, 34f), 7f, 0f,
                     new SubtitleLine("IA NEXUS-7", "Sala de Energía. El reactor está en parada y la nave funciona con energía de emergencia.", 0.3f, 3.3f),
                     new SubtitleLine("IA NEXUS-7", "Necesitas 4 bloques en la red: dos zócalos de batería, el interruptor del ala este y el reactor.", 3.8f, 3.0f)),
                Shot(new Vector3(-3.8f, 1.8f, 25.6f), new Vector3(-4.3f, 1.6f, 25.2f), new Vector3(-6f, 1.4f, 25f), new Vector3(-6f, 1.4f, 25f), 6f, 0f,
                     new SubtitleLine("IA NEXUS-7", "Un robot guardia armado con una llave inglesa gigante. No se aparta de las consolas.", 0.2f, 3.4f),
                     new SubtitleLine("Tú", "Pasaré por arriba, por las pasarelas.", 3.9f, 1.9f)),
                Shot(new Vector3(-12f, 2f, 5f), new Vector3(-13.5f, 1.8f, 6.5f), new Vector3(-16f, 1.5f, 10f), new Vector3(-16f, 1.5f, 10f), 6f, 0f,
                     new SubtitleLine("IA NEXUS-7", "La barrera del ala oeste se puede hackear. En el banco de trabajo puedes fabricar kits de reparación.", 0.2f, 3.6f),
                     new SubtitleLine("Tú", "Energía, reactor y salida. Entendido.", 4.0f, 1.8f)),
            };
        }

        /// <summary>Fase 4 — «Laboratorio» (19 s): los tanques, un robot de investigación con porra y el banco de trabajo.</summary>
        public static List<CinematicShot> Laboratorio()
        {
            return new List<CinematicShot>
            {
                Shot(new Vector3(-8f, 2.4f, 10.5f), new Vector3(-9.5f, 2.0f, 13f), new Vector3(-13.5f, 1.5f, 14f), new Vector3(-13.5f, 1.5f, 19f), 7f, 0f,
                     new SubtitleLine("IA NEXUS-7", "Laboratorio del proyecto ESPECTRO. Busca cristales ópticos por la sala: brillan y sirven para el camuflaje.", 0.3f, 3.3f),
                     new SubtitleLine("IA NEXUS-7", "La puerta de contención del norte exige la tarjeta del director.", 3.8f, 2.9f)),
                Shot(new Vector3(-6.4f, 1.7f, 14.6f), new Vector3(-6.8f, 1.5f, 14.0f), new Vector3(-7f, 1.2f, 12.5f), new Vector3(-7f, 1.2f, 12.5f), 6f, 0f,
                     new SubtitleLine("IA NEXUS-7", "Robot de investigación con porra eléctrica: si te alcanza, te deja aturdido.", 0.2f, 3.3f),
                     new SubtitleLine("Tú", "Y revisa los bancos de trabajo... Tomo nota.", 3.8f, 2.0f)),
                Shot(new Vector3(6f, 1.9f, 2.4f), new Vector3(7f, 1.7f, 3.0f), new Vector3(8f, 1.0f, 5f), new Vector3(8f, 1.0f, 5f), 6f, 0f,
                     new SubtitleLine("IA NEXUS-7", "Este banco nadie lo revisa: fabrica aquí señuelos, pulsos PEM, camuflaje o kits.", 0.2f, 3.6f),
                     new SubtitleLine("Tú", "A trabajar.", 4.0f, 1.6f)),
            };
        }

        /// <summary>Fase 5 — «Centro de Control» (19 s): el holograma, la pareja de élite con hachas y la compuerta blindada.</summary>
        public static List<CinematicShot> CentroControl()
        {
            return new List<CinematicShot>
            {
                Shot(new Vector3(0f, 3.5f, 4f), new Vector3(-2f, 3f, 8f), new Vector3(0f, 2.6f, 16f), new Vector3(0f, 2.6f, 16f), 7f, 0f,
                     new SubtitleLine("IA NEXUS-7", "Centro de Control: desde aquí se gobierna toda la nave. La seguridad responde al nivel de alerta.", 0.3f, 3.4f),
                     new SubtitleLine("IA NEXUS-7", "Las consolas desactivan sectores y autorizan compuertas... si superas el hackeo.", 3.9f, 2.9f)),
                Shot(new Vector3(-6.6f, 1.8f, 10.4f), new Vector3(-7f, 1.6f, 9.8f), new Vector3(-8f, 1.5f, 8f), new Vector3(-8f, 1.5f, 8f), 6f, 0f,
                     new SubtitleLine("IA NEXUS-7", "Robots de élite con hachas. Patrullan en pareja y en Alerta Roja te cazarán.", 0.2f, 3.3f),
                     new SubtitleLine("Tú", "Un golpe de esos y estoy fuera.", 3.8f, 2.0f)),
                Shot(new Vector3(0f, 2.4f, 21.5f), new Vector3(0f, 2.0f, 24.5f), new Vector3(0f, 2f, 30f), new Vector3(0f, 2f, 30f), 6f, 0f,
                     new SubtitleLine("IA NEXUS-7", "La compuerta blindada del puente se autoriza desde el ala oeste y se sella en Alerta Roja.", 0.2f, 3.6f),
                     new SubtitleLine("Tú", "Puente de mando. Ya casi estoy.", 4.0f, 1.8f)),
            };
        }

        /// <summary>Fase 6 — «Autodestrucción» (19 s): el hangar, la pareja de élite y las cápsulas A, B y C.</summary>
        public static List<CinematicShot> Autodestruccion()
        {
            return new List<CinematicShot>
            {
                Shot(new Vector3(0f, 6f, 2f), new Vector3(0f, 7f, 8f), new Vector3(0f, 3f, 18f), new Vector3(0f, 3f, 22f), 7f, 0.1f,
                     new SubtitleLine("IA NEXUS-7", "Hangar de evacuación. Autodestrucción activada: tendrás cinco minutos para salir.", 0.3f, 3.2f),
                     new SubtitleLine("IA NEXUS-7", "La cápsula A está destruida y la C tiene las abrazaderas fundidas.", 3.8f, 3.0f)),
                Shot(new Vector3(-7.5f, 1.8f, 10.5f), new Vector3(-8f, 1.6f, 9.8f), new Vector3(-9f, 1.5f, 8f), new Vector3(-9f, 1.5f, 8f), 6f, 0f,
                     new SubtitleLine("IA NEXUS-7", "Los robots de élite vigilan la lanzadera. El humo te ocultará.", 0.2f, 3.2f),
                     new SubtitleLine("Tú", "Cinco minutos. Ni uno más.", 3.7f, 2.0f)),
                Shot(new Vector3(0f, 6.5f, 30f), new Vector3(0f, 6f, 33f), new Vector3(0f, 5.4f, 38f), new Vector3(0f, 5.4f, 38f), 6f, 0f,
                     new SubtitleLine("IA NEXUS-7", "La cápsula B es tu única salida: libera las abrazaderas y carga la celda de ignición.", 0.2f, 3.6f),
                     new SubtitleLine("Tú", "Vamos a casa.", 4.0f, 1.6f)),
            };
        }

        /// <summary>Explosión del reactor en cada final: centro y escala final de la esfera (diámetro).</summary>
        /// La cámara siempre queda fuera de la esfera (el material solo se ve por fuera).
        public static readonly Vector3 DespegueBlastCenter = new Vector3(0f, 7f, 20f);
        public const float DespegueBlastScale = 100f;   // radio 50: la cámara del plano 2 está a 63–71 m
        public static readonly Vector3 DerrotaBlastCenter = new Vector3(0f, 6f, 34f);
        public const float DerrotaBlastScale = 28f;     // radio 14 en la pasarela: la cámara está a 24–28 m

        /// <summary>Final — «Despegue» (14 s): exterior, la cápsula se separa y la nave explota.</summary>
        public static List<CinematicShot> Despegue()
        {
            return new List<CinematicShot>
            {
                Shot(new Vector3(14f, 9f, 62f), new Vector3(10f, 8f, 70f), new Vector3(0f, 6f, 44f), new Vector3(0f, 6f, 60f), 6f, 0f,
                     new SubtitleLine("IA NEXUS-7", "Cápsula B separada del Nexus-7. Rumbo a la estación de rescate.", 0.3f, 3.0f),
                     new SubtitleLine("Tú", "Adiós, Nexus-7.", 3.6f, 2.0f)),
                Shot(new Vector3(30f, 14f, 75f), new Vector3(34f, 16f, 82f), new Vector3(0f, 7f, 25f), new Vector3(0f, 7f, 25f), 8f, 0.3f,
                     new SubtitleLine("IA NEXUS-7", "Detonación del reactor confirmada. La nave ya no existe.", 1.0f, 3.2f),
                     new SubtitleLine("IA NEXUS-7", "Has sobrevivido, tripulante.", 4.6f, 2.6f)),
            };
        }

        /// <summary>Final — «Derrota» (7 s): la nave se desintegra contigo dentro.</summary>
        public static List<CinematicShot> Derrota()
        {
            return new List<CinematicShot>
            {
                Shot(new Vector3(0f, 8f, 6f), new Vector3(0f, 9f, 10f), new Vector3(0f, 4f, 30f), new Vector3(0f, 4f, 30f), 7f, 0.4f,
                     new SubtitleLine("IA NEXUS-7", "Fallo de contención del reactor...", 0.3f, 2.4f),
                     new SubtitleLine("Tú", "No... todavía no...", 3.0f, 2.0f)),
            };
        }
    }
}
