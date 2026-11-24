using System;
using System.Collections.Generic;
using Nexus7.Combat;
using Nexus7.Director;
static class T {
  static int fails=0; static void Check(bool c,string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }

  // Jugador «neutro»: lo ven a menudo, precisión normal, ritmo medio, sin arma caliente favorita
  static RewardInput Neutral(){ return new RewardInput{ Accuracy=0.4f, Shots=20, Camping=0.1f, DetectionsPerMinute=2f, KillsPerMinute=0.5f, HotWeaponFavoured=false }; }

  static WeaponMod Pick(RewardInput i, WeaponMod last=WeaponMod.None, int seed=1, Func<WeaponMod,bool> usable=null){
    return ModRewardModel.Choose(i, usable ?? (m => true), last, new Random(seed));
  }

  static bool Always(RewardInput i, WeaponMod expected, WeaponMod last=WeaponMod.None){
    for (int s=0; s<50; s++) if (Pick(i,last,s)!=expected) return false;
    return true;
  }

  static void ByStyle(){
    var ghost = Neutral(); ghost.DetectionsPerMinute=0.2f;
    Check(Always(ghost, WeaponMod.Suppressor), "Sigiloso (≤ 0,5 detecciones/min): supresor");
    var camp = Neutral(); camp.Camping=0.6f;
    Check(Always(camp, WeaponMod.Suppressor), "Atrincherado (acampada ≥ 50 %): supresor");
    var miss = Neutral(); miss.Accuracy=0.2f;
    Check(Always(miss, WeaponMod.Stabilizer), "Falla mucho (< 30 %): estabilizador");
    var fewShots = Neutral(); fewShots.Accuracy=0.1f; fewShots.Shots=5;
    Check(!ModRewardModel.Fits(WeaponMod.Stabilizer, fewShots), "Con menos de 8 disparos no juzga la precisión");
    var aggro = Neutral(); aggro.KillsPerMinute=2f;
    Check(Always(aggro, WeaponMod.ExtendedMag), "Agresivo (≥ 1,5 bajas/min): cargador ampliado");
    var hot = Neutral(); hot.HotWeaponFavoured=true;
    Check(Always(hot, WeaponMod.Heatsink), "Usa sobre todo un arma que se calienta: disipador");
  }

  static void Variety(){
    var seen = new HashSet<WeaponMod>();
    for (int s=0; s<200; s++) seen.Add(Pick(Neutral(), WeaponMod.None, s));
    Check(seen.Count==4, "Si nada destaca, cualquiera de los 4 puede salir");
    var miss = Neutral(); miss.Accuracy=0.2f;
    Check(Always(miss, WeaponMod.Stabilizer, WeaponMod.Stabilizer), "Si encaja de verdad, lo repite aunque sea el de la última vez (40 > 25)");
    int repeats=0; for (int s=0; s<200; s++) if (Pick(Neutral(), WeaponMod.Suppressor, s)==WeaponMod.Suppressor) repeats++;
    Check(repeats==0, "Si nada destaca, no repite el de la última vez");
  }

  static void Usable(){
    var ghost = Neutral(); ghost.DetectionsPerMinute=0.2f;
    Check(Pick(ghost, WeaponMod.None, 3, m => m!=WeaponMod.Suppressor)!=WeaponMod.Suppressor, "Nunca da uno que ninguna arma pueda montar");
    Check(Pick(ghost, WeaponMod.None, 3, m => false)==WeaponMod.None, "Si no cabe ninguno, no hay recompensa");
    Check(ModRewardModel.Reason(WeaponMod.Stabilizer, new RewardInput{ Accuracy=0.1f, Shots=20, DetectionsPerMinute=3f })=="fallas muchos disparos", "Explica por qué lo da");
    Check(ModRewardModel.Reason(WeaponMod.ExtendedMag, Neutral())=="para variar tu equipo", "Si no encaja, lo dice");
  }

  static void Main(){
    ByStyle(); Variety(); Usable();
    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
