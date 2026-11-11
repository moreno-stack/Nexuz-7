using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Nexus7.Core;
using Nexus7.Environment;
using Nexus7.UI;
using A = Nexus7.EditorTools.AlmacenCargaBuilder;
using B = Nexus7.EditorTools.SalaInicialBuilder;

namespace Nexus7.EditorTools
{
    /// <summary>
    /// Menú principal «gráfico»: fondo 3D (nave Nexus-7 girando, escombros, cámara con vaivén) y la interfaz
    /// (nebulosas, halo del planeta, viñeta, líneas de escaneo, panel lateral, tarjetas de dificultad y récord).
    /// Lo usa EvacuacionBuilder.BuildMainMenuScene.
    /// </summary>
    internal static class MenuScreenBuilder
    {
        // Posición de la nave: a la derecha del encuadre de la cámara del menú (que mira a 20° de guiñada)
        internal static readonly Vector3 ShipPosition = new Vector3(24f, 5f, 40f);

        // =====================================================================
        //  FONDO 3D
        // =====================================================================

        internal static void BuildBackdrop(Transform space, Camera cam)
        {
            cam.gameObject.AddComponent<MenuCameraDrift>();
            BuildShip(space);
            BuildDebris(space);
        }

        /// <summary>La nave Nexus-7: casco, puente, anillo habitable con radios, módulos, paneles, motores encendidos y luces de navegación.</summary>
        internal static GameObject BuildShip(Transform parent)
        {
            var hull   = B.CreateMat("MM_Casco", new Color(0.62f, 0.65f, 0.7f), null, 0.55f, 0.7f);
            var dark   = B.CreateMat("MM_CascoOscuro", new Color(0.16f, 0.17f, 0.2f), null, 0.5f, 0.8f);
            var accent = B.CreateMat("MM_FranjaNaranja", new Color(0.9f, 0.45f, 0.12f), new Color(0.9f, 0.4f, 0.1f) * 0.6f, 0.4f, 0.3f);
            var window = B.CreateMat("MM_Ventanas", new Color(0.2f, 0.6f, 0.8f), new Color(0.35f, 0.85f, 1f) * 2.5f, 0.9f, 0f);
            var solar  = B.CreateMat("MM_PanelSolar", new Color(0.05f, 0.1f, 0.25f), new Color(0.05f, 0.15f, 0.4f) * 0.8f, 0.9f, 0.5f);
            var flame  = B.CreateMat("MM_Propulsor", new Color(1f, 0.6f, 0.2f), new Color(1f, 0.5f, 0.12f) * 5f, 0f, 0f);
            var red    = B.CreateMat("MM_LuzRoja", new Color(1f, 0.1f, 0.05f), new Color(1f, 0.1f, 0.05f) * 6f, 0f, 0f);
            var green  = B.CreateMat("MM_LuzVerde", new Color(0.1f, 1f, 0.3f), new Color(0.1f, 1f, 0.3f) * 6f, 0f, 0f);
            var white  = B.CreateMat("MM_LuzBlanca", Color.white, Color.white * 5f, 0f, 0f);

            var root = new GameObject("NaveNexus7");
            root.transform.SetParent(parent);
            root.transform.SetPositionAndRotation(ShipPosition, Quaternion.Euler(12f, -110f, 0f));
            var drift = root.AddComponent<DebrisDrift>();
            A.SetVector3(drift, "velocity", Vector3.zero);
            A.SetVector3(drift, "spin", new Vector3(0f, 3f, 0f));   // gira despacio sobre sí misma
            var t = root.transform;

            // Casco principal (16 m a lo largo de Z) y puente delantero con ventanal
            Part(PrimitiveType.Capsule, "Casco", t, new Vector3(0f, 0f, 0f), new Vector3(90f, 0f, 0f), new Vector3(3f, 8f, 3f), hull);
            Part(PrimitiveType.Sphere, "Puente", t, new Vector3(0f, 0.9f, 6.2f), Vector3.zero, new Vector3(2.6f, 1.8f, 3.6f), hull);
            Part(PrimitiveType.Cube, "Ventanal", t, new Vector3(0f, 1.25f, 7.35f), new Vector3(20f, 0f, 0f), new Vector3(1.6f, 0.35f, 0.9f), window);
            Part(PrimitiveType.Cube, "FranjaCasco", t, new Vector3(0f, 0f, 2.5f), Vector3.zero, new Vector3(3.1f, 0.25f, 0.6f), accent);
            for (int i = 0; i < 6; i++)   // ventanas a los dos lados
            {
                float z = -4f + i * 1.6f;
                Part(PrimitiveType.Cube, "Ventana", t, new Vector3(1.47f, 0.35f, z), Vector3.zero, new Vector3(0.08f, 0.22f, 0.7f), window);
                Part(PrimitiveType.Cube, "Ventana", t, new Vector3(-1.47f, 0.35f, z), Vector3.zero, new Vector3(0.08f, 0.22f, 0.7f), window);
            }

            // Anillo habitable: 18 segmentos alrededor del casco + 4 radios
            const int seg = 18; const float ringR = 6.5f;
            for (int i = 0; i < seg; i++)
            {
                float a = i * 360f / seg;
                var p = Quaternion.Euler(0f, 0f, a) * new Vector3(ringR, 0f, 0f) + new Vector3(0f, 0f, -1.5f);
                Part(PrimitiveType.Cube, "AnilloSegmento", t, p, new Vector3(0f, 0f, a), new Vector3(0.9f, 2.35f, 1.6f), i % 3 == 0 ? dark : hull);
                if (i % 2 == 0) Part(PrimitiveType.Cube, "AnilloLuz", t, Quaternion.Euler(0f, 0f, a) * new Vector3(ringR + 0.46f, 0f, 0f) + new Vector3(0f, 0f, -1.5f),
                                     new Vector3(0f, 0f, a), new Vector3(0.05f, 0.9f, 0.5f), window);
            }
            for (int i = 0; i < 4; i++)
                Part(PrimitiveType.Cube, "Radio", t, Quaternion.Euler(0f, 0f, 45f + i * 90f) * new Vector3(ringR / 2f, 0f, 0f) + new Vector3(0f, 0f, -1.5f),
                     new Vector3(0f, 0f, 45f + i * 90f), new Vector3(ringR - 1f, 0.35f, 0.35f), dark);

            // Módulos y paneles solares
            Part(PrimitiveType.Cube, "ModuloCarga", t, new Vector3(0f, -1.6f, -4.5f), Vector3.zero, new Vector3(2.2f, 1.2f, 3f), dark);
            Part(PrimitiveType.Cube, "ModuloSensores", t, new Vector3(0f, 1.7f, -3.5f), Vector3.zero, new Vector3(1.2f, 0.8f, 2f), hull);
            Part(PrimitiveType.Cube, "PanelSolarIzq", t, new Vector3(-4.2f, 0f, -7f), Vector3.zero, new Vector3(5.5f, 0.08f, 1.8f), solar);
            Part(PrimitiveType.Cube, "PanelSolarDer", t, new Vector3(4.2f, 0f, -7f), Vector3.zero, new Vector3(5.5f, 0.08f, 1.8f), solar);
            Part(PrimitiveType.Cylinder, "Antena", t, new Vector3(0f, 2.9f, -3.2f), Vector3.zero, new Vector3(0.06f, 0.8f, 0.06f), dark);
            Part(PrimitiveType.Sphere, "AntenaPunta", t, new Vector3(0f, 3.7f, -3.2f), Vector3.zero, new Vector3(0.18f, 0.18f, 0.18f), white);

            // Motores con la llama encendida
            for (int i = 0; i < 3; i++)
            {
                var off = Quaternion.Euler(0f, 0f, 90f + i * 120f) * new Vector3(1.05f, 0f, 0f);
                Part(PrimitiveType.Cylinder, "Motor", t, off + new Vector3(0f, 0f, -8.6f), new Vector3(90f, 0f, 0f), new Vector3(1f, 0.9f, 1f), dark);
                Part(PrimitiveType.Sphere, "Llama", t, off + new Vector3(0f, 0f, -10f), Vector3.zero, new Vector3(0.8f, 0.8f, 2.2f), flame);
            }
            A.PointLight("LuzPropulsores", t, new Vector3(0f, 0f, -11f), new Color(1f, 0.55f, 0.2f), 14f, 3f, false);

            // Luces de navegación que parpadean (roja a babor, verde a estribor, blanca en la cola)
            NavLight("LuzNavegacionRoja", t, Quaternion.Euler(0f, 0f, 180f) * new Vector3(ringR + 0.5f, 0f, 0f) + new Vector3(0f, 0f, -1.5f), red, new Color(1f, 0.15f, 0.1f), 0f);
            NavLight("LuzNavegacionVerde", t, new Vector3(ringR + 0.5f, 0f, -1.5f), green, new Color(0.2f, 1f, 0.35f), 0.5f);
            NavLight("LuzNavegacionBlanca", t, new Vector3(0f, 1.6f, -8.2f), white, Color.white, 0.25f);
            return root;
        }

