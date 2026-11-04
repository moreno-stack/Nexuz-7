using System; using System.Reflection; using UnityEngine; using UnityEngine.AI;
using Nexus7.Core; using Nexus7.Enemies;
static class T {
  static int fails=0; static void Check(bool c,string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }
  const BindingFlags F = BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance;
  static void SetF(object o,string f,object v){ var t=o.GetType(); while(t!=null){ var fi=t.GetField(f,F|BindingFlags.DeclaredOnly); if(fi!=null){ fi.SetValue(o,v); return;} t=t.BaseType;} throw new Exception(f); }
  static object GetF(object o,string f){ var t=o.GetType(); while(t!=null){ var fi=t.GetField(f,F|BindingFlags.DeclaredOnly); if(fi!=null) return fi.GetValue(o); t=t.BaseType;} throw new Exception(f); }
  static void Main(){
    Time.time = 100f;
    AlertSystem.ReportPlayerPosition(new Vector3(0,0,28));
    var p = new EnemyPatrol(); p.transform = new Transform(); p.gameObject = new GameObject("Elite");
    var vis = new EnemyVision(); vis.transform = new Transform(); SetF(p,"vision",vis);
    var ag = new NavMeshAgent(); ag.enabled = false; SetF(p,"agent",ag);
    SetF(p,"huntTimer",0.1f);
    p.ResetToStart();
    Check(!AlertSystem.HasRecentSighting(30f), "Al reaparecer, los robots olvidan dónde te vieron (no cazan en el punto de control)");
    Check(Math.Abs((float)GetF(p,"huntTimer") - (float)GetF(p,"huntInterval")) < 1e-4, "La caza en Alerta Roja vuelve a esperar su intervalo completo");
    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
