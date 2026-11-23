using System;
using System.Collections.Generic;
using Nexus7.Enemies;

namespace Nexus7.Director
{
    /// <summary>Punto en el plano del suelo (x, z). Sin dependencias de Unity para poder probarlo.</summary>
    public struct Point2
    {
        public float X, Z;
        public Point2(float x, float z) { X = x; Z = z; }
        public static float Distance(Point2 a, Point2 b) => (float)Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Z - b.Z) * (a.Z - b.Z));
    }

    /// <summary>Un refuerzo: qué tipo de robot (plantilla), en qué ruta, en qué waypoint y en qué sentido.</summary>
    public struct ReinforcementPick
    {
        public int Template, Route, StartIndex, Direction;
    }

    /// <summary>
    /// Anti-repetición al caer (lógica pura, probada en Tests/Suites/SpawnVariationTest.cs).
    /// En cada intento (cada vez que el jugador cae) decide, para cada robot, dónde reaparece y cómo patrulla,
    /// y qué refuerzos entran y por dónde, de forma que:
    ///   · JUSTO: nunca a menos de MinSafeDistance del punto de reaparición ni dentro de su campo de visión inicial
    ///     si hay otra opción (no te matan nada más aparecer).
    ///   · IMPREDECIBLE: penaliza los waypoints usados en los últimos intentos, y la combinación (waypoint, sentido)
    ///     nunca es igual a la del intento anterior si la ruta ofrece otra; ritmo y velocidad con pequeñas variaciones.
    ///   · REPRODUCIBLE: todo sale de la semilla de la partida + número de intento + robot (misma semilla = mismas decisiones,
    ///     útil para depurar), sin depender del orden en que se pregunte.
    /// </summary>
    public sealed class SpawnVariationModel
    {
        // ---- Parámetros de balance ----
        public float MinSafeDistance = 8f;
        public float ViewDistance = 18f;
        public float ViewHalfAngle = 35f;
        public int HistoryLength = 2;
        public float RandomWeight = 25f;
        public float MinWait = 0.7f, MaxWait = 1.3f;
        public float MinSpeed = 0.92f, MaxSpeed = 1.08f;

        private readonly int seed;
        private readonly Dictionary<string, List<int>> startHistory = new Dictionary<string, List<int>>();
        private readonly Dictionary<string, int> lastSignature = new Dictionary<string, int>();
        private readonly List<ReinforcementPick> lastReinforcements = new List<ReinforcementPick>();

        public int Attempt { get; private set; }

        public SpawnVariationModel(int seed) { this.seed = seed; }

        /// <summary>Empieza un intento nuevo (el jugador ha caído).</summary>
        public void BeginAttempt() => Attempt++;

        /// <summary>Puntuación de un punto de aparición (más = mejor). Pública para las pruebas y para ajustar el balance.</summary>
        public float Score(Point2 candidate, Point2 respawn, Point2 forward, int recentUses, bool wasLast, double random)
        {
            float d = Point2.Distance(candidate, respawn);
            float s = (float)(random * RandomWeight);
            if (d < MinSafeDistance) s -= 150f;                         // justo: nada encima del jugador
            if (InView(candidate, respawn, forward)) s -= 90f;           // ni delante de su cara (la justicia pesa más que la variedad)
            if (d >= 10f && d <= 25f) s += 10f;                          // a media distancia: amenaza presente pero lejos
            s -= recentUses * 25f;                                       // anti-repetición (máx. −80: nunca pesa más
            if (wasLast) s -= 30f;                                       //  que aparecer a la vista del jugador)
            return s;
        }

        /// <summary>¿Está 'p' dentro del cono de visión con el que reaparece el jugador?</summary>
        public bool InView(Point2 p, Point2 eye, Point2 forward)
        {
            float dx = p.X - eye.X, dz = p.Z - eye.Z;
            float d = (float)Math.Sqrt(dx * dx + dz * dz);
            if (d > ViewDistance || d < 0.001f) return d < 0.001f;
            float fl = (float)Math.Sqrt(forward.X * forward.X + forward.Z * forward.Z);
            if (fl < 0.001f) return false;
            double cos = (dx * forward.X + dz * forward.Z) / (d * fl);
            return cos >= Math.Cos(ViewHalfAngle * Math.PI / 180.0);
        }

        /// <summary>Cómo reaparece el robot 'robotId' que patrulla por 'waypoints' en este intento.</summary>
        public PatrolVariation Plan(string robotId, IReadOnlyList<Point2> waypoints, Point2 respawn, Point2 forward)
        {
            var rng = new Random(Hash(seed, Attempt, robotId));
            var v = new PatrolVariation
            {
                StartIndex = 0,
                Direction = rng.NextDouble() < 0.5 ? 1 : -1,
                WaitMultiplier = (float)(MinWait + (MaxWait - MinWait) * rng.NextDouble()),
                SpeedMultiplier = (float)(MinSpeed + (MaxSpeed - MinSpeed) * rng.NextDouble()),
            };
            int count = waypoints != null ? waypoints.Count : 0;
            if (count == 0) return v;

            var history = History(robotId);
            int last = history.Count > 0 ? history[history.Count - 1] : -1;
            int best = 0; float bestScore = float.MinValue;
            int second = -1; float secondScore = float.MinValue;
            for (int i = 0; i < count; i++)
            {
                float s = Score(waypoints[i], respawn, forward, CountRecent(history, i), i == last, rng.NextDouble());
                if (s > bestScore) { second = best; secondScore = bestScore; best = i; bestScore = s; }
                else if (s > secondScore) { second = i; secondScore = s; }
            }
            v.StartIndex = best;

            // La combinación (waypoint, sentido) no repite la del intento anterior
            int signature = best * 2 + (v.Direction > 0 ? 1 : 0);
            if (lastSignature.TryGetValue(robotId, out int prev) && prev == signature)
            {
                if (count > 1) v.Direction = -v.Direction;                 // misma salida: al revés
                else v.WaitMultiplier = v.WaitMultiplier > 1f ? MinWait : MaxWait;   // guardia fijo: otro ritmo
                signature = best * 2 + (v.Direction > 0 ? 1 : 0);
            }
            lastSignature[robotId] = signature;
            Remember(history, best);
            return v;
        }

        /// <summary>
        /// Refuerzos de este intento: 'budget' robots, cada uno de una plantilla y en una ruta. La composición
        /// (qué tipos y en qué rutas) cambia respecto al intento anterior siempre que haya alternativas.
        /// </summary>
        public List<ReinforcementPick> PlanReinforcements(int budget, int templateCount, IReadOnlyList<IReadOnlyList<Point2>> routes,
                                                          Point2 respawn, Point2 forward)
        {
            var picks = new List<ReinforcementPick>();
            if (budget <= 0 || templateCount <= 0 || routes == null || routes.Count == 0) { lastReinforcements.Clear(); return picks; }
            var rng = new Random(Hash(seed, Attempt, "refuerzos"));
            var usedRoutes = new HashSet<int>();
            for (int slot = 0; slot < budget; slot++)
            {
                // Tipo: al azar, pero distinto del que ocupó este hueco la vez anterior (si hay más de uno)
                int template = rng.Next(templateCount);
                if (templateCount > 1 && slot < lastReinforcements.Count && lastReinforcements[slot].Template == template)
                    template = (template + 1 + rng.Next(templateCount - 1)) % templateCount;

                // Ruta y punto: la mejor puntuación, evitando las rutas del intento anterior y repetir ruta en este
                int bestRoute = -1, bestIndex = 0; float bestScore = float.MinValue;
                for (int r = 0; r < routes.Count; r++)
                {
                    var wps = routes[r];
                    if (wps == null || wps.Count == 0) continue;
                    bool usedBefore = false;
                    foreach (var p in lastReinforcements) if (p.Route == r) usedBefore = true;
                    for (int i = 0; i < wps.Count; i++)
                    {
                        float s = Score(wps[i], respawn, forward, 0, false, rng.NextDouble());
                        if (usedBefore) s -= 30f;
                        if (usedRoutes.Contains(r)) s -= 50f;
                        if (s > bestScore) { bestScore = s; bestRoute = r; bestIndex = i; }
                    }
                }
                if (bestRoute < 0) break;
                usedRoutes.Add(bestRoute);
                picks.Add(new ReinforcementPick { Template = template, Route = bestRoute, StartIndex = bestIndex, Direction = rng.NextDouble() < 0.5 ? 1 : -1 });
            }
            lastReinforcements.Clear();
            lastReinforcements.AddRange(picks);
            return picks;
        }

        /// <summary>Variación del barrido de una cámara: dónde empieza (−1..1 del ángulo) y si invierte el sentido.</summary>
        public void PlanCameraSweep(string cameraId, out float startFraction, out bool reverse)
        {
            var rng = new Random(Hash(seed, Attempt, "cam:" + cameraId));
            startFraction = (float)(rng.NextDouble() * 2.0 - 1.0);
            reverse = rng.NextDouble() < 0.5;
        }

        private List<int> History(string id)
        {
            if (!startHistory.TryGetValue(id, out var h)) { h = new List<int>(); startHistory[id] = h; }
            return h;
        }

        private void Remember(List<int> history, int index)
        {
            history.Add(index);
            while (history.Count > HistoryLength) history.RemoveAt(0);
        }

        private static int CountRecent(List<int> history, int index)
        {
            int n = 0;
            foreach (var h in history) if (h == index) n++;
            return n;
        }

        /// <summary>Hash estable (FNV-1a): string.GetHashCode cambia entre ejecuciones y plataformas.</summary>
        public static int Hash(int seed, int attempt, string key)
        {
            unchecked
            {
                uint h = 2166136261u;
                h = (h ^ (uint)seed) * 16777619u;
                h = (h ^ (uint)attempt) * 16777619u;
                foreach (char c in key ?? "") h = (h ^ c) * 16777619u;
                return (int)(h & 0x7fffffff);
            }
        }
    }
}
