using System;
using System.Collections.Generic;

namespace Nexus7.Combat
{
    /// <summary>Punto de cobertura junto a una cara de un objeto (caja, estantería, consola...). Coordenadas del suelo.</summary>
    public struct CoverSpot
    {
        public int Id;
        public float X, Y, Z;
        /// <summary>Normal de la cara (hacia fuera del objeto): el robot se pone de espaldas a ella.</summary>
        public float NormalX, NormalZ;
        /// <summary>Cobertura baja (&lt; 1,2 m): protege menos de arriba.</summary>
        public bool Low;
    }

    /// <summary>Cómo pelea cada tipo de robot armado.</summary>
    public enum TacticStyle
    {
        /// <summary>Seguridad: se cubre y se asoma a disparar.</summary>
        Hold,
        /// <summary>Guardia: avanza de cobertura en cobertura hacia el jugador.</summary>
        Advance,
        /// <summary>Élite: si no consigue tiro, te rodea buscando un ángulo distinto.</summary>
        Flank,
    }

    /// <summary>Para qué busca cobertura el robot (cambia la puntuación).</summary>
    public enum CoverIntent { Normal, Advance, Flank }

    public static class TacticsCatalog
    {
        public static TacticStyle StyleFor(string robotName)
        {
            string n = (robotName ?? "").ToLowerInvariant();
            if (n.Contains("elite") || n.Contains("élite")) return TacticStyle.Flank;
            if (n.Contains("guard")) return TacticStyle.Advance;
            return TacticStyle.Hold;
        }
    }

    /// <summary>
    /// Puntuación de coberturas (pura, probada; diseño ia-combate-design.md). Entre los puntos a menos de 15 m del robot:
    ///   +50 si protege del jugador · +30 si está a 6–14 m del jugador (−3 por metro fuera) · −1 por metro de recorrido ·
    ///   +25 al flanquear si cambia el ángulo 60° o más · +20 al avanzar si queda 2 m o más cerca del jugador ·
    ///   descartado (−1000) si otro robot lo tiene reservado, está a menos de 3 m del jugador o a más de 15 m del robot.
    /// </summary>
    public static class CoverScoring
    {
        public const float Protects = 50f, IdealBonus = 30f, IdealMin = 6f, IdealMax = 14f, OutOfRangePerMeter = 3f;
        public const float TravelPerMeter = 1f, FlankBonus = 25f, FlankAngle = 60f, AdvanceBonus = 20f, AdvanceGain = 2f;
        public const float TooClose = 3f, SearchRadius = 15f, Rejected = -1000f;

        public static float Score(float robotX, float robotZ, float playerX, float playerZ, CoverSpot spot,
                                  bool protects, bool reservedByOther, CoverIntent intent)
        {
            float toPlayer = Dist(spot.X, spot.Z, playerX, playerZ);
            float travel = Dist(robotX, robotZ, spot.X, spot.Z);
            if (reservedByOther || toPlayer < TooClose || travel > SearchRadius) return Rejected;

            float s = protects ? Protects : 0f;
            if (toPlayer < IdealMin) s += IdealBonus - (IdealMin - toPlayer) * OutOfRangePerMeter;
            else if (toPlayer > IdealMax) s += IdealBonus - (toPlayer - IdealMax) * OutOfRangePerMeter;
            else s += IdealBonus;
            s -= travel * TravelPerMeter;

            if (intent == CoverIntent.Flank && AngleFromPlayer(playerX, playerZ, robotX, robotZ, spot.X, spot.Z) >= FlankAngle) s += FlankBonus;
            if (intent == CoverIntent.Advance && Dist(robotX, robotZ, playerX, playerZ) - toPlayer >= AdvanceGain) s += AdvanceBonus;
            return s;
        }

        /// <summary>Ángulo (grados) entre «jugador→robot» y «jugador→punto»: cuánto cambia la dirección del ataque.</summary>
        public static float AngleFromPlayer(float px, float pz, float rx, float rz, float sx, float sz)
        {
            double a = Math.Atan2(rx - px, rz - pz), b = Math.Atan2(sx - px, sz - pz);
            double d = Math.Abs(a - b) * 180.0 / Math.PI;
            if (d > 180.0) d = 360.0 - d;
            return (float)d;
        }

