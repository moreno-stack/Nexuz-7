using System;
using System.Collections.Generic;
using Nexus7.Core;
static class T {
  static int fails=0; static void Check(bool c,string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }
  const string F = SceneListModel.ScenesFolder;

  static void Main(){
    var all = SceneListModel.GamePaths();
    Check(all.Count==7 && all[0]==F+"MenuPrincipal.unity" && all[1]==F+"SalaInicial.unity" && all[6]==F+"Evacuacion.unity", "Menú primero y las 6 fases en orden");
    Check(SceneListModel.Merge(new List<string>(all), all)==null, "Si están todas, no toca nada");
    var onlyOne = SceneListModel.Merge(new List<string>{ F+"AlmacenCarga.unity" }, all);
    Check(onlyOne!=null && onlyOne.Count==7 && onlyOne[0]==F+"MenuPrincipal.unity" && onlyOne[2]==F+"AlmacenCarga.unity", "Con solo una fase registrada, añade las demás en orden");
    var extra = SceneListModel.Merge(new List<string>{ "Assets/Otra.unity", F+"SalaInicial.unity" }, all);
    Check(extra.Count==8 && extra[7]=="Assets/Otra.unity", "Conserva las escenas ajenas al final");
    var partialBuilt = new List<string>{ F+"MenuPrincipal.unity", F+"SalaInicial.unity", F+"AlmacenCarga.unity" };
    var p = SceneListModel.Merge(new List<string>(), partialBuilt);
    Check(p.Count==3, "Solo registra las escenas que existen en disco");
    var same = SceneListModel.Merge(new List<string>{ F+"AlmacenCarga.unity", F+"MenuPrincipal.unity", F+"SalaInicial.unity" }, partialBuilt);
    Check(same==null, "No reordena la lista del usuario si no falta ninguna");
    Check(SceneListModel.MissingSceneMessage("SalaEnergia").Contains("Fase 3 · Sala de energía") && SceneListModel.MissingSceneMessage("SalaEnergia").Contains("Construir todo el juego"), "El aviso dice qué fase falta y cómo arreglarlo");
    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
