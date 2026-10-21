using UnityEngine;

namespace Nexus7.Environment
{
    /// <summary>
    /// Crece de 'from' a 'to' (escala uniforme) en 'duration' segundos con un arranque rápido (explosión de la nave).
    /// Empieza al activarse el objeto. Colocar en: la esfera de la explosión (sin collider).
    /// </summary>
    public class GrowAndFade : MonoBehaviour
    {
        [SerializeField] private float from = 1f;
        [SerializeField] private float to = 140f;
        [SerializeField] private float duration = 4f;
        [SerializeField] private Light flash;

        private float t;

        private void OnEnable() { t = 0f; Apply(); }

        private void Update()
        {
            t += Time.unscaledDeltaTime;
            Apply();
        }

        private void Apply()
        {
            float s = ScaleAt(t, duration, from, to);
            transform.localScale = new Vector3(s, s, s);
            if (flash != null) flash.intensity = Mathf.Lerp(8f, 0f, duration > 0f ? Mathf.Clamp01(t / duration) : 1f);
        }

        /// <summary>Escala en el instante t (ease-out cuadrático), limitada a 'to' al terminar.</summary>
        public static float ScaleAt(float t, float duration, float from, float to)
        {
            if (duration <= 0f || t >= duration) return to;
            if (t <= 0f) return from;
            float k = t / duration;
            k = 1f - (1f - k) * (1f - k);
            return from + (to - from) * k;
        }
    }
}
