using System;
using Nexus7.Director;
static class T {
  static int fails=0; static void Check(bool c,string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }
  static bool Near(float a,float b,float e=0.01f){ return Math.Abs(a-b)<=e; }

  static void Accuracy(){
    var p = new PlayerPerformanceModel();
    Check(Near(p.Accuracy, PlayerPerformanceModel.TargetAccuracy), "Sin disparos la precisión es la de referencia (35 %)");
    for(int i=0;i<20;i++){ p.RecordShot(i*0.5f,i,"pistola"); if(i%2==0) p.RecordHit(i*0.5f+0.1f,i); }
    Check(p.Accuracy>0.45f && p.Accuracy<0.5f, "10 de 20: precisión ≈ 46 % suavizada ("+p.Accuracy.ToString("0.00")+")");
    p.RecordHit(10f,0); p.RecordHit(10f,0); Check(p.HitsInWindow==10, "Un disparo cuenta como acierto una sola vez (escopeta)");
    p.Advance(80f); Check(p.ShotsInWindow==0 && p.HitsInWindow==0, "Lo que sale de la ventana de 60 s se olvida");
  }
  static void Rates(){
    var p = new PlayerPerformanceModel();
    for(int i=0;i<3;i++) p.RecordKill(10f+i*10f); p.Advance(60f);
    Check(Near(p.KillsPerMinute,3f,0.05f), "Ritmo de bajas: 3 en un minuto");
    p.RecordDamageTaken(61f,30f); p.RecordDamageTaken(62f,30f); Check(p.DamagePerMinute>55f, "Daño por minuto");
    p.Advance(200f); Check(p.DamagePerMinute==0f && p.KillsPerMinute==0f, "Se olvida al salir de la ventana");
  }
  static void Deaths(){
    var p = new PlayerPerformanceModel();
    p.RecordDeath(10f); p.RecordDeath(20f); Check(p.RecentDeaths>1.8f && p.TotalDeaths==2, "Dos caídas recientes");
    p.Advance(20f+90f); Check(p.RecentDeaths>0.9f && p.RecentDeaths<1.05f, "Las caídas se olvidan a la mitad cada 90 s");
    Check(Near(p.TimeSinceDeath,90f), "Tiempo sobrevivido desde la última caída");
  }
  static void Camping(){
    var p = new PlayerPerformanceModel();
    for(int i=0;i<60;i++) p.SamplePosition(i*0.5f, 10.5f+(i%3)*0.3f, 3f);
    Check(p.CampingScore>0.95f, "Quieto 30 s en la misma zona: acampada alta");
    float x,z; p.CampCenter(out x,out z); Check(Near(x,10f,0.01f) && Near(z,2f,0.01f), "Centro de la celda de acampada ("+x+","+z+")");
    var q = new PlayerPerformanceModel();
    for(int i=0;i<60;i++) q.SamplePosition(i*0.5f, i*0.8f, 0f);
    Check(q.CampingScore<0.2f, "Recorriendo la sala: sin acampada");
    var r = new PlayerPerformanceModel(); r.SamplePosition(0f,0f,0f); r.SamplePosition(5f,0f,0f);
    Check(r.CampingScore==0f, "Con menos de 20 s de muestras no se juzga");
    var neg = new PlayerPerformanceModel(); for(int i=0;i<60;i++) neg.SamplePosition(i*0.5f,-3f,-3f); neg.CampCenter(out x,out z);
    Check(Near(x,-2f) && Near(z,-2f), "Celdas con coordenadas negativas");
  }
  static void Dominance(){
    var p = new PlayerPerformanceModel(); string w;
    for(int i=0;i<9;i++) p.RecordShot(i,i,"rifle"); Check(p.WeaponDominance(out w)==0f, "Con menos de 10 disparos no hay abuso");
    for(int i=9;i<20;i++) p.RecordShot(i,i,i==19?"pistola":"rifle");
    Check(p.WeaponDominance(out w)>0.9f && w=="rifle", "El rifle domina ("+w+")");
  }
  static void PerformanceSign(){
    var good = new PlayerPerformanceModel(); 
    for(int i=0;i<40;i++){ good.RecordShot(i*4f,i,"a"); good.RecordHit(i*4f,i); if(i%10==0) good.RecordKill(i*4f+1f); }
    good.Advance(240f); for(int i=0;i<12;i++){ good.RecordKill(200f+i*3f); good.RecordShot(200f+i*3f,100+i,"b"); good.RecordHit(200f+i*3f,100+i); }
    Check(good.Performance>0.3f, "Jugador que acierta, mata y sobrevive: rendimiento alto ("+good.Performance.ToString("0.00")+")");
    var bad = new PlayerPerformanceModel();
    for(int i=0;i<20;i++) bad.RecordShot(i,i,"a"); bad.RecordDamageTaken(20f,90f); bad.RecordDeath(21f); bad.RecordDamageTaken(40f,90f); bad.RecordDeath(41f); bad.HealthFraction=0.3f;
    Check(bad.Performance< -0.4f, "Jugador que falla, recibe daño y cae: rendimiento bajo ("+bad.Performance.ToString("0.00")+")");
    var stealth = new PlayerPerformanceModel(); stealth.Advance(120f);
    Check(stealth.Performance>-0.05f && stealth.Performance<0.35f, "Sigiloso sin disparar ni recibir daño: ligeramente positivo ("+stealth.Performance.ToString("0.00")+")");
  }
  static void Knobs(){
    var k = DirectorModel.KnobsFor(0.5f);
    Check(Near(k.EnemySpeed,1f)&&Near(k.Detection,1f)&&Near(k.EnemyDamage,1f)&&Near(k.EnemyHealth,1f)&&Near(k.AmmoDrop,1f)&&Near(k.PatrolWait,1f)&&k.Reinforcements==0, "Tensión 0,5 = valores de diseño");
    var lo = DirectorModel.KnobsFor(0f); var hi = DirectorModel.KnobsFor(1f);
    Check(lo.EnemyDamage<1f && hi.EnemyDamage>1f && lo.AmmoDrop>1f && hi.AmmoDrop<1f && hi.Reinforcements==2, "Tensión baja ayuda y alta aprieta");
    Check(DirectorModel.BaselineFor(0)<DirectorModel.BaselineFor(1) && DirectorModel.BaselineFor(1)<DirectorModel.BaselineFor(2), "La base depende de la dificultad elegida");
  }
  static void TensionFollows(){
    var d = new DirectorModel(1); var good = Good();
    for(int i=0;i<120;i++) d.Tick(0.5f, good);
    Check(d.Tension>0.55f, "Jugando bien la tensión sube ("+d.Tension.ToString("0.00")+")");
    float before=d.Tension; d.Tick(0.5f, good); Check(d.Tension-before<=d.RisePerSecond*0.5f+1e-4f, "Sube despacio (máx. "+d.RisePerSecond+"/s)");
    var bad = new PlayerPerformanceModel(); for(int i=0;i<20;i++) bad.RecordShot(i,i,"a"); bad.RecordDamageTaken(5f,100f); bad.HealthFraction=0.2f; float t0=d.Tension; d.Tick(1f,bad); Check(t0-d.Tension<=d.FallPerSecond+1e-4f && d.Tension<t0, "Baja más rápido que sube");
    var e = new DirectorModel(1); var neutral = new PlayerPerformanceModel(); neutral.Advance(100f);
    float n0=e.Tension; e.Tick(1f, neutral); Check(Near(e.Tension,n0,1e-4f) || Math.Abs(e.Target-n0)>e.DeadZone, "Zona muerta: cambios pequeños no mueven la tensión");
  }
  static void Relief(){
    var d = new DirectorModel(1, 0.7f); d.OnPlayerDeath();
    Check(Near(d.Tension,0.55f) && d.InRelief, "Al caer: −0,15 de tensión y respiro");
    var good=Good(); d.Tick(5f,good); Check(Near(d.Tension,0.55f), "Durante el respiro no sube");
    d.Tick(16f,good); d.Tick(1f,good); Check(!d.InRelief && d.Tension>0.55f, "Tras el respiro vuelve a subir");
  }
  static void Patterns(){
    var d = new DirectorModel(1); var camper = new PlayerPerformanceModel();
    for(int i=0;i<60;i++) camper.SamplePosition(i*0.5f,1f,1f);
    Check(d.Tick(0.5f,camper)==DirectorAction.FlushCamper, "Acampar → manda una patrulla");
    Check(d.Tick(0.5f,camper)!=DirectorAction.FlushCamper, "Con espera entre avisos (45 s)");
    var e = new DirectorModel(1); var spam = new PlayerPerformanceModel();
    for(int i=0;i<20;i++) spam.RecordShot(i*0.2f,i,"rifle");
    Check(e.Tick(0.1f,spam)==DirectorAction.AdaptToWeapon && e.AdaptedWeapon=="rifle", "Abusar del rifle → los robots se adaptan");
    Check(e.WeaponDamageFactor("rifle")<1f && e.WeaponDamageFactor("pistola")>1f, "El rifle hace menos daño y las demás armas más");
    for(int i=20;i<40;i++) spam.RecordShot(4f+i*0.1f,i,i%2==0?"pistola":"escopeta");
    Check(e.Tick(0.1f,spam)==DirectorAction.StopAdapting && e.WeaponDamageFactor("rifle")==1f, "Al variar de arma se deshace la adaptación");
    var f = new DirectorModel(1, 0.6f); var dying = new PlayerPerformanceModel(); dying.RecordDeath(1f); dying.RecordDeath(2f);
    Check(f.Tick(0.1f,dying)==DirectorAction.Assist && f.Tension<0.5f, "Caer dos veces seguidas → ayuda y baja la tensión");
  }
  static PlayerPerformanceModel Good(){ var p=new PlayerPerformanceModel(); for(int i=0;i<30;i++){ p.RecordShot(i,i,i%2==0?"a":"b"); p.RecordHit(i,i);} for(int i=0;i<4;i++) p.RecordKill(10f+i); p.Advance(250f); for(int i=0;i<30;i++){ p.RecordShot(200f+i,100+i,i%2==0?"a":"b"); p.RecordHit(200f+i,100+i);} for(int i=0;i<4;i++) p.RecordKill(240f+i); return p; }
  static void Main(){
    Accuracy(); Rates(); Deaths(); Camping(); Dominance(); PerformanceSign(); Knobs(); TensionFollows(); Relief(); Patterns();
    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
