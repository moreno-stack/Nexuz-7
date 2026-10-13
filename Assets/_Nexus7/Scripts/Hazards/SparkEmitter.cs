using UnityEngine;
using Nexus7.Power;

namespace Nexus7.Hazards
{
    /// <summary>
    /// Chispas eléctricas: ráfagas de partículas + destello de luz a intervalos aleatorios.
    /// Ambientación de paneles rotos y transformadores; también lo usan las trampas (Burst()).
    /// Por defecto deja de chispear cuando la energía se restaura por completo.
    ///
    /// Colocar en: un GameObject con ParticleSystem (Emission rate = 0; las ráfagas las lanza este script).
    /// </summary>
    public class SparkEmitter : MonoBehaviour
    {
        [SerializeField] private ParticleSystem particles;
        [SerializeField] private Light flashLight;
        [Tooltip("Segundos entre ráfagas automáticas (mín, máx). (0,0) = solo ráfagas manuales.")]
        [SerializeField] private Vector2 interval = new Vector2(0.8f, 3.5f);
        [SerializeField] private Vector2Int burstCount = new Vector2Int(8, 22);
        [SerializeField] private float flashIntensity = 3f;
        [SerializeField] private float flashDuration = 0.08f;
        [SerializeField] private bool stopWhenPowerRestored = true;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip[] sparkClips;

        private float timer;
        private float flashTimer;

        private void Awake()
        {
            if (particles == null) particles = GetComponent<ParticleSystem>();
            timer = Random.Range(interval.x, Mathf.Max(interval.x, interval.y));
        }

        private void Update()
        {
            if (flashLight != null)
            {
                flashTimer -= Time.deltaTime;
                flashLight.intensity = flashTimer > 0f ? flashIntensity * (flashTimer / flashDuration) : 0f;
            }

            bool auto = interval.y > 0f && !(stopWhenPowerRestored && PowerGrid.CurrentState == PowerState.Restored && PowerGrid.Instance != null);
            if (!auto) return;

            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = Random.Range(interval.x, interval.y);
            Burst();
        }

        /// <summary>Lanza una ráfaga. count &lt; 0 = cantidad aleatoria.</summary>
        public void Burst(int count = -1)
        {
            if (particles != null) particles.Emit(count < 0 ? Random.Range(burstCount.x, burstCount.y + 1) : count);
            flashTimer = flashDuration;
            if (audioSource != null && sparkClips != null && sparkClips.Length > 0)
                audioSource.PlayOneShot(sparkClips[Random.Range(0, sparkClips.Length)], 0.6f);
        }
    }
}
