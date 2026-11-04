using System; using System.Reflection; using Nexus7.Inventory;
static class T {
  static void Set(object o, string f, object v){ o.GetType().GetField(f, BindingFlags.NonPublic|BindingFlags.Instance).SetValue(o, v); }
  static ItemData Item(string n, int max, ItemCategory c){ var i = new ItemData(); Set(i,"displayName",n); Set(i,"maxStack",max); Set(i,"category",c); i.name=n; return i; }
  static int fails=0; static void Check(bool c, string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }
  static void Main(){
    var bat = Item("Bateria",5,ItemCategory.Battery); var scrap = Item("Chatarra",20,ItemCategory.Part); var key = Item("Llave",1,ItemCategory.Tool);
    var inv = new InventorySystem(); Set(inv,"capacity",3);
    int addedEvents=0, removedEvents=0; inv.ItemAdded += (i,n)=>addedEvents+=n; inv.ItemRemoved += (i,n)=>removedEvents+=n;
    Check(inv.Add(bat,3)==3 && inv.UsedSlots==1, "Añadir 3 baterías → 1 slot");
    Check(inv.Add(bat,4)==4 && inv.GetCount(bat)==7 && inv.UsedSlots==2, "Añadir 4 más → apila 5 + 2 (2 slots)");
    Check(inv.CanAdd(scrap,20) && !inv.CanAdd(scrap,21), "CanAdd con 1 slot libre (MaxStack 20)");
    Check(inv.Add(scrap,25)==20 && inv.UsedSlots==3, "Añadir 25 chatarras con 1 slot → entran 20");
    Check(inv.Add(key,1)==0, "Inventario lleno → no entra la llave");
    Check(inv.Add(bat,3)==3 && inv.GetCount(bat)==10, "Rellenar pila incompleta de baterías aunque no haya slots libres");
    Check(inv.Remove(bat,6)==6 && inv.GetCount(bat)==4 && inv.UsedSlots==2, "Quitar 6 baterías → libera un slot");
    Check(!inv.TryRemove(bat,5) && inv.GetCount(bat)==4, "TryRemove 5 (hay 4) → no quita nada");
    var req = new[]{ new ItemStack(bat,2), new ItemStack(bat,2) };
    Check(inv.HasAll(req), "HasAll agrupa requisitos repetidos (2+2 ≤ 4)");
    Check(!inv.TryConsume(new[]{ new ItemStack(bat,2), new ItemStack(key,1) }) && inv.GetCount(bat)==4, "TryConsume atómico: falta la llave → no consume baterías");
    Check(inv.Add(key,1)==1 && inv.TryConsume(new[]{ new ItemStack(bat,2), new ItemStack(key,1) }) && inv.GetCount(bat)==2 && inv.GetCount(key)==0, "TryConsume con todo → consume");
    Check(inv.GetSummary().Count==2 && inv.GetSummary()[0].item==scrap, "GetSummary ordenado por categoría (Pieza antes que Batería)");
    Check(inv.GetCountById("Bateria")==2, "GetCountById usa el nombre si no hay id");
    Check(addedEvents==3+4+20+3+1 && removedEvents==6+2+1, "Eventos ItemAdded/ItemRemoved con cantidades reales");
    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
