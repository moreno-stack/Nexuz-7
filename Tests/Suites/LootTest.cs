using System; using System.Collections.Generic; using System.Linq; using System.Reflection;
using Nexus7.Inventory;
static class T {
  static void Set(object o, string f, object v){ o.GetType().GetField(f, BindingFlags.NonPublic|BindingFlags.Instance).SetValue(o, v); }
  static ItemData Item(string n){ var i = new ItemData(); Set(i,"displayName",n); Set(i,"maxStack",20); Set(i,"category",ItemCategory.Part); i.name=n; return i; }
  static int fails=0; static void Check(bool c, string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }

  static ItemData bat=Item("Bateria"), scrap=Item("Chatarra"), circ=Item("Circuito");
  static List<LootEntry> Table(){ return new List<LootEntry>{ new LootEntry(scrap,3f,1,3), new LootEntry(circ,1f,1,1) }; }
  static float[] Empty(int n, float c){ var a=new float[n]; for(int i=0;i<n;i++) a[i]=c; return a; }

  static void SameSeedSameResult(){
    var g = new List<ItemStack>{ new ItemStack(bat,2) };
    var a = LootRoller.Roll(42, 8, Empty(8,0.2f), g, Table()); var b = LootRoller.Roll(42, 8, Empty(8,0.2f), g, Table());
    Check(a.Count==b.Count && a.Zip(b,(x,y)=>x.point==y.point && x.item==y.item && x.amount==y.amount).All(z=>z), "Misma semilla → mismo botín");
    var c = LootRoller.Roll(43, 8, Empty(8,0.2f), g, Table());
    Check(!(a.Count==c.Count && a.Zip(c,(x,y)=>x.point==y.point && x.item==y.item && x.amount==y.amount).All(z=>z)), "Otra semilla → botín distinto");
  }
  static void GuaranteedAlwaysPlaced(){
    var g = new List<ItemStack>{ new ItemStack(bat,3), new ItemStack(circ,1) };
    bool ok=true;
    for(int s=0;s<200;s++){
      var r = LootRoller.Roll(s, 10, Empty(10,0.5f), g, Table());
      if (r.Where(p=>p.item==bat && p.guaranteed).Sum(p=>p.amount)<3 || r.Where(p=>p.item==circ && p.guaranteed).Sum(p=>p.amount)<1) ok=false;
    }
    Check(ok, "En 200 partidas los objetos garantizados siempre aparecen");
  }
  static void WeightsRespected(){
    int s3=0, s1=0;
    for(int s=0;s<10000;s++){ var r=LootRoller.Roll(s,1,Empty(1,0f),new List<ItemStack>(),Table()); if(r[0].item==scrap) s3++; else s1++; }
    double ratio=(double)s3/s1; Check(ratio>2.7 && ratio<3.3, "Pesos 3:1 respetados ("+ratio.ToString("0.00")+")");
    var amounts = Enumerable.Range(0,500).Select(s=>LootRoller.Roll(s,1,Empty(1,0f),new List<ItemStack>(),Table())[0]).Where(p=>p.item==scrap).Select(p=>p.amount).ToList();
    Check(amounts.Min()==1 && amounts.Max()==3, "Cantidades entre el mínimo y el máximo (1–3)");
  }
  static void OnePerPoint(){
    var r = LootRoller.Roll(7, 12, Empty(12,0f), new List<ItemStack>{ new ItemStack(bat,1) }, Table());
    Check(r.Select(p=>p.point).Distinct().Count()==r.Count && r.Count==12, "Un objeto por punto");
    var e = LootRoller.Roll(7, 12, Empty(12,1f), new List<ItemStack>(), Table());
    Check(e.Count==0, "Probabilidad de vacío 1 → sin objetos aleatorios");
  }
  static void MoreGuaranteedThanPoints(){
    var g = new List<ItemStack>{ new ItemStack(bat,1), new ItemStack(circ,1), new ItemStack(scrap,2) };
    var r = LootRoller.Roll(1, 2, Empty(2,0f), g, Table());
    Check(r.Count(p=>p.guaranteed)==3, "Más garantizados que puntos: ninguno se pierde");
    Check(r.Where(p=>p.guaranteed).All(p=>p.point>=0 && p.point<2), "Los sobrantes van a un punto existente");
    var z = LootRoller.Roll(1, 0, new float[0], g, Table()); Check(z.Count==3 && z.TrueForAll(p=>p.point==-1), "Sala sin puntos: los garantizados van al propio spawner");
  }
  static void Main(){
    SameSeedSameResult(); GuaranteedAlwaysPlaced(); WeightsRespected(); OnePerPoint(); MoreGuaranteedThanPoints();
    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
