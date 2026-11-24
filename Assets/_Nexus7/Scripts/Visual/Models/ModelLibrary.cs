using System.Collections.Generic;
using Nexus7.Combat;

namespace Nexus7.Visual.Models
{
    /// <summary>
    /// Planos de todo lo que se lleva en la mano o se recoge: armas (con sus accesorios), objetos, dispositivos,
    /// munición y módulos de accesorio. Medidas en metros. Las armas miran a +Z con el origen en el cuerpo.
    /// </summary>
    public static partial class ModelLibrary
    {
        private static readonly MVec AlongZ = new MVec(90f, 0f, 0f);   // cilindro o cápsula tumbados hacia delante
        private static MVec V(float x, float y, float z) => new MVec(x, y, z);

        /// <summary>Puntos de montaje de los accesorios de un arma.</summary>
        private struct Mounts { public MVec Muzzle, Magazine, Under, Top; public float Bore; }

        // =====================================================================
        //  ARMAS
        // =====================================================================

        /// <summary>Modelo de un arma con sus accesorios. Un Id desconocido usa la pistola.</summary>
        public static ModelBlueprint Weapon(string id, MColor accent, IList<WeaponMod> mods)
        {
            var b = new ModelBlueprint("weapon_" + id, accent);
            Mounts m;
            if (id == PlayerWeaponCatalog.Rifle) m = Rifle(b);
            else if (id == PlayerWeaponCatalog.Shotgun) m = Shotgun(b);
            else if (id == PlayerWeaponCatalog.Railgun) m = Railgun(b);
            else m = Pistol(b);
            if (mods != null)
                foreach (var mod in mods) AddMod(b, m, mod);
            return b;
        }

        private static Mounts Pistol(ModelBlueprint b)
        {
            b.Add(ModelShape.Box, V(0f, 0.03f, 0.02f), V(0.046f, 0.045f, 0.22f), MaterialRole.Body);          // corredera
            b.Add(ModelShape.Box, V(0f, 0f, 0.01f), V(0.04f, 0.028f, 0.19f), MaterialRole.Metal);             // armazón
            b.Add(ModelShape.Cylinder, V(0f, 0.028f, 0.14f), V(0.022f, 0.04f, 0.022f), MaterialRole.Metal, AlongZ);   // boca
            b.Add(ModelShape.Box, V(0f, -0.06f, -0.055f), V(0.038f, 0.11f, 0.05f), MaterialRole.Grip, V(-15f, 0f, 0f)); // empuñadura
            b.Add(ModelShape.Box, V(0f, -0.028f, 0.01f), V(0.01f, 0.022f, 0.05f), MaterialRole.Metal);        // guardamonte
            b.Add(ModelShape.Box, V(0f, 0.057f, -0.07f), V(0.014f, 0.01f, 0.012f), MaterialRole.Metal);       // mira trasera
            b.Add(ModelShape.Box, V(0f, 0.057f, 0.115f), V(0.006f, 0.01f, 0.008f), MaterialRole.Glow);        // punto de mira
            b.Add(ModelShape.Box, V(0f, 0.044f, 0.02f), V(0.048f, 0.008f, 0.15f), MaterialRole.Accent);       // franja
            b.Add(ModelShape.Box, V(0f, -0.065f, -0.03f), V(0.04f, 0.03f, 0.008f), MaterialRole.Glow, V(-15f, 0f, 0f)); // célula
            return new Mounts { Muzzle = V(0f, 0.028f, 0.16f), Magazine = V(0f, -0.115f, -0.07f), Under = V(0f, -0.02f, 0.09f), Top = V(0f, 0.052f, 0.03f), Bore = 0.022f };
        }

        private static Mounts Shotgun(ModelBlueprint b)
        {
            b.Add(ModelShape.Box, V(0f, 0f, 0f), V(0.075f, 0.075f, 0.24f), MaterialRole.Body);                 // cajón de mecanismos
            b.Add(ModelShape.Cylinder, V(-0.018f, 0.022f, 0.2f), V(0.034f, 0.16f, 0.034f), MaterialRole.Metal, AlongZ);  // cañón izquierdo
            b.Add(ModelShape.Cylinder, V(0.018f, 0.022f, 0.2f), V(0.034f, 0.16f, 0.034f), MaterialRole.Metal, AlongZ);   // cañón derecho
            b.Add(ModelShape.Box, V(0f, -0.018f, 0.16f), V(0.07f, 0.045f, 0.1f), MaterialRole.Grip);           // guardamanos de bombeo
            b.Add(ModelShape.Box, V(0f, -0.02f, -0.155f), V(0.055f, 0.08f, 0.09f), MaterialRole.Grip, V(8f, 0f, 0f));   // culata corta
            b.Add(ModelShape.Box, V(0f, -0.07f, -0.07f), V(0.036f, 0.08f, 0.045f), MaterialRole.Grip, V(-18f, 0f, 0f)); // empuñadura
            b.Add(ModelShape.Box, V(0f, 0.039f, 0f), V(0.077f, 0.01f, 0.18f), MaterialRole.Accent);            // franja
            b.Add(ModelShape.Cylinder, V(0f, -0.01f, 0.03f), V(0.05f, 0.012f, 0.05f), MaterialRole.Glow, V(0f, 0f, 90f)); // indicador lateral
            return new Mounts { Muzzle = V(0f, 0.022f, 0.28f), Magazine = V(0f, -0.04f, 0.04f), Under = V(0f, -0.045f, 0.2f), Top = V(0f, 0.04f, -0.02f), Bore = 0.075f };
        }

