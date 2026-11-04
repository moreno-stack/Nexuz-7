using System; using System.IO; using System.Linq;
using Nexus7.Cinematics;
static class T {
  static int fails=0; static void Check(bool c,string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }
  static string Src(string rel){ return File.ReadAllText("Assets/_Nexus7/Scripts/"+rel); }
  static void Main(){
    var s = CinematicScripts.CentroControl();
    Check(s.Count==3, "Centro de Control: 3 planos");
    float total = s.Sum(x=>x.duration); Check(Math.Abs(total-19f)<0.01f, "Centro de Control dura 19 s ("+total+")");
    Check(s.All(x => x.lines.All(l => l.at + l.hold <= x.duration + 0.001f && l.at >= 0f)), "Cada subtítulo cabe en su plano");
    Check(s.All(x => x.lines.Zip(x.lines.Skip(1),(a,b)=>a.at+a.hold<=b.at+0.001f).All(z=>z)), "Los subtítulos de un plano no se solapan");
    Check(s[0].lines[0].text=="Centro de Control: desde aquí se gobierna toda la nave. La seguridad responde al nivel de alerta." && s[2].lines[1].text=="Puente de mando. Ya casi estoy.", "Textos exactos");
    Check(s[1].lines[0].text.Contains("hachas") && s[2].lines[0].text.Contains("Alerta Roja"), "Plano 2: las hachas · Plano 3: la compuerta");
    Check(s.All(x => x.fromPos.y < 11.5f && x.toPos.y < 11.5f), "La cámara no atraviesa el techo (12 m)");
    Check(Src("UI/ShipStatusReport.cs").Contains("Robots activos"), "Informe de la nave: «Robots activos»");
    Check(!Src("Gameplay/AlertEventBridge.cs").Contains("drones") && !Src("Enemies/DroneSquad.cs").Contains("un dron te ha visto"), "Alertas y escuadrón ya no dicen «drones»");
    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
