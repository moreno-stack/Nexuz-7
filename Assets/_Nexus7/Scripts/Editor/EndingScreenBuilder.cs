using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Nexus7.UI;
using A = Nexus7.EditorTools.AlmacenCargaBuilder;
using B = Nexus7.EditorTools.SalaInicialBuilder;
using M = Nexus7.EditorTools.MenuScreenBuilder;

namespace Nexus7.EditorTools
{
    /// <summary>
    /// Pantalla final «gráfica» (PanelResultados): fondo semitransparente con degradado y viñeta, banda de color
    /// según el final, tarjeta de evaluación con iconos y barras, tarjeta de estadísticas, insignia del rango con
    /// anillo que gira y estrellas, puntuación con «¡NUEVO RÉCORD!» y botones con tinte al pasar el ratón.
    /// Cada bloque entra animado (UIIntro) cuando el panel se activa al terminar la partida.
    /// </summary>
    internal static class EndingScreenBuilder
    {
        private static readonly Vector2 C = new Vector2(0.5f, 0.5f);
        private static readonly Color Card = new Color(0.03f, 0.06f, 0.1f, 0.86f);
        internal static readonly Color[] BarColors = { new Color(0.4f, 1f, 0.7f), new Color(1f, 0.8f, 0.3f), new Color(0.4f, 0.8f, 1f), new Color(0.85f, 0.5f, 1f) };

        private static Image CardRect(string name, Transform parent, Vector2 pos, Vector2 size, Color accent)
        {
            var img = M.Rect(name, parent, Card, C, pos, size);
            M.Rect("Acento", img.transform, accent, new Vector2(0.5f, 1f), new Vector2(0f, -1.5f), new Vector2(size.x, 3f));
            return img;
        }

