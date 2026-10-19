using System;
using UnityEngine;

namespace Nexus7.Core
{
    public enum AlertLevel { Normal, Yellow, Red }

    /// <summary>Sucesos que suben la tensión de la nave (cada uno con su peso en AlertSystem.Weight).</summary>
    public enum AlertEvent { AlarmRaised, PlayerSpotted, PlayerCaught, HackFailed, HackError, LoudNoise, DeviceDetonation, PlayerHit, Custom }

    /// <summary>
    /// Modificadores que aplica cada nivel a TODOS los sistemas hostiles.
    /// Los consultan EnemyVision, EnemyPatrol, SecurityCamera, HackTerminal y SecuritySector.
    /// </summary>
    public struct AlertProfile
    {
        public float detection;      // velocidad de detección de drones y cámaras
        public float viewDistance;   // alcance de visión
        public float hearing;        // radio de audición
        public float speed;          // velocidad de movimiento de los drones
        public float sweepSpeed;     // velocidad de barrido de las cámaras
        public float hackDuration;   // cuánto dura un sistema hackeado/desactivado (menos en alerta)

        public AlertProfile(float detection, float view, float hearing, float speed, float sweep, float hack)
        {
            this.detection = detection; viewDistance = view; this.hearing = hearing;
            this.speed = speed; sweepSpeed = sweep; hackDuration = hack;
        }
    }

    /// <summary>Historial de incidentes de la partida (se consulta en las consolas del Centro de Control).</summary>
    public class AlertStats
    {
        public int alarms, spotted, captures, hackFailures, hackErrors, deviceDetonations;
        public float loudNoise;
        public float peakHeat;
        public int redAlerts;
    }

    /// <summary>
    /// SISTEMA DE ALERTAS DINÁMICO (global, estático: persiste entre escenas durante toda la partida).
    ///
    ///   Sucesos (Report) ──► CALOR 0..100 ──► NIVEL con histéresis:
    ///        Normal  ──(≥ 35)──► Amarilla ──(≥ 70)──► Roja
    ///        Normal ◄──(< 20)── Amarilla ◄──(< 50 y ≥ 25 s en Roja)── Roja
    ///   El calor se disipa con el tiempo (más despacio cuanto más alta es la alerta, y se pausa 8 s tras cada suceso).
    ///
    /// Lo que hiciste en fases anteriores CUENTA: alarmas de cámaras/láseres/bancos, detecciones, capturas,
    /// carreras ruidosas, hackeos fallidos... todo suma desde la Fase 1 (lo conecta AlertEventBridge).
    ///
    /// API: Report(evento[, posición]) · ReportCustom · AddHeat · ReduceHeat · Level · Heat · Profile · Stats · LevelChanged
    /// </summary>
    public static class AlertSystem
    {
        // ---------- Configuración ----------
        public const float YellowThreshold = 35f, RedThreshold = 70f;
        public const float YellowExit = 20f, RedExit = 50f, RedMinDuration = 25f;
        public const float DecayPause = 8f;

        private static readonly AlertProfile[] Profiles =
        {
            new AlertProfile(1f,    1f,    1f,   1f,    1f,   1f),     // Normal
            new AlertProfile(1.25f, 1.15f, 1.2f, 1.15f, 1.3f, 0.8f),   // Amarilla
            new AlertProfile(1.6f,  1.3f,  1.5f, 1.3f,  1.7f, 0.5f),   // Roja
        };

        /// <summary>Calor que suma cada suceso.</summary>
        public static float Weight(AlertEvent e)
        {
            switch (e)
            {
                case AlertEvent.AlarmRaised:      return 22f;
                case AlertEvent.PlayerSpotted:    return 15f;
                case AlertEvent.PlayerCaught:     return 10f;
                case AlertEvent.HackFailed:       return 18f;
                case AlertEvent.HackError:        return 5f;
                case AlertEvent.LoudNoise:        return 0.35f;  // por cada paso corriendo
                case AlertEvent.DeviceDetonation: return 4f;
                case AlertEvent.PlayerHit:        return 6f;   // un robot te ha golpeado
                default:                          return 0f;
            }
        }

        private static float DecayPerSecond(AlertLevel l) => l == AlertLevel.Red ? 0.5f : l == AlertLevel.Yellow ? 0.8f : 1.2f;

        // ---------- Estado ----------
        public static float Heat { get; private set; }
        public static AlertLevel Level { get; private set; }
        public static AlertStats Stats { get; private set; } = new AlertStats();
        public static AlertProfile Profile => Profiles[(int)Level];

        /// <summary>Última posición conocida del jugador (la comparten todos los sistemas de seguridad).</summary>
        public static Vector3 LastKnownPlayerPosition { get; private set; }
        public static float LastKnownTime { get; private set; } = -999f;
        public static bool HasRecentSighting(float maxAge) => Time.time - LastKnownTime <= maxAge;