        private static Mounts Rifle(ModelBlueprint b)
        {
            b.Add(ModelShape.Box, V(0f, 0f, 0f), V(0.058f, 0.068f, 0.28f), MaterialRole.Body);                 // cajón
            b.Add(ModelShape.Box, V(0f, 0.004f, 0.2f), V(0.052f, 0.052f, 0.14f), MaterialRole.Body);           // guardamanos
            b.Add(ModelShape.Cylinder, V(0f, 0.01f, 0.31f), V(0.02f, 0.08f, 0.02f), MaterialRole.Metal, AlongZ);   // cañón
            b.Add(ModelShape.Box, V(0f, -0.012f, -0.19f), V(0.048f, 0.07f, 0.1f), MaterialRole.Grip);          // culata
            b.Add(ModelShape.Box, V(0f, -0.075f, 0.05f), V(0.032f, 0.09f, 0.05f), MaterialRole.Metal, V(14f, 0f, 0f));  // cargador curvo
            b.Add(ModelShape.Box, V(0f, -0.065f, -0.075f), V(0.034f, 0.08f, 0.04f), MaterialRole.Grip, V(-15f, 0f, 0f)); // empuñadura
            b.Add(ModelShape.Cylinder, V(0f, 0.058f, 0f), V(0.034f, 0.12f, 0.034f), MaterialRole.Body, AlongZ);    // mira telescópica
            b.Add(ModelShape.Cylinder, V(0f, 0.058f, 0.061f), V(0.028f, 0.004f, 0.028f), MaterialRole.Glow, AlongZ); // lente
            b.Add(ModelShape.Box, V(0f, 0.002f, 0f), V(0.06f, 0.012f, 0.22f), MaterialRole.Accent);            // franja
            b.Add(ModelShape.Box, V(0f, 0.032f, 0.2f), V(0.012f, 0.006f, 0.12f), MaterialRole.Metal);          // riel superior
            return new Mounts { Muzzle = V(0f, 0.01f, 0.35f), Magazine = V(0f, -0.12f, 0.06f), Under = V(0f, -0.03f, 0.2f), Top = V(0f, 0.034f, -0.08f), Bore = 0.02f };
        }

        private static Mounts Railgun(ModelBlueprint b)
        {
            b.Add(ModelShape.Box, V(0f, 0f, 0f), V(0.068f, 0.07f, 0.3f), MaterialRole.Body);                   // cuerpo
            b.Add(ModelShape.Box, V(-0.026f, 0f, 0.3f), V(0.012f, 0.05f, 0.3f), MaterialRole.Metal);           // raíl izquierdo
            b.Add(ModelShape.Box, V(0.026f, 0f, 0.3f), V(0.012f, 0.05f, 0.3f), MaterialRole.Metal);            // raíl derecho
            for (int i = 0; i < 3; i++)
                b.Add(ModelShape.Cylinder, V(0f, 0f, 0.2f + 0.08f * i), V(0.075f, 0.022f, 0.075f), MaterialRole.Glow, AlongZ); // bobinas
            b.Add(ModelShape.Box, V(0f, 0.005f, -0.2f), V(0.058f, 0.058f, 0.1f), MaterialRole.Accent);         // batería trasera
            b.Add(ModelShape.Box, V(0f, -0.07f, -0.06f), V(0.036f, 0.085f, 0.045f), MaterialRole.Grip, V(-15f, 0f, 0f)); // empuñadura
            b.Add(ModelShape.Box, V(0f, 0.045f, 0.02f), V(0.03f, 0.02f, 0.16f), MaterialRole.Metal);           // carril de la mira
            b.Add(ModelShape.Box, V(0f, 0.062f, 0.02f), V(0.024f, 0.016f, 0.05f), MaterialRole.Glow);          // pantalla de carga
            return new Mounts { Muzzle = V(0f, 0f, 0.45f), Magazine = V(0f, -0.035f, 0.06f), Under = V(0f, -0.035f, 0.17f), Top = V(0f, 0.056f, -0.09f), Bore = 0.05f };
        }

        private static void AddMod(ModelBlueprint b, Mounts m, WeaponMod mod)
        {
            switch (mod)
            {
                case WeaponMod.Suppressor:
                    float d = m.Bore + 0.022f;
                    b.Add(ModelShape.Cylinder, m.Muzzle + V(0f, 0f, 0.055f), V(d, 0.11f, d), MaterialRole.Body, AlongZ);
                    b.Add(ModelShape.Cylinder, m.Muzzle + V(0f, 0f, 0.012f), V(d + 0.004f, 0.012f, d + 0.004f), MaterialRole.Accent, AlongZ);
                    break;
                case WeaponMod.ExtendedMag:
                    b.Add(ModelShape.Box, m.Magazine + V(0f, -0.03f, 0f), V(0.034f, 0.06f, 0.048f), MaterialRole.Metal);
                    b.Add(ModelShape.Box, m.Magazine + V(0f, -0.058f, 0f), V(0.036f, 0.01f, 0.05f), MaterialRole.Accent);
                    break;
                case WeaponMod.Stabilizer:
                    b.Add(ModelShape.Box, m.Under + V(0f, -0.004f, 0f), V(0.03f, 0.012f, 0.05f), MaterialRole.Metal);
                    b.Add(ModelShape.Cylinder, m.Under + V(0f, -0.045f, 0f), V(0.026f, 0.07f, 0.026f), MaterialRole.Grip);
                    break;
                case WeaponMod.Heatsink:
                    for (int i = -1; i <= 1; i++)
                        b.Add(ModelShape.Box, m.Top + V(0f, 0.016f, 0.035f * i), V(0.05f, 0.03f, 0.007f), MaterialRole.Metal);
                    b.Add(ModelShape.Box, m.Top + V(0f, 0.003f, 0f), V(0.04f, 0.006f, 0.08f), MaterialRole.Glow);
                    break;
            }
        }
    }
}
