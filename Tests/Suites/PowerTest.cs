using System; using System.Reflection; using System.Collections.Generic; using UnityEngine;
using Nexus7.Power; using Nexus7.Inventory; using Nexus7.Core; using Nexus7.Hazards; using Nexus7.Security;
static class T {
  const BindingFlags F = BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance;
  static void Set(object o, string f, object v){ var t=o.GetType(); FieldInfo fi=null; while(t!=null && fi==null){ fi=t.GetField(f,F|BindingFlags.DeclaredOnly); t=t.BaseType; } fi.SetValue(o,v); }
  static void Call(object o, string m, params object[] a){ var t=o.GetType(); MethodInfo mi=null; while(t!=null && mi==null){ mi=t.GetMethod(m,F|BindingFlags.DeclaredOnly); t=t.BaseType; } mi.Invoke(o,a); }
  static T2 Make<T2>() where T2: Component, new() { var c=new T2(); c.transform=new Transform(); c.name=typeof(T2).Name; return c; }
  static int fails=0; static void Check(bool c,string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }
  static void Main(){
    var grid = Make<PowerGrid>(); Call(grid,"Awake");
    int events=0; grid.PowerChanged += (a,b)=>events++;
    var breaker = Make<PowerConsole>(); Set(breaker,"objectiveLabel","Interruptor");
    var b2 = Make<PowerConsole>(); Set(b2,"objectiveLabel","Zocalo");
    var b3 = Make<PowerConsole>(); Set(b3,"objectiveLabel","Zocalo B");
    var reactor = Make<PowerConsole>(); Set(reactor,"requiredGridPower",3); Set(reactor,"actionLabel","Arrancar");
    foreach (var s in new[]{breaker,b2,b3,reactor}) Call(s,"Start");
    Check(PowerGrid.Power==0 && grid.State==PowerState.Offline, "Red empieza OFFLINE (0/4)");
    // consumidores
    var c1 = Make<PowerConsumer>(); Set(c1,"requiredPower",1); Call(c1,"Start");
    var c4 = Make<PowerConsumer>(); Set(c4,"requiredPower",4); Call(c4,"Start");
    Check(!c1.IsPowered && !c4.IsPowered, "Puertas sin energía al inicio");
    Call(reactor,"OnInteract", new object[]{null});
    Check(!reactor.IsActive && reactor.Prompt.Contains("0/3"), "Reactor no arranca sin 3 bloques (prompt muestra 0/3)");
    Call(breaker,"OnInteract", new object[]{null});
    Check(PowerGrid.Power==1 && grid.State==PowerState.Partial && c1.IsPowered && !c4.IsPowered, "Interruptor → 1/4 PARCIAL, puerta secundaria con energía");
    Check(!breaker.CanInteract(null), "Fuente activa ya no es interactuable");
    Call(b2,"OnInteract", new object[]{null}); Call(b3,"OnInteract", new object[]{null});
    Check(PowerGrid.Power==3, "Tres fuentes → 3/4");
    Call(reactor,"OnInteract", new object[]{null});
    Check(reactor.IsActive && PowerGrid.Power==4 && grid.State==PowerState.Restored && c4.IsPowered, "Reactor arranca con 3 → 4/4 RESTAURADA, compuerta principal con energía");
    Check(events==4, "Evento PowerChanged una vez por cambio (4)");
    // Hackeo: barrera
    var bar = Make<ElectricBarrier>(); Time.time = 100f; bar.Disable(25f);
    Check(bar.IsDisabled && Math.Abs(bar.RemainingDisabledTime-25f)<0.01f, "Barrera hackeada 25 s");
    Time.time = 110f; Check(Math.Abs(bar.RemainingDisabledTime-15f)<0.01f, "Cuenta atrás: quedan 15 s");
    var term = Make<HackTerminal>(); Set(term,"targets", new MonoBehaviour[]{ bar });
    Check(term.TargetsDisabled && term.TargetLabel=="Barrera eléctrica" && Math.Abs(term.RemainingTime-15f)<0.01f, "Terminal ve su objetivo desactivado y el tiempo restante");
    Check(!term.CanInteract(null), "No se puede volver a hackear mientras sigue desactivada");
    // Inventario entre escenas
    var bat=new ItemData(); Set(bat,"displayName","Bateria"); Set(bat,"maxStack",5);
    var inv=new InventorySystem(); inv.Add(bat,2); GameSession.SaveInventory(inv);
    var next=new InventorySystem(); Set(next,"startingItems", new List<ItemStack>{ new ItemStack(bat,9) }); Call(next,"Awake");
    Check(next.GetCount(bat)==2, "Nueva escena carga el inventario guardado (2), no el equipo por defecto");
    GameSession.Clear(); var fresh=new InventorySystem(); Set(fresh,"startingItems", new List<ItemStack>{ new ItemStack(bat,1) }); Call(fresh,"Awake");
    Check(fresh.GetCount(bat)==1, "Sin sesión guardada → usa el equipo por defecto");
    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