        private static GameObject Part(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 euler, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            return go;
        }

        private static void NavLight(string name, Transform parent, Vector3 pos, Material mat, Color color, float phase)
        {
            var bulb = Part(PrimitiveType.Sphere, name, parent, pos, Vector3.zero, new Vector3(0.35f, 0.35f, 0.35f), mat);
            var light = A.PointLight("Brillo", bulb.transform, Vector3.zero, color, 6f, 3f, false);
            var blink = bulb.AddComponent<NavBlinkLight>();
            A.SetFloat(blink, "phase", phase);
            B.SetRef(blink, "bulb", bulb.GetComponent<Renderer>());
            B.SetRef(blink, "glow", light);
        }

        /// <summary>Escombros que flotan entre la cámara y la nave.</summary>
        internal static void BuildDebris(Transform parent)
        {
            var rock = B.CreateMat("MM_Escombro", new Color(0.3f, 0.29f, 0.28f), null, 0.15f, 0.2f);
            var metal = B.CreateMat("MM_Chatarra", new Color(0.45f, 0.47f, 0.5f), null, 0.5f, 0.8f);
            var root = new GameObject("Escombros").transform;
            root.SetParent(parent);
            var rng = new System.Random(7);
            for (int i = 0; i < 14; i++)
            {
                float yaw = 5f + (float)rng.NextDouble() * 45f, dist = 14f + (float)rng.NextDouble() * 30f;
                var dir = Quaternion.Euler(-12f + (float)rng.NextDouble() * 26f, yaw, 0f) * Vector3.forward;
                float s = 0.25f + (float)rng.NextDouble() * 0.9f;
                var go = Part(i % 3 == 0 ? PrimitiveType.Cube : PrimitiveType.Sphere, "Escombro", root, dir * dist, new Vector3(rng.Next(360), rng.Next(360), 0f),
                              new Vector3(s, s * (0.5f + (float)rng.NextDouble()), s * (0.6f + (float)rng.NextDouble() * 0.8f)), i % 3 == 0 ? metal : rock);
                var d = go.AddComponent<DebrisDrift>();
                A.SetVector3(d, "velocity", Vector3.zero);   // solo giran: así no salen del encuadre aunque el menú se quede abierto
                A.SetVector3(d, "spin", new Vector3(rng.Next(4, 20), rng.Next(4, 25), rng.Next(2, 10)));
            }
        }

