using System.Collections.Generic;
using UnityEngine;
using Nexus7.Core;
using Nexus7.Interaction;
using Nexus7.UI;

namespace Nexus7.Inventory
{
    /// <summary>
    /// Contenedor de carga con botín (cajas, taquillas). Al usarlo se abre la tapa
    /// y se transfiere todo lo que quepa al inventario. Abrirlo hace ruido: ¡cuidado con los drones!
    ///
    /// Colocar en: la raíz del contenedor (el collider puede estar en un hijo).
    /// </summary>
    public class LootContainer : Interactable
    {
        [SerializeField] private List<ItemStack> contents = new List<ItemStack>();

        [Header("Tapa (opcional)")]
        [Tooltip("Pivote de la tapa; rota al abrir.")]
        [SerializeField] private Transform lid;
        [SerializeField] private Vector3 openLidEuler = new Vector3(-110f, 0f, 0f);
        [SerializeField] private float lidSpeed = 4f;

        [Header("Efectos")]
        [Tooltip("Radio del ruido al abrir (bisagras oxidadas).")]
        [SerializeField] private float openNoiseRadius = 5f;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip openClip;

        private bool isOpen;
        private Quaternion closedRot, openRot;

        public bool IsEmpty => contents.TrueForAll(s => !s.IsValid);

        public override string Prompt => !isOpen ? "Abrir contenedor" : (IsEmpty ? "Contenedor vacío" : "Recoger contenido restante");
        public override bool CanInteract(PlayerInteractor interactor) => !(isOpen && IsEmpty) && base.CanInteract(interactor);

        private void Awake()
        {
            if (lid == null) return;
            closedRot = lid.localRotation;
            openRot = closedRot * Quaternion.Euler(openLidEuler);
        }

        private void Update()
        {
            if (lid == null) return;
            lid.localRotation = Quaternion.Slerp(lid.localRotation, isOpen ? openRot : closedRot, lidSpeed * Time.deltaTime);
        }

        protected override void OnInteract(PlayerInteractor interactor)
        {
            if (!isOpen)
            {
                isOpen = true;
                NoiseSystem.Emit(transform.position, openNoiseRadius, gameObject);
                if (audioSource != null && openClip != null) audioSource.PlayOneShot(openClip);
            }

            var inventory = interactor.GetComponentInParent<InventorySystem>();
            if (inventory == null) return;

            bool somethingLeft = false;
            for (int i = 0; i < contents.Count; i++)
            {
                var stack = contents[i];
                if (!stack.IsValid) continue;

                int added = inventory.Add(stack.item, stack.amount);
                stack.amount -= added;
                contents[i] = stack; // ItemStack es struct: hay que reasignarlo
                if (stack.amount > 0) somethingLeft = true;
            }

            if (IsEmpty && !somethingLeft && contents.Count == 0)
                PickupNotificationUI.Notify("El contenedor está vacío", new Color(0.7f, 0.7f, 0.7f));
            else if (somethingLeft)
                PickupNotificationUI.Notify("Inventario lleno: quedan objetos en el contenedor", new Color(1f, 0.4f, 0.3f));

            contents.RemoveAll(s => !s.IsValid);
        }
    }
}
