using System; using System.Linq;
using Nexus7.Player; using Nexus7.Inventory; using Nexus7.Characters;
static class T {
  static int fails=0; static void Check(bool c,string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }
  static void Main(){
    var r = PlayerRigSetup.Required;
    var need = new[]{ typeof(PlayerController), typeof(PlayerLook), typeof(PlayerHealth), typeof(SuitShield), typeof(PlayerSuit), typeof(InventorySystem), typeof(Hotbar), typeof(HeldItemView), typeof(CameraModeSwitcher), typeof(ProceduralAnimator), typeof(PlayerRigDriver) };
    Check(need.All(t => r.Contains(t)), "RequiredContainsAll: el jugador lleva los 11 componentes");
    Check(r.Distinct().Count()==r.Length, "NoDuplicateComponents: ningún tipo repetido");
    Check(Array.IndexOf(r,typeof(SuitShield)) < Array.IndexOf(r,typeof(PlayerHealth)), "HealthBeforeSuit: SuitShield antes que PlayerHealth");
    Check(Array.IndexOf(r,typeof(InventorySystem)) < Array.IndexOf(r,typeof(Hotbar)) && Array.IndexOf(r,typeof(ProceduralAnimator)) < Array.IndexOf(r,typeof(PlayerRigDriver)) && Array.IndexOf(r,typeof(PlayerController)) < Array.IndexOf(r,typeof(PlayerHealth)), "Dependencias antes que quien las requiere");
    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
