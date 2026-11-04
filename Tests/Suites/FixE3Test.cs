using System; using System.Collections.Generic; using System.Reflection; using UnityEngine;
using Nexus7.Power; using Nexus7.Core; using Nexus7.Devices; using Nexus7.Inventory; using Nexus7.Player;
static class T {
  static int fails=0; static void Check(bool c,string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }
  const BindingFlags F = BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance;
  static void SetF(object o,string f,object v){ var t=o.GetType(); while(t!=null){ var fi=t.GetField(f,F|BindingFlags.DeclaredOnly); if(fi!=null){ fi.SetValue(o,v); return;} t=t.BaseType;} throw new Exception(f); }
  static void Call(object o,string m){ var t=o.GetType(); while(t!=null){ var mi=t.GetMethod(m,F|BindingFlags.DeclaredOnly,null,Type.EmptyTypes,null); if(mi!=null){ mi.Invoke(o,null); return;} t=t.BaseType;} throw new Exception(m); }
  static void Main(){
    // I1: el reactor usa su propio módulo, que ni los hackeos ni las recetas gastan
    var withModule = new List<(EnergyArea,string)>{ (EnergyArea.South,"battery"), (EnergyArea.Central,"module"), (EnergyArea.WestWing,"battery") };
    Check(EnergyRoomProgress.Solvable(0, 0, withModule), "Con el módulo del reactor se termina aunque se hayan gastado todos los circuitos");
    var onlyCircuit = new List<(EnergyArea,string)>{ (EnergyArea.South,"battery"), (EnergyArea.South,"circuit"), (EnergyArea.WestWing,"battery") };
    Check(!EnergyRoomProgress.Solvable(0, 0, onlyCircuit), "Sin el módulo, los circuitos no arrancan el reactor");
    // I2: el kit de reparación no cuenta como «tipo de dispositivo» del arsenal
    DifficultySettings.Current = Difficulty.Facil; RunStats.Reset(); Time.time = 50f;
    var kit = new DeviceItemData(); SetF(kit,"deviceType",DeviceKind.RepairKit); SetF(kit,"useMode",DeviceUseMode.SelfActivate);
    SetF(kit,"displayName","Kit"); SetF(kit,"maxStack",5); SetF(kit,"category",ItemCategory.Device); kit.name="Kit";
    var inv = new InventorySystem(); SetF(inv,"capacity",6); inv.Add(kit,1);
    var h = new PlayerHealth(); h.transform=new Transform(); h.transform.position=new Vector3(0,0,0); SetF(h,"maxHealth",100f); Call(h,"Awake"); h.TakeDamage(50f);
    var dq = new DeviceQuickbar(); SetF(dq,"inventory",inv); SetF(dq,"health",h);
    typeof(DeviceQuickbar).GetMethod("Activate", F).Invoke(dq, new object[]{ kit });
    Check(RunStats.DistinctDeviceKinds==0 && RunStats.DevicesUsed==1, "El kit cuenta como dispositivo usado pero no como tipo del arsenal");
    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
