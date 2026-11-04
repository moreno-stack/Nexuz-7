using System; using System.Reflection; using UnityEngine;
using Nexus7.Core; using Nexus7.Cinematics; using Nexus7.Enemies; using Nexus7.Gameplay; using Nexus7.Player; using Nexus7.UI; using Nexus7.Inventory;
static class T {
  static int fails=0; static void Check(bool c,string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }
  const BindingFlags F = BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance;
  static void SetF(object o,string f,object v){ var t=o.GetType(); while(t!=null){ var fi=t.GetField(f,F|BindingFlags.DeclaredOnly); if(fi!=null){ fi.SetValue(o,v); return;} t=t.BaseType;} throw new Exception(f); }
  static void Call(object o,string m){ var t=o.GetType(); while(t!=null){ var mi=t.GetMethod(m,F|BindingFlags.DeclaredOnly,null,Type.EmptyTypes,null); if(mi!=null){ mi.Invoke(o,null); return;} t=t.BaseType;} throw new Exception(m); }
  static PlayerHealth NewHealth(){ var h=new PlayerHealth(); h.transform=new Transform(); h.transform.position=new Vector3(0,0,0); SetF(h,"maxHealth",100f); Call(h,"Awake"); SetF(h,"controller",new PlayerController()); return h; }

  static void HitInterruptsHackWithoutHangingLock(){
    DifficultySettings.Current = Difficulty.Facil; Time.time = 10f;
    var ui = new HackMinigameUI(); SetF(ui,"panel",new GameObject("p")); Call(ui,"OnEnable");
    var pc = new PlayerController(); object owner = new object(); pc.AddInputLock(owner);
    int calls=0; bool result=true;
    SetF(ui,"sequence", new Nexus7.Security.HackSequence(Nexus7.Security.HackDifficulty.Easy, 1, 1f, 0));
    SetF(ui,"onDone", (Action<bool>)(ok => { calls++; result=ok; pc.RemoveInputLock(owner); }));
    var h = NewHealth(); h.TakeDamage(10f, new Vector3(1,0,0));
    Call(ui,"OnDisable");
    Check(calls==1 && !result, "Un golpe no letal interrumpe el hackeo y avisa al dueño (callback false)");
    Check(pc.InputEnabled && !ui.IsActive, "No queda ningún bloqueo de entrada colgado");
  }
  static void ShieldRefillOnRespawn(){
    var sh = new SuitShield(); Call(sh,"Awake"); sh.Equip(); sh.Absorb(50f);
    Call(sh,"OnRespawned");
    Check(sh.Model.Value==50f, "Al reaparecer el escudo se rellena");
  }
  static void CinematicCleansUpOnDisable(){
    var d = new CinematicDirector(); d.transform=new Transform(); d.gameObject=new GameObject("Dir"); SetF(d,"player",new PlayerController());
    d.Begin(); Call(d,"OnDisable");
    Check(!CinematicDirector.IsPlaying && !EnemyPatrol.Paused, "Si la escena se descarga a mitad de cinemática, los robots no quedan pausados");
    EnemyPatrol.Paused = true; GameFlow.ResetRunState();
    Check(!EnemyPatrol.Paused, "Nueva partida / menú: pausa de robots reiniciada");
  }
  static void LootZeroPointsKeepsGuaranteed(){
    var it = new ItemData(); it.name="Bateria";
    var r = LootRoller.Roll(1, 0, new float[0], new System.Collections.Generic.List<ItemStack>{ new ItemStack(it,2) }, null);
    Check(r.Count==1 && r[0].point==-1 && r[0].guaranteed, "Sala sin puntos: el garantizado se coloca en el propio spawner (punto -1)");
  }
  static void Main(){
    HitInterruptsHackWithoutHangingLock(); ShieldRefillOnRespawn(); CinematicCleansUpOnDisable(); LootZeroPointsKeepsGuaranteed();
    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
