using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Nexus7.Combat;
using Nexus7.Core;
using Nexus7.Enemies;
using Nexus7.Gameplay;
using Nexus7.Security;
using Nexus7.UI;

namespace Nexus7.Director
{
    /// <summary>
    /// Respawn dinámico y anti-repetición. Adaptador de Unity de SpawnVariationModel:
    ///   · Al caer el jugador (PlayerCaught) planifica el siguiente intento: cada robot reaparece en otro waypoint, en otro
    ///     sentido y con otro ritmo (EnemyPatrol.VariationProvider), y las cámaras empiezan su barrido en otro punto.
    ///   · Según la tensión del director añade REFUERZOS (copias de los robots de la fase) en rutas lejos del jugador,
    ///     con tipos y rutas distintos a los del intento anterior. Al caer otra vez se retiran y se vuelve a decidir.
    ///   · Al entrar en una fase con la tensión alta (vienes dominando), la fase ya empieza con refuerzos.
    /// Justo: nada aparece a menos de 8 m del punto de reaparición ni delante del jugador si hay alternativa.
    ///
    /// Colocar en: el jugador (se añade solo junto a GameDirector).
    /// </summary>
    public class RespawnVariator : MonoBehaviour
    {
        [Tooltip("Segundos tras cargar la fase antes de desplegar los refuerzos iniciales (deja hornear el NavMesh).")]
        [SerializeField] private float initialDelay = 2f;
        [Tooltip("Tope de refuerzos vivos a la vez (además de los robots de la fase).")]
        [SerializeField] private int maxReinforcements = 2;

        public SpawnVariationModel Model { get; private set; }
        public int ActiveReinforcements => reinforcements.Count;

        private readonly Dictionary<EnemyPatrol, PatrolVariation> plan = new Dictionary<EnemyPatrol, PatrolVariation>();
        private readonly List<GameObject> templates = new List<GameObject>();
        private readonly List<PatrolRoute> routes = new List<PatrolRoute>();
        private readonly List<GameObject> reinforcements = new List<GameObject>();
        private List<ReinforcementPick> pendingPicks;
        private PlayerRespawn respawn;
        private Transform templateRoot;
        private string lastSummary = "—";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Hook()
        {
            CombatInstaller.PlayerInstalled -= Attach;
            CombatInstaller.PlayerInstalled += Attach;
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null && player.GetComponent<PlayerWeapon>() != null) Attach(player);
        }

        private static void Attach(GameObject player)
        {
            if (player.GetComponent<RespawnVariator>() == null) player.AddComponent<RespawnVariator>();
        }

        private void Awake()
        {
            respawn = GetComponent<PlayerRespawn>();
            int sceneHash = SpawnVariationModel.Hash(0, 0, SceneManager.GetActiveScene().name);
            // Tras un reinicio de fase por quedarse sin vidas, otra semilla: la fase no se repite igual
            Model = new SpawnVariationModel(GameSession.RunSeed ^ sceneHash ^ (LivesSystem.RestartsHere * 15485863));
        }

        private void OnEnable()
        {
            EnemyPatrol.VariationProvider = Provide;
            PlayerRespawn.PlayerCaught += OnCaught;
            PlayerRespawn.PlayerRespawned += OnRespawned;
            GameDirector.ExtraOverlay += DrawOverlay;
        }

        private void OnDisable()
        {
            if (EnemyPatrol.VariationProvider == (System.Func<EnemyPatrol, PatrolVariation?>)Provide) EnemyPatrol.VariationProvider = null;
            PlayerRespawn.PlayerCaught -= OnCaught;
            PlayerRespawn.PlayerRespawned -= OnRespawned;
            GameDirector.ExtraOverlay -= DrawOverlay;
        }

        private IEnumerator Start()
        {
            BuildTemplates();
            yield return new WaitForSeconds(initialDelay);
            // Progresión adaptativa: si vienes dominando, la fase ya empieza con más presión
            if (!respawn || !respawn.IsRespawning) SpawnReinforcements(PlanReinforcements(transform.position, transform.forward), "Al entrar");
        }

        /// <summary>Una plantilla inactiva por tipo de robot de la fase (seguridad, guardia, élite...).</summary>
        private void BuildTemplates()
        {
            var root = new GameObject("PlantillasRefuerzo");
            root.SetActive(false);   // lo que cuelga de aquí no se despierta (no patrulla, no detecta)
            templateRoot = root.transform;
            var seenStyles = new HashSet<string>();
            foreach (var robot in FindObjectsByType<EnemyPatrol>(FindObjectsSortMode.None))
            {
                if (robot.Route != null && !routes.Contains(robot.Route) && robot.Route.Count > 0) routes.Add(robot.Route);
                string style = Style(robot.name);
                if (!seenStyles.Add(style)) continue;
                var copy = Instantiate(robot.gameObject, templateRoot);
                copy.name = style;
                templates.Add(copy);
            }
        }

