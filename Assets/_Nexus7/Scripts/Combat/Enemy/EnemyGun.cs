using UnityEngine;
using UnityEngine.AI;
using Nexus7.Core;
using Nexus7.Enemies;
using Nexus7.Gameplay;

namespace Nexus7.Combat
{
    /// <summary>
    /// Arma a distancia de un robot (seguridad, guardia, élite). Solo dispara si:
    ///   · está persiguiendo al jugador, lo ve, y está entre 3,5 y 24 m (más cerca sigue el cuerpo a cuerpo);
    ///   · hay línea de tiro limpia desde su mano hasta el pecho del jugador;
    ///   · consigue un TURNO de ataque (AttackTokens): como mucho 1/2/3 robots disparan a la vez según la dificultad.
    /// Cada ráfaga empieza con un AVISO de 0,4 s (láser rojo + pitido) y el robot se queda quieto mientras dispara.
    /// Si el jugador le mete 15 de daño en 0,3 s se tambalea: corta la ráfaga y suelta el turno.
    /// Los proyectiles son esquivables (35 m/s) y tienen dispersión según distancia, movimiento y dificultad (AimModel).
    ///
    /// Colocar en: la raíz del robot (lo añade CombatInstaller según el tipo; investigación no lleva arma).
    /// </summary>
    public class EnemyGun : MonoBehaviour
    {
        /// <summary>Turnos compartidos por todos los robots de la escena.</summary>
        public static readonly AttackTokens Tokens = new AttackTokens();
        /// <summary>El director suma tiradores extra (+1 con tensión alta).</summary>
        public static int DirectorExtraShooters { get; set; }
        /// <summary>El director multiplica la dispersión (&lt; 1 = mejor puntería).</summary>
        public static float DirectorSpreadMultiplier { get; set; } = 1f;

        private const float ChestHeight = 1.1f;
        private static int nextId;
        private static readonly RaycastHit[] lineHits = new RaycastHit[8];

        public EnemyGunSpec Spec { get; private set; }
        public bool IsWarningOrFiring => burst != null && burst.IsWarningOrFiring;
        /// <summary>Ráfagas disparadas enteras (EnemyTactics lo usa para volver a cubrirse).</summary>
        public int BurstsCompleted { get; private set; }
        /// <summary>Hay turno de ataque para este robot (libre o ya suyo).</summary>
        public bool TokenAvailable => Tokens.Has(id) || Tokens.InUse < Tokens.Capacity;
        /// <summary>Permiso extra para disparar (EnemyTactics: solo asomado o al descubierto). Null = siempre.</summary>
        public System.Func<bool> FireGate;

        private BurstModel burst;
        private readonly StaggerModel stagger = new StaggerModel();
        private EnemyPatrol patrol;
        private EnemyVision vision;
        private EnemyMeleeAttack melee;
        private NavMeshAgent agent;
        private Transform muzzle, player;
        private CharacterController playerBody;
        private PlayerRespawn playerRespawn;
        private LineRenderer laser;
        private System.Random rng;
        private int id;
        private bool holdingAgent;
        private float dazzledUntil = -99f, noBurstUntil = -99f;

        /// <summary>Lo llama CombatInstaller justo después de añadirlo.</summary>
        public void Configure(EnemyGunSpec spec)
        {
            Spec = spec;
            burst = new BurstModel(spec);
        }

        private void Awake()
        {
            id = ++nextId;
            rng = new System.Random(id);
            patrol = GetComponent<EnemyPatrol>();
            vision = GetComponent<EnemyVision>();
            melee = GetComponent<EnemyMeleeAttack>();
            agent = GetComponent<NavMeshAgent>();
            muzzle = Nexus7.Characters.CharacterRigFactory.FindDeep(transform, Nexus7.Characters.RigJoints.HandSocket);
            if (Spec == null) Configure(EnemyGunCatalog.ForRobot(name) ?? EnemyGunCatalog.Security);
            BuildLaser();
        }

