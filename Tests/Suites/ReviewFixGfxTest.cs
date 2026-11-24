using System; using System.IO; using System.Reflection; using System.Diagnostics;
using UnityEngine; using Nexus7.Inventory; using Nexus7.Visual.Models; using Nexus7.Devices; using Nexus7.Combat;
// Arreglos de la revisión de los gráficos
static class T {
  static int fails=0; static void Check(bool c,string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }
  const BindingFlags F = BindingFlags.NonPublic|BindingFlags.Instance;
  static object GetF(object o,string f){ var t=o.GetType(); while(t!=null){ var fi=t.GetField(f,F|BindingFlags.DeclaredOnly); if(fi!=null) return fi.GetValue(o); t=t.BaseType;} throw new Exception(f); }
  static void SetF(object o,string f,object v){ var t=o.GetType(); while(t!=null){ var fi=t.GetField(f,F|BindingFlags.DeclaredOnly); if(fi!=null){ fi.SetValue(o,v); return;} t=t.BaseType;} throw new Exception(f); }
  static string Src(string f){ return File.ReadAllText("Assets/_Nexus7/Scripts/" + f); }
  static void Main(){
    // I1: los iconos de la barra rápida no tapan las cinemáticas ni la pantalla de resultados
    Check(Src("UI/HotbarUI.cs").Contains("SetSiblingIndex(slotsText.transform.GetSiblingIndex() + 1)"), "Iconos de la barra rápida justo encima de su texto en el orden de dibujo (no encima de todo el HUD)");
    // I2: el aviso del banco y los mensajes quedan por encima de la fila de iconos (84–128 px)
    string sala = Src("Editor/SalaInicialBuilder.cs");
    Check(sala.Contains("new Vector2(-300f, 150f)") && sala.Contains("\"TextoBanco\", canvas, new Vector2(0.5f, 0f), new Vector2(0f, 150f)"), "Aviso del banco de trabajo a 150 px (encima de los iconos)");
    Check(sala.Contains("\"TextoMensajes\",    go.transform, new Vector2(0.5f, 0f),   new Vector2(0, 190)"), "Mensajes a 190 px (no pisan los iconos)");
    // I3: las chatarras de la Fase 1 también tienen modelo
    Check(!Src("Editor/AlmacenCargaBuilder.cs").Contains("var visual = GameObject.CreatePrimitive(shape);"), "El constructor ya no pone cubos como objetos sueltos");
    var scrap = new ItemData(); SetF(scrap,"id","scrap_metal"); SetF(scrap,"category",ItemCategory.Part); SetF(scrap,"uiColor",new Color(0.75f,0.75f,0.8f));
    var pick = new PickupItem(); pick.transform = new Transform(); pick.gameObject = new GameObject("Pickup");
    SetF(pick,"item",scrap); Application.isPlaying = true;
    var awake = typeof(PickupItem).GetMethod("Awake", F); awake.Invoke(pick, null);
    Check(GetF(pick,"visual") != null, "Un objeto suelto sin forma monta su modelo al empezar");
    var pick2 = new PickupItem(); pick2.transform = new Transform(); pick2.gameObject = new GameObject("Vacío"); awake.Invoke(pick2, null);
    Check(GetF(pick2,"visual") == null, "Sin objeto asignado no monta nada (el botín lo monta después)");
    Application.isPlaying = false;
    // I4: los iconos se dibujan deprisa (antes ~18 ms cada uno: tirón al empezar la fase)
    var list = new System.Collections.Generic.List<ModelBlueprint>();
    var g = new MColor(0.5f,0.9f,0.4f);
    foreach (var id in new[]{"battery_ion","scrap_metal","component_circuit","component_optic_crystal","tool_hydraulic_wrench","key_director_card","key_reactor_module"}) list.Add(ModelLibrary.Item(id, ItemCategory.Tool, g));
    foreach (DeviceKind k in Enum.GetValues(typeof(DeviceKind))) list.Add(ModelLibrary.Device(k, g));
    foreach (var b in list) IconRasterizer.Render(b, 16);   // calentar el JIT
    var sw = Stopwatch.StartNew();
    foreach (var b in list) IconRasterizer.Render(b, 64);
    sw.Stop();
    Check(sw.ElapsedMilliseconds < 80, "11 iconos en menos de 80 ms (" + sw.ElapsedMilliseconds + " ms)");
    // I5: el inventario es más ancho para que el texto no se parta al lado de la rejilla
    string inv = Src("UI/InventoryUI.cs");
    Check(inv.Contains("PanelExtraWidth = 180f") && inv.Contains("TextWidth = 560f"), "Inventario: panel 180 px más ancho y texto de 560 px junto a la rejilla");
    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
