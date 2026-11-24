using System;
using System.Collections.Generic;
using Nexus7.Director;
static class T {
  static int fails=0; static void Check(bool c,string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }

  static PhaseReportModel Played(float seconds, float tension=0.5f, float camp=0f){
    var r = new PhaseReportModel("Fase 2 · Almacén de carga");
    for (float t=0; t<seconds; t+=0.5f) r.Sample(0.5f, tension, camp);
    return r;
  }

  static void Counting(){
    var r = Played(60f);
    r.RecordShot("pistola"); r.RecordShot("pistola"); r.RecordShot("escopeta"); r.RecordShot("pistola");
    r.RecordHit(1); r.RecordHit(1); r.RecordHit(1); r.RecordHit(2);
    Check(r.Shots==4 && r.Hits==2, "Los perdigones del mismo disparo cuentan un solo acierto");
    Check(Math.Abs(r.Accuracy-0.5f)<0.01f, "Precisión de toda la fase = aciertos / disparos");
    Check(new PhaseReportModel("x").Accuracy<0f, "Sin disparos: precisión sin dato (−1)");
    float share = r.DominantWeapon(out string id);
    Check(id=="pistola" && Math.Abs(share-0.75f)<0.01f, "Arma dominante y su porcentaje");
    Check(Math.Abs(r.Duration-60f)<0.01f && Math.Abs(r.AverageTension-0.5f)<0.01f, "Duración y tensión media de las muestras");
    r.Sample(0.5f, 0.9f, 0f); Check(Math.Abs(r.TensionMax-0.9f)<0.01f, "Guarda la tensión máxima");
    r.Sample(0f, 1f, 1f); Check(Math.Abs(r.TensionMax-0.9f)<0.01f, "Una muestra sin tiempo no cuenta");
    r.RecordDamage(-5f); r.RecordDamage(12f); Check(Math.Abs(r.DamageTaken-12f)<0.01f, "Daño recibido (ignora valores negativos)");
    Check(!new PhaseReportModel("x").Meaningful && Played(20f).Meaningful, "Solo enseña informe si la fase duró 20 s o más");
  }

  static void Styles(){
    var ghost = Played(120f); ghost.RecordDetected();
    Check(ghost.Style==PlayStyle.Ghost, "Sin disparar y casi sin detecciones: Fantasma");
    var hurt = Played(120f); hurt.RecordShot("pistola"); hurt.RecordLifeLost(); hurt.RecordLifeLost();
    Check(hurt.Style==PlayStyle.Struggling, "2 vidas perdidas: En apuros");
    var hurtGhost = Played(120f); hurtGhost.RecordLifeLost(); hurtGhost.RecordLifeLost();
    Check(hurtGhost.Style==PlayStyle.Ghost, "Sin disparar ni ser visto cuenta como Fantasma aunque haya caído (capturas)");
    var camp = Played(120f, 0.5f, 0.6f); camp.RecordShot("pistola");
    Check(camp.Style==PlayStyle.Entrenched, "Acampada media ≥ 50 %: Atrincherado");
    var sharp = Played(180f); for(int i=0;i<10;i++){ sharp.RecordShot("fusil"); if(i<5) sharp.RecordHit(i); } for(int i=0;i<3;i++) sharp.RecordKill();
    Check(sharp.Style==PlayStyle.Sharpshooter, "3 bajas con 50 % de precisión: Tirador");
    var aggro = Played(60f); for(int i=0;i<10;i++) aggro.RecordShot("escopeta"); aggro.RecordHit(1); aggro.RecordKill(); aggro.RecordKill();
    Check(aggro.Style==PlayStyle.Aggressive, "2 bajas por minuto con poca precisión: Agresivo");
    var mid = Played(120f); mid.RecordShot("pistola"); mid.RecordKill();
    Check(mid.Style==PlayStyle.Balanced, "Si nada destaca: Equilibrado");
    Check(PhaseReportModel.StyleName(PlayStyle.Entrenched)=="Atrincherado", "Nombres en español");
  }

  static void Text(){
    var r = Played(95f); for(int i=0;i<12;i++) r.RecordShot("pistola"); r.RecordHit(0); r.RecordFlush(); r.RecordAssist(); r.RecordShipEvent(); r.RecordModReward(); r.RecordModReward();
    Check(r.Verdict().Contains("misma arma"), "Si usa casi siempre la misma arma (≥ 70 %), el veredicto lo avisa");
    var few = Played(95f); few.RecordShot("pistola"); few.RecordKill();
    Check(!few.Verdict().Contains("misma arma"), "Con pocos disparos no saca conclusiones del arma");
    var lines = r.Lines(id => id=="pistola" ? "Pistola" : null);
    Check(lines[0].StartsWith("Tiempo 1:35"), "Tiempo en minutos y segundos");
    Check(lines[2].Contains("Pistola 100%"), "Usa el nombre visible del arma");
    Check(lines[5].Contains("1 expulsiones") && lines[5].Contains("1 ayudas") && lines[5].Contains("1 eventos") && lines[5].Contains("2 módulos"), "Cuenta las acciones de la IA (también los módulos regalados)");
    Check(new PhaseReportModel("x").Lines()[1].Contains("Sin disparos"), "Sin disparos lo dice en lugar de 0 %");
  }

  static void Main(){
    Counting(); Styles(); Text();
    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
