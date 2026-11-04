using System; using System.Reflection; using UnityEngine;
using Nexus7.Core; using Nexus7.Gameplay; using Nexus7.Player; using Nexus7.Devices; using Nexus7.Interaction; using Nexus7.Inventory;
static class T {
  static int fails=0; static void Check(bool c,string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }
  static bool Near(float a,float b,float e=0.001f){ return Math.Abs(a-b)<=e; }
  static ScoreInput Ghost(){ return new ScoreInput{ detections=0, alarms=0, redAlerts=0, captures=0, loudNoise=0f, resourceValue=450f,
      itemsCollected=30, itemsCrafted=3, devicesUsed=3, totalSeconds=1300f, evacuationRemaining=150f, distinctDeviceKinds=3,
      hacksSucceeded=6, hacksFailed=0, sectorsDisabled=2, empTargetsHit=3, decoysTriggered=2, deaths=0, escaped=true }; }

  static void DifficultyMultipliers(){
    DifficultySettings.Current = Difficulty.Facil;
    Check(DifficultySettings.DamageMultiplier==1f && DifficultySettings.ScoreMultiplier==1f, "Fácil: daño ×1, puntuación ×1");
    DifficultySettings.Current = Difficulty.Normal;
    Check(Near(DifficultySettings.DamageMultiplier,1.5f) && Near(DifficultySettings.ShieldRegenMultiplier,0.8f) && Near(DifficultySettings.DetectionMultiplier,1.15f) && Near(DifficultySettings.ScoreMultiplier,1.15f), "Normal: ×1.5 / 0.8 / 1.15 / 1.15");
    DifficultySettings.Current = Difficulty.Dificil;
    Check(Near(DifficultySettings.DamageMultiplier,2f) && Near(DifficultySettings.ShieldRegenMultiplier,0.6f) && Near(DifficultySettings.DetectionMultiplier,1.3f), "Difícil: ×2 / 0.6 / 1.3");
    Check(PlayerPrefs.GetInt("Nexus7_Dificultad",-1)==(int)Difficulty.Dificil, "La dificultad se guarda en PlayerPrefs");
    Check(DifficultySettings.DisplayName=="Difícil", "Nombre visible 'Difícil'");
    DifficultySettings.Current = Difficulty.Facil;
  }
  static void ScoreAppliesDifficulty(){
    var g = Ghost(); g.difficultyMultiplier = 1.3f; var r = ScoreCalculator.Calculate(g);
    int expected = (int)Math.Round(4245 * (double)1.3f, MidpointRounding.AwayFromZero);
    Check(r.total==expected && r.grade=="S", "Puntuación ×1.3 en Difícil ("+r.total+" = "+expected+")");
    var g0 = Ghost(); g0.difficultyMultiplier = 0f; Check(ScoreCalculator.Calculate(g0).total==4245, "Multiplicador 0 se trata como 1 (4245)");
  }

  const BindingFlags F = BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance;
  static void SetF(object o,string f,object v){ var t=o.GetType(); while(t!=null){ var fi=t.GetField(f,F|BindingFlags.DeclaredOnly); if(fi!=null){ fi.SetValue(o,v); return;} t=t.BaseType;} throw new Exception(f); }
  static void Call(object o,string m){ var t=o.GetType(); while(t!=null){ var mi=t.GetMethod(m,F|BindingFlags.DeclaredOnly,null,Type.EmptyTypes,null); if(mi!=null){ mi.Invoke(o,null); return;} t=t.BaseType;} throw new Exception(m); }
  static PlayerHealth NewHealth(SuitShield shield){ var h=new PlayerHealth(); h.transform=new Transform(); h.transform.position=new Vector3(0,0,0); SetF(h,"maxHealth",100f); if(shield!=null) SetF(h,"shield",shield); Call(h,"Awake"); return h; }

