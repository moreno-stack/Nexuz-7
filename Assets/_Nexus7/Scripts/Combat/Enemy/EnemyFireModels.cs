using System;
using System.Collections.Generic;

namespace Nexus7.Combat
{
    /// <summary>
    /// Turnos de ataque (puro, probado): solo pueden disparar a la vez 'Capacity' robots (1 Fácil, 2 Normal, 3 Difícil,
    /// +1 si el director está apretando). Los demás esperan: así nunca te disparan seis robots a la vez.
    /// </summary>
    public sealed class AttackTokens
    {
        private readonly HashSet<int> holders = new HashSet<int>();
        public int Capacity { get; set; } = 2;
        public int InUse => holders.Count;

        public static int CapacityFor(int difficulty, int directorBonus) => Math.Max(1, (difficulty <= 0 ? 1 : difficulty >= 2 ? 3 : 2) + directorBonus);

        public bool Has(int id) => holders.Contains(id);

        public bool TryAcquire(int id)
        {
            if (holders.Contains(id)) return true;
            if (holders.Count >= Capacity) return false;
            holders.Add(id);
            return true;
        }

        public void Release(int id) => holders.Remove(id);
        public void Clear() => holders.Clear();
    }

    public enum BurstEvent { None, Warn, Fire, Finished, Cancelled }

    /// <summary>
    /// Ráfaga de un robot (pura, probada): AVISO (láser 0,4 s) → DISPAROS (n a la cadencia del arma) → PAUSA (aleatoria).
    /// El aviso es lo que hace el combate justo: siempre sabes que te van a disparar y desde dónde.
    /// </summary>
    public sealed class BurstModel
    {
        public enum Phase { Idle, Warning, Firing, Pause }
        public const float WarnTime = 0.4f;

        public Phase Current { get; private set; } = Phase.Idle;
        public int ShotsLeft { get; private set; }
        private readonly EnemyGunSpec gun;
        private float phaseEnd, nextShot;

        public BurstModel(EnemyGunSpec gun) { this.gun = gun; }

        /// <summary>Empieza una ráfaga si está libre. 'time' en segundos.</summary>
        public bool Begin(float time)
        {
            if (Current != Phase.Idle) return false;
            Current = Phase.Warning;
            phaseEnd = time + WarnTime;
            return true;
        }

        /// <summary>Avanza. 'canShoot' = sigue viéndolo con línea de tiro; si lo pierde, la ráfaga se corta. 'pauseRoll' en [0, 1).</summary>
        public BurstEvent Tick(float time, bool canShoot, double pauseRoll)
        {
            switch (Current)
            {
                case Phase.Warning:
                    if (!canShoot) { Cancel(time, pauseRoll); return BurstEvent.Cancelled; }
                    if (time < phaseEnd) return BurstEvent.None;
                    Current = Phase.Firing;
                    ShotsLeft = gun.BurstCount;
                    nextShot = time;
                    goto case Phase.Firing;
                case Phase.Firing:
                    if (!canShoot) { Cancel(time, pauseRoll); return BurstEvent.Cancelled; }
                    if (time < nextShot) return BurstEvent.None;
                    ShotsLeft--;
                    nextShot = time + 1f / Math.Max(0.1f, gun.BurstRate);
                    if (ShotsLeft <= 0) StartPause(time, pauseRoll);
                    return BurstEvent.Fire;
                case Phase.Pause:
                    if (time < phaseEnd) return BurstEvent.None;
                    Current = Phase.Idle;
                    return BurstEvent.Finished;
                default:
                    return BurstEvent.None;
            }
        }

        /// <summary>La ráfaga se corta (tambaleo, PEM, lo pierde de vista): pasa a la pausa sin disparar más.</summary>
        public void Cancel(float time, double pauseRoll)
        {
            if (Current == Phase.Idle || Current == Phase.Pause) return;
            StartPause(time, pauseRoll);
        }

        public bool IsWarningOrFiring => Current == Phase.Warning || Current == Phase.Firing;

        private void StartPause(float time, double roll)
        {
            Current = Phase.Pause;
            phaseEnd = time + gun.PauseMin + (gun.PauseMax - gun.PauseMin) * (float)Math.Max(0.0, Math.Min(1.0, roll));
        }
    }

    /// <summary>
    /// Tambaleo (puro, probado): si un robot recibe 15 de daño o más en 0,3 s, se tambalea 0,6 s: corta la ráfaga,
    /// suelta su turno y no puede volver a disparar hasta que se recupere. Premia concentrar el fuego.
    /// </summary>
    public sealed class StaggerModel
    {
        public const float Threshold = 15f, Window = 0.3f, Duration = 0.6f;
        private readonly Queue<KeyValuePair<float, float>> hits = new Queue<KeyValuePair<float, float>>();
        private float sum, staggerEnd = -999f;

        public bool IsStaggered(float time) => time < staggerEnd;

        /// <summary>Registra un impacto. Devuelve true si este impacto provoca el tambaleo.</summary>
        public bool AddDamage(float time, float amount)
        {
            while (hits.Count > 0 && time - hits.Peek().Key > Window) sum -= hits.Dequeue().Value;
            hits.Enqueue(new KeyValuePair<float, float>(time, amount));
            sum += amount;
            if (sum < Threshold - 1e-4f || IsStaggered(time)) return false;
            staggerEnd = time + Duration;
            hits.Clear();
            sum = 0f;
            return true;
        }
    }

    /// <summary>Indicador de daño (puro, probado): ángulo en pantalla del atacante respecto a hacia dónde mira el jugador.</summary>
    public static class DamageIndicatorMath
    {
        /// <summary>0 = delante, 90 = derecha, 180 = detrás, −90 = izquierda (en el plano del suelo).</summary>
        public static float ScreenAngle(float forwardX, float forwardZ, float toAttackerX, float toAttackerZ)
        {
            double f = Math.Atan2(forwardX, forwardZ);
            double a = Math.Atan2(toAttackerX, toAttackerZ);
            double d = (a - f) * 180.0 / Math.PI;
            while (d > 180.0) d -= 360.0;
            while (d < -180.0) d += 360.0;
            return (float)d;
        }
    }
}
