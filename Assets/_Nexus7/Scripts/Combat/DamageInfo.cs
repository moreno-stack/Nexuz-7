using UnityEngine;

namespace Nexus7.Combat
{
    /// <summary>
    /// Un impacto: cuánto daño, dónde, desde dónde y con qué arma.
    /// Lo crea el proyectil (PlasmaBolt) y lo recibe cualquier IDamageable (robots, cámaras...).
    /// </summary>
    public struct DamageInfo
    {
        public float Amount;
        public Vector3 Point;
        public Vector3 Normal;
        public Vector3 Direction;
        /// <summary>Posición del tirador en el momento del disparo (los robots van a investigarla).</summary>
        public Vector3 Origin;
        /// <summary>Lo rellena el receptor: el impacto fue en un punto débil (cabeza / óptica).</summary>
        public bool Critical;
        /// <summary>Lo rellena el receptor: zona alcanzada (cuerpo, óptica o núcleo trasero de la élite).</summary>
        public HitZone Zone;
        public GameObject Source;
        /// <summary>Id del arma (PlayerWeaponCatalog) que disparó.</summary>
        public string WeaponId;
        /// <summary>Número del disparo (un disparo de escopeta tiene varios perdigones con el mismo número).</summary>
        public int ShotId;
    }

    /// <summary>Algo que puede recibir disparos.</summary>
    public interface IDamageable
    {
        bool IsAlive { get; }
        void TakeDamage(DamageInfo info);
    }
}
