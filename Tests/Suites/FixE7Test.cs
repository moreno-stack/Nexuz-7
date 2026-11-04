using System; using System.Linq; using System.Reflection; using UnityEngine;
using Nexus7.Tutorial; using Nexus7.Player; using Nexus7.Characters; using Nexus7.UI; using Nexus7.Gameplay;
static class T {
  static int fails=0; static void Check(bool c,string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }
  const BindingFlags F = BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance;
  static void SetF(object o,string f,object v){ var t=o.GetType(); while(t!=null){ var fi=t.GetField(f,F|BindingFlags.DeclaredOnly); if(fi!=null){ fi.SetValue(o,v); return;} t=t.BaseType;} throw new Exception(f); }
  static object GetF(object o,string f){ var t=o.GetType(); while(t!=null){ var fi=t.GetField(f,F|BindingFlags.DeclaredOnly); if(fi!=null) return fi.GetValue(o); t=t.BaseType;} throw new Exception(f); }
  static void Call(object o,string m){ var t=o.GetType(); while(t!=null){ var mi=t.GetMethod(m,F|BindingFlags.DeclaredOnly,null,Type.EmptyTypes,null); if(mi!=null){ mi.Invoke(o,null); return;} t=t.BaseType;} throw new Exception(m); }
  static void Main(){
    var cam = TutorialSteps.Phase1.First(s=>s.id=="camera").hint;
    Check(cam.Contains("R3") && !cam.Contains("View"), "Tutorial: la cámara del mando es R3 (View abre el inventario)");
    var sw = new CameraModeSwitcher(); sw.transform=new Transform(); sw.gameObject=new GameObject("P"); SetF(sw,"padToggle",KeyCode.JoystickButton6); Call(sw,"Awake");
    Check((KeyCode)GetF(sw,"padToggle")==KeyCode.JoystickButton9, "Escenas ya construidas: View se migra a R3 al cargar");
    Check(GameHud.ShouldHide(true,false) && GameHud.ShouldHide(false,true) && !GameHud.ShouldHide(false,false), "HUD oculto en cinemáticas y durante el final (no reaparece sobre el fundido)");
    var a0=GaitModel.FirstPersonRightArm(0f); var a1=GaitModel.FirstPersonRightArm(1f);
    Check(a0.Key==-55f && a1.Key>a0.Key && a1.Key<=-10f, "Brazo en 1ª persona agachado: baja la mano para que no flote ("+a1.Key+")");
    Check(new RigMaterials().Get(RigMaterial.Body)!=null && CharacterRigFactory.MaterialFor(null, RigMaterial.Eye)!=null, "Sin materiales: gris por defecto (también con RigMaterials nulo)");
    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
