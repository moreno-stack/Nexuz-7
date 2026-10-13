using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Nexus7.Core;
using Nexus7.Inventory;
using Nexus7.UI;

namespace Nexus7.Interaction
{
    /// <summary>
    /// Puerta corredera de ciencia ficción. Puede empezar bloqueada; mientras lo esté,
    /// interactuar muestra "ACCESO DENEGADO". Se desbloquea de dos formas:
    ///   a) Otro objeto llama a Unlock() (p. ej. AccessTerminal — Fase 1).
    ///   b) [Fase 2] Lista "Required Items": si el jugador lleva esos objetos, la puerta
    ///      se desbloquea y se abre al interactuar (y los consume si "Consume Items" está activo).
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
        [SerializeField] private string lockedPrompt = "Puerta bloqueada";
        [SerializeField, TextArea] private string lockedMessage = "ACCESO DENEGADO — Puerta sin energía.";
        [SerializeField] private float lockedMessageCooldown = 2f;

        [Header("Requisito de objetos (opcional, Fase 2)")]
        [Tooltip("Objetos necesarios para desbloquear. Vacío = solo se desbloquea con Unlock().")]
        [SerializeField] private List<ItemStack> requiredItems = new List<ItemStack>();
        [Tooltip("true = los objetos se gastan (baterías). false = solo hay que llevarlos (herramientas).")]
        [SerializeField] private bool consumeItems = true;
        [SerializeField, TextArea] private string unlockedWithItemsMessage = "";

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
        public bool HasItemRequirement => requiredItems.Any(r => r.IsValid);

        private Vector3 closedLocalPos;
        private float lastDeniedTime = -999f;
        private InventorySystem cachedPlayerInventory;

        public override string Prompt
        {
            get
            {
                if (!IsLocked) return IsOpen ? "Cerrar puerta" : "Abrir puerta";
                if (!HasItemRequirement) return lockedPrompt;
                return $"{lockedPrompt} ({RequirementProgress(PlayerInventory)})";
            }
        }

        private InventorySystem PlayerInventory
        {
            get
            {
                if (cachedPlayerInventory == null)
                {
                    var player = GameObject.FindGameObjectWithTag("Player");
                    if (player != null) cachedPlayerInventory = player.GetComponent<InventorySystem>();
                }
                return cachedPlayerInventory;
            }
        }

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
            if (IsLocked && HasItemRequirement)
            {
                var inventory = interactor.GetComponentInParent<InventorySystem>();
                if (inventory != null && TryUnlockWithItems(inventory)) return;
            }

            if (IsLocked)
            {
                if (Time.time - lastDeniedTime < lockedMessageCooldown) return; // evita llenar la cola de mensajes
                lastDeniedTime = Time.time;
                var hud = HUDManager.Instance;
                if (hud != null)
                {
                    hud.QueueMessage(lockedMessage, 2f);
                    if (HasItemRequirement) hud.QueueMessage("Necesitas: " + RequirementProgress(interactor.GetComponentInParent<InventorySystem>()), 2.5f);
                }
                Play(deniedClip);
                return;
            }
            SetOpen(!IsOpen);
        }

        private bool TryUnlockWithItems(InventorySystem inventory)
        {
            bool ok = consumeItems ? inventory.TryConsume(requiredItems) : inventory.HasAll(requiredItems);
            if (!ok) return false;

            Unlock();
            SetOpen(true);
            if (!string.IsNullOrEmpty(unlockedWithItemsMessage) && HUDManager.Instance != null)
                HUDManager.Instance.QueueMessage(unlockedWithItemsMessage);
            return true;
        }

        /// <summary>Texto tipo "Batería de iones 1/2, Llave hidráulica 0/1".</summary>
        private string RequirementProgress(InventorySystem inventory)
        {
            return string.Join(", ", requiredItems.Where(r => r.IsValid).Select(r =>
                $"{r.item.DisplayName} {(inventory != null ? Mathf.Min(inventory.GetCount(r.item), r.amount) : 0)}/{r.amount}"));
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
