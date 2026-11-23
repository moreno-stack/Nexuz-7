using System;
using System.Collections.Generic;

namespace Nexus7.Director
{
    /// <summary>
    /// Analizador del jugador en tiempo real (lógica pura, probada en Tests/Suites/DirectorTest.cs).
    /// Recibe eventos (disparos, aciertos, bajas, daño, caídas, detecciones y posición) y resume cómo juega
    /// en una ventana deslizante de 'Window' segundos. Coste: O(eventos en la ventana), sin reservar memoria por fotograma.
    ///
    /// Métricas:
    ///   · Precisión (disparos con al menos un impacto / disparos), suavizada hacia el 35 % con pocos datos.
    ///   · Ritmo de bajas y daño recibido por minuto.
    ///   · Caídas recientes (con olvido: vida media de 90 s) y tiempo sobrevivido desde la última.
    ///   · Acampada: qué parte de los últimos 30 s ha pasado en la misma celda de 4 × 4 m.
    ///   · Abuso de un arma: qué parte de los disparos de la ventana son de la misma arma.
    /// Rendimiento = combinación de todo en [−1, 1] (−1 lo pasa muy mal, 0 justo, +1 lo domina).
    /// </summary>
    public sealed class PlayerPerformanceModel
    {
        public const float Window = 60f;
        public const float CampWindow = 30f;
        public const float CellSize = 4f;
        public const float DeathHalfLife = 90f;
        public const float TargetAccuracy = 0.35f;
        public const float TargetKillsPerMinute = 1.5f;
        public const float TargetDamagePerMinute = 40f;
        public const float SurvivalForFull = 240f;

        private struct Shot { public float t; public int id; public string weapon; }
        private struct Stamp { public float t; public float v; }
        private struct Sample { public float t; public int cx, cz; }

        private readonly Queue<Shot> shots = new Queue<Shot>();
        private readonly HashSet<int> hitShots = new HashSet<int>();
        private readonly Queue<Stamp> kills = new Queue<Stamp>();
        private readonly Queue<Stamp> damage = new Queue<Stamp>();
        private readonly Queue<Stamp> detections = new Queue<Stamp>();
        private readonly Queue<Sample> samples = new Queue<Sample>();
        private readonly Dictionary<string, int> weaponShots = new Dictionary<string, int>();
        private readonly Dictionary<long, int> cellCounts = new Dictionary<long, int>();

        private float now;
        private float deathScore;          // caídas con olvido exponencial
        private float lastDeathTime;
        private float damageSum;

        public int TotalDeaths { get; private set; }
        public int TotalKills { get; private set; }
        public float HealthFraction { get; set; } = 1f;

        public PlayerPerformanceModel(float startTime = 0f)
        {
            now = startTime;
            lastDeathTime = startTime;
        }

        // ------------------------- Entradas -------------------------
        public void RecordShot(float t, int shotId, string weaponId)
        {
            Advance(t);
            shots.Enqueue(new Shot { t = t, id = shotId, weapon = weaponId ?? "" });
            weaponShots.TryGetValue(weaponId ?? "", out int n);
            weaponShots[weaponId ?? ""] = n + 1;
        }

        /// <summary>Un proyectil del disparo 'shotId' ha dado a un enemigo (los perdigones del mismo disparo cuentan una vez).</summary>
        public void RecordHit(float t, int shotId)
        {
            Advance(t);
            foreach (var s in shots) if (s.id == shotId) { hitShots.Add(shotId); return; }
        }

        public void RecordKill(float t) { Advance(t); kills.Enqueue(new Stamp { t = t, v = 1f }); TotalKills++; }

        public void RecordDamageTaken(float t, float amount)
        {
            if (amount <= 0f) return;
            Advance(t);
            damage.Enqueue(new Stamp { t = t, v = amount });
            damageSum += amount;
        }

        public void RecordDeath(float t)
        {
            Advance(t);
            deathScore += 1f;
            lastDeathTime = t;
            TotalDeaths++;
        }

        public void RecordDetected(float t) { Advance(t); detections.Enqueue(new Stamp { t = t, v = 1f }); }

        /// <summary>Posición del jugador (llamar a intervalos regulares, p. ej. cada 0,5 s).</summary>
        public void SamplePosition(float t, float x, float z)
        {
            Advance(t);
            var s = new Sample { t = t, cx = (int)Math.Floor(x / CellSize), cz = (int)Math.Floor(z / CellSize) };
            samples.Enqueue(s);
            long key = Key(s.cx, s.cz);
            cellCounts.TryGetValue(key, out int n);
            cellCounts[key] = n + 1;
        }

