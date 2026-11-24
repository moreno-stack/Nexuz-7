using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Nexus7.Combat;
using Nexus7.Core;
using Nexus7.Enemies;
using Nexus7.Gameplay;
using Nexus7.UI;

namespace Nexus7.Director
{
    /// <summary>
    /// Eventos de nave (ShipEventModel): al empezar la fase y en cada reaparición, el director lanza un evento en algún
    /// punto de tu camino (a 6–25 m, mejor por delante), distinto del intento anterior:
    ///   · APAGÓN: las luces en 7 m bajan al 20 % y la zona cuenta como sombra (te ven menos);
    ///   · FUGA DE VAPOR: 2 nubes que tapan la vista de los robots (VisionSmoke);
    ///   · BARRIDO DE SEGURIDAD: a los 15 s, y cada 45 s, un aviso de 25 m manda a los robots cercanos a revisar la zona.
    /// El evento dura hasta la siguiente caída. Lo que hay en juego se ve en el panel F3.
    /// Se añade solo al jugador (CombatInstaller.PlayerInstalled).
    /// </summary>
    public class ShipEventDirector : MonoBehaviour
    {
        public static ShipEventDirector Instance { get; private set; }
        private const float InitialDelay = 1f, RespawnDelay = 0.5f, MessageTime = 3.5f;

        public ShipEvent Current { get; private set; } = ShipEvent.None;

        private readonly List<Vector3> spots = new List<Vector3>();
        private readonly List<float> xs = new List<float>(), zs = new List<float>();
        private readonly List<GameObject> spawned = new List<GameObject>();
        private readonly List<Light> dimmed = new List<Light>();
        private readonly List<float> dimmedIntensity = new List<float>();
        private Vector3 spot;
        private ShipEvent previous = ShipEvent.None;
        private int previousSpot = -1, attempt;
        private float nextSweep = float.MaxValue;
        private Coroutine pending;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            CombatInstaller.PlayerInstalled -= Attach;
            CombatInstaller.PlayerInstalled += Attach;
        }