        /// <summary>(nivel anterior, nivel nuevo)</summary>
        public static event Action<AlertLevel, AlertLevel> LevelChanged;
        /// <summary>(suceso, calor añadido)</summary>
        public static event Action<AlertEvent, float> EventReported;

        private static float lastEventTime = -999f;
        private static float redSince;

        // ---------- API ----------
        public static void Report(AlertEvent e) => ReportInternal(e, false, default(Vector3), 0f);
        public static void Report(AlertEvent e, Vector3 position) => ReportInternal(e, true, position, 0f);
        /// <summary>Suceso propio con calor a medida (AlertEvent.Custom).</summary>
        public static void ReportCustom(float heat) => ReportInternal(AlertEvent.Custom, false, default(Vector3), heat);

        private static void ReportInternal(AlertEvent e, bool hasPosition, Vector3 position, float customHeat)
        {
            float heat = e == AlertEvent.Custom ? customHeat : Weight(e);
            switch (e)
            {
                case AlertEvent.AlarmRaised:      Stats.alarms++; break;
                case AlertEvent.PlayerSpotted:    Stats.spotted++; break;
                case AlertEvent.PlayerCaught:     Stats.captures++; break;
                case AlertEvent.HackFailed:       Stats.hackFailures++; break;
                case AlertEvent.HackError:        Stats.hackErrors++; break;
                case AlertEvent.LoudNoise:        Stats.loudNoise += heat; break;
                case AlertEvent.DeviceDetonation: Stats.deviceDetonations++; break;
            }
            if (hasPosition && (e == AlertEvent.AlarmRaised || e == AlertEvent.PlayerSpotted))
                ReportPlayerPosition(position);

            // El ruido de correr no detiene la disipación (si no, correr sin parar llevaría sola la nave a la Roja).
            if (e != AlertEvent.LoudNoise) lastEventTime = Time.time;
            AddHeat(heat);
            EventReported?.Invoke(e, heat);
        }

        /// <summary>Olvida la última posición conocida (al cambiar de escena las coordenadas ya no valen).</summary>
        public static void ForgetPlayerPosition() => LastKnownTime = -999f;

        public static void ReportPlayerPosition(Vector3 position)
        {
            LastKnownPlayerPosition = position;
            LastKnownTime = Time.time;
        }

        public static void AddHeat(float amount)
        {
            Heat = Mathf.Clamp(Heat + amount, 0f, 100f);
            Stats.peakHeat = Mathf.Max(Stats.peakHeat, Heat);
            Evaluate();
        }

        /// <summary>Baja el calor (p. ej. "reiniciar protocolo de alerta" desde una consola). Ignora el mínimo de la Roja.</summary>
        public static void ReduceHeat(float amount)
        {
            Heat = Mathf.Clamp(Heat - amount, 0f, 100f);
            redSince = -999f;
            Evaluate();
        }

        /// <summary>Lo llama AlertEventBridge cada frame.</summary>
        public static void Tick(float deltaTime)
        {
            if (Time.time - lastEventTime < DecayPause || Heat <= 0f) return;
            Heat = Mathf.Max(0f, Heat - DecayPerSecond(Level) * deltaTime);
            Evaluate();
        }

        /// <summary>Fuerza un nivel concreto (cinemáticas, pruebas, final de fase).</summary>
        public static void ForceLevel(AlertLevel level)
        {
            Heat = level == AlertLevel.Red ? RedThreshold : level == AlertLevel.Yellow ? YellowThreshold : 0f;
            redSince = -999f;
            SetLevel(level);
        }

        public static void ResetAll()
        {
            Heat = 0f;
            Level = AlertLevel.Normal;
            Stats = new AlertStats();
            lastEventTime = -999f;
            redSince = -999f;
            LastKnownTime = -999f;
        }

        // ---------- Histéresis ----------
        private static void Evaluate()
        {
            AlertLevel next = Level;
            switch (Level)
            {
                case AlertLevel.Normal:
                    if (Heat >= RedThreshold) next = AlertLevel.Red;
                    else if (Heat >= YellowThreshold) next = AlertLevel.Yellow;
                    break;
                case AlertLevel.Yellow:
                    if (Heat >= RedThreshold) next = AlertLevel.Red;
                    else if (Heat < YellowExit) next = AlertLevel.Normal;
                    break;
                case AlertLevel.Red:
                    if (Heat < RedExit && Time.time - redSince >= RedMinDuration)
                        next = Heat < YellowExit ? AlertLevel.Normal : AlertLevel.Yellow;
                    break;
            }
            SetLevel(next);
        }

        private static void SetLevel(AlertLevel next)
        {
            if (next == Level) return;
            var old = Level;
            Level = next;
            if (next == AlertLevel.Red) { redSince = Time.time; Stats.redAlerts++; }
            LevelChanged?.Invoke(old, next);
        }

        // Si se desactiva "Domain Reload", los estáticos no se reinician solos al pulsar Play.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            ResetAll();
            LevelChanged = null;
            EventReported = null;
        }
    }
}
