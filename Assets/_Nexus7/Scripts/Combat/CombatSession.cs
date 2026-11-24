using System.Collections.Generic;
using UnityEngine;

namespace Nexus7.Combat
{
    /// <summary>
    /// Armas y munición que sobreviven al cambio de fase (en memoria, como GameSession con el inventario).
    /// PlayerWeapon lo guarda al descargarse la escena y lo recupera en la siguiente, con sus accesorios. Nueva partida → arsenal inicial.
    /// </summary>
    public static class CombatSession
    {
        private struct Entry { public string id; public int ammo, reserve; public string mods; }

        private static readonly List<Entry> saved = new List<Entry>();
        private static int savedIndex;
        public static bool HasSaved { get; private set; }

        public static void Store(ArsenalModel arsenal)
        {
            if (arsenal == null) return;
            saved.Clear();
            foreach (var w in arsenal.Weapons) saved.Add(new Entry { id = w.Spec.Id, ammo = w.Ammo, reserve = w.Reserve, mods = WeaponModModel.Serialize(w.Mods) });
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
                    if (spec != null) a.Add(new WeaponModel(spec, e.ammo, e.reserve, WeaponModModel.Parse(e.mods)));
                }
                if (a.Weapons.Count > 0) { a.Select(Mathf.Clamp(savedIndex, 0, a.Weapons.Count - 1), -999f); return a; }
            }
            foreach (var spec in PlayerWeaponCatalog.All) a.Add(WeaponModel.Fresh(spec));
            return a;
        }

        /// <summary>Arsenal → lista para la partida guardada en disco.</summary>
        public static List<Nexus7.Core.SavedWeapon> ToSaved(ArsenalModel arsenal, out int currentIndex)
        {
            var list = new List<Nexus7.Core.SavedWeapon>();
            currentIndex = 0;
            if (arsenal == null) return list;
            foreach (var w in arsenal.Weapons)
                list.Add(new Nexus7.Core.SavedWeapon { id = w.Spec.Id, ammo = w.Ammo, reserve = w.Reserve, mods = WeaponModModel.Serialize(w.Mods) });
            currentIndex = System.Math.Max(0, arsenal.CurrentIndex);
            return list;
        }

        /// <summary>
        /// Partida guardada → sesión. Sin armas guardadas (guardado antiguo) se queda vacío y la fase empieza con el arsenal
        /// inicial. Los Id que ya no existen en el catálogo se omiten; la munición se limita al cargador y la reserva máximos.
        /// </summary>
        public static void RestoreSaved(List<Nexus7.Core.SavedWeapon> weapons, int currentIndex)
        {
            Clear();
            if (weapons == null) return;
            foreach (var w in weapons)
                if (w != null && PlayerWeaponCatalog.Get(w.id) != null)
                    saved.Add(new Entry { id = w.id, ammo = System.Math.Max(0, w.ammo), reserve = System.Math.Max(0, w.reserve), mods = w.mods });
            if (saved.Count == 0) return;
            savedIndex = System.Math.Min(System.Math.Max(0, currentIndex), saved.Count - 1);
            HasSaved = true;
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
