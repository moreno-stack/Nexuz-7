namespace Nexus7.Combat
{
    /// <summary>
    /// Brazo del arma de un robot tirador (puro, probado): 30 de vida propia. Al destruirlo, el robot suelta el arma
    /// y pasa al cuerpo a cuerpo. Solo desarma una vez.
    /// </summary>
    public sealed class WeaponArmModel
    {
        public const float MaxHealth = 30f;
        public float Health { get; private set; } = MaxHealth;
        public bool Disarmed { get; private set; }

        /// <summary>Daño al brazo. Devuelve true solo en el impacto que lo desarma.</summary>
        public bool Apply(float amount)
        {
            if (Disarmed || amount <= 0f) return false;
            Health -= amount;
            if (Health > 0f) return false;
            Health = 0f;
            Disarmed = true;
            return true;
        }
    }
}
