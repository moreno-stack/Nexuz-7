using UnityEngine;
using Nexus7.Player;
using Nexus7.Enemies;

namespace Nexus7.Stealth
{
    /// <summary>
    /// Calcula qué tan VISIBLE es el jugador (0 = invisible, 1 = normal, &gt;1 = llamativo).
    /// Los enemigos multiplican su velocidad de detección por este valor.
    ///
    ///   Agachado ×0.5 · Quieto ×0.8 · Corriendo ×1.4 · En zona de sombra ×0.45
    ///   Agachado y quieto en sombra ≈ 0.18 → "Oculto".
    ///
    /// También decide el ALCANCE de visión de los robots (PerceptionModel.Exposure):
    ///   agachado en sombra o con camuflaje → ×0,6 · corriendo o bajo luz directa (PlayerLightExposure) → ×1,3.
    ///
    /// Colocar en: el GameObject raíz "Player".
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerStealth : MonoBehaviour
    {
        [SerializeField] private float crouchMultiplier = 0.5f;
        [SerializeField] private float idleMultiplier = 0.8f;
        [SerializeField] private float runMultiplier = 1.4f;
        [SerializeField] private float shadowMultiplier = 0.45f;
        [Tooltip("Por debajo de este valor el jugador cuenta como oculto.")]
        [SerializeField] private float hiddenThreshold = 0.3f;

        private PlayerController controller;
        private int shadowZoneCount; // contador: las zonas pueden solaparse

        public float Visibility { get; private set; } = 1f;
        public bool InShadow => shadowZoneCount > 0;
        public bool IsHidden => Visibility <= hiddenThreshold;
        /// <summary>Bajo luz directa (y fuera de las zonas de sombra).</summary>
        public bool IsLit => !InShadow && lightExposure != null && lightExposure.IsLit;
        public StealthExposure Exposure { get; private set; } = StealthExposure.Normal;
        /// <summary>Multiplicador del alcance de visión de los robots: 0,6 / 1 / 1,3.</summary>
        public float VisionRangeMultiplier => PerceptionModel.RangeMultiplier(Exposure);

        private PlayerLightExposure lightExposure;

        // ---- Camuflaje óptico (Fase 4) ----
        private float cloakUntil = -999f;
        private float cloakMultiplier = 1f;
        public bool IsCloaked => Time.time < cloakUntil;
        public float CloakRemaining => Mathf.Max(0f, cloakUntil - Time.time);

        /// <summary>Multiplica la visibilidad por 'multiplier' durante 'duration' segundos.</summary>
        public void ApplyCloak(float duration, float multiplier)
        {
            cloakUntil = Time.time + duration;
            cloakMultiplier = Mathf.Clamp01(multiplier);
        }

        private void Awake()
        {
            controller = GetComponent<PlayerController>();
            lightExposure = GetComponent<PlayerLightExposure>();
            if (lightExposure == null) lightExposure = gameObject.AddComponent<PlayerLightExposure>();
        }

        // Al teletransportar el CharacterController (reaparición) Unity no llama a OnTriggerExit de las zonas
        // de sombra que deja atrás: se reinicia el contador y las zonas del nuevo punto vuelven a avisar al entrar.
        private void OnEnable() => Nexus7.Gameplay.PlayerRespawn.PlayerRespawned += ResetShadows;
        private void OnDisable() => Nexus7.Gameplay.PlayerRespawn.PlayerRespawned -= ResetShadows;
        public void ResetShadows() => shadowZoneCount = 0;

        private void Update()
        {
            float v = 1f;
            if (controller.IsCrouching) v *= crouchMultiplier;
            if (controller.State == MovementState.Running) v *= runMultiplier;
            else if (controller.State == MovementState.Idle) v *= idleMultiplier;
            if (InShadow) v *= shadowMultiplier;
            if (IsCloaked) v *= cloakMultiplier;
            Visibility = v;
            Exposure = PerceptionModel.Exposure(controller.IsCrouching, InShadow, IsCloaked,
                                                controller.State == MovementState.Running, IsLit);
        }

        public void EnterShadow() => shadowZoneCount++;
        public void ExitShadow() => shadowZoneCount = Mathf.Max(0, shadowZoneCount - 1);
    }
}
