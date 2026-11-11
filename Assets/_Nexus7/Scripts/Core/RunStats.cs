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

        /// <summary>Copia de las estadísticas para la partida guardada.</summary>
        public static RunStatsSnapshot Snapshot()
        {
            var kinds = new int[deviceKindsUsed.Count]; deviceKindsUsed.CopyTo(kinds);
            var sectors = new string[sectorsUsed.Count]; sectorsUsed.CopyTo(sectors);
            return new RunStatsSnapshot
            {
                elapsed = Elapsed, itemsCollected = ItemsCollected, itemsCrafted = ItemsCrafted, devicesUsed = DevicesUsed,
                hacksSucceeded = HacksSucceeded, empTargetsHit = EmpTargetsHit, decoysTriggered = DecoysTriggered,
                deaths = Deaths, damageTaken = DamageTaken, deviceKinds = kinds, sectors = sectors,
            };
        }

        /// <summary>Restaura las estadísticas guardadas (el reloj sigue desde el tiempo que llevaba).</summary>
        public static void Restore(RunStatsSnapshot s)
        {
            Reset();
            if (s == null) { EnsureStarted(); return; }
            StartTime = Time.realtimeSinceStartup - Mathf.Max(0f, s.elapsed);
            ItemsCollected = s.itemsCollected; ItemsCrafted = s.itemsCrafted; DevicesUsed = s.devicesUsed;
            HacksSucceeded = s.hacksSucceeded; EmpTargetsHit = s.empTargetsHit; DecoysTriggered = s.decoysTriggered;
            Deaths = s.deaths; DamageTaken = s.damageTaken;
            if (s.deviceKinds != null) foreach (var k in s.deviceKinds) deviceKindsUsed.Add(k);
            if (s.sectors != null) foreach (var k in s.sectors) if (!string.IsNullOrEmpty(k)) sectorsUsed.Add(k);
        }

        public static void RecordCollected(int amount) { if (amount > 0) ItemsCollected += amount; }
        public static void RecordCrafted(int amount) { if (amount > 0) ItemsCrafted += amount; }
        public static void RecordDeviceUse(int kind, bool countsAsKind = true) { DevicesUsed++; if (countsAsKind) deviceKindsUsed.Add(kind); }
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
