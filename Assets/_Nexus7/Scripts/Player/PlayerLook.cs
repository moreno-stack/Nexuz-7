using UnityEngine;
using Nexus7.Controls;

namespace Nexus7.Player
{
    /// <summary>
    /// Cámara en primera persona: el ratón gira el cuerpo (eje Y) y el CameraRoot (eje X).
    /// Bloquea el cursor al jugar; Esc lo libera y un clic lo vuelve a bloquear.
    ///
    /// Colocar en: el GameObject raíz "Player".
    /// </summary>
    public class PlayerLook : MonoBehaviour
    {
        [SerializeField] private Transform cameraRoot;
        [SerializeField] private PlayerController player;

        [Header("Sensibilidad")]
        [SerializeField] private float mouseSensitivity = 2f;
        [SerializeField] private bool invertY = false;
        [SerializeField] private float minPitch = -80f;
        [SerializeField] private float maxPitch = 80f;

        [Header("Mando (opcional)")]
        [Tooltip("Fuerza el stick derecho. No hace falta: se activa solo si existen los ejes 'LookX' y 'LookY' en el Input Manager.")]
        [SerializeField] private bool useGamepadLook = false;
        [SerializeField] private string gamepadAxisX = "LookX";
        [SerializeField] private string gamepadAxisY = "LookY";
        [Tooltip("Grados por segundo con el stick al máximo.")]
        [SerializeField] private float gamepadSensitivity = 160f;

        private float pitch;
        private bool gamepadLook;

        private void Start()
        {
            SetCursorLocked(true);
            // El stick derecho funciona solo si el proyecto tiene los ejes (ahora vienen en el Input Manager)
            gamepadLook = useGamepadLook || (Nexus7.Core.GamepadAxes.Available(gamepadAxisX) && Nexus7.Core.GamepadAxes.Available(gamepadAxisY));
        }

        private void Update()
        {
            // En PC: Esc suelta el cursor y un clic lo vuelve a bloquear. Con controles táctiles no hay cursor
            // (la tecla Atrás de Android es Esc y no debe dejar la cámara quieta).
            if (!TouchInput.Enabled)
            {
                if (GameInput.GetKeyDown(KeyCode.Escape)) SetCursorLocked(false);
                else if (GameInput.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked) SetCursorLocked(true);
            }

            if (!GameInput.PointerCaptured) return;
            if (player != null && !player.InputEnabled) return;

            float yaw = GameInput.GetAxis("Mouse X") * mouseSensitivity;
            float pitchDelta = GameInput.GetAxis("Mouse Y") * mouseSensitivity;

            if (gamepadLook)
            {
                yaw += Nexus7.Core.GamepadAxes.Read(gamepadAxisX) * gamepadSensitivity * Time.deltaTime;
                pitchDelta += Nexus7.Core.GamepadAxes.Read(gamepadAxisY) * gamepadSensitivity * Time.deltaTime;
            }

            if (invertY) pitchDelta = -pitchDelta;

            transform.Rotate(Vector3.up * yaw);                       // girar el cuerpo
            pitch = Mathf.Clamp(pitch - pitchDelta, minPitch, maxPitch);
            if (cameraRoot != null) cameraRoot.localRotation = Quaternion.Euler(pitch, 0f, 0f); // inclinar la cabeza
        }

        public static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
