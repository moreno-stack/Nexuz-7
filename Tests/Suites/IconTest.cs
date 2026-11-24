using System; using System.Linq;
using Nexus7.Visual.Models; using Nexus7.Inventory; using Nexus7.Devices; using Nexus7.Combat;
static class T {
  static int fails=0; static void Check(bool c,string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }
  const int S = 64;
  static float A(float[] px, int x, int y){ return px[(y*S+x)*4+3]; }
  static float Coverage(float[] px){ int n=0; for (int i=3;i<px.Length;i+=4) if (px[i] > 0.5f) n++; return n/(float)(S*S); }
  static bool HasOutline(float[] px){
    for (int y=1;y<S-1;y++) for (int x=1;x<S-1;x++){
      int i=(y*S+x)*4; if (px[i+3] < 0.99f) continue;
      float lum = 0.3f*px[i]+0.59f*px[i+1]+0.11f*px[i+2];
      bool edge = A(px,x+1,y)<0.01f || A(px,x-1,y)<0.01f || A(px,x,y+1)<0.01f || A(px,x,y-1)<0.01f;
      if (edge && lum < 0.12f) return true;
    }
    return false;
  }
  static bool HasGreen(float[] px){ for (int i=0;i<px.Length;i+=4) if (px[i+3]>0.5f && px[i+1] > px[i]+0.2f && px[i+1] > px[i+2]+0.2f) return true; return false; }
  static float Diff(float[] a, float[] b){ double d=0; for (int i=0;i<a.Length;i++) d+=Math.Abs(a[i]-b[i]); return (float)(d/a.Length); }
  static void Main(){
    var green = new MColor(0.55f,1f,0.35f);
    var battery = ModelLibrary.Item("battery_ion", ItemCategory.Battery, green);
    var px = IconRasterizer.Render(battery, S);
    Check(px.Length == S*S*4, "El icono tiene S×S píxeles RGBA");
    Check(A(px,0,0)==0 && A(px,S-1,0)==0 && A(px,0,S-1)==0 && A(px,S-1,S-1)==0, "Las esquinas son transparentes");
    float cov = Coverage(px);
    Check(cov > 0.15f && cov < 0.85f, "El objeto llena el icono sin salirse (" + (cov*100).ToString("0") + " %)");
    Check(HasOutline(px), "Tiene contorno oscuro");
    Check(HasGreen(px), "Aparece el color de la batería");
    Check(px.All(v => v >= 0f && v <= 1f), "Todos los valores entre 0 y 1");
    Check(Diff(px, IconRasterizer.Render(battery, S)) == 0f, "Mismo plano → mismo icono (determinista)");
    var wrench = ModelLibrary.Item("tool_hydraulic_wrench", ItemCategory.Tool, new MColor(1f,0.65f,0.2f));
    Check(Diff(px, IconRasterizer.Render(wrench, S)) > 0.02f, "La batería y la llave tienen iconos distintos");
    // Objetos planos y alargados también se encuadran bien
    foreach (var b in new[]{ ModelLibrary.Item("key_director_card", ItemCategory.Tool, green), ModelLibrary.Weapon(PlayerWeaponCatalog.Railgun, green, null), ModelLibrary.Device(DeviceKind.EmpPulse, green) }){
      float c = Coverage(IconRasterizer.Render(b, S));
      Check(c > 0.1f && c < 0.85f, b.Key + ": cobertura razonable (" + (c*100).ToString("0") + " %)");
    }
    var empty = IconRasterizer.Render(new ModelBlueprint("vacio", green), S);
    Check(empty.Length == S*S*4 && Coverage(empty) == 0f, "Plano vacío → icono transparente (sin errores)");
    Check(IconRasterizer.Render(battery, 32).Length == 32*32*4, "Acepta otros tamaños");
    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
