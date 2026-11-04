using System; using System.Linq;
using Nexus7.Cinematics;
static class T {
  static int fails=0; static void Check(bool c,string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }
  static void Main(){
    var s = CinematicScripts.Despertar();
    Check(s.Count==3, "Despertar: 3 planos");
    float total = s.Sum(x=>x.duration); Check(Math.Abs(total-19f)<0.01f, "Despertar dura 19 s ("+total+")");
    Check(s.All(x => x.lines.All(l => l.at + l.hold <= x.duration + 0.001f && l.at >= 0f)), "Cada subtítulo cabe en su plano");
    Check(s.All(x => x.lines.Zip(x.lines.Skip(1),(a,b)=>a.at+a.hold<=b.at+0.001f).All(z=>z)), "Los subtítulos de un plano no se solapan");
    Check(s[0].lines[0].text=="Reanimación de emergencia completada. Tripulante, ¿me recibe?" && s[0].lines[1].speaker=="Tú", "Texto exacto plano 1");
    Check(s[2].lines[0].text.Contains("traje espacial") && s[2].lines[1].text=="Entendido. Vamos allá.", "Plano 3: la IA pide ponerse el traje");
    Check(s.SelectMany(x=>x.lines).Count()==6, "6 líneas en total");
    Check(Math.Abs(s[0].shake-0.05f)<1e-4 && s[1].shake==0f, "Vibración solo en el plano 1");
    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
