using System;
using Nexus7.Combat;
static class T {
  static int fails=0; static void Check(bool c,string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }

  static void Main(){
    Check(!EncounterSealModel.ShouldSeal(0,false) && EncounterSealModel.ShouldSeal(1,false) && EncounterSealModel.ShouldSeal(2,false), "Sella en Normal y Difícil, no en Fácil");
    Check(!EncounterSealModel.ShouldSeal(2,true), "Con la última vida no sella (director clemente)");
    Check(EncounterSealModel.IsSealed(EncounterState.Countdown,true,10f,12f) && EncounterSealModel.IsSealed(EncounterState.Active,true,10f,100f), "Sellada en la cuenta atrás y durante las oleadas");
    Check(!EncounterSealModel.IsSealed(EncounterState.Idle,true,10f,12f) && !EncounterSealModel.IsSealed(EncounterState.Cleared,true,10f,12f), "Abierta antes de empezar y al despejar");
    Check(!EncounterSealModel.IsSealed(EncounterState.Active,false,10f,12f), "Si este intento no sella, abierta");
    Check(EncounterSealModel.IsSealed(EncounterState.Active,true,0f,149f) && !EncounterSealModel.IsSealed(EncounterState.Active,true,0f,150f), "Como mucho 150 s por intento");
    Check(Math.Abs(EncounterSealModel.Remaining(0f,100f)-50f)<0.01f && EncounterSealModel.Remaining(0f,200f)==0f, "Segundos que quedan de sello");
    string m=EncounterSealModel.Message(2,3,42f);
    Check(m.StartsWith("SALIDA SELLADA") && m.Contains("oleada 2/3") && m.Contains("42 s"), "El mensaje dice la oleada y cuándo se abre");
    Check(!EncounterSealModel.Message(0,3,10f).Contains("oleada"), "En la cuenta atrás no inventa oleada");
    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