        // =====================================================================
        //  INTERFAZ
        // =====================================================================

        private static readonly Vector2 C = new Vector2(0.5f, 0.5f);
        internal static readonly Color Cyan = new Color(0.35f, 0.85f, 1f);
        internal static readonly Color Orange = new Color(1f, 0.55f, 0.2f);

        /// <summary>Imagen con forma generada por código.</summary>
        internal static Image Shape(string name, Transform parent, UIShapeKind kind, Color color, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var img = B.UIImage(name, parent, color);
            B.Anchor(img.rectTransform, anchor, anchor, pos, size);
            var shape = img.gameObject.AddComponent<UIShape>();
            B.SetEnum(shape, "kind", (int)kind);
            shape.Apply();
            return img;
        }

        /// <summary>Rectángulo de color con anclaje y tamaño.</summary>
        internal static Image Rect(string name, Transform parent, Color color, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var img = B.UIImage(name, parent, color);
            B.Anchor(img.rectTransform, anchor, anchor, pos, size);
            return img;
        }

        /// <summary>Imagen a pantalla completa.</summary>
        internal static Image FullScreen(string name, Transform parent, Color color, UIShapeKind? kind = null)
        {
            var img = B.UIImage(name, parent, color);
            B.Anchor(img.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            if (kind.HasValue) { var s = img.gameObject.AddComponent<UIShape>(); B.SetEnum(s, "kind", (int)kind.Value); s.Apply(); }
            return img;
        }

        /// <summary>Contenedor con entrada animada (CanvasGroup + UIIntro).</summary>
        internal static RectTransform Group(string name, Transform parent, float delay, Vector2 slideFrom, float duration = 0.6f)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            B.Anchor(rt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            go.AddComponent<CanvasGroup>();
            var intro = go.AddComponent<UIIntro>();
            A.SetFloat(intro, "delay", delay);
            A.SetFloat(intro, "duration", duration);
            A.SetVector2(intro, "slideFrom", slideFrom);
            return rt;
        }

        /// <summary>Esquinas de pantalla de mando (en L) en las 4 esquinas del lienzo.</summary>
        internal static void HudCorners(Transform parent, Color color, float inset, float length, float thick)
        {
            Vector2[] corners = { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) };
            string[] names = { "Esquina_II", "Esquina_ID", "Esquina_SI", "Esquina_SD" };
            for (int i = 0; i < 4; i++)
            {
                var a = corners[i];
                float sx = a.x == 0f ? 1f : -1f, sy = a.y == 0f ? 1f : -1f;
                Rect(names[i] + "_H", parent, color, a, new Vector2(sx * (inset + length / 2f), sy * inset), new Vector2(length, thick));
                Rect(names[i] + "_V", parent, color, a, new Vector2(sx * inset, sy * (inset + length / 2f)), new Vector2(thick, length));
            }
        }

