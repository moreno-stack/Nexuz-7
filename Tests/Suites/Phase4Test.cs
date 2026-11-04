using System; using System.Reflection; using System.Collections.Generic; using UnityEngine; using UnityEngine.AI;
using Nexus7.Inventory; using Nexus7.Crafting; using Nexus7.Core; using Nexus7.Enemies; using Nexus7.Stealth;
class FakeTarget : IEmpTarget { public Vector3 pos; public float d; public float hits; public Vector3 EmpPosition => pos; public void OnEmp(float x){ hits++; d=x; } }
class FakeDecoy : IDistraction { public bool IsActive {get;set;} = true; public Vector3 Position {get;set;} = new Vector3(1,0,1); public Vector3 Origin {get;set;} = new Vector3(9,0,9); public int neutralized; public void Neutralize(){ neutralized++; IsActive=false; } }
static class T {
  const BindingFlags F = BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance;
  static FieldInfo Fi(object o,string f){ var t=o.GetType(); while(t!=null){ var fi=t.GetField(f,F|BindingFlags.DeclaredOnly); if(fi!=null) return fi; t=t.BaseType;} throw new Exception(f); }
  static void Set(object o,string f,object v)=>Fi(o,f).SetValue(o,v);
  static object Get(object o,string f)=>Fi(o,f).GetValue(o);
  static void Call(object o,string m,params object[] a){ var t=o.GetType(); while(t!=null){ var mi=t.GetMethod(m,F|BindingFlags.DeclaredOnly); if(mi!=null){ mi.Invoke(o,a); return;} t=t.BaseType;} throw new Exception(m); }
  static ItemData Item(string n,int max){ var i=new ItemData(); Set(i,"displayName",n); Set(i,"maxStack",max); i.name=n; return i; }
  static int fails=0; static void Check(bool c,string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }
  static void Main(){
    var scrap=Item("Chatarra",20); var circ=Item("Circuito",10); var bat=Item("Bateria",5); var decoy=Item("Senuelo",5);
    var inv=new InventorySystem(); Set(inv,"capacity",4);
    var cs=new CraftingSystem(); Set(cs,"inventory",inv);
    var rec=new CraftingRecipe(); Set(rec,"ingredients",new List<ItemStack>{new ItemStack(scrap,3),new ItemStack(circ,1)}); Set(rec,"result",decoy); Set(rec,"displayName","Senuelo");
    cs.LearnRecipe(rec);
    Check(cs.TryCraft(rec)==CraftingSystem.CraftResult.MissingIngredients, "Sin recursos → MissingIngredients");
    inv.Add(scrap,7); inv.Add(circ,3);
    Check(cs.MaxCraftable(rec)==2, "MaxCraftable = min(7/3, 3/1) = 2");
    Check(cs.TryCraft(rec)==CraftingSystem.CraftResult.Success && inv.GetCount(decoy)==1 && inv.GetCount(scrap)==4 && inv.GetCount(circ)==2, "Fabricar consume 3 chatarra + 1 circuito y da 1 señuelo");
    // inventario lleno: capacidad 4 con chatarra, circuito, señuelo + bateria → lleno; resultado nuevo no cabe
    var emp=Item("PEM",1); var rec2=new CraftingRecipe(); Set(rec2,"ingredients",new List<ItemStack>{new ItemStack(scrap,1)}); Set(rec2,"result",emp);
    inv.Add(bat,1); inv.Add(bat,5); // bateria ocupa 2 slots → 5 slots? capacidad 4: chat,circ,senuelo,bat(5) y la 6ª bateria no cabe
    int before=inv.GetCount(scrap);
    Check(cs.TryCraft(rec2)==CraftingSystem.CraftResult.NoSpace && inv.GetCount(scrap)==before && inv.GetCount(emp)==0, "Inventario lleno → NoSpace y se devuelven los ingredientes");
    // PEM por radio
    var near=new FakeTarget{pos=new Vector3(0,0,0)}; EmpRegistry.Register(near);
    Check(EmpRegistry.Pulse(new Vector3(0,0,0),6f,6f)==1 && near.hits==1 && near.d==6f, "PEM afecta a objetivos en el radio con la duración indicada");
    EmpRegistry.Unregister(near);
    Check(EmpRegistry.Pulse(new Vector3(0,0,0),6f,6f)==0, "Objetivo dado de baja ya no recibe pulsos");
    // Banco manipulado
    var wb=new Workbench(); wb.transform=new Transform(); Time.time=100f; wb.RegisterCraft();
    Time.time=130f; Check(wb.WasTamperedRecently(45f), "Banco usado hace 30 s → manipulado (ventana 45 s)");
    Time.time=150f; Check(!wb.WasTamperedRecently(45f), "Pasados 50 s → ya no cuenta");
    Time.time=151f; wb.RegisterCraft(); wb.ClearTamper(); Check(!wb.WasTamperedRecently(45f), "Tras inspeccionarlo (ClearTamper) no vuelve a alertar");
    // Dron ante señuelos y PEM
    var dr=new EnemyPatrol(); dr.transform=new Transform(); var agent=new NavMeshAgent(); agent.isOnNavMesh=true; var vis=new EnemyVision();
    Set(dr,"agent",agent); Set(dr,"vision",vis); Time.time=200f;
    var d1=new FakeDecoy(); var d2=new FakeDecoy();
    var g1=new GameObject("a"); Call(dr,"OnDistraction",d1);
    Check(dr.CurrentState==EnemyPatrol.EnemyState.Investigate && Get(dr,"currentDistraction")==(object)d1, "1er señuelo → va a investigarlo");
    Call(dr,"StartSearch"); Check(d1.neutralized==1 && vis.IsWary, "Al llegar lo neutraliza y queda receloso");
    Time.time=220f; Call(dr,"OnDistraction",d2);
    Check(Get(dr,"currentDistraction")==null && dr.CurrentState==EnemyPatrol.EnemyState.Investigate && agent.speed==3.6f, "2º señuelo a los 20 s → deduce el engaño y va al origen corriendo");
    Time.time=300f; var d3=new FakeDecoy(); Call(dr,"OnDistraction",d3);
    Check(Get(dr,"currentDistraction")==(object)d3, "Pasada la memoria (40 s) vuelve a caer en un señuelo");
    dr.OnEmp(6f); Check(dr.CurrentState==EnemyPatrol.EnemyState.Stunned && vis.IsBlinded && agent.isStopped, "PEM → aturdido, ciego y detenido");
    Call(dr,"OnDistraction",new FakeDecoy()); Check(dr.CurrentState==EnemyPatrol.EnemyState.Stunned, "Aturdido ignora señuelos");
    Time.deltaTime=7f; Call(dr,"Update"); Check(dr.CurrentState==EnemyPatrol.EnemyState.Search && !vis.IsBlinded && !agent.isStopped, "Tras el PEM se reinicia y busca alrededor");
    // Camuflaje
    var st=new PlayerStealth(); Time.time=500f; st.ApplyCloak(12f,0.1f); Time.time=505f;
    Check(st.IsCloaked && Math.Abs(st.CloakRemaining-7f)<0.01f, "Camuflaje activo, quedan 7 s");
    Time.time=513f; Check(!st.IsCloaked, "Camuflaje expirado a los 12 s");
    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
