using System;
using System.Collections.Generic;
using Nexus7.Combat;
static class T {
  static int fails=0; static void Check(bool c,string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }
  static bool Near(float a,float b){ return Math.Abs(a-b)<0.01f; }

  static void Effects(){
    var pistol = PlayerWeaponCatalog.Get(PlayerWeaponCatalog.Pistol);
    var sup = WeaponModModel.Apply(pistol, new List<WeaponMod>{ WeaponMod.Suppressor });
    Check(Near(sup.NoiseRadius, pistol.NoiseRadius*0.45f) && Near(sup.Damage, pistol.Damage*0.9f), "Supresor: ruido ×0,45 y daño ×0,9");
    Check(Near(pistol.NoiseRadius,10f) && Near(pistol.Damage,20f), "El catálogo no cambia (se modifica una copia)");
    var mag = WeaponModModel.Apply(pistol, new List<WeaponMod>{ WeaponMod.ExtendedMag });
    Check(mag.MagazineSize==18 && Near(mag.ReloadTime, pistol.ReloadTime*1.15f), "Cargador ampliado: 12 → 18 balas y recarga ×1,15");
    var rail = WeaponModModel.Apply(PlayerWeaponCatalog.Get(PlayerWeaponCatalog.Railgun), new List<WeaponMod>{ WeaponMod.ExtendedMag });
    Check(rail.MagazineSize==5, "Redondea hacia arriba (cañón de riel 3 → 5)");
    var stab = WeaponModModel.Apply(pistol, new List<WeaponMod>{ WeaponMod.Stabilizer });
    Check(Near(stab.HipSpread, 1.2f*0.7f) && Near(stab.AimSpread, 0.3f*0.7f) && Near(stab.BloomPerShot, 0.4f*0.6f) && Near(stab.Recoil, 0.6f*0.6f), "Estabilizador: dispersión ×0,7, apertura y retroceso ×0,6");
    var heat = WeaponModModel.Apply(pistol, new List<WeaponMod>{ WeaponMod.Heatsink });
    Check(Near(heat.HeatPerShot, 12f*0.6f), "Disipador: calor ×0,6");
    var both = WeaponModModel.Apply(pistol, new List<WeaponMod>{ WeaponMod.Suppressor, WeaponMod.Stabilizer });
    Check(Near(both.NoiseRadius, 4.5f) && Near(both.Recoil, 0.36f) && both.Id==pistol.Id, "Dos accesorios se suman y el arma sigue siendo la misma (Id)");
  }

  static void Slots(){
    var shotgun = PlayerWeaponCatalog.Get(PlayerWeaponCatalog.Shotgun);
    var mods = new List<WeaponMod>();
    Check(WeaponModModel.Install(mods, WeaponMod.Heatsink, shotgun, out _)==ModInstall.DoesNotFit, "El disipador no sirve en un arma que no se calienta (escopeta)");
    Check(WeaponModModel.Install(mods, WeaponMod.Suppressor, shotgun, out _)==ModInstall.Installed, "Monta un accesorio");
    Check(WeaponModModel.Install(mods, WeaponMod.Suppressor, shotgun, out _)==ModInstall.AlreadyHas && mods.Count==1, "No monta el mismo dos veces");
    WeaponModModel.Install(mods, WeaponMod.Stabilizer, shotgun, out _);
    Check(WeaponModModel.Install(mods, WeaponMod.ExtendedMag, shotgun, out WeaponMod old)==ModInstall.Replaced && old==WeaponMod.Suppressor
          && mods.Count==2 && mods[0]==WeaponMod.Stabilizer && mods[1]==WeaponMod.ExtendedMag, "Con 2 huecos llenos sustituye al más antiguo");
  }

