using System; using System.Reflection;
using Nexus7.Core; using Nexus7.UI;
static class T {
  static int fails=0; static void Check(bool c,string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }
  static void Main(){
    // I1: el fotograma en que se abre o cierra un panel, el menú de fondo no lee [Intro]/A
    Check(!MenuState.AcceptsMainInput(false,false,120,120), "Mismo fotograma en que se cerró un panel: [Intro] no lanza Nueva partida");
    Check(MenuState.AcceptsMainInput(false,false,121,120), "Al fotograma siguiente vuelve a responder");
    Check(!MenuState.AcceptsMainInput(true,false,200,120), "Con un panel abierto sigue sin responder");
    // M2→I: fecha fuera de rango → sin partida, sin excepción
    var bad=new SaveData{ version=SaveSlot.Version, scene="Laboratorio", savedAtUtcTicks=-5 };
    var huge=new SaveData{ version=SaveSlot.Version, scene="Laboratorio", savedAtUtcTicks=long.MaxValue };
    Check(!SaveSlot.IsCompatible(bad) && !SaveSlot.IsCompatible(huge), "Fecha guardada imposible → partida no compatible");
    bool threw=false; try { SaveSlot.Describe(bad, DateTime.UtcNow, true); } catch(Exception){ threw=true; }
    Check(!threw, "Describe no lanza con una fecha imposible");
    Check(SaveSlot.Parse("{\"version\":1,\"scene\":\"Laboratorio\",\"savedAtUtcTicks\":-5}")==null, "Parse rechaza la fecha imposible");
    // M5→I: volver al menú durante una carga no borra la partida que se está cargando
    RunStats.Reset(); RunStats.RecordDeath();
    var p=typeof(SceneLoader).GetProperty("IsLoading", BindingFlags.Public|BindingFlags.Static);
    p.SetValue(null, true);
    GameFlow.ReturnToMenu();
    Check(RunStats.Deaths==1, "ReturnToMenu durante una carga no reinicia el estado de la partida");
    p.SetValue(null, false);
    // M11→I: mover un volumen solo aplica el audio (no toca la pantalla)
    GameSettings.ResetToDefaults();
    UnityEngine.Screen.fullScreenMode=UnityEngine.FullScreenMode.ExclusiveFullScreen;
    GameSettings.Master=0.5f; GameSettings.ApplyAudio();
    Check(UnityEngine.Screen.fullScreenMode==UnityEngine.FullScreenMode.ExclusiveFullScreen, "ApplyAudio no cambia el modo de pantalla");
    Check(Math.Abs(UnityEngine.AudioListener.volume-0.25f)<1e-4f, "ApplyAudio aplica el volumen general (0,5 → 0,25)");
    GameSettings.ResetToDefaults();
    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
