using System;
using UnityEngine;

namespace Nexus7.Inventory
{
    /// <summary>
    /// Par (objeto, cantidad). Se usa para definir contenidos de contenedores,
    /// requisitos de puertas, recetas futuras, etc. Es serializable: se edita en el Inspector.
    /// </summary>
    [Serializable]
    public struct ItemStack
    {
        public ItemData item;
        [Min(1)] public int amount;

        public ItemStack(ItemData item, int amount)
        {
            this.item = item;
            this.amount = amount;
        }

        public bool IsValid => item != null && amount > 0;
        public override string ToString() => item != null ? $"{item.DisplayName} x{amount}" : "(vacío)";
    }

    /// <summary>
    /// Un espacio (slot) del inventario. Contiene un único tipo de objeto hasta su MaxStack.
    /// </summary>
    [Serializable]
    public class InventorySlot
    {
        [SerializeField] private ItemData item;
        [SerializeField] private int quantity;

        public ItemData Item => item;
        public int Quantity => quantity;
        public int FreeSpace => item != null ? item.MaxStack - quantity : 0;

        public InventorySlot(ItemData item, int quantity)
        {
            this.item = item;
            this.quantity = quantity;
        }

        /// <summary>Suma hasta llenar el slot. Devuelve cuántas unidades se añadieron realmente.</summary>
        internal int AddUpTo(int amount)
        {
            int added = Mathf.Min(amount, FreeSpace);
            quantity += added;
            return added;
        }

        /// <summary>Resta hasta vaciar el slot. Devuelve cuántas unidades se quitaron realmente.</summary>
        internal int RemoveUpTo(int amount)
        {
            int removed = Mathf.Min(amount, quantity);
            quantity -= removed;
            return removed;
        }
    }
}
