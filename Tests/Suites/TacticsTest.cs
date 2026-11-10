using System;
using UnityEngine; using Nexus7.Core; using Nexus7.Player; using Nexus7.Security; using Nexus7.Gameplay; using Nexus7.Inventory;
static class T {
  static int fails=0; static void Check(bool c,string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }
  static bool Near(float a,float b,float e=1e-3f){ return Math.Abs(a-b)<=e; }
  static Vector3 V(float x,float y,float z){ return new Vector3(x,y,z); }
  static void Main(){
    // ---------- 1. Propagación del sonido ----------
    Check(Near(SoundPropagation.EffectiveRadius(12f,0),12f) && Near(SoundPropagation.EffectiveRadius(12f,1),6f) && Near(SoundPropagation.EffectiveRadius(12f,2),3f), "Cada pared o caja en medio reduce el radio a la mitad (12 → 6 → 3 m)");
    Check(Near(SoundPropagation.EffectiveRadius(12f,5),3f), "Se cuentan como mucho 2 obstáculos (no desaparece del todo)");
    Check(SoundPropagation.CanHear(10f,12f,0,1f) && !SoundPropagation.CanHear(10f,12f,1,1f), "Correr a 10 m: se oye en abierto, no detrás de un muro");
    Check(SoundPropagation.CanHear(10f,12f,1,1.8f), "Un robot de oído fino (×1,8) sí lo oye a través del muro");
    Check(!SoundPropagation.CanHear(1.2f,0f,0,1f) && !SoundPropagation.CanHear(0f,0f,0,1f), "Sonido de radio 0 (quieto): nunca se oye");
    var est=SoundPropagation.EstimatePosition(V(10,0,0),V(0,0,0),1f,0f);
    Check(Near(est.x,12.5f) && Near(est.z,0f) && Near(est.y,0f), "Posición estimada: error máximo del 25 % de la distancia (10 m → 2,5 m)");
    est=SoundPropagation.EstimatePosition(V(10,0,0),V(0,0,0),0.5f,0.25f);
    Check(Near(est.x,10f) && Near(est.z,1.25f), "El error va en cualquier dirección horizontal");
    est=SoundPropagation.EstimatePosition(V(10,0,0),V(9,0,0),1f,0f);
    Check(Near(est.x,10.25f), "Muy cerca casi acierta (1 m → 25 cm)");
    var hd=new float[]{3.2f,1.0f,1.3f,0f}; Check(SoundPropagation.CountObstacles(hd,3,0.5f)==2, "Impactos pegados (1,0 y 1,3 m) son un solo obstáculo; el de 3,2 m, otro");
    Check(SoundPropagation.CountObstacles(new float[]{0.5f,0.6f,0.7f},3,0.5f)==1 && SoundPropagation.CountObstacles(new float[4],0,0.5f)==0, "Una pared hecha de varias cajas cuenta una vez; sin impactos, 0");
    Check(SoundPropagation.CountObstacles(new float[]{1f,3f,5f,7f},4,0.5f)==2, "Como mucho 2 obstáculos");
    Check(Near(PlayerNoise.StepRadius(false,false,true),0f) && Near(PlayerNoise.StepRadius(true,false,false),1f)
          && Near(PlayerNoise.StepRadius(false,false,false),6f) && Near(PlayerNoise.StepRadius(false,true,false),12f), "Pasos: quieto 0 · agachado 1 m · caminar 6 m · correr 12 m");

    // ---------- 3. Estamina ----------
    var st=new StaminaModel(1f);
    Check(Near(st.Current,100f) && st.CanRun && Near(st.Normalized,1f), "Empieza llena y se puede correr");
    float t=0f; while(st.CanRun && t<20f){ st.Tick(0.05f,true,true); t+=0.05f; }
    Check(t>5.5f && t<6.3f, "Correr agota la estamina en unos 6 s ("+t.ToString("0.00")+" s)");
    Check(st.Exhausted && !st.CanRun && Near(st.Current,0f), "Agotado: no se puede correr");
    bool justEx=new StaminaModel(1f).Tick(10f,true,true);
    Check(justEx, "Tick avisa justo al agotarse (para el jadeo)");
    st.Tick(0.9f,false,true); Check(Near(st.Current,0f), "Durante el primer segundo sin correr no se recupera");
    st.Tick(0.1f,false,true); st.Tick(1f,false,true); Check(Near(st.Current,14f,0.1f), "Después recupera 14/s caminando");
    st.Tick(1f,false,false); Check(Near(st.Current,36f,0.1f) && st.CanRun && !st.Exhausted, "Quieto recupera 22/s; con el 35 % vuelve a poder correr");
    var st2=new StaminaModel(1f); st2.Tick(10f,true,true); st2.Tick(1f,false,false); st2.Tick(1f,false,false);
    Check(!st2.CanRun && st2.Exhausted, "Con el 22 % sigue agotado (evita correr a tirones)");
    var st3=new StaminaModel(1f); st3.Jump(); Check(Near(st3.Current,92f), "Un salto cuesta 8");
    var st4=new StaminaModel(1f); st4.Tick(2f,true,true); st4.Tick(0.5f,true,true); st4.Tick(0.6f,false,true);
    Check(Near(st4.Current,100f-17f*2.5f), "Correr reinicia la espera de 1 s antes de recuperar");
    var easy=new StaminaModel(StaminaModel.DrainMultiplierFor(0)); var hard=new StaminaModel(StaminaModel.DrainMultiplierFor(2));
    easy.Tick(2f,true,true); hard.Tick(2f,true,true);
    Check(Near(easy.Current,100f-17f*0.75f*2f) && Near(hard.Current,100f-17f*1.25f*2f), "Dificultad: fácil gasta ×0,75 y difícil ×1,25");
    st.Refill(); Check(Near(st.Current,100f) && !st.Exhausted && st.CanRun, "Al reaparecer se rellena");
    var st5=new StaminaModel(1f); st5.Tick(1f,true,false); Check(Near(st5.Current,100f), "Pulsar correr sin moverse no gasta");

    // ---------- 2. Interferencias en el hackeo ----------
    float mn,mx,win; int max;
    HackInterference.GetParams(AlertLevel.Normal,HackDifficulty.Easy,out mn,out mx,out win,out max); Check(max==0, "Normal y fácil: sin interferencias");
    HackInterference.GetParams(AlertLevel.Normal,HackDifficulty.Medium,out mn,out mx,out win,out max); Check(max==1 && Near(win,1.4f), "Normal, media: 1 interferencia con 1,4 s para reaccionar");
    HackInterference.GetParams(AlertLevel.Yellow,HackDifficulty.Easy,out mn,out mx,out win,out max); Check(max>5 && Near(mn,5f) && Near(mx,7f) && Near(win,1.1f), "Amarilla: cada 5–7 s con 1,1 s");
    HackInterference.GetParams(AlertLevel.Red,HackDifficulty.Hard,out mn,out mx,out win,out max); Check(max>5 && Near(mn,3f) && Near(mx,4.5f) && Near(win,0.8f), "Roja: cada 3–4,5 s con solo 0,8 s");
    var hi=new HackInterference(AlertLevel.Red,HackDifficulty.Hard,7);
    int started=0, firstAt=-1; for(int i=0;i<200 && started==0;i++){ if(hi.Tick(0.01f)==HackInterference.Event.Started){ started++; firstAt=i; } }
    Check(started==1 && hi.Active && firstAt>=150, "La primera aparece tras al menos 1,5 s de hackeo");
    Check(hi.Respond() && !hi.Active && hi.Cleared==1, "Pulsar [Espacio] a tiempo la elimina");
    Check(!hi.Respond(), "Pulsar [Espacio] sin interferencia no hace nada");
    var ev=HackInterference.Event.None; float waited=0f; while(ev!=HackInterference.Event.Started && waited<10f){ ev=hi.Tick(0.05f); waited+=0.05f; }
    Check(ev==HackInterference.Event.Started && waited>=2.95f && waited<=4.6f, "En Roja la siguiente llega a los 3–4,5 s ("+waited.ToString("0.00")+" s)");
    var miss=HackInterference.Event.None; for(int i=0;i<20 && miss!=HackInterference.Event.Missed;i++) miss=hi.Tick(0.05f);
    Check(miss==HackInterference.Event.Missed && !hi.Active && hi.Missed==1, "Si no reaccionas en 0,8 s cuenta como fallo");
    var one=new HackInterference(AlertLevel.Normal,HackDifficulty.Medium,3); int n1=0;
    for(int i=0;i<3000;i++){ if(one.Tick(0.01f)==HackInterference.Event.Started){ n1++; one.Respond(); } }
    Check(n1==1, "Normal, media: solo una en todo el hackeo");
    var none=new HackInterference(AlertLevel.Normal,HackDifficulty.Easy,3); int n0=0;
    for(int i=0;i<3000;i++) if(none.Tick(0.01f)==HackInterference.Event.Started) n0++;
    Check(n0==0 && !none.Active, "Normal, fácil: ninguna");
    var seq=new HackSequence(HackDifficulty.Easy,1); float before=seq.TimeLeft; var r=seq.Penalize();
    Check(r==HackSequence.StepResult.Wrong && seq.Errors==1 && Near(seq.TimeLeft,before-2f) && seq.Index==0, "Fallar una interferencia = un error (−tiempo) sin avanzar ni retroceder el código");
    var seq2=new HackSequence(HackDifficulty.Hard,1); seq2.Penalize(); seq2.Penalize(); var r2=seq2.Penalize();
    Check(r2==HackSequence.StepResult.Failed && seq2.Failed, "Demasiados errores por interferencias → hackeo fallido");

    // ---------- 4. Checkpoints inteligentes ----------
    Check(RespawnPenalty.LostAmount(8,ItemCategory.Part,false)==2 && RespawnPenalty.LostAmount(5,ItemCategory.Battery,false)==1, "Recursos menores: se pierde el 25 % redondeado hacia abajo (8 → 2, 5 → 1)");
    Check(RespawnPenalty.LostAmount(3,ItemCategory.Component,false)==0 && RespawnPenalty.LostAmount(1,ItemCategory.Part,false)==0, "Montones pequeños (1–3) no pierden nada");
    Check(RespawnPenalty.LostAmount(6,ItemCategory.Device,false)==0 && RespawnPenalty.LostAmount(6,ItemCategory.Tool,false)==0 && RespawnPenalty.LostAmount(9,ItemCategory.Part,true)==0, "Dispositivos, herramientas y objetos clave se conservan siempre");
    Check(Near(RespawnPenalty.CountdownPenalty(200f),15f) && Near(RespawnPenalty.CountdownPenalty(70f),10f) && Near(RespawnPenalty.CountdownPenalty(50f),0f), "Fase 6: −15 s de cuenta atrás, sin bajar nunca de 60 s");
    Check(!RespawnPenalty.CanActivateCheckpoint(true) && RespawnPenalty.CanActivateCheckpoint(false), "El checkpoint no se guarda mientras te persiguen");

    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
