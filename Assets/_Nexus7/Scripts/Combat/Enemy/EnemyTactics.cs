using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Nexus7.Enemies;

namespace Nexus7.Combat
{
    /// <summary>
    /// Combate táctico de un robot tirador (TacticsModel + CoverScoring). Mientras persigue y ve al jugador:
    ///   · busca una cobertura que lo tape del jugador (CoverField), corre hasta ella sin disparar y espera;
    ///   · con turno de ataque libre se ASOMA (se aparta 0,9 m por el lado con tiro) y dispara una ráfaga; luego vuelve;
    ///   · según su tipo: seguridad aguanta, guardia avanza cada 5 s, élite flanquea cada 4–6 s o si no tiene tiro;
    ///   · sin coberturas útiles pelea al descubierto a 8–12 m.
    /// Mira siempre al jugador mientras se mueve (no pierde la visión al recolocarse).
    /// Si lo pierde de vista más de 1,5 s, suelta la cobertura y EnemyPatrol vuelve a su búsqueda normal.
    ///
    /// Colocar en: la raíz de un robot con EnemyGun (lo añade CombatInstaller).
    /// </summary>
    [RequireComponent(typeof(EnemyGun))]
    public class EnemyTactics : MonoBehaviour
    {
        public static readonly List<EnemyTactics> All = new List<EnemyTactics>();
        /// <summary>Multiplicador de los tiempos tácticos (el director: &lt; 1 = avanzan y flanquean antes).</summary>
        public static float DirectorTempo { get; set; } = 1f;

        private const float ChestHeight = 1.1f, PeekSide = 0.9f, ArriveDistance = 0.7f, LoseSightGrace = 1.5f;
        private const float OpenMin = 8f, OpenMax = 12f;
        private static readonly RaycastHit[] hits = new RaycastHit[12];
        private static readonly List<bool> protects = new List<bool>(), reserved = new List<bool>();
        private static int nextId;

        public TacticsModel Model { get; private set; }
        public bool Engaged { get; private set; }

        private EnemyGun gun;
        private EnemyPatrol patrol;
        private EnemyVision vision;
        private NavMeshAgent agent;
        private Transform player;
        private System.Random rng;
        private int id, burstsSeen, spotIndex = -1;
        private Vector3 coverPos, goal;
        private bool hasGoal;
        private float lastSeen = -99f, nextProtectCheck;
        private bool coverProtects = true;

        private void Awake()
        {
            id = ++nextId;
            rng = new System.Random(id * 7919);
            gun = GetComponent<EnemyGun>();
            patrol = GetComponent<EnemyPatrol>();
            vision = GetComponent<EnemyVision>();
            agent = GetComponent<NavMeshAgent>();
            Model = new TacticsModel(TacticsCatalog.StyleFor(name));
        }

        private void OnEnable()
        {
            All.Add(this);
            if (gun != null) gun.FireGate = () => !Engaged || Model.MayFire;
            if (patrol != null) patrol.ChaseDestination = Destination;
        }

        private void OnDisable()
        {
            All.Remove(this);
            Disengage();
            if (gun != null) gun.FireGate = null;
            if (patrol != null) patrol.ChaseDestination = null;
        }

        private bool Destination(out Vector3 destination)
        {
            destination = goal;
            return Engaged && hasGoal;
        }

        private void Update()
        {
            if (player == null)
            {
                var go = GameObject.FindGameObjectWithTag("Player");
                if (go == null) return;
                player = go.transform;
            }
            float now = Time.time;
            bool chasing = patrol != null && patrol.enabled && patrol.CurrentState == EnemyPatrol.EnemyState.Chase && !EnemyPatrol.Paused;
            if (chasing && vision != null && vision.CanSeePlayer) lastSeen = now;
            bool shouldEngage = chasing && now - lastSeen <= LoseSightGrace;

            if (!shouldEngage) { if (Engaged) Disengage(); return; }
            if (!Engaged) Engage(now);

            FacePlayer();
            if (spotIndex >= 0 && now >= nextProtectCheck)
            {
                nextProtectCheck = now + 0.25f;
                coverProtects = Protects(CoverField.Spots[spotIndex]);
            }

            int bursts = gun.BurstsCompleted;
            var input = new TacticInput
            {
                Arrived = hasGoal && Flat(transform.position - goal) <= ArriveDistance,
                CoverProtects = coverProtects,
                BurstActive = gun.IsWarningOrFiring,
                BurstFinished = bursts != burstsSeen,
                TokenAvailable = gun.TokenAvailable,
                Tempo = DirectorTempo,
                Roll = rng.NextDouble(),
            };
            burstsSeen = bursts;

            switch (Model.Tick(now, input))
            {
                case TacticOrder.SeekCover: SeekCover(now, Model.PendingIntent); break;
                case TacticOrder.GoToPeek: SetGoal(PeekPosition()); break;
                case TacticOrder.BackToCover: SetGoal(coverPos); break;
            }
            if (Model.Phase == TacticPhase.Open) SetGoal(OpenPosition());
        }

        private void Engage(float now)
        {
            Engaged = true;
            burstsSeen = gun.BurstsCompleted;
            Model.Engage(now);
            if (agent != null) agent.updateRotation = false;
        }

