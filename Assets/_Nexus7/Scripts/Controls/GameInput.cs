using UnityEngine;

namespace Nexus7.Controls
{
    /// <summary>
    /// Única puerta de entrada a los controles: devuelve lo que diga Input (teclado, ratón y mando) o lo que imiten
    /// los controles táctiles (TouchInput). En PC se comporta exactamente igual que Input.
    /// </summary>
    public static class GameInput
    {
        public static bool GetKey(KeyCode key) => Input.GetKey(key) || TouchInput.GetKey(key);
        public static bool GetKeyDown(KeyCode key) => Input.GetKeyDown(key) || TouchInput.GetKeyDown(key);
        public static bool GetKeyUp(KeyCode key) => Input.GetKeyUp(key) || TouchInput.GetKeyUp(key);

        public static float GetAxis(string axis) => Combine(axis, Input.GetAxis(axis));
        public static float GetAxisRaw(string axis) => Combine(axis, Input.GetAxisRaw(axis));

        public static bool GetMouseButtonDown(int button) => Input.GetMouseButtonDown(button) || TouchInput.GetKeyDown(MouseKey(button));

        /// <summary>
        /// ¿La mirada y el disparo están «capturados»? En PC, cuando el cursor está bloqueado (como antes); con
        /// controles táctiles, siempre (no hay cursor: los paneles abiertos cambian los botones que se ven).
        /// </summary>
        public static bool PointerCaptured => TouchInput.Enabled || Cursor.lockState == CursorLockMode.Locked;

        private static float Combine(string axis, float value)
        {
            float t = TouchInput.Axis(axis);
            if (t == 0f) return value;
            float sum = value + t;
            if (axis == "Horizontal" || axis == "Vertical") sum = Mathf.Clamp(sum, -1f, 1f);
            return sum;
        }

        private static KeyCode MouseKey(int button) => button == 0 ? KeyCode.Mouse0 : button == 1 ? KeyCode.Mouse1 : KeyCode.Mouse2;
    }
}
