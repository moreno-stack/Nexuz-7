using System; using System.Linq;
using Nexus7.Cinematics;
static class T {
  static int fails=0; static void Check(bool c,string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }
  static void Main(){
    var s = CinematicScripts.Laboratorio();
    Check(s.Count==3, "Laboratorio: 3 planos");
    float total = s.Sum(x=>x.duration); Check(Math.Abs(total-19f)<0.01f, "Laboratorio dura 19 s ("+total+")");
    Check(s.All(x => x.lines.All(l => l.at + l.hold <= x.duration + 0.001f && l.at >= 0f)), "Cada subtítulo cabe en su plano");
    Check(s.All(x => x.lines.Zip(x.lines.Skip(1),(a,b)=>a.at+a.hold<=b.at+0.001f).All(z=>z)), "Los subtítulos de un plano no se solapan");
    Check(s[0].lines[0].text=="Laboratorio del proyecto ESPECTRO. Busca cristales ópticos por la sala: brillan y sirven para el camuflaje." && s[2].lines[1].text=="A trabajar.", "Textos exactos");
    Check(s[1].lines[0].text.Contains("porra") && s[2].lines[0].text.Contains("kits"), "Plano 2: la porra · Plano 3: el banco");
    Check(s.All(x => x.fromPos.y < 5.8f && x.toPos.y < 5.8f), "La cámara no atraviesa el techo (6 m)");
    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
