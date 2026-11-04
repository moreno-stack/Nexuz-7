using System; using System.Linq; using System.Reflection; using UnityEngine;
using Nexus7.Core; using Nexus7.Player; using Nexus7.Tutorial; using Nexus7.UI; using Nexus7.Interaction;
static class T {
  static int fails=0; static void Check(bool c,string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }
  const BindingFlags F = BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance;
  static void SetF(object o,string f,object v){ var t=o.GetType(); while(t!=null){ var fi=t.GetField(f,F|BindingFlags.DeclaredOnly); if(fi!=null){ fi.SetValue(o,v); return;} t=t.BaseType;} throw new Exception(f); }
  static object GetF(object o,string f){ var t=o.GetType(); while(t!=null){ var fi=t.GetField(f,F|BindingFlags.DeclaredOnly); if(fi!=null) return fi.GetValue(o); t=t.BaseType;} throw new Exception(f); }
  static void Call(object o,string m){ var t=o.GetType(); while(t!=null){ var mi=t.GetMethod(m,F|BindingFlags.DeclaredOnly,null,Type.EmptyTypes,null); if(mi!=null){ mi.Invoke(o,null); return;} t=t.BaseType;} throw new Exception(m); }

  static void Main(){
    // I1: el paso de salto avisa de ponerse de pie (agacharse es un interruptor)
    var jump = TutorialSteps.Phase1.First(s=>s.id=="jump").hint;
    Check(jump.Contains("de pie") && jump.Contains("[Espacio]"), "Pista de salto: ponte de pie y pulsa [Espacio]");
    // I2: no se reconstruye el modelo si ya lleva el traje
    Check(!PlayerSuit.NeedsRebuild(false, true) && PlayerSuit.NeedsRebuild(false, false) && !PlayerSuit.NeedsRebuild(true, false), "PlayerSuit.NeedsRebuild: solo sin traje en el modelo");
    // I3: destello azul al ponerse el traje
    Time.deltaTime = 0.016f;
    var sh = new SuitShield(); Call(sh,"Awake");
    var hud = new ShieldHealthHUD(); SetF(hud,"shield",sh); Call(hud,"Update");
    float f0 = (float)GetF(hud,"flash"); sh.Equip(); Call(hud,"Update");
    Check(f0==0f && (float)GetF(hud,"flash")>0.5f, "Al ponerse el traje la pantalla destella en azul");
    // I4: volver al menú / Clear quita el traje; la taquilla vacía si ya lo llevas
    GameSession.HasSuit = true; GameSession.Clear(); Check(!GameSession.HasSuit, "GameSession.Clear reinicia el traje");
    GameSession.HasSuit = true; var pick = new SuitPickup(); pick.gameObject=new GameObject("Taquilla"); var vis = new GameObject("Traje"); vis.activeSelf=true; SetF(pick,"suitVisual",vis);
    Call(pick,"Start"); Check((bool)GetF(pick,"taken") && !vis.activeSelf, "Con el traje ya puesto la taquilla aparece vacía");
    GameSession.NewRun();
    // I6: barra de saltar solo visible durante la cinemática
    var sub = new SubtitleUI(); var bar = new GameObject("BarraSaltar"); bar.activeSelf=true; SetF(sub,"skipBar",bar); Call(sub,"Awake");
    bool hidden = !bar.activeSelf; sub.SetLetterbox(true); bool shown = bar.activeSelf; sub.SetLetterbox(false);
    Check(hidden && shown && !bar.activeSelf, "La barra de saltar solo se ve durante la cinemática");
    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