  static void ShieldAbsorbsFirst(){
    var m = new ShieldModel(50f);
    float r1 = m.Absorb(30f, 0f); Check(r1==0f && m.Value==20f, "Escudo absorbe 30 de 50 → resto 0, escudo 20");
    float r2 = m.Absorb(30f, 0f); Check(r2==10f && m.Value==0f, "Escudo con 20 recibe 30 → resto 10, escudo 0");
  }
  static void OverflowGoesToHealthOnce(){
    DifficultySettings.Current = Difficulty.Facil; RunStats.Reset(); Time.time = 100f;
    var sh = new SuitShield(); Call(sh,"Awake"); sh.Equip();
    var h = NewHealth(sh); int dmgEvents=0; float dmgSum=0f; h.Damaged += d => { dmgEvents++; dmgSum+=d; };
    h.TakeDamage(70f, new Vector3(1,0,1));
    Check(Near(h.Current,80f) && sh.Model.Value==0f, "70 de daño con escudo 50 → vida 80, escudo 0 ("+h.Current+")");
    Check(Near(RunStats.DamageTaken,20f) && dmgEvents==1 && Near(dmgSum,20f), "Daño a la vida contado una vez (20)");
    var h2 = NewHealth(null); h2.TakeDamage(-5f, new Vector3(0,0,0)); Check(h2.Current==100f, "Daño negativo ignorado");
  }
  static void RegenAfterSixSeconds(){
    var m = new ShieldModel(50f); m.Absorb(50f, 0f);
    float t=0f; while(t<5.9f){ m.Tick(0.1f, t, 1f); t+=0.1f; }
    Check(m.Value==0f, "Sin recarga antes de 6 s");
    var a = new ShieldModel(50f); a.Absorb(50f,0f); a.Tick(1f, 7f, 1f); Check(Near(a.Value,8f), "Recarga 8/s tras 6 s ("+a.Value+")");
    var b = new ShieldModel(50f); b.Absorb(50f,0f); b.Tick(1f, 7f, 0.6f); Check(Near(b.Value,4.8f), "Recarga ×0.6 en Difícil ("+b.Value+")");
    var c = new ShieldModel(50f); c.Absorb(10f,0f); c.Tick(100f, 200f, 1f); Check(c.Value==50f, "La recarga no supera la capacidad");
  }
  static void DifficultyScalesEnemyDamage(){
    DifficultySettings.Current = Difficulty.Dificil; Time.time = 300f;
    var h = NewHealth(null); h.TakeDamage(25f, new Vector3(2,0,0));
    Check(Near(h.Current,50f), "Difícil: golpe de bate 25 → 50 de daño ("+h.Current+")");
    DifficultySettings.Current = Difficulty.Facil;
  }
  static void NewRunResetsSuit(){
    GameSession.HasSuit = true; int s0 = GameSession.RunSeed; GameSession.NewRun();
    Check(!GameSession.HasSuit, "Nueva partida: sin traje");
  }

  static void SuitEquipsShield(){
    GameSession.NewRun();
    var sh = new SuitShield(); Call(sh,"Awake");
    var suit = new PlayerSuit(); SetF(suit,"shield",sh);
    var pick = new SuitPickup(); pick.gameObject = new GameObject("Taquilla"); SetF(pick,"playerSuit",suit);
    pick.Interact(null);
    Check(GameSession.HasSuit && sh.Equipped && sh.Model.Value==50f, "Taquilla: traje puesto y escudo a 50");
  }
  static void RepairKitHeals(){
    DifficultySettings.Current = Difficulty.Facil; Time.time = 900f;
    var kit = new DeviceItemData(); SetF(kit,"deviceType",DeviceKind.RepairKit); SetF(kit,"useMode",DeviceUseMode.SelfActivate);
    SetF(kit,"displayName","Kit de reparación"); SetF(kit,"maxStack",5); SetF(kit,"category",ItemCategory.Device); kit.name="Kit";
    var inv = new InventorySystem(); SetF(inv,"capacity",6); inv.Add(kit,1);
    var h = NewHealth(null); h.TakeDamage(70f);
    var dq = new DeviceQuickbar(); SetF(dq,"inventory",inv); SetF(dq,"health",h);
    var mi = typeof(DeviceQuickbar).GetMethod("Activate", F); mi.Invoke(dq, new object[]{ kit });
    Check(Near(h.Current,70f) && inv.GetCount(kit)==0, "Kit de reparación: +40 de vida y se gasta ("+h.Current+")");
    inv.Add(kit,1); var full = NewHealth(null); SetF(dq,"health",full); mi.Invoke(dq, new object[]{ kit });
    Check(inv.GetCount(kit)==1, "Con la vida al máximo el kit no se gasta");
  }
  static void DoorRequiresSuit(){
    var door = new DoorController(); door.transform = new Transform(); door.transform.localPosition = new Vector3(0,0,0);
    SetF(door,"startLocked",false); SetF(door,"requiresSuit",true); Call(door,"Awake");
    GameSession.HasSuit = false; Time.time = 950f; door.Interact(null);
    Check(!door.IsOpen, "Sin traje la puerta no se abre");
    GameSession.HasSuit = true; door.Interact(null);
    Check(door.IsOpen, "Con traje la puerta se abre");
  }

  static void Main(){
    DifficultyMultipliers();
    ScoreAppliesDifficulty();
    ShieldAbsorbsFirst(); OverflowGoesToHealthOnce(); RegenAfterSixSeconds(); DifficultyScalesEnemyDamage(); NewRunResetsSuit(); SuitEquipsShield(); RepairKitHeals(); DoorRequiresSuit();
    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
