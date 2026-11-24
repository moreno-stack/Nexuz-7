using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Nexus7.Core;
using Nexus7.Enemies;

namespace Nexus7.Combat
{
    /// <summary>
    /// Vida de un robot: recibe los disparos del jugador (IDamageable).
    ///   · Vida según el tipo (HealthModel.BaseHealthFor): seguridad 60, guardia 90, investigación 50, élite 120.
    ///   · Disparo en la cabeza = crítico ×2.
    ///   · Al recibir un impacto «da un respingo», y si no estaba persiguiendo va corriendo a investigar de dónde vino el disparo.
    ///   · Destruido: se apagan patrulla, visión, ataque y agente, cae hacia atrás, suelta munición y hace ruido.
    ///     Sigue destruido aunque el jugador reaparezca (los robots desactivados no se reinician).
    ///
    /// Colocar en: la raíz del robot, junto a EnemyPatrol (lo añade CombatInstaller si falta).
    /// </summary>
    public class EnemyHealth : MonoBehaviour, IDamageable
    {
        public static readonly List<EnemyHealth> All = new List<EnemyHealth>();

        [Tooltip("Vida máxima. ≤ 0 = según el tipo de robot (por su nombre).")]
        [SerializeField] private float maxHealth = -1f;
        [Tooltip("Paquetes de munición que suelta al ser destruido.")]
        [SerializeField] private float ammoPacks = 1f;

        /// <summary>Multiplicador global de la vida de los robots (lo ajusta el director adaptativo).</summary>
        public static float HealthMultiplier { get; private set; } = 1f;
        /// <summary>Multiplicador del daño recibido según el arma (el director lo usa contra el abuso de una sola arma).</summary>
        public static System.Func<string, float> WeaponDamageModifier;

        public bool IsAlive => model != null && !model.IsDead;
        public float Normalized => model != null ? model.Normalized : 0f;
        public float LastHitTime { get; private set; } = -999f;
        public bool LastHitCritical { get; private set; }
        /// <summary>Punto sobre la cabeza para la barra de vida del HUD.</summary>
        public Vector3 BarPosition => (head != null ? head.position : transform.position + Vector3.up * 1.8f) + Vector3.up * 0.45f;
        /// <summary>Robot añadido por el director (refuerzo): lo retira al reiniciar el tramo.</summary>
        public bool IsReinforcement { get; set; }

        private HealthModel model;
        private EnemyPatrol patrol;
        private EnemyGun gun;
        private Transform head;
        private float baseMax;
        private Vector3 baseScale = Vector3.one;
        private Coroutine punch;

        private void Awake()
        {
            patrol = GetComponent<EnemyPatrol>();
            gun = GetComponent<EnemyGun>();
            baseScale = transform.localScale;
            baseMax = maxHealth > 0f ? maxHealth : HealthModel.BaseHealthFor(name);
            model = new HealthModel(baseMax * HealthMultiplier);
            head = Nexus7.Characters.CharacterRigFactory.FindDeep(transform, Nexus7.Characters.RigJoints.Head);
        }

        private void OnEnable() => All.Add(this);
        private void OnDisable() => All.Remove(this);

        /// <summary>El director cambia la vida de todos los robots vivos (conservan su porcentaje).</summary>
        public static void SetHealthMultiplier(float multiplier)
        {
            HealthMultiplier = Mathf.Clamp(multiplier, 0.5f, 2f);
            foreach (var e in All) if (e != null && e.model != null) e.model.Rescale(e.baseMax * HealthMultiplier);
        }

        public void TakeDamage(DamageInfo info)
        {
            if (!IsAlive) return;
            info.Critical = head != null && info.Point.y >= head.position.y - 0.2f;
            float amount = info.Amount * (info.Critical ? HealthModel.CriticalMultiplier : 1f);
            if (WeaponDamageModifier != null) amount *= WeaponDamageModifier(info.WeaponId);
            info.Amount = amount;

            var result = model.Apply(amount);
            if (gun != null && result != HealthResult.Killed) gun.RegisterDamage(amount);
            LastHitTime = Time.time;
            LastHitCritical = info.Critical;
            CombatSfx.PlayAt(CombatSound.HitMetal, info.Point);

            if (result == HealthResult.Killed) Die(info);
            else
            {
                if (punch != null) StopCoroutine(punch);
                punch = StartCoroutine(Punch());
                ReactToHit(info);
            }
            CombatEvents.RaiseHit(info, result == HealthResult.Killed);
        }

        /// <summary>Le han disparado: si no estaba persiguiendo, va a investigar de dónde vino el disparo y queda alerta.</summary>
        private void ReactToHit(DamageInfo info)
        {
            if (patrol == null || !patrol.enabled) return;
            if (patrol.Vision != null) patrol.Vision.BoostDetection(1.8f, 10f);
            if (patrol.CurrentState != EnemyPatrol.EnemyState.Chase) patrol.RespondToSquad(info.Origin);
        }

        private IEnumerator Punch()
        {
            transform.localScale = baseScale * 1.06f;
            yield return new WaitForSeconds(0.08f);
            transform.localScale = baseScale;
            punch = null;
        }

        private void Die(DamageInfo info)
        {
            transform.localScale = baseScale;
            var melee = GetComponent<EnemyMeleeAttack>();
            if (melee != null) { melee.Cancel(); melee.enabled = false; }
            if (gun == null) gun = GetComponent<EnemyGun>();
            if (gun != null) gun.enabled = false;
            var tactics = GetComponent<EnemyTactics>();
            if (tactics != null) tactics.enabled = false;   // suelta su cobertura
            if (patrol != null) patrol.enabled = false;
            var vision = GetComponent<EnemyVision>();
            if (vision != null) vision.enabled = false;
            var agent = GetComponent<NavMeshAgent>();
            if (agent != null) agent.enabled = false;
            var anim = GetComponent<Nexus7.Characters.ProceduralAnimator>();
            if (anim != null) anim.enabled = false;
            foreach (var l in GetComponentsInChildren<Light>(true)) l.enabled = false;   // se apaga el foco de visión

            NoiseSystem.Emit(transform.position, 12f, gameObject);
            CombatSfx.PlayAt(CombatSound.Explosion, transform.position);
            Nexus7.Devices.PulseRing.Spawn(transform.position, 2.5f, new Color(1f, 0.55f, 0.2f), 0.5f);
            AmmoPickup.Spawn(transform.position + transform.right * 0.6f + Vector3.up * 0.4f, ammoPacks * AmmoPickup.DropMultiplier);
            StartCoroutine(Topple(info.Direction));
            CombatEvents.RaiseKilled(this);
        }

        /// <summary>Cae hacia donde lo empujó el disparo en 0,6 s. El colisionador se queda: el robot caído sirve de cobertura.</summary>
        private IEnumerator Topple(Vector3 hitDirection)
        {
            Vector3 dir = hitDirection;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) dir = -transform.forward;
            Vector3 axis = Vector3.Cross(Vector3.up, dir.normalized);
            Quaternion from = transform.rotation, to = Quaternion.AngleAxis(80f, axis) * from;
            for (float t = 0f; t < 0.6f; t += Time.deltaTime)
            {
                transform.rotation = Quaternion.Slerp(from, to, t / 0.6f);
                yield return null;
            }
            transform.rotation = to;
        }
    }
}
