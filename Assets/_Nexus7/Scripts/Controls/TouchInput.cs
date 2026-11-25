using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nexus7.Controls
{
    /// <summary>
    /// Teclas y ejes que imitan los controles táctiles (lógica pura). Cada dedo es una «fuente»: una tecla está pulsada
    /// mientras la mantenga al menos un dedo; 'Down' y 'Up' valen solo en el fotograma del cambio. Los ejes se suman
    /// durante el fotograma y vuelven a cero en el siguiente. GameInput lo combina con Input.
    /// </summary>
    public static class TouchInput
    {
        /// <summary>¿Hay controles táctiles? (móvil o Device Simulator).</summary>
        public static bool Enabled;

        /// <summary>Fotograma actual (TouchControlsUI pone Time.frameCount; las pruebas, su propio contador).</summary>
        public static Func<int> Clock = () => 0;

        private static readonly Dictionary<KeyCode, HashSet<int>> held = new Dictionary<KeyCode, HashSet<int>>();
        private static readonly Dictionary<KeyCode, int> downFrame = new Dictionary<KeyCode, int>();
        private static readonly Dictionary<KeyCode, int> upFrame = new Dictionary<KeyCode, int>();
        private static readonly Dictionary<string, float> axes = new Dictionary<string, float>();
        private static int axesFrame = int.MinValue;

        public static void Press(KeyCode key, int source)
        {
            if (key == KeyCode.None) return;
            if (!held.TryGetValue(key, out var set)) held[key] = set = new HashSet<int>();
            if (set.Count == 0) downFrame[key] = Clock();
            set.Add(source);
        }

        public static void Release(KeyCode key, int source)
        {
            if (!held.TryGetValue(key, out var set) || !set.Remove(source)) return;
            if (set.Count == 0) upFrame[key] = Clock();
        }

        /// <summary>Suelta todas las teclas (cambio de contexto, la app pierde el foco).</summary>
        public static void ReleaseAll()
        {
            int now = Clock();
            foreach (var kv in held)
                if (kv.Value.Count > 0) { kv.Value.Clear(); upFrame[kv.Key] = now; }
            axes.Clear();
        }

        public static bool GetKey(KeyCode key) => held.TryGetValue(key, out var set) && set.Count > 0;
        public static bool GetKeyDown(KeyCode key) => downFrame.TryGetValue(key, out int f) && f == Clock() && GetKey(key);
        public static bool GetKeyUp(KeyCode key) => upFrame.TryGetValue(key, out int f) && f == Clock() && !GetKey(key);

        public static void AddAxis(string axis, float value)
        {
            Refresh();
            axes.TryGetValue(axis, out float v);
            axes[axis] = v + value;
        }

        /// <summary>Valor del eje en este fotograma. «Horizontal» y «Vertical» se acotan a −1…1; la mirada no.</summary>
        public static float Axis(string axis)
        {
            Refresh();
            if (!axes.TryGetValue(axis, out float v)) return 0f;
            return axis == "Horizontal" || axis == "Vertical" ? Math.Max(-1f, Math.Min(1f, v)) : v;
        }

        private static void Refresh()
        {
            int now = Clock();
            if (now != axesFrame) { axes.Clear(); axesFrame = now; }
        }
    }
}
