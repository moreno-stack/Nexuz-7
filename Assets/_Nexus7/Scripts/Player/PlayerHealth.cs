using System;
using System.Collections;
using UnityEngine;
using Nexus7.Gameplay;

namespace Nexus7.Player
{
    /// <summary>
    /// Integridad (vida) del jugador. Recibe daño de descargas eléctricas, se regenera tras unos
    /// segundos sin daño y, al llegar a 0, reaparece en el último punto de control (PlayerRespawn).
    /// Stun(): aturdimiento — bloquea el control unos segundos (bloqueo temporal de las trampas).
    ///
    /// Colocar en: el GameObject raíz "Player".
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerHealth : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 100f;
        [Tooltip("Segundos sin recibir daño antes de empezar a regenerar.")]
        [SerializeField] private float regenDelay = 5f;
        [SerializeField] private float regenPerSecond = 6f;
        [SerializeField, TextArea] private string deathMessage =
            "Descarga eléctrica letal. Reiniciando sistemas vitales en el último punto seguro...";

        public float Current { get; private set; }
        public float Max => maxHealth;
        public float Normalized => maxHealth > 0f ? Current / maxHealth : 0f;
        public bool IsStunned { get; private set; }

        /// <summary>Cantidad de daño recibida (la UI hace un destello rojo).</summary>
        public event Action<float> Damaged;
        /// <summary>(actual, máximo)</summary>
        public event Action<float, float> HealthChanged;

        private PlayerController controller;
        private PlayerRespawn respawn;
        private float lastDamageTime = -999f;
        private Coroutine stunRoutine;

        private void Awake()
        {
            controller = GetComponent<PlayerController>();
            respawn = GetComponent<PlayerRespawn>();
            Current = maxHealth;
        }

        private void OnEnable() => PlayerRespawn.PlayerRespawned += OnRespawned;
        private void OnDisable() => PlayerRespawn.PlayerRespawned -= OnRespawned;

        private void Update()
        {
            if (Current > 0f && Current < maxHealth && Time.time - lastDamageTime > regenDelay)
                Heal(regenPerSecond * Time.deltaTime);
        }

        public void TakeDamage(float amount)
        {
            if (amount <= 0f || Current <= 0f || (respawn != null && respawn.IsRespawning)) return;

            Current = Mathf.Max(0f, Current - amount);
            lastDamageTime = Time.time;
            Damaged?.Invoke(amount);
            HealthChanged?.Invoke(Current, maxHealth);

            if (Current <= 0f) Die();
        }

        /// <summary>Descarga que aturde: daño + control bloqueado 'duration' segundos.</summary>
        public void Stun(float duration, float damage)
        {
            TakeDamage(damage);
            if (Current <= 0f || (respawn != null && respawn.IsRespawning)) return;
            if (stunRoutine != null) StopCoroutine(stunRoutine);
            stunRoutine = StartCoroutine(StunRoutine(duration));
        }

        public void Heal(float amount)
        {
            if (amount <= 0f) return;
            Current = Mathf.Min(maxHealth, Current + amount);
            HealthChanged?.Invoke(Current, maxHealth);
        }

        private IEnumerator StunRoutine(float duration)
        {
            IsStunned = true;
            controller.AddInputLock(this);
            yield return new WaitForSeconds(duration);
            EndStun();
        }

        private void EndStun()
        {
            if (stunRoutine != null) StopCoroutine(stunRoutine);
            stunRoutine = null;
            IsStunned = false;
            controller.RemoveInputLock(this);
        }

        private void Die()
        {
            EndStun();
            if (respawn != null) respawn.Respawn(deathMessage);
            else Heal(maxHealth);
        }

        private void OnRespawned()
        {
            EndStun();
            Current = maxHealth;
            HealthChanged?.Invoke(Current, maxHealth);
        }
    }
}
