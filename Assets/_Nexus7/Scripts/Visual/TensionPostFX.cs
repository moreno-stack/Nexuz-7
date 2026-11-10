using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Nexus7.Core;
using Nexus7.Player;

namespace Nexus7.Visual
{
    /// <summary>
    /// Ajusta el volumen global de posprocesado según la tensión del momento (PostFxModel):
    ///   · aberración cromática: 0 en calma, 0,15 en Alerta Amarilla, 0,35 en Roja y un pulso al recibir un golpe;
    ///   · viñeta: se cierra con la alerta y con poca vida;
    ///   · color: menos saturación y tinte rojizo en Alerta Roja o con poca vida.
    /// Trabaja sobre la copia del perfil en juego (volume.profile), así nunca modifica el asset.
    ///
    /// Colocar en: el objeto «VolumenPostProceso» (lo añade VisualUpgrade).
    /// </summary>
    [RequireComponent(typeof(Volume))]
    public class TensionPostFX : MonoBehaviour
    {
        [Tooltip("Rapidez con que la viñeta y el color siguen a la tensión.")]
        [SerializeField] private float speed = 3f;
        [Tooltip("Rapidez de la aberración cromática (más alta para que el golpe se note al instante).")]
        [SerializeField] private float chromaticSpeed = 12f;

        private ChromaticAberration chromatic;
        private Vignette vignette;
        private ColorAdjustments adjustments;
        private PlayerHealth health;
        private float searchTimer;
        private float hitPulse;
        private PostFxState current;
        private bool started;
        private VolumeProfile runtimeProfile;

        private void Awake()
        {
            var volume = GetComponent<Volume>();
            if (volume.sharedProfile == null) return;
            var profile = runtimeProfile = volume.profile;   // copia propia en juego (no toca el asset)
            profile.TryGet(out chromatic);
            profile.TryGet(out vignette);
            profile.TryGet(out adjustments);
        }

        // La copia del perfil se crea en Awake: se libera al salir de la escena
        private void OnDestroy() { if (runtimeProfile != null) Destroy(runtimeProfile); }

        private void OnEnable() => PlayerHealth.PlayerHit += OnPlayerHit;
        private void OnDisable() => PlayerHealth.PlayerHit -= OnPlayerHit;
        private void OnPlayerHit(Vector3 _) => hitPulse = 1f;

        private void Update()
        {
            if (health == null && (searchTimer -= Time.unscaledDeltaTime) <= 0f)
            {
                searchTimer = 1f;
                health = FindAnyObjectByType<PlayerHealth>();
            }

            float dt = Time.unscaledDeltaTime;
            hitPulse = PostFxModel.DecayPulse(hitPulse, dt);
            var target = PostFxModel.For(AlertSystem.Level, health != null ? health.Normalized : 1f, hitPulse);
            if (!started) { current = target; started = true; }
            else
            {
                current.chromatic = PostFxModel.Approach(current.chromatic, target.chromatic, dt, chromaticSpeed);
                current.vignette = PostFxModel.Approach(current.vignette, target.vignette, dt, speed);
                current.saturation = PostFxModel.Approach(current.saturation, target.saturation, dt, speed);
                current.colorFilter = new Color(
                    PostFxModel.Approach(current.colorFilter.r, target.colorFilter.r, dt, speed),
                    PostFxModel.Approach(current.colorFilter.g, target.colorFilter.g, dt, speed),
                    PostFxModel.Approach(current.colorFilter.b, target.colorFilter.b, dt, speed), 1f);
            }

            if (chromatic != null) chromatic.intensity.Override(current.chromatic);
            if (vignette != null) vignette.intensity.Override(current.vignette);
            if (adjustments != null)
            {
                adjustments.saturation.Override(current.saturation);
                adjustments.colorFilter.Override(current.colorFilter);
            }
        }
    }
}
