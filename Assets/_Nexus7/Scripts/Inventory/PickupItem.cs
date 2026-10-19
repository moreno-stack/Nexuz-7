using UnityEngine;
using Nexus7.Core;
using Nexus7.Interaction;
using Nexus7.UI;

namespace Nexus7.Inventory
{
    /// <summary>
    /// Objeto recolectable suelto en el suelo (Battery, ScrapMetal, ToolComponent...).
    /// Al interactuar se suma al inventario. Si no cabe todo, el resto se queda en el suelo.
    /// La notificación "+1 Batería" la muestra PickupNotificationUI escuchando al inventario.
    ///
    /// Colocar en: un objeto con collider (no trigger). Opcional: una luz hija para que destaque en la oscuridad.
    /// </summary>
    public class PickupItem : Interactable
    {
        [SerializeField] private ItemData item;
        [SerializeField, Min(1)] private int amount = 1;

        [Header("Efectos")]
        [Tooltip("Ruido al recoger (chatarra metálica = ruidosa). 0 = silencioso.")]
        [SerializeField] private float pickupNoiseRadius = 0f;
        [SerializeField] private AudioClip pickupClip;
        [Tooltip("Hijo visual que gira y flota (no el objeto con collider).")]
        [SerializeField] private Transform visual;
        [SerializeField] private float spinSpeed = 60f;
        [SerializeField] private float bobHeight = 0.05f;

        private Vector3 visualBasePos;

        public ItemData Item => item;
        public int Amount => amount;

        public override string Prompt =>
            item == null ? "Recoger" : (amount > 1 ? $"Recoger {item.DisplayName} x{amount}" : $"Recoger {item.DisplayName}");

        public override bool CanInteract(PlayerInteractor interactor) => item != null && amount > 0 && base.CanInteract(interactor);

        /// <summary>Permite configurar el objeto por código (spawners, enemigos que sueltan loot...).</summary>
        public void Setup(ItemData newItem, int newAmount)
        {
            item = newItem;
            amount = Mathf.Max(1, newAmount);
        }

        private void Awake()
        {
            if (visual != null) visualBasePos = visual.localPosition;
        }

        private void Update()
        {
            if (visual == null) return;
            visual.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
            visual.localPosition = visualBasePos + Vector3.up * (Mathf.Sin(Time.time * 2f) * bobHeight);
        }

        protected override void OnInteract(PlayerInteractor interactor)
        {
            var inventory = interactor.GetComponentInParent<InventorySystem>();
            if (inventory == null)
            {
                Debug.LogWarning("[PickupItem] El jugador no tiene InventorySystem.", this);
                return;
            }

            int added = inventory.Add(item, amount);
            if (added == 0)
            {
                PickupNotificationUI.Notify("Inventario lleno", new Color(1f, 0.4f, 0.3f));
                return;
            }

            amount -= added;
            RunStats.RecordCollected(added);
            if (pickupClip != null) AudioSource.PlayClipAtPoint(pickupClip, transform.position);
            NoiseSystem.Emit(transform.position, pickupNoiseRadius, gameObject);

            if (amount <= 0) Destroy(gameObject);
            else PickupNotificationUI.Notify($"Inventario lleno: quedan {amount}", new Color(1f, 0.4f, 0.3f));
        }
    }
}