        private void OnDisable()
        {
            Tokens.Release(id);
            if (laser != null) laser.enabled = false;
            ResumeAgent();
        }

        private void Update()
        {
            float now = Time.time;
            if (!FindPlayer()) return;
            Tokens.Capacity = AttackTokens.CapacityFor((int)DifficultySettings.Current, DirectorExtraShooters);
            Vector3 target = player.position + Vector3.up * ChestHeight;
            bool canShoot = CanEngage(target, now);

            if (burst.Current == BurstModel.Phase.Idle)
            {
                if (canShoot && now >= noBurstUntil && Tokens.TryAcquire(id) && burst.Begin(now))
                {
                    HoldAgent();
                    CombatSfx.PlayAt(CombatSound.EnemyWarn, transform.position);
                }
                else if (!canShoot) Tokens.Release(id);
            }

            switch (burst.Tick(now, canShoot, rng.NextDouble()))
            {
                case BurstEvent.Fire:
                    Fire(target);
                    if (burst.Current == BurstModel.Phase.Pause) BurstsCompleted++;
                    break;
                case BurstEvent.Cancelled: EndBurst(); break;
                case BurstEvent.Finished: Tokens.Release(id); break;
            }
            if (burst.Current == BurstModel.Phase.Pause && holdingAgent) EndBurst();

            UpdateLaser(target);
            if (holdingAgent) FaceTarget(target);
        }

        /// <summary>Disparo en la óptica: 2 s de mala puntería (+6°) y 0,8 s sin empezar ráfagas; corta el aviso en curso.</summary>
        public void Dazzle()
        {
            float now = Time.time;
            dazzledUntil = now + WeakPointModel.DazzleSeconds;
            noBurstUntil = now + WeakPointModel.DazzleNoBurstSeconds;
            if (burst.Current == BurstModel.Phase.Warning) { burst.Cancel(now, rng.NextDouble()); EndBurst(); }
        }

        /// <summary>Daño recibido: si acumula 15 en 0,3 s se tambalea y corta la ráfaga (lo llama EnemyHealth).</summary>
        public void RegisterDamage(float amount)
        {
            if (!stagger.AddDamage(Time.time, amount)) return;
            burst.Cancel(Time.time, rng.NextDouble());
            EndBurst();
        }

        private bool CanEngage(Vector3 target, float now)
        {
            if (Spec == null || EnemyPatrol.Paused || stagger.IsStaggered(now)) return false;
            if (FireGate != null && !FireGate()) return false;
            if (patrol == null || !patrol.enabled || patrol.CurrentState != EnemyPatrol.EnemyState.Chase) return false;
            if (vision == null || !vision.CanSeePlayer) return false;
            if (melee != null && melee.IsAttacking) return false;
            if (playerRespawn != null && playerRespawn.IsRespawning) return false;
            float d = Vector3.Distance(transform.position, player.position);
            if (d < Spec.MinRange || d > Spec.MaxRange) return false;
            return ClearLine(MuzzlePosition(), target);
        }

        /// <summary>Línea de tiro: el primer obstáculo (que no sea el propio robot) debe ser el jugador.</summary>
        private bool ClearLine(Vector3 from, Vector3 to)
        {
            Vector3 dir = to - from;
            float dist = dir.magnitude;
            if (dist < 0.01f) return true;
            int n = Physics.RaycastNonAlloc(from, dir / dist, lineHits, dist, ~(1 << 2), QueryTriggerInteraction.Ignore);
            float nearest = float.MaxValue;
            bool playerFirst = true;
            for (int i = 0; i < n; i++)
            {
                var t = lineHits[i].collider != null ? lineHits[i].collider.transform : null;
                if (t == null || t.IsChildOf(transform)) continue;
                if (lineHits[i].distance >= nearest) continue;
                nearest = lineHits[i].distance;
                playerFirst = t.IsChildOf(player);
            }
            return playerFirst;
        }