        private static string Style(string robotName)
        {
            int i = robotName.IndexOf('_');
            return i > 0 ? robotName.Substring(0, i) : robotName;
        }

        // ------------------------- Al caer: planificar el intento siguiente -------------------------
        private void OnCaught()
        {
            Model.BeginAttempt();
            DespawnReinforcements();
            plan.Clear();

            Vector3 point = respawn != null ? respawn.RespawnPoint : transform.position;
            Vector3 facing = respawn != null ? respawn.RespawnFacing * Vector3.forward : transform.forward;
            var origin = new Point2(point.x, point.z);
            var forward = new Point2(facing.x, facing.z);

            var sb = new System.Text.StringBuilder();
            foreach (var robot in EnemyPatrol.All)
            {
                if (robot == null || robot.Route == null || robot.Route.Count == 0) continue;
                var v = Model.Plan(robot.name, Waypoints(robot.Route), origin, forward);
                plan[robot] = v;
                if (sb.Length < 120) sb.Append(robot.name).Append(" WP").Append(v.StartIndex).Append(v.Direction > 0 ? "→ " : "← ");
            }
            lastSummary = sb.Length > 0 ? sb.ToString() : "—";

            foreach (var cam in FindObjectsByType<SecurityCamera>(FindObjectsSortMode.None))
            {
                Model.PlanCameraSweep(cam.name, out float fraction, out bool reverse);
                cam.VarySweep(fraction, reverse);
            }
            pendingPicks = PlanReinforcements(point, facing);
        }

        private PatrolVariation? Provide(EnemyPatrol robot) =>
            plan.TryGetValue(robot, out var v) ? v : (PatrolVariation?)null;

        private void OnRespawned()
        {
            if (pendingPicks != null) SpawnReinforcements(pendingPicks, "Al reaparecer");
            pendingPicks = null;
            if (HUDManager.Instance != null)
                HUDManager.Instance.QueueMessage(reinforcements.Count > 0
                    ? $"IA NEXUS-7: rutas de patrulla recalculadas. Refuerzos desplegados: {reinforcements.Count}."
                    : "IA NEXUS-7: rutas de patrulla recalculadas.", 3f);
        }

        // ------------------------- Refuerzos -------------------------
        private List<ReinforcementPick> PlanReinforcements(Vector3 point, Vector3 facing)
        {
            int budget = GameDirector.Instance != null ? GameDirector.Instance.Model.Knobs.Reinforcements : 0;
            budget = Mathf.Min(budget, maxReinforcements);
            var routePoints = new List<IReadOnlyList<Point2>>();
            foreach (var r in routes) routePoints.Add(r != null ? Waypoints(r) : new List<Point2>());
            return Model.PlanReinforcements(budget, templates.Count, routePoints, new Point2(point.x, point.z), new Point2(facing.x, facing.z));
        }

        private void SpawnReinforcements(List<ReinforcementPick> picks, string when)
        {
            if (picks == null) return;
            foreach (var p in picks)
            {
                if (p.Template >= templates.Count || p.Route >= routes.Count || routes[p.Route] == null) continue;
                var route = routes[p.Route];
                int index = Mathf.Clamp(p.StartIndex, 0, route.Count - 1);
                Vector3 pos = route.GetPosition(index);
                var go = Instantiate(templates[p.Template], pos, Quaternion.identity);   // se despierta ya; Start será el próximo fotograma
                go.name = templates[p.Template].name + "_Refuerzo" + (reinforcements.Count + 1);
                var patrol = go.GetComponent<EnemyPatrol>();
                if (patrol != null) patrol.AssignRoute(route, index, p.Direction);
                var health = go.GetComponent<EnemyHealth>();
                if (health != null) health.IsReinforcement = true;
                reinforcements.Add(go);
                Nexus7.Devices.PulseRing.Spawn(pos, 2f, new Color(1f, 0.3f, 0.25f), 0.8f);
            }
            if (picks.Count > 0) lastSummary += $" · {when}: {picks.Count} refuerzo(s)";
        }

        private void DespawnReinforcements()
        {
            foreach (var go in reinforcements) if (go != null) Destroy(go);
            reinforcements.Clear();
        }

        private static List<Point2> Waypoints(PatrolRoute route)
        {
            var list = new List<Point2>(route.Count);
            for (int i = 0; i < route.Count; i++)
            {
                var p = route.GetPosition(i);
                list.Add(new Point2(p.x, p.z));
            }
            return list;
        }

        private void DrawOverlay(float x, float y, float w, GUIStyle style)
        {
            GUI.Label(new Rect(x, y, w, 20f), $"<b>Respawn dinámico</b>  intento {Model.Attempt} · refuerzos vivos {reinforcements.Count}", style);
            GUI.Label(new Rect(x, y + 20f, w, 40f), lastSummary, style);
        }
    }
}
