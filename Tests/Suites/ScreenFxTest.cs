using System;
using UnityEngine; using Nexus7.UI; using Nexus7.Core; using Nexus7.Gameplay;
static class T {
  static int fails=0; static void Check(bool c,string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }
  static float Len(Vector3 v){ return (float)Math.Sqrt(v.x*v.x+v.y*v.y+v.z*v.z); }
  static bool Near(float a,float b,float e=1e-3f){ return Math.Abs(a-b)<=e; }
  static void Main(){
    // ---------- Formas generadas por código ----------
    Check(ProceduralSprites.CircleAlpha(32,32,64)>0.99f && ProceduralSprites.CircleAlpha(1,1,64)<0.01f, "Círculo: lleno en el centro y vacío en la esquina");
    float edge = ProceduralSprites.CircleAlpha(63.5f,32,64);
    Check(edge>0f && edge<1f, "Círculo: borde suavizado (alfa intermedio en el contorno)");
    Check(ProceduralSprites.RingAlpha(32,32,64,6)<0.01f && ProceduralSprites.RingAlpha(32,3,64,6)>0.99f && ProceduralSprites.RingAlpha(1,1,64,6)<0.01f, "Anillo: hueco en el centro, sólido en el aro, vacío fuera");
    Check(ProceduralSprites.StarAlpha(32,32,64)>0.99f && ProceduralSprites.StarAlpha(32,58,64)>0.5f && ProceduralSprites.StarAlpha(2,2,64)<0.01f, "Estrella: centro y punta superior llenos, esquina vacía");
    Check(ProceduralSprites.StarAlpha(32,8,64)<0.5f, "Estrella: entre puntas (abajo, en el eje) queda vacío");
    Check(ProceduralSprites.VignetteAlpha(0.5f,0.5f)<0.01f && ProceduralSprites.VignetteAlpha(0f,0f)>0.6f && ProceduralSprites.VignetteAlpha(0f,0f)<=1f, "Viñeta: transparente en el centro, oscura en las esquinas");
    Check(ProceduralSprites.VignetteAlpha(0.2f,0.5f) < ProceduralSprites.VignetteAlpha(0.05f,0.5f), "Viñeta: más oscura cuanto más cerca del borde");
    Check(ProceduralSprites.ScanlineAlpha(0)>0f && ProceduralSprites.ScanlineAlpha(2)==0f && ProceduralSprites.ScanlineAlpha(4)>0f, "Líneas de escaneo: una de cada 4 filas");
    Check(Near(ProceduralSprites.GradientAlpha(0f),0f) && Near(ProceduralSprites.GradientAlpha(1f),1f) && ProceduralSprites.GradientAlpha(0.5f)>0.2f && ProceduralSprites.GradientAlpha(0.5f)<0.8f, "Degradado: de 0 (abajo) a 1 (arriba)");
    foreach (UIShapeKind k in Enum.GetValues(typeof(UIShapeKind)))
      Check(ProceduralSprites.SizeOf(k) >= 4 && ProceduralSprites.SizeOf(k) <= 256, "Tamaño de textura razonable para " + k);

    Check(ProceduralSprites.GlowAlpha(0.5f,0.5f)>0.99f && ProceduralSprites.GlowAlpha(0.5f,0f)<0.01f && ProceduralSprites.GlowAlpha(0f,0f)<0.01f, "Resplandor: lleno en el centro y nulo en el borde");
    Check(ProceduralSprites.GlowAlpha(0.5f,0.7f) > ProceduralSprites.GlowAlpha(0.5f,0.9f), "Resplandor: se apaga hacia fuera");
    Check(Near(ProceduralSprites.Alpha(UIShapeKind.GradientH,0,0),1f,0.05f) && ProceduralSprites.Alpha(UIShapeKind.GradientH,ProceduralSprites.SizeOf(UIShapeKind.GradientH)-1,0)<0.05f, "Degradado horizontal: opaco a la izquierda, transparente a la derecha");
    // ---------- Tarjetas de dificultad y pantalla final ----------
    Check(DifficultySettings.DamageText(Difficulty.Facil)=="daño ×1,0" && DifficultySettings.DamageText(Difficulty.Normal)=="daño ×1,5" && DifficultySettings.DamageText(Difficulty.Dificil)=="daño ×2,0", "Tarjetas: «daño ×1,0» · «daño ×1,5» · «daño ×2,0»");
    Check(MainMenuUI.CardTitle(Difficulty.Normal,true)=="<b>« Normal »</b>" && MainMenuUI.CardTitle(Difficulty.Facil,false)=="Fácil", "Tarjeta elegida: «« Normal »» en negrita; las demás, solo el nombre");
    Check(EndingUI.StarLit(0,"S") && EndingUI.StarLit(2,"S") && EndingUI.StarLit(1,"A") && !EndingUI.StarLit(2,"A") && EndingUI.StarLit(0,"B") && !EndingUI.StarLit(1,"B") && !EndingUI.StarLit(0,"C"), "Estrellas encendidas según el rango");
    Check(EndingUI.CategoryInitials.Length==4 && string.Join("",EndingUI.CategoryInitials)=="SRTC", "Iconos de las categorías: S, R, T, C");

    Check(EndingManager.ResultsBackdropFade >= 0.2f && EndingManager.ResultsBackdropFade <= 0.6f, "Antes de los resultados el fundido es parcial: se ve la última imagen de la cinemática");

    Check(ProceduralSprites.HaloAlpha(0.5f,0.5f)<0.05f && ProceduralSprites.HaloAlpha(0.5f,0.875f)>0.9f && ProceduralSprites.HaloAlpha(0.5f,1f)<0.05f, "Halo de atmósfera: hueco en el centro, máximo cerca del borde del planeta, nulo fuera");
    var tg = ProceduralSprites.Texture(UIShapeKind.Gradient); var th = ProceduralSprites.Texture(UIShapeKind.GradientH); var tc = ProceduralSprites.Texture(UIShapeKind.Circle);
    Check(tg.width==4 && tg.height==64 && th.width==64 && th.height==4 && tc.width==64 && tc.height==64, "Texturas: degradado 4×64, horizontal 64×4, círculo 64×64");
    Check(Near(ProceduralSprites.Alpha(UIShapeKind.Gradient,0,63),1f,0.02f) && ProceduralSprites.Alpha(UIShapeKind.Gradient,0,0)<0.02f, "Degradado vertical: arriba opaco, abajo transparente");
    Check(Near(ScreenFx.FillsEnd(4), ScreenFx.StaggerStart + 3*ScreenFx.StaggerStep + ScreenFx.FillDuration), "Fin del llenado de las 4 barras");
    Check(!EndingUI.HideOnResults("PanelResultados") && !EndingUI.HideOnResults("Fundido") && EndingUI.HideOnResults("TextoObjetivo") && EndingUI.HideOnResults("Mira") && EndingUI.HideOnResults("BarraRapida"), "Resultados: se ocultan el objetivo, la mira y la barra rápida (no el panel ni el fundido)");
    Check(EndingUI.ScoreLine(3813, true, 2000, "A")=="PUNTUACIÓN  <b>3813</b>" && EndingUI.ScoreLine(1500, false, 2000, "A").Contains("Récord: 2000 (A)"), "Con récord nuevo, «¡NUEVO RÉCORD!» solo sale en el cartel");

    // ---------- Animaciones ----------
    Check(Near(ScreenFx.Intro(0f,0.2f,0.5f),0f) && Near(ScreenFx.Intro(0.2f,0.2f,0.5f),0f) && Near(ScreenFx.Intro(0.7f,0.2f,0.5f),1f) && Near(ScreenFx.Intro(5f,0.2f,0.5f),1f), "Entrada: 0 antes del retraso, 1 al acabar");
    bool mono=true; float prev=-1f; for(float t=0;t<=1f;t+=0.05f){ float v=ScreenFx.Intro(t,0.1f,0.6f); if(v<prev-1e-5f) mono=false; prev=v; }
    Check(mono, "Entrada: nunca retrocede");
    Check(Near(ScreenFx.Intro(1f,0f,0f),1f), "Entrada con duración 0: aparece de golpe");
    Check(Near(ScreenFx.Slide(0f,0f,0.5f,80f),80f) && Near(ScreenFx.Slide(0.5f,0f,0.5f,80f),0f), "Deslizamiento: empieza desplazado y acaba en su sitio");
    Check(Near(ScreenFx.StaggeredFill(0.3f,0,0.8f),0f) && Near(ScreenFx.StaggeredFill(10f,3,0.8f),0.8f), "Barras: vacías al principio y en su valor al final");
    Check(ScreenFx.StaggeredFill(0.8f,0,1f) > ScreenFx.StaggeredFill(0.8f,2,1f), "Barras: se llenan una tras otra");
    Check(Near(ScreenFx.StaggeredFill(10f,0,1.7f),1f) && Near(ScreenFx.StaggeredFill(10f,0,-1f),0f), "Barras: el relleno queda entre 0 y 1");
    Check(ScreenFx.FillsDone(ScreenFx.StaggerStart + 3*ScreenFx.StaggerStep + ScreenFx.FillDuration + 0.01f, 4) && !ScreenFx.FillsDone(0.5f, 4), "Barras: se sabe cuándo han terminado las 4");
    float p0=ScreenFx.Pulse(0f,2f), p1=ScreenFx.Pulse(0.25f,2f);
    Check(p0>=0f && p0<=1f && p1>=0f && p1<=1f && !Near(p0,p1), "Pulso: entre 0 y 1 y cambia con el tiempo");

    Check(Near(ScreenFx.SweepAlpha(0f),0f) && Near(ScreenFx.SweepAlpha(1f),0f) && Near(ScreenFx.SweepAlpha(0.5f),1f), "Destello del título: se apaga en los extremos (no salta de golpe)");

    // ---------- Rango, estrellas y colores ----------
    Check(ScreenFx.GradeStars("S")==3 && ScreenFx.GradeStars("A")==2 && ScreenFx.GradeStars("B")==1 && ScreenFx.GradeStars("C")==0 && ScreenFx.GradeStars("D")==0 && ScreenFx.GradeStars(null)==0, "Estrellas: S 3 · A 2 · B 1 · C/D 0");
    Color s=ScreenFx.GradeColor("S"), d=ScreenFx.GradeColor("D");
    Check(s.r>0.9f && s.g>0.7f && s.b<0.5f && d.r>0.9f && d.g<0.5f, "Color del rango: S dorado, D rojo");
    Check(ScreenFx.EndingColor(EndingType.Optimal).g > ScreenFx.EndingColor(EndingType.Optimal).r && ScreenFx.EndingColor(EndingType.Critical).r > 0.8f && ScreenFx.EndingColor(EndingType.Critical).g < 0.5f, "Banda del final: verde óptimo, rojo crítico");
    Color tac = ScreenFx.EndingColor(EndingType.Tactical); Check(tac.r>0.8f && tac.g>0.6f && tac.b<0.4f, "Banda del final: ámbar táctico");
    Check(ScreenFx.EndingTag(EndingType.Optimal)=="ÓPTIMO" && ScreenFx.EndingTag(EndingType.Tactical)=="TÁCTICO" && ScreenFx.EndingTag(EndingType.Critical)=="CRÍTICO", "Etiqueta de cada final: ÓPTIMO · TÁCTICO · CRÍTICO");

    // ---------- Menú ----------
    Check(ScreenFx.DangerPips(Difficulty.Facil)==1 && ScreenFx.DangerPips(Difficulty.Normal)==2 && ScreenFx.DangerPips(Difficulty.Dificil)==3, "Nivel de peligro: 1, 2 y 3 marcas");
    Check(Near(ScreenFx.CardScale(true),1.06f) && Near(ScreenFx.CardScale(false),1f), "Tarjeta elegida un 6 % más grande");
    float maxOff=0f; for(float t=0;t<60f;t+=0.1f){ var o=ScreenFx.MenuCameraOffset(t); maxOff=Math.Max(maxOff,Len(o)); }
    Check(maxOff>0.05f && maxOff<0.6f, "Cámara del menú: vaivén suave (entre 5 cm y 60 cm)");
    Check(Near(Len(ScreenFx.MenuCameraOffset(0f)),0f,0.05f), "Cámara del menú: empieza en su sitio");
    Check(Near(ScreenFx.NavBlink(0f),1f) && Near(ScreenFx.NavBlink(0.5f),0f) && Near(ScreenFx.NavBlink(1.0f),1f), "Luces de navegación: encendidas 0,2 s cada segundo");

    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
