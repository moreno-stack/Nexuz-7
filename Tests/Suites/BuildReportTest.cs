using System;
using Nexus7.Core;
static class T {
  static int fails=0; static void Check(bool c,string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }
  static void Main(){
    Check(BuildReport.Describe(new string[0])=="Juego construido: 6 fases y menú principal.", "Sin fallos: mensaje de éxito");
    string r=BuildReport.Describe(new[]{"Fase 3: Sala de energía"});
    Check(r.StartsWith("Se construyó el juego con errores") && r.Contains("Fase 3: Sala de energía"), "Con un fallo: lo nombra");
    string r2=BuildReport.Describe(new[]{"Fase 2: Almacén de carga","Fase 4: Laboratorio"});
    Check(r2.Contains("Fase 2: Almacén de carga") && r2.Contains("Fase 4: Laboratorio") && r2.Contains("Console"), "Con varios fallos: los nombra todos y remite a la consola");
    Check(BuildReport.Describe(null)=="Juego construido: 6 fases y menú principal.", "Lista nula = sin fallos");
    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
