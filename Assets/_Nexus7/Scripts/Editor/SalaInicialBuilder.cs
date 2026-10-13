using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Nexus7.Environment;
using Nexus7.Interaction;
using Nexus7.Player;
using Nexus7.Tutorial;

namespace Nexus7.EditorTools
{
    /// <summary>
    /// Herramienta de editor: menú "Nexus-7 > Fase 1 > Construir Sala Inicial".
    /// Crea las carpetas del proyecto, los materiales, la escena SalaInicial con el corredor,
    /// las luces de emergencia, la puerta bloqueada, la terminal, el jugador, el HUD y el tutorial,
    /// y guarda los prefabs principales. Puedes ejecutarlo de nuevo cuando quieras para regenerar la escena.
    ///
    /// Debe estar dentro de una carpeta llamada "Editor" (no se incluye en la build del juego).
    /// </summary>
    public static class SalaInicialBuilder
    {
        private const string Root = "Assets/_Nexus7";
        private const string ScenePath = Root + "/Scenes/SalaInicial.unity";

        // Dimensiones del corredor (metros)
        private const float Width = 4f, Height = 3f, CorridorLength = 20f, VestibuleLength = 4f, Wall = 0.2f;

        [MenuItem("Nexus-7/Fase 1/Construir Sala Inicial")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            CreateFolders();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ---------- Materiales ----------
            var floorMat   = CreateMat("M_SueloMetal",   new Color(0.18f, 0.19f, 0.21f), null, 0.55f, 0.8f);
            var wallMat    = CreateMat("M_ParedPanel",   new Color(0.30f, 0.32f, 0.35f), null, 0.35f, 0.6f);
            var trimMat    = CreateMat("M_Tuberia",      new Color(0.12f, 0.12f, 0.13f), null, 0.6f, 0.9f);
            var doorMat    = CreateMat("M_Puerta",       new Color(0.40f, 0.36f, 0.30f), null, 0.4f, 0.7f);
            var redLampMat = CreateMat("M_LamparaEmergencia", Color.black, new Color(1f, 0.08f, 0.03f) * 4f);
            var statusMat  = CreateMat("M_IndicadorPuerta",   Color.red,   new Color(1f, 0.1f, 0.05f) * 3f);
            var screenMat  = CreateMat("M_PantallaTerminal",  new Color(0.1f, 0.8f, 1f), new Color(0.1f, 0.8f, 1f) * 2f);
            var amberMat   = CreateMat("M_PantallaBitacora",  new Color(1f, 0.6f, 0.1f), new Color(1f, 0.6f, 0.1f) * 1.5f);
            var cryoMat    = CreateMat("M_CapsulaCrio",       new Color(0.5f, 0.85f, 1f), new Color(0.2f, 0.6f, 1f) * 1.2f, 0.9f, 0.2f);

            ConfigureLighting();

            // ---------- Entorno ----------
            var env = new GameObject("Entorno").transform;
            float totalLen = CorridorLength + VestibuleLength;
            float midZ = totalLen * 0.5f;

            Box("Suelo",      env, new Vector3(0, -Wall / 2, midZ),            new Vector3(Width + 2 * Wall, Wall, totalLen + 2 * Wall), floorMat);
            Box("Techo",      env, new Vector3(0, Height + Wall / 2, midZ),    new Vector3(Width + 2 * Wall, Wall, totalLen + 2 * Wall), wallMat);
            Box("ParedIzq",   env, new Vector3(-(Width + Wall) / 2, Height / 2, midZ), new Vector3(Wall, Height, totalLen + 2 * Wall), wallMat);
            Box("ParedDer",   env, new Vector3( (Width + Wall) / 2, Height / 2, midZ), new Vector3(Wall, Height, totalLen + 2 * Wall), wallMat);
            Box("ParedFondo", env, new Vector3(0, Height / 2, -Wall / 2),             new Vector3(Width, Height, Wall), wallMat);
            Box("ParedFinal", env, new Vector3(0, Height / 2, totalLen + Wall / 2),   new Vector3(Width, Height, Wall), wallMat);

            // Tuberías decorativas en las esquinas superiores
            NoCollider(Box("TuberiaIzq", env, new Vector3(-1.8f, 2.75f, midZ), new Vector3(0.15f, 0.15f, totalLen), trimMat));
            NoCollider(Box("TuberiaDer", env, new Vector3( 1.8f, 2.75f, midZ), new Vector3(0.15f, 0.15f, totalLen), trimMat));

            // Cápsula criogénica donde despierta el jugador
            var pod = Box("CapsulaCriogenica", env, new Vector3(-1.3f, 0.3f, 1.4f), new Vector3(1.1f, 0.6f, 2.2f), trimMat);
            NoCollider(Box("CristalCapsula", pod.transform.parent, new Vector3(-1.3f, 0.65f, 1.4f), new Vector3(0.9f, 0.1f, 1.9f), cryoMat));

            // Luces de emergencia rojas parpadeantes a lo largo del corredor (la del medio en modo alarma)
            EmergencyLamp(env, new Vector3(0, 2.95f, 4f),  redLampMat, EmergencyLight.Mode.Flicker);
            EmergencyLamp(env, new Vector3(0, 2.95f, 10f), redLampMat, EmergencyLight.Mode.Pulse);
            EmergencyLamp(env, new Vector3(0, 2.95f, 16f), redLampMat, EmergencyLight.Mode.Flicker);
            EmergencyLamp(env, new Vector3(0, 2.95f, 22f), redLampMat, EmergencyLight.Mode.Flicker);

            // ---------- Puerta bloqueada ----------
            var door = BuildDoor(env, CorridorLength, wallMat, doorMat, statusMat);

            // ---------- Terminal que desbloquea la puerta ----------
            // Raíz vacía (sin escala) para que el prefab incluya cuerpo + pantalla; el collider está en el cuerpo
            // y PlayerInteractor encuentra AccessTerminal en el padre gracias a GetComponentInParent.
            var terminal = new GameObject("TerminalMantenimiento");
            terminal.transform.SetParent(env);
            terminal.transform.localPosition = new Vector3(1.75f, 0f, 12f);
            Box("Cuerpo", terminal.transform, new Vector3(0f, 0.6f, 0f), new Vector3(0.5f, 1.2f, 0.7f), trimMat);
            var screen = NoCollider(Box("Pantalla", terminal.transform, new Vector3(-0.27f, 0.95f, 0f), new Vector3(0.04f, 0.45f, 0.55f), screenMat));
            var term = terminal.AddComponent<AccessTerminal>();
            SetString(term, "promptText", "Usar terminal de mantenimiento");
            SetRef(term, "targetDoor", door);
            SetRef(term, "screenRenderer", screen.GetComponent<Renderer>());

            // ---------- Bitácora en la pared (historia ambiental) ----------
            var log = Box("Bitacora", env, new Vector3(-1.97f, 1.35f, 4.5f), new Vector3(0.06f, 0.35f, 0.45f), amberMat);
            var logMsg = log.AddComponent<InteractableMessage>();
            SetString(logMsg, "promptText", "Leer bitácora");
            SetStrings(logMsg, "messages", new[]
            {
                "BITÁCORA — Ing. R. Salazar: «La brecha cortó la energía de la puerta C-7.»",
                "«Desvié el control manual a la terminal de mantenimiento del corredor.»",
                "«Si alguien lee esto... no corras. Eso los atrae.»"
            });

            // ---------- Triggers de historia ----------
            Trigger("Trigger_Terminal", env, new Vector3(0, 1.5f, 8f), new Vector3(Width, Height, 1f),
                    new[] { "IA NEXUS-7: Detecto una terminal de mantenimiento operativa a tu derecha." }, null);
            Trigger("Trigger_FinFase1", env, new Vector3(0, 1.5f, CorridorLength + 2.5f), new Vector3(Width, Height, 2f),
                    new[] { "IA NEXUS-7: Has salido del corredor C-7. Bien hecho, tripulante.", "— FIN DE LA FASE 1 —" },
                    "Objetivo: Dirígete al puente de mando.");

            // ---------- Jugador ----------
            var (player, interactor) = BuildPlayer();

            // ---------- HUD ----------
            var hud = BuildHUD();

            // ---------- Sistemas ----------
            var systems = new GameObject("GameSystems");
            var tutorial = systems.AddComponent<TutorialManager>();
            SetRef(tutorial, "player", player);
            SetRef(tutorial, "interactor", interactor);
            SetRef(tutorial, "hud", hud);

            // ---------- Prefabs ----------
            SavePrefab(player.gameObject, "Player");
            SavePrefab(door.gameObject, "PuertaCorredera");
            SavePrefab(terminal, "TerminalMantenimiento");
            SavePrefab(hud.gameObject, "HUD");

            // ---------- Guardar escena y añadirla a Build Settings ----------
            EditorSceneManager.SaveScene(scene, ScenePath);
            var scenes = EditorBuildSettings.scenes.ToList();
            if (!scenes.Any(s => s.path == ScenePath)) scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();

            Selection.activeGameObject = player.gameObject;
            Debug.Log("<color=cyan>[Nexus-7]</color> Sala Inicial construida en " + ScenePath + ". Pulsa Play.");
        }

