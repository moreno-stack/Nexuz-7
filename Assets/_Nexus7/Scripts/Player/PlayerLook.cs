using UnityEngine;

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
        [Tooltip("Actívalo SOLO si creaste los ejes 'LookX' y 'LookY' en Project Settings > Input Manager (stick derecho).")]
        [SerializeField] private bool useGamepadLook = false;
        [SerializeField] private string gamepadAxisX = "LookX";
        [SerializeField] private string gamepadAxisY = "LookY";
        [Tooltip("Grados por segundo con el stick al máximo.")]
        [SerializeField] private float gamepadSensitivity = 160f;

        private float pitch;

        private void Start() => SetCursorLocked(true);

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) SetCursorLocked(false);
            else if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked) SetCursorLocked(true);

            if (Cursor.lockState != CursorLockMode.Locked) return;
            if (player != null && !player.InputEnabled) return;

            float yaw = Input.GetAxis("Mouse X") * mouseSensitivity;
            float pitchDelta = Input.GetAxis("Mouse Y") * mouseSensitivity;

            if (useGamepadLook)
            {
                yaw += Input.GetAxis(gamepadAxisX) * gamepadSensitivity * Time.deltaTime;
                pitchDelta += Input.GetAxis(gamepadAxisY) * gamepadSensitivity * Time.deltaTime;
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
