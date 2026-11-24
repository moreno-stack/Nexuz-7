using System;
using System.Collections.Generic;
using Nexus7.Combat;
static class T {
  static int fails=0; static void Check(bool c,string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }

  static void Main(){
    var r=EncounterRewardModel.Choose(0.8f,0.9f,2,3);
    Check(r.AmmoPacks==1f && r.Heal==0f && r.Life, "Bien de todo y sin vidas llenas: 1 paquete y +1 vida");
    Check(EncounterRewardModel.Choose(0.5f,1f,2,3).AmmoPacks==2f && EncounterRewardModel.Choose(0.1f,1f,2,3).AmmoPacks==3f, "Más munición cuanto menos te queda (2 por debajo del 60 %, 3 del 30 %)");
    r=EncounterRewardModel.Choose(0.1f,1f,3,3);
    Check(!r.Life && r.AmmoPacks==4f, "Con todas las vidas, un paquete más en lugar de la vida");
    Check(EncounterRewardModel.Choose(1f,0.3f,2,3).Heal==40f && EncounterRewardModel.Choose(1f,0.5f,2,3).Heal==0f, "Reparación de +40 solo por debajo del 50 % de integridad");
    string m=EncounterRewardModel.Message(EncounterRewardModel.Choose(0.1f,0.2f,1,3));
    Check(m.StartsWith("Zona despejada") && m.Contains("3 paquetes") && m.Contains("+40") && m.Contains("+1 vida"), "El mensaje lista lo que da");
    Check(EncounterRewardModel.Message(new EncounterReward{AmmoPacks=1f}).Contains("1 paquete de munición"), "Singular con 1 paquete");
    var pistol=PlayerWeaponCatalog.Get(PlayerWeaponCatalog.Pistol);
    float full=EncounterRewardModel.AmmoFraction(new List<WeaponModel>{ new WeaponModel(pistol,pistol.MagazineSize,pistol.MaxReserve) });
    float low=EncounterRewardModel.AmmoFraction(new List<WeaponModel>{ new WeaponModel(pistol,0,10) });
    Check(Math.Abs(full-1f)<0.01f && low<EncounterRewardModel.LowAmmo, "Fracción de munición: cargador + reserva de todas las armas");
    Check(EncounterRewardModel.AmmoFraction(new List<WeaponModel>())==1f && EncounterRewardModel.AmmoFraction(null)==1f, "Sin armas no pide munición");
    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
