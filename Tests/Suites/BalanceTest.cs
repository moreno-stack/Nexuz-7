using System;
using Nexus7.Combat;
using Nexus7.Director;
using Nexus7.Gameplay;
// Revisión final de balance: reglas que unen varios sistemas. Si al ajustar un parámetro se rompe una, el juego se vuelve
// injusto o incoherente aunque cada suite por separado siga pasando.
static class T {
  static int fails=0; static void Check(bool c,string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }

  static void Director(){
    var m=new DirectorModel(1);
    for(int d=0; d<3; d++){ float b=DirectorModel.BaselineFor(d); if(b<m.MinTension||b>m.MaxTension) Check(false,"Base fuera de límites en dificultad "+d); }
    Check(DirectorModel.BaselineFor(0)<DirectorModel.BaselineFor(1) && DirectorModel.BaselineFor(1)<DirectorModel.BaselineFor(2), "La tensión base crece con la dificultad y está dentro de los límites");
    var low=DirectorModel.KnobsFor(m.MinTension); var high=DirectorModel.KnobsFor(m.MaxTension);
    Check(low.EnemySpeed<=high.EnemySpeed && low.Detection<=high.Detection && low.EnemyDamage<=high.EnemyDamage, "Más tensión nunca hace a los robots más lentos, ciegos o flojos");
    Check(low.AmmoDrop>=high.AmmoDrop && low.EnemySpread>=high.EnemySpread, "Más tensión nunca da más munición ni peor puntería a los robots");
    Check(low.Reinforcements<=high.Reinforcements && low.ExtraShooters<=high.ExtraShooters, "Los refuerzos y tiradores extra solo suben con la tensión");
    Check(DirectorModel.BaselineFor(0)-m.LastLifeMargin>=m.MinTension, "Con la última vida en Fácil el objetivo sigue dentro de los límites");
    Check(m.ReliefDrop<m.MaxTension-m.MinTension, "El respiro al caer no vacía la tensión de golpe");
    Check(ShipEventModel.HelpBelow<ShipEventModel.HurtFrom, "Los eventos que ayudan salen con menos tensión que los que aprietan");
  }

  static void Lives(){
    Check(LivesModel.MaxFor(0)>LivesModel.MaxFor(1) && LivesModel.MaxFor(1)>LivesModel.MaxFor(2) && LivesModel.MaxFor(2)>=2, "Vidas 5/3/2: menos con más dificultad y nunca una sola");
    Check(RespawnPenalty.LossFraction<=0.25f && RespawnPenalty.CountdownFloor>=60f, "El castigo al caer es moderado (≤ 25 % de recursos, cuenta atrás ≥ 60 s)");
  }

  static void Encounters(){
    int maxWaves=0;
    foreach (var s in new[]{"AlmacenCarga","SalaEnergia","Laboratorio","CentroControl","Evacuacion"}) maxWaves=Math.Max(maxWaves,EncounterCatalog.ForScene(s).Length);
    float worst=maxWaves*(WaveModel.MaxWaveTime+WaveModel.CountdownTime);
    Check(EncounterSealModel.MaxSeal>worst, $"El sello de la salida ({EncounterSealModel.MaxSeal:0} s) dura más que el encuentro más largo forzado por tiempo ({worst:0} s)");
    Check(!EncounterSealModel.ShouldSeal(0,false) && !EncounterSealModel.ShouldSeal(2,true), "Fácil y la última vida nunca sellan la salida");
    var r=EncounterRewardModel.Choose(1f,1f,LivesModel.MaxFor(1),LivesModel.MaxFor(1));
    Check(r.AmmoPacks>=1f, "Despejar un encuentro siempre da algo");
    Check(EncounterRewardModel.EmergencyHeal<100f, "La reparación del encuentro no sustituye a un kit completo de curación");
  }

  static void Spawns(){
    Check(HeatmapModel.SafeDistance>=8f, "La vigilancia de rutas no se coloca encima de la reaparición (≥ 8 m)");
    Check(EncounterSpawnPicker.MinDistance>=8f, "Las oleadas no aparecen a menos de 8 m");
    Check(ShipEventModel.MinDistance<ShipEventModel.MaxDistance, "Los eventos de nave tienen un rango de distancia válido");
  }

  static void Readability(){
    Check(DeathCauseModel.CloseSpot<DeathCauseModel.FarSpot, "«De cerca» y «desde lejos» no se pisan");
    Check(DeathCauseModel.CardTime>=5f && DeathCauseModel.Memory>=10f, "La tarjeta de la caída dura lo bastante para leerla y mira al menos 10 s atrás");
    Check(DebugPanelModel.TensionStep>0f && DebugPanelModel.TensionStep<=0.2f, "El panel F4 mueve la tensión en pasos pequeños");
    Check(WeaponModModel.MaxSlots>=1 && WeaponModModel.MaxSlots<=3, "Los accesorios no se acumulan sin límite");
  }

  static void Main(){
    Director(); Lives(); Encounters(); Spawns(); Readability();
    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
