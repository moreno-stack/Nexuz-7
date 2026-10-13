using UnityEngine;
using Nexus7.Core;
using Nexus7.UI;

namespace Nexus7.Interaction
{
    /// <summary>
    /// Puerta corredera de ciencia ficción. Puede empezar bloqueada; mientras lo esté,
    /// interactuar muestra "ACCESO DENEGADO". Otro objeto (p. ej. AccessTerminal) llama a Unlock().
    /// El indicador de estado cambia de rojo (bloqueada) a verde (desbloqueada).
    ///
    /// Colocar en: el GameObject raíz de la puerta. El panel móvil va como hijo con su collider.
    /// </summary>
    public class DoorController : Interactable
    {
        [Header("Movimiento")]
        [Tooltip("Hijo que se desplaza (el panel con collider).")]
        [SerializeField] private Transform doorPanel;
        [Tooltip("Desplazamiento local del panel al abrir (por defecto sube 2.45 m).")]
        [SerializeField] private Vector3 openOffset = new Vector3(0f, 2.45f, 0f);
        [SerializeField] private float openSpeed = 2.5f;

        [Header("Bloqueo")]
        [SerializeField] private bool startLocked = true;
        [SerializeField, TextArea] private string lockedMessage = "ACCESO DENEGADO — Puerta sin energía.";
        [SerializeField] private float lockedMessageCooldown = 2f;

        [Header("Indicador de estado (opcional)")]
        [SerializeField] private Renderer statusRenderer;
        [SerializeField] private Light statusLight;
        [SerializeField] private Color lockedColor = new Color(1f, 0.1f, 0.05f);
        [SerializeField] private Color unlockedColor = new Color(0.1f, 1f, 0.3f);

        [Header("Audio (opcional)")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip openClip;
        [SerializeField] private AudioClip deniedClip;
        [SerializeField] private AudioClip unlockClip;
        [Tooltip("Radio de ruido al abrirse/cerrarse.")]
        [SerializeField] private float doorNoiseRadius = 8f;

        public bool IsLocked { get; private set; }
        public bool IsOpen { get; private set; }

        private Vector3 closedLocalPos;
        private float lastDeniedTime = -999f;

        public override string Prompt => IsLocked ? "Puerta bloqueada" : (IsOpen ? "Cerrar puerta" : "Abrir puerta");

        private void Awake()
        {
            if (doorPanel == null) doorPanel = transform;
            closedLocalPos = doorPanel.localPosition;
            IsLocked = startLocked;
            UpdateStatusVisual();
        }

        private void Update()
        {
            Vector3 target = IsOpen ? closedLocalPos + openOffset : closedLocalPos;
            doorPanel.localPosition = Vector3.MoveTowards(doorPanel.localPosition, target, openSpeed * Time.deltaTime);
        }

        protected override void OnInteract(PlayerInteractor interactor)
        {
            if (IsLocked)
            {
                if (Time.time - lastDeniedTime < lockedMessageCooldown) return; // evita llenar la cola de mensajes
                lastDeniedTime = Time.time;
                if (HUDManager.Instance != null) HUDManager.Instance.QueueMessage(lockedMessage, 2f);
                Play(deniedClip);
                return;
            }
            SetOpen(!IsOpen);
        }

        /// <summary>Quita el bloqueo. Llamado por terminales, palancas, eventos de historia...</summary>
        public void Unlock()
        {
            if (!IsLocked) return;
            IsLocked = false;
            UpdateStatusVisual();
            Play(unlockClip);
        }

        public void Lock()
        {
            IsLocked = true;
            SetOpen(false);
            UpdateStatusVisual();
        }

        public void SetOpen(bool open)
        {
            if (IsOpen == open) return;
            IsOpen = open;
            Play(openClip);
            NoiseSystem.Emit(transform.position, doorNoiseRadius, gameObject);
        }

        private void UpdateStatusVisual()
        {
            Color c = IsLocked ? lockedColor : unlockedColor;
            if (statusLight != null) statusLight.color = c;
            if (statusRenderer != null)
            {
                // .material crea una instancia propia para no cambiar el color de todas las puertas.
                var mat = statusRenderer.material;
                mat.color = c;
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", c * 3f);
            }
        }

        private void Play(AudioClip clip)
        {
            if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
        }
    }
}
