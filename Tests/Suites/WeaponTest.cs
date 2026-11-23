using System;
using Nexus7.Combat;
static class T {
  static int fails=0; static void Check(bool c,string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }
  static bool Near(float a,float b,float e=0.01f){ return Math.Abs(a-b)<=e; }
  static PlayerWeaponSpec P(){ return PlayerWeaponCatalog.Get(PlayerWeaponCatalog.Pistol); }
  static PlayerWeaponSpec R(){ return PlayerWeaponCatalog.Get(PlayerWeaponCatalog.Rifle); }
  static PlayerWeaponSpec S(){ return PlayerWeaponCatalog.Get(PlayerWeaponCatalog.Shotgun); }
  static PlayerWeaponSpec G(){ return PlayerWeaponCatalog.Get(PlayerWeaponCatalog.Railgun); }

  static void Catalog(){
    Check(PlayerWeaponCatalog.All.Count>=3, "Hay al menos 3 armas en el catálogo");
    var ids = new System.Collections.Generic.HashSet<string>(); bool ok=true;
    foreach(var w in PlayerWeaponCatalog.All){ ok &= ids.Add(w.Id) && w.FireRate>0 && w.MagazineSize>0 && w.Range>0 && w.ProjectileSpeed>0 && w.Pellets>=1 && w.MaxReserve>=w.StartReserve; }
    Check(ok, "Ids únicos y valores válidos (cadencia, cargador, alcance, velocidad, perdigones, reserva)");
    Check(S().Pellets>1 && R().Mode==FireMode.FullAuto && G().Pierce>0, "Escopeta con perdigones, rifle automático y cañón de riel que atraviesa");
    Check(PlayerWeaponCatalog.Get("no_existe")==null && PlayerWeaponCatalog.IndexOf(PlayerWeaponCatalog.Rifle)==1, "Get e IndexOf");
  }
  static void FireRateAndSemi(){
    var w = WeaponModel.Fresh(P());
    Check(w.TryFire(0f,true)==FireResult.Fired && w.Ammo==11, "Primer disparo: gasta una bala");
    Check(w.TryFire(0.05f,true)==FireResult.NeedsRelease, "Semiautomática: sin soltar no vuelve a disparar");
    w.TryFire(0.1f,false);
    Check(w.TryFire(0.15f,true)==FireResult.Cooldown, "No dispara antes de 1/cadencia (0,25 s)");
    w.TryFire(0.2f,false);
    Check(w.TryFire(0.26f,true)==FireResult.Fired, "Tras soltar y pasar la cadencia, dispara");
    var r = WeaponModel.Fresh(R()); int shots=0; float t=0f;
    for(int i=0;i<100;i++){ t=i*0.01f; if(r.TryFire(t,true)==FireResult.Fired) shots++; r.Tick(t,0.01f); }
    Check(shots>=8 && shots<=10, "Automático: ~9 disparos por segundo manteniendo ("+shots+")");
  }
  static void MagazineAndReload(){
    var w = new WeaponModel(P(), 1, 5);
    Check(w.TryFire(0f,true)==FireResult.Fired && w.Ammo==0, "Vacía el cargador");
    w.TryFire(0.1f,false); w.Tick(0.3f,0.3f);
    Check(w.State==WeaponState.Reloading, "Recarga sola al vaciar el cargador si hay reserva");
    w.Tick(1.0f,0.7f); Check(w.State==WeaponState.Reloading && w.ReloadProgress(1.0f)>0.4f, "Durante la recarga sigue recargando y avanza el progreso");
    w.Tick(1.7f,0.7f); Check(w.State==WeaponState.Ready && w.Ammo==5 && w.Reserve==0, "Recarga parcial: mete lo que queda en la reserva (5)");
    var full = WeaponModel.Fresh(P()); Check(!full.StartReload(0f), "Con el cargador lleno no recarga");
    var none = new WeaponModel(P(), 3, 0); Check(!none.StartReload(0f), "Sin reserva no recarga");
    var c = new WeaponModel(P(), 3, 20); c.StartReload(0f); c.CancelReload(); c.Tick(5f,0.1f);
    Check(c.Ammo==3 && c.Reserve==20, "Cancelar la recarga no pierde munición");
    var e = new WeaponModel(P(), 0, 0); Check(e.TryFire(0f,true)==FireResult.Empty && e.IsEmpty, "Sin cargador ni reserva: vacío, sin excepción");
  }
  static void Heat(){
    var w = new WeaponModel(P(), 12, 50); float t=0f; int fired=0;
    for(int i=0;i<9;i++){ t=i*0.3f; if(w.TryFire(t,true)==FireResult.Fired) fired++; w.TryFire(t+0.01f,false); }
    Check(w.State==WeaponState.Overheated, "La pistola se sobrecalienta tras 9 disparos rápidos (12 de calor cada uno)");
    Check(w.TryFire(t+0.5f,true)==FireResult.Overheated, "Sobrecalentada no dispara");
    w.Tick(t+1.6f,0.1f); Check(w.State==WeaponState.Ready && Near(w.Heat,50f), "Tras 1,5 s vuelve a estar lista con el calor a 50");
    var c = WeaponModel.Fresh(P()); c.TryFire(0f,true); float h=c.Heat; c.Tick(0.1f,0.1f);
    Check(Near(c.Heat,h), "No se enfría antes de 0,25 s");
    c.Tick(0.5f,0.1f); Check(c.Heat<h, "Después se enfría (40/s)");
    var s = WeaponModel.Fresh(S()); s.TryFire(0f,true); Check(s.Heat==0f, "La escopeta no se calienta (HeatPerShot = 0)");
  }
  static void BloomAndSpread(){
    var w = WeaponModel.Fresh(R()); for(int i=0;i<20;i++){ w.TryFire(i*0.12f,true); }
    Check(w.Bloom<=R().MaxBloom+1e-4f && w.Bloom>0f, "La dispersión se abre disparando y no pasa del máximo");
    float b=w.Bloom; w.Tick(10f,0.25f); Check(w.Bloom<b, "Se recupera al dejar de disparar");
    Check(SpreadModel.Current(R(),true,false,0f) < SpreadModel.Current(R(),false,false,0f), "Apuntar reduce la dispersión");
    Check(SpreadModel.Current(R(),false,true,0f) > SpreadModel.Current(R(),false,false,0f), "Moverse la aumenta");
    bool inside=true; var rnd=new Random(3);
    for(int i=0;i<500;i++){ float y,p; SpreadModel.Offset(2f,rnd.NextDouble(),rnd.NextDouble(),out y,out p); inside &= Math.Sqrt(y*y+p*p)<=2.0001; }
    Check(inside, "Las direcciones caen siempre dentro del cono");
    bool pel=true; for(int i=0;i<7;i++){ float y,p; SpreadModel.PelletOffset(i,7,6f,rnd.NextDouble(),rnd.NextDouble(),out y,out p); pel &= Math.Sqrt(y*y+p*p)<=6.0001; }
    Check(pel, "Los perdigones de la escopeta también caen dentro del cono");
  }
  static void Falloff(){
    Check(Near(S().DamageFactorAt(3f),1f) && Near(S().DamageFactorAt(22f),0.25f), "Escopeta: daño completo de cerca y 25 % al alcance");
    Check(Near(P().DamageFactorAt(50f),1f), "Pistola: sin caída de daño");
    float mid=R().DamageFactorAt(60f); Check(mid<1f && mid>0.7f, "Rifle: caída lineal entre 40 y 80 m");
  }
  static void Health(){
    var h = new HealthModel(60f);
    Check(h.Apply(20f)==HealthResult.Damaged && Near(h.Current,40f), "Recibe daño");
    Check(h.Apply(50f)==HealthResult.Killed && h.IsDead, "Muere al llegar a 0");
    Check(h.Apply(10f)==HealthResult.Ignored, "Muerto no recibe más daño ni vuelve a morir");
    var r = new HealthModel(100f); r.Apply(50f); r.Rescale(120f); Check(Near(r.Current,60f) && Near(r.Normalized,0.5f), "Reescalar conserva el porcentaje");
    Check(HealthModel.BaseHealthFor("RobotElite_Puente")==120f && HealthModel.BaseHealthFor("RobotGuardia_X")==90f && HealthModel.BaseHealthFor("RobotSeguridad_A")==60f && HealthModel.BaseHealthFor("RobotInvestigacion_R1")==50f, "Vida por tipo de robot");
    Check(Math.Ceiling(60f/P().Damage)==3, "Pistola: 3 disparos al cuerpo para un robot de seguridad");
  }
  static void Arsenal(){
    var a = new ArsenalModel(); int switched=0; a.Switched += w => switched++;
    a.Add(WeaponModel.Fresh(P())); a.Add(WeaponModel.Fresh(R())); a.Add(WeaponModel.Fresh(S()));
    Check(a.Weapons.Count==3 && a.Current.Spec.Id==PlayerWeaponCatalog.Pistol, "Empieza con la primera arma en la mano");
    a.Add(new WeaponModel(R(),0,10)); Check(a.Weapons.Count==3 && a.Find(PlayerWeaponCatalog.Rifle).Reserve==R().StartReserve+10, "Un arma repetida solo suma munición");
    Check(a.Cycle(1,0f) && a.Current.Spec.Id==PlayerWeaponCatalog.Rifle && a.IsSwitching(0.2f) && !a.IsSwitching(0.4f), "Cambiar a la siguiente tarda 0,35 s");
    a.Cycle(1,1f); a.Cycle(1,2f); Check(a.Current.Spec.Id==PlayerWeaponCatalog.Pistol && switched==3, "El ciclo vuelve a la primera");
    a.Cycle(-1,3f); Check(a.Current.Spec.Id==PlayerWeaponCatalog.Shotgun, "Ciclo hacia atrás");
    var cur=a.Current; cur.TryFire(3.5f,true); cur.StartReload(3.6f); a.Select(0,3.7f);
    Check(cur.State==WeaponState.Ready, "Cambiar de arma cancela la recarga");
    int lost=a.ApplyRespawnPenalty(0.25f); Check(lost>0, "Al caer se pierde el 25 % de la reserva ("+lost+")");
    var b = new ArsenalModel(); b.Add(new WeaponModel(P(),12,96)); b.Add(new WeaponModel(S(),0,2));
    Check(b.NeediestWeapon().Spec.Id==PlayerWeaponCatalog.Shotgun, "El paquete de munición va al arma con menos munición");
  }
  static void Sounds(){
    bool ok=true; foreach(CombatSound k in Enum.GetValues(typeof(CombatSound))){ var d=CombatSoundSynth.Make(k); ok&=d.Length>100; foreach(var x in d) ok&= x>=-1f && x<=1f && !float.IsNaN(x); }
    Check(ok, "Sonidos del combate generados, sin saturar ni NaN");
  }
  static void Session(){
    CombatSession.Clear(); var a=CombatSession.Load();
    Check(a.Weapons.Count==PlayerWeaponCatalog.All.Count, "Sin partida guardada: arsenal inicial con todas las armas");
    a.Current.TryFire(0f,true); a.Select(2,0f); CombatSession.Store(a);
    var b=CombatSession.Load(); Check(b.CurrentIndex==2 && b.Weapons[0].Ammo==a.Weapons[0].Ammo && b.Weapons[0].Reserve==a.Weapons[0].Reserve, "Entre fases se conservan el arma en mano, el cargador y la reserva");
    CombatSession.Clear(); Check(!CombatSession.HasSaved, "Nueva partida: se borra el arsenal guardado");
  }
  static void DiskSave(){
    var a = CombatSession.Load(); a.Current.TryFire(0f,true); a.Select(3,0f); int idx;
    var list = CombatSession.ToSaved(a, out idx);
    Check(list.Count==a.Weapons.Count && idx==3 && list[0].ammo==a.Weapons[0].Ammo, "Arsenal → partida guardada (armas, munición y arma en mano)");
    list.Add(new Nexus7.Core.SavedWeapon{ id="arma_borrada", ammo=5, reserve=5 });
    CombatSession.RestoreSaved(list, idx); var b = CombatSession.Load();
    Check(b.Weapons.Count==a.Weapons.Count && b.CurrentIndex==3 && b.Weapons[0].Ammo==a.Weapons[0].Ammo, "Partida guardada → arsenal; un arma que ya no existe se omite");
    CombatSession.RestoreSaved(new System.Collections.Generic.List<Nexus7.Core.SavedWeapon>(), 0);
    Check(!CombatSession.HasSaved && CombatSession.Load().Weapons.Count==PlayerWeaponCatalog.All.Count, "Guardado antiguo sin armas → arsenal inicial");
    var huge = new System.Collections.Generic.List<Nexus7.Core.SavedWeapon>{ new Nexus7.Core.SavedWeapon{ id=PlayerWeaponCatalog.Pistol, ammo=999, reserve=9999 } };
    CombatSession.RestoreSaved(huge, 7); var c = CombatSession.Load();
    Check(c.Current.Ammo==P().MagazineSize && c.Current.Reserve==P().MaxReserve, "Valores manipulados se limitan al cargador y la reserva máximos");
    var json = "{\"version\":1,\"scene\":\"SalaEnergia\",\"savedAtUtcTicks\":1}";
    var old = Nexus7.Core.SaveSlot.Parse(json);
    Check(old!=null && (old.weapons==null || old.weapons.Count==0) && !old.hasDirectorTension, "Un guardado de antes de las armas se sigue leyendo");
    CombatSession.Clear();
  }
  static void Main(){
    DiskSave();
    Sounds(); Session();
    Catalog(); FireRateAndSemi(); MagazineAndReload(); Heat(); BloomAndSpread(); Falloff(); Health(); Arsenal();
    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
