using System;
using Nexus7.Core;
static class T {
  static int fails=0; static void Check(bool c,string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }
  static bool Near(float a,float b){ return Math.Abs(a-b)<1e-4f; }
  static void Main(){
    GameSettings.ResetToDefaults();
    Check(Near(GameSettings.Master,0.8f) && Near(GameSettings.Music,0.7f) && Near(GameSettings.Sfx,0.8f), "Volúmenes por defecto: general 80 %, música 70 %, efectos 80 %");
    Check(GameSettings.Quality==QualityLevel.Alta && GameSettings.Fullscreen, "Por defecto: calidad Alta y pantalla completa");
    GameSettings.Master=1.7f; Check(Near(GameSettings.Master,1f), "Más de 1 se recorta a 1");
    GameSettings.Music=-2f; Check(Near(GameSettings.Music,0f), "Menos de 0 se recorta a 0");
    Check(Near(GameSettings.Curve(0f),0f) && Near(GameSettings.Curve(1f),1f) && Near(GameSettings.Curve(0.5f),0.25f), "Curva de volumen: 0 → silencio real, 1 → volumen completo, 0,5 → 0,25");
    Check(Near(GameSettings.Curve(-1f),0f) && Near(GameSettings.Curve(3f),1f), "La curva no sale de 0–1");
    Check(GameSettings.NameOf(QualityLevel.Baja)=="Baja" && GameSettings.NameOf(QualityLevel.Media)=="Media" && GameSettings.NameOf(QualityLevel.Alta)=="Alta", "Nombres de calidad");
    int changes=0; Action h=()=>changes++; GameSettings.Changed+=h;
    GameSettings.Sfx=0.3f; Check(changes==1, "Cada cambio avisa una vez");
    GameSettings.Quality=QualityLevel.Media; GameSettings.Fullscreen=false; Check(changes==3, "Calidad y pantalla completa también avisan");
    GameSettings.Changed-=h;
    var b=QualityPresets.For(QualityLevel.Baja);
    Check(b.unityLevel=="Mobile" && !b.postProcessing && !b.shadows && b.antialiasing==AaMode.None && !b.filmGrain, "Baja: nivel Mobile, sin posprocesado, sin sombras, sin antialiasing");
    var m=QualityPresets.For(QualityLevel.Media);
    Check(m.unityLevel=="PC" && m.postProcessing && m.shadows && m.antialiasing==AaMode.Fxaa && !m.filmGrain, "Media: nivel PC, posprocesado sin grano, sombras, FXAA");
    var a=QualityPresets.For(QualityLevel.Alta);
    Check(a.unityLevel=="PC" && a.postProcessing && a.shadows && a.antialiasing==AaMode.Smaa && a.filmGrain, "Alta: nivel PC, todo el posprocesado, sombras, SMAA");
    Check(QualityPresets.FindLevel(new[]{"Mobile","PC"},"PC",0)==1 && QualityPresets.FindLevel(new[]{"Mobile","PC"},"Ultra",0)==0 && QualityPresets.FindLevel(null,"PC",1)==1, "Busca el nivel de Unity por nombre (si no existe, el de reserva)");
    GameSettings.ResetToDefaults(); Check(Near(GameSettings.Sfx,0.8f) && GameSettings.Quality==QualityLevel.Alta && GameSettings.Fullscreen, "Restablecer vuelve a los valores por defecto");
    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
