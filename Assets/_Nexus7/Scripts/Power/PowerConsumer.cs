using UnityEngine;
using UnityEngine.Events;
using Nexus7.Interaction;
using Nexus7.UI;

namespace Nexus7.Power
{
    /// <summary>
    /// Algo que NECESITA energía: cuando la red alcanza 'Required Power' se activa
    /// (desbloquea sus puertas, enciende su indicador, dispara On Powered) y si baja, se desactiva.
    ///
    /// Ejemplos: puerta secundaria (1 bloque), acceso al reactor (2), compuerta principal (4 = todo).
    ///
    /// Colocar en: la puerta (junto a DoorController) o en cualquier objeto que dependa de la energía.
    /// </summary>
    public class PowerConsumer : MonoBehaviour
    {
        [SerializeField, Min(1)] private int requiredPower = 1;
        [Tooltip("Puertas que se desbloquean con energía (deben empezar con Start Locked).")]
        [SerializeField] private DoorController[] doors;
        [Tooltip("Abrir las puertas automáticamente al recibir energía.")]
        [SerializeField] private bool autoOpenDoors = false;
        [SerializeField, TextArea] private string poweredMessage;

        [Header("Indicador (opcional)")]
        [SerializeField] private Renderer indicator;
        [SerializeField] private Light indicatorLight;
        [SerializeField] private Color unpoweredColor = new Color(1f, 0.15f, 0.05f);
        [SerializeField] private Color poweredColor = new Color(0.15f, 1f, 0.35f);

        [Header("Eventos")]
        [SerializeField] private UnityEvent onPowered;
        [SerializeField] private UnityEvent onUnpowered;

        public bool IsPowered { get; private set; }
        public int RequiredPower => requiredPower;

        private bool initialized;

        private void Start()
        {
            if (PowerGrid.Instance != null) PowerGrid.Instance.PowerChanged += OnPowerChanged;
            Evaluate(silent: true);
        }

        private void OnDestroy()
        {
            if (PowerGrid.Instance != null) PowerGrid.Instance.PowerChanged -= OnPowerChanged;
        }

        private void OnPowerChanged(int current, int max) => Evaluate(silent: false);

        private void Evaluate(bool silent)
        {
            bool powered = PowerGrid.Power >= requiredPower;
            if (initialized && powered == IsPowered) return;
            initialized = true;
            IsPowered = powered;

            if (doors != null)
            {
                foreach (var door in doors)
                {
                    if (door == null) continue;
                    if (powered) { door.Unlock(); if (autoOpenDoors) door.SetOpen(true); }
                    else door.Lock();
                }
            }

            Color c = powered ? poweredColor : unpoweredColor;
            if (indicatorLight != null) indicatorLight.color = c;
            if (indicator != null)
            {
                var mat = indicator.material;
                mat.color = c;
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", c * 3f);
            }

            if (powered)
            {
                onPowered?.Invoke();
                if (!silent && !string.IsNullOrEmpty(poweredMessage) && HUDManager.Instance != null)
                    HUDManager.Instance.QueueMessage(poweredMessage);
            }
            else onUnpowered?.Invoke();
        }
    }
}
