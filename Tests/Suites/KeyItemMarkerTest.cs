using System;
using Nexus7.Inventory;
static class T {
  static int fails=0; static void Check(bool c,string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }
  static bool Near(float a,float b){ return Math.Abs(a-b)<0.5f; }
  static void Main(){
    float x,y;
    Check(KeyItemMarkerModel.Place(500,300,10,1000,600,out x,out y) && Near(x,500) && Near(y,300), "A la vista: el cuadro va encima del objeto");
    Check(!KeyItemMarkerModel.Place(1500,300,10,1000,600,out x,out y) && Near(x,1000-KeyItemMarkerModel.Margin) && Near(y,300), "Fuera por la derecha: en el borde derecho");
    Check(!KeyItemMarkerModel.Place(-200,300,10,1000,600,out x,out y) && Near(x,KeyItemMarkerModel.Margin), "Fuera por la izquierda: en el borde izquierdo");
    Check(!KeyItemMarkerModel.Place(700,300,-5,1000,600,out x,out y) && x<500 && Near(x,KeyItemMarkerModel.Margin), "Detrás y a la derecha en pantalla: el borde contrario (gírate)");
    Check(!KeyItemMarkerModel.Place(500,300,-5,1000,600,out x,out y) && Near(y,600-KeyItemMarkerModel.Margin), "Justo detrás: abajo");
    Check(!KeyItemMarkerModel.Place(500,-100,10,1000,600,out x,out y) && Near(y,KeyItemMarkerModel.Margin) && Near(x,500), "Por encima de la vista: borde de arriba");
    Check(!KeyItemMarkerModel.Place(5000,5000,10,1000,600,out x,out y) && x<=1000-KeyItemMarkerModel.Margin+0.5f && y<=600-KeyItemMarkerModel.Margin+0.5f, "Nunca se sale de la pantalla");
    Check(KeyItemMarkerModel.Label("Batería de iones",23.4f)=="Batería de iones · 23 m", "Nombre y metros");
    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
