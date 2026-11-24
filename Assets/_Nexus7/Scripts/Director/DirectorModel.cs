using System;

namespace Nexus7.Director
{
    /// <summary>Perillas que el director mueve según la tensión (1 = valor de diseño).</summary>
    public struct DirectorKnobs
    {
        public float EnemySpeed;          // velocidad de patrulla/persecución
        public float Detection;           // rapidez con que te detectan
        public float EnemyDamage;         // daño de los golpes de los robots
        public float EnemyHealth;         // vida de los robots
        public float AmmoDrop;            // munición de los paquetes
        public float RespawnAmmoLoss;     // munición que se pierde al caer
        public float PatrolWait;          // espera en cada waypoint (menos = patrullas más activas)
        public int Reinforcements;        // robots extra que puede añadir al reiniciar un tramo
        public int ExtraShooters;         // robots extra que pueden disparar a la vez (turnos de ataque)
        public float EnemySpread;         // dispersión de los disparos de los robots (< 1 = mejor puntería)
        public float TacticTempo;         // tiempos de cobertura, avance y flanqueo (< 1 = más agresivos)
    }

    /// <summary>Lo que el director decide hacer además de mover perillas.</summary>
    public enum DirectorAction { None, FlushCamper, AdaptToWeapon, StopAdapting, Assist }

    /// <summary>
    /// Director adaptativo (lógica pura, probada). Mantiene una TENSIÓN en [0, 1] que persigue al rendimiento del jugador:
    ///   objetivo = base(dificultad) + rendimiento × Rango
    /// con zona muerta (no reacciona a cambios pequeños), subida lenta y bajada rápida (perdona antes de lo que castiga)
    /// y un «respiro» tras cada caída. De la tensión salen las perillas (DirectorKnobs) con interpolación lineal
    /// alrededor de 0,5 = valores de diseño.
    /// Además detecta patrones: acampar en una zona → manda una patrulla; abusar de un arma → los robots se adaptan a ella;
    /// caer varias veces seguidas → ayuda (más munición, menos tensión).
    /// </summary>
    public sealed class DirectorModel
    {
        // ---- Parámetros de balance (ver la guía) ----
        public float Range = 0.35f;
        public float DeadZone = 0.05f;
        public float RisePerSecond = 0.008f;
        public float FallPerSecond = 0.03f;
        public float MinTension = 0.1f, MaxTension = 0.95f;
        public float ReliefDuration = 20f;
        public float ReliefDrop = 0.15f;
        public float CampThreshold = 0.7f, CampCooldown = 45f;
        public float DominanceThreshold = 0.75f, DominanceRelease = 0.55f;
        public float AssistDeaths = 1.6f, AssistCooldown = 120f;

        public float Tension { get; private set; }
        /// <summary>El jugador está en su última vida: el objetivo de tensión no pasa de la base − LastLifeMargin.</summary>
        public bool LastLife { get; set; }
        public float LastLifeMargin = 0.1f;
        public float Baseline { get; private set; }
        public float Target { get; private set; }
        public bool InRelief => reliefLeft > 0f;
        public string AdaptedWeapon { get; private set; }
        public DirectorKnobs Knobs { get; private set; }

        private float reliefLeft;
        private float campCooldownLeft;
        private float assistCooldownLeft;

        /// <param name="difficulty">0 Fácil, 1 Normal, 2 Difícil (DifficultySettings).</param>
        public DirectorModel(int difficulty, float startTension = -1f)
        {
            Baseline = BaselineFor(difficulty);
            Tension = startTension >= 0f ? Clamp(startTension, MinTension, MaxTension) : Baseline;
            Target = Tension;
            Knobs = KnobsFor(Tension);
        }

        public static float BaselineFor(int difficulty) => difficulty <= 0 ? 0.38f : difficulty == 1 ? 0.5f : 0.62f;

        /// <summary>El jugador ha caído: respiro (la tensión baja de golpe y no sube durante ReliefDuration).</summary>
        public void OnPlayerDeath()
        {
            reliefLeft = ReliefDuration;
            Tension = Math.Max(MinTension, Tension - ReliefDrop);
            Knobs = KnobsFor(Tension);
        }

