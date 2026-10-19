using Nexus7.Characters;

namespace Nexus7.Enemies
{
    /// <summary>Valores de un arma de robot (daño base = dificultad Fácil).</summary>
    public struct WeaponSpec
    {
        public string name;
        public WeaponShape shape;
        public float damage, windup, range, recovery, stun, arc;
    }

    /// <summary>
    /// Armas de los robots según el diseño (§2.2). Los constructores de escena crean los WeaponData con estos valores.
    /// </summary>
    public static class WeaponCatalog
    {
        public static WeaponSpec Get(WeaponShape shape)
        {
            switch (shape)
            {
                case WeaponShape.Wrench: return Spec("Llave inglesa gigante", shape, 25f, 0.6f, 2.0f, 1.3f, 0f);
                case WeaponShape.Baton:  return Spec("Porra eléctrica", shape, 15f, 0.45f, 1.8f, 1.0f, 1.0f);
                case WeaponShape.Axe:    return Spec("Hacha", shape, 40f, 0.7f, 2.2f, 1.5f, 0f);
                default:                 return Spec("Bate metálico", WeaponShape.Bat, 25f, 0.55f, 1.9f, 1.2f, 0f);
            }
        }

        /// <summary>Arma que lleva cada tipo de robot.</summary>
        public static WeaponShape ForRobot(RobotStyle style)
        {
            switch (style)
            {
                case RobotStyle.Guard:    return WeaponShape.Wrench;
                case RobotStyle.Research: return WeaponShape.Baton;
                case RobotStyle.Elite:    return WeaponShape.Axe;
                default:                  return WeaponShape.Bat;
            }
        }

        private static WeaponSpec Spec(string n, WeaponShape s, float dmg, float wind, float rng, float rec, float stun) =>
            new WeaponSpec { name = n, shape = s, damage = dmg, windup = wind, range = rng, recovery = rec, stun = stun, arc = 70f };
    }
}