        private void Fire(Vector3 target)
        {
            Vector3 from = MuzzlePosition();
            float distance = Vector3.Distance(from, target);
            Vector3 v = playerBody != null ? playerBody.velocity : Vector3.zero;
            AimModel.Lead(target.x, target.y, target.z, v.x, 0f, v.z, distance, Spec.Projectile.ProjectileSpeed, out float ax, out float ay, out float az);
            Vector3 aim = new Vector3(ax, ay, az) - from;

            float speed = new Vector3(v.x, 0f, v.z).magnitude;
            bool moving = agent != null && agent.enabled && agent.velocity.sqrMagnitude > 0.04f;
            float spread = AimModel.Spread(distance, speed, moving, (vision != null && vision.IsBlinded) || Time.time < dazzledUntil,
                                           (int)DifficultySettings.Current, DirectorSpreadMultiplier);
            Vector3 dir = Quaternion.AngleAxis(RandomSigned() * spread, Vector3.up)
                        * Quaternion.AngleAxis(RandomSigned() * spread * 0.5f, transform.right) * aim.normalized;

            var info = new DamageInfo
            {
                Amount = Spec.Projectile.Damage, Origin = transform.position, Source = gameObject,
                WeaponId = Spec.Projectile.Id, Direction = dir,
            };
            ProjectilePool.Instance.Get().Launch(from, dir, Spec.Projectile, info, transform, true);
            CombatSfx.PlayAt(CombatSound.EnemyShot, from);
            NoiseSystem.Emit(transform.position, Spec.NoiseRadius, gameObject);
        }

        private float RandomSigned() => (float)(rng.NextDouble() * 2.0 - 1.0);

        private Vector3 MuzzlePosition() => muzzle != null ? muzzle.position : transform.position + Vector3.up * 1.3f + transform.forward * 0.4f;

        private void EndBurst()
        {
            Tokens.Release(id);
            ResumeAgent();
        }

        private void HoldAgent()
        {
            holdingAgent = true;
            if (agent != null && agent.enabled && agent.isOnNavMesh) agent.isStopped = true;
        }

        private void ResumeAgent()
        {
            if (!holdingAgent) return;
            holdingAgent = false;
            if (agent != null && agent.enabled && agent.isOnNavMesh && (patrol == null || patrol.CanMove)) agent.isStopped = false;
        }

        private void FaceTarget(Vector3 target)
        {
            Vector3 flat = target - transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude < 0.01f) return;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(flat), 360f * Time.deltaTime);
        }

        private bool FindPlayer()
        {
            if (player != null) return true;
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go == null) return false;
            player = go.transform;
            playerBody = go.GetComponent<CharacterController>();
            playerRespawn = go.GetComponent<PlayerRespawn>();
            return true;
        }

        /// <summary>Láser de aviso: rojo, fino, solo durante los 0,4 s previos a la ráfaga.</summary>
        private void BuildLaser()
        {
            var existing = transform.Find("LaserAviso");   // los refuerzos son copias: ya lo traen
            laser = existing != null ? existing.GetComponent<LineRenderer>() : null;
            if (laser != null) { laser.enabled = false; return; }
            var go = new GameObject("LaserAviso");
            go.transform.SetParent(transform, false);
            laser = go.AddComponent<LineRenderer>();
            laser.positionCount = 2;
            laser.useWorldSpace = true;
            laser.widthMultiplier = 0.025f;
            laser.material = new Material(Shader.Find("Sprites/Default"));
            laser.startColor = new Color(1f, 0.15f, 0.1f, 0.9f);
            laser.endColor = new Color(1f, 0.15f, 0.1f, 0.25f);
            laser.enabled = false;
        }

        private void UpdateLaser(Vector3 target)
        {
            bool on = burst.Current == BurstModel.Phase.Warning;
            laser.enabled = on;
            if (!on) return;
            laser.SetPosition(0, MuzzlePosition());
            laser.SetPosition(1, target);
        }
    }
}
