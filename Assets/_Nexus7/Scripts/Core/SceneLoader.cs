using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Nexus7.Core
{
    /// <summary>Matemática de las transiciones (lógica pura, se prueba fuera de Unity).</summary>
    public static class SceneFade
    {
        /// <summary>Opacidad del negro: fundido a negro 0→1 o de entrada 1→0.</summary>
        public static float Alpha(float elapsed, float duration, bool fadingIn)
        {
            float k = duration <= 0f ? 1f : Mathf01(elapsed / duration);
            return fadingIn ? 1f - k : k;
        }

        /// <summary>Solo se empieza una carga si no hay otra en curso y la escena está en Build Settings.</summary>
        public static bool CanStart(bool loading, bool sceneInBuild) => !loading && sceneInBuild;

        /// <summary>Unity da el progreso de 0 a 0,9 hasta activar la escena: 0,9 = 100 %.</summary>
        public static float Progress(float unityProgress) => Mathf01(unityProgress / 0.9f);

        private static float Mathf01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
    }

    /// <summary>
    /// Único punto para cambiar de escena: fundido a negro, carga en segundo plano con barra de progreso y fundido de entrada.
    /// Lo usan el menú (Nueva partida, Continuar), las salidas de cada fase, la consola de mando y la pantalla final.
    /// Además guarda la partida automáticamente al entrar en cada fase (SaveSystem).
    /// Se crea solo al arrancar el juego y sobrevive a los cambios de escena: no hace falta ponerlo en ninguna escena.
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        public const float FadeSeconds = 0.35f;

        private static SceneLoader instance;
        public static bool IsLoading { get; private set; }

        private CanvasGroup group;
        private Image bar;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Create()
        {
            IsLoading = false;
            if (instance != null) return;
            var go = new GameObject("SceneLoader");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<SceneLoader>();
            instance.BuildOverlay();
            SceneManager.sceneLoaded += instance.OnSceneLoaded;
        }

        /// <summary>Carga la escena con transición. false si ya hay una carga en curso o la escena no está en Build Settings.</summary>
        public static bool Load(string scene)
        {
            bool inBuild = !string.IsNullOrEmpty(scene) && Application.CanStreamedLevelBeLoaded(scene);
            if (!inBuild) Debug.LogWarning($"[Nexus-7] La escena '{scene}' no está en Build Settings (Nexus-7 → Ordenar Build Settings).");
            if (!SceneFade.CanStart(IsLoading, inBuild)) return false;
            if (instance == null) Create();
            IsLoading = true;
            instance.StartCoroutine(instance.LoadRoutine(scene));
            return true;
        }

        private IEnumerator LoadRoutine(string scene)
        {
            group.blocksRaycasts = true;
            bar.rectTransform.localScale = new Vector3(0f, 1f, 1f);
            for (float t = 0f; t < FadeSeconds; t += Time.unscaledDeltaTime) { group.alpha = SceneFade.Alpha(t, FadeSeconds, false); yield return null; }
            group.alpha = 1f;

            var op = SceneManager.LoadSceneAsync(scene);
            while (op != null && !op.isDone)
            {
                bar.rectTransform.localScale = new Vector3(SceneFade.Progress(op.progress), 1f, 1f);
                yield return null;
            }
            bar.rectTransform.localScale = Vector3.one;
            yield return null;   // un fotograma para que la escena nueva se coloque antes de mostrarla

            for (float t = 0f; t < FadeSeconds; t += Time.unscaledDeltaTime) { group.alpha = SceneFade.Alpha(t, FadeSeconds, true); yield return null; }
            group.alpha = 0f;
            group.blocksRaycasts = false;
            IsLoading = false;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (GameFlow.IsPhaseScene(scene.name)) StartCoroutine(AutoSave(scene.name));
        }

        /// <summary>Guarda un fotograma después de cargar la fase, cuando el inventario ya tiene lo que trae el jugador.</summary>
        private IEnumerator AutoSave(string scene)
        {
            yield return null;
            var player = GameObject.FindGameObjectWithTag("Player");
            SaveSystem.SaveCurrent(scene, player != null ? player.GetComponent<Nexus7.Inventory.InventorySystem>() : null);
        }

        private void BuildOverlay()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;   // por encima de todo
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();   // bloquea los clics durante la transición
            group = gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;

            var black = NewImage("Negro", transform, Color.black);
            Stretch(black.rectTransform);

            var label = new GameObject("TextoCargando", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            label.transform.SetParent(transform, false);
            label.text = "CARGANDO";
            label.fontSize = 26f;
            label.characterSpacing = 12f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(0.35f, 0.85f, 1f, 0.85f);
            Place(label.rectTransform, new Vector2(0f, -400f), new Vector2(600f, 40f));

            var bg = NewImage("BarraCarga", transform, new Color(1f, 1f, 1f, 0.12f));
            Place(bg.rectTransform, new Vector2(0f, -440f), new Vector2(520f, 4f));
            bar = NewImage("Relleno", bg.transform, new Color(0.35f, 0.85f, 1f));
            bar.rectTransform.pivot = new Vector2(0f, 0.5f);
            Stretch(bar.rectTransform);
        }

        private static Image NewImage(string name, Transform parent, Color color)
        {
            var img = new GameObject(name, typeof(RectTransform)).AddComponent<Image>();
            img.transform.SetParent(parent, false);
            img.color = color;
            img.raycastTarget = name == "Negro";
            return img;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private static void Place(RectTransform rt, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }
    }
}
