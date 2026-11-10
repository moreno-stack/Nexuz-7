using System; using System.Reflection;
using UnityEngine; using UnityEngine.AI; using Nexus7.Enemies;
static class T {
  const BindingFlags F = BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance;
  static FieldInfo Fi(object o,string f){ var t=o.GetType(); while(t!=null){ var fi=t.GetField(f,F|BindingFlags.DeclaredOnly); if(fi!=null) return fi; t=t.BaseType;} throw new Exception(f); }
  static void Set(object o,string f,object v)=>Fi(o,f).SetValue(o,v);
  static object Get(object o,string f)=>Fi(o,f).GetValue(o);
  static void Prop(object o,string p,object v)=>o.GetType().GetProperty(p,F).SetValue(o,v);
  static void Call(object o,string m,params object[] a){ var t=o.GetType(); while(t!=null){ var mi=t.GetMethod(m,F|BindingFlags.DeclaredOnly); if(mi!=null){ mi.Invoke(o,a); return;} t=t.BaseType;} throw new Exception(m); }
  static int fails=0; static void Check(bool c,string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }
  static bool Near(float a,float b,float e=1e-3f){ return Math.Abs(a-b)<=e; }
  static Vector3 V(float x,float y,float z){ return new Vector3(x,y,z); }
  static void Main(){
    // ---------- Sigilo: multiplicador del alcance ----------
    Check(PerceptionModel.Exposure(true,true,false,false,false)==StealthExposure.Concealed, "Agachado en sombra → oculto");
    Check(PerceptionModel.Exposure(false,false,true,true,true)==StealthExposure.Concealed, "Con camuflaje → oculto aunque corra bajo la luz");
    Check(PerceptionModel.Exposure(true,false,false,false,false)==StealthExposure.Normal, "Agachado pero sin sombra → normal");
    Check(PerceptionModel.Exposure(false,true,false,false,false)==StealthExposure.Normal, "En sombra de pie → normal");
    Check(PerceptionModel.Exposure(false,false,false,true,false)==StealthExposure.Exposed, "Corriendo → expuesto");
    Check(PerceptionModel.Exposure(false,false,false,false,true)==StealthExposure.Exposed, "Bajo luz directa → expuesto");
    Check(PerceptionModel.Exposure(true,true,false,false,true)==StealthExposure.Concealed, "Agachado en sombra gana a la luz");
    Check(Near(PerceptionModel.RangeMultiplier(StealthExposure.Concealed),0.6f) && Near(PerceptionModel.RangeMultiplier(StealthExposure.Normal),1f)
          && Near(PerceptionModel.RangeMultiplier(StealthExposure.Exposed),1.3f), "Multiplicadores: oculto ×0,6 · normal ×1 · expuesto ×1,3");

    // ---------- Alcance efectivo con tope duro ----------
    Check(Near(PerceptionModel.EffectiveRange(12f,1f,1f,false,15f,6f),12f), "Base: 12 m");
    Check(Near(PerceptionModel.EffectiveRange(12f,1f,0.6f,false,15f,6f),7.2f), "Oculto: 12 × 0,6 = 7,2 m (−40 %)");
    Check(Near(PerceptionModel.EffectiveRange(12f,1f,1.3f,false,15f,6f),15f), "Expuesto: 12 × 1,3 = 15,6 → tope de 15 m");
    Check(Near(PerceptionModel.EffectiveRange(18f,1.3f,1.3f,false,15f,6f),15f), "Alerta Roja y sala grande: nunca pasa de 15 m");
    Check(Near(PerceptionModel.EffectiveRange(11f,1.15f,1f,false,15f,6f),12.65f), "Alerta Amarilla dentro del tope: 11 × 1,15");
    Check(Near(PerceptionModel.EffectiveRange(12f,1.3f,1.3f,true,15f,6f),6f), "En humo: como mucho 6 m");
    Check(Near(PerceptionModel.EffectiveRange(12f,1f,0.6f,true,15f,6f),6f) && Near(PerceptionModel.EffectiveRange(8f,1f,0.6f,true,15f,6f),4.8f), "En humo y oculto: el menor de los dos límites");
    Check(Near(PerceptionModel.EffectiveRange(12f,1f,1f,true,4f,6f),4f), "El tope de humo nunca supera el tope general");

    // ---------- Cono de visión ----------
    var fwd=V(0,0,1);
    Check(PerceptionModel.InCone(fwd,V(0,0,5),80f), "Delante: dentro del cono");
    Check(PerceptionModel.InCone(fwd,V(3.2f,0,5),80f), "A 32,6° del centro con cono de 80°: dentro");
    Check(!PerceptionModel.InCone(fwd,V(5,0,5),80f), "A 45° con cono de 80° (media apertura 40°): fuera");
    Check(PerceptionModel.InCone(fwd,V(5,0,5),90.5f), "A 45° con cono de 90,5°: dentro");
    Check(!PerceptionModel.InCone(fwd,V(0,0,-5),80f) && !PerceptionModel.InCone(fwd,V(-5,0,0),80f), "Detrás y al lado: totalmente ciego");
    Check(!PerceptionModel.InCone(fwd,V(0,0,0),80f) && !PerceptionModel.InCone(V(0,0,0),V(0,0,5),80f), "Vectores nulos: no ve (sin divisiones por cero)");
    var down8=V(0,-0.1405f,1);   // ojo inclinado 8° hacia abajo
    Check(PerceptionModel.InViewCone(down8,V(0,-1,0.8f),80f,60f), "Agachado a 0,8 m delante del robot: lo ve (cono horizontal + límite vertical)");
    Check(!PerceptionModel.InCone(down8,V(0,-1,0.8f),80f), "(con el cono 3D antiguo quedaba en un punto ciego bajo el ojo)");
    Check(!PerceptionModel.InViewCone(down8,V(-1,-0.5f,-1),80f,60f) && !PerceptionModel.InViewCone(down8,V(5,0,5),80f,60f), "Cono horizontal: detrás y a 45° del centro sigue ciego");
    Check(!PerceptionModel.InViewCone(down8,V(0,6,1),80f,60f) && !PerceptionModel.InViewCone(down8,V(0,-1.6f,0.1f),80f,60f), "Muy por encima o justo a sus pies: fuera del límite vertical");
    Check(!PerceptionModel.InViewCone(down8,V(0,0,0),80f,60f), "Vector nulo: no ve");
    Check(Near(PerceptionModel.ClampViewAngle(30f),75f) && Near(PerceptionModel.ClampViewAngle(120f),90f) && Near(PerceptionModel.ClampViewAngle(80f),80f), "Ángulo configurable entre 75° y 90°");

    // ---------- Humo en el rayo ----------
    Check(Near(PerceptionModel.SegmentSphereLength(V(-5,0,0),V(5,0,0),V(0,0,0),1f),2f), "Rayo por el centro de un humo de radio 1: atraviesa 2 m");
    Check(Near(PerceptionModel.SegmentSphereLength(V(-5,0.6f,0),V(5,0.6f,0),V(0,0,0),1f),1.6f), "Rayo descentrado 0,6 m: atraviesa 1,6 m");
    Check(Near(PerceptionModel.SegmentSphereLength(V(-5,2,0),V(5,2,0),V(0,0,0),1f),0f), "Rayo que pasa por encima: 0 m");
    Check(Near(PerceptionModel.SegmentSphereLength(V(-5,0,0),V(-2,0,0),V(0,0,0),1f),0f), "Rayo que termina antes del humo: 0 m");
    Check(Near(PerceptionModel.SegmentSphereLength(V(0,0,0),V(5,0,0),V(0,0,0),1f),1f), "Ojo dentro del humo: cuenta desde el ojo hasta el borde");
    Check(Near(PerceptionModel.SegmentSphereLength(V(0.2f,0,0),V(0.5f,0,0),V(0,0,0),1f),0.3f), "Rayo entero dentro del humo: su longitud");
    var cs=new Vector3[]{V(0,0,0),V(4,0,0)}; var rs=new float[]{1f,1f};
    Check(PerceptionModel.SmokeBlocks(V(-5,0,0),V(5,0,0),cs,rs,2,1.5f), "Dos humos en el camino (4 m de humo): tapa");
    Check(!PerceptionModel.SmokeBlocks(V(-5,0.8f,0),V(-2,0.8f,0),cs,rs,2,1.5f), "Sin humo en el camino: no tapa");
    Check(!PerceptionModel.SmokeBlocks(V(-5,0.8f,0),V(1,0.8f,0),cs,rs,2,1.5f), "Solo 1,2 m de humo fino en el borde: no tapa");
    Check(PerceptionModel.InsideAnySmoke(V(3.5f,0,0),cs,rs,2) && !PerceptionModel.InsideAnySmoke(V(2,0,0),cs,rs,2), "Dentro o fuera de un humo");
    Check(!PerceptionModel.SmokeBlocks(V(-5,0,0),V(5,0,0),cs,rs,0,1.5f), "Sin humos registrados: nunca tapa");

    // ---------- Puntos de comprobación del cuerpo ----------
    var pts=new Vector3[PerceptionModel.SamplePointCount];
    int n=PerceptionModel.SamplePoints(V(0,0,10),1.8f,V(0,2,0),pts);
    Check(n==5, "5 rayos: cabeza, pecho, cadera y dos hombros");
    Check(Near(pts[0].y,1.62f) && Near(pts[1].y,1.08f) && Near(pts[2].y,0.63f), "Alturas de pie: cabeza 1,62 · pecho 1,08 · cadera 0,63 m");
    Check(Near(pts[3].x,-0.22f) && Near(pts[4].x,0.22f) && Near(pts[3].z,10f) && Near(pts[3].y,1.35f), "Hombros a ±22 cm, perpendiculares a la mirada del robot");
    n=PerceptionModel.SamplePoints(V(0,0,10),1f,V(0,2,0),pts);
    Check(Near(pts[0].y,0.9f) && Near(pts[2].y,0.35f), "Agachado (1 m): los puntos bajan con la altura");
    n=PerceptionModel.SamplePoints(V(10,0,0),1.8f,V(0,2,0),pts);
    Check(Near(pts[3].z,0.22f) && Near(pts[4].z,-0.22f) && Near(pts[3].x,10f), "Mirando por el eje X los hombros se separan en Z");
    n=PerceptionModel.SamplePoints(V(0,0,0),1.8f,V(0,2,0),pts);
    Check(n==5 && Near(pts[3].x,-0.22f), "Robot justo encima: hombros con dirección por defecto (sin NaN)");

    // ---------- Rayos visibles → velocidad de detección ----------
    Check(Near(PerceptionModel.DetectionFactor(0,5),0f) && Near(PerceptionModel.DetectionFactor(5,5),1f), "0 rayos → no detecta · 5 rayos → velocidad completa");
    Check(PerceptionModel.DetectionFactor(1,5)>0.4f && PerceptionModel.DetectionFactor(1,5)<PerceptionModel.DetectionFactor(3,5), "Asomar un hombro detecta más despacio que estar medio a la vista");
    Check(Near(PerceptionModel.DetectionFactor(2,0),0f), "Sin rayos: 0");

    // ---------- Luz directa ----------
    Check(PerceptionModel.IsLitBy(V(0,3,0),10f,1.6f,false,V(0,0,0),80f,V(0,1,0),0.5f), "Bajo una lámpara cercana encendida: iluminado");
    Check(!PerceptionModel.IsLitBy(V(0,3,0),10f,1.6f,false,V(0,0,0),80f,V(8,1,0),0.5f), "Lejos de la lámpara (más del 60 % de su alcance): no");
    Check(!PerceptionModel.IsLitBy(V(0,3,0),10f,0.2f,false,V(0,0,0),80f,V(0,1,0),0.5f), "Lámpara casi apagada: no");
    Check(PerceptionModel.IsLitBy(V(0,3,0),10f,2f,true,V(0,-1,0),80f,V(0.5f,1,0),0.5f), "Dentro del haz de un foco: iluminado");
    Check(!PerceptionModel.IsLitBy(V(0,3,0),10f,2f,true,V(0,-1,0),40f,V(3,1,0),0.5f), "Fuera del haz de un foco estrecho: no");
    Check(PerceptionModel.IsLitBy(V(0,5.4f,0),10f,1.5f,false,V(0,0,0),80f,V(1,1.1f,1),0.5f), "Lámpara alta del laboratorio: justo debajo, iluminado");
    Check(!PerceptionModel.IsLitBy(V(0,5.4f,0),10f,1.5f,false,V(0,0,0),80f,V(3.5f,1.1f,0),0.5f), "A 3,5 m en horizontal de esa lámpara ya no es «luz directa» (charco de luz de 2,5 m)");
    Check(PerceptionModel.IsLitBy(V(0,2.7f,0),5f,2.5f,false,V(0,0,0),80f,V(1.4f,1,0),0.5f) && !PerceptionModel.IsLitBy(V(0,2.7f,0),5f,2.5f,false,V(0,0,0),80f,V(1.7f,1,0),0.5f), "Lámpara pequeña: charco de luz de 1,5 m como mínimo");

    // ---------- Búsqueda en la última posición ----------
    var probe=PerceptionModel.SearchProbe(V(2,0,3),V(0,0,3f),3f);
    Check(Near(probe.x,2f) && Near(probe.z,6f) && Near(probe.y,0f), "Busca 3 m más allá en la dirección en que huías");
    probe=PerceptionModel.SearchProbe(V(2,0,3),V(0.1f,0,0.1f),3f);
    Check(Near(probe.x,2f) && Near(probe.z,3f), "Si estabas quieto, busca en el mismo punto");
    probe=PerceptionModel.SearchProbe(V(0,0,0),V(4,5,0),2f);
    Check(Near(probe.x,2f) && Near(probe.y,0f), "Solo cuenta el movimiento horizontal (no salta ni cae)");
    Check(PerceptionModel.ShouldEndPursuit(false,true,1f,10f) && PerceptionModel.ShouldEndPursuit(false,false,10f,10f)
          && !PerceptionModel.ShouldEndPursuit(false,false,3f,10f) && !PerceptionModel.ShouldEndPursuit(true,true,30f,10f),
          "Al perderte va a la última posición: busca al llegar o si tarda demasiado; si te ve, sigue persiguiendo");

    // ---------- Comportamiento del robot al perderte ----------
    var dr=new EnemyPatrol(); dr.transform=new Transform(); var ag=new NavMeshAgent(); ag.isOnNavMesh=true; var vis=new EnemyVision();
    Set(dr,"agent",ag); Set(dr,"vision",vis); Set(dr,"player",new Transform()); Time.deltaTime=0.1f;
    Prop(vis,"Awareness",1f); Prop(vis,"CanSeePlayer",false);
    Call(dr,"Update");
    Check(dr.CurrentState==EnemyPatrol.EnemyState.Patrol, "Alerta al máximo pero sin verte ahora mismo: no empieza a perseguir");
    var seen=V(4,0,9); Prop(vis,"LastSeenPosition",seen); Prop(vis,"LastSeenVelocity",V(0,0,0)); Prop(vis,"CanSeePlayer",true);
    Call(dr,"Update");
    Check(dr.CurrentState==EnemyPatrol.EnemyState.Chase, "Te ve con la alerta al máximo: persigue");
    Prop(vis,"CanSeePlayer",false); ag.pathPending=true; ag.remainingDistance=6f; int before=ag.destinationsSet;
    Call(dr,"UpdateChase");
    Check(dr.CurrentState==EnemyPatrol.EnemyState.Chase && ag.destinationsSet==before, "Parpadeo de 0,1 s sin verte: aún no lo da por perdido");
    Time.deltaTime=0.3f; Call(dr,"UpdateChase");
    Check(dr.CurrentState==EnemyPatrol.EnemyState.Chase && ag.destination==(object)seen && ag.destinationsSet==before+1, "Te pierde: va a la última posición donde te vio");
    Call(dr,"UpdateChase"); Call(dr,"UpdateChase");
    Check(ag.destinationsSet==before+1, "No actualiza el destino mientras no te ve (no te sigue «por arte de magia»)");
    ag.pathPending=false; ag.remainingDistance=0f;
    Call(dr,"UpdateChase");
    Check(dr.CurrentState==EnemyPatrol.EnemyState.Search && Near((float)Get(dr,"stateTimer"),6f), "Al llegar busca durante 6 s");
    Time.deltaTime=3.5f; Call(dr,"UpdateSearch"); Check(dr.CurrentState==EnemyPatrol.EnemyState.Search, "A los 3,5 s sigue buscando");
    Call(dr,"UpdateSearch"); Check(dr.CurrentState==EnemyPatrol.EnemyState.Patrol, "Sin encontrarte, vuelve a patrullar");
    var dr2=new EnemyPatrol(); dr2.transform=new Transform(); var ag2=new NavMeshAgent(); ag2.isOnNavMesh=true; var vis2=new EnemyVision();
    Set(dr2,"agent",ag2); Set(dr2,"vision",vis2); Set(dr2,"player",new Transform()); Prop(vis2,"Awareness",1f); Prop(vis2,"CanSeePlayer",true); Prop(vis2,"LastSeenPosition",V(1,0,1)); Prop(vis2,"LastSeenVelocity",V(0,0,0));
    Time.deltaTime=0.1f; Call(dr2,"Update"); Prop(vis2,"CanSeePlayer",false); ag2.pathPending=true;
    Time.deltaTime=8.1f; Call(dr2,"UpdateChase");
    Check(dr2.CurrentState==EnemyPatrol.EnemyState.Search, "Si no consigue llegar en 8 s, busca donde esté");

    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
