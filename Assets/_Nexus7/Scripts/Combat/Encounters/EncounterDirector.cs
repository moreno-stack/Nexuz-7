using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Nexus7.Core;
using Nexus7.Director;
using Nexus7.Enemies;
using Nexus7.Gameplay;
using Nexus7.UI;

namespace Nexus7.Combat
{
    /// <summary>
    /// Encuentro por oleadas de la fase (WaveModel + EncounterCatalog + EncounterSpawnPicker):
    ///   · Se activa una vez al cumplir un objetivo (hackeo, terminal de acceso, consola de energía o de mando, paso de
    ///     lanzamiento). En fases sin objetivos, con el primer punto seguro; sin ninguno de los dos, al alejarse 20 m.
    ///   · Cuenta atrás de 3 s y aparece la oleada (copias de los robots de la fase) en puntos a 8–35 m, fuera de la vista
    ///     si se puede y distintos del intento anterior. Los robots llegan avisados y van a por el jugador.
    ///   · Siguiente oleada con 1 o 0 robots vivos, o a los 25 s. Al despejar la última: «Zona despejada» y munición.
    ///   · Si el jugador cae a mitad, los robots de las oleadas se retiran y el encuentro vuelve a empezar 4 s después
    ///     de reaparecer, con otros puntos de aparición. La dificultad y la tensión del director deciden cuántos vienen.
    /// Lo crea CombatInstaller en cada fase (no hace falta reconstruir escenas).
    /// </summary>
    public class EncounterDirector : MonoBehaviour
    {
        public static EncounterDirector Instance { get; private set; }

        private const float RestartDelay = 4f;
        private const float SpawnJitter = 1.2f;

        private WaveModel model;
        private RobotKind[][] catalog;
        private List<List<RobotKind>> waves;
        private readonly Dictionary<RobotKind, GameObject> templates = new Dictionary<RobotKind, GameObject>();
        private readonly List<EnemyHealth> currentWave = new List<EnemyHealth>();
        private readonly List<GameObject> spawned = new List<GameObject>();
        private readonly List<float> candX = new List<float>(), candZ = new List<float>();
        private readonly List<Vector3> candPos = new List<Vector3>();
        private readonly HashSet<int> lastUsed = new HashSet<int>(), usedThisAttempt = new HashSet<int>();
        private readonly List<PatrolRoute> routes = new List<PatrolRoute>();
        private Transform player;
        private Vector3 startPosition;
        private bool hasObjectives, hasCheckpoints, restartPending;
        private float restartAt = -1f;
        private int attempt, spawnCount;
        private string lastEvent = "—";

        public static void EnsureForScene()
        {
            if (Instance != null) return;
            var scene = SceneManager.GetActiveScene().name;
            if (!GameFlow.IsPhaseScene(scene) || EncounterCatalog.ForScene(scene).Length == 0) return;
            new GameObject("DirectorEncuentros").AddComponent<EncounterDirector>();
        }

        private void Awake() => Instance = this;

        private void OnEnable()
        {
            ObjectiveEvents.Completed += OnObjective;
            PlayerRespawn.PlayerCaught += OnCaught;
            PlayerRespawn.PlayerRespawned += OnRespawned;
        }

        private void OnDisable()
        {
            ObjectiveEvents.Completed -= OnObjective;
            PlayerRespawn.PlayerCaught -= OnCaught;
            PlayerRespawn.PlayerRespawned -= OnRespawned;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            catalog = EncounterCatalog.ForScene(SceneManager.GetActiveScene().name);
            model = new WaveModel(catalog.Length);
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) { player = go.transform; startPosition = player.position; }
            hasObjectives = FindObjectsByType<Nexus7.Security.HackTerminal>(FindObjectsSortMode.None).Length > 0
                         || FindObjectsByType<Nexus7.Interaction.AccessTerminal>(FindObjectsSortMode.None).Length > 0
                         || FindObjectsByType<Nexus7.Power.PowerConsole>(FindObjectsSortMode.None).Length > 0
                         || FindObjectsByType<Nexus7.Security.ControlConsole>(FindObjectsSortMode.None).Length > 0
                         || FindObjectsByType<LaunchStepTerminal>(FindObjectsSortMode.None).Length > 0;
            hasCheckpoints = FindObjectsByType<Checkpoint>(FindObjectsSortMode.None).Length > 0;
            BuildTemplates();
        }

        // ------------------------- Activación -------------------------
        private void OnObjective(string kind, Vector3 position)
        {
            if (!EncounterTriggerRule.ShouldActivate(kind, hasObjectives)) return;
            Activate("objetivo «" + kind + "»");
        }

