using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Nexus7.Core;
using Nexus7.Gameplay;

namespace Nexus7.Enemies
{
    /// <summary>
    /// Dron de seguridad básico con máquina de estados:
    ///
    ///   PATROL ──(ve algo, alerta ≥ umbral / oye ruido)──► INVESTIGATE ──(llega)──► SEARCH ──(tiempo)──► PATROL
    ///     │                                                     │
    ///     └──────────────(alerta = 1)──────────────► CHASE ◄────┘
    ///                                                  │ pierde de vista X s → INVESTIGATE (última posición)
    ///                                                  │ distancia ≤ catchDistance → captura → PlayerRespawn
    ///
    /// Se mueve con NavMeshAgent (el NavMesh lo genera RuntimeNavMeshBaker al iniciar la escena).
    /// Escucha el NoiseSystem: correr, abrir contenedores o puertas cerca de él lo atrae.
    ///
    /// Colocar en: la raíz del enemigo, junto a NavMeshAgent y EnemyVision.
    /// Deja el NavMeshAgent DESACTIVADO en la escena: se activa en Start, cuando el NavMesh ya existe.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(EnemyVision))]
    public class EnemyPatrol : MonoBehaviour
    {
        public enum EnemyState { Patrol, Investigate, Search, Chase }

        /// <summary>Todos los enemigos activos (lo usa el indicador de sigilo del HUD).</summary>
        public static readonly List<EnemyPatrol> All = new List<EnemyPatrol>();

        [SerializeField] private PatrolRoute route;

        [Header("Velocidades")]
        [SerializeField] private float patrolSpeed = 1.8f;
        [SerializeField] private float investigateSpeed = 2.6f;
        [Tooltip("Menor que la carrera del jugador (6): se le puede dejar atrás corriendo... haciendo ruido.")]
        [SerializeField] private float chaseSpeed = 4.4f;

        [Header("Comportamiento")]
        [Range(0.05f, 0.95f)]
        [SerializeField] private float suspicionThreshold = 0.35f;
        [Tooltip("Multiplica el radio de los ruidos que oye (1 = normal).")]
        [SerializeField] private float hearingMultiplier = 1f;
        [SerializeField] private float searchDuration = 4f;
        [SerializeField] private float searchTurnSpeed = 90f;
        [SerializeField] private float loseSightTime = 2.5f;
        [SerializeField] private float catchDistance = 1.3f;

        [Header("Audio (opcional)")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip suspiciousClip;
        [SerializeField] private AudioClip alertClip;

        public EnemyState CurrentState { get; private set; } = EnemyState.Patrol;
        public EnemyVision Vision => vision;

        private NavMeshAgent agent;
        private EnemyVision vision;
        private Transform player;
        private PlayerRespawn playerRespawn;

        private int waypointIndex;
        private int routeDirection = 1;
        private float waitTimer;
        private float stateTimer;
        private float lostSightTimer;
        private float repathTimer;
        private bool frozen; // durante la secuencia de captura
        private Vector3 startPosition;
        private Quaternion startRotation;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            vision = GetComponent<EnemyVision>();
            startPosition = transform.position;
            startRotation = transform.rotation;
        }

        private void OnEnable()
        {
            All.Add(this);
            NoiseSystem.NoiseEmitted += OnNoiseHeard;
            AlarmSystem.AlarmRaised += OnAlarm;
            PlayerRespawn.PlayerCaught += OnPlayerCaught;
            PlayerRespawn.PlayerRespawned += ResetToStart;
        }

        private void OnDisable()
        {
            All.Remove(this);
            NoiseSystem.NoiseEmitted -= OnNoiseHeard;
            AlarmSystem.AlarmRaised -= OnAlarm;
            PlayerRespawn.PlayerCaught -= OnPlayerCaught;
            PlayerRespawn.PlayerRespawned -= ResetToStart;
        }

        private void Start()
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null)
            {
                player = go.transform;
                playerRespawn = go.GetComponent<PlayerRespawn>();
            }

            agent.enabled = true;
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                agent.Warp(hit.position);
            else
                Debug.LogWarning($"[EnemyPatrol] {name} no está sobre el NavMesh. ¿Existe un RuntimeNavMeshBaker en la escena?", this);

