using System; using System.Collections.Generic; using System.Reflection; using UnityEngine;
using Nexus7.Cinematics; using Nexus7.Enemies; using Nexus7.Player;
static class T {
  static int fails=0; static void Check(bool c,string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }
  static bool Near(float a,float b){ return Math.Abs(a-b)<0.001f; }
  const BindingFlags F = BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance;
  static void SetF(object o,string f,object v){ var t=o.GetType(); while(t!=null){ var fi=t.GetField(f,F|BindingFlags.DeclaredOnly); if(fi!=null){ fi.SetValue(o,v); return;} t=t.BaseType;} throw new Exception(f); }
  static object GetF(object o,string f){ var t=o.GetType(); while(t!=null){ var fi=t.GetField(f,F|BindingFlags.DeclaredOnly); if(fi!=null) return fi.GetValue(o); t=t.BaseType;} throw new Exception(f); }

  static void TimelineEvaluate(){
    var tl = new CinematicTimeline(new List<float>{ 2f, 3f });
    int s; float k;
    tl.Evaluate(1f, out s, out k); Check(s==0 && Near(k,0.5f), "t=1 → plano 0 a la mitad");
    tl.Evaluate(3.5f, out s, out k); Check(s==1 && Near(k,0.5f), "t=3,5 → plano 1 a la mitad");
    Check(tl.IsFinished(5f) && !tl.IsFinished(4.9f) && Near(tl.Total,5f), "Termina a los 5 s");
    tl.Evaluate(9f, out s, out k); Check(s==1 && Near(k,1f), "Pasado el final se queda en el último fotograma");
  }
  static void SkipHoldTest(){
    var h = new SkipHold(1f);
    bool done=false; for(int i=0;i<9;i++) done |= h.Update(true, 0.1f); Check(!done, "Mantener 0,9 s no salta");
    Check(h.Update(true, 0.1f), "Mantener 1 s salta");
    var g = new SkipHold(1f); g.Update(true,0.5f); g.Update(false,0.1f); Check(Near(g.Progress,0f), "Soltar reinicia el progreso");
  }
  static void SubtitleTiming(){
    var lines = new List<SubtitleLine>{ new SubtitleLine("IA NEXUS-7","Hola",1f,2f), new SubtitleLine("Tú","Adiós",3.5f,1f) };
    Check(CinematicTimeline.ActiveLine(lines, 1.5f)==0, "Subtítulo visible a 1,5 s");
    Check(CinematicTimeline.ActiveLine(lines, 3.1f)==-1, "Oculto entre líneas (3,1 s)");
    Check(CinematicTimeline.ActiveLine(lines, 4f)==1, "Segunda línea a 4 s");
    Check(CinematicTimeline.ActiveLine(lines, 0.5f)==-1, "Nada antes de la primera");
  }
  static void SkipAtEndFiresOnce(){
    var d = new CinematicDirector(); d.transform = new Transform(); d.gameObject = new GameObject("Dir");
    var pc = new PlayerController(); SetF(d,"player",pc);
    int finished=0; d.Finished += () => finished++;
    d.Begin(); Check(CinematicDirector.IsPlaying && EnemyPatrol.Paused && !pc.InputEnabled, "Durante la cinemática: jugador bloqueado y robots en pausa");
    d.Skip(); d.Skip(); d.End();
    Check(finished==1, "Finished una sola vez ("+finished+")");
    Check(!CinematicDirector.IsPlaying && !EnemyPatrol.Paused && pc.InputEnabled, "Al terminar: bloqueo liberado y robots activos");
  }
  static void Main(){
    TimelineEvaluate(); SkipHoldTest(); SubtitleTiming(); SkipAtEndFiresOnce();
    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
