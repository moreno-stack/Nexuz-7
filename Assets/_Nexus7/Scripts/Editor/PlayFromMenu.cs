using UnityEditor;
using UnityEditor.SceneManagement;

namespace Nexus7.EditorTools
{
    /// <summary>
    /// En el editor, Play arranca la escena que esté abierta. Con esto, Play empieza SIEMPRE por "MenuPrincipal"
    /// (como el juego compilado, que empieza por el índice 0 de Build Settings).
    /// Interruptor: Nexus-7 → Empezar Play por el menú (desmárcalo para probar una sala suelta).
    /// </summary>
    [InitializeOnLoad]
    public static class PlayFromMenu
    {
        private const string MenuPath = "Nexus-7/Empezar Play por el menú";
        private const string PrefsKey = "Nexus7.PlayFromMenu";

        public static bool Enabled
        {
            get => EditorPrefs.GetBool(PrefsKey, true);
            set { EditorPrefs.SetBool(PrefsKey, value); Apply(); }
        }

        static PlayFromMenu() => EditorApplication.delayCall += Apply;

        /// <summary>Asigna (o quita) la escena de arranque de Play según el interruptor.</summary>
        public static void Apply()
        {
            var menu = AssetDatabase.LoadAssetAtPath<SceneAsset>(BuildAllScenes.MenuScenePath);
            EditorSceneManager.playModeStartScene = Enabled && menu != null ? menu : null;
        }

        [MenuItem(MenuPath, false, 1)]
        private static void Toggle() => Enabled = !Enabled;

        [MenuItem(MenuPath, true)]
        private static bool ToggleValidate()
        {
            Menu.SetChecked(MenuPath, Enabled);
            return true;
        }
    }
}