        private static void Attach(GameObject player)
        {
            if (player.GetComponent<ShipEventDirector>() == null) player.AddComponent<ShipEventDirector>();
        }

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            Clear();
            if (Instance == this) Instance = null;
        }

        private void OnEnable() => PlayerRespawn.PlayerRespawned += OnRespawned;
        private void OnDisable() => PlayerRespawn.PlayerRespawned -= OnRespawned;

        private void Start() => pending = StartCoroutine(RollAfter(InitialDelay));

        private void OnRespawned()
        {
            attempt++;
            Clear();
            if (pending != null) StopCoroutine(pending);
            pending = StartCoroutine(RollAfter(RespawnDelay));   // deja que el jugador llegue al punto de reaparición
        }

        private IEnumerator RollAfter(float delay)
        {
            yield return new WaitForSeconds(delay);
            pending = null;
            Roll();
        }

        private void Update()
        {
            if (Current != ShipEvent.SecuritySweep || Time.time < nextSweep || EnemyPatrol.Paused) return;
            nextSweep = Time.time + ShipEventModel.SweepRepeat;
            NoiseSystem.Emit(spot, ShipEventModel.SweepNoiseRadius, null);
            Say("IA NEXUS-7: barrido de seguridad en curso. Las unidades cercanas revisan el sector.");
        }

        private void Roll()
        {
            var rng = new System.Random(GameSession.RunSeed ^ SceneManager.GetActiveScene().name.GetHashCode()
                                        ^ (attempt * 7919) ^ (LivesSystem.RestartsHere * 104729) ^ 0x5EED);
            var director = GameDirector.Instance;
            float tension = director != null ? director.Model.Tension : 0.5f;
            float deaths = director != null ? director.Performance.RecentDeaths : 0f;
            var next = ShipEventModel.Choose(tension, deaths, previous, rng.NextDouble());
            if (next == ShipEvent.None) return;

            CollectSpots();
            Vector3 p = transform.position, f = transform.forward;
            int i = ShipEventModel.PickSpot(xs, zs, p.x, p.z, f.x, f.z, previousSpot, rng);
            if (i < 0) return;   // nada a 6–25 m: esta vez no hay evento
            previousSpot = i;
            spot = spots[i];
            Current = previous = next;
            if (director != null) director.Report.RecordShipEvent();

            switch (next)
            {
                case ShipEvent.Blackout:
                    Blackout();
                    Say("NAVE: fallo eléctrico en un sector cercano. Sin luz, a los robots les cuesta más verte.");
                    break;
                case ShipEvent.SteamLeak:
                    SteamLeak();
                    Say("NAVE: fuga de vapor en un conducto cercano. El vapor tapa la vista de los robots.");
                    break;
                case ShipEvent.SecuritySweep:
                    nextSweep = Time.time + ShipEventModel.SweepDelay;
                    Say($"IA NEXUS-7: barrido de seguridad programado en {ShipEventModel.SweepDelay:0} s. Muévete.");
                    break;
            }
        }

        /// <summary>Candidatos: puntos de cobertura y waypoints de las rutas de patrulla (sitios por los que se pasa).</summary>
        private void CollectSpots()
        {
            spots.Clear();
            foreach (var s in CoverField.Spots) spots.Add(CoverField.Position(s));
            var routes = new HashSet<PatrolRoute>();
            foreach (var e in EnemyPatrol.All)
            {
                if (e == null || e.Route == null || !routes.Add(e.Route)) continue;
                for (int k = 0; k < e.Route.Count; k++) spots.Add(e.Route.GetPosition(k));
            }
            xs.Clear();
            zs.Clear();
            foreach (var s in spots) { xs.Add(s.x); zs.Add(s.z); }
        }

        private void Blackout()
        {
            float r = ShipEventModel.BlackoutRadius;
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (l == null || !l.enabled || l.type == LightType.Directional) continue;
                if (l.transform.IsChildOf(transform) || l.GetComponentInParent<EnemyPatrol>() != null) continue;   // linternas y ópticas
                if ((l.transform.position - spot).sqrMagnitude > r * r) continue;
                dimmed.Add(l);
                dimmedIntensity.Add(l.intensity);
                l.intensity *= ShipEventModel.BlackoutLightFactor;
            }
            var zone = new GameObject("Evento_Apagon") { layer = 2 };   // Ignore Raycast: no tapa disparos ni visión
            zone.transform.position = spot + Vector3.up * 2f;
            var box = zone.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(r * 2f, 4f, r * 2f);
            zone.AddComponent<BlackoutZone>();
            spawned.Add(zone);
        }

        private void SteamLeak()
        {
            Vector3 side = Vector3.Cross(Vector3.up, spot - transform.position);
            side.y = 0f;
            side = side.sqrMagnitude > 0.01f ? side.normalized : Vector3.right;
            for (int k = 0; k < ShipEventModel.SteamClouds; k++)
            {
                var go = new GameObject("Evento_Vapor") { layer = 2 };
                go.transform.position = spot + side * (k == 0 ? -1.3f : 1.3f);
                go.AddComponent<ParticleSystem>();
                go.AddComponent<Nexus7.Environment.SmokeEmitter>();
                go.AddComponent<Nexus7.Environment.VisionSmoke>();
                spawned.Add(go);
            }
        }

        /// <summary>Quita el evento en curso: devuelve las luces y borra zonas y nubes.</summary>
        private void Clear()
        {
            for (int k = 0; k < dimmed.Count; k++)
                if (dimmed[k] != null) dimmed[k].intensity = dimmedIntensity[k];
            dimmed.Clear();
            dimmedIntensity.Clear();
            foreach (var go in spawned) if (go != null) Destroy(go);
            spawned.Clear();
            nextSweep = float.MaxValue;
            Current = ShipEvent.None;
        }

        private static void Say(string text)
        {
            if (HUDManager.Instance != null) HUDManager.Instance.QueueMessage(text, MessageTime);
        }

        /// <summary>Línea para el panel F3.</summary>
        public static string Summary()
        {
            var d = Instance;
            if (d == null || d.Current == ShipEvent.None) return "Evento de nave: ninguno";
            float dist = Vector3.Distance(d.transform.position, d.spot);
            string extra = d.Current == ShipEvent.SecuritySweep ? $" · próximo barrido en {Mathf.Max(0f, d.nextSweep - Time.time):0} s" : "";
            return $"Evento de nave: <b>{ShipEventModel.Name(d.Current)}</b> a {dist:0} m{extra}";
        }
    }
}
