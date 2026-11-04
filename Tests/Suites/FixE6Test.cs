using System; using System.IO; using System.Linq; using System.Reflection; using UnityEngine;
using Nexus7.Cinematics; using Nexus7.Core; using Nexus7.Gameplay; using Nexus7.Player;
static class T {
  static int fails=0; static void Check(bool c,string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }
  const BindingFlags F = BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance;
  static void SetF(object o,string f,object v){ var t=o.GetType(); while(t!=null){ var fi=t.GetField(f,F|BindingFlags.DeclaredOnly); if(fi!=null){ fi.SetValue(o,v); return;} t=t.BaseType;} throw new Exception(f); }
  static void Call(object o,string m){ var t=o.GetType(); while(t!=null){ var mi=t.GetMethod(m,F|BindingFlags.DeclaredOnly,null,Type.EmptyTypes,null); if(mi!=null){ mi.Invoke(o,null); return;} t=t.BaseType;} throw new Exception(m); }
  static float Dist(Vector3 a, Vector3 b){ return (float)Math.Sqrt((a.x-b.x)*(a.x-b.x)+(a.y-b.y)*(a.y-b.y)+(a.z-b.z)*(a.z-b.z)); }
  static bool Outside(System.Collections.Generic.List<CinematicShot> s, int shot, Vector3 c, float scale){ return Dist(s[shot].fromPos,c) > scale/2f+2f && Dist(s[shot].toPos,c) > scale/2f+2f; }
  static void Main(){
    // I1: la explosión nunca engulle la cámara
    Check(Outside(CinematicScripts.Despegue(), 1, CinematicScripts.DespegueBlastCenter, CinematicScripts.DespegueBlastScale), "Despegue: la cámara queda fuera de la explosión");
    Check(Outside(CinematicScripts.Derrota(), 0, CinematicScripts.DerrotaBlastCenter, CinematicScripts.DerrotaBlastScale), "Derrota: la cámara queda fuera de la explosión");
    // I2: las cinemáticas de final no devuelven la cámara al hangar
    var player = new PlayerController(); var parent = new Transform(); var cam = new Transform(); cam.parent = parent;
    var d = new CinematicDirector(); d.transform = new Transform(); d.gameObject = new GameObject("Fin");
    SetF(d,"player",player); SetF(d,"cameraTransform",cam); SetF(d,"returnToPlayer",false);
    d.Begin(); d.End();
    Check(cam.parent == null && !CinematicDirector.IsPlaying && player.InputEnabled, "Final: la cámara se queda fuera (no vuelve al hangar) y no queda ningún bloqueo");
    var d2 = new CinematicDirector(); d2.transform = new Transform(); d2.gameObject = new GameObject("Intro"); var cam2 = new Transform(); cam2.parent = parent;
    SetF(d2,"cameraTransform",cam2); d2.Begin(); d2.End();
    Check(cam2.parent == parent, "Cinemática normal: la cámara vuelve al jugador");
    // I4: la alerta amarilla se activa al empezar la cuenta, no durante la cinemática de llegada
    AlertSystem.ResetAll(); Time.time = 10f; Time.deltaTime = 0.02f;
    var cd = new EvacuationCountdown(); cd.transform = new Transform(); cd.gameObject = new GameObject("Cuenta");
    var intro = new CinematicDirector(); intro.transform = new Transform(); intro.gameObject = new GameObject("Llegada"); intro.Begin();
    Call(cd,"Start"); Call(cd,"Update");
    bool calmDuring = AlertSystem.Level == AlertLevel.Normal;
    intro.End(); Call(cd,"Update");
    Check(calmDuring && AlertSystem.Level == AlertLevel.Yellow, "Alerta amarilla al empezar la cuenta atrás (no se gasta durante la cinemática)");
    // I6 + temblores: textos y pausas
    string root = "Assets/_Nexus7/Scripts/";
    Check(!File.ReadAllText(root+"Gameplay/EndingManager.cs").Contains("drones"), "Historias de los finales sin «drones»");
    Check(File.ReadAllText(root+"Environment/ShipTremor.cs").Contains("CinematicDirector.IsPlaying"), "Los temblores de la nave esperan a que acabe la cinemática");
    AlertSystem.ResetAll();
    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
