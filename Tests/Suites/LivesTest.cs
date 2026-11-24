using System;
using Nexus7.Gameplay;
static class T {
  static int fails=0; static void Check(bool c,string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }
  static void Lives(){
    Check(LivesModel.MaxFor(0)==5 && LivesModel.MaxFor(1)==3 && LivesModel.MaxFor(2)==2, "5 vidas en Fácil, 3 en Normal y 2 en Difícil");
    var m = new LivesModel(1); m.EnterScene("AlmacenCarga", 1);
    Check(m.Lives==3 && !m.IsLastLife, "Empieza la fase con las vidas llenas");
    Check(!m.LoseLife() && !m.LoseLife() && m.IsLastLife, "Dos caídas: última vida");
    Check(m.GainLife() && m.Lives==2, "Despejar un encuentro devuelve una vida");
    m.GainLife(); Check(!m.GainLife() && m.Lives==3, "Nunca más del máximo");
    m.LoseLife(); m.LoseLife(); Check(m.LoseLife() && m.Lives==0, "Sin vidas: hay que reiniciar la fase");
    m.RestartPhase(); Check(m.Lives==3 && m.RestartsIn("AlmacenCarga")==1 && m.RestartsIn("SalaEnergia")==0, "Reiniciar: vidas llenas y se cuenta el reinicio de esa fase");
    m.LoseLife(); m.EnterScene("AlmacenCarga", 1); Check(m.Lives==2, "Recargar la misma fase no rellena las vidas");
    m.EnterScene("SalaEnergia", 1); Check(m.Lives==3, "Una fase nueva rellena las vidas");
    m.EnterScene("SalaEnergia", 2); Check(m.Max==2, "El máximo sigue a la dificultad");
  }
  static void Main(){
    Lives();
    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
