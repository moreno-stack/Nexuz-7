using System.Collections.Generic;
using UnityEngine;

namespace Nexus7.Core
{
    /// <summary>
    /// Lectura segura de ejes del Input Manager: si un eje no existe (proyecto antiguo o ajustes cambiados) devuelve 0
    /// en lugar de lanzar una excepción cada fotograma, y lo recuerda para no volver a intentarlo.
    /// Ejes del proyecto para el mando (ProjectSettings/InputManager.asset, mando tipo Xbox en Windows):
    ///   LookX / LookY (stick derecho), Nexus7 Gatillo Der / Izq (RT / LT), Nexus7 Cruceta X / Y.
    /// </summary>
    public static class GamepadAxes
    {
        public const string LookX = "LookX", LookY = "LookY";
        public const string RightTrigger = "Nexus7 Gatillo Der", LeftTrigger = "Nexus7 Gatillo Izq";
        public const string DpadX = "Nexus7 Cruceta X", DpadY = "Nexus7 Cruceta Y";

        private static readonly HashSet<string> missing = new HashSet<string>();
        private static readonly HashSet<string> present = new HashSet<string>();

        public static float Read(string axis)
        {
            if (string.IsNullOrEmpty(axis) || missing.Contains(axis)) return 0f;
            try
            {
                float v = Input.GetAxisRaw(axis);
                present.Add(axis);
                return v;
            }
            catch (System.ArgumentException)
            {
                missing.Add(axis);
                return 0f;
            }
        }

        /// <summary>¿Existe el eje en el Input Manager?</summary>
        public static bool Available(string axis)
        {
            if (present.Contains(axis)) return true;
            Read(axis);
            return present.Contains(axis);
        }

        private static bool anyGamepad;
        private static float nextGamepadCheck = -1f;

        /// <summary>¿Hay algún mando conectado? (para mostrar sus controles en el HUD). Se comprueba como mucho una vez por segundo.</summary>
        public static bool AnyGamepad
        {
            get
            {
                if (Time.unscaledTime < nextGamepadCheck) return anyGamepad;
                nextGamepadCheck = Time.unscaledTime + 1f;
                anyGamepad = false;
                foreach (var n in Input.GetJoystickNames()) if (!string.IsNullOrEmpty(n)) { anyGamepad = true; break; }
                return anyGamepad;
            }
        }
    }
}
