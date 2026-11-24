using UnityEngine;
using UnityEngine.SceneManagement;
using Nexus7.Combat;
using Nexus7.Core;
using Nexus7.Gameplay;
using Nexus7.UI;

namespace Nexus7.Director
{
    /// <summary>
    /// Recompensas del director (ModRewardModel): al despejar un encuentro de oleadas o destruir un élite de la fase
    /// (no los de refuerzo ni los de las oleadas), suelta un módulo de accesorio (ModPickup) elegido según cómo juegas,
    /// que alguna de tus armas pueda montar y, si puede, distinto del anterior. Lo dice un mensaje y la línea en F3.
    /// Se añade solo al jugador (CombatInstaller.PlayerInstalled).
    /// </summary>
    public class ModRewardDirector : MonoBehaviour
    {
        public static ModRewardDirector Instance { get; private set; }
        private const float MessageTime = 4f, FavouredShare = 0.6f;

        public WeaponMod Last { get; private set; } = WeaponMod.None;
        public int DropsThisPhase { get; private set; }
        private string lastReason = "";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            CombatInstaller.PlayerInstalled -= Attach;
            CombatInstaller.PlayerInstalled += Attach;
        }

        private static void Attach(GameObject player)
        {
            if (player.GetComponent<ModRewardDirector>() == null) player.AddComponent<ModRewardDirector>();
        }

        private void Awake() => Instance = this;
        private void OnDestroy() { if (Instance == this) Instance = null; }

        private void OnEnable()
        {
            CombatEvents.EnemyKilled += OnKilled;
            EncounterDirector.Cleared += OnCleared;
        }

        private void OnDisable()
        {
            CombatEvents.EnemyKilled -= OnKilled;
            EncounterDirector.Cleared -= OnCleared;
        }

        private void OnKilled(EnemyHealth enemy)
        {
            if (enemy == null || !enemy.HasCore || enemy.IsReinforcement) return;   // solo los élites de la fase
            Drop(enemy.transform.position + Vector3.up * 0.5f, "élite destruido");
        }

        private void OnCleared(Vector3 position) => Drop(position, "zona despejada");

        private void Drop(Vector3 position, string why)
        {
            var weapon = PlayerWeapon.Instance;
            if (weapon == null || weapon.Arsenal == null) return;
            var input = BuildInput(weapon.Arsenal);
            var rng = new System.Random(GameSession.RunSeed ^ SceneManager.GetActiveScene().name.GetHashCode()
                                        ^ ((DropsThisPhase + 1) * 7919) ^ (LivesSystem.RestartsHere * 104729));
            var mod = ModRewardModel.Choose(input, m => weapon.Arsenal.ModTarget(m) != null, Last, rng);
            if (mod == WeaponMod.None) return;   // todas las armas llevan ya lo que pueden llevar

            ModPickup.Spawn(position, mod);
            Last = mod;
            lastReason = ModRewardModel.Reason(mod, input);
            DropsThisPhase++;
            if (GameDirector.Instance != null) GameDirector.Instance.Report.RecordModReward();
            if (HUDManager.Instance != null)
                HUDManager.Instance.QueueMessage($"TRAJE: {why}. Módulo de arma recuperado: {WeaponModModel.Name(mod).ToLowerInvariant()} ({lastReason}). Recógelo.", MessageTime);
        }

        private static RewardInput BuildInput(ArsenalModel arsenal)
        {
            var input = new RewardInput { Accuracy = 0.35f, DetectionsPerMinute = 1f };
            var d = GameDirector.Instance;
            if (d == null) return input;
            var p = d.Performance;
            input.Accuracy = p.Accuracy;
            input.Shots = p.ShotsInWindow;
            input.Camping = p.CampingScore;
            input.DetectionsPerMinute = p.DetectionsPerMinute;
            input.KillsPerMinute = p.KillsPerMinute;
            var current = arsenal.Current;
            float share = p.WeaponDominance(out string id);
            input.HotWeaponFavoured = current != null && current.BaseSpec.HeatPerShot > 0f && id == current.Spec.Id && share >= FavouredShare;
            return input;
        }

        /// <summary>Línea para el panel F3.</summary>
        public static string Summary()
        {
            var d = Instance;
            if (d == null || d.Last == WeaponMod.None) return "Recompensas: ninguna todavía (élites y encuentros despejados)";
            return $"Recompensas: {d.DropsThisPhase} en la fase · última {WeaponModModel.Name(d.Last)} ({d.lastReason})";
        }
    }
}
