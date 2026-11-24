using System;
using System.Collections.Generic;
using Nexus7.Combat;

namespace Nexus7.Director
{
    /// <summary>Acciones del panel de pruebas [F4]. El orden es el de las teclas F5–F12.</summary>
    public enum DebugAction { KeyItems, Ammo, Mod, Life, TensionUp, TensionDown, ShipEvent, NextPhase }

    /// <summary>Qué hay en la escena ahora (lo rellena DebugPanel cada vez que se dibuja).</summary>
    public struct DebugContext
    {
        public bool HasInventory, HasWeapon, HasDirector, HasShipEvents;
        public int KeyItemsMissing;
        public int Lives, MaxLives;
        public float Tension, MinTension, MaxTension;
        public int PhaseIndex, PhaseCount;
    }

    /// <summary>
    /// Panel de pruebas [F4] (puro, probado en DebugPanelTest). Solo existe en el editor y en las compilaciones de
    /// desarrollo: en una compilación normal ni se crea. Cada acción dice si se puede usar ahora y, si no, por qué.
    /// Con el panel abierto se usan F5–F12 (los números 1–9 ya son de la barra rápida):
    ///   F5 Objetos imprescindibles de la fase · F6 Munición llena · F7 Accesorio · F8 +1 vida ·
    ///   F9/F10 Tensión ±0,1 · F11 Evento de nave (en rueda) · F12 Fase siguiente.
    /// </summary>
    public static class DebugPanelModel
    {
        public const float TensionStep = 0.1f;
        /// <summary>La primera acción va en F5.</summary>
        public const int FirstKey = 5;

        private static readonly DebugAction[] actions =
        {
            DebugAction.KeyItems, DebugAction.Ammo, DebugAction.Mod, DebugAction.Life,
            DebugAction.TensionUp, DebugAction.TensionDown, DebugAction.ShipEvent, DebugAction.NextPhase,
        };

        public static IReadOnlyList<DebugAction> Actions => actions;

        /// <summary>El panel solo existe en el editor o en una compilación de desarrollo.</summary>
        public static bool Enabled(bool isEditor, bool isDebugBuild) => isEditor || isDebugBuild;

        /// <summary>Número de la tecla de función (F5–F12) de la acción.</summary>
        public static int Key(DebugAction a) => Array.IndexOf(actions, a) + FirstKey;

        /// <summary>La acción de la tecla F'n' (F5–F12), o false si no hay ninguna.</summary>
        public static bool FromKey(int n, out DebugAction action)
        {
            action = DebugAction.KeyItems;
            int i = n - FirstKey;
            if (i < 0 || i >= actions.Length) return false;
            action = actions[i];
            return true;
        }

        public static string Label(DebugAction a)
        {
            switch (a)
            {
                case DebugAction.KeyItems: return "Dar los objetos imprescindibles de la fase";
                case DebugAction.Ammo: return "Llenar la munición de todas las armas";
                case DebugAction.Mod: return "Dar un accesorio al arma";
                case DebugAction.Life: return "+1 vida";
                case DebugAction.TensionUp: return $"Subir la tensión +{TensionStep:0.0}";
                case DebugAction.TensionDown: return $"Bajar la tensión −{TensionStep:0.0}";
                case DebugAction.ShipEvent: return "Forzar un evento de nave";
                default: return "Saltar a la fase siguiente";
            }
        }

        /// <summary>null si la acción se puede usar ahora; si no, el motivo (se ve en gris en el panel).</summary>
        public static string Unavailable(DebugAction a, DebugContext c)
        {
            switch (a)
            {
                case DebugAction.KeyItems:
                    if (!c.HasInventory) return "sin inventario";
                    return c.KeyItemsMissing > 0 ? null : "ya los tienes (o la fase no tiene)";
                case DebugAction.Ammo:
                case DebugAction.Mod:
                    return c.HasWeapon ? null : "sin arma";
                case DebugAction.Life:
                    return c.Lives < c.MaxLives ? null : "vidas al máximo";
                case DebugAction.TensionUp:
                    if (!c.HasDirector) return "sin director";
                    return c.Tension < c.MaxTension - 0.001f ? null : "tensión al máximo";
                case DebugAction.TensionDown:
                    if (!c.HasDirector) return "sin director";
                    return c.Tension > c.MinTension + 0.001f ? null : "tensión al mínimo";
                case DebugAction.ShipEvent:
                    return c.HasShipEvents ? null : "sin eventos de nave";
                default:
                    if (c.PhaseIndex < 0) return "no es una fase";
                    return NextPhase(c.PhaseIndex, c.PhaseCount) >= 0 ? null : "es la última fase";
            }
        }

        /// <summary>La tensión tras pulsar ±: un paso, sin salir de [min, max].</summary>
        public static float Nudge(float tension, int direction, float min, float max)
        {
            float t = tension + Math.Sign(direction) * TensionStep;
            return t < min ? min : t > max ? max : t;
        }

        /// <summary>El evento siguiente en la rueda Apagón → Vapor → Barrido → Apagón.</summary>
        public static ShipEvent NextEvent(ShipEvent current)
        {
            switch (current)
            {
                case ShipEvent.Blackout: return ShipEvent.SteamLeak;
                case ShipEvent.SteamLeak: return ShipEvent.SecuritySweep;
                default: return ShipEvent.Blackout;
            }
        }

        /// <summary>El siguiente accesorio de la rueda que admita alguna arma (None si ninguno).</summary>
        public static WeaponMod NextMod(WeaponMod last, Func<WeaponMod, bool> usable)
        {
            const int count = 4;   // Suppressor, ExtendedMag, Stabilizer, Heatsink
            int start = last == WeaponMod.None ? 0 : (int)last;
            for (int k = 0; k < count; k++)
            {
                var mod = (WeaponMod)(1 + (start + k) % count);
                if (usable == null || usable(mod)) return mod;
            }
            return WeaponMod.None;
        }

        /// <summary>Índice de la fase siguiente, o −1 si es la última (o no es una fase).</summary>
        public static int NextPhase(int index, int count) => index >= 0 && index < count - 1 ? index + 1 : -1;

        /// <summary>Cuántos objetos faltan para tener 'needed' si ya tienes 'have'.</summary>
        public static int Missing(int needed, int have) => Math.Max(0, needed - have);
    }
}
