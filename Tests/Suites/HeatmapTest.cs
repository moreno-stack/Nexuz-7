using System;
using Nexus7.Director;
static class T {
  static int fails=0; static void Check(bool c,string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }
  static bool Near(float a,float b){ return Math.Abs(a-b)<0.01f; }

  static void Cells(){
    var m = new HeatmapModel();
    m.Add(1f,1f,1f); m.Add(3.9f,0.2f,1f);
    Check(m.Cells==1 && Near(m.HeatAt(2f,2f),2f), "Celdas de 4 × 4 m: dos sustos cerca suman en la misma");
    m.Add(-1f,-1f,1f); m.Add(-5f,9f,1f);
    Check(m.Cells==3 && Near(m.HeatAt(-0.5f,-0.5f),1f) && Near(m.HeatAt(-4.1f,8.1f),1f), "Las coordenadas negativas no se mezclan con las positivas");
    m.Add(0f,0f,0f); m.Add(0f,0f,-3f);
    Check(Near(m.HeatAt(1f,1f),2f), "Cantidades nulas o negativas no cuentan");
    m.Decay();
    Check(Near(m.HeatAt(1f,1f),1.2f), "Cada intento el calor se enfría ×0,6");
    for(int i=0;i<10;i++) m.Decay();
    Check(m.Cells==0, "Lo que casi se ha enfriado del todo se olvida");
  }

  static void Picking(){
    var m = new HeatmapModel();
    m.Add(22f,22f,HeatmapModel.DetectionHeat);
    Check(!m.PickHotspot(0f,0f,out _,out _,out _), "Un solo susto (calor < 2) no es una ruta");
    m.Add(22f,22f,HeatmapModel.DeathHeat);   // 4 en la celda (20..24)
    m.Add(30f,2f,HeatmapModel.DetectionHeat*2.5f);
    Check(m.PickHotspot(0f,0f,out float x,out float z,out float h) && Near(x,22f) && Near(z,22f) && Near(h,4f), "Elige la celda más caliente y devuelve su centro");
    Check(m.PickHotspot(0f,0f,out x,out z,out h) && Near(x,30f) && Near(z,2f), "En el intento siguiente no repite la misma si hay otra");
    var solo = new HeatmapModel(); solo.Add(22f,22f,5f);
    solo.PickHotspot(0f,0f,out _,out _,out _);
    Check(solo.PickHotspot(0f,0f,out x,out z,out _) && Near(x,22f), "Si es la única válida, la repite");
    var safe = new HeatmapModel(); safe.Add(5f,5f,10f); safe.Add(30f,30f,2f);
    Check(safe.PickHotspot(3f,3f,out x,out z,out _) && Near(x,30f), "Nunca la que está a menos de 10 m de la reaparición");
    var none = new HeatmapModel(); none.Add(1f,1f,10f);
    Check(!none.PickHotspot(0f,0f,out _,out _,out _), "Si solo hay calor junto a la reaparición, no manda a nadie");
    var tie = new HeatmapModel(); tie.Add(42f,2f,3f); tie.Add(2f,42f,3f);
    var tie2 = new HeatmapModel(); tie2.Add(2f,42f,3f); tie2.Add(42f,2f,3f);
    tie.PickHotspot(0,0,out x,out z,out _); tie2.PickHotspot(0,0,out float x2,out float z2,out _);
    Check(Near(x,x2) && Near(z,z2), "Con empate el resultado no depende del orden");
    m.Clear(); Check(m.Cells==0 && !m.PickHotspot(0,0,out _,out _,out _), "Se puede vaciar (nueva partida)");
  }

  static void Main(){
    Cells(); Picking();
    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
