using System; using System.Reflection; using UnityEngine; using UnityEngine.AI;
using Nexus7.Core; using Nexus7.Security; using Nexus7.Enemies;
static class T {
  const BindingFlags F = BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance;
  static FieldInfo Fi(object o,string f){ var t=o.GetType(); while(t!=null){ var fi=t.GetField(f,F|BindingFlags.DeclaredOnly); if(fi!=null) return fi; t=t.BaseType;} throw new Exception(f); }
  static void Set(object o,string f,object v)=>Fi(o,f).SetValue(o,v);
  static void Call(object o,string m,params object[] a){ var t=o.GetType(); while(t!=null){ var mi=t.GetMethod(m,F|BindingFlags.DeclaredOnly); if(mi!=null){ mi.Invoke(o,a); return;} t=t.BaseType;} throw new Exception(m); }
  static int fails=0; static void Check(bool c,string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }
  static EnemyPatrol Drone(out NavMeshAgent ag){ var d=new EnemyPatrol(); d.transform=new Transform(); ag=new NavMeshAgent(); ag.isOnNavMesh=true; Set(d,"agent",ag); Set(d,"vision",new EnemyVision()); return d; }
  static void Main(){
    AlertSystem.ResetAll(); Time.time=0f;
    int changes=0; AlertSystem.LevelChanged += (x1,x2)=>changes++;
    AlertSystem.Report(AlertEvent.HackError); AlertSystem.Report(AlertEvent.HackError);
    Check(AlertSystem.Level==AlertLevel.Normal && Math.Abs(AlertSystem.Heat-10f)<0.01f, "2 errores de hackeo → 10 de tensión, sigue Normal");
    AlertSystem.Report(AlertEvent.AlarmRaised, new Vector3(1,0,1));
    Check(AlertSystem.Level==AlertLevel.Normal && AlertSystem.Heat==32f && AlertSystem.Stats.alarms==1, "+ alarma (22) → 32: aún Normal (umbral 35)");
    AlertSystem.Report(AlertEvent.PlayerSpotted); AlertSystem.Report(AlertEvent.HackFailed);
    Check(AlertSystem.Level==AlertLevel.Yellow && AlertSystem.Heat==65f, "+ detección (15) + hackeo fallido (18) → 65: Amarilla");
    AlertSystem.Report(AlertEvent.AlarmRaised, new Vector3(1,0,1));
    Check(AlertSystem.Level==AlertLevel.Red && AlertSystem.Heat==87f, "+ otra alarma → 87: ALERTA ROJA");
    Check(AlertSystem.HasRecentSighting(5f), "La alarma registró la última posición conocida del jugador");
    // disipación: pausa de 8 s
    Time.time=5f; float h=AlertSystem.Heat; AlertSystem.Tick(1f); Check(AlertSystem.Heat==h, "Sin disipación durante 8 s tras un suceso");
    Time.time=20f; AlertSystem.ReduceHeat(0f); // no-op
    // bajar el calor por debajo de 50 antes de 25 s en roja → se mantiene roja
    AlertSystem.AddHeat(-0f);
    float before=AlertSystem.Heat;
    Time.time=10f; for(int i=0;i<60;i++){ AlertSystem.Tick(1f); }
    Check(AlertSystem.Level==AlertLevel.Red || Time.time-0<25f, "Roja con mínimo de 25 s");
    Time.time=200f; for(int i=0;i<200;i++) AlertSystem.Tick(1f);
    Check(AlertSystem.Level!=AlertLevel.Red, "Con el tiempo la tensión se disipa y sale de la Roja (calor "+AlertSystem.Heat.ToString("0")+")");
    // histéresis en amarilla
    AlertSystem.ResetAll(); Time.time=1000f; AlertSystem.AddHeat(40f);
    Check(AlertSystem.Level==AlertLevel.Yellow, "40 → Amarilla");
    AlertSystem.AddHeat(-15f); Check(AlertSystem.Level==AlertLevel.Yellow, "25 → sigue Amarilla (histéresis, sale por debajo de 20)");
    AlertSystem.AddHeat(-6f); Check(AlertSystem.Level==AlertLevel.Normal, "19 → Normal");
    AlertSystem.ForceLevel(AlertLevel.Red); Time.time=1001f; AlertSystem.ReduceHeat(40f);
    Check(AlertSystem.Level==AlertLevel.Yellow, "Reiniciar protocolo (−40) saca de la Roja sin esperar el mínimo");
    Check(Math.Abs(AlertSystem.Profile.speed-1.15f)<0.001f, "Perfil Amarilla: drones ×1.15 de velocidad");
    // Minijuego
    var s=new HackSequence(HackDifficulty.Medium, 42);
    Check(s.Symbols.Length==7 && s.MaxErrors==3 && Math.Abs(s.TimeLimit-12f)<0.01f, "Media: 7 códigos, 12 s, 3 errores");
    var s2=new HackSequence(HackDifficulty.Medium, 42); bool same=true; for(int i=0;i<7;i++) same&=s.Symbols[i]==s2.Symbols[i];
    Check(same, "Misma semilla → misma secuencia (reproducible)");
    Check(s.Input(s.Symbols[0])==HackSequence.StepResult.Correct && s.Index==1, "Acierto avanza");
    var wrong=(HackSymbol)(((int)s.Symbols[1]+1)%4);
    Check(s.Input(wrong)==HackSequence.StepResult.Wrong && Math.Abs(s.TimeLeft-9f)<0.01f, "Error: −3 s de penalización");
    for(int i=1;i<7;i++) s.Input(s.Symbols[i]);
    Check(s.Succeeded, "Completar la secuencia → éxito");
    var h3=new HackSequence(HackDifficulty.Hard, 7); var w=(HackSymbol)(((int)h3.Symbols[0]+1)%4);
    h3.Input(w); h3.Input(w); Check(!h3.Failed, "Difícil: 2 errores permitidos");
    Check(h3.Input(w)==HackSequence.StepResult.Failed, "3er error → fallo");
    var t=new HackSequence(HackDifficulty.Easy, 1, 0.8f, 1);
    Check(t.Symbols.Length==6 && Math.Abs(t.TimeLimit-8f)<0.01f, "En Alerta Roja: +1 código y 80 % del tiempo");
    Check(t.Tick(9f) && t.Failed, "Tiempo agotado → fallo");
    // Caza en Alerta Roja
    AlertSystem.ResetAll(); Time.time=2000f;
    var hunter=Drone(out var ag); Set(hunter,"aggressiveOnRed",true);
    AlertSystem.ReportPlayerPosition(new Vector3(5,0,5));
    AlertSystem.ForceLevel(AlertLevel.Red);
    Call(hunter,"OnAlertLevelChanged",AlertLevel.Normal,AlertLevel.Red);
    Check(hunter.CurrentState==EnemyPatrol.EnemyState.Investigate && Math.Abs(ag.speed-3.6f*1.3f)<0.01f, "Élite en Roja: caza a la última posición conocida a velocidad de alarma ×1.3");
    var calm=Drone(out var ag2); Call(calm,"OnAlertLevelChanged",AlertLevel.Normal,AlertLevel.Red);
    Check(calm.CurrentState==EnemyPatrol.EnemyState.Patrol, "Dron normal no caza (solo élite)");
    // Escuadrón
    var a=Drone(out var aa); var b=Drone(out var bb); var sq=new DroneSquad(); Set(sq,"members",new EnemyPatrol[]{a,b});
    Call(sq,"OnSpotted",a);
    Check(b.CurrentState==EnemyPatrol.EnemyState.Investigate && a.CurrentState==EnemyPatrol.EnemyState.Patrol, "Escuadrón: si uno te ve, el compañero acude");
    var outsider=Drone(out var oo); Call(sq,"OnSpotted",outsider); 
    Check(true, "Avistamiento de un dron ajeno no afecta (sin excepción)");
    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