        /// <summary>Botón con franja de acento, tinte al pasar el ratón y la tecla en un recuadro a la derecha.</summary>
        internal static Button MenuButton(string name, Transform parent, Vector2 pos, Vector2 size, string label, string key, Color bg, Color accent)
        {
            var img = Rect(name, parent, bg, C, pos, size);
            img.raycastTarget = true;   // B.UIImage lo desactiva: sin esto el botón no recibe clics
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.navigation = new Navigation { mode = Navigation.Mode.None };   // [Intro] no puede «pulsar» Salir por accidente
            btn.colors = HoverColors();
            Rect("Acento", img.transform, accent, new Vector2(0f, 0.5f), new Vector2(4f, 0f), new Vector2(8f, size.y));
            float textW = size.x - (string.IsNullOrEmpty(key) ? 60f : 150f);
            var t = B.UIText("Texto", img.transform, new Vector2(0f, 0.5f), new Vector2(30f + textW / 2f, 0f), new Vector2(textW, size.y), 30f, TextAlignmentOptions.Left, Color.white);
            t.text = $"<b>{label}</b>";
            NoWrap(t); t.enableAutoSizing = true; t.fontSizeMax = 30f; t.fontSizeMin = 18f;   // una sola línea
            t.fontSize = 30f;
            if (!string.IsNullOrEmpty(key))
            {
                var chip = Rect("Tecla", img.transform, new Color(0f, 0f, 0f, 0.35f), new Vector2(1f, 0.5f), new Vector2(-56f, 0f), new Vector2(84f, 34f));
                B.UIText("TextoTecla", chip.transform, C, Vector2.zero, new Vector2(84f, 34f), 18f, TextAlignmentOptions.Center, new Color(0.8f, 0.88f, 0.95f)).text = key;
            }
            return btn;
        }

        /// <summary>Texto en una sola línea (Unity 6 cambió la propiedad de TextMeshPro).</summary>
        internal static void NoWrap(TextMeshProUGUI t)
        {
#if UNITY_6000_0_OR_NEWER
            t.textWrappingMode = TextWrappingModes.NoWrap;
#else
            t.enableWordWrapping = false;
#endif
        }

        /// <summary>Tinte de los botones: algo apagados en reposo, brillan al pasar el ratón y se oscurecen al pulsar.</summary>
        internal static ColorBlock HoverColors()
        {
            var c = ColorBlock.defaultColorBlock;
            // Con el multiplicador 1,4 el reposo queda en ~0,95 y al pasar el ratón el fondo se aclara un 40 %
            c.normalColor = new Color(0.68f, 0.68f, 0.68f, 1f);
            c.highlightedColor = Color.white;
            c.selectedColor = new Color(0.68f, 0.68f, 0.68f, 1f);
            c.pressedColor = new Color(0.5f, 0.5f, 0.5f, 1f);
            c.disabledColor = new Color(0.35f, 0.35f, 0.35f, 0.6f);
            c.colorMultiplier = 1.4f;
            c.fadeDuration = 0.08f;
            return c;
        }

        internal static Color DifficultyColor(int i) =>
            i == 0 ? new Color(0.35f, 0.95f, 0.55f) : i == 1 ? new Color(1f, 0.78f, 0.25f) : new Color(1f, 0.32f, 0.25f);