        /// <summary>
        /// Avanza 'dt' segundos con las métricas del analizador. Devuelve la acción especial que toque (o None).
        /// </summary>
        public DirectorAction Tick(float dt, PlayerPerformanceModel perf)
        {
            if (dt < 0f) dt = 0f;
            reliefLeft = Math.Max(0f, reliefLeft - dt);
            campCooldownLeft = Math.Max(0f, campCooldownLeft - dt);
            assistCooldownLeft = Math.Max(0f, assistCooldownLeft - dt);

            Target = Clamp(Baseline + perf.Performance * Range, MinTension, MaxTension);
            if (LastLife) Target = Math.Max(MinTension, Math.Min(Target, Baseline - LastLifeMargin));   // la última vida es más clemente
            float diff = Target - Tension;
            if (Math.Abs(diff) > DeadZone)
            {
                if (diff > 0f && !InRelief) Tension = Math.Min(Target, Tension + RisePerSecond * dt);
                else if (diff < 0f) Tension = Math.Max(Target, Tension - FallPerSecond * dt);
            }
            Knobs = KnobsFor(Tension);

            // ---- Patrones (una acción por tick, la más urgente primero) ----
            if (perf.RecentDeaths >= AssistDeaths && assistCooldownLeft <= 0f)
            {
                assistCooldownLeft = AssistCooldown;
                Tension = Math.Max(MinTension, Tension - ReliefDrop);
                Knobs = KnobsFor(Tension);
                return DirectorAction.Assist;
            }
            if (perf.CampingScore >= CampThreshold && campCooldownLeft <= 0f)
            {
                campCooldownLeft = CampCooldown;
                return DirectorAction.FlushCamper;
            }
            float dom = perf.WeaponDominance(out string weapon);
            if (AdaptedWeapon == null && dom >= DominanceThreshold && weapon != null)
            {
                AdaptedWeapon = weapon;
                return DirectorAction.AdaptToWeapon;
            }
            if (AdaptedWeapon != null && (dom < DominanceRelease || weapon != AdaptedWeapon))
            {
                AdaptedWeapon = null;
                return DirectorAction.StopAdapting;
            }
            return DirectorAction.None;
        }

        /// <summary>Daño que recibe un robot de 'weaponId' mientras el director está adaptado a un arma.</summary>
        public float WeaponDamageFactor(string weaponId) =>
            AdaptedWeapon == null ? 1f : weaponId == AdaptedWeapon ? 0.8f : 1.15f;

        /// <summary>Perillas para una tensión dada (0,5 = valores de diseño).</summary>
        public static DirectorKnobs KnobsFor(float tension)
        {
            float t = Clamp(tension, 0f, 1f);
            return new DirectorKnobs
            {
                EnemySpeed = Knob(t, 0.85f, 1.15f),
                Detection = Knob(t, 0.75f, 1.3f),
                EnemyDamage = Knob(t, 0.7f, 1.3f),
                EnemyHealth = Knob(t, 0.8f, 1.25f),
                AmmoDrop = Knob(t, 1.6f, 0.8f),
                RespawnAmmoLoss = Knob(t, 0.3f, 1.2f),
                PatrolWait = Knob(t, 1.3f, 0.7f),
                Reinforcements = t >= 0.8f ? 2 : t >= 0.6f ? 1 : 0,
                ExtraShooters = t >= 0.75f ? 1 : 0,
                EnemySpread = Knob(t, 1.35f, 0.75f),
                TacticTempo = Knob(t, 1.4f, 0.7f),
            };
        }

        /// <summary>Perilla en dos tramos: 'at0' con tensión 0, exactamente 1 con 0,5 y 'at1' con 1.</summary>
        public static float Knob(float t, float at0, float at1) =>
            t < 0.5f ? at0 + (1f - at0) * (t / 0.5f) : 1f + (at1 - 1f) * ((t - 0.5f) / 0.5f);
        private static float Clamp(float v, float a, float b) => v < a ? a : v > b ? b : v;
    }
}
