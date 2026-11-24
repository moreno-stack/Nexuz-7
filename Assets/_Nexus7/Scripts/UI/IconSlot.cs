using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Nexus7.Inventory;

namespace Nexus7.UI
{
    /// <summary>
    /// Casilla de la interfaz con el icono de un objeto: fondo oscuro, marco (blanco si está seleccionada, naranja si
    /// es un objeto clave), el icono y un texto pequeño abajo a la derecha (cantidad o tecla). Se crea por código.
    /// </summary>
    public class IconSlot : MonoBehaviour
    {
        private static readonly Color Background = new Color(0.03f, 0.05f, 0.08f, 0.8f);
        private static readonly Color FrameNormal = new Color(0.35f, 0.45f, 0.55f, 0.6f);
        private static readonly Color FrameSelected = Color.white;
        private static readonly Color FrameKey = new Color(1f, 0.7f, 0.28f, 0.9f);

        private Image frame, icon;
        private TextMeshProUGUI label;
        private RectTransform rect;
        public ItemData Item { get; private set; }

        public static IconSlot Create(Transform parent, float size)
        {
            var go = new GameObject("Casilla", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var slot = go.AddComponent<IconSlot>();
            slot.rect = go.GetComponent<RectTransform>();
            if (slot.rect != null) slot.rect.sizeDelta = new Vector2(size, size);
            slot.frame = go.AddComponent<Image>();
            slot.frame.raycastTarget = false;
            var bg = Child("Fondo", go.transform, 2f).AddComponent<Image>();
            bg.color = Background; bg.raycastTarget = false;
            slot.icon = Child("Icono", go.transform, 4f).AddComponent<Image>();
            slot.icon.preserveAspect = true; slot.icon.raycastTarget = false;
            slot.label = Child("Texto", go.transform, 3f).AddComponent<TextMeshProUGUI>();
            slot.label.fontSize = Mathf.Max(12f, size * 0.3f);
            slot.label.alignment = TextAlignmentOptions.BottomRight;
            slot.label.color = Color.white;
            slot.label.raycastTarget = false;
            return slot;
        }

        private static GameObject Child(string name, Transform parent, float inset)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.offsetMin = new Vector2(inset, inset); rt.offsetMax = new Vector2(-inset, -inset);
            }
            return go;
        }

        public void Set(ItemData item, string text, bool selected)
        {
            Item = item;
            if (icon != null)
            {
                icon.sprite = ItemIcons.For(item);
                icon.color = icon.sprite != null ? Color.white : (item != null ? item.UiColor : Color.clear);
            }
            if (label != null) label.text = text;
            if (frame != null) frame.color = selected ? FrameSelected : item != null && item.IsKeyItem ? FrameKey : FrameNormal;
            transform.localScale = selected ? Vector3.one * 1.12f : Vector3.one;
        }

        public void Place(Vector2 anchor, Vector2 position)
        {
            if (rect == null) return;
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
        }
    }
}
