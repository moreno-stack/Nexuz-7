using System;

namespace Nexus7.Combat
{
    /// <summary>Arma a distancia de un tipo de robot: ráfagas con aviso previo.</summary>
    public sealed class EnemyGunSpec
    {
        public string DisplayName;
        /// <summary>Datos del proyectil (daño por impacto, velocidad, alcance, color). Usa el mismo pool que el jugador.</summary>
        public PlayerWeaponSpec Projectile;
        public int BurstCount;
        /// <summary>Disparos por segundo dentro de la ráfaga.</summary>
        public float BurstRate;
        /// <summary>Pausa entre ráfagas (aleatoria entre mínimo y máximo).</summary>
        public float PauseMin, PauseMax;
        /// <summary>Distancias a las que dispara: más cerca usa el cuerpo a cuerpo, más lejos no se molesta.</summary>
        public float MinRange, MaxRange;
        public float NoiseRadius;
    }

    /// <summary>
    /// Armas de los robots (diseño de docs/superpowers/specs/2026-11-11-ia-combate-design.md):
    ///   Seguridad → pistola de energía (ráfagas de 3) · Guardia → carabina (4) · Élite → rifle de pulsos (5, pausas cortas).
    ///   Investigación → sin arma: sigue con el cuerpo a cuerpo y avisando al escuadrón.
    /// Daño por impacto en Normal 4–5 (lo multiplican la dificultad y el director). Proyectil a 35 m/s: se puede esquivar.
    /// </summary>
    public static class EnemyGunCatalog
    {
        public static readonly EnemyGunSpec Security = Make("Pistola de energía", 4f, 3, 5f, 1.2f, 2.0f);
        public static readonly EnemyGunSpec Guard = Make("Carabina de energía", 4f, 4, 6f, 1.2f, 2.0f);
        public static readonly EnemyGunSpec Elite = Make("Rifle de pulsos", 5f, 5, 7f, 0.9f, 1.5f);

        private static EnemyGunSpec Make(string name, float damage, int burst, float rate, float pauseMin, float pauseMax) => new EnemyGunSpec
        {
            DisplayName = name, BurstCount = burst, BurstRate = rate, PauseMin = pauseMin, PauseMax = pauseMax,
            MinRange = 3.5f, MaxRange = 24f, NoiseRadius = 12f,
            Projectile = new PlayerWeaponSpec
            {
                Id = "robot_" + name.ToLowerInvariant().Replace(' ', '_'), DisplayName = name, Mode = FireMode.FullAuto,
                Damage = damage, Pellets = 1, FireRate = rate, MagazineSize = 999, ProjectileSpeed = 35f, Range = 40f,
                FalloffStart = 40f, MinDamageFactor = 1f, ColorR = 1f, ColorG = 0.35f, ColorB = 0.15f,
            },
        };

        /// <summary>Arma según el nombre del robot en la escena (RobotSeguridad_A, RobotGuardia_X, RobotElite_Y...). Null = sin arma.</summary>
        public static EnemyGunSpec ForRobot(string robotName)
        {
            string n = (robotName ?? "").ToLowerInvariant();
            if (n.Contains("investig") || n.Contains("research") || n.Contains("camara") || n.Contains("cámara")) return null;
            if (n.Contains("elite") || n.Contains("élite")) return Elite;
            if (n.Contains("guard")) return Guard;
            if (n.Contains("segur") || n.Contains("security")) return Security;
            return null;
        }
    }

    /// <summary>
    /// Puntería de los robots (pura, probada): dispersión en grados según la distancia, cómo se mueve el jugador,
    /// si el robot está quieto, si está cegado y la dificultad; y anticipación del 50 % del movimiento del jugador.
    /// </summary>
    public static class AimModel
    {
        public const float Base = 1.5f, PerMeter = 0.15f, FreeDistance = 8f;
        public const float Running = 2f, Walking = 1f, RobotStill = -0.5f, Blinded = 6f;
        public const float LeadFactor = 0.5f;

        /// <param name="playerSpeed">m/s del jugador (≥ 4 corre, ≥ 0,5 camina).</param>
        /// <param name="difficulty">0 Fácil (×1,5), 1 Normal, 2 Difícil (×0,6).</param>
        public static float Spread(float distance, float playerSpeed, bool robotMoving, bool blinded, int difficulty, float directorMultiplier = 1f)
        {
            float s = Base + Math.Max(0f, distance - FreeDistance) * PerMeter;
            if (playerSpeed >= 4f) s += Running;
            else if (playerSpeed >= 0.5f) s += Walking;
            if (!robotMoving) s += RobotStill;
            if (blinded) s += Blinded;
            s *= difficulty <= 0 ? 1.5f : difficulty >= 2 ? 0.6f : 1f;
            return Math.Max(0.2f, s * Math.Max(0.1f, directorMultiplier));
        }

        /// <summary>Punto al que apunta: la posición más el 50 % de lo que se moverá mientras llega el proyectil.</summary>
        public static void Lead(float px, float py, float pz, float vx, float vy, float vz, float distance, float projectileSpeed,
                                out float ax, out float ay, out float az)
        {
            float t = projectileSpeed > 0f ? distance / projectileSpeed : 0f;
            ax = px + vx * t * LeadFactor;
            ay = py + vy * t * LeadFactor;
            az = pz + vz * t * LeadFactor;
        }
    }
}
