using System;
using Nexus7.UI;
static class T {
  static int fails=0; static void Check(bool c,string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }
  static void Main(){
    Check(!MenuState.ContinueEnabled(false) && MenuState.ContinueEnabled(true), "Continuar solo con partida guardada");
    Check(MenuState.NeedsConfirm(true) && !MenuState.NeedsConfirm(false), "Nueva partida pide confirmar solo si hay partida guardada");
    Check(MenuState.AcceptsMainInput(false,false), "Sin paneles abiertos: [Intro] y ←/→ funcionan");
    Check(!MenuState.AcceptsMainInput(true,false) && !MenuState.AcceptsMainInput(false,true), "Con Configuración o la confirmación abiertas, el menú de fondo no responde");
    Check(MenuState.ToPercent(0.8f)==80 && MenuState.ToPercent(1.2f)==100 && MenuState.ToPercent(-1f)==0 && MenuState.ToPercent(0.555f)==56, "0–1 → 0–100 %");
    Check(Math.Abs(MenuState.FromPercent(35f)-0.35f)<1e-5f && MenuState.FromPercent(150f)==1f && MenuState.FromPercent(-5f)==0f, "0–100 % → 0–1");
    Check(MenuState.FullscreenLabel(true)=="PANTALLA COMPLETA: SÍ" && MenuState.FullscreenLabel(false)=="PANTALLA COMPLETA: NO", "Texto del botón de pantalla completa");
    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
