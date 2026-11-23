using System;
using System.Collections.Generic;
using Nexus7.Director;
using Nexus7.Enemies;
static class T {
  static int fails=0; static void Check(bool c,string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }
  // Ruta en anillo de 6 waypoints alrededor de (20, 0), radio 8
  static List<Point2> Ring(float cx, float cz, float r, int n){ var l=new List<Point2>(); for(int i=0;i<n;i++){ double a=2*Math.PI*i/n; l.Add(new Point2(cx+(float)(r*Math.Cos(a)), cz+(float)(r*Math.Sin(a)))); } return l; }
  static Point2 O = new Point2(0,0), Fwd = new Point2(1,0);

  static void NoRepeat(){
    var m = new SpawnVariationModel(1234); var wps = Ring(20,0,8,6);
    int prevSig=-1; bool differ=true; var starts=new HashSet<int>();
    for(int a=0;a<12;a++){ m.BeginAttempt(); var v=m.Plan("RobotSeguridad_A",wps,O,Fwd); int sig=v.StartIndex*2+(v.Direction>0?1:0); differ&=sig!=prevSig; prevSig=sig; starts.Add(v.StartIndex); }
    Check(differ, "Dos intentos seguidos nunca repiten (waypoint, sentido)");
    Check(starts.Count>=3, "A lo largo de 12 intentos aparece en varios waypoints distintos ("+starts.Count+")");
    var m2 = new SpawnVariationModel(1234); int same=0; var m3=new SpawnVariationModel(1234);
    for(int a=0;a<5;a++){ m2.BeginAttempt(); m3.BeginAttempt(); if(m2.Plan("X",wps,O,Fwd).StartIndex==m3.Plan("X",wps,O,Fwd).StartIndex) same++; }
    Check(same==5, "Misma semilla → mismas decisiones (reproducible)");
  }
  static void Fairness(){
    var m = new SpawnVariationModel(7);
    // Ruta que pasa por encima del punto de reaparición: el waypoint 0 está a 2 m
    var wps = new List<Point2>{ new Point2(2,0), new Point2(14,10), new Point2(14,-10), new Point2(-14,0) };
    bool safe=true; for(int a=0;a<20;a++){ m.BeginAttempt(); var v=m.Plan("R",wps,O,Fwd); safe&= v.StartIndex!=0; }
    Check(safe, "Nunca aparece a menos de 8 m del jugador si hay otra opción");
    var view = new List<Point2>{ new Point2(12,0), new Point2(-12,0) };   // el primero, justo delante
    int inView=0; for(int a=0;a<20;a++){ m.BeginAttempt(); if(m.Plan("V",view,O,Fwd).StartIndex==0) inView++; }
    Check(inView<=4, "Evita aparecer dentro del campo de visión inicial ("+inView+"/20 delante)");
    Check(m.InView(new Point2(10,1),O,Fwd) && !m.InView(new Point2(-10,0),O,Fwd) && !m.InView(new Point2(40,0),O,Fwd), "Cono de visión: delante sí, detrás no, lejos no");
    var only = new List<Point2>{ new Point2(1,0) }; m.BeginAttempt(); Check(m.Plan("Solo",only,O,Fwd).StartIndex==0, "Con un único punto lo usa (sin excepción)");
    var none = new List<Point2>(); m.BeginAttempt(); var nv=m.Plan("Nada",none,O,Fwd); Check(nv.StartIndex==0 && (nv.Direction==1||nv.Direction==-1), "Sin ruta: valores por defecto");
  }
  static void Rhythm(){
    var m = new SpawnVariationModel(99); var wps=Ring(0,30,6,4); bool ok=true; var waits=new HashSet<float>();
    for(int a=0;a<10;a++){ m.BeginAttempt(); var v=m.Plan("R",wps,O,Fwd); ok&= v.WaitMultiplier>=0.7f&&v.WaitMultiplier<=1.3f&&v.SpeedMultiplier>=0.92f&&v.SpeedMultiplier<=1.08f; waits.Add((float)Math.Round(v.WaitMultiplier,3)); }
    Check(ok && waits.Count>=8, "Ritmo y velocidad varían dentro de sus rangos");
    var g = new SpawnVariationModel(5); var fixedPost = new List<Point2>{ new Point2(20,0) }; float prevW=-1; bool diff=true;
    for(int a=0;a<6;a++){ g.BeginAttempt(); var v=g.Plan("Guardia",fixedPost,O,Fwd); if(prevW>=0) diff&=Math.Abs(v.WaitMultiplier-prevW)>1e-4f; prevW=v.WaitMultiplier; }
    Check(diff, "Guardia fijo (un solo punto): el ritmo cambia en cada intento");
  }
  static void Reinforcements(){
    var m = new SpawnVariationModel(42);
    var routes = new List<IReadOnlyList<Point2>>{ Ring(20,0,5,4), Ring(-20,0,5,4), Ring(0,25,5,4), Ring(0,-25,5,4) };
    m.BeginAttempt(); var a = m.PlanReinforcements(2,3,routes,O,Fwd);
    Check(a.Count==2 && a[0].Route!=a[1].Route, "Dos refuerzos por rutas distintas");
    m.BeginAttempt(); var b = m.PlanReinforcements(2,3,routes,O,Fwd);
    bool compositionChanged = a[0].Template!=b[0].Template || a[1].Template!=b[1].Template;
    bool routesChanged = !(new HashSet<int>{a[0].Route,a[1].Route}).SetEquals(new HashSet<int>{b[0].Route,b[1].Route});
    Check(compositionChanged && routesChanged, "El intento siguiente cambia tipos y rutas de los refuerzos");
    bool far=true; foreach(var p in b){ far&= Point2.Distance(routes[p.Route][p.StartIndex],O)>=8f; }
    Check(far, "Los refuerzos aparecen lejos del jugador");
    Check(m.PlanReinforcements(0,3,routes,O,Fwd).Count==0 && m.PlanReinforcements(2,0,routes,O,Fwd).Count==0, "Sin presupuesto o sin plantillas: ninguno");
  }
  static void Cameras(){
    var m = new SpawnVariationModel(3); float f1,f2; bool r1,r2;
    m.BeginAttempt(); m.PlanCameraSweep("Cam_1",out f1,out r1); m.BeginAttempt(); m.PlanCameraSweep("Cam_1",out f2,out r2);
    Check(f1>=-1f&&f1<=1f&&Math.Abs(f1-f2)>1e-4f, "Las cámaras empiezan su barrido en otro punto en cada intento");
    Check(SpawnVariationModel.Hash(1,2,"a")==SpawnVariationModel.Hash(1,2,"a") && SpawnVariationModel.Hash(1,2,"a")!=SpawnVariationModel.Hash(1,3,"a"), "Hash estable y distinto por intento");
  }
  static void Main(){
    NoRepeat(); Fairness(); Rhythm(); Reinforcements(); Cameras();
    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
