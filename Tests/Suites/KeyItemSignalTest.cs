using System;
using Nexus7.Inventory;
static class T {
  static int fails=0; static void Check(bool c,string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }
  static void Main(){
    Check(KeyItemSignal.Direction(0,10)=="norte" && KeyItemSignal.Direction(10,0)=="este" && KeyItemSignal.Direction(0,-10)=="sur" && KeyItemSignal.Direction(-10,0)=="oeste", "Rumbos principales (norte = +Z, este = +X)");
    Check(KeyItemSignal.Direction(5,5)=="noreste" && KeyItemSignal.Direction(-5,-5)=="suroeste" && KeyItemSignal.Direction(5,-5)=="sureste" && KeyItemSignal.Direction(-5,5)=="noroeste", "Rumbos intermedios");
    Check(KeyItemSignal.Direction(1,10)=="norte" && KeyItemSignal.Direction(-1,10)=="norte", "Casi al norte sigue siendo norte");
    Check(KeyItemSignal.Message("Batería de iones", 0, 14)=="TRAJE: señal de Batería de iones a 14 m al norte.", "Mensaje con distancia y dirección");
    Check(KeyItemSignal.Message("Batería de iones", 1, 1)==null, "A menos de 3 m no avisa");
    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
