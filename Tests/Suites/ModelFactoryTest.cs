using System; using System.IO;
using Nexus7.Visual.Models;
static class T {
  static int fails=0; static void Check(bool c,string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }
  static bool Eq(MVec a, MVec b){ return Math.Abs(a.x-b.x)<1e-5f && Math.Abs(a.y-b.y)<1e-5f && Math.Abs(a.z-b.z)<1e-5f; }
  static string Src(string f){ return File.ReadAllText("Assets/_Nexus7/Scripts/" + f); }
  static void Main(){
    var s = new MVec(0.1f, 0.4f, 0.2f);
    Check(Eq(ModelScale.ForUnity(ModelShape.Box, s), s), "Caja: la escala es el tamaño (el cubo de Unity mide 1)");
    Check(Eq(ModelScale.ForUnity(ModelShape.Sphere, s), s), "Esfera: la escala es el tamaño (diámetro 1)");
    Check(Eq(ModelScale.ForUnity(ModelShape.Cylinder, s), new MVec(0.1f, 0.2f, 0.2f)), "Cilindro: la altura se divide entre 2 (el de Unity mide 2)");
    Check(Eq(ModelScale.ForUnity(ModelShape.Capsule, s), new MVec(0.1f, 0.2f, 0.2f)), "Cápsula: la altura se divide entre 2 (la de Unity mide 2)");

    string vm = Src("Combat/WeaponViewModel.cs"), pw = Src("Combat/PlayerWeapon.cs"), fac = Src("Visual/Models/ModelFactory.cs");
    Check(vm.Contains("ModelLibrary.Weapon(") && vm.Contains("ModelFactory.Build(") && !vm.Contains("CreatePrimitive"), "El arma en la mano usa el modelo del arma (no cajas sueltas)");
    Check(pw.Contains("view.Show(w?.Spec, w?.Mods)") || pw.Contains("RefreshView()"), "Al cambiar de arma se enseña con sus accesorios");
    int give = pw.IndexOf("public bool GiveMod(");
    Check(give >= 0 && pw.IndexOf("RefreshView()", give) > give, "Al instalar un accesorio se ve en el arma");
    Check(fac.Contains("ModelScale.ForUnity") && fac.Contains("ModelMaterials.For("), "El fabricante usa la escala de Unity y los materiales compartidos");
    Check(!Src("Visual/Models/ModelMaterials.cs").Contains("Destroy("), "Los materiales compartidos no se destruyen");
    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
