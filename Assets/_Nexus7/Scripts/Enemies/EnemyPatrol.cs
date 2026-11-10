using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Nexus7.Core;
using Nexus7.Gameplay;
using Nexus7.UI;

namespace Nexus7.Enemies
{
    /// <summary>
    /// Dron con máquina de estados:
    ///
    ///   PATROL ──(ve algo / oye ruido / alarma)──► INVESTIGATE ──(llega)──► SEARCH ──(tiempo)──► PATROL
    ///     │  └─ en waypoints con "Inspect Target": escanea el banco; si se usó hace poco → ALARMA
    ///     └──────────(alerta = 1 y lo ve)──────────► CHASE ── lo alcanza → captura
    ///                                                  └─ lo pierde → va SOLO a la última posición vista ──► SEARCH
    ///   En SEARCH avanza unos metros hacia donde huía el jugador mirando alrededor y, si no lo encuentra, vuelve a patrullar.
    ///   Cualquier estado ──(Pulso PEM)──► STUNNED ──(fin)──► SEARCH con detección reforzada
    ///
    /// Inteligencia ante distractores (Fase 4):
    ///   · Oye un SEÑUELO → va a inspeccionarlo y lo neutraliza. Queda "receloso" (detecta más rápido) un tiempo.
    ///   · Si recibe OTRO señuelo antes de 'Decoy Memory' segundos → deduce el engaño y va a buscar
    ///     al punto desde donde se lanzó.
    ///   · Tras un PEM se reinicia "con la mosca detrás de la oreja": búsqueda larga y detección reforzada.
    ///
    /// Colocar en: la raíz del enemigo, junto a NavMeshAgent (DESACTIVADO en la escena) y EnemyVision.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(EnemyVision))]
    public class EnemyPatrol : MonoBehaviour, IEmpTarget
    {
        public enum EnemyState { Patrol, Investigate, Search, Chase, Stunned }

        /// <summary>Todos los enemigos activos (lo usa el indicador de sigilo del HUD).</summary>
        public static readonly List<EnemyPatrol> All = new List<EnemyPatrol>();
        /// <summary>true durante las cinemáticas: los drones se quedan quietos y no detectan.</summary>
        public static bool Paused;
        private bool pausedHere;
        /// <summary>El dron puede volver a moverse (no está congelado, aturdido ni en pausa por una cinemática).</summary>
        public bool CanMove => !frozen && !Paused && CurrentState != EnemyState.Stunned;

        /// <summary>Se dispara cuando un dron empieza a perseguir al jugador (alertas, escuadrones).</summary>
        public static event System.Action<EnemyPatrol> PlayerSpotted;

        [SerializeField] private PatrolRoute route;
        [Tooltip("Waypoint por el que empieza (útil para desfasar varios drones en la misma ruta).")]
        [SerializeField, Min(0)] private int startWaypoint = 0;

        [Header("Velocidades")]
        [SerializeField] private float patrolSpeed = 1.8f;
        [SerializeField] private float investigateSpeed = 2.6f;
        [Tooltip("Menor que la carrera del jugador (6): se le puede dejar atrás corriendo... haciendo ruido.")]
        [SerializeField] private float chaseSpeed = 4.4f;
        [Tooltip("Velocidad al acudir a una alarma o a un señuelo deducido.")]
        [SerializeField] private float alarmSpeed = 3.6f;

        [Header("Comportamiento")]
        [Range(0.05f, 0.95f)]
        [SerializeField] private float suspicionThreshold = 0.35f;
        [Tooltip("Multiplica el radio de los ruidos que oye (1 = normal).")]
        [SerializeField] private float hearingMultiplier = 1f;
        [Tooltip("Segundos que busca en la última posición vista antes de volver a patrullar.")]
        [SerializeField] private float searchDuration = 6f;
        [SerializeField] private float searchTurnSpeed = 90f;
        [Tooltip("Metros que avanza más allá de la última posición, hacia donde huía el jugador.")]
        [SerializeField] private float searchProbeDistance = 3f;
        [Tooltip("Tiempo máximo yendo a la última posición vista (si no llega, empieza a buscar donde esté).")]
        [SerializeField] private float maxPursuitTime = 8f;
        [Tooltip("Margen antes de darlo por perdido (evita cortar la persecución por un parpadeo de 1–2 fotogramas).")]
        [SerializeField] private float lostSightGrace = 0.25f;
        [SerializeField] private float catchDistance = 1.3f;

