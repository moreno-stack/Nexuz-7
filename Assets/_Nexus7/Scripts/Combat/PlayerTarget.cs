using UnityEngine;
using Nexus7.Player;

namespace Nexus7.Combat
{
    /// <summary>
    /// Hace que los proyectiles de los robots puedan dar al jugador (IDamageable): pasa el daño a
    /// PlayerHealth.TakeRangedDamage, que aplica dificultad, director, escudo, empujón pequeño y el indicador de impacto.
    /// Los disparos del propio jugador lo ignoran.
    ///
    /// Colocar en: el jugador (lo añade CombatInstaller).
    /// </summary>
    [RequireComponent(typeof(PlayerHealth))]
    public class PlayerTarget : MonoBehaviour, IDamageable
    {
        private PlayerHealth health;

        private void Awake() => health = GetComponent<PlayerHealth>();

        public bool IsAlive => health != null && health.Current > 0f;

        public void TakeDamage(DamageInfo info) => health.TakeRangedDamage(info.Amount, info.Origin);
    }
}
