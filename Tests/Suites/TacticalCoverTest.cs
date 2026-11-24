using System;
using System.Collections.Generic;
using Nexus7.Combat;
static class T {
  static int fails=0; static void Check(bool c,string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }

  static CoverSpot S(int id,float x,float z){ return new CoverSpot{ Id=id, X=x, Z=z }; }
  static TacticInput In(bool arrived=false,bool protects=true,bool active=false,bool finished=false,bool token=true,float tempo=1f,double roll=0.0){
    return new TacticInput{ Arrived=arrived, CoverProtects=protects, BurstActive=active, BurstFinished=finished, TokenAvailable=token, Tempo=tempo, Roll=roll };
  }

  static void Styles(){
    Check(TacticsCatalog.StyleFor("RobotSeguridad_A")==TacticStyle.Hold && TacticsCatalog.StyleFor("RobotGuardia_B")==TacticStyle.Advance && TacticsCatalog.StyleFor("RobotElite_C")==TacticStyle.Flank, "Estilo según el tipo: seguridad se cubre, guardia avanza, élite flanquea");
  }

  static void Scoring(){
    // Robot en (0,0), jugador en (0,12)
    var spot = S(1,2f,2f);
    float prot = CoverScoring.Score(0,0, 0,12, spot, true, false, CoverIntent.Normal);
    float open = CoverScoring.Score(0,0, 0,12, spot, false, false, CoverIntent.Normal);
    Check(Math.Abs(prot-open-CoverScoring.Protects)<0.01f, "Un punto que protege suma 50");
    float ideal = CoverScoring.Score(0,0, 0,12, S(2,0,2f), false,false,CoverIntent.Normal);   // a 10 m del jugador
    float tooFar = CoverScoring.Score(0,0, 0,30, S(3,0,2f), false,false,CoverIntent.Normal);  // a 28 m
    Check(ideal>tooFar, "Prefiere estar a 6–14 m del jugador");
    Check(CoverScoring.Score(0,0, 0,12, spot, true, true, CoverIntent.Normal)==CoverScoring.Rejected, "Reservado por otro: descartado");
    Check(CoverScoring.Score(0,0, 0,12, S(4,0,10f), true,false,CoverIntent.Normal)==CoverScoring.Rejected, "A menos de 3 m del jugador: descartado");
    Check(CoverScoring.Score(0,0, 0,12, S(5,20f,0), true,false,CoverIntent.Normal)==CoverScoring.Rejected, "A más de 15 m del robot: descartado");
    Check(CoverScoring.Score(0,0, 0,12, S(6,1,1), true,false,CoverIntent.Normal) > CoverScoring.Score(0,0, 0,12, S(7,-6,1), true,false,CoverIntent.Normal), "Recorrer menos puntúa más");
    // Flanqueo: un punto al lado del jugador cambia mucho el ángulo
    var side = S(8,8f,10f);
    Check(Math.Abs(CoverScoring.Score(0,0, 0,12, side, true,false,CoverIntent.Flank) - CoverScoring.Score(0,0, 0,12, side, true,false,CoverIntent.Normal) - CoverScoring.FlankBonus) < 0.01f, "Al flanquear, cambiar de ángulo suma 25");
    Check(CoverScoring.Score(0,0, 0,12, S(9,0,1), true,false,CoverIntent.Flank) == CoverScoring.Score(0,0, 0,12, S(9,0,1), true,false,CoverIntent.Normal), "El mismo ángulo no cuenta como flanqueo");
    var closer = S(10,0,5f);
    Check(Math.Abs(CoverScoring.Score(0,0, 0,12, closer, true,false,CoverIntent.Advance) - CoverScoring.Score(0,0, 0,12, closer, true,false,CoverIntent.Normal) - CoverScoring.AdvanceBonus) < 0.01f, "Al avanzar, acercarse 2 m o más suma 20");
    Check(Math.Abs(CoverScoring.AngleFromPlayer(0,0, 0,-5, 5,0)-90f)<0.5f, "Ángulo de flanqueo bien medido (90°)");
  }

  static void Best(){
    var spots = new List<CoverSpot>{ S(1,1,1), S(2,2,3), S(3,-2,3) };
    var prot = new List<bool>{ false, true, true };
    var res = new List<bool>{ false, true, false };
    int i = CoverScoring.Best(0,0, 0,12, spots, prot, res, CoverIntent.Normal);
    Check(i==2, "Elige el mejor punto libre que protege (el reservado no)");
    Check(CoverScoring.Best(0,0, 0,12, spots, prot, res, CoverIntent.Normal, excludeId:3)==0 || CoverScoring.Best(0,0, 0,12, spots, prot, res, CoverIntent.Normal, excludeId:3)==-1, "Puede excluir su cobertura actual");
    Check(CoverScoring.Best(0,0, 0,40, new List<CoverSpot>{ S(1,1,1) }, new List<bool>{false}, new List<bool>{false}, CoverIntent.Normal)==-1, "Sin punto que compense: pelea al descubierto");
  }

