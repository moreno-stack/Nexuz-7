using System; using System.Linq;
using Nexus7.Tutorial; using Nexus7.Core; using Nexus7.Player;
static class T {
  static int fails=0; static void Check(bool c,string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }
  static void Main(){
    var ids = TutorialSteps.Phase1.Select(s=>s.id).ToArray();
    Check(string.Join(",",ids)=="move,run,crouch,jump,camera,interact,suit", "OrderAndIds: "+string.Join(",",ids));
    Func<string,string> H = id => TutorialSteps.Phase1.First(s=>s.id==id).hint;
    Check(H("jump").Contains("[Espacio]") && H("camera").Contains("[V]") && H("interact").Contains("X en el mando") && !H("interact").Contains("A en el mando"), "HintsMentionKeys");
    Check(TutorialManager.ShouldStartSteps(false, 0.25f) && !TutorialManager.ShouldStartSteps(true, 5f) && !TutorialManager.ShouldStartSteps(false, 0.1f), "StartsAfterCinematicEvenIfSkipped");
    GameSession.NewRun(); Check(!TutorialManager.IsStepDone("suit", null, false, false), "Sin traje el paso del traje no está hecho");
    GameSession.HasSuit = true; Check(TutorialManager.IsStepDone("suit", null, false, false), "SuitStepAlreadyDone");
    Check(TutorialManager.IsStepDone("camera", null, false, true) && !TutorialManager.IsStepDone("camera", null, false, false), "Paso de cámara: hecho al pulsar V");
    Check(TutorialManager.IsStepDone("interact", null, true, false) && !TutorialManager.IsStepDone("move", null, true, true), "Interactuar hecho; sin jugador los pasos de movimiento no se cumplen");
    GameSession.NewRun();
    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
