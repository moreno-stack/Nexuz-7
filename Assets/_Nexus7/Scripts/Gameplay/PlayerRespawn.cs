using System;
using System.Collections;
using UnityEngine;
using Nexus7.Player;
using Nexus7.UI;

namespace Nexus7.Gameplay
{
    /// <summary>
    /// Gestiona la captura del jugador: fundido a negro, mensaje, teletransporte al último
    /// punto de control y reinicio de los enemigos. El inventario SE CONSERVA (decisión de diseño:
    /// castigar con tiempo, no con frustración).
    ///
    /// Eventos estáticos:
    ///   PlayerCaught    → los enemigos se congelan
    ///   PlayerRespawned → los enemigos vuelven a su posición inicial
    ///
    /// Colocar en: el GameObject raíz "Player".
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerRespawn : MonoBehaviour
    {
        public static event Action PlayerCaught;
        public static event Action PlayerRespawned;

        /// <summary>Tras reaparecer, los drones no pueden verte hasta este instante (margen para orientarte).</summary>
        public static float GraceUntil { get; private set; } = -999f;
        public static bool InGrace => Time.time < GraceUntil;
        [Tooltip("Segundos de 'invisibilidad' tras recuperar el control al reaparecer.")]
        [SerializeField] private float graceAfterRespawn = 2.5f;

        [SerializeField, TextArea] private string caughtMessage =
            "¡DETECTADO! Un dron de seguridad te ha neutralizado. Reiniciando desde el último punto seguro...";
        [SerializeField] private float fadeOutTime = 0.6f;
        [SerializeField] private float blackTime = 1.2f;
        [SerializeField] private float fadeInTime = 1f;

        private PlayerController controller;
        private CharacterController characterController;
        private Vector3 respawnPosition;
        private Quaternion respawnRotation;
        private bool isRespawning;

        public bool IsRespawning => isRespawning;

        private void Awake()
        {
            controller = GetComponent<PlayerController>();
            characterController = GetComponent<CharacterController>();
            respawnPosition = transform.position;
            respawnRotation = transform.rotation;
        }

        /// <summary>Lo llaman los Checkpoint al cruzarlos.</summary>
        public void SetCheckpoint(Transform point)
        {
            respawnPosition = point.position;
            respawnRotation = Quaternion.Euler(0f, point.eulerAngles.y, 0f);
        }

        /// <summary>Lo llama el enemigo al alcanzar al jugador.</summary>
        public void Caught() => Respawn(caughtMessage);

        [SerializeField, TextArea] private string knockedOutMessage = "FUERA DE COMBATE. Reiniciando sistemas vitales en el último punto seguro...";

        /// <summary>Reaparición genérica (captura, muerte por descarga...). Muestra 'message' durante el fundido.</summary>
        public void Respawn(string message)
        {
            if (isRespawning) return;
            // Lo primero, en el mismo frame: bloquear al jugador y avisar (las interfaces abiertas se cierran).
            isRespawning = true;
            if (controller != null) controller.InputEnabled = false;
            PlayerCaught?.Invoke();
            StartCoroutine(RespawnRoutine(message));
        }

        /// <summary>Vida a 0 por un golpe: "Fuera de combate".</summary>
        public void KnockedOut() => Respawn(knockedOutMessage);

        private IEnumerator RespawnRoutine(string message)
        {
            var hud = HUDManager.Instance;
            if (hud != null)
            {
                hud.QueueMessage(message, 2f);
                yield return hud.Fade(0f, 1f, fadeOutTime);
            }
            yield return new WaitForSeconds(blackTime);

            // Teletransporte: hay que desactivar el CharacterController o ignora el cambio de posición.
            if (characterController != null) characterController.enabled = false;
            transform.SetPositionAndRotation(respawnPosition, respawnRotation);
            if (characterController != null) characterController.enabled = true;

            GraceUntil = Time.time + fadeInTime + graceAfterRespawn;
            PlayerRespawned?.Invoke();

            if (hud != null) yield return hud.Fade(1f, 0f, fadeInTime);
            controller.ClearInputLocks();
            controller.InputEnabled = true;
            isRespawning = false;
        }
    }
}
