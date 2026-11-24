using System;

namespace Nexus7.Combat
{
    /// <summary>
    /// Bus de eventos del combate. Desacopla las armas del resto del juego: el HUD, el director adaptativo
    /// y las estadísticas escuchan aquí sin que PlayerWeapon ni EnemyHealth sepan que existen.
    /// </summary>
    public static class CombatEvents
    {
        /// <summary>El jugador ha disparado (número de disparo, id del arma, proyectiles del disparo).</summary>
        public static event Action<int, string, int> ShotFired;
        /// <summary>Un proyectil ha impactado a un objetivo (información del impacto, ¿lo ha destruido?).</summary>
        public static event Action<DamageInfo, bool> HitLanded;
        /// <summary>Un enemigo ha sido destruido.</summary>
        public static event Action<EnemyHealth> EnemyKilled;
        /// <summary>A un robot le han destruido el brazo del arma.</summary>
        public static event Action<EnemyHealth> EnemyDisarmed;

        public static void RaiseShot(int shotId, string weaponId, int pellets) => ShotFired?.Invoke(shotId, weaponId, pellets);
        public static void RaiseHit(DamageInfo info, bool killed) => HitLanded?.Invoke(info, killed);
        public static void RaiseKilled(EnemyHealth enemy) => EnemyKilled?.Invoke(enemy);
        public static void RaiseDisarmed(EnemyHealth enemy) => EnemyDisarmed?.Invoke(enemy);

        private static int nextShotId;
        public static int NextShotId() => ++nextShotId;
    }
}
