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
    // Tarea 5: nada que se recoja o se lleve en la mano sigue siendo una primitiva suelta
    foreach (var f in new[]{ "Inventory/RoomLootSpawner.cs", "Player/HeldItemView.cs", "Combat/AmmoPickup.cs", "Combat/ModPickup.cs", "Devices/DeviceFactory.cs" }){
      string src = Src(f);
      bool primitives = src.Contains("CreatePrimitive(shape)") || src.Contains("CreatePrimitive(PrimitiveType.Cube)") || src.Contains("CreatePrimitive(PrimitiveType.Sphere)");   // el haz de luz (cilindro) sí es una primitiva
      Check(!primitives && src.Contains("ModelFactory.Build("), f + ": usa su modelo (sin primitivas sueltas)");
    }
    string loot = Src("Inventory/RoomLootSpawner.cs");
    Check(loot.Contains("ItemModels.For(") && loot.Contains("Scaled(KeyItemScale)") && loot.Contains("AddBeacon(") && loot.Contains("MoveToFreeFloor("), "Botín: modelo del objeto; los imprescindibles más grandes, con haz y en suelo libre");
    Check(Src("Player/HeldItemView.cs").Contains("ItemModels.For(") && !Src("Player/HeldItemView.cs").Contains("Destroy(ownMaterial)"), "En la mano: modelo del objeto y sin destruir materiales compartidos");
    string dev = Src("Devices/DeviceFactory.cs");
    Check(dev.Contains("AddComponent<SphereCollider>()") && dev.Contains("ThrownRadius") && dev.Contains("Physics.IgnoreCollision("), "Dispositivo lanzado: esfera de choque en la raíz que ignora al jugador");
    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
