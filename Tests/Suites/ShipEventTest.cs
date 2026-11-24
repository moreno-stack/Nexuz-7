using System;
using System.Collections.Generic;
using Nexus7.Director;
static class T {
  static int fails=0; static void Check(bool c,string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }
  static void Choose(){
    bool allHelp=true, allHurt=true; int none=0, total=0;
    for (int i=0;i<50;i++){
      double r=i/50.0;
      allHelp &= ShipEventModel.Helps(ShipEventModel.Choose(0.3f,0f,ShipEvent.None,r)) && ShipEventModel.Helps(ShipEventModel.Choose(0.7f,1.2f,ShipEvent.None,r));
      allHurt &= ShipEventModel.Choose(0.7f,0f,ShipEvent.None,r)==ShipEvent.SecuritySweep;
      if (ShipEventModel.Choose(0.55f,0f,ShipEvent.None,r)==ShipEvent.None) none++; total++;
    }
    Check(allHelp, "Si vas mal (tensión baja o caídas recientes), el evento te ayuda");
    Check(allHurt, "Si vas sobrado, barrido de seguridad");
    Check(none>15 && none<35, "Con tensión media, la mitad de las veces no pasa nada ("+none+"/"+total+")");
    bool varied=true;
    for (int i=0;i<50;i++){ double r=i/50.0; varied &= ShipEventModel.Choose(0.3f,0f,ShipEvent.Blackout,r)!=ShipEvent.Blackout && ShipEventModel.Choose(0.55f,0f,ShipEvent.SteamLeak,r)!=ShipEvent.SteamLeak; }
    Check(varied, "No repite el evento del intento anterior");
  }
  static void Spots(){
    // Jugador en (0,0) mirando a +Z
    var xs=new List<float>{0f,0f,2f,0f,12f}; var zs=new List<float>{12f,-12f,1f,60f,0f};
    int ahead=0; for(int s=0;s<20;s++) if (ShipEventModel.PickSpot(xs,zs,0,0,0,1,-1,new Random(s))==0) ahead++;
    Check(ahead==20, "Elige un sitio por delante, en tu camino (20/20)");
    bool ok=true; for(int s=0;s<20;s++){ int i=ShipEventModel.PickSpot(xs,zs,0,0,0,1,-1,new Random(s)); ok &= i!=2 && i!=3; }
    Check(ok, "Nunca a menos de 6 m ni a más de 25 m");
    bool moved=true; for(int s=0;s<20;s++) moved &= ShipEventModel.PickSpot(xs,zs,0,0,0,1,0,new Random(s))!=0;
    Check(moved, "No repite el sitio del intento anterior");
    Check(ShipEventModel.PickSpot(new List<float>{1f}, new List<float>{1f},0,0,0,1,-1,new Random(1))==-1, "Sin sitios válidos no hay evento");
  }
  static void Main(){
    Choose(); Spots();
    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
