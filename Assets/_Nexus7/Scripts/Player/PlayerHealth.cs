using System;
using System.Collections;
using UnityEngine;
using Nexus7.Core;
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
        [Tooltip("Velocidad del empujón al recibir un golpe (m/s).")]
        [SerializeField] private float knockback = 3f;
        [Tooltip("false = en la zona final la muerte no reaparece: la gestiona EndingManager (final crítico).")]
        [SerializeField] private bool respawnOnDeath = true;
        [Tooltip("Escudo del traje (si lo lleva puesto absorbe el daño primero).")]
        [SerializeField] private SuitShield shield;
        [SerializeField, TextArea] private string deathMessage =
            "Descarga eléctrica letal. Reiniciando sistemas vitales en el último punto seguro...";

        public float Current { get; private set; }
        public float Max => maxHealth;
        public float Normalized => maxHealth > 0f ? Current / maxHealth : 0f;
        public bool IsStunned { get; private set; }
        /// <summary>El último daño a la vida lo hizo un enemigo (fuera de combate = captura).</summary>
        public bool LastDamageFromEnemy { get; private set; }

        /// <summary>Se dispara cuando la integridad llega a 0.</summary>
        public static event Action PlayerDied;
        /// <summary>Un enemigo ha golpeado al jugador (posición del atacante).</summary>
        public static event Action<Vector3> PlayerHit;

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
            if (shield == null) shield = GetComponent<SuitShield>();
            Current = maxHealth;
        }

        private void OnEnable() => PlayerRespawn.PlayerRespawned += OnRespawned;
        private void OnDisable() => PlayerRespawn.PlayerRespawned -= OnRespawned;

        private void Update()
        {
            if (Current > 0f && Current < maxHealth && Time.time - lastDamageTime > regenDelay)
                Heal(regenPerSecond * Time.deltaTime);
        }

        /// <summary>Daño ambiental (descargas, trampas). Se escala por la dificultad.</summary>
        public void TakeDamage(float amount) => ApplyDamage(amount, transform.position, false);

        /// <summary>Golpe de un enemigo desde 'origin': escala por dificultad, escudo, vida, empujón y aviso a la alerta.</summary>
        public void TakeDamage(float amount, Vector3 origin) => ApplyDamage(amount, origin, true);

        private void ApplyDamage(float amount, Vector3 origin, bool fromEnemy)
        {
            if (amount <= 0f || Current <= 0f || (respawn != null && respawn.IsRespawning)) return;

            float scaled = amount * DifficultySettings.DamageMultiplier;
            float rest = shield != null ? shield.Absorb(scaled) : scaled;
            lastDamageTime = Time.time;

            if (fromEnemy)
            {
                Vector3 away = transform.position - origin;
                away.y = 0f;
                if (controller != null && away.sqrMagnitude > 0.0001f) controller.AddImpulse(away.normalized * knockback);
                PlayerHit?.Invoke(origin);
            }
            if (rest <= 0f) return;

            LastDamageFromEnemy = fromEnemy;
            float applied = Mathf.Min(rest, Current);
            RunStats.RecordDamage(applied);
            Current = Mathf.Max(0f, Current - rest);
            Damaged?.Invoke(applied);
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
            RunStats.RecordDeath();
            PlayerDied?.Invoke();
            if (!respawnOnDeath) return;               // la gestiona el final (EndingManager)
            if (respawn != null) { if (LastDamageFromEnemy) respawn.KnockedOut(); else respawn.Respawn(deathMessage); }
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