        /// <summary>Índice del mejor punto (o −1 si ninguno vale). 'protects' y 'reserved' van en paralelo a 'spots'.</summary>
        public static int Best(float robotX, float robotZ, float playerX, float playerZ, IList<CoverSpot> spots,
                               IList<bool> protects, IList<bool> reserved, CoverIntent intent, int excludeId = -1)
        {
            int best = -1;
            float bestScore = 0f;   // un punto sin protección y lejos del rango ideal no compensa: mejor al descubierto
            for (int i = 0; i < spots.Count; i++)
            {
                if (spots[i].Id == excludeId) continue;
                float s = Score(robotX, robotZ, playerX, playerZ, spots[i], protects[i], reserved[i], intent);
                if (s > bestScore) { bestScore = s; best = i; }
            }
            return best;
        }

        public static float Dist(float ax, float az, float bx, float bz)
        {
            float dx = ax - bx, dz = az - bz;
            return (float)Math.Sqrt(dx * dx + dz * dz);
        }
    }

    /// <summary>Reservas de cobertura: cada punto es de un solo robot. Se liberan al irse, al morir o al reiniciar.</summary>
    public sealed class CoverReservations
    {
        private readonly Dictionary<int, int> owner = new Dictionary<int, int>();

        public bool IsReservedByOther(int spotId, int robotId) => owner.TryGetValue(spotId, out int o) && o != robotId;

        public bool TryReserve(int spotId, int robotId)
        {
            if (IsReservedByOther(spotId, robotId)) return false;
            ReleaseAll(robotId);
            owner[spotId] = robotId;
            return true;
        }

        public void ReleaseAll(int robotId)
        {
            int found = -1;
            foreach (var kv in owner) if (kv.Value == robotId) { found = kv.Key; break; }
            if (found >= 0) owner.Remove(found);
        }

        public int Count => owner.Count;
        public void Clear() => owner.Clear();
    }

    public enum TacticPhase { Open, Moving, InCover, Peeking }

    public enum TacticOrder { None, SeekCover, GoToPeek, BackToCover }

    /// <summary>Lo que ve el robot en este fotograma (lo rellena EnemyTactics).</summary>
    public struct TacticInput
    {
        public bool Arrived;
        /// <summary>La cobertura actual sigue tapándolo del jugador.</summary>
        public bool CoverProtects;
        public bool BurstActive;
        /// <summary>Ha terminado una ráfaga desde el fotograma anterior.</summary>
        public bool BurstFinished;
        /// <summary>Hay turno de ataque libre (o ya es suyo).</summary>
        public bool TokenAvailable;
        /// <summary>Multiplicador de los tiempos (el director: &lt; 1 = más agresivo).</summary>
        public float Tempo;
        /// <summary>Aleatorio en [0, 1).</summary>
        public double Roll;
    }

    /// <summary>
    /// Máquina de estados del combate táctico (pura, probada):
    ///   AL DESCUBIERTO (dispara, busca cobertura cada 1,5 s) → MOVIÉNDOSE (no dispara) → A CUBIERTO (espera 0,6–1,2 s y un
    ///   turno libre) → ASOMADO (puede disparar; vuelve a cubrirse al acabar la ráfaga o a los 2,5 s sin disparar).
    ///   · Seguridad (Hold): repite cubrirse/asomarse; tras 2 asomadas sin tiro cambia de cobertura.
    ///   · Guardia (Advance): cada 5 s busca una cobertura más cerca del jugador.
    ///   · Élite (Flank): cada 4–6 s, o tras 1 asomada sin tiro, busca un ángulo distinto (flanquear).
    /// Todos los tiempos se multiplican por 'Tempo' (el director).
    /// </summary>
    public sealed class TacticsModel
    {
        public const float RetryCover = 1.5f, MoveTimeout = 6f, WaitMin = 0.6f, WaitMax = 1.2f, PeekMax = 2.5f;
        public const float AdvanceEvery = 5f, FlankMin = 4f, FlankMax = 6f;

