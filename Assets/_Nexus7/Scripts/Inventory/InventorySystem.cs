using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Nexus7.Inventory
{
    /// <summary>
    /// Inventario del jugador basado en espacios (slots) con apilamiento.
    ///
    /// Estructura de datos:
    ///   List&lt;InventorySlot&gt; slots   → cada slot = (ItemData, cantidad ≤ MaxStack)
    ///   capacity                         → número máximo de slots
    /// Un mismo objeto puede ocupar varios slots si supera su MaxStack (p. ej. 25 chatarras con MaxStack 20 = 2 slots).
    ///
    /// API principal:
    ///   Add(item, n)        → añade lo que quepa y devuelve cuántas unidades entraron
    ///   CanAdd(item, n)     → ¿caben todas?
    ///   Remove(item, n)     → quita hasta n y devuelve cuántas se quitaron
    ///   TryRemove(item, n)  → todo o nada
    ///   GetCount / Has / HasAll / TryConsume(lista)
    ///   GetByCategory(cat), GetSummary()
    /// Eventos: ItemAdded, ItemRemoved, Changed (la UI se suscribe a ellos).
    ///
    /// Colocar en: el GameObject raíz "Player".
    /// </summary>
    public class InventorySystem : MonoBehaviour
    {
        [Tooltip("Número máximo de espacios.")]
        [SerializeField, Min(1)] private int capacity = 12;
        [Tooltip("Objetos con los que se empieza (útil para pruebas).")]
        [SerializeField] private List<ItemStack> startingItems = new List<ItemStack>();

        private readonly List<InventorySlot> slots = new List<InventorySlot>();

        /// <summary>(objeto, cantidad añadida)</summary>
        public event Action<ItemData, int> ItemAdded;
        /// <summary>(objeto, cantidad quitada)</summary>
        public event Action<ItemData, int> ItemRemoved;
        /// <summary>Cualquier cambio en el contenido.</summary>
        public event Action Changed;

        public int Capacity => capacity;
        public int UsedSlots => slots.Count;
        public bool IsFull => slots.Count >= capacity && slots.All(s => s.FreeSpace == 0);
        public IReadOnlyList<InventorySlot> Slots => slots;

        private void Awake()
        {
            foreach (var stack in startingItems)
                if (stack.IsValid) Add(stack.item, stack.amount);
        }

        // ===================== AGREGAR =====================

        /// <summary>Añade hasta 'amount' unidades. Primero rellena slots existentes y luego abre nuevos.</summary>
        /// <returns>Unidades realmente añadidas (0 si el inventario está lleno).</returns>
        public int Add(ItemData item, int amount = 1)
        {
            if (item == null || amount <= 0) return 0;

            int remaining = amount;

            // 1) Rellenar pilas existentes del mismo objeto
            foreach (var slot in slots)
            {
                if (remaining == 0) break;
                if (slot.Item == item) remaining -= slot.AddUpTo(remaining);
            }

            // 2) Abrir slots nuevos mientras quede espacio
            while (remaining > 0 && slots.Count < capacity)
            {
                int put = Mathf.Min(remaining, item.MaxStack);
                slots.Add(new InventorySlot(item, put));
                remaining -= put;
            }

            int added = amount - remaining;
            if (added > 0)
            {
                ItemAdded?.Invoke(item, added);
                Changed?.Invoke();
            }
            return added;
        }

        /// <summary>¿Cabrían TODAS las unidades?</summary>
        public bool CanAdd(ItemData item, int amount = 1)
        {
            if (item == null || amount <= 0) return false;
            int space = slots.Where(s => s.Item == item).Sum(s => s.FreeSpace);
            space += (capacity - slots.Count) * item.MaxStack;
            return space >= amount;
        }

        // ===================== REMOVER =====================

        /// <summary>Quita hasta 'amount' unidades (empezando por los últimos slots).</summary>
        /// <returns>Unidades realmente quitadas.</returns>
        public int Remove(ItemData item, int amount = 1)
        {
            if (item == null || amount <= 0) return 0;

            int remaining = amount;
            for (int i = slots.Count - 1; i >= 0 && remaining > 0; i--)
            {
                if (slots[i].Item != item) continue;
                remaining -= slots[i].RemoveUpTo(remaining);
                if (slots[i].Quantity == 0) slots.RemoveAt(i);
            }

            int removed = amount - remaining;
            if (removed > 0)
            {
                ItemRemoved?.Invoke(item, removed);
                Changed?.Invoke();
            }
            return removed;
        }

        /// <summary>Quita exactamente 'amount' o no quita nada.</summary>
        public bool TryRemove(ItemData item, int amount = 1)
        {
            if (!Has(item, amount)) return false;
            Remove(item, amount);
            return true;
        }

        /// <summary>Consume una lista de requisitos de forma atómica (o todos o ninguno).</summary>
        public bool TryConsume(IEnumerable<ItemStack> requirements)
        {
            var list = requirements.Where(r => r.IsValid).ToList();
            if (!HasAll(list)) return false;
            foreach (var r in list) Remove(r.item, r.amount);
            return true;
        }

        public void Clear()
        {
            slots.Clear();
            Changed?.Invoke();
        }

        // ===================== CONSULTAR =====================

        public int GetCount(ItemData item) => item == null ? 0 : slots.Where(s => s.Item == item).Sum(s => s.Quantity);

        /// <summary>Cantidad por identificador (útil para sistemas de guardado o diálogos).</summary>
        public int GetCountById(string itemId) => slots.Where(s => s.Item != null && s.Item.Id == itemId).Sum(s => s.Quantity);

        public bool Has(ItemData item, int amount = 1) => GetCount(item) >= amount;

        public bool HasAll(IEnumerable<ItemStack> requirements)
        {
            // Agrupa por objeto por si la lista repite el mismo item.
            return requirements.Where(r => r.IsValid)
                               .GroupBy(r => r.item)
                               .All(g => GetCount(g.Key) >= g.Sum(r => r.amount));
        }

        /// <summary>Total por objeto dentro de una categoría (une los slots del mismo objeto).</summary>
        public IEnumerable<ItemStack> GetByCategory(ItemCategory category) =>
            GetSummary().Where(s => s.item.Category == category);

        /// <summary>Lista compacta: un ItemStack por tipo de objeto con su total.</summary>
        public List<ItemStack> GetSummary() =>
            slots.GroupBy(s => s.Item)
                 .Select(g => new ItemStack(g.Key, g.Sum(s => s.Quantity)))
                 .OrderBy(s => s.item.Category)
                 .ThenBy(s => s.item.DisplayName)
                 .ToList();
    }
}
