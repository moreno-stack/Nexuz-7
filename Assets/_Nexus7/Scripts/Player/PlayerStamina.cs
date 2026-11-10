using UnityEngine;
using Nexus7.Core;
using Nexus7.Gameplay;

namespace Nexus7.Player
{
    /// <summary>
    /// Estamina del jugador (StaminaModel): limita la carrera a unos 6 s para que no pueda huir sin fin de los drones de élite.
    /// Al agotarse jadea (ruido de 3 m cada 0,8 s hasta recuperar el aliento). Se rellena al reaparecer.
    /// PlayerController la consulta para correr y saltar; StaminaUI la muestra.
    ///
    /// Colocar en: el GameObject raíz "Player" (PlayerController la añade si falta).
    /// </summary>
    public class PlayerStamina : MonoBehaviour
    {
        [Tooltip("Gasto por segundo al correr (100 = barra completa).")]
        [SerializeField] private float drainPerSecond = 17f;
        [SerializeField] private float jumpCost = 8f;
        [SerializeField] private float regenWalking = 14f;
        [SerializeField] private float regenIdle = 22f;
        [SerializeField] private float regenDelay = 1f;
        [Tooltip("Agotado: no puede volver a correr hasta tener este valor.")]
        [SerializeField] private float recoverThreshold = 35f;
        [SerializeField] private float pantingInterval = 0.8f;

        private StaminaModel model;
        private float pantTimer;

        public float Current => Model.Current;
        public float Normalized => Model.Normalized;
        public bool CanRun => Model.CanRun;
        public bool Exhausted => Model.Exhausted;

        private StaminaModel Model
        {
            get
            {
                if (model == null)
                {
                    model = new StaminaModel(DifficultySettings.StaminaDrainMultiplier)
                    {
                        DrainPerSecond = drainPerSecond, JumpCost = jumpCost, RegenWalking = regenWalking,
                        RegenIdle = regenIdle, RegenDelay = regenDelay, RecoverThreshold = recoverThreshold,
                    };
                }
                return model;
            }
        }

        private void OnEnable() => PlayerRespawn.PlayerRespawned += Refill;
        private void OnDisable() => PlayerRespawn.PlayerRespawned -= Refill;

        /// <summary>Lo llama PlayerController cada fotograma con lo que ha hecho el jugador.</summary>
        public void Tick(float dt, bool running, bool moving)
        {
            Model.DrainMultiplier = DifficultySettings.StaminaDrainMultiplier;
            if (Model.Tick(dt, running, moving)) pantTimer = 0f;   // acaba de agotarse: jadea ya
            if (!Model.Exhausted) return;
            pantTimer -= dt;
            if (pantTimer > 0f) return;
            pantTimer = pantingInterval;
            NoiseSystem.Emit(transform.position, PlayerNoise.Panting, gameObject);
        }

        public void OnJump() { if (Model.Jump()) pantTimer = 0f; }
        public void Refill() => Model.Refill();
    }
}
