using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Nexus7.Core;

namespace Nexus7.EditorTools
{
    /// <summary>
    /// Mantiene el menú y las 6 fases en Build Settings. Sin ellas, el montacargas de una fase no puede cargar la siguiente
    /// (Unity solo carga escenas de esa lista) y el juego se queda en «FIN DE LA FASE».
    /// Pasa al abrir el proyecto o al recompilar (por ejemplo, tras un git pull) y con Nexus-7 → Registrar las fases.
    /// Solo añade las escenas que ya existen en disco y no toca la lista si no falta ninguna (SceneListModel).
    /// </summary>
    [InitializeOnLoad]
    public static class PhaseScenesRegistrar
    {
        static PhaseScenesRegistrar() => EditorApplication.delayCall += () => Register(false);

        [MenuItem("Nexus-7/Registrar las fases en Build Settings", false, 2)]
        private static void RegisterFromMenu() => Register(true);

        public static void Register(bool verbose)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var built = new List<string>();
            foreach (var path in SceneListModel.GamePaths()) if (File.Exists(path)) built.Add(path);

            var current = new List<string>();
            if (EditorBuildSettings.scenes != null) foreach (var s in EditorBuildSettings.scenes) current.Add(s.path);

            var merged = SceneListModel.Merge(current, built);
            if (merged == null)
            {
                if (verbose) Debug.Log($"[Nexus-7] Build Settings ya tiene las {built.Count} escenas construidas del juego.");
                return;
            }
            var scenes = new List<EditorBuildSettingsScene>();
            foreach (var path in merged) scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log($"[Nexus-7] Escenas del juego registradas en Build Settings ({built.Count} de {SceneListModel.GamePaths().Count}). " +
                      (built.Count < SceneListModel.GamePaths().Count ? "Faltan fases por construir: Nexus-7 → Construir todo el juego." : ""));
        }
    }
}
