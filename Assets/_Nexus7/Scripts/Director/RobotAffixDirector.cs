using System.Collections;
using System.Collections.Generic;
using System.Text;
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
    /// Variantes de robot (RobotAffixModel): al empezar la fase y en cada reaparición, el director reparte rasgos
    /// (blindado, rápido, ojo de águila) entre los robots vivos según la tensión, sin repetir el reparto anterior.
    /// Los refuerzos y las oleadas aparecen sin rasgo. Lo que hay en juego se ve en el panel F3.
    /// Se añade solo al jugador (CombatInstaller.PlayerInstalled).
    /// </summary>
    public class RobotAffixDirector : MonoBehaviour
    {
        public static RobotAffixDirector Instance { get; private set; }
        private const float InitialDelay = 0.6f;

        private Dictionary<string, RobotAffix> current = new Dictionary<string, RobotAffix>();
        private int attempt;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            CombatInstaller.PlayerInstalled -= Attach;
            CombatInstaller.PlayerInstalled += Attach;
        }

        private static void Attach(GameObject player)
        {
            if (player.GetComponent<RobotAffixDirector>() == null) player.AddComponent<RobotAffixDirector>();
        }

        private void Awake() => Instance = this;
        private void OnDestroy() { if (Instance == this) Instance = null; }
        private void OnEnable() => PlayerRespawn.PlayerRespawned += OnRespawned;
        private void OnDisable() => PlayerRespawn.PlayerRespawned -= OnRespawned;

        private IEnumerator Start()
        {
            yield return new WaitForSeconds(InitialDelay);
            Roll(false);
        }

        private void OnRespawned()
        {
            attempt++;
            Roll(true);
        }

        private void Roll(bool announce)
        {
            var robots = new List<EnemyPatrol>();
            var ids = new List<string>();
            foreach (var p in EnemyPatrol.All)
            {
                if (p == null || !p.enabled) continue;
                var h = p.GetComponent<EnemyHealth>();
                if (h != null && (!h.IsAlive || h.IsReinforcement)) continue;
                robots.Add(p);
                ids.Add(p.name);
            }
            float tension = GameDirector.Instance != null ? GameDirector.Instance.Model.Tension : 0.5f;
            int count = RobotAffixModel.CountFor(tension, robots.Count);
            int seed = GameSession.RunSeed ^ SceneManager.GetActiveScene().name.GetHashCode() ^ (attempt * 7919) ^ (LivesSystem.RestartsHere * 104729);
            var next = RobotAffixModel.Assign(ids, count, seed, current);

            var sb = new StringBuilder();
            foreach (var p in robots)
            {
                var state = p.GetComponent<RobotAffixState>();
                if (state == null) state = p.gameObject.AddComponent<RobotAffixState>();   // sin «??»: Unity devuelve un nulo falso
                next.TryGetValue(p.name, out var affix);
                state.Apply(affix);
                if (affix != RobotAffix.None) sb.Append(sb.Length > 0 ? ", " : "").Append(RobotAffixModel.Name(affix));
            }
            current = next;
            if (announce && sb.Length > 0 && HUDManager.Instance != null)
                HUDManager.Instance.QueueMessage("IA NEXUS-7: unidades mejoradas en este intento: " + sb + ".", 3f);
        }

        /// <summary>Línea para el panel F3.</summary>
        public static string Summary()
        {
            var d = Instance;
            if (d == null || d.current.Count == 0) return "Variantes: ninguna (tensión < 0,4)";
            var sb = new StringBuilder("Variantes: ");
            bool first = true;
            foreach (var kv in d.current)
            {
                if (!first) sb.Append(" · ");
                sb.Append(kv.Key).Append(' ').Append(RobotAffixModel.Name(kv.Value));
                first = false;
            }
            return sb.ToString();
        }
    }
}
