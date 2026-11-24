using System; using System.IO; using System.Reflection;
using UnityEngine; using Nexus7.Inventory; using Nexus7.Devices; using Nexus7.UI; using Nexus7.Visual.Models;
static class T {
  static int fails=0; static void Check(bool c,string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }
  const BindingFlags F = BindingFlags.NonPublic|BindingFlags.Instance;
  static void SetF(object o,string f,object v){ var t=o.GetType(); while(t!=null){ var fi=t.GetField(f,F|BindingFlags.DeclaredOnly); if(fi!=null){ fi.SetValue(o,v); return;} t=t.BaseType;} throw new Exception(f); }
  static ItemData Item(string id, ItemCategory cat, Color c){ var i = new ItemData(); SetF(i,"id",id); SetF(i,"category",cat); SetF(i,"uiColor",c); return i; }
  static string Src(string f){ return File.ReadAllText("Assets/_Nexus7/Scripts/" + f); }
  static void Main(){
    var battery = Item("battery_ion", ItemCategory.Battery, new Color(0.55f,1f,0.35f));
    var wrench = Item("tool_hydraulic_wrench", ItemCategory.Tool, new Color(1f,0.65f,0.2f));
    var emp = new DeviceItemData(); SetF(emp,"id","device_emp"); SetF(emp,"category",ItemCategory.Device); SetF(emp,"deviceType",DeviceKind.EmpPulse); SetF(emp,"deviceColor",new Color(0.3f,0.6f,1f));
    Check(ItemModels.For(battery).Key == "item_battery_ion", "Un objeto normal usa el modelo de su Id");
    Check(ItemModels.For(emp).Key == "device_EmpPulse", "Un dispositivo usa el modelo de su tipo");
    Check(ItemModels.For(null) != null, "Sin objeto → forma genérica (nunca nulo)");
    var s1 = ItemIcons.For(battery);
    Check(s1 != null && ReferenceEquals(s1, ItemIcons.For(battery)), "El icono se crea una vez y se reutiliza (caché)");
    Check(!ReferenceEquals(s1, ItemIcons.For(wrench)), "Cada objeto tiene su icono");
    Check(ItemIcons.For(null) == null, "Sin objeto → sin icono");
    Check(ItemIcons.Size == 64, "Iconos de 64 px");

    string hb = Src("UI/HotbarUI.cs"), inv = Src("UI/InventoryUI.cs");
    Check(hb.Contains("ItemIcons.For(") && hb.Contains("IconSlot"), "Barra rápida: fila de iconos");
    Check(inv.Contains("ItemIcons.For(") && inv.Contains("GridColumns = 4"), "Inventario: rejilla de iconos de 4 columnas");
    Check(!hb.Contains("ItemIcons.For(it)") || hb.Contains("iconCache") || hb.Contains("shownItems"), "La barra rápida no vuelve a pedir iconos si no cambia");
    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
