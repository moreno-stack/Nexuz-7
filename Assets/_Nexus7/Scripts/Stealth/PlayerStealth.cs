using UnityEngine;
using Nexus7.Player;

namespace Nexus7.Stealth
{
    /// <summary>
    /// Calcula qué tan VISIBLE es el jugador (0 = invisible, 1 = normal, &gt;1 = llamativo).
    /// Los enemigos multiplican su velocidad de detección por este valor.
    ///
    ///   Agachado ×0.5 · Quieto ×0.8 · Corriendo ×1.4 · En zona de sombra ×0.45
    ///   Agachado y quieto en sombra ≈ 0.18 → "Oculto": solo te ven si están casi encima.
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

        private void Awake() => controller = GetComponent<PlayerController>();

        private void Update()
        {
            float v = 1f;
            if (controller.IsCrouching) v *= crouchMultiplier;
            if (controller.State == MovementState.Running) v *= runMultiplier;
            else if (controller.State == MovementState.Idle) v *= idleMultiplier;
            if (InShadow) v *= shadowMultiplier;
            if (IsCloaked) v *= cloakMultiplier;
            Visibility = v;
        }

        public void EnterShadow() => shadowZoneCount++;
        public void ExitShadow() => shadowZoneCount = Mathf.Max(0, shadowZoneCount - 1);
    }
}
