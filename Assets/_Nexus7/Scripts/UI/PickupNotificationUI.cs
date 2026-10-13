using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Nexus7.Inventory;

namespace Nexus7.UI
{
    /// <summary>
    /// Notificaciones apiladas en el lateral de la pantalla: "+1 Batería de iones", "-2 Batería", "Inventario lleno"...
    /// Se suscribe solo a los eventos del inventario, así que cualquier sistema que añada o quite objetos
    /// genera su aviso sin tener que llamar a la UI. Para avisos libres: PickupNotificationUI.Notify("texto", color).
    ///
    /// Colocar en: el Canvas "HUD". "Container" = RectTransform con VerticalLayoutGroup.
    /// </summary>
    public class PickupNotificationUI : MonoBehaviour
    {
        public static PickupNotificationUI Instance { get; private set; }

        [SerializeField] private InventorySystem inventory;
        [SerializeField] private RectTransform container;
        [SerializeField] private float lifetime = 2.8f;
        [SerializeField] private float fadeTime = 0.6f;
        [SerializeField] private int maxEntries = 5;
        [SerializeField] private float fontSize = 28f;
        [SerializeField] private Color removedColor = new Color(0.75f, 0.75f, 0.75f);

        private class Entry
        {
            public TextMeshProUGUI text;
            public Color color;
            public float age;
        }

        private readonly List<Entry> entries = new List<Entry>();

        private void Awake()
        {
            Instance = this;
            if (container == null) container = (RectTransform)transform;
        }

        private void Start()
        {
            if (inventory == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) inventory = player.GetComponent<InventorySystem>();
            }
            if (inventory != null)
            {
                inventory.ItemAdded += OnItemAdded;
                inventory.ItemRemoved += OnItemRemoved;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (inventory != null)
            {
                inventory.ItemAdded -= OnItemAdded;
                inventory.ItemRemoved -= OnItemRemoved;
            }
        }

        private void OnItemAdded(ItemData item, int amount) => Show($"+{amount}  {item.DisplayName}", item.UiColor);
        private void OnItemRemoved(ItemData item, int amount) => Show($"-{amount}  {item.DisplayName}", removedColor);

        /// <summary>Acceso estático cómodo (no falla si no hay HUD en la escena).</summary>
        public static void Notify(string text, Color color)
        {
            if (Instance != null) Instance.Show(text, color);
        }

        public void Show(string message, Color color)
        {
            var go = new GameObject("Notificacion", typeof(RectTransform));
            go.transform.SetParent(container, false);
            ((RectTransform)go.transform).sizeDelta = new Vector2(0f, fontSize + 12f);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = message;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Right;
            tmp.raycastTarget = false;

            entries.Add(new Entry { text = tmp, color = color });
            while (entries.Count > maxEntries) RemoveAt(0);
        }

        private void Update()
        {
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                var e = entries[i];
                e.age += Time.deltaTime;

                // Pequeño "pop" al aparecer y desvanecimiento al final.
                float pop = Mathf.Lerp(1.25f, 1f, Mathf.Clamp01(e.age / 0.15f));
                e.text.rectTransform.localScale = new Vector3(pop, pop, 1f);

                float alpha = 1f - Mathf.Clamp01((e.age - (lifetime - fadeTime)) / fadeTime);
                var c = e.color;
                c.a = alpha;
                e.text.color = c;

                if (e.age >= lifetime) RemoveAt(i);
            }
        }

        private void RemoveAt(int index)
        {
            if (entries[index].text != null) Destroy(entries[index].text.gameObject);
            entries.RemoveAt(index);
        }
    }
}