        // =====================================================================
        //  Construcción de piezas
        // =====================================================================

        internal static DoorController BuildDoor(Transform parent, float z, Material wallMat, Material doorMat, Material statusMat)
        {
            const float doorW = 2f, doorH = 2.5f;
            var root = new GameObject("PuertaC7");
            root.transform.SetParent(parent);
            root.transform.localPosition = new Vector3(0, 0, z);

            // Marco: dos paredes laterales + dintel
            float sideW = (Width + 2 * Wall - doorW) / 2f;
            float sideX = doorW / 2f + sideW / 2f;
            Box("MarcoIzq", root.transform, new Vector3(-sideX, Height / 2, 0), new Vector3(sideW, Height, Wall), wallMat);
            Box("MarcoDer", root.transform, new Vector3( sideX, Height / 2, 0), new Vector3(sideW, Height, Wall), wallMat);
            Box("Dintel",   root.transform, new Vector3(0, doorH + (Height - doorH) / 2, 0), new Vector3(doorW, Height - doorH, Wall), wallMat);

            // Panel móvil (con collider: bloquea el paso y recibe el raycast)
            var panel = Box("PanelPuerta", root.transform, new Vector3(0, doorH / 2, 0), new Vector3(doorW, doorH, 0.15f), doorMat);

            // Indicador de estado sobre la puerta
            var status = NoCollider(Box("Indicador", root.transform, new Vector3(0, 2.75f, -0.15f), new Vector3(0.35f, 0.12f, 0.05f), statusMat));
            var statusLightGo = new GameObject("LuzIndicador");
            statusLightGo.transform.SetParent(root.transform);
            statusLightGo.transform.localPosition = new Vector3(0, 2.6f, -0.6f);
            var statusLight = statusLightGo.AddComponent<Light>();
            statusLight.type = LightType.Point;
            statusLight.range = 3f;
            statusLight.intensity = 1.5f;

            var audio = root.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.spatialBlend = 1f;

            var door = root.AddComponent<DoorController>();
            SetRef(door, "doorPanel", panel.transform);
            SetRef(door, "statusRenderer", status.GetComponent<Renderer>());
            SetRef(door, "statusLight", statusLight);
            SetRef(door, "audioSource", audio);
            return door;
        }

