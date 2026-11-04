using System; using System.Linq;
using Nexus7.Cinematics;
static class T {
  static int fails=0; static void Check(bool c,string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }
  static void Main(){
    var s = CinematicScripts.Energia();
    Check(s.Count==3, "Energía: 3 planos");
    float total = s.Sum(x=>x.duration); Check(Math.Abs(total-19f)<0.01f, "Energía dura 19 s ("+total+")");
    Check(s.All(x => x.lines.All(l => l.at + l.hold <= x.duration + 0.001f && l.at >= 0f)), "Cada subtítulo cabe en su plano");
    Check(s.All(x => x.lines.Zip(x.lines.Skip(1),(a,b)=>a.at+a.hold<=b.at+0.001f).All(z=>z)), "Los subtítulos de un plano no se solapan");
    Check(s[0].lines[0].text=="Sala de Energía. El reactor está en parada y la nave funciona con energía de emergencia." && s[2].lines[1].text=="Energía, reactor y salida. Entendido.", "Textos exactos");
    Check(s[1].lines[0].text.Contains("llave inglesa") && s[2].lines[0].text.Contains("kits de reparación"), "Plano 2: la llave · Plano 3: el kit");
    Check(s.All(x => x.fromPos.y < 9.5f && x.toPos.y < 9.5f), "La cámara no atraviesa el techo (10 m)");
    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
