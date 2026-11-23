using System.Collections.Generic;
using UnityEngine;

namespace Nexus7.Combat
{
    /// <summary>
    /// Armas y munición que sobreviven al cambio de fase (en memoria, como GameSession con el inventario).
    /// PlayerWeapon lo guarda al descargarse la escena y lo recupera en la siguiente. Nueva partida → arsenal inicial.
    /// </summary>
    public static class CombatSession
    {
        private struct Entry { public string id; public int ammo, reserve; }

        private static readonly List<Entry> saved = new List<Entry>();
        private static int savedIndex;
        public static bool HasSaved { get; private set; }

        public static void Store(ArsenalModel arsenal)
        {
            if (arsenal == null) return;
            saved.Clear();
            foreach (var w in arsenal.Weapons) saved.Add(new Entry { id = w.Spec.Id, ammo = w.Ammo, reserve = w.Reserve });
            savedIndex = arsenal.CurrentIndex;
            HasSaved = true;
        }

        /// <summary>Arsenal guardado o, si no hay, el inicial: todas las armas del catálogo con su munición de salida.</summary>
        public static ArsenalModel Load()
        {
            var a = new ArsenalModel();
            if (HasSaved)
            {
                foreach (var e in saved)
                {
                    var spec = PlayerWeaponCatalog.Get(e.id);
                    if (spec != null) a.Add(new WeaponModel(spec, e.ammo, e.reserve));
                }
                if (a.Weapons.Count > 0) { a.Select(Mathf.Clamp(savedIndex, 0, a.Weapons.Count - 1), -999f); return a; }
            }
            foreach (var spec in PlayerWeaponCatalog.All) a.Add(WeaponModel.Fresh(spec));
            return a;
        }

        public static void Clear()
        {
            saved.Clear();
            savedIndex = 0;
            HasSaved = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => Clear();
    }
}