        internal static (PlayerController, PlayerInteractor) BuildPlayer()
        {
            var go = new GameObject("Player") { tag = "Player" };
            go.transform.position = new Vector3(0f, 0.05f, 2.8f); // junto a la cápsula, mirando hacia +Z

            var cc = go.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0, 0.9f, 0);
            cc.stepOffset = 0.3f;

            var footsteps = go.AddComponent<AudioSource>();
            footsteps.playOnAwake = false;
            footsteps.spatialBlend = 0f;

            var controller = go.AddComponent<PlayerController>();
            var look = go.AddComponent<PlayerLook>();

            var camRoot = new GameObject("CameraRoot").transform;
            camRoot.SetParent(go.transform, false);
            camRoot.localPosition = new Vector3(0, 1.6f, 0);

            var camGo = new GameObject("PlayerCamera") { tag = "MainCamera" };
            camGo.transform.SetParent(camRoot, false);
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            cam.fieldOfView = 70f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            camGo.AddComponent<AudioListener>();
            var interactor = camGo.AddComponent<PlayerInteractor>();

            SetRef(controller, "cameraRoot", camRoot);
            SetRef(controller, "footstepSource", footsteps);
            SetRef(look, "cameraRoot", camRoot);
            SetRef(look, "player", controller);
            SetRef(interactor, "player", controller);
            return (controller, interactor);
        }

        internal static Nexus7.UI.HUDManager BuildHUD()
        {
            var go = new GameObject("HUD", typeof(RectTransform));
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();

            // El orden de los hijos = orden de dibujado. La mira va detrás del fundido; los textos, encima.
            var crosshair = UIImage("Mira", go.transform, new Color(1, 1, 1, 0.6f));
            Anchor(crosshair.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(6, 6));

            var fade = UIImage("Fundido", go.transform, Color.black);
            fade.rectTransform.anchorMin = Vector2.zero;
            fade.rectTransform.anchorMax = Vector2.one;
            fade.rectTransform.offsetMin = fade.rectTransform.offsetMax = Vector2.zero;

            var prompt    = UIText("TextoInteraccion", go.transform, new Vector2(0.5f, 0.5f), new Vector2(0, -70),  new Vector2(900, 60),   30, TextAlignmentOptions.Center, new Color(0.6f, 0.95f, 1f));
            var message   = UIText("TextoMensajes",    go.transform, new Vector2(0.5f, 0f),   new Vector2(0, 150),  new Vector2(1500, 170), 34, TextAlignmentOptions.Center, Color.white);
            var hint      = UIText("TextoTutorial",    go.transform, new Vector2(0.5f, 1f),   new Vector2(0, -110), new Vector2(1300, 120), 30, TextAlignmentOptions.Center, new Color(1f, 0.85f, 0.3f));
            var objective = UIText("TextoObjetivo",    go.transform, new Vector2(0f, 1f),     new Vector2(40, -40), new Vector2(1000, 60),  26, TextAlignmentOptions.TopLeft, new Color(0.6f, 0.9f, 1f));
            objective.rectTransform.pivot = new Vector2(0, 1);

            var hud = go.AddComponent<Nexus7.UI.HUDManager>();
            SetRef(hud, "promptText", prompt);
            SetRef(hud, "messageText", message);
            SetRef(hud, "hintText", hint);
            SetRef(hud, "objectiveText", objective);
            SetRef(hud, "fadeImage", fade);
            return hud;
        }

