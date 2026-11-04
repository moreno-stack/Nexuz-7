using System; using System.Collections.Generic; using Nexus7.Power;
static class T {
  static int fails=0; static void Check(bool c,string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }
  static void Main(){
    var south = new[]{ EnergyArea.South, EnergyArea.Central };
    var inner = new[]{ EnergyArea.EastWing, EnergyArea.WestWing, EnergyArea.Reactor };
    bool all=true; int n=0;
    foreach (var sb in south) foreach (var sc in south) foreach (var ib in inner) foreach (var ic in inner)
    { n++; var loot = new List<(EnergyArea, string)>{ (sb,"battery"), (sc,"module"), (sc,"circuit"), (ib,"battery"), (ic,"circuit") };
      if (!EnergyRoomProgress.Solvable(0,0,loot)) { all=false; Console.WriteLine("  sin solución: "+sb+" "+sc+" "+ib+" "+ic); } }
    Check(all, "Las "+n+" combinaciones de garantizados (módulo del reactor en el sur) se pueden terminar llegando sin nada");
    Check(!EnergyRoomProgress.Solvable(0,0,new List<(EnergyArea,string)>{ (EnergyArea.South,"battery"), (EnergyArea.South,"module") }), "Con una sola batería la sala no tiene solución (la prueba detecta el bloqueo)");
    Check(!EnergyRoomProgress.Solvable(0,0,new List<(EnergyArea,string)>{ (EnergyArea.EastWing,"battery"), (EnergyArea.South,"battery"), (EnergyArea.South,"module") }) == false, "Batería en el ala este: se llega con la del sur");
    Check(!EnergyRoomProgress.Solvable(0,0,new List<(EnergyArea,string)>{ (EnergyArea.Reactor,"battery"), (EnergyArea.Reactor,"battery"), (EnergyArea.South,"module") }), "Sin batería accesible al principio no se puede empezar");
    Check(EnergyRoomProgress.AreaOpen(EnergyArea.Central,0) && !EnergyRoomProgress.AreaOpen(EnergyArea.EastWing,0) && EnergyRoomProgress.AreaOpen(EnergyArea.WestWing,1) && !EnergyRoomProgress.AreaOpen(EnergyArea.Reactor,1) && EnergyRoomProgress.AreaOpen(EnergyArea.Reactor,2), "Zonas abiertas según la energía");
    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
