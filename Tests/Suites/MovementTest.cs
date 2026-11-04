using System; using UnityEngine;
using Nexus7.Player;
static class T {
  static int fails=0; static void Check(bool c,string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }
  static bool Near(float a,float b,float e=0.01f){ return Math.Abs(a-b)<=e; }

  static void JumpVelocityMatchesHeight(){
    var j = new JumpModel(1.1f, -18f, 0.1f); j.NotifyGrounded(true, 0f, 0f);
    float v; bool ok = j.TryJump(true, false, false, 0f, out v);
    Check(ok && Near(v, (float)Math.Sqrt(2*18*1.1)), "Velocidad de salto para 1,1 m con g=-18 ("+v+")");
  }
  static void NoJumpCrouchedOrLocked(){
    var j = new JumpModel(1.1f, -18f, 0.1f); j.NotifyGrounded(true, 0f, 0f); float v;
    Check(!j.TryJump(true, true, false, 0f, out v), "No se salta agachado");
    Check(!j.TryJump(true, false, true, 0f, out v), "No se salta con bloqueo de entrada");
  }
  static void CoyoteTime(){
    var j = new JumpModel(1.1f, -18f, 0.1f); float v;
    j.NotifyGrounded(true, 0.9f, 0f); j.NotifyGrounded(false, 1.0f, 0f);
    Check(j.TryJump(false, false, false, 1.08f, out v), "Margen tras dejar el borde: salta a los 0,08 s");
    var k = new JumpModel(1.1f, -18f, 0.1f); k.NotifyGrounded(true, 0.9f, 0f); k.NotifyGrounded(false, 1.0f, 0f);
    Check(!k.TryJump(false, false, false, 1.15f, out v), "Fuera del margen (0,15 s) no salta");
    var m = new JumpModel(1.1f, -18f, 0.1f); m.NotifyGrounded(true, 0f, 0f); m.TryJump(true,false,false,0f,out v); m.NotifyGrounded(false, 0.02f, 0.1f);
    Check(!m.TryJump(false, false, false, 0.05f, out v), "No hay doble salto con el margen");
  }
  static void LandingNoise(){
    var j = new JumpModel(1.1f, -18f, 0.1f);
    Check(j.LandingNoiseRadius(0.2f)==0f && j.LandingNoiseRadius(1.5f)==6f && j.LandingNoiseRadius(3f)==9f, "Ruido al aterrizar: 0 / 6 / 9");
    j.NotifyGrounded(true, 0f, 4.2f); j.NotifyGrounded(false, 0.1f, 4.2f); j.NotifyGrounded(false, 0.5f, 5.0f);
    float fall = j.NotifyGrounded(true, 1.0f, 0f);
    Check(Near(fall, 5.0f), "La caída se mide desde el punto más alto ("+fall+")");
  }
  static void BoomCollision(){
    Check(Near(CameraBoomMath.ResolveDistance(3.2f, 1.0f, true, 0.25f), 0.75f), "Brazo de cámara choca a 1 m → 0,75");
    Check(Near(CameraBoomMath.ResolveDistance(3.2f, 0f, false, 0.25f), 3.2f), "Sin choque → 3,2");
    Check(CameraBoomMath.ShouldFallbackToFirstPerson(0.5f) && !CameraBoomMath.ShouldFallbackToFirstPerson(0.75f), "Menos de 0,6 m → 1ª persona temporal");
    Check(CameraBoomMath.ResolveDistance(3.2f, 0.1f, true, 0.25f)==0f, "Nunca negativo");
  }
  static void Main(){
    JumpVelocityMatchesHeight(); NoJumpCrouchedOrLocked(); CoyoteTime(); LandingNoise();
    BoomCollision();
    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