        /// <summary>Avanza el reloj: olvida lo que sale de la ventana y atenúa las caídas.</summary>
        public void Advance(float t)
        {
            if (t > now)
            {
                deathScore *= (float)Math.Pow(0.5, (t - now) / DeathHalfLife);
                now = t;
            }
            while (shots.Count > 0 && now - shots.Peek().t > Window)
            {
                var s = shots.Dequeue();
                hitShots.Remove(s.id);
                if (weaponShots.TryGetValue(s.weapon, out int n)) { if (n <= 1) weaponShots.Remove(s.weapon); else weaponShots[s.weapon] = n - 1; }
            }
            while (kills.Count > 0 && now - kills.Peek().t > Window) kills.Dequeue();
            while (damage.Count > 0 && now - damage.Peek().t > Window) damageSum -= damage.Dequeue().v;
            while (detections.Count > 0 && now - detections.Peek().t > Window) detections.Dequeue();
            while (samples.Count > 0 && now - samples.Peek().t > CampWindow)
            {
                var s = samples.Dequeue();
                long key = Key(s.cx, s.cz);
                if (cellCounts.TryGetValue(key, out int n)) { if (n <= 1) cellCounts.Remove(key); else cellCounts[key] = n - 1; }
            }
            if (damageSum < 0f) damageSum = 0f;
        }

        // ------------------------- Métricas -------------------------
        public int ShotsInWindow => shots.Count;
        public int HitsInWindow => hitShots.Count;

        /// <summary>Precisión suavizada: con pocos disparos se acerca al 35 % (no castiga ni premia 2 disparos sueltos).</summary>
        public float Accuracy => (hitShots.Count + TargetAccuracy * 4f) / (shots.Count + 4f);

        /// <summary>Minutos efectivos de la ventana (al empezar la partida la ventana aún no está llena).</summary>
        private float WindowMinutes(float since) => Math.Max(0.5f, Math.Min(Window, now - since)) / 60f;

        public float KillsPerMinute => kills.Count / WindowMinutes(Start);
        public float DamagePerMinute => damageSum / WindowMinutes(Start);
        public float DetectionsPerMinute => detections.Count / WindowMinutes(Start);
        public float RecentDeaths => deathScore;
        public float TimeSinceDeath => now - lastDeathTime;
        private float Start => lastDeathTime;   // la ventana efectiva empieza en la última caída o al empezar

        /// <summary>Parte de los últimos 30 s en la misma celda de 4 × 4 m (0..1). Necesita al menos 20 s de muestras.</summary>
        public float CampingScore
        {
            get
            {
                if (samples.Count == 0 || now - samples.Peek().t < CampWindow * 0.66f) return 0f;
                int best = 0;
                foreach (var kv in cellCounts) if (kv.Value > best) best = kv.Value;
                return (float)best / samples.Count;
            }
        }

        /// <summary>Centro de la celda donde más tiempo ha pasado (para mandar allí una patrulla).</summary>
        public void CampCenter(out float x, out float z)
        {
            long bestKey = 0; int best = -1;
            foreach (var kv in cellCounts) if (kv.Value > best) { best = kv.Value; bestKey = kv.Key; }
            int cx = (int)(bestKey >> 32), cz = (int)(bestKey & 0xffffffffL);
            x = (cx + 0.5f) * CellSize;
            z = (cz + 0.5f) * CellSize;
        }

        /// <summary>Arma más usada en la ventana y qué parte de los disparos son suyos (0 con menos de 10 disparos).</summary>
        public float WeaponDominance(out string weaponId)
        {
            weaponId = null;
            if (shots.Count < 10) return 0f;
            int best = 0;
            foreach (var kv in weaponShots) if (kv.Value > best) { best = kv.Value; weaponId = kv.Key; }
            return (float)best / shots.Count;
        }

        /// <summary>
        /// Rendimiento en [−1, 1]. Pesos (suman más de 1 a propósito: un jugador que muere seguido baja a −1 aunque acierte):
        ///   +0,25 precisión · +0,20 ritmo de bajas (solo suma) · +0,20 supervivencia · +0,10 vida actual
        ///   −0,20 daño recibido · −0,35 caídas recientes · −0,10 detecciones
        /// Un jugador sigiloso que no dispara ni recibe daño queda en torno a +0,2 (la partida se anima poco a poco).
        /// </summary>
        public float Performance
        {
            get
            {
                float acc = Clamp((Accuracy - TargetAccuracy) / TargetAccuracy, -1f, 1f);
                // Solo suma: un jugador sigiloso que no dispara no «va mal» por no matar
                float kpm = Clamp((KillsPerMinute - TargetKillsPerMinute) / TargetKillsPerMinute, 0f, 1f);
                // 0 al minuto de la última caída, +1 a los 4 minutos, algo negativo justo después de caer
                float surv = Clamp((TimeSinceDeath - 60f) / (SurvivalForFull - 60f), -0.33f, 1f);
                // Recibir poco daño ayuda menos de lo que hunde recibir mucho
                float dmg = Clamp((DamagePerMinute - TargetDamagePerMinute) / TargetDamagePerMinute, -0.5f, 1f);
                float deaths = Math.Min(1f, RecentDeaths / 2f);
                float det = Math.Min(1f, DetectionsPerMinute / 4f);
                float hp = Clamp(HealthFraction * 2f - 1f, -1f, 1f);
                float p = 0.25f * acc + 0.2f * kpm + 0.2f * surv + 0.1f * hp - 0.2f * dmg - 0.35f * deaths - 0.1f * det;
                return Clamp(p, -1f, 1f);
            }
        }

        private static long Key(int cx, int cz) => ((long)cx << 32) | (uint)cz;
        private static float Clamp(float v, float a, float b) => v < a ? a : v > b ? b : v;
    }
}
