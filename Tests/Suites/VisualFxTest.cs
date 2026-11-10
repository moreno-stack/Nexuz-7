using System;
using System.Collections.Generic;
using UnityEngine; using Nexus7.Visual; using Nexus7.Core;
static class T {
  static int fails=0; static void Check(bool c,string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }
  static bool Near(float a,float b,float e=1e-3f){ return Math.Abs(a-b)<=e; }
  static void Main(){
    // ---------- Ruido y parpadeo ----------
    bool inRange=true, det=true; float minHum=9f, maxHum=-9f, minFaulty=9f;
    for(float t=0;t<60f;t+=0.013f){
      float h=FlickerModel.Sample(FlickerStyle.Hum,t,3.1f), f=FlickerModel.Sample(FlickerStyle.Faulty,t,3.1f), e=FlickerModel.Sample(FlickerStyle.Emergency,t,3.1f);
      if(h<0||h>1.2f||f<0||f>1.2f||e<0||e>1.2f) inRange=false;
      if(!Near(f,FlickerModel.Sample(FlickerStyle.Faulty,t,3.1f))) det=false;
      minHum=Math.Min(minHum,h); maxHum=Math.Max(maxHum,h); minFaulty=Math.Min(minFaulty,f);
    }
    Check(inRange, "Parpadeo: el factor de intensidad queda entre 0 y 1,2");
    Check(det, "Parpadeo: es determinista (mismo instante y semilla → mismo valor)");
    Check(minHum>0.8f && maxHum-minHum>0.02f, "Zumbido: varía un poco pero nunca se apaga");
    Check(minFaulty<0.15f, "Luz averiada: tiene apagones");
    Check(!Near(FlickerModel.Sample(FlickerStyle.Faulty,5f,1f),FlickerModel.Sample(FlickerStyle.Faulty,5f,7f)) || !Near(FlickerModel.Sample(FlickerStyle.Faulty,5.3f,1f),FlickerModel.Sample(FlickerStyle.Faulty,5.3f,7f)), "Cada luz parpadea distinto según su semilla");
    float onTime=0; int n=0; for(float t=0;t<120f;t+=0.01f){ if(FlickerModel.Sample(FlickerStyle.Faulty,t,2f)>0.5f) onTime++; n++; }
    Check(onTime/n>0.7f, "Luz averiada: encendida la mayor parte del tiempo (más del 70 %)");
    Check(Near(FlickerModel.Sample(FlickerStyle.Steady,12.3f,4f),1f), "Estilo fijo: siempre al 100 %");

    // ---------- Fallo digital de los hologramas ----------
    int glitches=0, cuts=0, samples=0; bool glitchOk=true;
    for(float t=0;t<200f;t+=0.05f){
      var g=GlitchModel.Sample(t,1.7f); samples++;
      if(g.active) glitches++; if(!g.visible) cuts++;
      if(Math.Abs(g.offsetX)>0.1f || g.scaleY<0.5f || g.scaleY>1.2f || g.intensity<0f || g.intensity>2f) glitchOk=false;
      if(!g.active && (!Near(g.offsetX,0) || !Near(g.scaleY,1) || !g.visible)) glitchOk=false;
    }
    Check(glitchOk, "Holograma: sin fallo está quieto y entero; con fallo, saltos pequeños (±0,1) y escala 0,5–1,2");
    Check(glitches>samples*0.04f && glitches<samples*0.3f, "Holograma: fallos de vez en cuando (entre el 4 % y el 30 % del tiempo)");
    Check(cuts>0 && cuts<glitches, "Holograma: algunos fallos son cortes completos");
    float scan=GlitchModel.ScanlineAlpha(0.5f,0f), scan2=GlitchModel.ScanlineAlpha(0.5f,0.37f);
    Check(scan>=0f && scan<=1f && !Near(scan,scan2), "Holograma: las líneas de escaneo se desplazan con el tiempo");

    // ---------- Temporizador intermitente (chispas y vapor) ----------
    var timer=new IntermittentTimer(0.8f,3.5f,42); bool tOk=true; float sum=0;
    for(int i=0;i<200;i++){ float d=timer.Next(); if(d<0.8f||d>3.5f) tOk=false; sum+=d; }
    Check(tOk, "Temporizador: cada espera está entre el mínimo y el máximo");
    Check(sum/200>1.5f && sum/200<2.8f, "Temporizador: la espera media queda en el centro del intervalo");
    var a=new IntermittentTimer(1,2,7); var b=new IntermittentTimer(1,2,7);
    Check(Near(a.Next(),b.Next()) && Near(a.Next(),b.Next()), "Temporizador: con la misma semilla repite la secuencia");
    var tick=new IntermittentTimer(1f,1f,1); int fired=0; for(int i=0;i<100;i++) if(tick.Tick(0.05f)) fired++;
    Check(fired==5, "Temporizador: con espera de 1 s, en 5 s dispara 5 veces");

    // ---------- Posprocesado según la tensión ----------
    PostFxState calm=PostFxModel.For(AlertLevel.Normal,1f,0f), yellow=PostFxModel.For(AlertLevel.Yellow,1f,0f), red=PostFxModel.For(AlertLevel.Red,1f,0f);
    Check(Near(calm.chromatic,0f) && Near(yellow.chromatic,0.15f) && Near(red.chromatic,0.35f), "Aberración cromática: 0 · 0,15 · 0,35 según la alerta");
    Check(Near(calm.vignette,PostFxModel.BaseVignette) && yellow.vignette>calm.vignette && red.vignette>yellow.vignette, "Viñeta: se cierra al subir la alerta");
    Check(red.saturation<yellow.saturation && yellow.saturation<calm.saturation && Near(calm.saturation,PostFxModel.BaseSaturation), "Saturación: baja con la alerta");
    Check(red.colorFilter.r>red.colorFilter.g && Near(calm.colorFilter.g,1f), "Alerta roja: tinte rojizo; en calma, sin tinte");
    PostFxState hit=PostFxModel.For(AlertLevel.Normal,1f,1f), hurt=PostFxModel.For(AlertLevel.Normal,0.2f,0f);
    Check(hit.chromatic>calm.chromatic+0.3f && hit.chromatic<=1f, "Golpe: pulso de aberración cromática (sin pasar de 1)");
    Check(hurt.vignette>calm.vignette && hurt.colorFilter.g<calm.colorFilter.g, "Vida baja: más viñeta y tono rojizo");
    Check(Near(PostFxModel.DecayPulse(1f,0.2f),0.5f) && Near(PostFxModel.DecayPulse(0.1f,1f),0f), "El pulso del golpe se apaga en 0,4 s");
    float s=0; for(int i=0;i<200;i++) s=PostFxModel.Approach(s,1f,0.016f,4f);
    Check(s>0.99f && s<=1f && PostFxModel.Approach(0f,1f,0.016f,4f)<0.1f, "Transición suave hacia el valor de destino");
    Check(Near(PostFxModel.Defaults.bloomThreshold,1f) && Near(PostFxModel.Defaults.bloomIntensity,0.8f) && Near(PostFxModel.Defaults.bloomScatter,0.7f) && Near(PostFxModel.Defaults.temperature,-12f) && Near(PostFxModel.Defaults.grain,0.12f), "Parámetros del volumen: bloom 1/0,8/0,7, temperatura −12, grano 0,12");

    // ---------- Recorte por distancia ----------
    Check(VisualCull.ShouldRun(10f,25f,false) && !VisualCull.ShouldRun(30f,25f,true), "Efectos: activos cerca, apagados lejos");
    Check(VisualCull.ShouldRun(26f,25f,true) && VisualCull.ShouldRun(24.5f,25f,false), "Efectos: margen del 10 % para no encender y apagar sin parar");
    Check(!VisualCull.ShouldRun(26f,25f,false), "Efectos: apagado no se enciende hasta entrar en el radio");

    // ---------- Elección de luces con sombra ----------
    var lights=new List<Vector3>{ new Vector3(0,3,0), new Vector3(10,3,0), new Vector3(20,3,0), new Vector3(30,3,0) };
    var occ=new List<Vector3>{ new Vector3(10,1,1), new Vector3(11,1,-1), new Vector3(9,0.5f,0), new Vector3(30,1,0), new Vector3(29,1,1), new Vector3(0.5f,1,0) };
    var pick=ShadowLightPicker.Pick(lights,occ,2,4f);
    Check(pick.Count==2 && pick.Contains(1) && pick.Contains(3), "Sombras: elige las luces con más estanterías/cajas cerca");
    Check(ShadowLightPicker.Pick(lights,new List<Vector3>(),3,4f).Count==0, "Sombras: sin obstáculos cerca no se gasta ninguna sombra");
    Check(ShadowLightPicker.Pick(lights,occ,10,4f).Count==3, "Sombras: nunca elige luces sin obstáculos aunque sobren huecos");

    // ---------- Materiales ----------
    Check(SurfaceClassifier.Classify("M2_SueloAlmacen")==SurfaceKind.Floor && SurfaceClassifier.Classify("M5_SueloMando")==SurfaceKind.Floor, "Clasificación: suelos");
    Check(SurfaceClassifier.Classify("M_ParedPanel")==SurfaceKind.Wall && SurfaceClassifier.Classify("M2_MuroAlmacen")==SurfaceKind.Wall && SurfaceClassifier.Classify("M6_PanelHangar")==SurfaceKind.Wall, "Clasificación: paredes y paneles");
    Check(SurfaceClassifier.Classify("M2_Estanteria")==SurfaceKind.Rust && SurfaceClassifier.Classify("M_Tuberia")==SurfaceKind.Rust && SurfaceClassifier.Classify("M2_MetalOscuro")==SurfaceKind.Rust && SurfaceClassifier.Classify("M3_Transformador")==SurfaceKind.Rust, "Clasificación: metal oxidado");
    Check(SurfaceClassifier.Classify("M2_CajaNaranja")==SurfaceKind.Crate && SurfaceClassifier.Classify("M4_BidonQuimico")==SurfaceKind.Crate, "Clasificación: cajas y bidones");
    foreach (var skip in new[]{"M3_Pantalla","M5_Holograma","M_LamparaEmergencia","MJ_Traje","MR_EliteCuerpo","M6_Planeta","M5_Estrella","M6_Explosion","M4_CristalOptico","MM_Casco","M_PantallaTerminal"})
      Check(SurfaceClassifier.Classify(skip)==SurfaceKind.Skip, "Clasificación: no se toca " + skip);
    Check(SurfaceClassifier.Tiling(SurfaceKind.Floor)>SurfaceClassifier.Tiling(SurfaceKind.Wall) && SurfaceClassifier.Tiling(SurfaceKind.Crate)>=1f, "Repetición: el suelo repite más que la pared");

    // ---------- Texturas generadas ----------
    const int N=64;
    foreach (SurfaceKind k in new[]{SurfaceKind.Floor,SurfaceKind.Wall,SurfaceKind.Rust,SurfaceKind.Crate}){
      var h=ProceduralSurface.HeightMap(k,N); bool hr=true; foreach(var v in h) if(v<0f||v>1f) hr=false;
      Check(h.Length==N*N && hr, k+": mapa de altura de "+N+"×"+N+" entre 0 y 1");
      float seam=0, inner=0;
      for(int i=0;i<N;i++){ seam+=Math.Abs(h[i*N+0]-h[i*N+N-1]); inner+=Math.Abs(h[i*N+N/2]-h[i*N+N/2-1]); }
      Check(seam<=inner*3f+0.5f, k+": la textura se repite sin costura visible");
      var nm=ProceduralSurface.NormalMap(h,N,4f); bool nOk=true;
      foreach(var c in nm){ float x=c.r*2-1,y=c.g*2-1,z=c.b*2-1; if(Math.Abs(x*x+y*y+z*z-1)>0.05f||z<0) nOk=false; }
      Check(nOk, k+": mapa normal con vectores unitarios que miran hacia fuera");
      var al=ProceduralSurface.Albedo(k,h,N); var mk=ProceduralSurface.Mask(k,h,N); bool aOk=true;
      for(int i=0;i<al.Length;i++) if(al[i].r<0||al[i].r>1||mk[i].a<0||mk[i].a>1||mk[i].r<0||mk[i].r>1) aOk=false;
      Check(aOk && al.Length==N*N && mk.Length==N*N, k+": color y máscara (metal en R, suavidad en A) válidos");
    }
    var flat=ProceduralSurface.NormalMap(new float[N*N],N,4f);
    Check(Near(flat[5].r,0.5f,0.01f) && Near(flat[5].g,0.5f,0.01f) && Near(flat[5].b,1f,0.01f), "Superficie plana → normal (0,5; 0,5; 1)");
    var rh=ProceduralSurface.HeightMap(SurfaceKind.Rust,N); var rm=ProceduralSurface.Mask(SurfaceKind.Rust,rh,N);
    float rough=0, met=0; foreach(var c in rm){ rough+=1f-c.a; met+=c.r; } rough/=rm.Length; met/=rm.Length;
    Check(rough>0.35f && met>0.3f && met<0.95f, "Metal oxidado: bastante rugoso y metálico solo en parte (el óxido no es metal)");
    var fh=ProceduralSurface.HeightMap(SurfaceKind.Floor,N);
    Check(Math.Abs(fh[3*N+3]-fh[3*N+N/2])>0.001f || Math.Abs(fh[0]-fh[N/2])>0.001f, "Suelo: tiene relieve (juntas entre placas)");

    var holo=ProceduralSurface.HoloPattern(N); float hInner=0; bool hOk=true; int innerN=0;
    for(int y=0;y<N;y++) for(int x=0;x<N;x++){ var c=holo[y*N+x]; if(c.a<0||c.a>1) hOk=false; if(x>8&&x<N-8&&y>8&&y<N-8){ hInner+=c.a; innerN++; } }
    Check(hOk && Near(holo[0].a,1f) && Near(holo[N/2].a,1f) && hInner/innerN<0.5f && hInner/innerN>0.08f, "Holograma: marco opaco y contenido translúcido con barras y renglones");
    // ---------- Reparto de efectos por la escena ----------
    var items=new List<int>(); for(int i=0;i<10;i++) items.Add(i);
    var sp=VisualPlacement.Spread(items,3);
    Check(sp.Count==3 && sp[0]<sp[1] && sp[1]<sp[2] && sp[0]<=2 && sp[2]>=7, "Reparto: 3 de 10 elementos repartidos (no los 3 primeros)");
    Check(VisualPlacement.Spread(new List<int>{1,2},5).Count==2 && VisualPlacement.Spread(new List<int>(),3).Count==0 && VisualPlacement.Spread(items,0).Count==0, "Reparto: con pocos elementos los toma todos; con 0 no toma ninguno");
    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
