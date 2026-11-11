using System;
using Nexus7.Audio; using Nexus7.Core;
static class T {
  static int fails=0; static void Check(bool c,string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }
  static bool InRange(float[] a){ foreach(var x in a) if(x<-1f||x>1f||float.IsNaN(x)) return false; return true; }
  static float Rms(float[] a){ double s=0; foreach(var x in a) s+=x*x; return (float)Math.Sqrt(s/a.Length); }
  // Empalme sin chasquido: el salto del último al primer valor no es mayor que el mayor salto entre muestras vecinas
  static bool Seamless(float[] a){ float m=0; for(int i=1;i<a.Length;i++) m=Math.Max(m,Math.Abs(a[i]-a[i-1])); return Math.Abs(a[0]-a[a.Length-1])<=m*1.05f; }
  static float Peak(float[] a){ float p=0; foreach(var x in a) p=Math.Max(p,Math.Abs(x)); return p; }
  static void Main(){
    int sr=ProceduralAudio.SampleRate;
    Check(sr==22050, "Frecuencia de muestreo 22 050 Hz");
    var menu=ProceduralAudio.MenuPad(16f);
    Check(menu.Length==16*sr && InRange(menu), "Música del menú: 16 s, muestras en [−1, 1]");
    Check(Seamless(menu), "Música del menú: el bucle cierra sin chasquido");
    Check(Rms(menu)>0.02f, "Música del menú: se oye");
    var broken=new float[menu.Length-777]; Array.Copy(menu,broken,broken.Length); Check(!Seamless(broken), "(la comprobación del empalme detecta un chasquido)");
    var ship=ProceduralAudio.ShipDrone(12f);
    Check(ship.Length==12*sr && InRange(ship) && Seamless(ship), "Zumbido de la nave: 12 s, en rango y en bucle");
    Check(Rms(ship)>0.01f && Rms(ship)<Rms(menu), "La nave suena más baja que el menú");
    foreach (UiSound k in Enum.GetValues(typeof(UiSound))) {
      var s=ProceduralAudio.Ui(k); float dur=(float)s.Length/sr;
      Check(dur>=0.03f && dur<=0.4f && InRange(s) && Math.Abs(s[0])<0.01f && Math.Abs(s[s.Length-1])<0.01f && Peak(s)>0.1f, "Sonido "+k+": "+(dur*1000).ToString("0")+" ms, empieza y acaba en silencio, se oye");
    }
    GameSettings.ResetToDefaults();
    GameSettings.Music=1f; Check(Math.Abs(AudioChannel.VolumeFor(AudioCategory.Music,0.5f)-0.5f)<1e-4f, "Música al 100 %: volumen base");
    GameSettings.Music=0f; Check(AudioChannel.VolumeFor(AudioCategory.Music,0.5f)==0f, "Música al 0 %: silencio");
    GameSettings.Sfx=0.5f; Check(Math.Abs(AudioChannel.VolumeFor(AudioCategory.Sfx,1f)-0.25f)<1e-4f, "Efectos al 50 %: volumen 0,25 (curva)");
    GameSettings.ResetToDefaults();
    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
