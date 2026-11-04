using System; using UnityEngine; using Nexus7.Enemies; using Nexus7.Characters; using Nexus7.Player;
static class T {
  static int fails=0; static void Check(bool c,string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }
  static RaycastHit Hit(Transform t){ var c=new Collider(); c.transform=t; return new RaycastHit{ collider=c, transform=t }; }
  static void Main(){
    var robot=new Transform(); var robotPart=new Transform(); robotPart.parent=robot;
    var player=new Transform(); var playerPart=new Transform(); playerPart.parent=player;
    var wall=new Transform();
    Check(EnemyMeleeAttack.LineBlocked(new[]{ Hit(wall) }, robot, player), "Una pared entre robot y jugador bloquea el golpe");
    Check(!EnemyMeleeAttack.LineBlocked(new[]{ Hit(robotPart), Hit(playerPart), Hit(player) }, robot, player), "El propio robot y el jugador no bloquean");
    Check(!EnemyMeleeAttack.LineBlocked(new RaycastHit[0], robot, player), "Línea despejada → golpe posible");
    var d = CharacterRigFactory.WeaponForward(-15f);
    Check(d.y < -0.2f && d.z > 0.3f, "El bate apunta hacia delante y abajo con el brazo en reposo (y="+d.y.ToString("0.00")+", z="+d.z.ToString("0.00")+")");
    var j = new JumpModel(1.1f, -20f, 0.1f);
    j.SuppressLanding(0f, 3f); j.NotifyGrounded(true, 0.01f, 1.5f); j.NotifyGrounded(false, 0.05f, 1.5f); float fall = j.NotifyGrounded(true, 1f, 0f);
    Check(fall==0f && j.LandingNoiseRadius(fall)==0f, "Reaparecer en el aire no hace ruido al aterrizar");
    var k = new JumpModel(1.1f, -20f, 0.1f); k.NotifyGrounded(false,0f,1.5f); Check(k.NotifyGrounded(true,1f,0f)>1.4f, "Una caída normal sí cuenta");
    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
