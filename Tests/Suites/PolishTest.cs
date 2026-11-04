using System; using System.Collections.Generic; using System.Linq; using System.Reflection; using UnityEngine; using UnityEngine.AI; using UnityEngine.Rendering;
using Nexus7.Core; using Nexus7.Enemies; using Nexus7.Characters; using Nexus7.Player; using Nexus7.UI; using Nexus7.Inventory; using Nexus7.Devices; using Nexus7.Cinematics; using Nexus7.Gameplay;
static class T {
  static int fails=0; static void Check(bool c,string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }
  const BindingFlags F = BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance;
  static void SetF(object o,string f,object v){ var t=o.GetType(); while(t!=null){ var fi=t.GetField(f,F|BindingFlags.DeclaredOnly); if(fi!=null){ fi.SetValue(o,v); return;} t=t.BaseType;} throw new Exception(f); }
  static object GetF(object o,string f){ var t=o.GetType(); while(t!=null){ var fi=t.GetField(f,F|BindingFlags.DeclaredOnly); if(fi!=null) return fi.GetValue(o); t=t.BaseType;} throw new Exception(f); }
  static void Call(object o,string m){ var t=o.GetType(); while(t!=null){ var mi=t.GetMethod(m,F|BindingFlags.DeclaredOnly,null,Type.EmptyTypes,null); if(mi!=null){ mi.Invoke(o,null); return;} t=t.BaseType;} throw new Exception(m); }
  static ItemData Item(string n){ var i=new ItemData(); i.name=n; SetF(i,"displayName",n); SetF(i,"maxStack",9); return i; }

