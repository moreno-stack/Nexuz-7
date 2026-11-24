using System; using System.Collections.Generic; using System.Linq;
using Nexus7.Visual.Models; using Nexus7.Combat; using Nexus7.Inventory; using Nexus7.Devices;
static class T {
  static int fails=0; static void Check(bool c,string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }
  static readonly MColor Cyan = new MColor(0.2f,0.9f,1f);
  static float Length(ModelBlueprint b){ return b.MaxCorner().z - b.MinCorner().z; }
  static float Largest(ModelBlueprint b){ var mn=b.MinCorner(); var mx=b.MaxCorner(); return Math.Max(mx.x-mn.x, Math.Max(mx.y-mn.y, mx.z-mn.z)); }
  static bool Valid(ModelBlueprint b){ return b != null && b.Parts.Count > 0 && b.Parts.All(p => p.Size.x > 0 && p.Size.y > 0 && p.Size.z > 0); }
  static bool Lit(ModelBlueprint b){ return b.Parts.Any(p => p.Role == MaterialRole.Accent || p.Role == MaterialRole.Glow); }
  static string Sig(ModelBlueprint b){ return string.Join("|", b.Parts.Select(p => p.Shape + ":" + p.Size.x.ToString("0.000") + "," + p.Size.y.ToString("0.000") + "," + p.Size.z.ToString("0.000"))); }
  static ModelBlueprint W(string id, params WeaponMod[] mods){ return ModelLibrary.Weapon(id, Cyan, mods); }
  static void Main(){
    // ---------- Tarea 1: armas ----------
    string[] ids = { PlayerWeaponCatalog.Pistol, PlayerWeaponCatalog.Shotgun, PlayerWeaponCatalog.Rifle, PlayerWeaponCatalog.Railgun };
    var bps = ids.Select(i => W(i)).ToArray();
    for (int i = 0; i < ids.Length; i++){
      Check(Valid(bps[i]), ids[i] + ": piezas válidas");
      Check(Lit(bps[i]), ids[i] + ": tiene franja de color o piezas que brillan");
      Check(Length(bps[i]) <= 0.75f, ids[i] + ": no pasa de 0,75 m (" + Length(bps[i]).ToString("0.00") + ")");
      Check(bps[i].Parts.Count >= 6, ids[i] + ": al menos 6 piezas (" + bps[i].Parts.Count + ")");
      Check(bps[i].Parts.Where(p => p.Role == MaterialRole.Accent).All(p => p.Color.Equals(Cyan)) && bps[i].Accent.Equals(Cyan), ids[i] + ": la franja lleva el color del arma");
    }
    for (int i = 0; i + 1 < bps.Length; i++) Check(Length(bps[i]) < Length(bps[i+1]), ids[i] + " es más corta que " + ids[i+1]);
    Check(bps.Select(Sig).Distinct().Count() == 4, "Las 4 armas son distintas");
    Check(W("arma_rara").Parts.Count > 0, "Un arma desconocida usa la pistola");

    var rifle = W(PlayerWeaponCatalog.Rifle);
    var supp = W(PlayerWeaponCatalog.Rifle, WeaponMod.Suppressor);
    Check(supp.MaxCorner().z > rifle.MaxCorner().z + 0.05f, "Supresor: el arma se alarga por delante");
    var mag = W(PlayerWeaponCatalog.Rifle, WeaponMod.ExtendedMag);
    Check(mag.MinCorner().y < rifle.MinCorner().y - 0.02f, "Cargador ampliado: el cargador baja más");
    Check(W(PlayerWeaponCatalog.Rifle, WeaponMod.Stabilizer).Parts.Count > rifle.Parts.Count, "Estabilizador: añade la empuñadura vertical");
    Check(W(PlayerWeaponCatalog.Rifle, WeaponMod.Heatsink).Parts.Count >= rifle.Parts.Count + 3, "Disipador: añade 3 aletas");
    Check(Sig(W(PlayerWeaponCatalog.Rifle, new WeaponMod[0])) == Sig(rifle), "Sin accesorios = plano base");
    Check(Sig(ModelLibrary.Weapon(PlayerWeaponCatalog.Rifle, Cyan, null)) == Sig(rifle), "Lista de accesorios nula = plano base");
    var both = W(PlayerWeaponCatalog.Pistol, WeaponMod.Suppressor, WeaponMod.Heatsink);
    Check(both.Parts.Count >= W(PlayerWeaponCatalog.Pistol).Parts.Count + 4, "Dos accesorios a la vez se ven los dos");
    var sc = rifle.Scaled(2f);
    Check(Math.Abs(Length(sc) - 2f * Length(rifle)) < 0.001f, "Scaled multiplica posiciones y tamaños");
    // Límites con piezas giradas: una caja de 1×0,1×0,1 girada 90° en Y ocupa 1 m en Z
    var rot = new ModelBlueprint("giro", Cyan); rot.Add(ModelShape.Box, new MVec(0,0,0), new MVec(1f,0.1f,0.1f), MaterialRole.Body, new MVec(0,90,0));
    Check(Math.Abs(Length(rot) - 1f) < 0.01f, "Los límites tienen en cuenta el giro de la pieza");

    Items();
    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
  static void Items(){
    // ---------- Tarea 2: objetos, dispositivos, munición y accesorios ----------
    string[] own = { "battery_ion", "scrap_metal", "component_circuit", "component_optic_crystal", "tool_hydraulic_wrench", "key_director_card", "key_reactor_module" };
    ItemCategory[] cats = { ItemCategory.Battery, ItemCategory.Part, ItemCategory.Component, ItemCategory.Component, ItemCategory.Tool, ItemCategory.Tool, ItemCategory.Tool };
    var green = new MColor(0.55f,1f,0.35f);
    var sigs = new List<string>();
    for (int n = 0; n < own.Length; n++){
      string id = own[n];
      var b = ModelLibrary.Item(id, cats[n], green);
      Check(ModelLibrary.HasOwnModel(id), id + ": tiene modelo propio");
      Check(Valid(b) && b.Parts.Count >= 3, id + ": piezas válidas (" + b.Parts.Count + ")");
      Check(Largest(b) <= 0.45f, id + ": cabe en el punto de botín (" + Largest(b).ToString("0.00") + " m)");
      Check(Lit(b), id + ": tiene una parte de su color");
      Check(b.Accent.Equals(green) && b.Parts.Where(p => p.Role == MaterialRole.Accent || p.Role == MaterialRole.Glow).Any(p => p.Color.Equals(green)), id + ": usa el color del objeto");
      sigs.Add(Sig(b));
    }
    Check(sigs.Distinct().Count() == own.Length, "Los 7 objetos son distintos entre sí");
    foreach (ItemCategory cat in Enum.GetValues(typeof(ItemCategory))){
      var g = ModelLibrary.Item("objeto_nuevo", cat, green);
      Check(Valid(g) && g.Key == "generic_" + cat, "Id desconocido de " + cat + " → forma genérica de su categoría");
    }
    Check(!ModelLibrary.HasOwnModel("objeto_nuevo") && !ModelLibrary.HasOwnModel(null), "Un Id desconocido o nulo no tiene modelo propio");
    var dsig = new List<string>();
    foreach (DeviceKind k in Enum.GetValues(typeof(DeviceKind))){
      var d = ModelLibrary.Device(k, green);
      Check(Valid(d) && Lit(d) && Largest(d) <= 0.45f, "Dispositivo " + k + ": modelo válido, con luz y de tamaño de mano");
      dsig.Add(Sig(d));
    }
    Check(dsig.Distinct().Count() == 4, "Los 4 dispositivos son distintos");
    var ammo = ModelLibrary.Ammo();
    Check(Valid(ammo) && ammo.Parts.Count(p => p.Role == MaterialRole.Glow) >= 3 && Largest(ammo) <= 0.45f, "Munición: caja con 3 balas que brillan");
    foreach (var mod in WeaponModModel.All){
      var c = ModelLibrary.ModCase(mod);
      Check(Valid(c) && Largest(c) <= 0.45f && c.Parts.Count >= 3, "Accesorio suelto " + mod + ": maletín con su forma");
    }
    Check(Sig(ModelLibrary.ModCase(WeaponMod.Suppressor)) != Sig(ModelLibrary.ModCase(WeaponMod.Heatsink)), "Cada accesorio suelto enseña su forma");
  }
}
