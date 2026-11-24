using System;
using System.Collections.Generic;

namespace Nexus7.Combat
{
    /// <summary>Lo que va a dar un encuentro despejado.</summary>
    public struct EncounterReward
    {
        /// <summary>Paquetes de munición (AmmoPickup).</summary>
        public float AmmoPacks;
        /// <summary>Integridad que se repara al momento (0 si no hace falta).</summary>
        public float Heal;
        /// <summary>+1 vida.</summary>
        public bool Life;
    }

    /// <summary>
    /// Recompensa al despejar un encuentro según lo que te falte (pura, probada en EncounterRewardTest):
    ///   · Munición: 3 paquetes si te queda menos del 30 %, 2 si menos del 60 %, 1 si no. +1 si ya tienes todas las vidas.
    ///   · Integridad: con menos del 50 %, reparación de emergencia de +40 (como un kit).
    ///   · Vida: +1 si no estás al máximo.
    /// El accesorio de arma lo sigue decidiendo ModRewardDirector.
    /// </summary>
    public static class EncounterRewardModel
    {
        public const float LowAmmo = 0.3f, MidAmmo = 0.6f, LowHealth = 0.5f, EmergencyHeal = 40f;

        /// <param name="ammoFraction">Munición total / máximo total de todas las armas, en [0, 1].</param>
        /// <param name="healthFraction">Integridad / máxima, en [0, 1].</param>
        public static EncounterReward Choose(float ammoFraction, float healthFraction, int lives, int maxLives)
        {
            var r = new EncounterReward
            {
                AmmoPacks = ammoFraction < LowAmmo ? 3f : ammoFraction < MidAmmo ? 2f : 1f,
                Heal = healthFraction < LowHealth ? EmergencyHeal : 0f,
                Life = lives < maxLives,
            };
            if (!r.Life) r.AmmoPacks += 1f;
            return r;
        }

        /// <summary>El mensaje de «Zona despejada» con lo que se da.</summary>
        public static string Message(EncounterReward r)
        {
            var parts = new List<string>();
            if (r.AmmoPacks > 0f) parts.Add($"{r.AmmoPacks:0} paquete{(r.AmmoPacks >= 2f ? "s" : "")} de munición");
            if (r.Heal > 0f) parts.Add($"reparación de emergencia +{r.Heal:0}");
            if (r.Life) parts.Add("+1 vida");
            return "Zona despejada. Recompensa: " + (parts.Count > 0 ? string.Join(", ", parts) : "ninguna") + ".";
        }

        /// <summary>Munición total / máxima de un arsenal (1 si no hay armas: no hace falta munición).</summary>
        public static float AmmoFraction(IEnumerable<WeaponModel> weapons)
        {
            int have = 0, max = 0;
            if (weapons != null)
                foreach (var w in weapons)
                {
                    if (w == null) continue;
                    have += w.Ammo + w.Reserve;
                    max += w.Spec.MagazineSize + w.Spec.MaxReserve;
                }
            return max > 0 ? Math.Min(1f, (float)have / max) : 1f;
        }
    }
}
