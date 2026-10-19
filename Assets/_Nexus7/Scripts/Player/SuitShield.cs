using System;
using UnityEngine;
using Nexus7.Core;

namespace Nexus7.Player
{
    /// <summary>
    /// Escudo del traje espacial. Solo actúa cuando el jugador lleva el traje (Equip()).
    /// PlayerHealth le pasa el daño primero; la recarga depende de la dificultad.
    ///
    /// Colocar en: el GameObject raíz "Player" (junto a PlayerHealth).
    /// </summary>
    public class SuitShield : MonoBehaviour
    {
        [SerializeField] private float capacity = 50f;

        public ShieldModel Model { get; private set; }
        public bool Equipped { get; private set; }

        /// <summary>Daño absorbido por el escudo (la UI y el traje hacen un destello azul).</summary>
        public event Action<float> ShieldHit;
        /// <summary>(valor, capacidad)</summary>
        public event Action<float, float> ShieldChanged;

        private void Awake() => Model = new ShieldModel(capacity);
        private void OnEnable() => Nexus7.Gameplay.PlayerRespawn.PlayerRespawned += OnRespawned;
        private void OnDisable() => Nexus7.Gameplay.PlayerRespawn.PlayerRespawned -= OnRespawned;
        private void OnRespawned() { if (Equipped) Refill(); }

        private void Update()
        {
            if (!Equipped) return;
            float before = Model.Value;
            Model.Tick(Time.deltaTime, Time.time, DifficultySettings.ShieldRegenMultiplier);
            if (Model.Value != before) ShieldChanged?.Invoke(Model.Value, Model.Capacity);
        }

        public void Equip()
        {
            if (Model == null) Model = new ShieldModel(capacity);
            Equipped = true;
            Model.Refill();
            ShieldChanged?.Invoke(Model.Value, Model.Capacity);
        }

        /// <summary>Devuelve el daño que pasa a la vida.</summary>
        public float Absorb(float damage)
        {
            if (!Equipped || Model == null || damage <= 0f) return damage;
            float rest = Model.Absorb(damage, Time.time);
            float absorbed = damage - rest;
            if (absorbed > 0f)
            {
                ShieldHit?.Invoke(absorbed);
                ShieldChanged?.Invoke(Model.Value, Model.Capacity);
            }
            return rest;
        }

        public void Refill()
        {
            if (Model == null) return;
            Model.Refill();
            ShieldChanged?.Invoke(Model.Value, Model.Capacity);
        }
    }
}
