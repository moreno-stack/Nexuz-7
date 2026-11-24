using System; using System.IO;
// Los constructores de fase deben crear la escena nueva ANTES de cargar los objetos (ItemData, recetas...).
// NewScene(Single) descarga los assets que no están en uso: si los objetos se cargan antes, sus referencias
// quedan en null en la escena guardada ("objeto imprescindible sin asignar") y las puertas, el generador y
// los bancos de trabajo piden objetos que nunca aparecen.
static class T {
  static int fails=0; static void Check(bool c,string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }
  static string BuildBody(string file){
    string src = File.ReadAllText(file);
    int start = src.IndexOf("public static void Build()");
    if (start < 0) return "";
    int next = src.IndexOf("\n        }", start);   // fin del método (sangría de 8 espacios)
    return next < 0 ? src.Substring(start) : src.Substring(start, next - start);
  }
  static void Main(){
    string dir = "Assets/_Nexus7/Scripts/Editor/";
    foreach (var f in new[]{ "AlmacenCargaBuilder.cs", "SalaEnergiaBuilder.cs", "LaboratorioBuilder.cs", "CentroControlBuilder.cs", "EvacuacionBuilder.cs", "SalaInicialBuilder.cs" }){
      string body = BuildBody(dir + f);
      int scene = body.IndexOf("EditorSceneManager.NewScene(");
      Check(scene >= 0, f + ": crea una escena nueva en Build()");
      foreach (var load in new[]{ "EnsureItems(", "EnsurePhase4Data(", "CreateItem(", "CreateDevice(" }){
        int at = body.IndexOf(load);
        if (at >= 0) Check(scene >= 0 && scene < at, f + ": " + load.TrimEnd('(') + " va después de NewScene (si no, sus referencias se guardan vacías)");
      }
    }
    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