        internal static void Build(Transform canvas)
        {
            // ---------- Fondo: la última imagen de la cinemática se ve detrás ----------
            var panel = B.UIImage("PanelResultados", canvas, new Color(0.01f, 0.02f, 0.05f, 0.62f));
            B.Anchor(panel.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var p = panel.transform;
            var shade = M.FullScreen("DegradadoInferior", p, new Color(0.01f, 0.02f, 0.05f, 0.9f), UIShapeKind.Gradient);
            shade.rectTransform.localEulerAngles = new Vector3(0f, 0f, 180f);   // opaco abajo, donde van los textos largos
            M.FullScreen("Vineta", p, Color.black, UIShapeKind.Vignette);

            // ---------- Cabecera con la banda de color del final ----------
            var head = M.Group("BloqueCabecera", p, 0f, new Vector2(0f, 40f), 0.7f);
            var band = B.UIImage("BandaCabecera", head, new Color(0.4f, 1f, 0.69f, 0.16f));
            B.Anchor(band.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -100f), new Vector2(0f, 200f));
            var line = B.UIImage("LineaCabecera", head, new Color(0.4f, 1f, 0.69f, 1f));
            B.Anchor(line.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -200f), new Vector2(0f, 4f));
            var tagBg = M.Rect("EtiquetaFinal", head, new Color(0.4f, 1f, 0.69f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(260f, 34f));
            var tag = B.UIText("TextoEtiqueta", tagBg.transform, C, Vector2.zero, new Vector2(260f, 34f), 20f, TextAlignmentOptions.Center, new Color(0.02f, 0.03f, 0.05f));
            var title = B.UIText("Titulo", head, new Vector2(0.5f, 1f), new Vector2(0f, -52f), new Vector2(1700f, 140f), 64f, TextAlignmentOptions.Top, Color.white);
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            var story = B.UIText("Narracion", head, new Vector2(0.5f, 1f), new Vector2(0f, -212f), new Vector2(1500f, 80f), 24f, TextAlignmentOptions.Top, new Color(0.78f, 0.82f, 0.88f));
            story.rectTransform.pivot = new Vector2(0.5f, 1f);
            story.fontStyle = FontStyles.Italic;

            // ---------- Evaluación: tarjeta con un icono y una barra por categoría ----------
            var evalGroup = M.Group("BloqueEvaluacion", p, 0.25f, new Vector2(-80f, 0f));
            CardRect("TarjetaEvaluacion", evalGroup, new Vector2(-470f, 0f), new Vector2(860f, 470f), new Color(0.6f, 0.9f, 1f, 0.9f));
            B.UIText("CabeceraEvaluacion", evalGroup, C, new Vector2(-430f, 205f), new Vector2(760f, 40f), 28f, TextAlignmentOptions.Left, new Color(0.6f, 0.9f, 1f)).text = "<b>EVALUACIÓN</b>";
            var labels = new TextMeshProUGUI[4];
            var bars = new Image[4];
            for (int i = 0; i < 4; i++)
            {
                float y = 145f - i * 72f;
                var icon = M.Shape("Icono" + i, evalGroup, UIShapeKind.Circle, BarColors[i], C, new Vector2(-860f, y - 12f), new Vector2(46f, 46f));
                B.UIText("Letra", icon.transform, C, Vector2.zero, new Vector2(46f, 46f), 26f, TextAlignmentOptions.Center, new Color(0.02f, 0.03f, 0.05f)).text =
                    $"<b>{EndingUI.CategoryInitials[i]}</b>";
                labels[i] = B.UIText("Categoria" + i, evalGroup, C, new Vector2(-405f, y), new Vector2(740f, 36f), 26f, TextAlignmentOptions.Left, Color.white);
                var bg = B.UIImage("Barra" + i, evalGroup, new Color(1f, 1f, 1f, 0.08f));
                bg.rectTransform.pivot = new Vector2(0f, 0.5f);
                B.Anchor(bg.rectTransform, C, C, new Vector2(-775f, y - 30f), new Vector2(740f, 14f));
                var fill = B.UIImage("Relleno", bg.transform, BarColors[i]);
                fill.rectTransform.pivot = new Vector2(0f, 0.5f);
                fill.rectTransform.anchorMin = Vector2.zero;
                fill.rectTransform.anchorMax = Vector2.one;
                fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
                fill.rectTransform.localScale = new Vector3(0f, 1f, 1f);
                M.FullScreen("Brillo", fill.transform, new Color(1f, 1f, 1f, 0.35f), UIShapeKind.Gradient);   // reflejo arriba de la barra
                bars[i] = fill;
            }
            // Hasta 6 líneas (penalizaciones + 4 logros): se encogen solas para caber bajo la última barra
            var breakdown = B.UIText("Destacados", evalGroup, C, new Vector2(-470f, -168f), new Vector2(780f, 118f), 22f, TextAlignmentOptions.TopLeft, Color.white);
            breakdown.enableAutoSizing = true; breakdown.fontSizeMax = 22f; breakdown.fontSizeMin = 14f;

            // ---------- Estadísticas ----------
            var statsGroup = M.Group("BloqueEstadisticas", p, 0.4f, new Vector2(80f, 0f));
            CardRect("TarjetaEstadisticas", statsGroup, new Vector2(440f, 50f), new Vector2(860f, 370f), new Color(1f, 0.83f, 0.35f, 0.9f));
            var stats = B.UIText("Estadisticas", statsGroup, C, new Vector2(440f, 40f), new Vector2(800f, 340f), 22f, TextAlignmentOptions.TopLeft, Color.white);

            // ---------- Puntuación ----------
            var scoreGroup = M.Group("BloquePuntuacion", p, 0.5f, new Vector2(0f, -50f));
            CardRect("TarjetaPuntuacion", scoreGroup, new Vector2(270f, -235f), new Vector2(500f, 130f), new Color(1f, 1f, 1f, 0.6f));
            var total = B.UIText("PuntuacionTotal", scoreGroup, C, new Vector2(270f, -235f), new Vector2(480f, 120f), 46f, TextAlignmentOptions.Center, Color.white);
            var record = M.Rect("NuevoRecord", scoreGroup, new Color(1f, 0.88f, 0.29f, 0.95f), C, new Vector2(270f, -150f), new Vector2(260f, 34f));
            B.UIText("TextoRecord", record.transform, C, Vector2.zero, new Vector2(260f, 34f), 20f, TextAlignmentOptions.Center, new Color(0.15f, 0.08f, 0f)).text = "<b>¡NUEVO RÉCORD!</b>";
            var rp = record.gameObject.AddComponent<UIPulse>();
            A.SetFloat(rp, "speed", 1.4f); A.SetFloat(rp, "minAlpha", 0.75f); A.SetFloat(rp, "scaleAmount", 0.06f);

            // ---------- Insignia del rango: anillo que gira, disco, letra y estrellas ----------
            var badge = M.Group("BloqueRango", p, 0.65f, new Vector2(0f, -60f), 0.7f);
            M.Shape("HaloRango", badge, UIShapeKind.Glow, new Color(1f, 1f, 1f, 0.18f), C, new Vector2(760f, -250f), new Vector2(360f, 360f));
            var ring = M.Shape("AnilloRango", badge, UIShapeKind.Ring, Color.white, C, new Vector2(760f, -250f), new Vector2(230f, 230f));
            var spin = ring.gameObject.AddComponent<UISpin>();
            A.SetFloat(spin, "degreesPerSecond", 30f);
            // marcas en el anillo para que se note el giro
            for (int k = 0; k < 4; k++)
            {
                var mark = M.Rect("Marca" + k, ring.transform, new Color(0.02f, 0.03f, 0.05f, 1f), C, Vector2.zero, new Vector2(14f, 230f));
                mark.rectTransform.localEulerAngles = new Vector3(0f, 0f, k * 45f);
            }
            M.Shape("FondoRango", badge, UIShapeKind.Circle, new Color(0.03f, 0.05f, 0.08f, 0.97f), C, new Vector2(760f, -250f), new Vector2(196f, 196f));
            var grade = B.UIText("Rango", badge, C, new Vector2(760f, -240f), new Vector2(200f, 170f), 130f, TextAlignmentOptions.Center, Color.white);
            B.UIText("EtiquetaRango", badge, C, new Vector2(760f, -310f), new Vector2(200f, 30f), 18f, TextAlignmentOptions.Center, new Color(0.6f, 0.65f, 0.7f)).text = "RANGO";
            var stars = new Image[3];
            for (int s = 0; s < 3; s++)
                stars[s] = M.Shape("Estrella" + s, badge, UIShapeKind.Star, EndingUI.StarOff, C, new Vector2(700f + s * 60f, -400f), new Vector2(50f, 50f));

            // ---------- Botones ----------
            var btnGroup = M.Group("BloqueBotones", p, 0.9f, new Vector2(0f, -40f));
            var restart = M.MenuButton("BotonReiniciar", btnGroup, new Vector2(-230f, -450f), new Vector2(420f, 70f), "REINICIAR PARTIDA", "R", new Color(0.06f, 0.32f, 0.45f, 0.95f), M.Cyan);
            var menu = M.MenuButton("BotonMenu", btnGroup, new Vector2(230f, -450f), new Vector2(420f, 70f), "MENÚ PRINCIPAL", "M", new Color(0.16f, 0.18f, 0.22f, 0.95f), new Color(0.75f, 0.8f, 0.85f));

            var ui = canvas.gameObject.AddComponent<EndingUI>();
            B.SetRef(ui, "panel", panel.gameObject);
            B.SetRef(ui, "titleText", title);
            B.SetRef(ui, "storyText", story);
            B.SetRef(ui, "breakdownText", breakdown);
            B.SetRef(ui, "statsText", stats);
            B.SetRef(ui, "totalText", total);
            B.SetRef(ui, "gradeText", grade);
            A.SetObjectArray(ui, "categoryLabels", labels.Cast<Object>().ToArray());
            A.SetObjectArray(ui, "categoryBars", bars.Cast<Object>().ToArray());
            B.SetRef(ui, "restartButton", restart);
            B.SetRef(ui, "menuButton", menu);
            B.SetRef(ui, "headerBand", band);
            B.SetRef(ui, "headerLine", line);
            B.SetRef(ui, "endingTagBackground", tagBg);
            B.SetRef(ui, "endingTagText", tag);
            B.SetRef(ui, "gradeRing", ring);
            A.SetObjectArray(ui, "stars", stars.Cast<Object>().ToArray());
            B.SetRef(ui, "recordBanner", record.gameObject);
            panel.gameObject.SetActive(false);
        }
    }
}
