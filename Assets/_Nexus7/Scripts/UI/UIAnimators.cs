using UnityEngine;
using UnityEngine.UI;

namespace Nexus7.UI
{
    /// <summary>
    /// Entrada animada de un bloque de la interfaz: aparece (CanvasGroup) y se desliza hasta su sitio.
    /// Vuelve a empezar cada vez que el objeto se activa (la pantalla final se activa al terminar la partida).
    /// Usa tiempo sin escala: funciona aunque el juego esté en pausa.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class UIIntro : MonoBehaviour
    {
        [SerializeField] private float delay = 0f;
        [SerializeField] private float duration = 0.6f;
        [Tooltip("Desde dónde llega (en píxeles del lienzo, relativo a su posición final).")]
        [SerializeField] private Vector2 slideFrom = new Vector2(-60f, 0f);

        private CanvasGroup group;
        private RectTransform rt;
        private Vector2 home;
        private bool hasHome;
        private float start;
        private bool done;

        private void OnEnable()
        {
            group = GetComponent<CanvasGroup>();
            rt = transform as RectTransform;
            if (rt != null && !hasHome) { home = rt.anchoredPosition; hasHome = true; }
            start = Time.unscaledTime;
            done = false;
            Apply(0f);
        }

        private void Update() { if (!done) Apply(Time.unscaledTime - start); }

        private void Apply(float t)
        {
            float k = ScreenFx.Intro(t, delay, duration);
            if (group != null)
            {
                group.alpha = k;
                group.blocksRaycasts = group.interactable = k > 0.5f;   // nada se pulsa mientras es invisible
            }
            if (rt != null && hasHome) rt.anchoredPosition = home + slideFrom * (1f - k);
            done = k >= 1f;
        }
    }

    /// <summary>Gira un elemento de la interfaz (el anillo de la insignia del rango).</summary>
    public class UISpin : MonoBehaviour
    {
        [SerializeField] private float degreesPerSecond = 25f;
        private void Update() => transform.Rotate(0f, 0f, -degreesPerSecond * Time.unscaledDeltaTime);
    }

    /// <summary>Pulso de opacidad y tamaño (resplandor del título, «¡NUEVO RÉCORD!», línea animada del menú).</summary>
    public class UIPulse : MonoBehaviour
    {
        [SerializeField] private Graphic graphic;
        [SerializeField] private float speed = 0.6f;
        [SerializeField] private float minAlpha = 0.35f;
        [SerializeField] private float maxAlpha = 1f;
        [SerializeField] private float scaleAmount = 0f;

        private Vector3 baseScale;

        private void Awake() { if (graphic == null) graphic = GetComponent<Graphic>(); baseScale = transform.localScale; }

        private void Update()
        {
            float p = ScreenFx.Pulse(Time.unscaledTime, speed);
            // Alfa del CanvasRenderer: no reconstruye la malla del texto en cada fotograma (se multiplica por el color)
            if (graphic != null) graphic.canvasRenderer.SetAlpha(Mathf.Lerp(minAlpha, maxAlpha, p));
            if (scaleAmount != 0f) transform.localScale = baseScale * (1f + scaleAmount * p);
        }
    }

    /// <summary>Barrido horizontal: un destello que cruza la línea bajo el título del menú (aparece y se apaga en los extremos).</summary>
    public class UISweep : MonoBehaviour
    {
        [SerializeField] private float fromX = -400f;
        [SerializeField] private float toX = 400f;
        [SerializeField] private float period = 3f;

        private RectTransform rt;
        private Graphic graphic;

        private void Awake() { rt = transform as RectTransform; graphic = GetComponent<Graphic>(); }

        private void Update()
        {
            if (rt == null || period <= 0f) return;
            float k = Mathf.Repeat(Time.unscaledTime, period) / period;
            var p = rt.anchoredPosition; p.x = Mathf.Lerp(fromX, toX, k); rt.anchoredPosition = p;
            if (graphic != null) graphic.canvasRenderer.SetAlpha(ScreenFx.SweepAlpha(k));
        }
    }
}