        internal static void EmergencyLamp(Transform parent, Vector3 pos, Material lampMat, EmergencyLight.Mode mode)
        {
            var fixture = NoCollider(Box("LamparaEmergencia", parent, pos, new Vector3(0.6f, 0.08f, 0.2f), lampMat));
            var lightGo = new GameObject("LuzRoja");
            lightGo.transform.SetParent(parent);
            lightGo.transform.localPosition = pos + Vector3.down * 0.25f;

            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.12f, 0.06f);
            light.range = 7f;
            light.intensity = 2.5f;
            light.shadows = LightShadows.Soft;

            var flicker = lightGo.AddComponent<EmergencyLight>();
            SetEnum(flicker, "mode", (int)mode);
            SetRef(flicker, "emissiveRenderer", fixture.GetComponent<Renderer>());
        }

        internal static void Trigger(string name, Transform parent, Vector3 pos, Vector3 size, string[] messages, string objective)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.localPosition = pos;
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = size;
            var trig = go.AddComponent<TutorialTrigger>();
            SetStrings(trig, "messages", messages);
            if (!string.IsNullOrEmpty(objective)) SetString(trig, "newObjective", objective);
        }

        internal static void ConfigureLighting()
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.035f, 0.035f, 0.05f); // casi negro: la escena la iluminan las luces rojas
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.02f, 0.015f, 0.02f);
            RenderSettings.fogDensity = 0.07f;
        }

        // =====================================================================
        //  Utilidades
        // =====================================================================

        internal static void CreateFolders()
        {
            EnsureFolder("Assets", "_Nexus7");
            foreach (var f in new[] { "Scripts", "Prefabs", "Scenes", "Materials", "UI", "Audio" })
                EnsureFolder(Root, f);
        }

        internal static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder($"{parent}/{name}")) AssetDatabase.CreateFolder(parent, name);
        }

        internal static Shader LitShader()
        {
            if (GraphicsSettings.currentRenderPipeline != null)
            {
                var urp = Shader.Find("Universal Render Pipeline/Lit");
                if (urp != null) return urp;
            }
            return Shader.Find("Standard");
        }

        internal static Material CreateMat(string name, Color color, Color? emission = null, float smoothness = 0.3f, float metallic = 0.5f)
        {
            string path = $"{Root}/Materials/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(LitShader());
                AssetDatabase.CreateAsset(mat, path);
            }
            else mat.shader = LitShader();

            mat.color = color;
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness); // URP
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness); // Built-in
            if (mat.HasProperty("_Metallic"))   mat.SetFloat("_Metallic", metallic);

            if (emission.HasValue)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission.Value);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }

        internal static GameObject Box(string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            go.isStatic = false;
            return go;
        }

        internal static GameObject NoCollider(GameObject go)
        {
            Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        internal static Image UIImage(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        internal static TextMeshProUGUI UIText(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size,
                                              float fontSize, TextAlignmentOptions align, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.color = color;
            tmp.raycastTarget = false;
            tmp.text = "";
            Anchor(tmp.rectTransform, anchor, anchor, pos, size);
            return tmp;
        }

        internal static void Anchor(RectTransform rt, Vector2 min, Vector2 max, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        internal static void SavePrefab(GameObject go, string name)
        {
            PrefabUtility.SaveAsPrefabAssetAndConnect(go, $"{Root}/Prefabs/{name}.prefab", InteractionMode.AutomatedAction);
        }

        // --- Asignación de campos privados [SerializeField] mediante SerializedObject ---
        internal static SerializedProperty Prop(Object target, string field, out SerializedObject so)
        {
            so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) Debug.LogWarning($"[Nexus-7] Campo '{field}' no encontrado en {target.GetType().Name}");
            return p;
        }

        internal static void SetRef(Object target, string field, Object value)
        {
            var p = Prop(target, field, out var so);
            if (p == null) return;
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void SetString(Object target, string field, string value)
        {
            var p = Prop(target, field, out var so);
            if (p == null) return;
            p.stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void SetStrings(Object target, string field, string[] values)
        {
            var p = Prop(target, field, out var so);
            if (p == null) return;
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).stringValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void SetEnum(Object target, string field, int index)
        {
            var p = Prop(target, field, out var so);
            if (p == null) return;
            p.enumValueIndex = index;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