            ResumePatrol(0);
        }

        private void Update()
        {
            if (frozen || !agent.isOnNavMesh) return;

            // ---------- Transiciones por percepción ----------
            if (CurrentState != EnemyState.Chase && vision.Awareness >= 1f)
            {
                EnterChase();
            }
            else if (CurrentState != EnemyState.Chase && vision.CanSeePlayer && vision.Awareness >= suspicionThreshold)
            {
                // Al investigar, sigue actualizando el destino mientras te vea (5 veces/s).
                repathTimer -= Time.deltaTime;
                if (CurrentState != EnemyState.Investigate || repathTimer <= 0f)
                {
                    repathTimer = 0.2f;
                    Investigate(vision.LastSeenPosition);
                }
            }

            // ---------- Lógica de cada estado ----------
            switch (CurrentState)
            {
                case EnemyState.Patrol:      UpdatePatrol(); break;
                case EnemyState.Investigate: if (HasArrived()) StartSearch(); break;
                case EnemyState.Search:      UpdateSearch(); break;
                case EnemyState.Chase:       UpdateChase(); break;
            }
        }

        // =============== PATRULLA ===============
        private void UpdatePatrol()
        {
            if (route == null || route.Count == 0 || !HasArrived()) return;

            // Esperar en el waypoint (y mirar hacia donde indique)
            waitTimer += Time.deltaTime;
            if (route.TryGetLookRotation(waypointIndex, out Quaternion look))
                transform.rotation = Quaternion.RotateTowards(transform.rotation, look, 180f * Time.deltaTime);

            if (waitTimer >= route.GetWaitTime(waypointIndex))
            {
                waitTimer = 0f;
                waypointIndex = route.GetNextIndex(waypointIndex, ref routeDirection);
                agent.SetDestination(route.GetPosition(waypointIndex));
            }
        }

        private void ResumePatrol(int index)
        {
            CurrentState = EnemyState.Patrol;
            agent.speed = patrolSpeed;
            waitTimer = 0f;
            if (route == null || route.Count == 0) return;
            waypointIndex = Mathf.Clamp(index, 0, route.Count - 1);
            agent.SetDestination(route.GetPosition(waypointIndex));
        }

        // =============== INVESTIGAR / BUSCAR ===============
        private void Investigate(Vector3 point)
        {
            if (CurrentState != EnemyState.Investigate) Play(suspiciousClip);
            CurrentState = EnemyState.Investigate;
            agent.speed = investigateSpeed;
            if (NavMesh.SamplePosition(point, out NavMeshHit hit, 3f, NavMesh.AllAreas)) point = hit.position;
            agent.SetDestination(point);
        }

        private void StartSearch()
        {
            CurrentState = EnemyState.Search;
            stateTimer = searchDuration;
            agent.ResetPath();
        }

        private void UpdateSearch()
        {
            // Gira sobre sí mismo "mirando alrededor" (cambia de sentido a mitad).
            float dir = stateTimer > searchDuration * 0.5f ? 1f : -1f;
            transform.Rotate(Vector3.up, dir * searchTurnSpeed * Time.deltaTime);

            stateTimer -= Time.deltaTime;
            if (stateTimer <= 0f)
                ResumePatrol(route != null ? route.GetClosestIndex(transform.position) : 0);
        }

        // =============== PERSECUCIÓN ===============
        private void EnterChase()
        {
            CurrentState = EnemyState.Chase;
            agent.speed = chaseSpeed;
            lostSightTimer = 0f;
            repathTimer = 0f;
            Play(alertClip);
        }

        private void UpdateChase()
        {
            if (player == null) { ResumePatrol(0); return; }

            if (vision.CanSeePlayer) lostSightTimer = 0f;
            else lostSightTimer += Time.deltaTime;

            // Recalcular la ruta 5 veces por segundo es suficiente y más barato que cada frame.
            repathTimer -= Time.deltaTime;
            if (repathTimer <= 0f)
            {
                repathTimer = 0.2f;
                agent.SetDestination(vision.CanSeePlayer ? player.position : vision.LastSeenPosition);
            }

            if (Vector3.Distance(transform.position, player.position) <= catchDistance)
            {
                if (playerRespawn != null) playerRespawn.Caught();
                return;
            }

            if (lostSightTimer >= loseSightTime)
                Investigate(vision.LastSeenPosition);
        }

        // =============== OÍDO ===============
        private void OnNoiseHeard(Vector3 position, float radius, GameObject source)
        {
            if (frozen || source == gameObject || CurrentState == EnemyState.Chase || !agent.isOnNavMesh) return;
            if (Vector3.Distance(transform.position, position) <= radius * hearingMultiplier)
                Investigate(position);
        }

        // =============== ALARMA (cámaras de seguridad) ===============
        [Header("Alarma")]
        [Tooltip("Velocidad al acudir a una alarma (entre investigar y perseguir).")]
        [SerializeField] private float alarmSpeed = 3.6f;

        private void OnAlarm(Vector3 position, float radius)
        {
            if (frozen || CurrentState == EnemyState.Chase || !agent.isOnNavMesh) return;
            if (Vector3.Distance(transform.position, position) > radius) return;
            Investigate(position);
            agent.speed = alarmSpeed; // acude corriendo
        }

        // =============== CAPTURA / REINICIO ===============
        private void OnPlayerCaught()
        {
            frozen = true;
            if (agent.isOnNavMesh) agent.isStopped = true;
        }

        /// <summary>Vuelve a la posición inicial y reanuda la patrulla (tras reaparecer el jugador).</summary>
        public void ResetToStart()
        {
            frozen = false;
            vision.ResetAwareness();
            routeDirection = 1;
            if (!agent.enabled) return;
            agent.Warp(startPosition);
            transform.rotation = startRotation;
            agent.isStopped = false;
            ResumePatrol(0);
        }

        // =============== UTILIDADES ===============
        private bool HasArrived() =>
            !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.15f;

        private void Play(AudioClip clip)
        {
            if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
        }
    }
}
