using UnityEngine;
using UnityEngine.AI;
using Nexus7.Characters;
using Nexus7.Player;

namespace Nexus7.Enemies
{
    /// <summary>
    /// Golpe cuerpo a cuerpo de un robot armado. EnemyPatrol lo llama al alcanzar al jugador durante la persecución.
    /// Anticipación visible (arma levantada + ojo rojo parpadeando) → impacto si el jugador sigue en el arco y el alcance
    /// (si se aparta, lo esquiva) → recuperación. Daño = arma × dificultad (lo aplica PlayerHealth: escudo → vida).
    ///
    /// Colocar en: el dron (junto a EnemyPatrol). 'Weapon' = WeaponData del tipo de robot.
    /// </summary>
    public class EnemyMeleeAttack : MonoBehaviour
    {
        [SerializeField] private WeaponData weapon;
        [SerializeField] private ProceduralAnimator animator;
        [Tooltip("Ojo del robot: parpadea en rojo durante la anticipación.")]
        [SerializeField] private Renderer eyeRenderer;
        [SerializeField] private float strikeAnimTime = 0.15f;
        [SerializeField] private float turnSpeed = 540f;

        private MeleeAttackModel model;
        private NavMeshAgent agent;
        private Transform player;
        private PlayerHealth playerHealth;
        private MaterialPropertyBlock block;
        private EnemyPatrol patrol;

        public WeaponData Weapon => weapon;
        public float Range => weapon != null ? weapon.Range : 1.5f;
        public bool IsAttacking => model != null && model.Current != MeleeAttackModel.Phase.Idle;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            patrol = GetComponent<EnemyPatrol>();
            if (animator == null) animator = GetComponent<ProceduralAnimator>();
            model = weapon != null ? new MeleeAttackModel(weapon.Windup, weapon.Recovery) : new MeleeAttackModel(0.55f, 1.2f);
        }

        private void Start()
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) { player = go.transform; playerHealth = go.GetComponent<PlayerHealth>(); }
        }

        /// <summary>Empieza un golpe si no hay otro en curso. Devuelve true si ha empezado.</summary>
        public bool TryAttack()
        {
            if (weapon == null || model == null || !model.Begin(Time.time)) return false;
            if (agent != null && agent.isOnNavMesh) { agent.isStopped = true; agent.ResetPath(); }
            if (animator != null) animator.PlayAttack(weapon.Windup, strikeAnimTime);
            return true;
        }

        /// <summary>PEM, aturdimiento, cinemática: corta el golpe.</summary>
        public void Cancel()
        {
            if (model == null || model.Current == MeleeAttackModel.Phase.Idle) return;
            model.Cancel();
            if (animator != null) animator.CancelAttack();
            SetEyeFlash(false);
            if (agent != null && agent.isOnNavMesh && (patrol == null || patrol.CanMove)) agent.isStopped = false;
        }

        private void Update()
        {
            if (!IsAttacking || player == null || EnemyPatrol.Paused) return;

            Vector3 to = player.position - transform.position; to.y = 0f;
            float dist = to.magnitude;
            if (model.Current == MeleeAttackModel.Phase.Windup && dist > 0.01f)
            {
                // Durante la anticipación el robot encara al jugador (pero no le persigue: se puede esquivar).
                var look = Quaternion.LookRotation(to / dist, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, look, turnSpeed * 0.25f * Time.deltaTime);
            }
            SetEyeFlash(model.Current == MeleeAttackModel.Phase.Windup && Mathf.Repeat(Time.time * 8f, 1f) < 0.5f);

            float angle = dist > 0.01f ? Vector3.Angle(transform.forward, to) : 0f;
            // Si una pared se interpone en el momento del golpe, cuenta como esquivado.
            bool reach = dist <= weapon.Range && (model.Current != MeleeAttackModel.Phase.Windup || HasClearLine());
            var ev = model.Tick(Time.time, MeleeAttackModel.InArc(angle, weapon.ArcDegrees), reach);
            if (ev == MeleeEvent.Hit && playerHealth != null)
            {
                playerHealth.TakeDamage(weapon.Damage, transform.position);
                if (weapon.StunSeconds > 0f) playerHealth.Stun(weapon.StunSeconds, 0f);
            }
            else if (ev == MeleeEvent.Finished)
            {
                SetEyeFlash(false);
                if (agent != null && agent.isOnNavMesh && (patrol == null || patrol.CanMove)) agent.isStopped = false;
            }
        }

        /// <summary>¿Algo que no es el robot ni el jugador se interpone (pared, estantería, caja, techo del conducto)?</summary>
        public static bool LineBlocked(RaycastHit[] hits, Transform self, Transform target)
        {
            if (hits == null) return false;
            foreach (var h in hits)
            {
                var t = h.collider != null ? h.collider.transform : h.transform;
                if (t == null) continue;
                if (self != null && t.IsChildOf(self)) continue;
                if (target != null && t.IsChildOf(target)) continue;
                return true;
            }
            return false;
        }

        /// <summary>Línea despejada de pecho a pecho (sin atravesar paredes ni el techo del conducto).</summary>
        public bool HasClearLine()
        {
            if (player == null) return false;
            Vector3 from = transform.position + Vector3.up * 1.2f;
            Vector3 to = player.position + Vector3.up * 1.0f;
            Vector3 d = to - from;
            float len = d.magnitude;
            if (len < 0.01f) return true;
            var hits = Physics.RaycastAll(new Ray(from, d / len), len, ~0, QueryTriggerInteraction.Ignore);
            return !LineBlocked(hits, transform, player);
        }

        private void SetEyeFlash(bool on)
        {
            if (eyeRenderer == null) return;
            if (block == null) block = new MaterialPropertyBlock();
            eyeRenderer.GetPropertyBlock(block);
            block.SetColor("_EmissionColor", on ? new Color(4f, 0.2f, 0.1f) : new Color(1.5f, 0.1f, 0.05f));
            eyeRenderer.SetPropertyBlock(block);
        }
    }
}