        private void Activate(string reason)
        {
            if (model == null || templates.Count == 0 || player == null) return;
            if (!model.Activate(Time.time)) return;
            float tension = GameDirector.Instance != null ? GameDirector.Instance.Model.Tension : 0.5f;
            waves = EncounterCatalog.Scale(catalog, (int)DifficultySettings.Current, tension);
            BuildCandidates();
            lastEvent = "Activado por " + reason;
            Say($"IA NEXUS-7: intrusión confirmada. Unidades de respuesta en camino ({waves.Count} oleada{(waves.Count == 1 ? "" : "s")}).");
            CombatSfx.Play(CombatSound.EnemyWarn, 0.9f);
        }

        private void Update()
        {
            if (model == null || player == null || EnemyPatrol.Paused) return;

            if (restartPending && restartAt > 0f && Time.time >= restartAt)
            {
                restartPending = false;
                restartAt = -1f;
                Activate("reinicio tras caer");
            }
            if (model.State == EncounterState.Idle && !restartPending && !hasObjectives && !hasCheckpoints
                && Vector3.Distance(player.position, startPosition) >= EncounterTriggerRule.WanderDistance)
                Activate("exploración");

            int alive = 0;
            foreach (var e in currentWave) if (e != null && e.IsAlive) alive++;
            switch (model.Tick(Time.time, alive))
            {
                case EncounterEvent.SpawnWave: SpawnWave(model.CurrentWave); break;
                case EncounterEvent.Announce:
                    Say($"¡REFUERZOS! Oleada {model.CurrentWave + 2}/{waves.Count} en {WaveModel.CountdownTime:0} s.");
                    CombatSfx.Play(CombatSound.EnemyWarn, 0.9f);
                    break;
                case EncounterEvent.Cleared:
                    lastEvent = "Zona despejada";
                    Say("Zona despejada. Los refuerzos han caído: munición de recompensa disponible.");
                    AmmoPickup.Spawn(player.position + player.forward * 1.5f + Vector3.up * 0.4f, 2f);
                    break;
            }
        }

        // ------------------------- Oleadas -------------------------
        private void SpawnWave(int index)
        {
            currentWave.Clear();
            if (waves == null || index < 0 || index >= waves.Count) return;
            var list = waves[index];
            var picks = EncounterSpawnPicker.Pick(candX, candZ, player.position.x, player.position.z,
                                                  player.forward.x, player.forward.z, lastUsed, list.Count,
                                                  new System.Random(Seed(index)));
            for (int i = 0; i < list.Count && i < picks.Count; i++)
            {
                usedThisAttempt.Add(picks[i]);
                Vector3 pos = candPos[picks[i]];
                if (CountBefore(picks, i) > 0)   // punto repetido: separarlos un poco
                    pos += new Vector3(Mathf.Sin(i * 2.1f), 0f, Mathf.Cos(i * 2.1f)) * SpawnJitter;
                var health = Spawn(list[i], pos);
                if (health != null) currentWave.Add(health);
            }
            lastEvent = $"Oleada {index + 1}/{waves.Count}: {currentWave.Count} robots";
            Say($"¡ALERTA! Oleada {index + 1}/{waves.Count}: {currentWave.Count} unidades.");
        }

        private static int CountBefore(List<int> picks, int i)
        {
            int n = 0;
            for (int j = 0; j < i; j++) if (picks[j] == picks[i]) n++;
            return n;
        }

        private EnemyHealth Spawn(RobotKind kind, Vector3 pos)
        {
            if (!templates.TryGetValue(kind, out var template) || template == null) return null;
            if (UnityEngine.AI.NavMesh.SamplePosition(pos, out UnityEngine.AI.NavMeshHit hit, 2f, UnityEngine.AI.NavMesh.AllAreas)) pos = hit.position;
            Vector3 look = player.position - pos;
            look.y = 0f;
            var rot = look.sqrMagnitude > 0.01f ? Quaternion.LookRotation(look) : Quaternion.identity;
            var go = Instantiate(template, pos, rot);
            go.name = template.name + "_Oleada" + (++spawnCount);
            var patrol = go.GetComponent<EnemyPatrol>();
            if (patrol != null)
            {
                var route = NearestRoute(pos, out int wp);
                if (route != null) patrol.AssignRoute(route, wp, 1);
            }
            var health = go.GetComponent<EnemyHealth>();
            if (health != null) health.IsReinforcement = true;
            spawned.Add(go);
            Nexus7.Devices.PulseRing.Spawn(pos, 2f, new Color(1f, 0.3f, 0.25f), 0.8f);
            CombatSfx.PlayAt(CombatSound.EnemyWarn, pos);
            StartCoroutine(SendToPlayer(patrol));
            return health;
        }

