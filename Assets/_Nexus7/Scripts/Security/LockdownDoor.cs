using UnityEngine;
using Nexus7.Core;
using Nexus7.Interaction;
using Nexus7.UI;

namespace Nexus7.Security
{
    /// <summary>
    /// Compuerta blindada con protocolo de bloqueo:
    ///   · Empieza bloqueada. Authorize() (desde una ControlConsole con hackeo) la desbloquea y la abre.
    ///   · En ALERTA ROJA se cierra y se sella automáticamente (aunque esté autorizada).
    ///   · Al salir de la Alerta Roja, si estaba autorizada, se vuelve a desbloquear.
    ///
    /// Colocar en: el mismo objeto que el DoorController de la compuerta.
    /// </summary>
    [RequireComponent(typeof(DoorController))]
    public class LockdownDoor : MonoBehaviour
    {
        [SerializeField] private string doorName = "Compuerta blindada";
        [SerializeField] private bool lockOnRedAlert = true;

        private DoorController door;

        public bool IsAuthorized { get; private set; }
        public bool IsInLockdown => lockOnRedAlert && AlertSystem.Level == AlertLevel.Red;
        public string DoorName => doorName;

        private void Awake() => door = GetComponent<DoorController>();
        private void OnEnable() => AlertSystem.LevelChanged += OnLevelChanged;
        private void OnDisable() => AlertSystem.LevelChanged -= OnLevelChanged;

        private void Start()
        {
            if (IsInLockdown) door.Lock();
        }

        public void Authorize()
        {
            IsAuthorized = true;
            if (IsInLockdown)
            {
                Message($"{doorName}: autorización registrada, pero la ALERTA ROJA mantiene el sellado.");
                return;
            }
            door.Unlock();
            door.SetOpen(true);
        }

        private void OnLevelChanged(AlertLevel from, AlertLevel to)
        {
            if (!lockOnRedAlert) return;
            if (to == AlertLevel.Red)
            {
                door.Lock(); // cierra y bloquea
                Message($"{doorName}: ¡SELLADO DE EMERGENCIA!");
            }
            else if (from == AlertLevel.Red && IsAuthorized)
            {
                door.Unlock();
                Message($"{doorName}: sellado levantado.");
            }
        }

        private static void Message(string text)
        {
            if (HUDManager.Instance != null) HUDManager.Instance.QueueMessage(text, 2.5f);
        }
    }
}
