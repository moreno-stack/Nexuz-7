using System; using System.Linq; using System.Reflection; using UnityEngine;
using Nexus7.Core; using Nexus7.Gameplay; using Nexus7.Inventory;
static class T {
  static void Set(object o, string f, object v){ o.GetType().GetField(f, BindingFlags.NonPublic|BindingFlags.Instance).SetValue(o, v); }
  static ItemData Item(string n, int max, ItemCategory c){ var i = new ItemData(); Set(i,"displayName",n); Set(i,"maxStack",max); Set(i,"category",c); i.name=n; return i; }
  static int fails=0; static void Check(bool c, string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }
  static ScoreInput Ghost(){ return new ScoreInput{ detections=0, alarms=0, redAlerts=0, captures=0, loudNoise=0f, resourceValue=450f,
      itemsCollected=30, itemsCrafted=3, devicesUsed=3, totalSeconds=1300f, evacuationRemaining=150f, distinctDeviceKinds=3,
      hacksSucceeded=6, hacksFailed=0, sectorsDisabled=2, empTargetsHit=3, decoysTriggered=2, deaths=0, escaped=true }; }
  static void Main(){
    // 1. Partida fantasma
    var g = Ghost(); var r = ScoreCalculator.Calculate(g);
    Check(r.ending==EndingType.Optimal, "Fantasma: escape sin Alerta Roja y con recursos → FINAL ÓPTIMO");
    Check(r.stealth==1500 && r.resources==850 && r.time==950, $"Fantasma: sigilo 1500, recursos 450+400, tiempo 800+150 (={r.stealth}/{r.resources}/{r.time})");
    Check(r.creativity==450+240+120+75+60, $"Creatividad 3 tipos+6 hackeos+2 sectores+3 PEM+2 señuelos = 945 ({r.creativity})");
    Check(r.total==1500+850+950+945 && r.grade=="S", $"Total {r.total} → rango S");
    Check(r.highlights.Count==4, "4 logros destacados (fantasma, ingeniero, relámpago, arsenal)");
    // 2. Táctico
    var t = Ghost(); t.redAlerts=2; t.alarms=4; t.detections=6;
    var rt = ScoreCalculator.Calculate(t);
    Check(rt.ending==EndingType.Tactical, "Escape con Alerta Roja → FINAL TÁCTICO");
    Check(rt.stealth==Math.Max(0,1200-720-320-600), "Sigilo con penalizaciones se recorta a 0 y pierde el bonus fantasma");
    var t2 = Ghost(); t2.resourceValue=60f; Check(ScoreCalculator.DetermineEnding(t2)==EndingType.Tactical, "Sin Alerta Roja pero sin recursos sobrantes (<100) → TÁCTICO");
    var t3 = Ghost(); t3.captures=2; Check(ScoreCalculator.DetermineEnding(t3)==EndingType.Tactical, "Capturado 2 veces → no es óptimo");
    // 3. Crítico
    var c = Ghost(); c.escaped=false; c.evacuationRemaining=0f; var rc = ScoreCalculator.Calculate(c);
    int sumC = rc.stealth+rc.resources+rc.time+rc.creativity-rc.penalty;
    Check(rc.ending==EndingType.Critical && rc.total==sumC/2, "Sin escape → FINAL CRÍTICO y puntuación a la mitad");
    Check(!rc.highlights.Any(h=>h.StartsWith("Relámpago")), "Sin escape no hay logro 'Relámpago'");
    var d = Ghost(); d.deaths=2; Check(ScoreCalculator.Calculate(d).penalty==400, "Penalización de 200 por muerte");
    // 4. Tiempo y límites
    var s1 = Ghost(); s1.totalSeconds=3000f; s1.evacuationRemaining=0f; Check(ScoreCalculator.Calculate(s1).time==400, "50 min → mitad de puntos de tiempo (400)");
    var s2 = Ghost(); s2.totalSeconds=9000f; s2.evacuationRemaining=999f; Check(ScoreCalculator.Calculate(s2).time==200, "Partida eterna: 0 base + bonus máx. 200");
    var k = Ghost(); k.hacksSucceeded=50; k.sectorsDisabled=20; k.empTargetsHit=40; k.decoysTriggered=40; Check(ScoreCalculator.Calculate(k).creativity==1000, "Creatividad con tope 1000");
    var k2 = Ghost(); k2.distinctDeviceKinds=0; k2.hacksSucceeded=0; k2.sectorsDisabled=0; k2.empTargetsHit=0; k2.decoysTriggered=0; k2.hacksFailed=10;
    Check(ScoreCalculator.Calculate(k2).creativity==0, "Hackeos fallidos no dejan la creatividad en negativo");
    var e = Ghost(); e.itemsCrafted=0; e.devicesUsed=0; e.resourceValue=900f; Check(ScoreCalculator.Calculate(e).resources==800, "Sin fabricar: eficiencia neutra 200 + sobrantes con tope 600");
    Check(ScoreCalculator.Grade(3800)=="S" && ScoreCalculator.Grade(3799)=="A" && ScoreCalculator.Grade(2200)=="B" && ScoreCalculator.Grade(1399)=="D", "Umbrales de rango S/A/B/D");
    // 5. Valor de recursos
    var inv = new InventorySystem(); Set(inv,"capacity",10);
    inv.Add(Item("Chatarra",20,ItemCategory.Part),4); inv.Add(Item("Circuito",10,ItemCategory.Component),2);
    inv.Add(Item("Bateria",5,ItemCategory.Battery),1); inv.Add(Item("PEM",5,ItemCategory.Device),1); inv.Add(Item("Llave",1,ItemCategory.Tool),1);
    Check(Math.Abs(EndingManager.ResourceValue(inv)-(20+50+40+60))<0.01f, "Valor de inventario: 4×5 + 2×25 + 40 + 60 (+ llave 0) = 170");
    Check(EndingManager.ResourceValue(null)==0f, "Sin inventario → 0");
    // 6. RunStats
    RunStats.Reset(); Time.realtimeSinceStartup=10f; RunStats.EnsureStarted(); Time.realtimeSinceStartup=70f; RunStats.EnsureStarted();
    Check(Math.Abs(RunStats.Elapsed-60f)<0.01f, "Cronómetro: EnsureStarted no reinicia una partida en curso (60 s)");
    RunStats.RecordDeviceUse(0); RunStats.RecordDeviceUse(0); RunStats.RecordDeviceUse(2); RunStats.RecordCollected(3); RunStats.RecordCollected(-1); RunStats.RecordEmpHits(0);
    Check(RunStats.DevicesUsed==3 && RunStats.DistinctDeviceKinds==2 && RunStats.ItemsCollected==3 && RunStats.EmpTargetsHit==0, "RunStats: 3 usos, 2 tipos, recogidas ignoran negativos");
    RunStats.Reset(); Check(RunStats.DevicesUsed==0 && RunStats.DistinctDeviceKinds==0 && !RunStats.HasStarted, "RunStats.Reset limpia todo");
    // 7. Récord
    Check(GameFlow.SubmitScore(2000,"B") && GameFlow.BestScore==2000, "Primera puntuación → récord");
    Check(!GameFlow.SubmitScore(1500,"C") && GameFlow.BestGrade=="B", "Puntuación menor no sustituye el récord");
    Check(GameFlow.SubmitScore(3900,"S") && GameFlow.BestGrade=="S", "Nuevo récord S");
    // 8. Correcciones de la revisión
    RunStats.Reset(); RunStats.RecordSectorDisabled("CentroControl/Sector Este"); RunStats.RecordSectorDisabled("CentroControl/Sector Este"); RunStats.RecordSectorDisabled("Evacuacion/Sector Pasarela");
    Check(RunStats.SectorsDisabled==2, "Repetir el mismo sector no suma creatividad (2 sectores distintos)");
    var nz = Ghost(); nz.loudNoise=500f; Check(ScoreCalculator.Calculate(nz).stealth==1200-300+300, "Penalización por ruido con tope 300");
    var tq = Ghost(); tq.redAlerts=1; tq.hacksSucceeded=1; tq.devicesUsed=1;
    Check(ScoreCalculator.DetermineEnding(tq)==EndingType.Tactical && !ScoreCalculator.UsedHeavyTactics(tq), "Táctico sin apenas hackeos/dispositivos → texto de 'escape a la carrera'");
    tq.hacksSucceeded=3; Check(ScoreCalculator.UsedHeavyTactics(tq), "Táctico con 4+ hackeos/dispositivos → texto de 'escape a la fuerza'");
    AlertSystem.ResetAll(); Time.time=5000f; AlertSystem.AddHeat(30f); Time.time=5100f;
    for(int nn=0;nn<10;nn++){ AlertSystem.Report(AlertEvent.LoudNoise); AlertSystem.Tick(1f); Time.time+=1f; }
    Check(AlertSystem.Heat<30f, "Correr no detiene la disipación de la tensión (calor "+AlertSystem.Heat.ToString("0.0")+" < 30)");
    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
