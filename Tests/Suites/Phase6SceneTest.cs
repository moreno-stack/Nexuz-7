using System; using System.Linq; using System.Collections.Generic;
using UnityEngine; using Nexus7.Cinematics; using Nexus7.Gameplay; using Nexus7.Core; using Nexus7.Environment; using Nexus7.UI;
static class T {
  static int fails=0; static void Check(bool c,string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }
  static void Script(string name, List<CinematicShot> s, int shots, float dur, float maxY){
    Check(s.Count==shots && Math.Abs(s.Sum(x=>x.duration)-dur)<0.01f, name+": "+shots+" planos, "+dur+" s");
    Check(s.All(x => x.lines.All(l => l.at + l.hold <= x.duration + 0.001f && l.at >= 0f)), name+": cada subtítulo cabe en su plano");
    Check(s.All(x => x.lines.Zip(x.lines.Skip(1),(a,b)=>a.at+a.hold<=b.at+0.001f).All(z=>z)), name+": subtítulos sin solaparse");
    if (maxY>0) Check(s.All(x => x.fromPos.y < maxY && x.toPos.y < maxY), name+": la cámara no atraviesa el techo");
  }
  static void Main(){
    var a = CinematicScripts.Autodestruccion(); Script("Autodestrucción", a, 3, 19f, 13.5f);
    Check(a[0].lines[0].text=="Hangar de evacuación. Autodestrucción activada: tendrás cinco minutos para salir." && a[2].lines[1].text=="Vamos a casa.", "Autodestrucción: textos exactos");
    var d = CinematicScripts.Despegue(); Script("Despegue", d, 2, 14f, 0f);
    Check(d[0].lines[1].text=="Adiós, Nexus-7." && d[1].lines[1].text=="Has sobrevivido, tripulante." && d[1].shake>0f, "Despegue: textos y vibración de la explosión");
    var f = CinematicScripts.Derrota(); Script("Derrota", f, 1, 7f, 13.5f);
    Check(f[0].lines[0].text=="Fallo de contención del reactor..." && f[0].lines[1].speaker=="Tú", "Derrota: textos exactos");
    // Cuenta atrás en pausa durante las cinemáticas
    Check(!EvacuationCountdown.ShouldTick(true,true,true) && EvacuationCountdown.ShouldTick(true,true,false) && EvacuationCountdown.ShouldTick(true,false,true) && !EvacuationCountdown.ShouldTick(false,false,false), "La cuenta atrás no corre durante una cinemática");
    // Explosión que crece
    bool mono=true; float prev=0f; for(float t=0;t<=4f;t+=0.25f){ float v=GrowAndFade.ScaleAt(t,4f,1f,120f); if(v<prev) mono=false; prev=v; }
    Check(GrowAndFade.ScaleAt(0,4,1,120)==1f && Math.Abs(GrowAndFade.ScaleAt(4,4,1,120)-120f)<1e-3 && Math.Abs(GrowAndFade.ScaleAt(9,4,1,120)-120f)<1e-3 && mono, "La explosión crece de 1 a 120 sin retroceder");
    // Dificultad
    Check(DifficultySettings.Label(Difficulty.Facil)=="Fácil ×1,0" && DifficultySettings.Label(Difficulty.Normal)=="Normal ×1,5" && DifficultySettings.Label(Difficulty.Dificil)=="Difícil ×2,0", "Botones del menú: «Fácil ×1,0» · «Normal ×1,5» · «Difícil ×2,0»");
    Check(DifficultySettings.ResultLine(Difficulty.Facil)=="Dificultad: Fácil ×1,0" && DifficultySettings.ResultLine(Difficulty.Normal)=="Dificultad: Normal ×1,15" && DifficultySettings.ResultLine(Difficulty.Dificil)=="Dificultad: Difícil ×1,3", "Resultados: «Dificultad: Normal ×1,15»");
    MainMenuUI.SelectDifficulty(Difficulty.Normal);
    Check(DifficultySettings.Current==Difficulty.Normal && PlayerPrefs.GetInt(DifficultySettings.PrefsKey,-1)==1, "Elegir Normal en el menú la guarda");
    DifficultySettings.Current = Difficulty.Facil;
    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
