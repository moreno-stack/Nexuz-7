using System;
using System.Collections.Generic;
using Nexus7.Combat;
static class T {
  static int fails=0; static void Check(bool c,string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }
  static int Total(List<List<RobotKind>> w){ int n=0; foreach(var x in w) n+=x.Count; return n; }

  static void Catalog(){
    Check(EncounterCatalog.ForScene("AlmacenCarga").Length==2 && EncounterCatalog.ForScene("Laboratorio").Length==3 && EncounterCatalog.ForScene("CentroControl").Length==3, "Oleadas por fase como en el diseño (2, 3 y 3)");
    Check(EncounterCatalog.ForScene("SalaInicial").Length==0, "La Sala inicial no tiene robots: sin encuentro");
    Check(EncounterCatalog.ForScene("MenuPrincipal").Length==0, "Fuera de las fases no hay encuentros");
    Check(Array.IndexOf(EncounterCatalog.ForScene("Evacuacion")[1], RobotKind.Elite)>=0, "La evacuación acaba con élites");
    var normal = EncounterCatalog.Scale(EncounterCatalog.ForScene("CentroControl"), 1, 0.5f);
    Check(Total(normal)==9, "Normal con tensión media: 3+3+3");
    var easy = EncounterCatalog.Scale(EncounterCatalog.ForScene("CentroControl"), 0, 0.5f);
    var hard = EncounterCatalog.Scale(EncounterCatalog.ForScene("CentroControl"), 2, 0.5f);
    Check(Total(easy)<Total(normal) && Total(hard)>Total(normal), "Fácil trae menos y Difícil más");
    var tiny = EncounterCatalog.Scale(new[]{ new[]{ RobotKind.Security } }, 0, 0.2f);
    Check(tiny.Count==1 && tiny[0].Count==1, "Mínimo 1 robot por oleada");
    var hot = EncounterCatalog.Scale(EncounterCatalog.ForScene("AlmacenCarga"), 1, 0.8f);
    var cold = EncounterCatalog.Scale(EncounterCatalog.ForScene("AlmacenCarga"), 1, 0.2f);
    Check(hot[1].Count==3 && Total(cold)==3, "Director: tensión alta +1 en la última oleada; baja −1 en la mayor");
    Check(hard[0].Contains(RobotKind.Security) && hard[2].Contains(RobotKind.Elite), "Al escalar se repiten los tipos de cada oleada");
  }

  static void Trigger(){
    Check(EncounterTriggerRule.ShouldActivate("hackeo", true) && EncounterTriggerRule.ShouldActivate("consola", true), "Un objetivo cumplido activa el encuentro");
    Check(!EncounterTriggerRule.ShouldActivate("punto_seguro", true), "Con objetivos en la fase, un punto seguro no lo activa");
    Check(EncounterTriggerRule.ShouldActivate("punto_seguro", false), "Sin objetivos, lo activa el punto seguro");
  }

  static void Waves(){
    var m = new WaveModel(2);
    Check(m.Activate(0f) && !m.Activate(1f), "Se activa una sola vez");
    Check(m.Tick(2.9f, 0)==EncounterEvent.None && m.CountdownLeft(2f)>0.9f, "Cuenta atrás de 3 s");
    Check(m.Tick(3f, 0)==EncounterEvent.SpawnWave && m.CurrentWave==0, "Aparece la oleada 1");
    Check(m.Tick(5f, 2)==EncounterEvent.None, "Con 2 vivos espera");
    Check(m.Tick(6f, 1)==EncounterEvent.Announce && m.State==EncounterState.Countdown, "Con 1 vivo: «¡REFUERZOS!» y cuenta atrás");
    Check(m.Tick(9f, 1)==EncounterEvent.SpawnWave && m.CurrentWave==1, "Aparece la oleada 2");
    Check(m.Tick(40f, 1)==EncounterEvent.None, "En la última oleada no hay prisa: hay que despejar");
    Check(m.Tick(41f, 0)==EncounterEvent.Cleared && m.State==EncounterState.Cleared, "Sin robots vivos: zona despejada");
    var slow = new WaveModel(3); slow.Activate(0f); slow.Tick(3f, 0);
    Check(slow.Tick(27.9f, 3)==EncounterEvent.None && slow.Tick(28f, 3)==EncounterEvent.Announce, "Si tardas 25 s, llega la siguiente oleada igualmente");
    slow.Reset();
    Check(slow.State==EncounterState.Idle && slow.CurrentWave==-1 && slow.Activate(30f), "Al caer se reinicia y se puede volver a activar");
    Check(!new WaveModel(0).Activate(0f), "Sin oleadas no se activa");
  }

  static void Spawns(){
    // Jugador en (0,0) mirando a +Z. Candidatos: delante a 15 m, detrás a 15 m, muy cerca, muy lejos, lado a 14 m
    var xs = new List<float>{ 0f, 0f, 2f, 0f, 14f };
    var zs = new List<float>{ 15f, -15f, 2f, -60f, 0f };
    var rng = new Random(1);
    var pick = EncounterSpawnPicker.Pick(xs, zs, 0,0, 0,1, null, 2, rng);
    Check(pick.Count==2 && !pick.Contains(2) && !pick.Contains(3), "Nunca a menos de 8 m ni a más de 35 m");
    Check(!pick.Contains(0), "Evita aparecer delante de tu cara si hay otros puntos");
    var sx = new List<float>{ 0f, 14f, -14f }; var sz = new List<float>{ -15f, 0f, 0f };
    var first = EncounterSpawnPicker.Pick(sx, sz, 0,0, 0,1, null, 1, new Random(5));
    bool varied = true;
    for (int seed=0; seed<20; seed++){ var next = EncounterSpawnPicker.Pick(sx, sz, 0,0, 0,1, new HashSet<int>(first), 1, new Random(seed)); varied &= next[0]!=first[0]; }
    Check(varied, "Anti-repetición: el intento siguiente usa otro punto (20 semillas)");
    var fair = EncounterSpawnPicker.Pick(new List<float>{0f,0f}, new List<float>{15f,-15f}, 0,0, 0,1, new HashSet<int>{1}, 1, new Random(6));
    Check(fair[0]==1, "La justicia pesa más que la variedad: antes repetir que aparecer delante");
    var many = EncounterSpawnPicker.Pick(new List<float>{0f}, new List<float>{-15f}, 0,0, 0,1, null, 3, new Random(3));
    Check(many.Count==3, "Con pocos puntos se reutilizan (se separan al aparecer)");
    Check(EncounterSpawnPicker.Pick(new List<float>{1f}, new List<float>{1f}, 0,0, 0,1, null, 2, new Random(4)).Count==0, "Sin puntos válidos no aparece nadie encima del jugador");
  }

  static void Main(){
    Catalog(); Trigger(); Waves(); Spawns();
    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