        [Header("Inteligencia (Fase 4)")]
        [Tooltip("Segundos durante los que recuerda un señuelo: si llega otro antes, busca al lanzador.")]
        [SerializeField] private float decoyMemory = 40f;
        [Tooltip("Tras un señuelo o un PEM, multiplica su velocidad de detección durante un tiempo.")]
        [SerializeField] private float wariness = 1.6f;
        [SerializeField] private float warinessDuration = 25f;
        [Tooltip("Un banco usado hace menos de estos segundos se considera manipulado.")]
        [SerializeField] private float tamperWindow = 45f;
        [SerializeField] private float tamperAlarmRadius = 18f;
        [SerializeField] private string displayName = "Dron de seguridad";
        [Tooltip("Luz de escaneo (se enciende al inspeccionar).")]
        [SerializeField] private Light scanLight;
        [Tooltip("Efecto visual mientras está aturdido por un PEM (chispas, luz...).")]
        [SerializeField] private GameObject stunEffect;

        [Header("Alerta de la nave (Fase 5)")]
        [Tooltip("En ALERTA ROJA caza activamente: acude cada pocos segundos a la última posición conocida del jugador.")]
        [SerializeField] private bool aggressiveOnRed = false;
        [SerializeField] private float huntInterval = 6f;
        [Tooltip("Antigüedad máxima (s) de un avistamiento para ir a cazar.")]
        [SerializeField] private float huntMemory = 45f;

        [Header("Audio (opcional)")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip suspiciousClip;
        [SerializeField] private AudioClip alertClip;

        public EnemyState CurrentState { get; private set; } = EnemyState.Patrol;
        public EnemyVision Vision => vision;
        public Vector3 EmpPosition => transform.position;

        private NavMeshAgent agent;
        private EnemyVision vision;
        private Transform player;
        private PlayerRespawn playerRespawn;
        private EnemyMeleeAttack melee;

        private int waypointIndex;
        private int routeDirection = 1;
        private float waitTimer;
        private float stateTimer;
        private float lostSightTimer;
        private float repathTimer;
        private bool frozen; // durante la secuencia de captura
        private Vector3 startPosition;
        private Quaternion startRotation;

        private IDistraction currentDistraction;
        private IDistraction lastDistraction;
        private float lastDistractionTime = -999f;
        private float stunTimer;
        private float currentSearchDuration;
        private float huntTimer;
        private bool probing;          // en SEARCH: aún va hacia el punto donde huía el jugador
        private bool pursuitPathSet;   // en CHASE sin verlo: ya va hacia la última posición vista
        private float spinDuration;    // en SEARCH: segundos de giro que quedaban al terminar de avanzar

        /// <summary>Velocidad base afectada por el nivel de alerta de la nave.</summary>
        private float Speed(float baseSpeed) => baseSpeed * AlertSystem.Profile.speed;

        private Nexus7.Characters.ProceduralAnimator rig;

        /// <summary>La velocidad de persecución sigue al nivel de alerta aunque cambie a mitad de la persecución.</summary>
        private void ApplyChaseSpeed()
        {
            if (agent != null) agent.speed = Speed(chaseSpeed);
        }

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            vision = GetComponent<EnemyVision>();
            rig = GetComponent<Nexus7.Characters.ProceduralAnimator>();
            startPosition = transform.position;
            startRotation = transform.rotation;
            if (scanLight != null) scanLight.enabled = false;
            if (stunEffect != null) stunEffect.SetActive(false);
        }