        public TacticStyle Style { get; }
        public TacticPhase Phase { get; private set; } = TacticPhase.Open;
        public CoverIntent PendingIntent { get; private set; } = CoverIntent.Normal;
        public int FailedPeeks { get; private set; }
        /// <summary>Puede disparar ahora (al descubierto o asomado).</summary>
        public bool MayFire => Phase == TacticPhase.Open || Phase == TacticPhase.Peeking;

        private float phaseEnd, nextStyleMove = float.MaxValue, nextSeek;

        public TacticsModel(TacticStyle style) { Style = style; }

        /// <summary>Entra en combate (o vuelve tras perderlo de vista): empieza al descubierto y busca cobertura ya.</summary>
        public void Engage(float time)
        {
            Phase = TacticPhase.Open;
            nextSeek = time;
            FailedPeeks = 0;
            PendingIntent = CoverIntent.Normal;
        }

        /// <summary>No encontró cobertura válida: pelea al descubierto y vuelve a intentarlo en 1,5 s.</summary>
        public void NoCoverFound(float time, float tempo)
        {
            Phase = TacticPhase.Open;
            nextSeek = time + RetryCover * Tempo(tempo);
        }

        /// <summary>Ya tiene punto reservado y va hacia él.</summary>
        public void CoverAssigned(float time)
        {
            Phase = TacticPhase.Moving;
            phaseEnd = time + MoveTimeout;
        }

        public TacticOrder Tick(float time, TacticInput input)
        {
            float k = Tempo(input.Tempo);
            switch (Phase)
            {
                case TacticPhase.Open:
                    if (time < nextSeek || input.BurstActive) return TacticOrder.None;
                    return Seek(CoverIntent.Normal);

                case TacticPhase.Moving:
                    if (input.Arrived) { EnterCover(time, k, input.Roll); return TacticOrder.None; }
                    if (time >= phaseEnd) return Seek(CoverIntent.Normal);   // atascado
                    return TacticOrder.None;

                case TacticPhase.InCover:
                    if (!input.CoverProtects) return Seek(CoverIntent.Normal);   // el jugador lo ha rodeado
                    if (time >= nextStyleMove)
                        return Seek(Style == TacticStyle.Advance ? CoverIntent.Advance : CoverIntent.Flank);
                    if (time < phaseEnd || !input.TokenAvailable) return TacticOrder.None;
                    Phase = TacticPhase.Peeking;
                    phaseEnd = time + PeekMax;
                    return TacticOrder.GoToPeek;

                case TacticPhase.Peeking:
                    if (input.BurstFinished)
                    {
                        FailedPeeks = 0;
                        EnterCover(time, k, input.Roll);
                        return TacticOrder.BackToCover;
                    }
                    if (input.BurstActive || time < phaseEnd) return TacticOrder.None;
                    FailedPeeks++;
                    int limit = Style == TacticStyle.Flank ? 1 : 2;
                    if (FailedPeeks >= limit)
                        return Seek(Style == TacticStyle.Flank ? CoverIntent.Flank : Style == TacticStyle.Advance ? CoverIntent.Advance : CoverIntent.Normal);
                    EnterCover(time, k, input.Roll);
                    return TacticOrder.BackToCover;
            }
            return TacticOrder.None;
        }

        private TacticOrder Seek(CoverIntent intent)
        {
            PendingIntent = intent;
            FailedPeeks = 0;
            return TacticOrder.SeekCover;
        }

        private void EnterCover(float time, float k, double roll)
        {
            bool fromMove = Phase == TacticPhase.Moving;
            Phase = TacticPhase.InCover;
            phaseEnd = time + (WaitMin + (WaitMax - WaitMin) * (float)Clamp01(roll)) * k;
            if (!fromMove) return;
            // El reloj del estilo se reinicia cada vez que llega a una cobertura nueva
            if (Style == TacticStyle.Advance) nextStyleMove = time + AdvanceEvery * k;
            else if (Style == TacticStyle.Flank) nextStyleMove = time + (FlankMin + (FlankMax - FlankMin) * (float)Clamp01(roll)) * k;
            else nextStyleMove = float.MaxValue;
        }

        private static float Tempo(float t) => t <= 0f ? 1f : t;
        private static double Clamp01(double v) => v < 0 ? 0 : v > 1 ? 1 : v;
    }
}
