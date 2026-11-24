using System;
using Nexus7.Combat;
static class T {
  static int fails=0; static void Check(bool c,string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }

  static void Zones(){
    // Robot mirando a +Z, cabeza a 1,8 m
    Check(WeakPointModel.Classify(false, 1.7f, 1.8f, 0,-1, 0,1)==HitZone.Optic, "Cabeza (desde 20 cm por debajo): óptica");
    Check(WeakPointModel.Classify(true, 1.75f, 1.8f, 0,1, 0,1)==HitZone.Optic, "La óptica manda también por detrás");
    Check(WeakPointModel.Classify(false, 1.1f, 1.8f, 0,-1, 0,1)==HitZone.Body, "Torso de frente: cuerpo");
    Check(WeakPointModel.Classify(true, 1.1f, 1.8f, 0,1, 0,1)==HitZone.Core, "Élite por la espalda a la altura del torso: núcleo");
    Check(WeakPointModel.Classify(true, 1.1f, 1.8f, 0,-1, 0,1)==HitZone.Body, "Élite de frente: cuerpo");
    Check(WeakPointModel.Classify(true, 1.1f, 1.8f, 1,1, 0,1)==HitZone.Core && WeakPointModel.Classify(true, 1.1f, 1.8f, 1,0.3f, 0,1)==HitZone.Body, "Por detrás en diagonal (45°) cuenta; casi de lado (73°) no");
    Check(WeakPointModel.Classify(false, 1.1f, 1.8f, 0,1, 0,1)==HitZone.Body, "Sin núcleo (seguridad, guardia): la espalda es cuerpo");
    Check(WeakPointModel.Classify(true, 0.4f, 1.8f, 0,1, 0,1)==HitZone.Body, "Piernas por detrás: cuerpo");
    Check(WeakPointModel.Classify(false, 3f, 0f, 0,1, 0,1)==HitZone.Body, "Sin cabeza localizada no hay óptica");
  }
  static void Multipliers(){
    Check(WeakPointModel.Multiplier(HitZone.Body)==1f && WeakPointModel.Multiplier(HitZone.Optic)==2f && WeakPointModel.Multiplier(HitZone.Core)==3f, "Daño ×1 cuerpo, ×2 óptica, ×3 núcleo");
    Check(WeakPointModel.DazzleNoBurstSeconds<WeakPointModel.DazzleSeconds, "El deslumbramiento bloquea ráfagas menos tiempo del que dura la mala puntería");
  }
  static void Main(){
    Zones(); Multipliers();
    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
