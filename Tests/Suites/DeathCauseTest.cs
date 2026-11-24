using System;
using Nexus7.Director;
static class T {
  static int fails=0; static void Check(bool c,string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }

  // Visto a media distancia por un dron, andando de pie y sin luz: nada destaca
  static DeathFacts Seen(){ return new DeathFacts{ Kind=DeathKind.Caught, Spotted=true, Spotter="un dron de seguridad", SpotDistance=8f, SecondsSinceSpot=4f, Spotters=1, Noise=3f }; }

  static void Tips(){
    var f=Seen(); Check(DeathCauseModel.ChooseTip(f)==DeathTip.Route, "Si nada destaca: estudiar la ruta");
    f=Seen(); f.Kind=DeathKind.Hazard; f.Running=true; Check(DeathCauseModel.ChooseTip(f)==DeathTip.Hazard, "Una descarga va antes que todo");
    f=Seen(); f.Running=true; f.Lit=true; Check(DeathCauseModel.ChooseTip(f)==DeathTip.Running, "Corriendo cuando te vieron (va antes que la luz)");
    f=Seen(); f.Lit=true; Check(DeathCauseModel.ChooseTip(f)==DeathTip.Light, "Bajo luz directa");
    f=Seen(); f.Spotters=3; Check(DeathCauseModel.ChooseTip(f)==DeathTip.Outnumbered, "Te vieron varios robots");
    f=Seen(); f.Kind=DeathKind.KnockedOut; f.DamageTaken=100f; Check(DeathCauseModel.ChooseTip(f)==DeathTip.UnderFire, "Fuera de combate: cubrirse");
    f=Seen(); f.Spotted=false; f.Noise=8f; Check(DeathCauseModel.ChooseTip(f)==DeathTip.Noise, "Nadie te vio pero hacías ruido");
    f=Seen(); f.Spotted=false; f.Running=true; Check(DeathCauseModel.ChooseTip(f)==DeathTip.Unseen, "Correr sin que te vean no es la causa: te sorprendieron");
    f=Seen(); f.SpotDistance=15f; Check(DeathCauseModel.ChooseTip(f)==DeathTip.FarSight, "Te vieron desde lejos (≥ 12 m)");
    f=Seen(); f.SpotDistance=4f; Check(DeathCauseModel.ChooseTip(f)==DeathTip.Ambush, "Te vieron de cerca (≤ 5 m)");
    bool all=true; foreach (DeathTip t in Enum.GetValues(typeof(DeathTip))) if (string.IsNullOrEmpty(DeathCauseModel.TipText(t)) || string.IsNullOrEmpty(DeathCauseModel.TipName(t))) all=false;
    Check(all, "Cada consejo tiene texto y nombre corto");
  }

  static void Card(){
    var a=DeathCauseModel.Analyze(Seen());
    Check(a.Title=="TE HAN ATRAPADO" && a.Seen.Contains("un dron de seguridad") && a.Seen.Contains("8 m") && a.Seen.Contains("4 s"), "Dice quién te vio, a qué distancia y cuándo");
    Check(a.Factors.Count==0, "Sin factores si nada pesaba");
    var f=Seen(); f.Lit=true; f.Running=true; f.Noise=9f; f.Spotters=2; f.DamageTaken=35f; a=DeathCauseModel.Analyze(f);
    Check(a.Factors.Count==5 && a.Factors[0].Contains("luz") && a.Factors[2].Contains("9 m") && a.Factors[3].Contains("2 robots") && a.Factors[4].Contains("35"), "Lista lo que pesaba: luz, correr, ruido, varios robots, daño");
    f=Seen(); f.Crouching=true; f.Shadow=true; a=DeathCauseModel.Analyze(f);
    Check(a.Factors.Count==1 && a.Factors[0].Contains("sombra"), "Agachado en la sombra cuenta como un solo factor");
    f=Seen(); f.Spotted=false; a=DeathCauseModel.Analyze(f);
    Check(a.Seen.Contains("Ningún robot"), "Sin persecución lo dice");
    f=Seen(); f.Spotter=null; Check(DeathCauseModel.Analyze(f).Seen.Contains("un robot"), "Sin nombre del robot: «un robot»");
    f=Seen(); f.Kind=DeathKind.Hazard; a=DeathCauseModel.Analyze(f);
    Check(a.Title=="ACCIDENTE" && a.Seen.Contains("descarga") && a.Tip==DeathTip.Hazard && a.TipText.Length>0, "Descarga: título y consejo propios");
    Check(DeathCauseModel.Analyze(new DeathFacts{Kind=DeathKind.KnockedOut}).Title=="FUERA DE COMBATE", "Título de fuera de combate");
    Check(DeathCauseModel.Summary(null)=="Última caída: ninguna" && DeathCauseModel.Summary(DeathCauseModel.Analyze(Seen())).Contains("estudiar la ruta"), "Línea corta para el panel F3");
  }

  static void Main(){
    Tips(); Card();
    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
