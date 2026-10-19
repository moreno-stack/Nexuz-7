using UnityEngine;
using Nexus7.Characters;

namespace Nexus7.Enemies
{
    /// <summary>
    /// Arma de un robot (bate, llave inglesa, porra eléctrica, hacha). El daño es el de dificultad Fácil;
    /// PlayerHealth lo multiplica por la dificultad elegida.
    /// Crear: Assets > Create > Nexus-7 > Arma de robot (o lo hacen los constructores de escena).
    /// </summary>
    [CreateAssetMenu(menuName = "Nexus-7/Arma de robot", fileName = "NuevaArma")]
    public class WeaponData : ScriptableObject
    {
        [SerializeField] private string displayName = "Bate metálico";
        [SerializeField] private WeaponShape shape = WeaponShape.Bat;
        [SerializeField] private float damage = 25f;
        [Tooltip("Segundos con el arma levantada antes del impacto (tiempo para esquivar).")]
        [SerializeField] private float windup = 0.55f;
        [SerializeField] private float range = 1.9f;
        [SerializeField] private float arcDegrees = 70f;
        [SerializeField] private float recovery = 1.2f;
        [Tooltip("Aturdimiento extra al impactar (porra eléctrica).")]
        [SerializeField] private float stunSeconds = 0f;

        public string DisplayName => displayName;
        public WeaponShape Shape => shape;
        public float Damage => damage;
        public float Windup => windup;
        public float Range => range;
        public float ArcDegrees => arcDegrees;
        public float Recovery => recovery;
        public float StunSeconds => stunSeconds;

        /// <summary>Valores del spec por forma (los usan los constructores).</summary>
        public void Configure(string name, WeaponShape s, float dmg, float wind, float rng, float rec, float stun)
        {
            displayName = name; shape = s; damage = dmg; windup = wind; range = rng; recovery = rec; stunSeconds = stun; arcDegrees = 70f;
        }
    }
}
