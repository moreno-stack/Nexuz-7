using System;
using Nexus7.Inventory;
static class T {
  static int fails=0; static void Check(bool c,string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }
  static void Main(){
    Check(KeyItemRescue.Shortfall(2,0,0)==2, "Sin ninguno en la sala ni encima: fabrica los 2");
    Check(KeyItemRescue.Shortfall(2,1,0)==1, "Solo fabrica lo que falta");
    Check(KeyItemRescue.Shortfall(2,0,3)==0, "Si quedan alcanzables en la sala, no regala nada");
    Check(KeyItemRescue.Shortfall(2,1,1)==0 && KeyItemRescue.Shortfall(1,5,0)==0, "Con lo que llevas y lo que queda basta");
    Check(KeyItemRescue.Shortfall(2,-1,-4)==2, "Valores negativos no cuentan");
    string r=KeyItemRescue.RescueMessage("Batería de iones",2);
    Check(r.StartsWith("TRAJE") && r.Contains("batería de iones") && r.Contains("+2"), "Mensaje del repuesto");
    string h=KeyItemRescue.HintMessage("Llave hidráulica",0f,20f);
    Check(h.Contains("20 m") && h.Contains("norte") && h.Contains("llave hidráulica"), "Pista con distancia y rumbo");
    Check(KeyItemRescue.HintMessage("Batería de iones",1f,1f).Contains("menos de 3 m"), "Si está al lado, lo dice");
    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
