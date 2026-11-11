using System; using System.Collections.Generic; using System.Reflection;
using Nexus7.Core; using Nexus7.Inventory;
static class T {
  static int fails=0; static void Check(bool c,string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }
  static void Set(object o,string f,object v){ o.GetType().GetField(f,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(o,v); }
  static ItemData Item(string id){ var i=new ItemData(); Set(i,"id",id); Set(i,"displayName",id); i.name=id; return i; }
  static void Main(){
    // ---------- Tiempo relativo y descripción ----------
    Check(SaveSlot.RelativeTime(TimeSpan.FromSeconds(30))=="hace un momento", "Menos de 1 min: «hace un momento»");
    Check(SaveSlot.RelativeTime(TimeSpan.FromMinutes(5))=="hace 5 min", "5 min");
    Check(SaveSlot.RelativeTime(new TimeSpan(2,10,0))=="hace 2 h", "2 h 10 min → «hace 2 h»");
    Check(SaveSlot.RelativeTime(TimeSpan.FromDays(1))=="hace 1 día" && SaveSlot.RelativeTime(TimeSpan.FromDays(3.5))=="hace 3 días", "Días (singular y plural)");
    Check(SaveSlot.RelativeTime(TimeSpan.FromMinutes(-5))=="hace un momento", "Reloj adelantado (tiempo negativo): «hace un momento»");
    var now=new DateTime(2026,11,11,20,0,0,DateTimeKind.Utc);
    var d=new SaveData{ version=SaveSlot.Version, scene="SalaEnergia", savedAtUtcTicks=now.AddHours(-2).Ticks };
    Check(SaveSlot.Describe(d,now,true)=="Fase 3 · Sala de energía — hace 2 h", "Descripción: «Fase 3 · Sala de energía — hace 2 h»");
    Check(SaveSlot.Describe(d,now,false)=="Partida no disponible", "Escena que ya no está en Build Settings: «Partida no disponible»");
    Check(SaveSlot.Describe(null,now,true)=="Sin partida guardada", "Sin datos: «Sin partida guardada»");
    // ---------- Compatibilidad y lectura ----------
    Check(SaveSlot.IsCompatible(d) && !SaveSlot.IsCompatible(new SaveData{version=2,scene="SalaEnergia"}) && !SaveSlot.IsCompatible(new SaveData{version=1,scene=""}) && !SaveSlot.IsCompatible(null), "Compatible solo la versión 1 con escena");
    Check(SaveSlot.Parse("")==null && SaveSlot.Parse(null)==null && SaveSlot.Parse("{")==null, "Archivo vacío o corrupto → sin partida (sin excepción)");
    Check(SaveSlot.Parse("{\"version\":2,\"scene\":\"SalaEnergia\"}")==null, "Otra versión → sin partida");
    var ok=SaveSlot.Parse("{\"version\":1,\"scene\":\"Laboratorio\"}");
    Check(ok!=null && ok.scene=="Laboratorio", "JSON válido → se lee");
    // ---------- Inventario ----------
    var bat=Item("bateria"); var chat=Item("chatarra");
    var saved=SaveSlot.ToSaved(new[]{ new ItemStack(bat,2), new ItemStack(chat,5) });
    Check(saved.Count==2 && saved[0].id=="bateria" && saved[0].amount==2 && saved[1].id=="chatarra" && saved[1].amount==5, "Inventario → lista de Id + cantidad");
    saved.Add(new SavedItem{ id="objeto_borrado", amount=1 });
    int unknown;
    var back=SaveSlot.FromSaved(saved, id => id=="bateria"?bat:id=="chatarra"?chat:null, out unknown);
    Check(back.Count==2 && back[0].item==bat && back[0].amount==2 && back[1].amount==5 && unknown==1, "Lista → inventario; un Id que ya no existe se ignora y se cuenta");
    var empty=SaveSlot.FromSaved(null, id=>null, out unknown); Check(empty.Count==0 && unknown==0, "Sin lista: inventario vacío");
    // ---------- Fases ----------
    Check(!GameFlow.IsPhaseScene("MenuPrincipal") && GameFlow.IsPhaseScene("Laboratorio") && GameFlow.IsPhaseScene("Evacuacion"), "Escenas de fase (el menú no)");
    Check(GameFlow.PhaseTitle("SalaInicial")=="Fase 1 · Sala inicial" && GameFlow.PhaseTitle("CentroControl")=="Fase 5 · Centro de control" && GameFlow.PhaseTitle("Otra")=="Otra", "Títulos de fase");
    // ---------- Estadísticas ----------
    RunStats.Reset(); RunStats.RecordDeath(); RunStats.RecordDeath(); RunStats.RecordCrafted(3);
    var snap=RunStats.Snapshot(); RunStats.Reset(); RunStats.Restore(snap);
    Check(RunStats.Deaths==2 && RunStats.ItemsCrafted==3, "Las estadísticas se guardan y se restauran");
    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
