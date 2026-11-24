using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Nexus7.Combat;
using Nexus7.Enemies;
using Nexus7.Gameplay;
using Nexus7.UI;

namespace Nexus7.Director
{
    /// <summary>
    /// La IA aprende tus rutas (HeatmapModel): apunta dónde te detectan, dónde caes y dónde acampas. En cada reaparición
    /// enfría el mapa y manda al robot libre más cercano a vigilar la zona más caliente (lejos de tu reaparición y distinta
    /// de la última). Con la última vida no lo hace (director clemente). El mapa de cada fase sobrevive al reinicio por
    /// quedarte sin vidas, así la fase reiniciada también recuerda; una partida nueva lo borra.
    /// Se añade solo al jugador (CombatInstaller.PlayerInstalled).
    /// </summary>
    public class HeatmapDirector : MonoBehaviour
    {
        public static HeatmapDirector Instance { get; private set; }
        private const float SpotCooldown = 3f, CampSampleEvery = 5f, AssignDelay = 2.5f, MessageTime = 4f;

        private static readonly Dictionary<string, HeatmapModel> maps = new Dictionary<string, HeatmapModel>();

        public HeatmapModel Map { get; private set; }
        private float lastSpot = -99f, campTimer;
        private string lastAction = "";
        private PlayerRespawn respawn;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            CombatInstaller.PlayerInstalled -= Attach;
            CombatInstaller.PlayerInstalled += Attach;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => maps.Clear();

        /// <summary>Partida nueva: se olvidan todas las rutas (lo llama DirectorSession.Clear).</summary>
        public static void ClearAll() => maps.Clear();

        private static void Attach(GameObject player)
        {
            if (player.GetComponent<HeatmapDirector>() == null) player.AddComponent<HeatmapDirector>();
        }

        private void Awake()
        {
            Instance = this;
            respawn = GetComponent<PlayerRespawn>();
            string scene = SceneManager.GetActiveScene().name;
            if (!maps.TryGetValue(scene, out var m)) { m = new HeatmapModel(); maps[scene] = m; }
            Map = m;
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        private void OnEnable()
        {
            EnemyPatrol.PlayerSpotted += OnSpotted;
            PlayerRespawn.PlayerCaught += OnCaught;
            PlayerRespawn.PlayerRespawned += OnRespawned;
        }

        private void OnDisable()
        {
            EnemyPatrol.PlayerSpotted -= OnSpotted;
            PlayerRespawn.PlayerCaught -= OnCaught;
            PlayerRespawn.PlayerRespawned -= OnRespawned;
        }

        private void OnSpotted(EnemyPatrol by)
        {
            if (Time.time - lastSpot < SpotCooldown) return;   // el mismo encuentro no cuenta varias veces
            lastSpot = Time.time;
            Map.Add(transform.position.x, transform.position.z, HeatmapModel.DetectionHeat);
        }

        private void OnCaught() => Map.Add(transform.position.x, transform.position.z, HeatmapModel.DeathHeat);

        private void Update()
        {
            campTimer += Time.deltaTime;
            if (campTimer < CampSampleEvery) return;
            campTimer = 0f;
            var d = GameDirector.Instance;
            if (d != null && d.Performance.CampingScore >= HeatmapModel.CampThreshold)
                Map.Add(transform.position.x, transform.position.z, HeatmapModel.CampHeat);
        }

        private void OnRespawned()
        {
            Map.Decay();
            StopAllCoroutines();
            StartCoroutine(AssignAfterDelay());
        }

        /// <summary>Espera a que los robots vuelvan a moverse tras la reaparición y manda a uno a la zona caliente.</summary>
        private IEnumerator AssignAfterDelay()
        {
            yield return new WaitForSeconds(AssignDelay);
            if (LivesSystem.IsLastLife) { lastAction = "última vida: no vigila"; yield break; }
            Vector3 safe = respawn != null ? respawn.RespawnPoint : transform.position;
            if (!Map.PickHotspot(safe.x, safe.z, out float x, out float z, out float h)) { lastAction = "sin zona caliente"; yield break; }
            var target = new Vector3(x, transform.position.y, z);
            var unit = GameDirector.NearestFreeRobot(target);
            if (unit == null) { lastAction = "sin robot libre"; yield break; }
            unit.RespondToSquad(target);
            lastAction = $"{unit.name} vigila ({x:0}, {z:0}), calor {h:0.0}";
            if (GameDirector.Instance != null) GameDirector.Instance.Report.RecordHeatGuard();
            if (HUDManager.Instance != null)
                HUDManager.Instance.QueueMessage("IA NEXUS-7: hemos aprendido tu ruta. Una unidad vigila el sector donde más te hemos visto.", MessageTime);
        }

        /// <summary>Línea para el panel F3.</summary>
        public static string Summary()
        {
            var d = Instance;
            if (d == null) return "Mapa de calor: —";
            return $"Mapa de calor: {d.Map.Cells} zonas · aquí {d.Map.HeatAt(d.transform.position.x, d.transform.position.z):0.0}"
                   + (d.lastAction.Length > 0 ? " · " + d.lastAction : "");
        }
    }
}
