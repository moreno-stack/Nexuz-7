using System; using System.Linq;
using Nexus7.Cinematics; using Nexus7.Tutorial;
static class T {
  static int fails=0; static void Check(bool c,string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }
  static void Main(){
    var s = CinematicScripts.Almacen();
    Check(s.Count==3, "Almacén: 3 planos");
    float total = s.Sum(x=>x.duration); Check(Math.Abs(total-19f)<0.01f, "Almacén dura 19 s ("+total+")");
    Check(s.All(x => x.lines.All(l => l.at + l.hold <= x.duration + 0.001f && l.at >= 0f)), "Cada subtítulo cabe en su plano");
    Check(s.All(x => x.lines.Zip(x.lines.Skip(1),(a,b)=>a.at+a.hold<=b.at+0.001f).All(z=>z)), "Los subtítulos de un plano no se solapan");
    Check(s[0].lines[0].text=="Almacén de Carga del Módulo C. El montacargas del muro norte no tiene energía." && s[1].lines[1].speaker=="Tú" && s[1].lines[1].text=="Entonces mejor que no me vea.", "Textos exactos");
    Check(s[1].lines[0].text.Contains("bate") && s[2].lines[0].text.Contains("señuelo"), "Plano 2: el bate · Plano 3: el señuelo");
    Check(s.All(x => x.fromPos.y < 5.8f && x.toPos.y < 5.8f), "La cámara no atraviesa el techo (6 m, lámparas a 5,7)");
    Check(LevelIntro.ReadyToShow(true,false,0.25f) && !LevelIntro.ReadyToShow(true,true,9f) && LevelIntro.ReadyToShow(false,true,0f), "LevelIntroTest.StartsAfterSkip");
    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
