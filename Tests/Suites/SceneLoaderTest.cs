using System;
using Nexus7.Core;
static class T {
  static int fails=0; static void Check(bool c,string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }
  static bool Near(float a,float b){ return Math.Abs(a-b)<1e-4f; }
  static void Main(){
    Check(Near(SceneFade.Alpha(0f,0.35f,false),0f) && Near(SceneFade.Alpha(0.35f,0.35f,false),1f), "Fundido a negro: de 0 a 1");
    Check(Near(SceneFade.Alpha(0.175f,0.35f,true),0.5f) && Near(SceneFade.Alpha(0.35f,0.35f,true),0f) && Near(SceneFade.Alpha(0f,0.35f,true),1f), "Fundido de entrada: de 1 a 0");
    Check(Near(SceneFade.Alpha(9f,0.35f,false),1f) && Near(SceneFade.Alpha(-1f,0.35f,false),0f), "Fuera de rango se recorta");
    Check(Near(SceneFade.Alpha(0f,0f,false),1f) && Near(SceneFade.Alpha(0f,0f,true),0f), "Duración 0: salto inmediato");
    Check(!SceneFade.CanStart(true,true), "Ya cargando: no empieza otra carga (doble clic)");
    Check(!SceneFade.CanStart(false,false), "Escena fuera de Build Settings: no carga");
    Check(SceneFade.CanStart(false,true), "Libre y escena válida: carga");
    Check(Near(SceneFade.Progress(0.45f),0.5f) && Near(SceneFade.Progress(0.9f),1f) && Near(SceneFade.Progress(1f),1f), "Progreso de carga: 0,9 de Unity = 100 %");
    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
