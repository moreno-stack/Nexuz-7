using System;
using Nexus7.Combat;
using Nexus7.Director;
static class T {
  static int fails=0; static void Check(bool c,string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }
  static bool Near(float a,float b){ return Math.Abs(a-b)<0.001f; }

  static DebugContext Full(){
    return new DebugContext{ HasInventory=true, HasWeapon=true, HasDirector=true, HasShipEvents=true, KeyItemsMissing=3,
      Lives=2, MaxLives=3, Tension=0.5f, MinTension=0.1f, MaxTension=0.95f, PhaseIndex=1, PhaseCount=6 };
  }

  static void Basics(){
    Check(DebugPanelModel.Enabled(true,false) && DebugPanelModel.Enabled(false,true) && !DebugPanelModel.Enabled(false,false), "Solo en el editor o en compilaciones de desarrollo");
    Check(DebugPanelModel.Actions.Count==8 && DebugPanelModel.Key(DebugAction.KeyItems)==5 && DebugPanelModel.Key(DebugAction.NextPhase)==12, "8 acciones en F5–F12 (los números son de la barra rápida)");
    Check(DebugPanelModel.FromKey(9,out var a) && a==DebugAction.TensionUp && !DebugPanelModel.FromKey(4,out _) && !DebugPanelModel.FromKey(13,out _), "Tecla → acción (fuera de F5–F12 no hace nada)");
    bool labels=true; foreach (var x in DebugPanelModel.Actions) if (string.IsNullOrEmpty(DebugPanelModel.Label(x))) labels=false;
    Check(labels && DebugPanelModel.Label(DebugAction.KeyItems).Contains("imprescindibles"), "Todas tienen texto en español");
  }

  static void Availability(){
    var c=Full(); bool all=true;
    foreach (var x in DebugPanelModel.Actions) if (DebugPanelModel.Unavailable(x,c)!=null) all=false;
    Check(all, "En una fase normal todo está disponible");
    c=Full(); c.KeyItemsMissing=0; Check(DebugPanelModel.Unavailable(DebugAction.KeyItems,c)!=null, "Si ya tienes los imprescindibles, no los repite");
    c=Full(); c.HasInventory=false; Check(DebugPanelModel.Unavailable(DebugAction.KeyItems,c)=="sin inventario", "Sin inventario no da objetos");
    c=Full(); c.HasWeapon=false; Check(DebugPanelModel.Unavailable(DebugAction.Ammo,c)!=null && DebugPanelModel.Unavailable(DebugAction.Mod,c)!=null, "Sin arma: ni munición ni accesorio");
    c=Full(); c.Lives=3; Check(DebugPanelModel.Unavailable(DebugAction.Life,c)=="vidas al máximo", "Con las vidas al máximo no suma");
    c=Full(); c.Tension=0.95f; Check(DebugPanelModel.Unavailable(DebugAction.TensionUp,c)!=null && DebugPanelModel.Unavailable(DebugAction.TensionDown,c)==null, "Tensión al máximo: solo se puede bajar");
    c=Full(); c.Tension=0.1f; Check(DebugPanelModel.Unavailable(DebugAction.TensionDown,c)!=null, "Tensión al mínimo: no se puede bajar más");
    c=Full(); c.HasDirector=false; Check(DebugPanelModel.Unavailable(DebugAction.TensionUp,c)=="sin director", "Sin director no hay tensión");
    c=Full(); c.HasShipEvents=false; Check(DebugPanelModel.Unavailable(DebugAction.ShipEvent,c)!=null, "Sin director de eventos no fuerza nada");
    c=Full(); c.PhaseIndex=5; Check(DebugPanelModel.Unavailable(DebugAction.NextPhase,c)=="es la última fase", "En la Fase 6 no hay fase siguiente");
    c=Full(); c.PhaseIndex=-1; Check(DebugPanelModel.Unavailable(DebugAction.NextPhase,c)=="no es una fase", "Fuera de una fase (menú) no salta");
  }

  static void Values(){
    Check(Near(DebugPanelModel.Nudge(0.5f,1,0.1f,0.95f),0.6f) && Near(DebugPanelModel.Nudge(0.5f,-1,0.1f,0.95f),0.4f), "La tensión sube y baja 0,1");
    Check(Near(DebugPanelModel.Nudge(0.9f,1,0.1f,0.95f),0.95f) && Near(DebugPanelModel.Nudge(0.15f,-1,0.1f,0.95f),0.1f), "Sin salir de los límites del director");
    var m=new DirectorModel(1); m.SetTension(2f);
    Check(Near(m.Tension,m.MaxTension) && Near(m.Knobs.EnemySpeed, DirectorModel.KnobsFor(m.MaxTension).EnemySpeed), "SetTension limita y actualiza las perillas al momento");
    Check(DebugPanelModel.NextEvent(ShipEvent.None)==ShipEvent.Blackout && DebugPanelModel.NextEvent(ShipEvent.Blackout)==ShipEvent.SteamLeak
       && DebugPanelModel.NextEvent(ShipEvent.SteamLeak)==ShipEvent.SecuritySweep && DebugPanelModel.NextEvent(ShipEvent.SecuritySweep)==ShipEvent.Blackout, "Eventos de nave en rueda");
    Check(DebugPanelModel.NextMod(WeaponMod.None,m2=>true)==WeaponMod.Suppressor && DebugPanelModel.NextMod(WeaponMod.Suppressor,m2=>true)==WeaponMod.ExtendedMag
       && DebugPanelModel.NextMod(WeaponMod.Heatsink,m2=>true)==WeaponMod.Suppressor, "Accesorios en rueda");
    Check(DebugPanelModel.NextMod(WeaponMod.Stabilizer,m2=>m2!=WeaponMod.Heatsink)==WeaponMod.Suppressor, "Se salta el que no admite ninguna arma (disipador sin arma que se caliente)");
    Check(DebugPanelModel.NextMod(WeaponMod.None,m2=>false)==WeaponMod.None, "Si ninguno sirve, ninguno");
    Check(DebugPanelModel.NextPhase(0,6)==1 && DebugPanelModel.NextPhase(5,6)==-1 && DebugPanelModel.NextPhase(-1,6)==-1, "Índice de la fase siguiente");
    Check(DebugPanelModel.Missing(3,1)==2 && DebugPanelModel.Missing(3,5)==0, "Solo da lo que falta");
  }

  static void Main(){
    Basics(); Availability(); Values();
    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