        private void OnEnable()
        {
            All.Add(this);
            EmpRegistry.Register(this);
            NoiseSystem.NoiseEmitted += OnNoiseHeard;
            AlarmSystem.AlarmRaised += OnAlarm;
            AlertSystem.LevelChanged += OnAlertLevelChanged;
            PlayerRespawn.PlayerCaught += OnPlayerCaught;
            PlayerRespawn.PlayerRespawned += ResetToStart;
        }

        private void OnDisable()
        {
            All.Remove(this);
            EmpRegistry.Unregister(this);
            NoiseSystem.NoiseEmitted -= OnNoiseHeard;
            AlarmSystem.AlarmRaised -= OnAlarm;
            AlertSystem.LevelChanged -= OnAlertLevelChanged;
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
            melee = GetComponent<EnemyMeleeAttack>();

            agent.enabled = true;
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                agent.Warp(hit.position);
            else
                Debug.LogWarning($"[EnemyPatrol] {name} no está sobre el NavMesh. ¿Existe un RuntimeNavMeshBaker en la escena?", this);

            ResumePatrol(startWaypoint);
        }

        private void Update()
        {
            if (Paused)
            {
                if (!pausedHere && agent.isOnNavMesh) { agent.isStopped = true; pausedHere = true; }
                return;
            }
            if (pausedHere) { pausedHere = false; if (agent.isOnNavMesh && !frozen && CurrentState != EnemyState.Stunned) agent.isStopped = false; }
            if (frozen || !agent.isOnNavMesh) return;

            if (CurrentState == EnemyState.Stunned) { UpdateStunned(); return; }

            // Lo que ve un dron lo sabe toda la nave (para la caza en Alerta Roja).
            if (vision.CanSeePlayer && vision.Awareness >= suspicionThreshold)
                AlertSystem.ReportPlayerPosition(vision.LastSeenPosition);

            // ---------- Alerta Roja: caza activa ----------
            if (aggressiveOnRed && AlertSystem.Level == AlertLevel.Red
                && (CurrentState == EnemyState.Patrol || CurrentState == EnemyState.Search))
            {
                huntTimer -= Time.deltaTime;
                if (huntTimer <= 0f) Hunt();
            }

            // ---------- Transiciones por percepción ----------
            // Solo persigue si lo VE ahora mismo (la alerta alta tras perderlo no basta: no hay persecución "mágica")
            if (CurrentState != EnemyState.Chase && vision.Awareness >= 1f && vision.CanSeePlayer)
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
                    currentDistraction = null; // el jugador tiene prioridad sobre un señuelo
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

        // =============== PATRULLA + INSPECCIÓN ===============
        private void UpdatePatrol()
        {
            if (route == null || route.Count == 0 || !HasArrived()) return;

            var inspect = route.GetInspectTarget(waypointIndex);
            if (waitTimer == 0f && scanLight != null && inspect != null) scanLight.enabled = true;

            waitTimer += Time.deltaTime;
            if (route.TryGetLookRotation(waypointIndex, out Quaternion look))
                transform.rotation = Quaternion.RotateTowards(transform.rotation, look, 180f * Time.deltaTime);

            if (waitTimer < route.GetWaitTime(waypointIndex)) return;

            // Fin de la espera: si era un punto de inspección, comprobar si alguien lo ha manipulado.
            if (scanLight != null) scanLight.enabled = false;
            if (inspect != null && inspect.WasTamperedRecently(tamperWindow))
            {
                inspect.ClearTamper();
                if (HUDManager.Instance != null)
                    HUDManager.Instance.QueueMessage($"{displayName.ToUpperInvariant()}: actividad no autorizada en {inspect.InspectionLabel}. ¡Alerta!", 2.5f);
                Play(alertClip);
                vision.BoostDetection(wariness, warinessDuration);
                AlarmSystem.Raise(inspect.InspectionPosition, tamperAlarmRadius); // también avisa a los demás
                return; // OnAlarm ya lo ha puesto a investigar
            }

            waitTimer = 0f;
            waypointIndex = route.GetNextIndex(waypointIndex, ref routeDirection);
            agent.SetDestination(route.GetPosition(waypointIndex));
        }

        private void ResumePatrol(int index)
        {
            CurrentState = EnemyState.Patrol;
            agent.speed = Speed(patrolSpeed);
            waitTimer = 0f;
            if (scanLight != null) scanLight.enabled = false;
            if (route == null || route.Count == 0) return;
            waypointIndex = Mathf.Clamp(index, 0, route.Count - 1);
            agent.SetDestination(route.GetPosition(waypointIndex));
        }

        // =============== INVESTIGAR / BUSCAR ===============
        private void Investigate(Vector3 point)
        {
            if (CurrentState != EnemyState.Investigate) Play(suspiciousClip);
            CurrentState = EnemyState.Investigate;
            agent.speed = Speed(investigateSpeed);
            if (scanLight != null) scanLight.enabled = false;
            if (NavMesh.SamplePosition(point, out NavMeshHit hit, 3f, NavMesh.AllAreas)) point = hit.position;
            agent.SetDestination(point);
        }

        private void StartSearch()
        {
            CurrentState = EnemyState.Search;
            currentSearchDuration = searchDuration;
            agent.ResetPath();
            probing = false;

            // Llegó a un señuelo: lo neutraliza y queda receloso.
            if (currentDistraction != null)
            {
                if (currentDistraction.IsActive) { currentDistraction.Neutralize(); RunStats.RecordDecoyTriggered(); }
                currentDistraction = null;
                vision.BoostDetection(wariness, warinessDuration);
                currentSearchDuration = searchDuration * 1.5f;
            }
            stateTimer = currentSearchDuration;
            spinDuration = currentSearchDuration;
        }

        /// <summary>
        /// Búsqueda tras perder al jugador: primero avanza 'searchProbeDistance' metros más allá de la última posición vista,
        /// hacia donde huía (PerceptionModel.SearchProbe), y después mira alrededor. Si no lo encuentra, vuelve a patrullar.
        /// </summary>
        private void StartPursuitSearch()
        {
            StartSearch();
            huntTimer = huntInterval;   // en Alerta Roja no sale a "cazar" a su propio avistamiento a mitad de la búsqueda
            Vector3 seen = vision.LastSeenPosition;
            // Si lo vio en el aire o en un borde, parte del punto de NavMesh más cercano
            if (NavMesh.SamplePosition(seen, out NavMeshHit onMesh, 2f, NavMesh.AllAreas)) seen = onMesh.position;
            Vector3 probe = PerceptionModel.SearchProbe(seen, vision.LastSeenVelocity, searchProbeDistance);
            // Si hay una pared entre la última posición y ese punto, se queda en la pared (no lo busca "a través" de ella)
            if (NavMesh.Raycast(seen, probe, out NavMeshHit edge, NavMesh.AllAreas)) probe = edge.position;
            if (NavMesh.SamplePosition(probe, out NavMeshHit hit, 1.5f, NavMesh.AllAreas)
                && (hit.position - transform.position).sqrMagnitude > 0.5f * 0.5f)
            {
                agent.speed = Speed(investigateSpeed);
                agent.SetDestination(hit.position);
                probing = true;
            }
        }

        private void UpdateSearch()
        {
            // Primero llega al punto hacia donde huía (el tiempo de búsqueda ya corre); después mira alrededor
            if (probing)
            {
                stateTimer -= Time.deltaTime;
                if (HasArrived() || stateTimer <= currentSearchDuration * 0.5f) { probing = false; agent.ResetPath(); spinDuration = stateTimer; }
                else return;
            }
            // Gira sobre sí mismo "mirando alrededor" (cambia de sentido a mitad del giro).
            float dir = stateTimer > spinDuration * 0.5f ? 1f : -1f;
            transform.Rotate(Vector3.up, dir * searchTurnSpeed * Time.deltaTime);

            stateTimer -= Time.deltaTime;
            if (stateTimer <= 0f)
                ResumePatrol(route != null ? route.GetClosestIndex(transform.position) : 0);
        }

        // =============== PERSECUCIÓN ===============
        private void EnterChase()
        {
            CurrentState = EnemyState.Chase;
            currentDistraction = null;
            agent.speed = Speed(chaseSpeed);
            lostSightTimer = 0f;
            repathTimer = 0f;
            pursuitPathSet = false;
            if (scanLight != null) scanLight.enabled = false;
            Play(alertClip);
            PlayerSpotted?.Invoke(this);
        }

        private void UpdateChase()
        {
            if (player == null) { ResumePatrol(0); return; }
            ApplyChaseSpeed();

            if (vision.CanSeePlayer)
            {
                // Lo ve: lo sigue (5 veces por segundo)
                lostSightTimer = 0f;
                pursuitPathSet = false;
                repathTimer -= Time.deltaTime;
                if (repathTimer <= 0f) { repathTimer = 0.2f; agent.SetDestination(player.position); }
            }
            else
            {
                // Lo ha perdido: va SOLO a la última posición donde lo vio (no a donde está ahora)
                lostSightTimer += Time.deltaTime;
                if (lostSightTimer < lostSightGrace) return;   // parpadeo breve: sigue hacia donde iba, sin atacar
                if (!pursuitPathSet)
                {
                    pursuitPathSet = true;
                    Vector3 target = vision.LastSeenPosition;
                    if (NavMesh.SamplePosition(target, out NavMeshHit hit, 3f, NavMesh.AllAreas)) target = hit.position;
                    agent.SetDestination(target);
                }
                if (PerceptionModel.ShouldEndPursuit(false, HasArrived(), lostSightTimer, maxPursuitTime))
                {
                    StartPursuitSearch();
                    return;
                }
            }

            if (melee != null)
            {
                // Robot armado: golpea al llegar al alcance del arma (se puede esquivar apartándose).
                // Solo ataca si lo VE (fuera del cono está ciego, también para golpear)
                if (melee.IsAttacking) return;
                if (vision.CanSeePlayer && Vector3.Distance(transform.position, player.position) <= melee.Range * 0.9f && melee.HasClearLine()) { melee.TryAttack(); return; }
            }
            else if (vision.CanSeePlayer && Vector3.Distance(transform.position, player.position) <= catchDistance)
            {
                if (playerRespawn != null) playerRespawn.Caught();
            }
        }

        // =============== OÍDO Y SEÑUELOS ===============
        private void OnNoiseHeard(Vector3 position, float radius, GameObject source)
        {
            if (frozen || source == gameObject || CurrentState == EnemyState.Chase
                || CurrentState == EnemyState.Stunned || !agent.isOnNavMesh) return;
            if (Vector3.Distance(transform.position, position) > radius * hearingMultiplier * AlertSystem.Profile.hearing) return;

            var distraction = source != null ? source.GetComponent<IDistraction>() : null;
            if (distraction != null) { OnDistraction(distraction); return; }

            currentDistraction = null;
            Investigate(position);
        }

        private void OnDistraction(IDistraction distraction)
        {
            if (frozen || CurrentState == EnemyState.Stunned || CurrentState == EnemyState.Chase) return;
            if (!distraction.IsActive || distraction == currentDistraction) return; // ya va hacia él

            bool recentlyFooled = lastDistraction != null && lastDistraction != distraction
                                  && Time.time - lastDistractionTime < decoyMemory;
            lastDistraction = distraction;
            lastDistractionTime = Time.time;

            if (recentlyFooled)
            {
                // "Ya me engañaron hace poco": ignora el señuelo y busca a quien lo lanzó.
                currentDistraction = null;
                vision.BoostDetection(wariness, warinessDuration);
                if (HUDManager.Instance != null)
                    HUDManager.Instance.QueueMessage($"IA NEXUS-7: El {displayName.ToLowerInvariant()} ha deducido el engaño. Busca el origen del señuelo.", 2.5f);
                Investigate(distraction.Origin);
                agent.speed = Speed(alarmSpeed);
                return;
            }

            currentDistraction = distraction;
            Investigate(distraction.Position);
        }

        // =============== ALARMA ===============
        private void OnAlarm(Vector3 position, float radius)
        {
            if (frozen || CurrentState == EnemyState.Chase || CurrentState == EnemyState.Stunned || !agent.isOnNavMesh) return;
            if (Vector3.Distance(transform.position, position) > radius) return;
            currentDistraction = null;
            Investigate(position);
            agent.speed = Speed(alarmSpeed); // acude corriendo
        }

        // =============== ALERTA DE LA NAVE Y ESCUADRÓN ===============
        private void OnAlertLevelChanged(AlertLevel from, AlertLevel to)
        {
            if (to == AlertLevel.Red && aggressiveOnRed) Hunt();
        }

        /// <summary>Caza: va corriendo a la última posición conocida del jugador.</summary>
        private void Hunt()
        {
            huntTimer = huntInterval;
            if (frozen || !agent.isOnNavMesh || CurrentState == EnemyState.Chase || CurrentState == EnemyState.Stunned) return;
            if (!AlertSystem.HasRecentSighting(huntMemory)) return;
            // Buscando donde él mismo lo vio: no vuelve atrás a ese mismo punto
            if (CurrentState == EnemyState.Search
                && (AlertSystem.LastKnownPlayerPosition - vision.LastSeenPosition).sqrMagnitude < 2f * 2f) return;
            currentDistraction = null;
            Investigate(AlertSystem.LastKnownPlayerPosition);
            agent.speed = Speed(alarmSpeed);
        }

        /// <summary>Un compañero de escuadrón ha visto al jugador: acude a esa posición.</summary>
        public void RespondToSquad(Vector3 position)
        {
            if (frozen || !agent.isOnNavMesh || CurrentState == EnemyState.Chase || CurrentState == EnemyState.Stunned) return;
            currentDistraction = null;
            Investigate(position);
            agent.speed = Speed(alarmSpeed);
        }

        // =============== PULSO PEM ===============
        /// <summary>Aturde al dron: se detiene, se queda ciego y no oye nada durante 'duration' segundos.</summary>
        public void OnEmp(float duration) => Stun(duration);

        public void Stun(float duration)
        {
            if (frozen) return;
            if (melee != null) melee.Cancel();
            CurrentState = EnemyState.Stunned;
            stunTimer = Mathf.Max(stunTimer, duration);
            currentDistraction = null;
            if (agent.isOnNavMesh) { agent.isStopped = true; agent.ResetPath(); }
            vision.SetBlinded(true);
            if (scanLight != null) scanLight.enabled = false;
            if (stunEffect != null) stunEffect.SetActive(true);
            if (rig != null) rig.SetStunned(true);
        }

        private void UpdateStunned()
        {
            stunTimer -= Time.deltaTime;
            if (stunTimer > 0f) return;

            // Reinicio: búsqueda larga y receloso.
            if (stunEffect != null) stunEffect.SetActive(false);
            if (rig != null) rig.SetStunned(false);
            vision.SetBlinded(false);
            vision.BoostDetection(wariness, warinessDuration);
            agent.isStopped = false;
            CurrentState = EnemyState.Search;
            probing = false;
            currentSearchDuration = searchDuration * 2f;
            stateTimer = currentSearchDuration;
            spinDuration = currentSearchDuration;
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
            stunTimer = 0f;
            huntTimer = huntInterval;                 // en Alerta Roja no salen a cazar nada más reaparecer
            AlertSystem.ForgetPlayerPosition();       // ni van al sitio donde te dejaron fuera de combate
            if (melee != null) melee.Cancel();        // ningún golpe a medias tras el reinicio
            currentDistraction = null;
            lastDistraction = null;
            if (stunEffect != null) stunEffect.SetActive(false);
            if (rig != null) rig.SetStunned(false);
            vision.SetBlinded(false);
            vision.ResetAwareness();
            routeDirection = 1;
            if (!agent.enabled) return;
            agent.Warp(startPosition);
            transform.rotation = startRotation;
            agent.isStopped = false;
            ResumePatrol(startWaypoint);
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
