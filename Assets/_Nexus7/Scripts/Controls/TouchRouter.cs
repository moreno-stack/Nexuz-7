using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nexus7.Controls
{
    public enum TouchPhaseLite { Began, Moved, Stationary, Ended, Canceled }

    /// <summary>Un toque leído de la pantalla (independiente de UnityEngine.Touch para poder probarlo).</summary>
    public struct RawTouch
    {
        public int Id; public float X, Y; public TouchPhaseLite Phase;
        public RawTouch(int id, float x, float y, TouchPhaseLite phase) { Id = id; X = x; Y = y; Phase = phase; }
    }

    /// <summary>
    /// Reparte los dedos entre los controles (lógica pura). Cada dedo se queda con el control que tocó al empezar
    /// (botón, joystick o mirar) hasta que se levanta, así que se puede mover, mirar y disparar a la vez.
    /// Escribe en TouchInput: teclas de los botones, «Horizontal»/«Vertical» del joystick y «Mouse X/Y» de la mirada.
    /// </summary>
    public class TouchRouter
    {
        public const int MaxFingers = 10;
        public const float StickRadius = 110f, DeadZone = 0.12f;
        /// <summary>Se corre solo arrastrando el dedo más allá del borde (× radio): a fondo dentro del círculo se camina
        /// (correr hace ruido y levanta al jugador agachado, mal asunto en un juego de sigilo).</summary>
        public const float RunThreshold = 1.25f;
        /// <summary>Unidades de «Mouse X/Y» por píxel de referencia (1080 p) arrastrado.</summary>
        public const float LookScale = 0.08f;
        /// <summary>Margen para acertar un botón con el dedo (× radio).</summary>
        public const float HitTolerance = 1.15f;
        private const KeyCode RunKey = KeyCode.LeftShift;
        private const int RunSourceOffset = 100000;

        private enum Kind { Ignored, Button, Stick, Look }
        private class Finger
        {
            public Kind Kind; public string ButtonId; public KeyCode Key; public bool Looks;
            public float LastX, LastY, OriginX, OriginY; public bool Running;
        }

        private readonly Dictionary<int, Finger> fingers = new Dictionary<int, Finger>();
        private readonly HashSet<int> seen = new HashSet<int>();
        private readonly List<int> gone = new List<int>();
        private TouchContext context = TouchContext.None;

        public bool StickActive { get; private set; }
        public Vector2 StickOrigin { get; private set; }
        /// <summary>Dirección del joystick ya con zona muerta (longitud 0…1).</summary>
        public Vector2 StickVector { get; private set; }
        public HashSet<string> PressedButtons { get; } = new HashSet<string>();
        public int TrackedFingers => fingers.Count;

        public void Update(TouchContext ctx, IList<RawTouch> touches, float width, float height)
        {
            float scale = height / TouchLayout.ReferenceHeight;
            if (ctx != context)
            {
                foreach (var kv in fingers) { ReleaseFinger(kv.Key, kv.Value); kv.Value.Kind = Kind.Ignored; }
                context = ctx;
            }
            var buttons = TouchLayout.For(ctx, width, height);
            seen.Clear();
            float sx = 0f, sy = 0f; bool stick = false;
            float lookX = 0f, lookY = 0f;

            foreach (var t in touches)
            {
                seen.Add(t.Id);
                fingers.TryGetValue(t.Id, out var f);
                bool ending = t.Phase == TouchPhaseLite.Ended || t.Phase == TouchPhaseLite.Canceled;
                if (t.Phase == TouchPhaseLite.Began || (f == null && !ending))
                {
                    if (f != null) ReleaseFinger(t.Id, f);
                    else if (fingers.Count >= MaxFingers) { seen.Remove(t.Id); continue; }
                    f = Assign(t, ctx, buttons, width);
                    fingers[t.Id] = f;
                }
                if (f == null) continue;
                if (ending)
                {
                    ReleaseFinger(t.Id, f);
                    fingers.Remove(t.Id);
                    continue;
                }

                float dx = t.X - f.LastX, dy = t.Y - f.LastY;
                f.LastX = t.X; f.LastY = t.Y;
                switch (f.Kind)
                {
                    case Kind.Stick:
                    {
                        float vx = (t.X - f.OriginX) / (StickRadius * scale), vy = (t.Y - f.OriginY) / (StickRadius * scale);
                        float mag = (float)Math.Sqrt(vx * vx + vy * vy);
                        bool run = mag > RunThreshold;
                        if (mag > 1f) { vx /= mag; vy /= mag; mag = 1f; }
                        float k = mag <= DeadZone ? 0f : (mag - DeadZone) / (1f - DeadZone) / mag;
                        sx = vx * k; sy = vy * k; stick = true;
                        StickOrigin = new Vector2(f.OriginX, f.OriginY);
                        if (run && !f.Running) TouchInput.Press(RunKey, RunSourceOffset + t.Id);
                        else if (!run && f.Running) TouchInput.Release(RunKey, RunSourceOffset + t.Id);
                        f.Running = run;
                        break;
                    }
                    case Kind.Look:
                        lookX += dx; lookY += dy; break;
                    case Kind.Button:
                        if (f.Looks) { lookX += dx; lookY += dy; }
                        break;
                }
            }

            // Dedos que desaparecen sin 'Ended' (la app perdió el foco): se sueltan igual
            gone.Clear();
            foreach (var kv in fingers) if (!seen.Contains(kv.Key)) gone.Add(kv.Key);
            foreach (int id in gone) { ReleaseFinger(id, fingers[id]); fingers.Remove(id); }

            StickActive = stick || HasStickFinger();
            StickVector = new Vector2(sx, sy);
            if (sx != 0f) TouchInput.AddAxis("Horizontal", sx);
            if (sy != 0f) TouchInput.AddAxis("Vertical", sy);
            float lk = LookScale / scale;
            if (lookX != 0f) TouchInput.AddAxis("Mouse X", lookX * lk);
            if (lookY != 0f) TouchInput.AddAxis("Mouse Y", lookY * lk);

            PressedButtons.Clear();
            foreach (var kv in fingers) if (kv.Value.Kind == Kind.Button) PressedButtons.Add(kv.Value.ButtonId);
        }

        /// <summary>Olvida todos los dedos y suelta sus teclas.</summary>
        public void Reset()
        {
            foreach (var kv in fingers) ReleaseFinger(kv.Key, kv.Value);
            fingers.Clear(); PressedButtons.Clear();
            StickActive = false; StickVector = new Vector2(0f, 0f);
        }

        private bool HasStickFinger()
        {
            foreach (var kv in fingers) if (kv.Value.Kind == Kind.Stick) return true;
            return false;
        }

        private static Finger Assign(RawTouch t, TouchContext ctx, TouchButton[] buttons, float width)
        {
            var f = new Finger { LastX = t.X, LastY = t.Y, OriginX = t.X, OriginY = t.Y };
            int best = -1; float bestDist = float.MaxValue;
            for (int i = 0; i < buttons.Length; i++)
            {
                float dx = t.X - buttons[i].X, dy = t.Y - buttons[i].Y;
                float d = (float)Math.Sqrt(dx * dx + dy * dy);
                if (d <= buttons[i].Radius * HitTolerance && d < bestDist) { best = i; bestDist = d; }
            }
            if (best >= 0)
            {
                f.Kind = Kind.Button; f.ButtonId = buttons[best].Id; f.Key = buttons[best].Key; f.Looks = buttons[best].LooksWhileHeld;
                TouchInput.Press(f.Key, t.Id);
            }
            else if (TouchLayout.HasStick(ctx) && t.X < width * 0.5f) f.Kind = Kind.Stick;
            else if (TouchLayout.HasLook(ctx) && t.X >= width * 0.5f) f.Kind = Kind.Look;
            else f.Kind = Kind.Ignored;
            return f;
        }

        private static void ReleaseFinger(int id, Finger f)
        {
            if (f.Kind == Kind.Button) TouchInput.Release(f.Key, id);
            if (f.Running) { TouchInput.Release(RunKey, RunSourceOffset + id); f.Running = false; }
        }
    }
}
