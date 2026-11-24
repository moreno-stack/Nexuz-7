using System;
using System.Collections.Generic;

namespace Nexus7.Combat
{
    /// <summary>
    /// Armas que lleva el jugador y cuál tiene en la mano (lógica pura, probada).
    /// Cambiar de arma tarda SwitchTime y cancela la recarga sin perder munición.
    /// </summary>
    public sealed class ArsenalModel
    {
        public const float SwitchTime = 0.35f;

        private readonly List<WeaponModel> weapons = new List<WeaponModel>();
        private float switchEnd = -999f;

        public IReadOnlyList<WeaponModel> Weapons => weapons;
        public int CurrentIndex { get; private set; } = -1;
        public WeaponModel Current => CurrentIndex >= 0 && CurrentIndex < weapons.Count ? weapons[CurrentIndex] : null;
        public bool IsSwitching(float time) => time < switchEnd;
        public float SwitchProgress(float time) => IsSwitching(time) ? 1f - (switchEnd - time) / SwitchTime : 1f;

        public event Action<WeaponModel> Switched;

        public WeaponModel Find(string id)
        {
            foreach (var w in weapons) if (w.Spec.Id == id) return w;
            return null;
        }

        /// <summary>Añade un arma. Si ya la tiene, solo suma su munición de reserva. Devuelve el arma.</summary>
        public WeaponModel Add(WeaponModel weapon)
        {
            var existing = Find(weapon.Spec.Id);
            if (existing != null) { existing.AddReserve(weapon.Ammo + weapon.Reserve); return existing; }
            weapons.Add(weapon);
            if (CurrentIndex < 0) CurrentIndex = 0;
            return weapon;
        }

        public bool Select(int index, float time)
        {
            if (index < 0 || index >= weapons.Count || index == CurrentIndex) return false;
            Current?.CancelReload();
            Current?.ReleaseTrigger();
            CurrentIndex = index;
            switchEnd = time + SwitchTime;
            Switched?.Invoke(Current);
            return true;
        }

        /// <summary>Siguiente (+1) o anterior (−1) arma, en ciclo.</summary>
        public bool Cycle(int direction, float time)
        {
            if (weapons.Count < 2) return false;
            int n = weapons.Count;
            int next = ((CurrentIndex + (direction >= 0 ? 1 : -1)) % n + n) % n;
            return Select(next, time);
        }

        /// <summary>Al caer: se pierde 'fraction' de la reserva de cada arma (redondeando hacia abajo). Devuelve el total perdido.</summary>
        public int ApplyRespawnPenalty(float fraction)
        {
            int total = 0;
            foreach (var w in weapons)
            {
                w.CancelReload();
                total += w.RemoveReserve((int)Math.Floor(w.Reserve * Math.Max(0f, fraction)));
            }
            return total;
        }

        /// <summary>
        /// Arma a la que va un accesorio: la de la mano si le sirve y no lo lleva; si no, la primera que tenga hueco libre;
        /// si no, la primera a la que le sirva (sustituirá su accesorio más antiguo). Null si ninguna lo admite.
        /// </summary>
        public WeaponModel ModTarget(WeaponMod mod)
        {
            if (Wants(Current, mod)) return Current;
            foreach (var w in weapons) if (Wants(w, mod) && w.Mods.Count < WeaponModModel.MaxSlots) return w;
            foreach (var w in weapons) if (Wants(w, mod)) return w;
            return null;
        }

        private static bool Wants(WeaponModel w, WeaponMod mod) => w != null && WeaponModModel.Fits(mod, w.BaseSpec) && !w.HasMod(mod);

        /// <summary>
        /// Arma a la que va un paquete de munición: la que tiene menor fracción de munición
        /// (así el botín empuja a usar todo el arsenal); en empate, la que está en la mano.
        /// </summary>
        public WeaponModel NeediestWeapon()
        {
            WeaponModel best = null;
            foreach (var w in weapons)
            {
                if (w.Reserve >= w.Spec.MaxReserve) continue;
                if (best == null || w.AmmoFraction < best.AmmoFraction - 1e-4f || (Math.Abs(w.AmmoFraction - best.AmmoFraction) < 1e-4f && w == Current))
                    best = w;
            }
            return best;
        }
    }
}
