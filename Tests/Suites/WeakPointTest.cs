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
  static void Arm(){
    Check(WeakPointModel.Classify(false, true, 0.1f, 1.2f, 1.8f, 0,-1, 0,1)==HitZone.Arm, "Cerca de la mano del arma: brazo");
    Check(WeakPointModel.Classify(false, true, 0.4f, 1.2f, 1.8f, 0,-1, 0,1)==HitZone.Body, "A 40 cm del brazo: cuerpo");
    Check(WeakPointModel.Classify(false, false, 0.1f, 1.2f, 1.8f, 0,-1, 0,1)==HitZone.Body, "Ya desarmado (o sin arma): el brazo no cuenta");
    Check(WeakPointModel.Classify(false, true, 0.1f, 1.75f, 1.8f, 0,-1, 0,1)==HitZone.Optic, "La óptica tiene prioridad sobre el brazo");
    Check(WeakPointModel.Classify(true, true, 0.1f, 1.1f, 1.8f, 0,1, 0,1)==HitZone.Arm, "El brazo tiene prioridad sobre el núcleo");
    Check(WeakPointModel.Multiplier(HitZone.Arm)==1f, "El brazo recibe daño normal");
    var a = new WeaponArmModel();
    Check(!a.Apply(12f) && !a.Apply(12f) && a.Health>0f, "24 de daño: aún armado");
    Check(a.Apply(12f) && a.Disarmed && a.Health==0f, "30 de daño: desarmado");
    Check(!a.Apply(50f), "Solo desarma una vez");
  }
  static void Recoil(){
    Check(Nexus7.Characters.HitRecoilModel.Angle(-0.1f,1f)==0f && Nexus7.Characters.HitRecoilModel.Angle(0.25f,1f)==0f, "Sin retroceso fuera de los 0,25 s");
    Check(Math.Abs(Nexus7.Characters.HitRecoilModel.Angle(0.06f,1f)-12f)<0.01f, "Pico de 12° a los 0,06 s");
    Check(Nexus7.Characters.HitRecoilModel.Angle(0.03f,1f)>0f && Nexus7.Characters.HitRecoilModel.Angle(0.03f,1f)<12f, "Sube rápido hasta el pico");
    Check(Nexus7.Characters.HitRecoilModel.Angle(0.1f,1f)>Nexus7.Characters.HitRecoilModel.Angle(0.2f,1f), "Vuelve suavemente");
    Check(Nexus7.Characters.HitRecoilModel.StrengthFor(5f)==0.4f && Nexus7.Characters.HitRecoilModel.StrengthFor(20f)==1f && Nexus7.Characters.HitRecoilModel.StrengthFor(100f)==1.5f, "Fuerza según el daño (0,4–1,5)");
    Check(Nexus7.Characters.HitRecoilModel.Angle(0.06f,1.5f)>Nexus7.Characters.HitRecoilModel.Angle(0.06f,0.4f), "Un impacto fuerte echa más hacia atrás");
  }
  static void Main(){
    Zones(); Multipliers(); Arm(); Recoil();
    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
