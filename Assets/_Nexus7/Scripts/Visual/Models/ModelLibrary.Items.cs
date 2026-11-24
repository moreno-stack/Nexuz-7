using Nexus7.Combat;
using Nexus7.Devices;
using Nexus7.Inventory;

namespace Nexus7.Visual.Models
{
    public static partial class ModelLibrary
    {
        public static readonly MColor AmmoColor = new MColor(0.35f, 0.9f, 1f);
        public static readonly MColor ModColor = new MColor(0.75f, 0.45f, 1f);
        private static readonly MColor BoardGreen = new MColor(0.08f, 0.32f, 0.16f);
        private static readonly MColor CardWhite = new MColor(0.86f, 0.87f, 0.84f);
        private static readonly MColor CaseDark = new MColor(0.16f, 0.12f, 0.22f);
        private static readonly MColor KitWhite = new MColor(0.82f, 0.84f, 0.86f);

        // =====================================================================
        //  OBJETOS
        // =====================================================================

        /// <summary>¿Este Id tiene un modelo propio? (si no, se usa la forma genérica de su categoría).</summary>
        public static bool HasOwnModel(string id)
        {
            switch (id)
            {
                case "battery_ion": case "scrap_metal": case "component_circuit": case "component_optic_crystal":
                case "tool_hydraulic_wrench": case "key_director_card": case "key_reactor_module":
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>Modelo de un objeto del inventario, centrado en el origen. 'color' = color del objeto (ItemData.UiColor).</summary>
        public static ModelBlueprint Item(string id, ItemCategory category, MColor color)
        {
            if (!HasOwnModel(id)) return Generic(category, color);
            var b = new ModelBlueprint("item_" + id, color);
            switch (id)
            {
                case "battery_ion":             Battery(b); break;
                case "scrap_metal":             Scrap(b); break;
                case "component_circuit":       Circuit(b); break;
                case "component_optic_crystal": Crystal(b); break;
                case "tool_hydraulic_wrench":   Wrench(b); break;
                case "key_director_card":       Card(b); break;
                case "key_reactor_module":      ReactorModule(b); break;
            }
            return b;
        }

        private static ModelBlueprint Generic(ItemCategory category, MColor color)
        {
            var b = new ModelBlueprint("generic_" + category, color);
            switch (category)
            {
                case ItemCategory.Battery:   Battery(b); break;
                case ItemCategory.Component: Circuit(b); break;
                case ItemCategory.Device:
                    b.Add(ModelShape.Sphere, V(0f, 0f, 0f), V(0.12f, 0.12f, 0.12f), MaterialRole.Body);
                    b.Add(ModelShape.Cylinder, V(0f, 0f, 0f), V(0.15f, 0.014f, 0.15f), MaterialRole.Glow);
                    b.Add(ModelShape.Cylinder, V(0f, 0.065f, 0f), V(0.04f, 0.02f, 0.04f), MaterialRole.Metal);
                    break;
                case ItemCategory.Tool:
                    b.Add(ModelShape.Cylinder, V(0f, 0f, -0.04f), V(0.032f, 0.14f, 0.032f), MaterialRole.Grip, AlongZ);
                    b.Add(ModelShape.Box, V(0f, 0f, 0.07f), V(0.05f, 0.03f, 0.08f), MaterialRole.Metal);
                    b.Add(ModelShape.Cylinder, V(0f, 0f, 0.025f), V(0.036f, 0.016f, 0.036f), MaterialRole.Accent, AlongZ);
                    break;
                default:   // piezas
                    b.Add(ModelShape.Box, V(0f, 0f, 0f), V(0.2f, 0.06f, 0.14f), MaterialRole.Metal);
                    b.Add(ModelShape.Box, V(0f, 0.031f, 0f), V(0.16f, 0.004f, 0.03f), MaterialRole.Accent);
                    b.Add(ModelShape.Cylinder, V(0.06f, 0.035f, 0.04f), V(0.02f, 0.012f, 0.02f), MaterialRole.Body);
                    break;
            }
            return b;
        }

        private static void Battery(ModelBlueprint b)
        {
            b.Add(ModelShape.Cylinder, V(0f, 0f, 0f), V(0.09f, 0.15f, 0.09f), MaterialRole.Body);              // celda
            for (int i = -1; i <= 1; i++)
                b.Add(ModelShape.Cylinder, V(0f, 0.04f * i, 0f), V(0.094f, 0.014f, 0.094f), MaterialRole.Glow); // franjas de carga
            b.Add(ModelShape.Cylinder, V(0f, 0.085f, 0f), V(0.072f, 0.02f, 0.072f), MaterialRole.Metal);       // tapa
            b.Add(ModelShape.Cylinder, V(0f, 0.1f, 0f), V(0.026f, 0.016f, 0.026f), MaterialRole.Metal);        // borne
            b.Add(ModelShape.Cylinder, V(0f, -0.083f, 0f), V(0.084f, 0.016f, 0.084f), MaterialRole.Metal);     // base
        }

        private static void Scrap(ModelBlueprint b)
        {
            b.Add(ModelShape.Box, V(-0.03f, 0f, 0f), V(0.17f, 0.012f, 0.12f), MaterialRole.Metal, V(0f, 15f, 8f));     // placa doblada
            b.Add(ModelShape.Box, V(0.05f, 0.025f, 0.01f), V(0.1f, 0.01f, 0.09f), MaterialRole.Body, V(20f, -25f, -30f));
            b.Add(ModelShape.Box, V(0.0f, 0.05f, -0.03f), V(0.08f, 0.008f, 0.06f), MaterialRole.Accent, V(-35f, 40f, 10f));
            b.Add(ModelShape.Cylinder, V(-0.06f, 0.03f, 0.04f), V(0.018f, 0.05f, 0.018f), MaterialRole.Metal, V(0f, 0f, 70f)); // tornillo
            b.Add(ModelShape.Cylinder, V(-0.083f, 0.039f, 0.04f), V(0.03f, 0.008f, 0.03f), MaterialRole.Metal, V(0f, 0f, 70f));
        }

        private static void Circuit(ModelBlueprint b)
        {
            b.AddColored(ModelShape.Box, V(0f, 0f, 0f), V(0.2f, 0.012f, 0.14f), MaterialRole.Body, BoardGreen); // placa
            b.Add(ModelShape.Box, V(-0.04f, 0.014f, 0.02f), V(0.05f, 0.016f, 0.05f), MaterialRole.Grip);         // chips
            b.Add(ModelShape.Box, V(0.05f, 0.012f, -0.03f), V(0.035f, 0.012f, 0.035f), MaterialRole.Grip);
            b.Add(ModelShape.Box, V(0.055f, 0.01f, 0.04f), V(0.02f, 0.008f, 0.03f), MaterialRole.Grip);
            b.Add(ModelShape.Box, V(0.005f, 0.0075f, -0.02f), V(0.15f, 0.003f, 0.006f), MaterialRole.Glow);      // pistas
            b.Add(ModelShape.Box, V(0.01f, 0.0075f, 0.0f), V(0.006f, 0.003f, 0.1f), MaterialRole.Glow);
            b.Add(ModelShape.Box, V(-0.105f, 0f, 0f), V(0.012f, 0.008f, 0.1f), MaterialRole.Metal);              // conector
        }

        private static void Crystal(ModelBlueprint b)
        {
            b.Add(ModelShape.Cylinder, V(0f, -0.08f, 0f), V(0.1f, 0.025f, 0.1f), MaterialRole.Metal);            // base
            b.Add(ModelShape.Box, V(0f, 0.01f, 0f), V(0.05f, 0.17f, 0.05f), MaterialRole.Glow, V(0f, 45f, 0f));  // prisma
            b.Add(ModelShape.Box, V(0.035f, -0.03f, 0.01f), V(0.03f, 0.09f, 0.03f), MaterialRole.Glow, V(0f, 20f, -25f));
            b.Add(ModelShape.Cylinder, V(0f, -0.055f, 0f), V(0.07f, 0.02f, 0.07f), MaterialRole.Accent);         // abrazadera
        }

        private static void Wrench(ModelBlueprint b)
        {
            b.Add(ModelShape.Cylinder, V(0f, 0f, -0.07f), V(0.032f, 0.16f, 0.032f), MaterialRole.Grip, AlongZ);  // mango
            b.Add(ModelShape.Box, V(0f, 0f, 0.05f), V(0.03f, 0.02f, 0.1f), MaterialRole.Metal);                  // cuerpo
            b.Add(ModelShape.Box, V(-0.026f, 0f, 0.12f), V(0.014f, 0.02f, 0.05f), MaterialRole.Metal);           // boca abierta
            b.Add(ModelShape.Box, V(0.026f, 0f, 0.115f), V(0.014f, 0.02f, 0.04f), MaterialRole.Metal);
            b.Add(ModelShape.Box, V(0f, 0f, 0.098f), V(0.066f, 0.02f, 0.014f), MaterialRole.Metal);
            b.Add(ModelShape.Cylinder, V(0f, 0f, 0.03f), V(0.026f, 0.03f, 0.026f), MaterialRole.Accent, V(0f, 0f, 90f)); // rueda de ajuste
        }

        private static void Card(ModelBlueprint b)
        {
            b.AddColored(ModelShape.Box, V(0f, 0f, 0f), V(0.15f, 0.008f, 0.095f), MaterialRole.Body, CardWhite);
            b.Add(ModelShape.Box, V(0f, 0.0045f, -0.03f), V(0.15f, 0.002f, 0.018f), MaterialRole.Grip);          // banda magnética
            b.Add(ModelShape.Box, V(-0.04f, 0.005f, 0.015f), V(0.028f, 0.003f, 0.022f), MaterialRole.Accent);    // chip
            b.Add(ModelShape.Box, V(0.035f, 0.005f, 0.018f), V(0.05f, 0.002f, 0.03f), MaterialRole.Glow);        // holograma
        }

        private static void ReactorModule(ModelBlueprint b)
        {
            b.Add(ModelShape.Box, V(0f, 0f, 0f), V(0.18f, 0.11f, 0.14f), MaterialRole.Body);
            b.Add(ModelShape.Cylinder, V(0f, 0.06f, 0f), V(0.09f, 0.016f, 0.09f), MaterialRole.Metal);           // anillo
            b.Add(ModelShape.Sphere, V(0f, 0.065f, 0f), V(0.07f, 0.05f, 0.07f), MaterialRole.Glow);              // núcleo
            b.Add(ModelShape.Box, V(-0.1f, 0.02f, 0f), V(0.02f, 0.05f, 0.08f), MaterialRole.Metal);              // asas
            b.Add(ModelShape.Box, V(0.1f, 0.02f, 0f), V(0.02f, 0.05f, 0.08f), MaterialRole.Metal);
            b.Add(ModelShape.Box, V(0f, -0.02f, 0.071f), V(0.14f, 0.015f, 0.003f), MaterialRole.Accent);         // franjas
            b.Add(ModelShape.Box, V(0f, -0.02f, -0.071f), V(0.14f, 0.015f, 0.003f), MaterialRole.Accent);
        }

        // =====================================================================
        //  DISPOSITIVOS, MUNICIÓN Y ACCESORIOS SUELTOS
        // =====================================================================

        /// <summary>Modelo de un dispositivo fabricado (en la mano, en el suelo o lanzado). 'color' = DeviceColor.</summary>
        public static ModelBlueprint Device(DeviceKind kind, MColor color)
        {
            var b = new ModelBlueprint("device_" + kind, color);
            switch (kind)
            {
                case DeviceKind.SonicDecoy:
                    b.Add(ModelShape.Cylinder, V(0f, 0f, 0f), V(0.1f, 0.12f, 0.1f), MaterialRole.Body);
                    b.Add(ModelShape.Cylinder, V(0f, 0.062f, 0f), V(0.08f, 0.008f, 0.08f), MaterialRole.Glow);   // altavoz
                    b.Add(ModelShape.Cylinder, V(0f, -0.02f, 0f), V(0.104f, 0.012f, 0.104f), MaterialRole.Accent);
                    b.Add(ModelShape.Cylinder, V(0.03f, 0.12f, 0f), V(0.008f, 0.12f, 0.008f), MaterialRole.Metal); // antena
                    b.Add(ModelShape.Sphere, V(0.03f, 0.18f, 0f), V(0.02f, 0.02f, 0.02f), MaterialRole.Glow);
                    break;
                case DeviceKind.EmpPulse:
                    b.Add(ModelShape.Sphere, V(0f, 0f, 0f), V(0.11f, 0.11f, 0.11f), MaterialRole.Body);
                    b.Add(ModelShape.Cylinder, V(0f, 0f, 0f), V(0.15f, 0.012f, 0.15f), MaterialRole.Glow);       // anillos
                    b.Add(ModelShape.Cylinder, V(0f, 0f, 0f), V(0.15f, 0.012f, 0.15f), MaterialRole.Glow, V(90f, 0f, 0f));
                    b.Add(ModelShape.Cylinder, V(0f, 0.058f, 0f), V(0.04f, 0.016f, 0.04f), MaterialRole.Metal);  // detonador
                    break;
                case DeviceKind.OpticalCamo:
                    b.Add(ModelShape.Cylinder, V(0f, 0f, 0f), V(0.14f, 0.03f, 0.14f), MaterialRole.Body);
                    b.Add(ModelShape.Sphere, V(0f, 0.016f, 0f), V(0.07f, 0.035f, 0.07f), MaterialRole.Glow);     // lente
                    b.Add(ModelShape.Cylinder, V(0f, 0.012f, 0f), V(0.1f, 0.01f, 0.1f), MaterialRole.Accent);
                    for (int i = 0; i < 3; i++)
                        b.Add(ModelShape.Box, MVec.Rotate(V(0.075f, 0f, 0f), V(0f, 120f * i, 0f)), V(0.02f, 0.02f, 0.025f), MaterialRole.Metal, V(0f, 120f * i, 0f));
                    break;
                default:   // kit de reparación
                    b.AddColored(ModelShape.Box, V(0f, 0f, 0f), V(0.18f, 0.1f, 0.12f), MaterialRole.Body, KitWhite);
                    b.Add(ModelShape.Box, V(0f, 0.051f, 0f), V(0.08f, 0.003f, 0.025f), MaterialRole.Accent);     // cruz
                    b.Add(ModelShape.Box, V(0f, 0.051f, 0f), V(0.025f, 0.003f, 0.08f), MaterialRole.Accent);
                    b.Add(ModelShape.Box, V(0f, 0.07f, 0f), V(0.07f, 0.012f, 0.016f), MaterialRole.Grip);        // asa
                    b.Add(ModelShape.Box, V(-0.06f, 0.02f, 0.061f), V(0.02f, 0.02f, 0.004f), MaterialRole.Metal); // cierres
                    b.Add(ModelShape.Box, V(0.06f, 0.02f, 0.061f), V(0.02f, 0.02f, 0.004f), MaterialRole.Metal);
                    b.Add(ModelShape.Box, V(0f, -0.03f, 0.061f), V(0.03f, 0.008f, 0.003f), MaterialRole.Glow);   // piloto
                    break;
            }
            return b;
        }

        /// <summary>Paquete de munición: caja con tres balas de plasma de pie.</summary>
        public static ModelBlueprint Ammo()
        {
            var b = new ModelBlueprint("ammo", AmmoColor);
            b.Add(ModelShape.Box, V(0f, 0f, 0f), V(0.2f, 0.08f, 0.12f), MaterialRole.Body);
            b.Add(ModelShape.Box, V(0f, 0f, 0.061f), V(0.16f, 0.02f, 0.004f), MaterialRole.Accent);
            for (int i = -1; i <= 1; i++)
                b.Add(ModelShape.Capsule, V(0.05f * i, 0.065f, 0f), V(0.026f, 0.07f, 0.026f), MaterialRole.Glow);
            b.Add(ModelShape.Box, V(-0.085f, 0f, 0f), V(0.012f, 0.084f, 0.124f), MaterialRole.Metal);          // refuerzos
            b.Add(ModelShape.Box, V(0.085f, 0f, 0f), V(0.012f, 0.084f, 0.124f), MaterialRole.Metal);
            return b;
        }

        /// <summary>Módulo de accesorio suelto: maletín morado con la forma del accesorio encima.</summary>
        public static ModelBlueprint ModCase(WeaponMod mod)
        {
            var b = new ModelBlueprint("mod_" + mod, ModColor);
            b.AddColored(ModelShape.Box, V(0f, 0f, 0f), V(0.2f, 0.06f, 0.14f), MaterialRole.Body, CaseDark);
            b.Add(ModelShape.Box, V(0f, 0.031f, 0f), V(0.204f, 0.006f, 0.144f), MaterialRole.Accent);           // tapa
            b.Add(ModelShape.Box, V(0f, -0.015f, 0.071f), V(0.06f, 0.012f, 0.004f), MaterialRole.Glow);         // piloto
            switch (mod)
            {
                case WeaponMod.Suppressor:
                    b.Add(ModelShape.Cylinder, V(0f, 0.058f, 0f), V(0.045f, 0.15f, 0.045f), MaterialRole.Body, V(0f, 0f, 90f));
                    b.Add(ModelShape.Cylinder, V(-0.07f, 0.058f, 0f), V(0.049f, 0.012f, 0.049f), MaterialRole.Glow, V(0f, 0f, 90f));
                    break;
                case WeaponMod.ExtendedMag:
                    b.Add(ModelShape.Box, V(0f, 0.08f, 0f), V(0.04f, 0.09f, 0.055f), MaterialRole.Metal, V(14f, 0f, 0f));
                    b.Add(ModelShape.Box, V(0f, 0.126f, 0.01f), V(0.042f, 0.01f, 0.057f), MaterialRole.Glow, V(14f, 0f, 0f));
                    break;
                case WeaponMod.Stabilizer:
                    b.Add(ModelShape.Cylinder, V(0f, 0.075f, 0f), V(0.03f, 0.08f, 0.03f), MaterialRole.Grip);
                    b.Add(ModelShape.Box, V(0f, 0.12f, 0f), V(0.034f, 0.014f, 0.06f), MaterialRole.Metal);
                    break;
                default:   // disipador
                    for (int i = -1; i <= 1; i++)
                        b.Add(ModelShape.Box, V(0f, 0.055f, 0.035f * i), V(0.08f, 0.04f, 0.008f), MaterialRole.Metal);
                    break;
            }
            return b;
        }
    }
}