  static EnemyPatrol Robot(){
    var p=new EnemyPatrol(); p.transform=new Transform(); p.gameObject=new GameObject("R");
    SetF(p,"vision",new EnemyVision()); var ag=new NavMeshAgent(); ag.enabled=false; SetF(p,"agent",ag);
    var an=new ProceduralAnimator(); SetF(p,"rig",an); return p;
  }
  static void Robots(){
    var p=Robot(); var an=(ProceduralAnimator)GetF(p,"rig");
    p.Stun(2f); Check(an.Stunned, "Robot aturdido: postura de aturdido");
    p.ResetToStart(); Check(!an.Stunned, "Al reiniciar vuelve a la postura normal");
    AlertSystem.ResetAll(); AlertSystem.ForceLevel(AlertLevel.Red);
    var q=Robot(); Call(q,"ApplyChaseSpeed");
    float exp=(float)GetF(q,"chaseSpeed")*AlertSystem.Profile.speed;
    Check(Math.Abs(((NavMeshAgent)GetF(q,"agent")).speed-exp)<1e-4, "La velocidad de persecución sigue a la alerta (Roja)");
    AlertSystem.ResetAll();
  }
  static float Cycles(float v){ var g=new GaitModel(); g.Advance(v,1f); return g.Phase/(2f*(float)Math.PI); }
  static void PlayerFeel(){
    float w=Cycles(3.2f), r=Cycles(6f);
    Check(w>=0.9f&&w<=1.4f && r>=1.5f&&r<=2.2f, "Ritmo de pasos natural (andar "+w.ToString("0.00")+" ciclos/s, correr "+r.ToString("0.00")+")");
    var j=new JumpModel(1.1f,-20f,0.1f);
    Check(j.LandingNoiseRadius(0.45f)==0f && j.LandingNoiseRadius(1f)==6f && j.LandingNoiseRadius(2.5f)==9f, "Bajar un escalón no hace ruido; una caída sí");
    Check(JumpModel.AfterCeilingHit(5f,true)==0f && JumpModel.AfterCeilingHit(5f,false)==5f && JumpModel.AfterCeilingHit(-3f,true)==-3f, "El salto rebota en el techo");
    Check(CameraModeSwitcher.ShadowModeFor(true,false,ShadowCastingMode.Off)==ShadowCastingMode.Off && CameraModeSwitcher.ShadowModeFor(false,true,ShadowCastingMode.On)==ShadowCastingMode.On
          && CameraModeSwitcher.ShadowModeFor(false,false,ShadowCastingMode.On)==ShadowCastingMode.ShadowsOnly && CameraModeSwitcher.ShadowModeFor(true,false,ShadowCastingMode.On)==ShadowCastingMode.On, "Visibilidad del cuerpo: brillos sin sombra, piezas ocultas solo sombra");
    Check((KeyCode)GetF(new CameraModeSwitcher(),"padToggle")==KeyCode.JoystickButton9, "Cámara en el mando: R3 (View es el inventario)");
    var a=GaitModel.FirstPersonRightArm(); Check(a.Key==-55f && a.Value==-35f, "Brazo derecho adelantado en 1ª persona");
  }
  static void UiControls(){
    Check(MainMenuUI.NextDifficulty(Difficulty.Facil,-1)==Difficulty.Facil && MainMenuUI.NextDifficulty(Difficulty.Facil,1)==Difficulty.Normal && MainMenuUI.NextDifficulty(Difficulty.Dificil,1)==Difficulty.Dificil, "Dificultad con LB/RB sin salirse de los límites");
    var m=new HotbarModel(); var a=Item("a"); var b=Item("b"); m.Rebuild(new List<ItemData>{a,b});
    Check(m.SelectItemById("b") && m.SelectedItem==b, "Barra rápida: restaura la selección guardada");
    Check(!m.SelectItemById("zzz") && m.SelectedItem==b, "Selección guardada que ya no existe: no cambia nada");
    GameSession.HotbarSelectedId="b"; GameSession.NewRun(); Check(string.IsNullOrEmpty(GameSession.HotbarSelectedId), "Nueva partida: selección borrada");
    var d=CinematicScripts.DerrotaMuerte();
    Check(d.Count==1 && Math.Abs(d[0].duration-7f)<0.01f && d[0].lines[0].text=="Constantes vitales perdidas..." && d[0].lines[1].text=="Lo siento... no lo he conseguido." && d[0].lines.All(l=>l.at+l.hold<=7f), "Derrota por muerte: su propio texto");
    Check(WorkbenchIndicatorUI.Text=="BANCO DE TRABAJO cerca", "Indicador del banco sin prometer [E] a 4 m");
  }
  static void Hygiene(){
    var kit=new DeviceItemData(); SetF(kit,"deviceType",DeviceKind.RepairKit); SetF(kit,"useMode",DeviceUseMode.SelfActivate); SetF(kit,"displayName","Kit"); SetF(kit,"maxStack",5); kit.name="Kit";
    var inv=new InventorySystem(); SetF(inv,"capacity",6); inv.Add(kit,1);
    var h=new PlayerHealth(); h.transform=new Transform(); h.transform.position=new Vector3(0,0,0); SetF(h,"maxHealth",100f); Call(h,"Awake"); Time.time=500f; DifficultySettings.Current=Difficulty.Facil; h.TakeDamage(50f);
    var dq=new DeviceQuickbar(); SetF(dq,"inventory",inv); SetF(dq,"health",h);
    typeof(DeviceQuickbar).GetMethod("Activate",F).Invoke(dq,new object[]{kit});
    Check(Math.Abs(h.Current-90f)<1e-3, "El kit cura +40 aunque el asset tenga otro valor de efecto ("+h.Current+")");
    Check(RoomLootSpawner.PickupNoiseFor(ItemCategory.Part)==3f && RoomLootSpawner.PickupNoiseFor(ItemCategory.Battery)==0f, "Recoger chatarra hace ruido (3 m)");
    Check(new RigMaterials().Get(RigMaterial.Body)!=null, "Modelo sin materiales: gris por defecto, no rosa");
  }
  static void Main(){ Robots(); PlayerFeel(); UiControls(); Hygiene(); Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails); }
}
