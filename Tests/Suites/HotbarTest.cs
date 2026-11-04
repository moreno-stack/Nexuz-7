using System; using System.Collections.Generic; using System.Reflection;
using Nexus7.Inventory;
static class T {
  static void Set(object o, string f, object v){ o.GetType().GetField(f, BindingFlags.NonPublic|BindingFlags.Instance).SetValue(o, v); }
  static ItemData Item(string n){ var i = new ItemData(); Set(i,"displayName",n); Set(i,"maxStack",10); Set(i,"category",ItemCategory.Part); i.name=n; return i; }
  static int fails=0; static void Check(bool c, string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }

  static void SlotsFollowArrivalOrder(){
    var a=Item("A"); var b=Item("B"); var c=Item("C");
    var h = new HotbarModel(); h.Rebuild(new List<ItemData>{ b, a, b, c });
    Check(h.Get(0)==b && h.Get(1)==a && h.Get(2)==c && h.Get(3)==null, "Orden de llegada sin repetidos: B, A, C");
    Check(h.Count==3, "3 casillas ocupadas");
  }
  static void MaxNineDistinct(){
    var list = new List<ItemData>(); for(int i=0;i<11;i++) list.Add(Item("I"+i));
    var h = new HotbarModel(); h.Rebuild(list);
    Check(h.Get(8)==list[8] && h.Get(9)==null && h.Count==9, "Máximo 9 casillas");
    h.Select(12); Check(h.Selected==0, "Seleccionar fuera de rango no cambia nada");
    h.Select(8); Check(h.SelectedItem==list[8], "Tecla 9 → novena casilla");
  }
  static void SelectedStackDepleted(){
    var d=Item("Señuelo"); var e=Item("PEM");
    var h = new HotbarModel(); h.Rebuild(new List<ItemData>{ d, e }); h.Select(0);
    h.Rebuild(new List<ItemData>{ e });
    Check(h.SelectedItem==e && h.Selected==0, "Pila seleccionada agotada → pasa a la siguiente (PEM)");
    h.Rebuild(new List<ItemData>());
    Check(h.SelectedItem==null && h.Selected==0, "Inventario vacío → sin selección y sin excepción");
    var x=Item("X"); var y=Item("Y"); var z=Item("Z");
    h.Rebuild(new List<ItemData>{ x, y, z }); h.Select(2);
    h.Rebuild(new List<ItemData>{ y, z });
    Check(h.SelectedItem==z && h.Selected==1, "La selección sigue al objeto aunque cambie de casilla");
  }
  static void CycleWraps(){
    var a=Item("A"); var b=Item("B"); var c=Item("C");
    var h = new HotbarModel(); h.Rebuild(new List<ItemData>{ a, b, c });
    h.Select(2); h.Cycle(1); Check(h.Selected==0, "Siguiente desde la última vuelve a la 1");
    h.Cycle(-1); Check(h.Selected==2, "Anterior desde la 1 va a la última");
    var empty = new HotbarModel(); empty.Rebuild(new List<ItemData>()); empty.Cycle(1); Check(empty.Selected==0, "Ciclar vacío no rompe");
  }
  static void Main(){
    SlotsFollowArrivalOrder(); MaxNineDistinct(); SelectedStackDepleted(); CycleWraps();
    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