        /// <summary>Construye la interfaz del menú en el lienzo y la conecta con MainMenuUI.</summary>
        internal static MainMenuUI BuildMenuUI(Transform canvas)
        {
            // ---------- Capas de fondo (detrás del texto) ----------
            Shape("Nebulosa_Morada", canvas, UIShapeKind.Glow, new Color(0.55f, 0.2f, 0.85f, 0.32f), new Vector2(1f, 1f), new Vector2(-420f, -230f), new Vector2(1100f, 760f));
            Shape("Nebulosa_Azul", canvas, UIShapeKind.Glow, new Color(0.15f, 0.45f, 1f, 0.26f), new Vector2(1f, 1f), new Vector2(-120f, -480f), new Vector2(900f, 700f));
            Shape("Nebulosa_Rosa", canvas, UIShapeKind.Glow, new Color(1f, 0.3f, 0.55f, 0.14f), new Vector2(0.5f, 1f), new Vector2(120f, -60f), new Vector2(900f, 420f));
            // Atmósfera: halo con el máximo sobre el borde del planeta (radio en pantalla ≈ 340 px, centro ≈ (60, −320))
            Shape("HaloPlaneta", canvas, UIShapeKind.Halo, new Color(0.4f, 0.7f, 1f, 0.45f), C, new Vector2(60f, -320f), new Vector2(910f, 910f));
            FullScreen("Vineta", canvas, Color.black, UIShapeKind.Vignette);
            var scan = new GameObject("LineasEscaneo", typeof(RectTransform));
            scan.transform.SetParent(canvas, false);
            B.Anchor((RectTransform)scan.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var raw = scan.AddComponent<RawImage>();
            raw.color = new Color(0f, 0f, 0f, 0.55f);
            raw.raycastTarget = false;
            var scanShape = scan.AddComponent<UIShape>();
            B.SetEnum(scanShape, "kind", (int)UIShapeKind.Scanlines);
            scanShape.Apply();

            // Panel lateral con degradado hacia la derecha, franja de acento y esquinas de pantalla de mando
            var panel = B.UIImage("PanelLateral", canvas, new Color(0.02f, 0.05f, 0.09f, 0.88f));
            B.Anchor(panel.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(600f, 0f), new Vector2(1200f, 0f));
            var ps = panel.gameObject.AddComponent<UIShape>();
            B.SetEnum(ps, "kind", (int)UIShapeKind.GradientH);
            ps.Apply();
            Rect("FranjaAcento", canvas, new Color(Cyan.r, Cyan.g, Cyan.b, 0.9f), new Vector2(0f, 0.5f), new Vector2(58f, 40f), new Vector2(4f, 760f));
            Rect("FranjaAcentoBrillo", canvas, new Color(Cyan.r, Cyan.g, Cyan.b, 0.25f), new Vector2(0f, 0.5f), new Vector2(58f, 40f), new Vector2(14f, 760f));
            HudCorners(canvas, new Color(Cyan.r, Cyan.g, Cyan.b, 0.55f), 26f, 70f, 3f);

            // Estado del sistema (arriba a la derecha) con un punto que late
            var status = Group("BloqueEstado", canvas, 0.9f, new Vector2(0f, 20f));
            var dot = Shape("PuntoEstado", status, UIShapeKind.Circle, new Color(0.3f, 1f, 0.5f, 1f), new Vector2(1f, 1f), new Vector2(-392f, -62f), new Vector2(14f, 14f));
            var dp = dot.gameObject.AddComponent<UIPulse>();
            A.SetFloat(dp, "speed", 1.2f);
            B.UIText("TextoEstado", status, new Vector2(1f, 1f), new Vector2(-200f, -62f), new Vector2(360f, 30f), 18f, TextAlignmentOptions.Left, new Color(0.6f, 0.9f, 0.7f)).text =
                "SISTEMA NEXUS-7 · EN LÍNEA";

            // ---------- Título ----------
            var title = Group("BloqueTitulo", canvas, 0.1f, new Vector2(-70f, 0f), 0.8f);
            // Sombra azul que late detrás del título (da relieve y resplandor)
            var glow = B.UIText("TituloBrillo", title, C, new Vector2(-376f, 225f), new Vector2(1000f, 140f), 96f, TextAlignmentOptions.Left, new Color(0.1f, 0.4f, 0.95f, 0.75f));
            glow.text = "<b>ESCAPE ESPACIAL</b>";
            var gp = glow.gameObject.AddComponent<UIPulse>();
            A.SetFloat(gp, "speed", 0.35f); A.SetFloat(gp, "minAlpha", 0.45f); A.SetFloat(gp, "maxAlpha", 1f);
            B.UIText("Titulo", title, C, new Vector2(-380f, 230f), new Vector2(1000f, 140f), 96f, TextAlignmentOptions.Left, new Color(0.6f, 0.9f, 1f)).text = "<b>ESCAPE ESPACIAL</b>";
            B.UIText("Subtitulo", title, C, new Vector2(-380f, 130f), new Vector2(1000f, 80f), 60f, TextAlignmentOptions.Left, Orange).text = "<b>NEXUS-7</b>";
            var line = Rect("LineaTitulo", title, new Color(Orange.r, Orange.g, Orange.b, 0.85f), C, new Vector2(-560f, 88f), new Vector2(640f, 3f));
            var sweep = Shape("DestelloLinea", line.transform, UIShapeKind.Glow, new Color(1f, 0.9f, 0.7f, 0.9f), C, Vector2.zero, new Vector2(150f, 18f));
            var sw = sweep.gameObject.AddComponent<UISweep>();
            A.SetFloat(sw, "fromX", -245f); A.SetFloat(sw, "toX", 245f); A.SetFloat(sw, "period", 3.2f);
            B.UIText("Lema", title, C, new Vector2(-380f, 50f), new Vector2(1000f, 50f), 26f, TextAlignmentOptions.Left, new Color(0.75f, 0.8f, 0.85f)).text =
                "La nave se ha vuelto contra ti. Sigilo, ingenio... y una cápsula de escape.";

            // ---------- Botones ----------
            var buttons = Group("BloqueBotones", canvas, 0.35f, new Vector2(-70f, 0f));
            var newGame = MenuButton("BotonNuevaPartida", buttons, new Vector2(-670f, -20f), new Vector2(420f, 56f), "NUEVA PARTIDA", "Intro", new Color(0.06f, 0.32f, 0.45f, 0.95f), Cyan);
            var cont = MenuButton("BotonContinuar", buttons, new Vector2(-670f, -80f), new Vector2(420f, 52f), "CONTINUAR", "C", new Color(0.05f, 0.2f, 0.3f, 0.95f), Cyan);
            var contDetail = B.UIText("DetalleContinuar", buttons, C, new Vector2(-230f, -80f), new Vector2(440f, 40f), 20f, TextAlignmentOptions.Left, new Color(0.7f, 0.76f, 0.82f));
            NoWrap(contDetail);
            contDetail.text = "Sin partida guardada";
            var settingsBtn = MenuButton("BotonConfiguracion", buttons, new Vector2(-670f, -140f), new Vector2(420f, 52f), "CONFIGURACIÓN", "O", new Color(0.1f, 0.14f, 0.22f, 0.95f), new Color(0.7f, 0.78f, 0.9f));
            var quit = MenuButton("BotonSalir", buttons, new Vector2(-670f, -200f), new Vector2(420f, 52f), "SALIR", null, new Color(0.3f, 0.09f, 0.09f, 0.95f), new Color(1f, 0.35f, 0.3f));

            // ---------- Dificultad: 3 tarjetas ----------
            var diff = Group("BloqueDificultad", canvas, 0.55f, new Vector2(-70f, 0f));
            B.UIText("TituloDificultad", diff, C, new Vector2(-380f, -250f), new Vector2(1000f, 36f), 24f, TextAlignmentOptions.Left, Cyan).text = "<b>DIFICULTAD</b>";
            var diffButtons = new Object[3]; var diffLabels = new Object[3]; var frames = new Object[3]; var cards = new Object[3];
            for (int i = 0; i < 3; i++)
            {
                var d = (Difficulty)i; var col = DifficultyColor(i);
                var card = new GameObject("TarjetaDificultad_" + i, typeof(RectTransform));
                card.transform.SetParent(diff, false);
                var crt = (RectTransform)card.transform;
                B.Anchor(crt, C, C, new Vector2(-770f + i * 200f, -320f), new Vector2(186f, 96f));
                var frame = Rect("Marco", card.transform, MainMenuUI.IdleFrame, C, Vector2.zero, new Vector2(194f, 104f));
                var img = Rect("BotonDificultad_" + i, card.transform, new Color(col.r * 0.22f, col.g * 0.22f, col.b * 0.22f, 0.95f), C, Vector2.zero, new Vector2(186f, 96f));
                img.raycastTarget = true;
                var btn = img.gameObject.AddComponent<Button>();
                btn.targetGraphic = img;
                btn.navigation = new Navigation { mode = Navigation.Mode.None };
                btn.colors = HoverColors();
                Rect("Franja", img.transform, col, new Vector2(0.5f, 1f), new Vector2(0f, -2f), new Vector2(186f, 4f));
                var lbl = B.UIText("Texto", img.transform, new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(176f, 36f), 26f, TextAlignmentOptions.Center, Color.white);
                lbl.text = MainMenuUI.CardTitle(d, false);
                NoWrap(lbl); lbl.enableAutoSizing = true; lbl.fontSizeMax = 26f; lbl.fontSizeMin = 14f;
                for (int k = 0; k < 3; k++)   // nivel de peligro: 1–3 marcas encendidas
                    Rect("Peligro_" + k, img.transform, k < ScreenFx.DangerPips(d) ? col : new Color(1f, 1f, 1f, 0.12f), C, new Vector2(-34f + k * 34f, -6f), new Vector2(28f, 8f));
                B.UIText("Detalle", img.transform, new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(176f, 26f), 18f, TextAlignmentOptions.Center, new Color(0.75f, 0.8f, 0.85f)).text =
                    DifficultySettings.DamageText(d);
                diffButtons[i] = btn; diffLabels[i] = lbl; frames[i] = frame; cards[i] = crt;
            }
            var hint = B.UIText("PistaDificultad", diff, C, new Vector2(-380f, -392f), new Vector2(1000f, 30f), 19f, TextAlignmentOptions.Left, new Color(0.7f, 0.76f, 0.82f));

            // ---------- Récord ----------
            var rec = Group("BloqueRecord", canvas, 0.75f, new Vector2(-70f, 0f));
            var recCard = Rect("TarjetaRecord", rec, new Color(0.04f, 0.07f, 0.11f, 0.85f), C, new Vector2(-580f, -436f), new Vector2(600f, 48f));
            Rect("Acento", recCard.transform, new Color(1f, 0.85f, 0.35f, 0.95f), new Vector2(0f, 0.5f), new Vector2(3f, 0f), new Vector2(6f, 48f));
            Shape("IconoRecord", recCard.transform, UIShapeKind.Star, new Color(1f, 0.85f, 0.35f, 1f), new Vector2(0f, 0.5f), new Vector2(34f, 0f), new Vector2(32f, 32f));
            var best = B.UIText("Record", recCard.transform, new Vector2(0f, 0.5f), new Vector2(330f, 0f), new Vector2(540f, 40f), 24f, TextAlignmentOptions.Left, new Color(1f, 0.85f, 0.35f));

            // ---------- Barra de controles (abajo) ----------
            var bar = B.UIImage("BarraControles", canvas, new Color(0f, 0f, 0f, 0.6f));
            B.Anchor(bar.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 36f), new Vector2(0f, 72f));
            Rect("LineaControles", bar.transform, new Color(Cyan.r, Cyan.g, Cyan.b, 0.5f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(1920f, 2f));
            B.UIText("Controles", bar.transform, C, Vector2.zero, new Vector2(1800f, 60f), 20f, TextAlignmentOptions.Center, new Color(0.65f, 0.7f, 0.76f)).text =
                "[WASD] moverse · [Shift] correr · [Ctrl/C] agacharse · [Espacio] saltar · [V] cámara · [E] interactuar · [Tab/I] inventario · [1-9] elegir objeto · [G] usar/lanzar";

            // ---------- Paneles encima de todo: Configuración y confirmación ----------
            var settings = BuildSettingsPanel(canvas);
            var confirm = BuildConfirmPanel(canvas, out var yes, out var no);
            canvas.gameObject.AddComponent<Nexus7.Audio.UISounds>();   // sonidos al pasar por encima y al pulsar

            var menu = canvas.gameObject.AddComponent<MainMenuUI>();
            B.SetRef(menu, "newGameButton", newGame);
            B.SetRef(menu, "quitButton", quit);
            B.SetRef(menu, "continueButton", cont);
            B.SetRef(menu, "continueDetail", contDetail);
            B.SetRef(menu, "settingsButton", settingsBtn);
            B.SetRef(menu, "settings", settings);
            B.SetRef(menu, "confirmPanel", confirm);
            B.SetRef(menu, "confirmYes", yes);
            B.SetRef(menu, "confirmNo", no);
            B.SetRef(menu, "bestScoreText", best);
            A.SetObjectArray(menu, "difficultyButtons", diffButtons);
            A.SetObjectArray(menu, "difficultyLabels", diffLabels);
            A.SetObjectArray(menu, "difficultyFrames", frames);
            A.SetObjectArray(menu, "difficultyCards", cards);
            B.SetRef(menu, "difficultyHint", hint);
            return menu;
        }

        // =====================================================================
        //  CONFIGURACIÓN Y CONFIRMACIÓN
        // =====================================================================

        /// <summary>Fondo oscuro a pantalla completa que bloquea los clics al menú de detrás, con un panel centrado.</summary>
        private static Image Modal(string name, Transform canvas, Vector2 size, out GameObject root)
        {
            root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(canvas, false);
            B.Anchor((RectTransform)root.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var dim = FullScreen("Oscurecer", root.transform, new Color(0f, 0.01f, 0.03f, 0.78f));
            dim.raycastTarget = true;
            var panel = Rect("Panel", root.transform, new Color(0.03f, 0.06f, 0.1f, 0.97f), C, Vector2.zero, size);
            panel.raycastTarget = true;
            Rect("FranjaSuperior", panel.transform, Cyan, new Vector2(0.5f, 1f), new Vector2(0f, -2f), new Vector2(size.x, 4f));
            HudCorners(panel.transform, new Color(Cyan.r, Cyan.g, Cyan.b, 0.6f), 10f, 40f, 2f);
            return panel;
        }

        private static void Automatic(Selectable s) => s.navigation = new Navigation { mode = Navigation.Mode.Automatic };

        /// <summary>Fila de volumen: nombre, slider (0–100) y valor en %.</summary>
        private static Slider SliderRow(Transform parent, string name, string label, float y, out TextMeshProUGUI value)
        {
            B.UIText("Etiqueta" + name, parent, C, new Vector2(-250f, y), new Vector2(300f, 40f), 26f, TextAlignmentOptions.Left, Color.white).text = label;

            var root = new GameObject("Slider" + name, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            B.Anchor((RectTransform)root.transform, C, C, new Vector2(90f, y), new Vector2(380f, 22f));
            var bg = B.UIImage("Fondo", root.transform, new Color(1f, 1f, 1f, 0.12f));
            B.Anchor(bg.rectTransform, new Vector2(0f, 0.3f), new Vector2(1f, 0.7f), Vector2.zero, Vector2.zero);
            bg.raycastTarget = true;
            var fillArea = new GameObject("ZonaRelleno", typeof(RectTransform));
            fillArea.transform.SetParent(root.transform, false);
            var fa = (RectTransform)fillArea.transform;
            fa.anchorMin = new Vector2(0f, 0.3f); fa.anchorMax = new Vector2(1f, 0.7f);
            fa.offsetMin = new Vector2(0f, 0f); fa.offsetMax = new Vector2(-11f, 0f);
            var fill = B.UIImage("Relleno", fillArea.transform, Cyan);
            fill.rectTransform.sizeDelta = new Vector2(11f, 0f);
            var handleArea = new GameObject("ZonaTirador", typeof(RectTransform));
            handleArea.transform.SetParent(root.transform, false);
            var ha = (RectTransform)handleArea.transform;
            ha.anchorMin = Vector2.zero; ha.anchorMax = Vector2.one;
            ha.offsetMin = new Vector2(11f, 0f); ha.offsetMax = new Vector2(-11f, 0f);
            var handle = B.UIImage("Tirador", handleArea.transform, Color.white);
            handle.rectTransform.sizeDelta = new Vector2(22f, 0f);
            handle.raycastTarget = true;
            var hs = handle.gameObject.AddComponent<UIShape>();
            B.SetEnum(hs, "kind", (int)UIShapeKind.Circle);
            hs.Apply();

            var slider = root.AddComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f; slider.maxValue = 100f; slider.wholeNumbers = true; slider.value = 80f;
            slider.colors = HoverColors();
            Automatic(slider);

            value = B.UIText("Valor" + name, parent, C, new Vector2(345f, y), new Vector2(110f, 40f), 24f, TextAlignmentOptions.Right, Cyan);
            value.text = "80 %";
            return slider;
        }

        /// <summary>Panel de Configuración (empieza oculto) conectado con SettingsMenuUI.</summary>
        internal static SettingsMenuUI BuildSettingsPanel(Transform canvas)
        {
            var panel = Modal("PanelConfiguracion", canvas, new Vector2(900f, 640f), out var root);
            var p = panel.transform;
            B.UIText("Titulo", p, C, new Vector2(0f, 262f), new Vector2(800f, 60f), 40f, TextAlignmentOptions.Center, Cyan).text = "<b>CONFIGURACIÓN</b>";

            var master = SliderRow(p, "General", "Volumen general", 170f, out var masterValue);
            var music = SliderRow(p, "Musica", "Música", 100f, out var musicValue);
            var sfx = SliderRow(p, "Efectos", "Efectos", 30f, out var sfxValue);

            B.UIText("EtiquetaCalidad", p, C, new Vector2(-250f, -50f), new Vector2(300f, 40f), 26f, TextAlignmentOptions.Left, Color.white).text = "Calidad gráfica";
            var qButtons = new Object[3]; var qFrames = new Object[3];
            string[] qNames = { "BAJA", "MEDIA", "ALTA" };
            for (int i = 0; i < 3; i++)
            {
                var frame = Rect("MarcoCalidad_" + i, p, MainMenuUI.IdleFrame, C, new Vector2(-40f + i * 150f, -50f), new Vector2(142f, 54f));
                var b = MenuButton("BotonCalidad_" + i, p, new Vector2(-40f + i * 150f, -50f), new Vector2(134f, 46f), qNames[i], null,
                                   new Color(0.08f, 0.12f, 0.18f, 0.95f), Cyan);
                Automatic(b);
                qButtons[i] = b; qFrames[i] = frame;
            }

            var fs = MenuButton("BotonPantallaCompleta", p, new Vector2(0f, -135f), new Vector2(520f, 52f), "PANTALLA COMPLETA: SÍ", null,
                                new Color(0.08f, 0.12f, 0.18f, 0.95f), Cyan);
            Automatic(fs);
            var fsLabel = fs.transform.Find("Texto").GetComponent<TextMeshProUGUI>();

            var back = MenuButton("BotonVolver", p, new Vector2(0f, -240f), new Vector2(320f, 56f), "VOLVER", "Esc",
                                  new Color(0.3f, 0.09f, 0.09f, 0.95f), new Color(1f, 0.35f, 0.3f));
            Automatic(back);
            B.UIText("Nota", p, C, new Vector2(0f, -298f), new Vector2(840f, 30f), 18f, TextAlignmentOptions.Center, new Color(0.6f, 0.66f, 0.72f)).text =
                "Los cambios se aplican al momento y se guardan. Teclado/mando: ↑ ↓ elegir · ← → ajustar";

            var ui = canvas.gameObject.AddComponent<SettingsMenuUI>();
            B.SetRef(ui, "panel", root);
            B.SetRef(ui, "masterSlider", master);
            B.SetRef(ui, "musicSlider", music);
            B.SetRef(ui, "sfxSlider", sfx);
            B.SetRef(ui, "masterValue", masterValue);
            B.SetRef(ui, "musicValue", musicValue);
            B.SetRef(ui, "sfxValue", sfxValue);
            A.SetObjectArray(ui, "qualityButtons", qButtons);
            A.SetObjectArray(ui, "qualityFrames", qFrames);
            B.SetRef(ui, "fullscreenButton", fs);
            B.SetRef(ui, "fullscreenLabel", fsLabel);
            B.SetRef(ui, "backButton", back);
            B.SetRef(ui, "firstSelected", master);
            root.SetActive(false);
            return ui;
        }

        /// <summary>«Se sobrescribirá la partida guardada. ¿Empezar de nuevo?» con SÍ / NO (empieza oculto).</summary>
        internal static GameObject BuildConfirmPanel(Transform canvas, out Button yes, out Button no)
        {
            var panel = Modal("PanelConfirmacion", canvas, new Vector2(760f, 280f), out var root);
            var p = panel.transform;
            B.UIText("Pregunta", p, C, new Vector2(0f, 60f), new Vector2(700f, 110f), 28f, TextAlignmentOptions.Center, Color.white).text =
                "Se sobrescribirá la partida guardada.\n<b>¿Empezar de nuevo?</b>";
            yes = MenuButton("BotonSi", p, new Vector2(-150f, -70f), new Vector2(240f, 56f), "SÍ", null, new Color(0.06f, 0.32f, 0.45f, 0.95f), Cyan);
            no = MenuButton("BotonNo", p, new Vector2(150f, -70f), new Vector2(240f, 56f), "NO", "Esc", new Color(0.3f, 0.09f, 0.09f, 0.95f), new Color(1f, 0.35f, 0.3f));
            Automatic(yes); Automatic(no);
            root.SetActive(false);
            return root;
        }
    }
}
