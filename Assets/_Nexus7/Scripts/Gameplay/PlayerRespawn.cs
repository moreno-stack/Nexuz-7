using System;
using System.Collections;
using System.Text;
using UnityEngine;
using Nexus7.Inventory;
using Nexus7.Player;
using Nexus7.UI;

namespace Nexus7.Gameplay
{
    /// <summary>
    /// Gestiona la captura del jugador: fundido a negro, mensaje, teletransporte al último
    /// punto de control y reinicio de los enemigos.
    /// Castigo justo (RespawnPenalty): se conservan los dispositivos fabricados, las herramientas y los objetos clave;
    /// se pierde el 25 % de los recursos menores (piezas, baterías, componentes) y, en la Fase 6, 15 s de la cuenta atrás
    /// (sin bajar nunca de 60 s). La estamina se rellena.
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

        /// <summary>
        /// Aplica la penalización de RespawnPenalty y devuelve el mensaje con lo perdido ("" si no se perdió nada).
        /// </summary>
        public string ApplyPenalty()
        {
            var sb = new StringBuilder();
            var inventory = GetComponent<InventorySystem>();
            if (inventory != null)
            {
                foreach (var stack in inventory.GetSummary())
                {
                    int lost = RespawnPenalty.LostAmount(stack.amount, stack.item.Category, stack.item.IsKeyItem);
                    if (lost <= 0) continue;
                    lost = inventory.Remove(stack.item, lost);
                    if (lost <= 0) continue;
                    sb.Append(sb.Length == 0 ? "" : ", ").Append(lost).Append(' ').Append(stack.item.DisplayName);
                }
            }
            string text = sb.Length > 0 ? "Recursos perdidos al caer: " + sb : "";
            var countdown = FindAnyObjectByType<EvacuationCountdown>();
            if (countdown != null && countdown.Running)
            {
                float secs = countdown.Penalize(RespawnPenalty.CountdownPenalty(countdown.Remaining));
                if (secs > 0.5f) text += (text.Length > 0 ? " · " : "") + $"−{secs:0} s de cuenta atrás";
            }
            if (text.Length > 0) text += ". Dispositivos y objetos clave conservados.";
            return text;
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
            if (controller != null) controller.OnTeleported();

            GraceUntil = Time.time + fadeInTime + graceAfterRespawn;
            string penalty = ApplyPenalty();
            PlayerRespawned?.Invoke();
            // Va por las notificaciones (no por los mensajes): así no lo tapa un aviso de la cuenta atrás
            if (!string.IsNullOrEmpty(penalty)) PickupNotificationUI.Notify(penalty, new Color(1f, 0.7f, 0.35f));

            if (hud != null) yield return hud.Fade(1f, 0f, fadeInTime);
            controller.ClearInputLocks();
            controller.InputEnabled = true;
            isRespawning = false;
        }
    }
}