        /// <summary>Un fotograma después (cuando ya ha hecho su Start) va a donde está el jugador, en alerta.</summary>
        private IEnumerator SendToPlayer(EnemyPatrol patrol)
        {
            yield return null;
            if (patrol == null || player == null) yield break;
            patrol.RespondToSquad(player.position);
            if (patrol.Vision != null) patrol.Vision.BoostDetection(2f, 12f);
        }

        // ------------------------- Al caer -------------------------
        private void OnCaught()
        {
            if (model == null || model.State == EncounterState.Idle || model.State == EncounterState.Cleared) return;
            foreach (var go in spawned) if (go != null) Destroy(go);
            spawned.Clear();
            currentWave.Clear();
            lastUsed.Clear();
            foreach (var i in usedThisAttempt) lastUsed.Add(i);
            usedThisAttempt.Clear();
            model.Reset();
            attempt++;
            restartPending = true;
            restartAt = -1f;
            lastEvent = "Reiniciado al caer (intento " + (attempt + 1) + ")";
        }

        private void OnRespawned()
        {
            if (restartPending) restartAt = Time.time + RestartDelay;
        }

        // ------------------------- Preparación -------------------------
        /// <summary>Una plantilla inactiva por tipo de robot de la fase. Si falta un tipo, se usa otro (seguridad primero).</summary>
        private void BuildTemplates()
        {
            var root = new GameObject("PlantillasOleadas");
            root.transform.SetParent(transform, false);
            root.SetActive(false);
            foreach (var robot in FindObjectsByType<EnemyPatrol>(FindObjectsSortMode.None))
            {
                if (robot.Route != null && robot.Route.Count > 0 && !routes.Contains(robot.Route)) routes.Add(robot.Route);
                var kind = KindOf(robot.name);
                if (templates.ContainsKey(kind)) continue;
                var copy = Instantiate(robot.gameObject, root.transform);
                int cut = robot.name.IndexOf('_');
                copy.name = cut > 0 ? robot.name.Substring(0, cut) : robot.name;
                templates[kind] = copy;
            }
            if (templates.Count == 0) return;
            GameObject fallback = templates.TryGetValue(RobotKind.Security, out var s) ? s : null;
            if (fallback == null) foreach (var t in templates.Values) { fallback = t; break; }
            foreach (RobotKind k in System.Enum.GetValues(typeof(RobotKind)))
                if (!templates.ContainsKey(k)) templates[k] = k == RobotKind.Elite && templates.TryGetValue(RobotKind.Guard, out var g) ? g : fallback;
        }

        public static RobotKind KindOf(string robotName)
        {
            string n = (robotName ?? "").ToLowerInvariant();
            if (n.Contains("elite") || n.Contains("élite")) return RobotKind.Elite;
            if (n.Contains("guard")) return RobotKind.Guard;
            if (n.Contains("investig") || n.Contains("research")) return RobotKind.Research;
            return RobotKind.Security;
        }

        /// <summary>Candidatos: waypoints de las rutas y puntos de cobertura de la fase.</summary>
        private void BuildCandidates()
        {
            candX.Clear(); candZ.Clear(); candPos.Clear();
            foreach (var r in routes)
                for (int i = 0; i < r.Count; i++) AddCandidate(r.GetPosition(i));
            foreach (var spot in CoverField.Spots) AddCandidate(CoverField.Position(spot));
        }

        private void AddCandidate(Vector3 p)
        {
            candPos.Add(p);
            candX.Add(p.x);
            candZ.Add(p.z);
        }

        private PatrolRoute NearestRoute(Vector3 pos, out int waypoint)
        {
            PatrolRoute best = null;
            waypoint = 0;
            float bestD = float.MaxValue;
            foreach (var r in routes)
                for (int i = 0; i < r.Count; i++)
                {
                    float d = (r.GetPosition(i) - pos).sqrMagnitude;
                    if (d < bestD) { bestD = d; best = r; waypoint = i; }
                }
            return best;
        }

        private int Seed(int wave) => SceneManager.GetActiveScene().name.GetHashCode() ^ (attempt * 7919) ^ (wave * 104729) ^ (int)(Time.time * 10f);

        private static void Say(string text)
        {
            if (HUDManager.Instance != null) HUDManager.Instance.QueueMessage(text, 3f);
        }

        /// <summary>Línea para el panel F3.</summary>
        public static string Summary()
        {
            var d = Instance;
            if (d == null || d.model == null) return "Encuentro: no hay en esta fase";
            string state = d.model.State == EncounterState.Idle ? (d.restartPending ? "esperando reaparición" : "en espera")
                         : d.model.State == EncounterState.Countdown ? $"cuenta atrás {d.model.CountdownLeft(Time.time):0.0} s"
                         : d.model.State == EncounterState.Active ? $"oleada {d.model.CurrentWave + 1}/{d.model.WaveCount}"
                         : "despejado";
            return $"Encuentro: {state} · {d.lastEvent}";
        }
    }
}
