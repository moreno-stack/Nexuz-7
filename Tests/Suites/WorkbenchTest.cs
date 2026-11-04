using System; using System.Collections.Generic; using UnityEngine; using Nexus7.Crafting;
static class T {
  static int fails=0; static void Check(bool c,string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }
  static void Main(){
    var o = new Vector3(0,0,0);
    var l = new List<Vector3>{ new Vector3(5,0,0), new Vector3(2,0,0), new Vector3(10,0,0) };
    Check(Workbench.NearestInRange(o, l, 4f)==1, "Banco más cercano dentro del radio");
    Check(Workbench.NearestInRange(o, l, 1f)==-1, "Ninguno dentro del radio → -1");
    Check(Workbench.NearestInRange(o, new List<Vector3>(), 4f)==-1, "Sin bancos → -1");
    Check(Workbench.NearestInRange(o, new List<Vector3>{ new Vector3(0,0,3), new Vector3(3,0,0) }, 4f)==0, "Empate: gana el primero");
    Check(Workbench.NearestInRange(o, new List<Vector3>{ new Vector3(0,5,1) }, 4f)==0, "La altura no cuenta (distancia en el plano)");
    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