        private void Disengage()
        {
            Engaged = false;
            hasGoal = false;
            spotIndex = -1;
            coverProtects = true;
            CoverField.Reservations.ReleaseAll(id);
            if (agent != null) agent.updateRotation = true;
        }

        private void SeekCover(float now, CoverIntent intent)
        {
            var spots = CoverField.Spots;
            protects.Clear();
            reserved.Clear();
            Vector3 p = player.position, r = transform.position;
            for (int i = 0; i < spots.Count; i++)
            {
                bool inRange = CoverScoring.Dist(r.x, r.z, spots[i].X, spots[i].Z) <= CoverScoring.SearchRadius;
                protects.Add(inRange && Protects(spots[i]));   // solo se lanzan rayos a los cercanos
                reserved.Add(CoverField.Reservations.IsReservedByOther(spots[i].Id, id));
            }
            int exclude = spotIndex >= 0 ? spots[spotIndex].Id : -1;
            int best = CoverScoring.Best(r.x, r.z, p.x, p.z, spots, protects, reserved, intent, exclude);
            if (best < 0 || !CoverField.Reservations.TryReserve(spots[best].Id, id))
            {
                CoverField.Reservations.ReleaseAll(id);
                spotIndex = -1;
                Model.NoCoverFound(now, DirectorTempo);
                return;
            }
            spotIndex = best;
            coverProtects = true;
            coverPos = CoverField.Position(spots[best]);
            SetGoal(coverPos);
            Model.CoverAssigned(now);
        }

        /// <summary>¿El objeto tapa el punto desde el pecho del jugador? (ignora robots y al propio jugador)</summary>
        private bool Protects(CoverSpot s)
        {
            Vector3 from = player.position + Vector3.up * ChestHeight;
            Vector3 to = CoverField.Position(s) + Vector3.up * (s.Low ? 0.7f : ChestHeight);
            return Blocked(from, to);
        }

        private bool Blocked(Vector3 from, Vector3 to)
        {
            Vector3 d = to - from;
            float dist = d.magnitude;
            if (dist < 0.1f) return false;
            int n = Physics.RaycastNonAlloc(from, d / dist, hits, dist - 0.3f, ~(1 << 2), QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var c = hits[i].collider;
                if (c == null || c.transform.IsChildOf(player) || c.GetComponentInParent<EnemyPatrol>() != null) continue;
                return true;
            }
            return false;
        }

        /// <summary>Se aparta de la cobertura por el lado desde el que ve al jugador (o el más cercano a él).</summary>
        private Vector3 PeekPosition()
        {
            if (spotIndex < 0) return transform.position;
            var s = CoverField.Spots[spotIndex];
            Vector3 tangent = new Vector3(-s.NormalZ, 0f, s.NormalX);
            Vector3 a = coverPos + tangent * PeekSide, b = coverPos - tangent * PeekSide;
            Vector3 chest = player.position + Vector3.up * ChestHeight, up = Vector3.up * ChestHeight;
            bool aClear = !Blocked(chest, a + up), bClear = !Blocked(chest, b + up);
            Vector3 pick = aClear && !bClear ? a : bClear && !aClear ? b
                         : (a - player.position).sqrMagnitude <= (b - player.position).sqrMagnitude ? a : b;
            return NavMesh.SamplePosition(pick, out NavMeshHit hit, 0.8f, NavMesh.AllAreas) ? hit.position : coverPos;
        }

        /// <summary>Sin cobertura: se coloca a 8–12 m del jugador en la línea que los une.</summary>
        private Vector3 OpenPosition()
        {
            Vector3 away = transform.position - player.position;
            away.y = 0f;
            float d = away.magnitude;
            if (d >= OpenMin && d <= OpenMax) return transform.position;
            if (d < 0.01f) away = -transform.forward;
            Vector3 p = player.position + away.normalized * (d < OpenMin ? OpenMin + 1f : OpenMax - 1f);
            return NavMesh.SamplePosition(p, out NavMeshHit hit, 2f, NavMesh.AllAreas) ? hit.position : transform.position;
        }

        private void SetGoal(Vector3 p)
        {
            goal = p;
            hasGoal = true;
        }

        private void FacePlayer()
        {
            Vector3 flat = (vision != null ? vision.LastSeenPosition : player.position) - transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude < 0.01f) return;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(flat), 300f * Time.deltaTime);
        }

        private static float Flat(Vector3 v) { v.y = 0f; return v.magnitude; }

        /// <summary>Resumen para el panel F3: cuántos robots hay en cada fase.</summary>
        public static string Summary()
        {
            int open = 0, moving = 0, cover = 0, peek = 0;
            foreach (var t in All)
            {
                if (t == null || !t.Engaged) continue;
                switch (t.Model.Phase)
                {
                    case TacticPhase.Open: open++; break;
                    case TacticPhase.Moving: moving++; break;
                    case TacticPhase.InCover: cover++; break;
                    case TacticPhase.Peeking: peek++; break;
                }
            }
            return $"Tácticas: cubiertos {cover} · asomados {peek} · moviéndose {moving} · al descubierto {open} · coberturas {CoverField.Spots.Count}";
        }
    }
}
