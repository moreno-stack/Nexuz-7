using UnityEngine;
using Nexus7.Core;
using Nexus7.Interaction;
using Nexus7.UI;

namespace Nexus7.Power
{
    /// <summary>
    /// Clase base de todo lo que APORTA energía a la red (zócalo de batería, interruptor, reactor).
    /// Es interactuable: cada subclase decide qué hace falta para activarla (OnInteract → Activate()).
    /// Al activarse suma 'Output' unidades a la PowerGrid, cambia su indicador a verde y hace ruido.
    /// </summary>
    public abstract class PowerSource : Interactable
    {
        [Header("Energía")]
        [SerializeField, Min(1)] private int output = 1;
        [Tooltip("Texto corto para el objetivo dinámico, p. ej. 'Zócalo de batería (ala oeste)'.")]
        [SerializeField] private string objectiveLabel;
        [SerializeField, TextArea] private string activatedMessage;
        [Tooltip("Las máquinas al arrancar hacen ruido: los drones cercanos lo oirán.")]
        [SerializeField] private float activationNoiseRadius = 6f;

        [Header("Indicador (opcional)")]
        [SerializeField] private Renderer statusRenderer;
        [SerializeField] private Light statusLight;
        [SerializeField] private Color inactiveColor = new Color(1f, 0.15f, 0.05f);
        [SerializeField] private Color activeColor = new Color(0.15f, 1f, 0.35f);

        [Header("Audio (opcional)")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip activateClip;
        [SerializeField] private AudioClip deniedClip;

        public bool IsActive { get; private set; }
        public int Output => output;
        public string ObjectiveLabel => objectiveLabel;

        private float lastDeniedTime = -999f;

        protected virtual void Start()
        {
            if (PowerGrid.Instance != null) PowerGrid.Instance.Register(this);
            else Debug.LogWarning($"[PowerSource] No hay PowerGrid en la escena ({name}).", this);
            UpdateIndicator();
        }

        public override bool CanInteract(PlayerInteractor interactor) => !IsActive && base.CanInteract(interactor);

        /// <summary>Conecta la fuente a la red. Las subclases lo llaman cuando se cumplen sus requisitos.</summary>
        protected void Activate()
        {
            if (IsActive) return;
            IsActive = true;
            UpdateIndicator();
            if (audioSource != null && activateClip != null) audioSource.PlayOneShot(activateClip);
            NoiseSystem.Emit(transform.position, activationNoiseRadius, gameObject);
            if (!string.IsNullOrEmpty(activatedMessage) && HUDManager.Instance != null)
                HUDManager.Instance.QueueMessage(activatedMessage);
            if (PowerGrid.Instance != null) PowerGrid.Instance.Recalculate();
            OnActivated();
        }

        /// <summary>Gancho para efectos propios (mostrar la batería, encender el núcleo...).</summary>
        protected virtual void OnActivated() { }

        /// <summary>Mensaje de "no se puede" con antispam.</summary>
        protected void Deny(string message)
        {
            if (Time.time - lastDeniedTime < 1.5f) return;
            lastDeniedTime = Time.time;
            if (HUDManager.Instance != null) HUDManager.Instance.QueueMessage(message, 2.5f);
            if (audioSource != null && deniedClip != null) audioSource.PlayOneShot(deniedClip);
        }

        private void UpdateIndicator()
        {
            Color c = IsActive ? activeColor : inactiveColor;
            if (statusLight != null) statusLight.color = c;
            if (statusRenderer != null)
            {
                var mat = statusRenderer.material;
                mat.color = c;
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", c * 3f);
            }
        }
    }
}
