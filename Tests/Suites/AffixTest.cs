using System;
using System.Collections.Generic;
using Nexus7.Director;
static class T {
  static int fails=0; static void Check(bool c,string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }
  static void Counts(){
    Check(RobotAffixModel.CountFor(0.3f,6)==0 && RobotAffixModel.CountFor(0.5f,6)==1 && RobotAffixModel.CountFor(0.7f,6)==2 && RobotAffixModel.CountFor(0.9f,6)==3, "0/1/2/3 rasgos según la tensión (0,4 · 0,6 · 0,8)");
    Check(RobotAffixModel.CountFor(0.9f,2)==1 && RobotAffixModel.CountFor(0.9f,3)==2 && RobotAffixModel.CountFor(0.9f,0)==0, "Nunca más de la mitad de los robots");
  }
  static void Stats(){
    Check(RobotAffixModel.Health(RobotAffix.Armored)==1.4f && RobotAffixModel.Speed(RobotAffix.Swift)==1.2f && RobotAffixModel.Detection(RobotAffix.Hawkeye)==1.3f && RobotAffixModel.Spread(RobotAffix.Hawkeye)==0.8f, "Blindado ×1,4 vida · Rápido ×1,2 velocidad · Ojo de águila ×1,3 detección y ×0,8 dispersión");
    Check(RobotAffixModel.Health(RobotAffix.None)==1f && RobotAffixModel.Speed(RobotAffix.Armored)==1f && RobotAffixModel.Spread(RobotAffix.Swift)==1f, "Cada rasgo solo toca lo suyo");
  }
  static void Assign(){
    var robots = new List<string>{ "A","B","C","D","E","F" };
    var first = RobotAffixModel.Assign(robots, 2, 11, null);
    Check(first.Count==2, "Reparte tantos rasgos como toca");
    var kinds = new HashSet<RobotAffix>(first.Values);
    Check(kinds.Count==2 && !kinds.Contains(RobotAffix.None), "Rasgos distintos en el mismo intento");
    bool fresh = true;
    for (int s=0; s<30; s++){ var next = RobotAffixModel.Assign(robots, 2, s, first); foreach (var k in next.Keys) fresh &= !first.ContainsKey(k); }
    Check(fresh, "El intento siguiente elige robots que no tenían rasgo (30 semillas)");
    var small = new List<string>{ "A","B" };
    var p = new Dictionary<string,RobotAffix>{ {"A",RobotAffix.Armored}, {"B",RobotAffix.Swift} };
    bool changed = true;
    for (int s=0; s<30; s++){ var n = RobotAffixModel.Assign(small, 2, s, p); foreach (var kv in n) changed &= kv.Value != p[kv.Key]; }
    Check(changed, "Si tiene que repetir robot, le cambia el rasgo");
    Check(RobotAffixModel.Assign(robots, 0, 1, null).Count==0 && RobotAffixModel.Assign(null, 2, 1, null).Count==0, "Sin rasgos con tensión baja o sin robots");
    var a = RobotAffixModel.Assign(robots, 3, 5, null); var b = RobotAffixModel.Assign(robots, 3, 5, null);
    bool same = a.Count==b.Count; foreach (var kv in a) same &= b.ContainsKey(kv.Key) && b[kv.Key]==kv.Value;
    Check(same, "Misma semilla, mismo reparto (para depurar)");
  }
  static void Main(){
    Counts(); Stats(); Assign();
    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
