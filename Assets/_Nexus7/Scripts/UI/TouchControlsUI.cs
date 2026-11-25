using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Nexus7.Cinematics;
using Nexus7.Controls;
using Nexus7.Player;

namespace Nexus7.UI
{
    /// <summary>
    /// Controles táctiles del móvil. Se crea solo (solo en Android/iOS o en el Device Simulator) y sobrevive a los
    /// cambios de escena. En cada fotograma, antes que el resto de scripts:
    /// 1. decide el contexto (juego, inventario, consola o banco, hackeo, pantalla final, cinemática);
    /// 2. pasa los toques de la zona segura a TouchRouter, que pulsa teclas y mueve ejes en TouchInput;
    /// 3. dibuja los botones, el joystick y lo que está pulsado.
    /// Los toques se leen de Input.touches: no hace falta EventSystem, y las imágenes no bloquean nada.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class TouchControlsUI : MonoBehaviour
    {
        private static TouchControlsUI instance;

        private static readonly Color ButtonIdle = new Color(0.08f, 0.12f, 0.16f, 0.45f);
        private static readonly Color ButtonPressed = new Color(0.35f, 0.85f, 1f, 0.6f);
        private static readonly Color StickBase = new Color(0.6f, 0.85f, 1f, 0.18f);
        private static readonly Color StickKnob = new Color(0.6f, 0.9f, 1f, 0.5f);
        private static readonly Color StickHint = new Color(0.6f, 0.85f, 1f, 0.08f);

        private readonly TouchRouter router = new TouchRouter();
        private readonly List<RawTouch> touches = new List<RawTouch>();
        private readonly Dictionary<string, Image> buttonImages = new Dictionary<string, Image>();

        private Canvas canvas;
        private RectTransform root;
        private Image stickBase, stickKnob;
        private Sprite circle;
        private TouchContext shownContext = (TouchContext)(-1);
        private Rect shownArea;

        private PlayerController player;
        private InventoryUI inventory;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            TouchInput.Clock = () => Time.frameCount;
            TouchInput.Enabled = Application.isMobilePlatform;
            if (!TouchInput.Enabled || instance != null) return;
            Input.simulateMouseWithTouches = false;   // un toque no es un clic (si no, cualquier toque dispararía)
            Input.multiTouchEnabled = true;
            var go = new GameObject("ControlesTactiles");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<TouchControlsUI>();
        }

