using System; using System.IO;
using Nexus7.Core;
static class T {
  static int fails=0; static void Check(bool c,string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }
  static string Src(string f){ return File.ReadAllText("Assets/_Nexus7/Scripts/" + f); }
  static void Main(){
    string ui = Src("UI/TouchControlsUI.cs");
    Check(ui.Contains("RuntimeInitializeOnLoadMethod") && ui.Contains("Application.isMobilePlatform") && ui.Contains("DontDestroyOnLoad"), "Se crea sola en el móvil y sobrevive a los cambios de escena");
    Check(ui.Contains("Input.simulateMouseWithTouches = false"), "Los toques no se convierten en clics de ratón");
    Check(ui.Contains("[DefaultExecutionOrder(-1000)]"), "Lee los toques antes que el resto de scripts (los 'Down' llegan en el mismo fotograma)");
    Check(ui.Contains("OnApplicationPause") && ui.Contains("OnApplicationFocus") && ui.Contains("TouchInput.ReleaseAll()"), "Al perder el foco se suelta todo (nada se queda pegado)");
    Check(ui.Contains("TouchLayout.Pick(") && ui.Contains("CinematicDirector.IsPlaying") && ui.Contains("HackMinigameUI.Instance") && ui.Contains("EndingUI.Instance") && ui.Contains("ConsoleUI.Instance") && ui.Contains("CraftingUI.Instance"), "Elige los botones según lo que esté abierto");
    Check(ui.Contains("Screen.safeArea"), "Respeta la zona segura (muesca de la cámara)");
    Check(ui.Contains("sortingOrder = 900"), "Por encima del HUD (10) y por debajo de los fundidos (1000)");
    Check(Src("UI/HackMinigameUI.cs").Contains("public bool IsOpen") && Src("UI/EndingUI.cs").Contains("public bool IsOpen"), "El hackeo y la pantalla final dicen si están abiertos");
    Check(Src("UI/CombatHUD.cs").Contains("TouchInput.Enabled ?"), "En el móvil el panel del arma sube para no quedar debajo de Disparar");
    Check(GameSettings.DefaultQualityFor(true) == GraphicsQuality.Media && GameSettings.DefaultQualityFor(false) == GameSettings.DefaultQuality, "Calidad por defecto: Media en el móvil, la de siempre en PC");
    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
