using System; using Nexus7.Enemies; using Nexus7.Characters;
static class T {
  static int fails=0; static void Check(bool c,string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }
  static bool Is(WeaponSpec w, string n, float d, float wu, float r, float rec, float st){ return w.name==n && w.damage==d && Math.Abs(w.windup-wu)<1e-4 && Math.Abs(w.range-r)<1e-4 && Math.Abs(w.recovery-rec)<1e-4 && Math.Abs(w.stun-st)<1e-4 && w.arc==70f; }
  static void Main(){
    Check(Is(WeaponCatalog.Get(WeaponShape.Bat),"Bate metálico",25,0.55f,1.9f,1.2f,0) && WeaponCatalog.Get(WeaponShape.Bat).shape==WeaponShape.Bat, "Bate: 25 / 0,55 / 1,9 / 1,2");
    Check(Is(WeaponCatalog.Get(WeaponShape.Wrench),"Llave inglesa gigante",25,0.6f,2.0f,1.3f,0), "Llave: 25 / 0,6 / 2,0 / 1,3");
    Check(Is(WeaponCatalog.Get(WeaponShape.Baton),"Porra eléctrica",15,0.45f,1.8f,1.0f,1.0f), "Porra: 15 / 0,45 / 1,8 / 1,0 + aturde 1,0");
    Check(Is(WeaponCatalog.Get(WeaponShape.Axe),"Hacha",40,0.7f,2.2f,1.5f,0), "Hacha: 40 / 0,7 / 2,2 / 1,5");
    Check(WeaponCatalog.ForRobot(RobotStyle.Security)==WeaponShape.Bat && WeaponCatalog.ForRobot(RobotStyle.Guard)==WeaponShape.Wrench && WeaponCatalog.ForRobot(RobotStyle.Research)==WeaponShape.Baton && WeaponCatalog.ForRobot(RobotStyle.Elite)==WeaponShape.Axe, "Arma de cada tipo de robot");
    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