        private void Awake()
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 900;   // por encima del HUD (10), por debajo de los fundidos (1000)
            root = new GameObject("Zona", typeof(RectTransform)).GetComponent<RectTransform>();
            root.SetParent(transform, false);
            circle = MakeCircle(128);
        }

        private void Update()
        {
            if (player == null) player = FindAnyObjectByType<PlayerController>();
            if (inventory == null) inventory = FindAnyObjectByType<InventoryUI>();

            var ctx = TouchLayout.Pick(
                CinematicDirector.IsPlaying,
                EndingUI.Instance != null && EndingUI.Instance.IsOpen,
                HackMinigameUI.Instance != null && HackMinigameUI.Instance.IsOpen,
                (ConsoleUI.Instance != null && ConsoleUI.Instance.IsOpen) || (CraftingUI.Instance != null && CraftingUI.Instance.IsOpen),
                inventory != null && inventory.IsOpen,
                player != null);

            // Zona segura (muesca de la cámara): los botones y los toques se miden dentro de ella
            Rect area = Screen.safeArea;
            touches.Clear();
            for (int i = 0; i < Input.touchCount; i++)
            {
                var t = Input.GetTouch(i);
                touches.Add(new RawTouch(t.fingerId, t.position.x - area.xMin, t.position.y - area.yMin, ToLite(t.phase)));
            }
            router.Update(ctx, touches, area.width, area.height);

            if (ctx != shownContext || !SameArea(area, shownArea)) Rebuild(ctx, area);
            Draw(ctx, area);
        }

        private void OnApplicationPause(bool paused) { if (paused) ReleaseEverything(); }
        private void OnApplicationFocus(bool focused) { if (!focused) ReleaseEverything(); }
        private void OnDestroy() { if (instance == this) { ReleaseEverything(); instance = null; } }

        private void ReleaseEverything()
        {
            router.Reset();
            TouchInput.ReleaseAll();
        }

        // ------------------------------------------------------------------ dibujo
        private void Rebuild(TouchContext ctx, Rect area)
        {
            shownContext = ctx; shownArea = area;
            for (int i = root.childCount - 1; i >= 0; i--) Destroy(root.GetChild(i).gameObject);
            buttonImages.Clear();
            stickBase = stickKnob = null;

            root.anchorMin = root.anchorMax = Vector2.zero;
            root.pivot = Vector2.zero;
            root.anchoredPosition = new Vector2(area.xMin, area.yMin);
            root.sizeDelta = new Vector2(area.width, area.height);

            float s = area.height / TouchLayout.ReferenceHeight;
            if (TouchLayout.HasStick(ctx))
            {
                stickBase = Circle("BaseJoystick", TouchRouter.StickRadius * s, StickHint);
                stickKnob = Circle("Mando", TouchRouter.StickRadius * 0.45f * s, StickKnob);
                Place(stickBase.rectTransform, 240f * s, 240f * s);
            }
            foreach (var b in TouchLayout.For(ctx, area.width, area.height))
            {
                var img = Circle("Boton_" + b.Id, b.Radius, ButtonIdle);
                Place(img.rectTransform, b.X, b.Y);
                buttonImages[b.Id] = img;
                var label = new GameObject("Texto", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
                label.transform.SetParent(img.transform, false);
                var lr = label.rectTransform;
                lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one; lr.offsetMin = lr.offsetMax = Vector2.zero;
                label.text = b.Label;
                label.fontSize = Mathf.Clamp(b.Radius * (b.Label.Length > 6 ? 0.3f : 0.42f), 12f, 40f);
                label.alignment = TextAlignmentOptions.Center;
                label.color = new Color(1f, 1f, 1f, 0.85f);
                label.raycastTarget = false;
            }
            if (stickKnob != null) stickKnob.gameObject.SetActive(false);
        }

        private void Draw(TouchContext ctx, Rect area)
        {
            foreach (var kv in buttonImages)
                kv.Value.color = router.PressedButtons.Contains(kv.Key) ? ButtonPressed : ButtonIdle;
            if (stickBase == null) return;
            float s = area.height / TouchLayout.ReferenceHeight;
            if (router.StickActive)
            {
                stickBase.color = StickBase;
                Place(stickBase.rectTransform, router.StickOrigin.x, router.StickOrigin.y);
                stickKnob.gameObject.SetActive(true);
                var v = router.StickVector;
                Place(stickKnob.rectTransform, router.StickOrigin.x + v.x * TouchRouter.StickRadius * s, router.StickOrigin.y + v.y * TouchRouter.StickRadius * s);
            }
            else
            {
                stickBase.color = StickHint;   // pista de dónde va el joystick
                Place(stickBase.rectTransform, 240f * s, 240f * s);
                stickKnob.gameObject.SetActive(false);
            }
        }

        private Image Circle(string name, float radius, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(root, false);
            var img = go.AddComponent<Image>();
            img.sprite = circle;
            img.color = color;
            img.raycastTarget = false;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(radius * 2f, radius * 2f);
            return img;
        }

        private static bool SameArea(Rect a, Rect b) => a.xMin == b.xMin && a.yMin == b.yMin && a.width == b.width && a.height == b.height;

        private static void Place(RectTransform rt, float x, float y) => rt.anchoredPosition = new Vector2(x, y);

        /// <summary>Círculo blanco con borde suave y un anillo más claro (se tiñe con el color de cada control).</summary>
        private static Sprite MakeCircle(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color[size * size];
            float c = (size - 1) * 0.5f, r = size * 0.5f - 1f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                    float edge = Mathf.Clamp01(r - d);                       // borde suave de 1 px
                    float ring = d > r - 5f ? 1f : 0.55f;                     // anillo exterior más visible
                    px[y * size + x] = new Color(1f, 1f, 1f, edge * ring);
                }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static TouchPhaseLite ToLite(TouchPhase p)
        {
            switch (p)
            {
                case TouchPhase.Began:      return TouchPhaseLite.Began;
                case TouchPhase.Moved:      return TouchPhaseLite.Moved;
                case TouchPhase.Stationary: return TouchPhaseLite.Stationary;
                case TouchPhase.Ended:      return TouchPhaseLite.Ended;
                default:                    return TouchPhaseLite.Canceled;
            }
        }
    }
}
