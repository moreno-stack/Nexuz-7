using System.Collections.Generic;
using UnityEngine;

namespace Nexus7.Core
{
    /// <summary>
    /// Estadísticas de rendimiento de TODA la partida (estático: persiste entre escenas).
    /// Complementa a AlertSystem.Stats (sigilo) con recursos, fabricación, dispositivos, hackeos y tiempo.
    /// Los sistemas del juego llaman a los métodos Record*; ScoreCalculator los lee al final.
    /// </summary>
    public static class RunStats
    {
        private static readonly HashSet<int> deviceKindsUsed = new HashSet<int>();
        private static readonly HashSet<string> sectorsUsed = new HashSet<string>();

        public static float StartTime { get; private set; } = -1f;
        public static int ItemsCollected { get; private set; }
        public static int ItemsCrafted { get; private set; }
        public static int DevicesUsed { get; private set; }
        public static int DistinctDeviceKinds => deviceKindsUsed.Count;
        public static int HacksSucceeded { get; private set; }
        /// <summary>Sectores DISTINTOS apagados en la partida (repetir el mismo no suma más).</summary>
        public static int SectorsDisabled => sectorsUsed.Count;
        public static int EmpTargetsHit { get; private set; }
        public static int DecoysTriggered { get; private set; }
        public static float DamageTaken { get; private set; }
        public static int Deaths { get; private set; }

        public static bool HasStarted => StartTime >= 0f;
        /// <summary>Segundos desde el inicio de la partida (tiempo real, no afectado por pausas de Time.timeScale).</summary>
        public static float Elapsed => HasStarted ? Time.realtimeSinceStartup - StartTime : 0f;

        public static void EnsureStarted()
        {
            if (!HasStarted) StartTime = Time.realtimeSinceStartup;
        }

        public static void Reset()
        {
            StartTime = -1f;
            ItemsCollected = ItemsCrafted = DevicesUsed = HacksSucceeded = EmpTargetsHit = DecoysTriggered = Deaths = 0;
            DamageTaken = 0f;
            deviceKindsUsed.Clear();
            sectorsUsed.Clear();
        }

        public static void RecordCollected(int amount) { if (amount > 0) ItemsCollected += amount; }
        public static void RecordCrafted(int amount) { if (amount > 0) ItemsCrafted += amount; }
        public static void RecordDeviceUse(int kind) { DevicesUsed++; deviceKindsUsed.Add(kind); }
        public static void RecordHackSuccess() => HacksSucceeded++;
        public static void RecordSectorDisabled(string sectorId) { if (!string.IsNullOrEmpty(sectorId)) sectorsUsed.Add(sectorId); }
        public static void RecordEmpHits(int hits) { if (hits > 0) EmpTargetsHit += hits; }
        public static void RecordDecoyTriggered() => DecoysTriggered++;
        public static void RecordDamage(float amount) { if (amount > 0f) DamageTaken += amount; }
        public static void RecordDeath() => Deaths++;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => Reset();

        // El cronómetro arranca con la primera escena jugable (o al pulsar "Nueva partida").
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartOnFirstScene()
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != GameFlow.MenuScene) EnsureStarted();
        }
    }
}