  static void Reservations(){
    var r = new CoverReservations();
    Check(r.TryReserve(5, 1) && !r.TryReserve(5, 2), "Cada punto es de un solo robot");
    Check(r.TryReserve(6, 1) && r.TryReserve(5, 2) && r.Count==2, "Al cambiar de punto suelta el anterior");
    r.ReleaseAll(2); Check(!r.IsReservedByOther(5, 3), "Al morir o irse lo libera");
  }

  static void Cycle(){
    var m = new TacticsModel(TacticStyle.Hold); m.Engage(0f);
    Check(m.MayFire && m.Tick(0f, In())==TacticOrder.SeekCover, "Al entrar en combate busca cobertura (y puede disparar mientras)");
    m.CoverAssigned(0f);
    Check(!m.MayFire && m.Phase==TacticPhase.Moving, "Mientras corre a la cobertura no dispara");
    m.Tick(1f, In(arrived:true)); Check(m.Phase==TacticPhase.InCover && !m.MayFire, "Llega: a cubierto");
    Check(m.Tick(1.3f, In())==TacticOrder.None, "Espera 0,6–1,2 s antes de asomarse");
    Check(m.Tick(2f, In(token:false))==TacticOrder.None, "Sin turno libre se queda cubierto");
    Check(m.Tick(2.1f, In())==TacticOrder.GoToPeek && m.MayFire, "Con turno: se asoma y puede disparar");
    Check(m.Tick(2.5f, In(active:true))==TacticOrder.None, "Durante la ráfaga sigue asomado");
    Check(m.Tick(3f, In(finished:true))==TacticOrder.BackToCover && m.Phase==TacticPhase.InCover, "Acaba la ráfaga: vuelve a cubrirse");
    Check(m.Tick(3.1f, In(protects:false))==TacticOrder.SeekCover, "Si el jugador lo rodea, cambia de cobertura");
  }

  static void FailedPeeks(){
    var m = new TacticsModel(TacticStyle.Hold); m.Engage(0f); m.CoverAssigned(0f); m.Tick(0f, In(arrived:true));
    m.Tick(2f, In()); Check(m.Tick(4.6f, In())==TacticOrder.BackToCover && m.FailedPeeks==1, "Asomado 2,5 s sin tiro: se vuelve a cubrir");
    m.Tick(6f, In()); Check(m.Tick(8.6f, In())==TacticOrder.SeekCover, "Seguridad: 2 asomadas sin tiro → otra cobertura");
    var e = new TacticsModel(TacticStyle.Flank); e.Engage(0f); e.CoverAssigned(0f); e.Tick(0f, In(arrived:true));
    e.Tick(2f, In()); Check(e.Tick(4.6f, In())==TacticOrder.SeekCover && e.PendingIntent==CoverIntent.Flank, "Élite: 1 asomada sin tiro → flanquea");
  }

  static void StyleClocks(){
    var g = new TacticsModel(TacticStyle.Advance); g.Engage(0f); g.CoverAssigned(0f); g.Tick(0f, In(arrived:true));
    Check(g.Tick(4.9f, In(token:false))==TacticOrder.None, "Guardia: espera su momento");
    Check(g.Tick(5f, In(token:false))==TacticOrder.SeekCover && g.PendingIntent==CoverIntent.Advance, "Guardia: cada 5 s avanza a una cobertura más cerca");
    var e = new TacticsModel(TacticStyle.Flank); e.Engage(0f); e.CoverAssigned(0f); e.Tick(0f, In(arrived:true, roll:0.0));
    Check(e.Tick(4f, In(token:false))==TacticOrder.SeekCover && e.PendingIntent==CoverIntent.Flank, "Élite: flanquea cada 4–6 s");
    var fast = new TacticsModel(TacticStyle.Advance); fast.Engage(0f); fast.CoverAssigned(0f); fast.Tick(0f, In(arrived:true, tempo:0.7f));
    Check(fast.Tick(3.6f, In(token:false, tempo:0.7f))==TacticOrder.SeekCover, "Con el director apretando (tempo 0,7) avanza antes (3,5 s)");
    var h = new TacticsModel(TacticStyle.Hold); h.Engage(0f); h.CoverAssigned(0f); h.Tick(0f, In(arrived:true));
    Check(h.Tick(60f, In(token:false))==TacticOrder.None, "Seguridad no se mueve por reloj: aguanta en su cobertura");
  }

  static void OpenAndStuck(){
    var m = new TacticsModel(TacticStyle.Hold); m.Engage(0f); m.Tick(0f, In()); m.NoCoverFound(0f, 1f);
    Check(m.MayFire && m.Tick(1f, In())==TacticOrder.None && m.Tick(1.5f, In())==TacticOrder.SeekCover, "Sin cobertura: al descubierto y reintenta a los 1,5 s");
    Check(m.Tick(1.5f, In(active:true))==TacticOrder.None, "No interrumpe una ráfaga para buscar cobertura");
    m.CoverAssigned(2f); Check(m.Tick(8f, In())==TacticOrder.SeekCover, "Atascado 6 s de camino: busca otra");
  }

  static void Main(){
    Styles(); Scoring(); Best(); Reservations(); Cycle(); FailedPeeks(); StyleClocks(); OpenAndStuck();
    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
