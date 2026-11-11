using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Nexus7.EditorTools
{
    /// <summary>
    /// Construye todo el juego de una vez (Fases 1 a 6 y el menú principal) y abre el menú listo para pulsar Play.
    /// Al abrir el proyecto por primera vez, si las escenas aún no existen, ofrece construirlas.
    /// </summary>
    [InitializeOnLoad]
    public static class BuildAllScenes
    {
        internal const string MenuScenePath = "Assets/_Nexus7/Scenes/MenuPrincipal.unity";
        private const string AskedKey = "Nexus7.BuildAllAsked";

        static BuildAllScenes()
        {
            EditorApplication.delayCall += OfferOnFirstOpen;
        }

        private static void OfferOnFirstOpen()
        {
            if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (File.Exists(MenuScenePath) || SessionState.GetBool(AskedKey, false)) return;
            SessionState.SetBool(AskedKey, true);
            if (EditorUtility.DisplayDialog("Escape Espacial: Nexus-7",
                    "Las escenas del juego todavía no están construidas.\n\n¿Construir ahora las 6 fases y el menú principal? Tarda uno o dos minutos.",
                    "Construir", "Más tarde"))
                Build();
        }

        [MenuItem("Nexus-7/Construir todo el juego (Fases 1–6 y menú)", false, 0)]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string[] names = { "Fase 1: Sala inicial", "Fase 2: Almacén de carga", "Fase 3: Sala de energía",
                               "Fase 4: Laboratorio", "Fase 5: Centro de control", "Fase 6: Evacuación y menú principal" };
            System.Action[] builds = { SalaInicialBuilder.Build, AlmacenCargaBuilder.Build, SalaEnergiaBuilder.Build,
                                       LaboratorioBuilder.Build, CentroControlBuilder.Build, EvacuacionBuilder.Build };
            // Cada fase por separado: si una falla, se registra y se siguen construyendo las demás
            var failed = new System.Collections.Generic.List<string>();
            try
            {
                for (int i = 0; i < builds.Length; i++)
                {
                    EditorUtility.DisplayProgressBar("Nexus-7", "Construyendo " + names[i] + "...", (float)i / builds.Length);
                    try { builds[i](); }
                    catch (System.Exception e)
                    {
                        Debug.LogException(e);
                        failed.Add(names[i]);
                    }
                }
                EditorUtility.DisplayProgressBar("Nexus-7", "Catálogo de objetos y Build Settings...", 1f);
                ItemCatalogBuilder.Build();                   // objetos por Id para la partida guardada
                EvacuacionBuilder.ConfigureBuildSettings();   // menú principal en el índice 0
            }
            finally { EditorUtility.ClearProgressBar(); }

            if (File.Exists(MenuScenePath)) EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
            PlayFromMenu.Apply();
            string report = Nexus7.Core.BuildReport.Describe(failed.ToArray());
            if (failed.Count == 0) Debug.Log("<color=cyan>[Nexus-7]</color> " + report + " Escena abierta: MenuPrincipal. Pulsa Play.");
            else Debug.LogError("[Nexus-7] " + report);
            if (!Application.isBatchMode) EditorUtility.DisplayDialog("Escape Espacial: Nexus-7", report, "Aceptar");
        }
    }
}