  static void Model(){
    var pistol = PlayerWeaponCatalog.Get(PlayerWeaponCatalog.Pistol);
    var w = new WeaponModel(pistol, 12, 30);
    Check(w.Spec==pistol && w.Mods.Count==0, "Sin accesorios usa el arma del catálogo");
    Check(w.InstallMod(WeaponMod.ExtendedMag, out _)==ModInstall.Installed && w.Spec.MagazineSize==18 && w.Ammo==12 && w.BaseSpec==pistol, "Al montar cambia el arma al momento y conserva la munición");
    w.Tick(0f,0f); w.StartReload(0f); w.Tick(2f,2f);
    Check(w.Ammo==18 && w.Reserve==24, "Recarga hasta el cargador ampliado");
    w.InstallMod(WeaponMod.Suppressor, out _); w.InstallMod(WeaponMod.Stabilizer, out WeaponMod gone);
    Check(gone==WeaponMod.ExtendedMag && w.Spec.MagazineSize==12 && w.Ammo==12 && w.Reserve==30, "Al quitar el cargador ampliado, las balas que sobran vuelven a la reserva");
    Check(w.HasMod(WeaponMod.Suppressor) && Near(w.Spec.NoiseRadius,4.5f), "El ruido del disparo usa el arma con accesorios");
    var loaded = new WeaponModel(pistol, 18, 10, new List<WeaponMod>{ WeaponMod.ExtendedMag });
    Check(loaded.Ammo==18 && loaded.Spec.MagazineSize==18, "Al cargar una partida, la munición se limita al cargador con accesorios");
    var rail = new WeaponModel(PlayerWeaponCatalog.Get(PlayerWeaponCatalog.Shotgun), 6, 0, new List<WeaponMod>{ WeaponMod.Heatsink, WeaponMod.Stabilizer });
    Check(rail.Mods.Count==1 && rail.Mods[0]==WeaponMod.Stabilizer, "Al cargar descarta los accesorios que no sirven en esa arma");
  }

  static void Saving(){
    var list = new List<WeaponMod>{ WeaponMod.Suppressor, WeaponMod.Stabilizer };
    string s = WeaponModModel.Serialize(list);
    Check(s=="Suppressor,Stabilizer", "Se guarda como texto legible");
    var back = WeaponModModel.Parse(s);
    Check(back.Count==2 && back[0]==WeaponMod.Suppressor && back[1]==WeaponMod.Stabilizer, "Y se recupera igual");
    Check(WeaponModModel.Parse(null).Count==0 && WeaponModModel.Parse("").Count==0, "Guardado antiguo (sin accesorios): lista vacía");
    var junk = WeaponModModel.Parse("Laser, Suppressor,Suppressor,None,7,Heatsink,Stabilizer");
    Check(junk.Count==2 && junk[0]==WeaponMod.Suppressor && junk[1]==WeaponMod.Heatsink, "Ignora nombres desconocidos, repetidos y lo que pase de 2");
    Check(WeaponModModel.Name(WeaponMod.ExtendedMag)=="Cargador ampliado" && WeaponModModel.Tag(WeaponMod.Stabilizer)=="EST", "Nombres y etiquetas en español");
  }

  static void Targets(){
    var a = new ArsenalModel();
    foreach (var spec in PlayerWeaponCatalog.All) a.Add(WeaponModel.Fresh(spec));
    a.Select(PlayerWeaponCatalog.IndexOf(PlayerWeaponCatalog.Shotgun), 0f);
    Check(a.ModTarget(WeaponMod.Suppressor)==a.Current, "El accesorio va al arma de la mano si le sirve");
    Check(a.ModTarget(WeaponMod.Heatsink).Spec.Id==PlayerWeaponCatalog.Pistol, "Si a la de la mano no le sirve (disipador en la escopeta), va a otra");
    a.Current.InstallMod(WeaponMod.Suppressor, out _);
    Check(a.ModTarget(WeaponMod.Suppressor).Spec.Id!=PlayerWeaponCatalog.Shotgun, "Si la de la mano ya lo lleva, va a otra");
    a.Weapons[0].InstallMod(WeaponMod.Stabilizer, out _); a.Weapons[0].InstallMod(WeaponMod.Heatsink, out _);
    Check(a.ModTarget(WeaponMod.Suppressor)!=a.Weapons[0], "Prefiere un arma con hueco libre antes que sustituir");
    var solo = new ArsenalModel(); solo.Add(WeaponModel.Fresh(PlayerWeaponCatalog.Get(PlayerWeaponCatalog.Shotgun)));
    Check(solo.ModTarget(WeaponMod.Heatsink)==null, "Si ninguna arma lo admite, nadie lo recoge");
    Check(WeaponModModel.Serialize(a.Weapons[0].Mods)=="Stabilizer,Heatsink", "Se guardan los accesorios del arma");
  }

  static void Main(){
    Effects(); Slots(); Model(); Saving(); Targets();
    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
