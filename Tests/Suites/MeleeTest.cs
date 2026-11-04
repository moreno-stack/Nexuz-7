using System; using System.Reflection; using UnityEngine;
using Nexus7.Core; using Nexus7.Enemies; using Nexus7.Gameplay; using Nexus7.Player;
static class T {
  static int fails=0; static void Check(bool c,string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }
  const BindingFlags F = BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance;
  static void SetF(object o,string f,object v){ var t=o.GetType(); while(t!=null){ var fi=t.GetField(f,F|BindingFlags.DeclaredOnly); if(fi!=null){ fi.SetValue(o,v); return;} t=t.BaseType;} throw new Exception(f); }
  static void Call(object o,string m){ var t=o.GetType(); while(t!=null){ var mi=t.GetMethod(m,F|BindingFlags.DeclaredOnly,null,Type.EmptyTypes,null); if(mi!=null){ mi.Invoke(o,null); return;} t=t.BaseType;} throw new Exception(m); }

  static MeleeAttackModel Bat(){ return new MeleeAttackModel(0.55f, 1.2f); }

  static void WindupThenHit(){
    var m = Bat(); Check(m.Begin(0f) && m.Current==MeleeAttackModel.Phase.Windup, "Empieza la anticipación");
    Check(!m.Begin(0.1f), "No se puede empezar otro golpe a mitad");
    Check(m.Tick(0.5f,true,true)==MeleeEvent.None, "Durante la anticipación no hay golpe");
    Check(m.Tick(0.56f,true,true)==MeleeEvent.Hit && m.Current==MeleeAttackModel.Phase.Recovery, "Al terminar la anticipación golpea");
    Check(m.Tick(1.0f,true,true)==MeleeEvent.None, "Un solo impacto por golpe");
    Check(m.Tick(1.76f,true,true)==MeleeEvent.Finished && m.Current==MeleeAttackModel.Phase.Idle, "Tras la recuperación (1,2 s) termina");
  }
  static void DodgeIfOutOfRange(){
    var m = Bat(); m.Begin(0f);
    Check(m.Tick(0.56f,true,false)==MeleeEvent.Dodged, "Fuera de alcance al golpear → esquivado");
    var n = Bat(); n.Begin(0f); Check(n.Tick(0.56f,false,true)==MeleeEvent.Dodged, "Fuera del arco → esquivado");
  }
  static void CancelByEmp(){
    var m = Bat(); m.Begin(0f); m.Cancel();
    Check(m.Current==MeleeAttackModel.Phase.Idle && m.Tick(0.6f,true,true)==MeleeEvent.None, "PEM cancela el golpe");
  }
  static void ArcCheck(){
    Check(MeleeAttackModel.InArc(30f,70f) && !MeleeAttackModel.InArc(40f,70f), "Arco de 70°: 30° dentro, 40° fuera");
  }
  static void HitClosesOpenUi(){
    DifficultySettings.Current = Difficulty.Facil; Time.time = 50f;
    var h = new PlayerHealth(); h.transform=new Transform(); h.transform.position=new Vector3(0,0,0); h.gameObject = new GameObject("P");
    var r = new PlayerRespawn(); r.transform = h.transform; SetF(h,"respawn",r); SetF(h,"maxHealth",100f); Call(h,"Awake"); SetF(h,"respawn",r);
    var ctrl = new PlayerController(); SetF(r,"controller",ctrl); SetF(h,"controller",ctrl);
    int caught=0; Action a = () => caught++; PlayerRespawn.PlayerCaught += a;
    h.TakeDamage(60f, new Vector3(1,0,0)); h.TakeDamage(60f, new Vector3(1,0,0));
    PlayerRespawn.PlayerCaught -= a;
    Check(caught==1, "Fuera de combate: PlayerCaught una sola vez (cierra interfaces) ("+caught+")");
    Check(h.LastDamageFromEnemy, "El último daño vino de un enemigo (cuenta como captura)");
  }
  static void Main(){
    WindupThenHit(); DodgeIfOutOfRange(); CancelByEmp(); ArcCheck(); HitClosesOpenUi();
    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
