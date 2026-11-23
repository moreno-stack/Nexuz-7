using System;
using Nexus7.Combat;
static class T {
  static int fails=0; static void Check(bool c,string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }
  static bool Near(float a,float b,float e=0.01f){ return Math.Abs(a-b)<=e; }

  static void Catalog(){
    Check(EnemyGunCatalog.ForRobot("RobotSeguridad_A")==EnemyGunCatalog.Security && EnemyGunCatalog.ForRobot("RobotGuardia_Reactor")==EnemyGunCatalog.Guard && EnemyGunCatalog.ForRobot("RobotElite_Puente")==EnemyGunCatalog.Elite, "Arma según el tipo de robot");
    Check(EnemyGunCatalog.ForRobot("RobotInvestigacion_R1")==null, "Investigación no dispara (cuerpo a cuerpo)");
    Check(EnemyGunCatalog.ForRobot("RobotSeguridad_Refuerzo1")==EnemyGunCatalog.Security, "Los refuerzos también disparan");
    Check(EnemyGunCatalog.Security.BurstCount==3 && EnemyGunCatalog.Guard.BurstCount==4 && EnemyGunCatalog.Elite.BurstCount==5, "Ráfagas de 3, 4 y 5");
    Check(EnemyGunCatalog.Elite.PauseMax<EnemyGunCatalog.Security.PauseMax && EnemyGunCatalog.Security.Projectile.ProjectileSpeed<=40f, "Élite con pausas más cortas; proyectiles esquivables (≤ 40 m/s)");
  }
  static void Aim(){
    float near=AimModel.Spread(5f,0f,false,false,1), far=AimModel.Spread(20f,0f,false,false,1);
    Check(Near(near,1f) && Near(far,1f+12f*0.15f), "Dispersión: 1° de cerca y robot quieto, +0,15°/m desde 8 m");
    Check(AimModel.Spread(10f,6f,true,false,1)>AimModel.Spread(10f,1f,true,false,1) && AimModel.Spread(10f,1f,true,false,1)>AimModel.Spread(10f,0f,true,false,1), "Correr > caminar > quieto");
    Check(AimModel.Spread(10f,0f,true,true,1)>=AimModel.Spread(10f,0f,true,false,1)+5.9f, "Cegado: +6°");
    Check(AimModel.Spread(10f,0f,true,false,0)>AimModel.Spread(10f,0f,true,false,1) && AimModel.Spread(10f,0f,true,false,2)<AimModel.Spread(10f,0f,true,false,1), "Fácil falla más, Difícil menos");
    Check(AimModel.Spread(10f,0f,true,false,1,1.4f)>AimModel.Spread(10f,0f,true,false,1), "El director puede abrir la puntería si vas mal");
    float ax,ay,az; AimModel.Lead(0,0,10, 4,0,0, 14f,35f, out ax,out ay,out az);
    Check(Near(ax,0.8f) && Near(az,10f), "Anticipación del 50 %: 4 m/s × 0,4 s × 0,5 = 0,8 m");
  }
  static void Tokens(){
    Check(AttackTokens.CapacityFor(0,0)==1 && AttackTokens.CapacityFor(1,0)==2 && AttackTokens.CapacityFor(2,0)==3 && AttackTokens.CapacityFor(1,1)==3, "Turnos: 1/2/3 por dificultad, +1 con el director apretando");
    var t = new AttackTokens{ Capacity=2 };
    Check(t.TryAcquire(1) && t.TryAcquire(2) && !t.TryAcquire(3), "Solo 2 disparan a la vez en Normal");
    Check(t.TryAcquire(1) && t.InUse==2, "Pedirlo dos veces no ocupa dos turnos");
    t.Release(1); Check(t.TryAcquire(3), "Al soltar un turno lo coge otro");
  }
  static void Burst(){
    var b = new BurstModel(EnemyGunCatalog.Security);
    Check(b.Begin(0f) && b.Current==BurstModel.Phase.Warning && !b.Begin(0.1f), "Empieza con el aviso");
    Check(b.Tick(0.3f,true,0.5)==BurstEvent.None, "Durante el aviso (0,4 s) no dispara");
    int shots=0; float t=0.4f; for(int i=0;i<200 && b.Current!=BurstModel.Phase.Pause;i++){ if(b.Tick(t,true,0.5)==BurstEvent.Fire) shots++; t+=0.01f; }
    Check(shots==3, "Ráfaga de 3 disparos ("+shots+")");
    Check(t>=0.4f+2f/5f-0.02f, "A la cadencia del arma (5/s)");
    BurstEvent last=BurstEvent.None; for(int i=0;i<400 && last!=BurstEvent.Finished;i++){ t+=0.01f; last=b.Tick(t,true,0.5); }
    Check(last==BurstEvent.Finished && b.Current==BurstModel.Phase.Idle, "Tras la pausa (1,2–2,0 s) puede volver a disparar");
    var c = new BurstModel(EnemyGunCatalog.Guard); c.Begin(0f);
    Check(c.Tick(0.2f,false,0.0)==BurstEvent.Cancelled && c.Current==BurstModel.Phase.Pause, "Si pierde la línea de tiro durante el aviso, no dispara");
    var d = new BurstModel(EnemyGunCatalog.Guard); d.Begin(0f); d.Tick(0.4f,true,0); d.Cancel(0.45f,0);
    Check(d.Tick(0.5f,true,0)==BurstEvent.None && d.Current==BurstModel.Phase.Pause, "Cancelar a mitad (tambaleo/PEM) corta la ráfaga");
  }
  static void Stagger(){
    var s = new StaggerModel();
    Check(!s.AddDamage(0f,7f) && s.AddDamage(0.2f,8f) && s.IsStaggered(0.5f), "15 de daño en 0,3 s → tambalea");
    Check(!s.IsStaggered(0.81f), "El tambaleo dura 0,6 s");
    var n = new StaggerModel(); n.AddDamage(0f,7f); Check(!n.AddDamage(0.1f,7f), "14 no basta");
    var w = new StaggerModel(); w.AddDamage(0f,10f); Check(!w.AddDamage(0.5f,10f), "La ventana se desliza: lo de hace más de 0,3 s no cuenta");
  }
  static void Indicator(){
    Check(Near(DamageIndicatorMath.ScreenAngle(0,1, 0,1),0f) && Near(DamageIndicatorMath.ScreenAngle(0,1, 1,0),90f), "Delante 0°, derecha 90°");
    Check(Near(Math.Abs(DamageIndicatorMath.ScreenAngle(0,1, 0,-1)),180f) && Near(DamageIndicatorMath.ScreenAngle(0,1, -1,0),-90f), "Detrás 180°, izquierda −90°");
    Check(Near(DamageIndicatorMath.ScreenAngle(1,0, 0,1),-90f), "Relativo a la mirada (mirando a la derecha, lo de delante queda a la izquierda)");
  }
  static void Main(){
    Catalog(); Aim(); Tokens(); Burst(); Stagger(); Indicator();
    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
