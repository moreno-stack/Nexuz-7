using System.Collections.Generic;
using UnityEngine;

namespace Nexus7.Controls
{
    /// <summary>Qué controles táctiles hay en pantalla según lo que esté pasando en el juego.</summary>
    public enum TouchContext { None, Gameplay, Inventory, Menu, Hack, Ending, Cinematic }

    /// <summary>Un botón táctil: imita la tecla 'Key'. Posición y radio en píxeles de la pantalla real (origen abajo a la izquierda).</summary>
    public struct TouchButton
    {
        public string Id, Label;
        public KeyCode Key;
        public float X, Y, Radius;
        /// <summary>Mientras se mantiene, arrastrar el dedo también mueve la cámara (disparar y apuntar).</summary>
        public bool LooksWhileHeld;
    }

    /// <summary>
    /// Distribución de los botones de cada contexto (lógica pura). Se diseña a 1920×1080 anclada a las esquinas y se
    /// escala con el alto de la pantalla, así que funciona en 16:9, 20:9 y 4:3.
    /// </summary>
    public static class TouchLayout
    {
        public const float ReferenceHeight = 1080f;

        private struct Def
        {
            public string Id, Label; public KeyCode Key; public float Ax, Ay, Ox, Oy, R; public bool Looks;
            public Def(string id, string label, KeyCode key, float ax, float ay, float ox, float oy, float r, bool looks = false)
            { Id = id; Label = label; Key = key; Ax = ax; Ay = ay; Ox = ox; Oy = oy; R = r; Looks = looks; }
        }

        private static readonly Def[] Gameplay =
        {
            // abajo a la derecha, alrededor de Disparar
            new Def("fire",     "FUEGO",  KeyCode.Mouse0, 1f, 0f, -210f, 230f, 95f, true),
            new Def("aim",      "MIRA",   KeyCode.Mouse1, 1f, 0f, -420f, 170f, 62f, true),
            new Def("jump",     "SALTO",  KeyCode.Space,  1f, 0f, -150f, 430f, 60f),
            new Def("crouch",   "AGACH.", KeyCode.C,      1f, 0f, -330f, 400f, 55f),
            new Def("reload",   "R",      KeyCode.R,      1f, 0f, -70f,  90f,  45f),
            new Def("use",      "USAR",   KeyCode.E,      1f, 0f, -560f, 300f, 62f),
            // arriba a la derecha
            new Def("weapon",   "ARMA",   KeyCode.Q,      1f, 1f, -90f,  -200f, 50f),
            new Def("device",   "DISP.",  KeyCode.G,      1f, 1f, -220f, -200f, 50f),
            new Def("inventory","INV.",   KeyCode.Tab,    1f, 1f, -350f, -200f, 50f),
            new Def("camera",   "CÁM.",   KeyCode.V,      1f, 1f, -480f, -200f, 50f),
            // junto a la barra rápida
            new Def("prev",     "◄",      KeyCode.JoystickButton4, 0.5f, 0f, -430f, 106f, 40f),
            new Def("next",     "►",      KeyCode.JoystickButton5, 0.5f, 0f, 430f,  106f, 40f),
        };

        private static readonly Def[] Hack =
        {
            new Def("up",     "▲", KeyCode.W, 1f, 0f, -260f, 360f, 70f),
            new Def("down",   "▼", KeyCode.S, 1f, 0f, -260f, 100f, 70f),
            new Def("left",   "◄", KeyCode.A, 1f, 0f, -400f, 230f, 70f),
            new Def("right",  "►", KeyCode.D, 1f, 0f, -120f, 230f, 70f),
            new Def("jam",    "¡INTERF.!", KeyCode.Space, 0f, 0f, 220f, 220f, 100f),
            new Def("cancel", "✕", KeyCode.Q, 1f, 1f, -90f, -120f, 50f),
        };

        private static readonly Def[] Menu =
        {
            new Def("up",      "▲",      KeyCode.W, 1f, 0f, -200f, 400f, 65f),
            new Def("down",    "▼",      KeyCode.S, 1f, 0f, -200f, 150f, 65f),
            new Def("confirm", "ACEPTAR", KeyCode.E, 1f, 0f, -420f, 275f, 75f),
            new Def("back",    "✕",      KeyCode.Q, 1f, 1f, -90f, -120f, 50f),
        };

        private static readonly Def[] Ending =
        {
            new Def("retry", "REINTENTAR", KeyCode.R, 0.5f, 0f, -180f, 130f, 75f),
            new Def("menu",  "MENÚ",       KeyCode.M, 0.5f, 0f, 180f,  130f, 75f),
        };

        private static readonly Def[] Cinematic = { new Def("skip", "SALTAR ►►", KeyCode.Space, 1f, 0f, -150f, 150f, 70f) };
        private static readonly Def[] Inventory = { new Def("close", "✕", KeyCode.Tab, 1f, 1f, -90f, -120f, 55f) };
        private static readonly Def[] Empty = new Def[0];

        public static TouchButton[] For(TouchContext ctx, float width, float height)
        {
            Def[] defs;
            switch (ctx)
            {
                case TouchContext.Gameplay:  defs = Gameplay; break;
                case TouchContext.Hack:      defs = Hack; break;
                case TouchContext.Menu:      defs = Menu; break;
                case TouchContext.Ending:    defs = Ending; break;
                case TouchContext.Cinematic: defs = Cinematic; break;
                case TouchContext.Inventory: defs = Inventory; break;
                default:                     defs = Empty; break;
            }
            float s = height / ReferenceHeight;
            var result = new TouchButton[defs.Length];
            for (int i = 0; i < defs.Length; i++)
            {
                var d = defs[i];
                result[i] = new TouchButton
                {
                    Id = d.Id, Label = d.Label, Key = d.Key, LooksWhileHeld = d.Looks,
                    X = d.Ax * width + d.Ox * s, Y = d.Ay * height + d.Oy * s, Radius = d.R * s,
                };
            }
            return result;
        }

        public static bool HasStick(TouchContext ctx) => ctx == TouchContext.Gameplay;
        public static bool HasLook(TouchContext ctx) => ctx == TouchContext.Gameplay;

        /// <summary>Contexto según lo que esté abierto, de más a menos prioritario.</summary>
        public static TouchContext Pick(bool cinematic, bool ending, bool hack, bool menu, bool inventory, bool hasPlayer)
        {
            if (cinematic) return TouchContext.Cinematic;
            if (ending) return TouchContext.Ending;
            if (hack) return TouchContext.Hack;
            if (menu) return TouchContext.Menu;
            if (inventory) return TouchContext.Inventory;
            return hasPlayer ? TouchContext.Gameplay : TouchContext.None;
        }
    }
}
