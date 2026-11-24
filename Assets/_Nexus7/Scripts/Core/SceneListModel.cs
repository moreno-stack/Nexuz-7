using System.Collections.Generic;

namespace Nexus7.Core
{
    /// <summary>
    /// Lista de escenas del juego (pura, probada en SceneListTest). El menú va el primero (índice 0, por el que arranca el
    /// juego compilado) y después las 6 fases en orden. Lo usa el registro automático del editor (PhaseScenesRegistrar):
    /// si falta alguna fase ya construida en Build Settings, se vuelven a ordenar; si no falta ninguna, no se toca nada.
    /// </summary>
    public static class SceneListModel
    {
        public const string ScenesFolder = "Assets/_Nexus7/Scenes/";

        /// <summary>Rutas de las escenas del juego, en orden: menú y Fases 1–6.</summary>
        public static List<string> GamePaths()
        {
            var list = new List<string> { ScenesFolder + GameFlow.MenuScene + ".unity" };
            for (int i = 0; i < GameFlow.Phases.GetLength(0); i++) list.Add(ScenesFolder + GameFlow.Phases[i, 0] + ".unity");
            return list;
        }

        /// <summary>
        /// Nueva lista para Build Settings, o null si no hace falta cambiar nada.
        /// 'current': lo que hay ahora · 'built': escenas del juego que existen en disco (en el orden de GamePaths).
        /// Resultado: las del juego en orden y detrás las demás que ya hubiera (sin repetir).
        /// </summary>
        public static List<string> Merge(IList<string> current, IList<string> built)
        {
            bool missing = false;
            foreach (var b in built) if (!current.Contains(b)) { missing = true; break; }
            if (!missing) return null;
            var result = new List<string>(built);
            foreach (var c in current) if (!string.IsNullOrEmpty(c) && !result.Contains(c)) result.Add(c);
            return result;
        }

        /// <summary>Título de la fase que falta para el mensaje de la salida («Fase 3 · Sala de energía»).</summary>
        public static string MissingSceneMessage(string scene) =>
            $"Falta «{GameFlow.PhaseTitle(scene)}» en la lista de escenas. Para el juego y usa Nexus-7 → Construir todo el juego.";
    }
}
