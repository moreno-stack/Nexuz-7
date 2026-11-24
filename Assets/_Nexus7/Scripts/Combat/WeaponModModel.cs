using System;
using System.Collections.Generic;
using System.Text;

namespace Nexus7.Combat
{
    /// <summary>Accesorios de arma del jugador (sistema modular).</summary>
    public enum WeaponMod { None, Suppressor, ExtendedMag, Stabilizer, Heatsink }

    public enum ModInstall { Installed, Replaced, AlreadyHas, DoesNotFit }

    /// <summary>
    /// Accesorios de arma (puro, probado en WeaponModTest). Cada arma lleva como mucho 2; uno nuevo con los dos huecos
    /// llenos sustituye al más antiguo. El arma con accesorios es una copia del arma del catálogo con los valores cambiados:
    ///   · Supresor: ruido ×0,45 y daño ×0,9.
    ///   · Cargador ampliado: cargador ×1,5 (redondeando hacia arriba) y recarga ×1,15.
    ///   · Estabilizador: dispersión (cadera y apuntando) ×0,7, apertura por disparo ×0,6 y retroceso ×0,6.
    ///   · Disipador: calor por disparo ×0,6 (solo armas que se calientan).
    /// </summary>
    public static class WeaponModModel
    {
        public const int MaxSlots = 2;
        public const float SuppressorNoise = 0.45f, SuppressorDamage = 0.9f;
        public const float MagFactor = 1.5f, MagReload = 1.15f;
        public const float StabSpread = 0.7f, StabBloom = 0.6f, StabRecoil = 0.6f;
        public const float HeatsinkHeat = 0.6f;

        public static readonly WeaponMod[] All = { WeaponMod.Suppressor, WeaponMod.ExtendedMag, WeaponMod.Stabilizer, WeaponMod.Heatsink };

        /// <summary>¿Sirve este accesorio en esta arma? (el disipador solo en las que se calientan)</summary>
        public static bool Fits(WeaponMod mod, PlayerWeaponSpec spec)
        {
            if (spec == null || mod == WeaponMod.None) return false;
            return mod != WeaponMod.Heatsink || spec.HeatPerShot > 0f;
        }

        /// <summary>Copia de 'baseSpec' con los accesorios aplicados (el catálogo no se toca).</summary>
        public static PlayerWeaponSpec Apply(PlayerWeaponSpec baseSpec, IList<WeaponMod> mods)
        {
            var s = baseSpec.Clone();
            if (mods == null) return s;
            foreach (var m in mods)
            {
                switch (m)
                {
                    case WeaponMod.Suppressor:
                        s.NoiseRadius *= SuppressorNoise;
                        s.Damage *= SuppressorDamage;
                        break;
                    case WeaponMod.ExtendedMag:
                        s.MagazineSize = (int)Math.Ceiling(s.MagazineSize * MagFactor);
                        s.ReloadTime *= MagReload;
                        break;
                    case WeaponMod.Stabilizer:
                        s.HipSpread *= StabSpread;
                        s.AimSpread *= StabSpread;
                        s.BloomPerShot *= StabBloom;
                        s.Recoil *= StabRecoil;
                        break;
                    case WeaponMod.Heatsink:
                        s.HeatPerShot *= HeatsinkHeat;
                        break;
                }
            }
            return s;
        }

        /// <summary>Pone 'mod' en la lista. Si ya hay 2, quita el más antiguo ('replaced').</summary>
        public static ModInstall Install(List<WeaponMod> mods, WeaponMod mod, PlayerWeaponSpec spec, out WeaponMod replaced)
        {
            replaced = WeaponMod.None;
            if (!Fits(mod, spec)) return ModInstall.DoesNotFit;
            if (mods.Contains(mod)) return ModInstall.AlreadyHas;
            if (mods.Count >= MaxSlots)
            {
                replaced = mods[0];
                mods.RemoveAt(0);
                mods.Add(mod);
                return ModInstall.Replaced;
            }
            mods.Add(mod);
            return ModInstall.Installed;
        }

        public static string Name(WeaponMod m)
        {
            switch (m)
            {
                case WeaponMod.Suppressor: return "Supresor";
                case WeaponMod.ExtendedMag: return "Cargador ampliado";
                case WeaponMod.Stabilizer: return "Estabilizador";
                case WeaponMod.Heatsink: return "Disipador";
                default: return "—";
            }
        }

        /// <summary>Etiqueta corta para el HUD.</summary>
        public static string Tag(WeaponMod m)
        {
            switch (m)
            {
                case WeaponMod.Suppressor: return "SUP";
                case WeaponMod.ExtendedMag: return "CAR";
                case WeaponMod.Stabilizer: return "EST";
                case WeaponMod.Heatsink: return "DIS";
                default: return "";
            }
        }

        public static string Effect(WeaponMod m)
        {
            switch (m)
            {
                case WeaponMod.Suppressor: return "menos ruido, algo menos de daño";
                case WeaponMod.ExtendedMag: return "+50 % de cargador, recarga algo más lenta";
                case WeaponMod.Stabilizer: return "menos dispersión y retroceso";
                case WeaponMod.Heatsink: return "se calienta menos";
                default: return "";
            }
        }

        /// <summary>Lista → texto para guardar ("Suppressor,Stabilizer").</summary>
        public static string Serialize(IList<WeaponMod> mods)
        {
            if (mods == null || mods.Count == 0) return "";
            var sb = new StringBuilder();
            for (int i = 0; i < mods.Count; i++) { if (i > 0) sb.Append(','); sb.Append(mods[i]); }
            return sb.ToString();
        }

        /// <summary>Texto guardado → lista (ignora nombres desconocidos, repetidos y lo que pase de 2).</summary>
        public static List<WeaponMod> Parse(string text)
        {
            var list = new List<WeaponMod>();
            if (string.IsNullOrEmpty(text)) return list;
            foreach (var part in text.Split(','))
            {
                if (!Enum.TryParse(part.Trim(), out WeaponMod m) || m == WeaponMod.None || !Enum.IsDefined(typeof(WeaponMod), m)) continue;
                if (list.Contains(m) || list.Count >= MaxSlots) continue;
                list.Add(m);
            }
            return list;
        }
    }
}
