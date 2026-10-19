using System;
using System.Collections.Generic;

namespace Nexus7.Gameplay
{
    public enum EndingType { Optimal, Tactical, Critical }

    /// <summary>Datos de entrada de la evaluación (se rellenan desde AlertSystem.Stats, RunStats e inventario).</summary>
    public struct ScoreInput
    {
        // Sigilo
        public int detections, alarms, redAlerts, captures;
        public float loudNoise;
        // Recursos
        public float resourceValue;      // valor de lo que queda en el inventario al escapar
        public int itemsCollected, itemsCrafted, devicesUsed;
        // Tiempo
        public float totalSeconds, evacuationRemaining;
        // Creatividad
        public int distinctDeviceKinds, hacksSucceeded, hacksFailed, sectorsDisabled, empTargetsHit, decoysTriggered;
        // Resultado
        public int deaths;
        /// <summary>Multiplicador de dificultad (Fácil 1 · Normal 1.15 · Difícil 1.3). 0 = sin indicar → 1.</summary>
        public float difficultyMultiplier;
        public bool escaped;
    }

    public class ScoreResult
    {
        public int stealth, resources, time, creativity, penalty, total;
        public string grade;
        public EndingType ending;
        public List<string> highlights = new List<string>();
    }

    /// <summary>
    /// EVALUACIÓN DE LA PARTIDA (lógica pura, sin Unity: se prueba aislada).
    ///
    ///   SIGILO       máx. 1500 = 1200 − 120·detecciones − 80·alarmas − 300·alertas rojas − 150·capturas − 10·ruido (máx. 300)
    ///                            + 300 de bonus "Fantasma" si nunca hubo Alerta Roja
    ///   RECURSOS     máx. 1000 = valor del inventario sobrante (máx. 600)
    ///                            + eficiencia de fabricación 400 × (dispositivos usados / fabricados)  [200 si no fabricó]
    ///   TIEMPO       máx. 1000 = 800 si la partida dura ≤ 25 min (baja linealmente hasta 0 a los 75 min)
    ///                            + segundos sobrantes de la cuenta atrás (máx. 200)
    ///   CREATIVIDAD  máx. 1000 = 150·tipos de dispositivo usados + 40·hackeos (máx. 300) + 60·sectores distintos apagados
    ///                            + 25·objetivos PEM (máx. 150) + 30·señuelos efectivos (máx. 150) − 20·hackeos fallidos
    ///   PENALIZACIÓN           = 200 por cada muerte
    ///   TOTAL = (suma − penalización), ×0.5 si no escapó.   Rangos: S ≥ 3800 · A ≥ 3000 · B ≥ 2200 · C ≥ 1400 · D
    ///
    /// FINALES:
    ///   Crítico  → no escapó (tiempo agotado o integridad 0 en la zona final)
    ///   Óptimo   → escapó sin ninguna Alerta Roja, con ≥ 100 de recursos sobrantes y como mucho 1 captura
    ///   Táctico  → cualquier otro escape (seguridad superada a base de hackeos y dispositivos bajo presión)
    /// </summary>
    public static class ScoreCalculator
    {
        public const int StealthMax = 1500, ResourcesMax = 1000, TimeMax = 1000, CreativityMax = 1000;
        public const float OptimalMinResources = 100f;
        /// <summary>Correr resta sigilo, pero como mucho esto (la cuenta atrás te obliga a correr).</summary>
        public const int NoisePenaltyMax = 300;
        /// <summary>Hackeos + dispositivos usados a partir de los cuales el escape cuenta como "a la fuerza".</summary>
        public const int TacticalActions = 4;

        public static EndingType DetermineEnding(ScoreInput i)
        {
            if (!i.escaped) return EndingType.Critical;
            if (i.redAlerts == 0 && i.resourceValue >= OptimalMinResources && i.captures <= 1) return EndingType.Optimal;
            return EndingType.Tactical;
        }

        /// <summary>Final táctico "de manual": la seguridad se superó con hackeos y dispositivos (no solo corriendo).</summary>
        public static bool UsedHeavyTactics(ScoreInput i) => i.hacksSucceeded + i.devicesUsed >= TacticalActions;

        public static ScoreResult Calculate(ScoreInput i)
        {
            var r = new ScoreResult { ending = DetermineEnding(i) };

            // ---- Sigilo ----
            int stealth = 1200 - 120 * i.detections - 80 * i.alarms - 300 * i.redAlerts - 150 * i.captures - Math.Min(NoisePenaltyMax, (int)(i.loudNoise * 10f));
            stealth = Clamp(stealth, 0, 1200);
            if (i.redAlerts == 0) { stealth += 300; r.highlights.Add("Fantasma: nunca se activó la Alerta Roja (+300)"); }
            r.stealth = Clamp(stealth, 0, StealthMax);

            // ---- Recursos ----
            int leftovers = Clamp((int)i.resourceValue, 0, 600);
            int efficiency = i.itemsCrafted > 0 ? (int)(400f * Math.Min(1f, (float)i.devicesUsed / i.itemsCrafted)) : 200;
            r.resources = Clamp(leftovers + efficiency, 0, ResourcesMax);
            if (i.itemsCrafted > 0 && i.devicesUsed >= i.itemsCrafted) r.highlights.Add("Ingeniero eficiente: usaste todo lo que fabricaste");

            // ---- Tiempo ----
            float baseTime = i.totalSeconds <= 1500f ? 800f : Math.Max(0f, 800f * (1f - (i.totalSeconds - 1500f) / 3000f));
            int bonus = Clamp((int)i.evacuationRemaining, 0, 200);
            r.time = Clamp((int)baseTime + bonus, 0, TimeMax);
            if (i.escaped && i.totalSeconds <= 1500f) r.highlights.Add("Relámpago: escapaste en menos de 25 minutos");

            // ---- Creatividad ----
            int creativity = 150 * i.distinctDeviceKinds
                           + Math.Min(300, 40 * i.hacksSucceeded)
                           + 60 * i.sectorsDisabled
                           + Math.Min(150, 25 * i.empTargetsHit)
                           + Math.Min(150, 30 * i.decoysTriggered)
                           - 20 * i.hacksFailed;
            r.creativity = Clamp(creativity, 0, CreativityMax);
            if (i.distinctDeviceKinds >= 3) r.highlights.Add("Arsenal completo: usaste los tres tipos de dispositivo");

            // ---- Total ----
            r.penalty = 200 * i.deaths;
            int sum = r.stealth + r.resources + r.time + r.creativity - r.penalty;
            int baseTotal = Math.Max(0, i.escaped ? sum : sum / 2);
            double mult = i.difficultyMultiplier > 0f ? i.difficultyMultiplier : 1.0;
            r.total = (int)Math.Round(baseTotal * mult, MidpointRounding.AwayFromZero);
            r.grade = Grade(r.total);
            return r;
        }

        public static string Grade(int total) =>
            total >= 3800 ? "S" : total >= 3000 ? "A" : total >= 2200 ? "B" : total >= 1400 ? "C" : "D";

        private static int Clamp(int v, int min, int max) => v < min ? min : v > max ? max : v;
    }
}
