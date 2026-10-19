using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nexus7.Cinematics
{
    /// <summary>Una línea de subtítulo: quién habla, qué dice, cuándo aparece (s desde el inicio del plano) y cuánto dura.</summary>
    [Serializable]
    public class SubtitleLine
    {
        public string speaker = "IA NEXUS-7";
        [TextArea(1, 3)] public string text;
        public float at;
        public float hold = 3f;

        public SubtitleLine() { }
        public SubtitleLine(string speaker, string text, float at, float hold)
        {
            this.speaker = speaker; this.text = text; this.at = at; this.hold = hold;
        }
    }

    /// <summary>Un plano de cámara: de (fromPos → toPos) mirando de (fromLook → toLook) durante 'duration' segundos.</summary>
    [Serializable]
    public class CinematicShot
    {
        public Vector3 fromPos, toPos, fromLook, toLook;
        public float duration = 4f;
        [Tooltip("Vibración de cámara durante el plano (0 = ninguna).")]
        public float shake;
        public List<SubtitleLine> lines = new List<SubtitleLine>();
        [Tooltip("Objetos que se activan al empezar el plano (luces, efectos...).")]
        public GameObject[] activateOnStart;
    }
}
