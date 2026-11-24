// Definiciones simplificadas de IMGUI (OnGUI) para compilar el HUD de combate y el panel del director fuera de Unity.
namespace UnityEngine
{
    public enum FontStyle { Normal, Bold, Italic, BoldAndItalic }
    public class GUIStyle { public GUIStyle() {} public GUIStyle(GUIStyle o) {} public int fontSize; public FontStyle fontStyle; public TextAnchor alignment; public bool wordWrap, richText; public float CalcHeight(GUIContent c, float w) { return 20f; } }
    public class GUIContent { public GUIContent() {} public GUIContent(string t) {} }
    public class GUISkin { public GUIStyle label = new GUIStyle(), box = new GUIStyle(); }
    public static class GUI {
        public static Color color = Color.white; public static GUISkin skin = new GUISkin(); public static int depth;
        public static void Label(Rect r, string t) {} public static void Label(Rect r, string t, GUIStyle s) {}
        public static void Box(Rect r, string t) {} public static void DrawTexture(Rect r, Texture t) {}
    }
}
