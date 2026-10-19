using System.Collections.Generic;

namespace Nexus7.Inventory
{
    /// <summary>
    /// Lógica pura de la barra rápida 1–9 (probada con Mono).
    /// Las casillas son los objetos DISTINTOS del inventario por orden de llegada (máximo 9).
    /// La selección sigue al OBJETO: si cambia de casilla, la selección lo acompaña; si se agota,
    /// se queda en la misma posición (recortada al tamaño).
    /// </summary>
    public class HotbarModel
    {
        public const int MaxSlots = 9;

        private readonly List<ItemData> items = new List<ItemData>(MaxSlots);

        public int Selected { get; private set; }
        public int Count => items.Count;
        public ItemData SelectedItem => Selected >= 0 && Selected < items.Count ? items[Selected] : null;

        public ItemData Get(int index) => index >= 0 && index < items.Count ? items[index] : null;

        public void Rebuild(IReadOnlyList<ItemData> stackItemsInOrder)
        {
            var previous = SelectedItem;
            items.Clear();
            if (stackItemsInOrder != null)
                for (int i = 0; i < stackItemsInOrder.Count && items.Count < MaxSlots; i++)
                {
                    var it = stackItemsInOrder[i];
                    if (it != null && !items.Contains(it)) items.Add(it);
                }

            int follow = previous != null ? items.IndexOf(previous) : -1;
            if (follow >= 0) Selected = follow;
            else if (items.Count == 0) Selected = 0;
            else if (Selected >= items.Count) Selected = items.Count - 1;
        }

        public void Select(int index)
        {
            if (index < 0 || index >= MaxSlots || index >= items.Count) return;
            Selected = index;
        }

        public void Cycle(int dir)
        {
            if (items.Count == 0) { Selected = 0; return; }
            int n = items.Count;
            Selected = ((Selected + (dir >= 0 ? 1 : -1)) % n + n) % n;
        }
    }
}
