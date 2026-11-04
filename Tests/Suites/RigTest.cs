using System; using System.Collections.Generic; using System.Linq;
using Nexus7.Characters;
static class T {
  static int fails=0; static void Check(bool c,string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }
  static bool Near(float a,float b,float e){ return Math.Abs(a-b)<=e; }

  static float WorldY(List<RigPart> parts, string name){
    var p = parts.First(x=>x.name==name); float y = p.y;
    while(!string.IsNullOrEmpty(p.parent)){ p = parts.First(x=>x.name==p.parent); y += p.y; }
    return y;
  }
  static void CheckSpec(List<RigPart> parts, string label){
    foreach (var j in RigJoints.All) Check(parts.Any(p=>p.name==j), label+": articulación "+j);
    Check(parts.Select(p=>p.name).Distinct().Count()==parts.Count, label+": nombres únicos");
    Check(parts.All(p=>string.IsNullOrEmpty(p.parent) || parts.Any(q=>q.name==p.parent)), label+": todos los padres existen");
    Check(parts.All(p=>p.shape!=RigShape.Box), label+": sin cajas (redondeado)");
  }
  static void PlayerRigHasAllJoints(){
    var a = CharacterRigSpec.Player(false); var b = CharacterRigSpec.Player(true);
    CheckSpec(a, "Jugador"); CheckSpec(b, "Jugador con traje");
    Check(Near(WorldY(a,RigJoints.Hips),0.95f,0.01f) && Near(WorldY(a,RigJoints.Head),1.62f,0.01f), "Proporciones: caderas 0,95 y cabeza 1,62");
    Check(b.Any(p=>p.name=="Casco") && b.Any(p=>p.name=="Mochila") && !a.Any(p=>p.name=="Casco"), "El traje añade casco y mochila");
    Check(a.Any(p=>p.firstPerson) && a.Where(p=>p.firstPerson).All(p=>p.name.Contains("Antebrazo")||p.name.Contains("Mano")), "Solo antebrazos y manos visibles en 1ª persona");
  }
  static void RobotRigs(){
    foreach (RobotStyle s in Enum.GetValues(typeof(RobotStyle))){
      var r = CharacterRigSpec.Robot(s); CheckSpec(r, "Robot "+s);
      Check(r.Any(p=>p.name=="Eye" && p.material==RigMaterial.Eye), "Robot "+s+": ojo emisivo 'Eye'");
    }
    Check(CharacterRigSpec.Robot(RobotStyle.Elite).Any(p=>p.name.StartsWith("Hombrera")), "Élite con hombreras");
    Check(CharacterRigSpec.RobotScale(RobotStyle.Guard)>CharacterRigSpec.RobotScale(RobotStyle.Security), "El guardia es más grande");
    foreach (WeaponShape w in Enum.GetValues(typeof(WeaponShape))){
      var parts = CharacterRigSpec.Weapon(w); Check(parts.Count>=2 && parts.All(p=>p.shape!=RigShape.Box), "Arma "+w+" redondeada ("+parts.Count+" piezas)");
    }
  }
  static void GaitAmplitude(){
    var g = new GaitModel();
    Check(g.LegSwingDegrees(0f)==0f, "Quieto: 0°");
    Check(Near(g.LegSwingDegrees(2f),25f,0.5f), "Caminando a 2 m/s: ~25°");
    Check(g.LegSwingDegrees(6f)==45f, "Corriendo: tope de 45°");
    Check(Near(g.ArmSwingDegrees(2f),17.5f,0.5f), "Brazos: 0,7 de las piernas");
    float p0 = g.Phase; g.Advance(2f, 0.5f); Check(g.Phase>p0, "La fase avanza con la velocidad");
  }
  static void AttackPoseTimeline(){
    Check(Near(AttackPose.RightArmAngle(0f,0.55f,0.15f),0f,0.01f), "Inicio del ataque: brazo en reposo");
    Check(Near(AttackPose.RightArmAngle(0.55f,0.55f,0.15f),-120f,1f), "Fin de la anticipación: -120°");
    Check(Near(AttackPose.RightArmAngle(0.70f,0.55f,0.15f),40f,1f), "Fin del golpe: +40°");
    Check(Near(AttackPose.RightArmAngle(5f,0.55f,0.15f),0f,0.01f), "Después vuelve al reposo");
  }
  static void Main(){
    PlayerRigHasAllJoints(); RobotRigs(); GaitAmplitude(); AttackPoseTimeline();
    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
