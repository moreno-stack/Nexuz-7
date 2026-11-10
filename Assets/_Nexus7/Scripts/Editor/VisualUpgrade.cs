using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Nexus7.Visual;

namespace Nexus7.EditorTools
{
    /// <summary>
    /// Capa visual sobre las escenas ya construidas (no cambia la jugabilidad):
    ///   1. Volumen global de posprocesado (bloom, viñeta, tonemapping ACES, color frío, grano) + TensionPostFX.
    ///   2. Partículas: chispas en paneles y cables dañados, vapor en conductos y polvo bajo las lámparas.
    ///   3. Materiales: metal oxidado, placas de suelo y paneles de pared con mapa normal y de metal/suavidad (PNG generados),
    ///      pantallas que parpadean, hologramas con fallo digital y pantallas holográficas flotantes.
    ///   4. Luces: fluorescentes que zumban o fallan y sombras suaves en las luces del techo con estanterías y cajas cerca.
    /// Se ejecuta sola al construir cada fase y desde «Nexus-7 → Gráficos».
    /// Solo toca luces y pantallas que no controla ningún otro script, y se puede ejecutar varias veces sin duplicar nada.
    /// </summary>
    public static class VisualUpgrade
    {
        private const string MaterialsRoot = "Assets/_Nexus7/Materials";
        private const string ScenesRoot = "Assets/_Nexus7/Scenes";
        private const string DataRoot = "Assets/_Nexus7/Data/Visual";
        private const string TexturesRoot = DataRoot + "/Texturas";
        private const string ProfilePath = DataRoot + "/Nexus7_PostProceso.asset";
        private const string HoloMaterialPath = DataRoot + "/M_HoloFlotante.mat";
        private const int TextureSize = 256;

        // Límites por escena (rendimiento)
        internal const int MaxSparks = 3, MaxSteam = 3, MaxDust = 4, MaxFloatingHolos = 3;
        // Presupuesto de sombras en «caras»: una luz puntual se dibuja 6 veces (cubo) y un foco, 1
        internal const int MaxShadowTiles = 18;

        // =====================================================================
        //  MENÚS
        // =====================================================================

        [MenuItem("Nexus-7/Gráficos/Aplicar mejoras visuales a la escena abierta", false, 40)]
        public static void ApplyToOpenSceneMenu()
        {
            ApplyToActiveScene();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("<color=cyan>[Nexus-7]</color> Mejoras visuales aplicadas a la escena abierta.");
        }

        [MenuItem("Nexus-7/Gráficos/Aplicar mejoras visuales a todas las escenas", false, 41)]
        public static void ApplyToAllScenesMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string current = EditorSceneManager.GetActiveScene().path;
            if (!Directory.Exists(ScenesRoot)) { Debug.LogWarning("[Nexus-7] Aún no hay escenas: usa «Nexus-7 → Construir todo el juego»."); return; }
            var scenes = Directory.GetFiles(ScenesRoot, "*.unity").Select(p => p.Replace('\\', '/')).ToArray();
            try
            {
                for (int i = 0; i < scenes.Length; i++)
                {
                    EditorUtility.DisplayProgressBar("Nexus-7", "Mejoras visuales: " + Path.GetFileNameWithoutExtension(scenes[i]), (float)i / scenes.Length);
                    var scene = EditorSceneManager.OpenScene(scenes[i], OpenSceneMode.Single);
                    ApplyToActiveScene();
                    EditorSceneManager.SaveScene(scene);
                }
            }
            finally { EditorUtility.ClearProgressBar(); }
            if (!string.IsNullOrEmpty(current) && File.Exists(current)) EditorSceneManager.OpenScene(current, OpenSceneMode.Single);
            Debug.Log("<color=cyan>[Nexus-7]</color> Mejoras visuales aplicadas a " + scenes.Length + " escenas.");
        }

        // =====================================================================
        //  PASO COMPLETO
        // =====================================================================

        /// <summary>Aplica toda la capa visual a la escena activa (no la guarda: lo hace quien llama).</summary>
        public static void ApplyToActiveScene()
        {
            SalaInicialBuilder.EnsureFolder("Assets/_Nexus7", "Data");
            SalaInicialBuilder.EnsureFolder("Assets/_Nexus7/Data", "Visual");
            SalaInicialBuilder.EnsureFolder(DataRoot, "Texturas");

            UpgradeMaterials();
            var profile = EnsureProfile();
            EnsureVolume(profile);
            EnableCameraPostProcessing();

            var claimed = ClaimedObjects();
            AddLightFlicker(claimed);
            AddScreenFlicker(claimed);
            AddHoloGlitch(claimed);
            // Partículas, hologramas y sombras solo en las salas jugables (no en el menú)
            if (AllInScene<Nexus7.Player.PlayerController>().Count > 0)
            {
                AddFloatingHolograms();
                AddSparks();
                AddSteam();
                AddDust();
                AssignShadows();
            }
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        // =====================================================================
        //  1. POSPROCESADO
        // =====================================================================

        private static T Component<T>(VolumeProfile profile) where T : VolumeComponent
        {
            T c;
            if (profile.TryGet(out c)) return c;
            c = profile.Add<T>(true);
            c.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(c, profile);
            return c;
        }

        /// <summary>Crea (o actualiza) el perfil con los parámetros recomendados de PostFxModel.Defaults.</summary>
        internal static VolumeProfile EnsureProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }
            var d = PostFxModel.Defaults;

            var bloom = Component<Bloom>(profile);
            bloom.threshold.Override(d.bloomThreshold);
            bloom.intensity.Override(d.bloomIntensity);
            bloom.scatter.Override(d.bloomScatter);
            bloom.highQualityFiltering.Override(false);   // más barato

            var vignette = Component<Vignette>(profile);
            vignette.intensity.Override(d.vignette);
            vignette.smoothness.Override(d.vignetteSmoothness);
            vignette.color.Override(Color.black);

            Component<ChromaticAberration>(profile).intensity.Override(0f);   // la sube TensionPostFX

            Component<Tonemapping>(profile).mode.Override(TonemappingMode.ACES);

            var wb = Component<WhiteBalance>(profile);
            wb.temperature.Override(d.temperature);
            wb.tint.Override(d.tint);

            var adj = Component<ColorAdjustments>(profile);
            adj.postExposure.Override(d.postExposure);
            adj.contrast.Override(d.contrast);
            adj.saturation.Override(d.saturation);
            adj.colorFilter.Override(Color.white);

            var smh = Component<ShadowsMidtonesHighlights>(profile);
            smh.shadows.Override(d.shadows);
            smh.highlights.Override(d.highlights);

            var grain = Component<FilmGrain>(profile);
            grain.type.Override(FilmGrainLookup.Thin1);
            grain.intensity.Override(d.grain);
            grain.response.Override(0.8f);

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            return profile;
        }

        private static void EnsureVolume(VolumeProfile profile)
        {
            var volume = AllInScene<Volume>().FirstOrDefault(v => v.gameObject.name == "VolumenPostProceso");
            if (volume == null)
            {
                var go = new GameObject("VolumenPostProceso");
                volume = go.AddComponent<Volume>();
            }
            volume.isGlobal = true;
            volume.priority = 0f;
            volume.weight = 1f;
            volume.sharedProfile = profile;
            if (volume.GetComponent<TensionPostFX>() == null) volume.gameObject.AddComponent<TensionPostFX>();
            EditorUtility.SetDirty(volume);
        }

        private static void EnableCameraPostProcessing()
        {
            foreach (var cam in AllInScene<Camera>())
            {
                cam.allowHDR = true;
                var data = cam.GetUniversalAdditionalCameraData();
                if (data != null) { data.renderPostProcessing = true; EditorUtility.SetDirty(data); }
                EditorUtility.SetDirty(cam);
            }
        }

        // =====================================================================
        //  2. MATERIALES Y TEXTURAS
        // =====================================================================

        private static string TexPath(SurfaceKind k, string map) => $"{TexturesRoot}/T_{k}_{map}.png";

        private static Texture2D WritePng(string path, Color[] pixels, int w, int h, bool srgb, bool normal,
                                          TextureWrapMode wrap = TextureWrapMode.Repeat)
        {
            if (!File.Exists(path))
            {
                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                tex.SetPixels(pixels);
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp != null)
            {
                bool dirty = false;
                var type = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                if (imp.textureType != type) { imp.textureType = type; dirty = true; }
                if (!normal && imp.sRGBTexture != srgb) { imp.sRGBTexture = srgb; dirty = true; }
                if (imp.wrapMode != wrap) { imp.wrapMode = wrap; dirty = true; }
                if (imp.anisoLevel != 4) { imp.anisoLevel = 4; dirty = true; }
                if (dirty) imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static readonly Dictionary<SurfaceKind, Texture2D[]> surfaceCache = new Dictionary<SurfaceKind, Texture2D[]>();

        /// <summary>Color, normal y máscara (metal R / suavidad A) de cada tipo de superficie, guardados como PNG.</summary>
        internal static Texture2D[] SurfaceTextures(SurfaceKind k)
        {
            Texture2D[] t;
            if (surfaceCache.TryGetValue(k, out t) && t.All(x => x != null)) return t;
            const int n = TextureSize;
            float[] height = null;
            System.Func<float[]> H = () => height ?? (height = ProceduralSurface.HeightMap(k, n));
            string pa = TexPath(k, "Color"), pn = TexPath(k, "Normal"), pm = TexPath(k, "MetalSuavidad");
            t = new[]
            {
                WritePng(pa, File.Exists(pa) ? null : ProceduralSurface.Albedo(k, H(), n), n, n, true, false),
                WritePng(pn, File.Exists(pn) ? null : ProceduralSurface.NormalMap(H(), n, 6f), n, n, false, true),
                WritePng(pm, File.Exists(pm) ? null : ProceduralSurface.Mask(k, H(), n), n, n, false, false),
            };
            surfaceCache[k] = t;
            return t;
        }

        /// <summary>Asigna las texturas de superficie a los materiales del juego según su nombre (SurfaceClassifier).</summary>
        internal static void UpgradeMaterials()
        {
            if (!AssetDatabase.IsValidFolder(MaterialsRoot)) return;
            foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { MaterialsRoot }))
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (mat == null) continue;
                var kind = SurfaceClassifier.Classify(mat.name);
                if (kind == SurfaceKind.Skip) continue;
                var tex = SurfaceTextures(kind);
                float tiling = SurfaceClassifier.Tiling(kind);
                foreach (var prop in new[] { "_BaseMap", "_MainTex" })
                    if (mat.HasProperty(prop)) { mat.SetTexture(prop, tex[0]); mat.SetTextureScale(prop, new Vector2(tiling, tiling)); }
                if (mat.HasProperty("_BumpMap"))
                {
                    mat.SetTexture("_BumpMap", tex[1]);
                    if (mat.HasProperty("_BumpScale")) mat.SetFloat("_BumpScale", 1f);
                    mat.EnableKeyword("_NORMALMAP");
                }
                if (mat.HasProperty("_MetallicGlossMap"))
                {
                    mat.SetTexture("_MetallicGlossMap", tex[2]);
                    mat.EnableKeyword("_METALLICSPECGLOSSMAP");   // URP
                    mat.EnableKeyword("_METALLICGLOSSMAP");       // Built-in
                    if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 1f);     // la suavidad sale de la máscara
                    if (mat.HasProperty("_GlossMapScale")) mat.SetFloat("_GlossMapScale", 1f);
                    if (mat.HasProperty("_SmoothnessTextureChannel")) mat.SetFloat("_SmoothnessTextureChannel", 0f);
                }
                EditorUtility.SetDirty(mat);
            }
            AssetDatabase.SaveAssets();
        }

        // =====================================================================
        //  QUÉ OBJETOS YA CONTROLA OTRO SCRIPT
        // =====================================================================

        /// <summary>
        /// Objetos que ya controla algún script del juego: los que llevan un MonoBehaviour propio
        /// o a los que apunta un campo de cualquier script (p. ej. el renderer emisivo de EmergencyLight).
        /// La capa visual no les añade parpadeos para no pelear con ellos.
        /// </summary>
        internal static HashSet<GameObject> ClaimedObjects()
        {
            var claimed = new HashSet<GameObject>();
            foreach (var mb in AllInScene<MonoBehaviour>())
            {
                if (mb == null) continue;
                string ns = mb.GetType().Namespace ?? "";
                // Solo cuentan los scripts del juego (no los de URP, como UniversalAdditionalLightData, ni los de esta capa)
                if (!ns.StartsWith("Nexus7") || ns == "Nexus7.Visual") continue;
                claimed.Add(mb.gameObject);
                using (var so = new SerializedObject(mb))
                {
                var it = so.GetIterator();
                while (it.Next(true))
                {
                    if (it.propertyType != SerializedPropertyType.ObjectReference || it.objectReferenceValue == null) continue;
                    var obj = it.objectReferenceValue;
                    var go = obj as GameObject;
                    var comp = obj as Component;
                    if (go != null) claimed.Add(go);
                    else if (comp != null && !(comp is Transform)) claimed.Add(comp.gameObject);
                }
                }
            }
            return claimed;
        }

        // =====================================================================
        //  3. LUCES Y PANTALLAS
        // =====================================================================

        private static bool NameHas(Object o, params string[] words) => words.Any(w => o.name.IndexOf(w, System.StringComparison.OrdinalIgnoreCase) >= 0);

        private static bool IsOnRobotOrPlayer(Component c) =>
            c.GetComponentInParent<Nexus7.Enemies.EnemyPatrol>() != null || c.GetComponentInParent<Nexus7.Player.PlayerController>() != null;

        /// <summary>
        /// Lámpara (caja emisiva «Lampara…»/«Fluorescente») a la que pertenece una luz: en los constructores
        /// la luz y la lámpara son hermanas, así que se busca la más cercana a menos de 1 m.
        /// </summary>
        private static Renderer FixtureFor(Light l, List<MeshRenderer> fixtures)
        {
            Renderer best = null;
            float bestD = 1f;
            foreach (var f in fixtures)
            {
                float d = Vector3.Distance(f.transform.position, l.transform.position);
                if (d < bestD) { bestD = d; best = f; }
            }
            return best;
        }

        private static List<MeshRenderer> LampFixtures() =>
            AllInScene<MeshRenderer>().Where(r => NameHas(r, "Lampara", "Fluorescente")).ToList();

        /// <summary>Luces de lámpara del techo que no controla ningún script, ordenadas de forma estable.</summary>
        private static List<Light> FreeLampLights(HashSet<GameObject> claimed, List<MeshRenderer> fixtures) =>
            AllInScene<Light>()
                .Where(l => l.type == LightType.Point && !claimed.Contains(l.gameObject) && !IsOnRobotOrPlayer(l)
                            && FixtureFor(l, fixtures) != null)
                .OrderBy(l => l.transform.position.z).ThenBy(l => l.transform.position.x).ToList();

        private static void AddLightFlicker(HashSet<GameObject> claimed)
        {
            var fixtures = LampFixtures();
            int index = 0;
            foreach (var l in FreeLampLights(claimed, fixtures))
            {
                if (l.GetComponent<FlickerLight>() != null) { index++; continue; }
                var f = l.gameObject.AddComponent<FlickerLight>();
                // 1 de cada 5 lámparas está averiada; el resto zumba
                SalaInicialBuilder.SetEnum(f, "style", (int)(index % 5 == 3 ? FlickerStyle.Faulty : FlickerStyle.Hum));
                var lamp = FixtureFor(l, fixtures);
                if (!claimed.Contains(lamp.gameObject)) SalaInicialBuilder.SetRef(f, "emissiveRenderer", lamp);
                index++;
            }
        }

        private static bool IsEmissive(Renderer r)
        {
            var m = r.sharedMaterial;
            return m != null && m.IsKeywordEnabled("_EMISSION") && m.HasProperty("_EmissionColor");
        }

        private static void AddScreenFlicker(HashSet<GameObject> claimed)
        {
            int index = 0;
            foreach (var r in AllInScene<MeshRenderer>().OrderBy(x => x.name).ThenBy(x => x.transform.position.z))
            {
                if (claimed.Contains(r.gameObject) || IsOnRobotOrPlayer(r) || r.GetComponent<EmissiveFlicker>() != null) continue;
                if (!NameHas(r, "Pantalla", "Monitor", "Terminal", "Bitacora", "Tablero", "Led") || NameHas(r, "Holo")) continue;
                if (!IsEmissive(r)) continue;
                var f = r.gameObject.AddComponent<EmissiveFlicker>();
                SalaInicialBuilder.SetEnum(f, "style", (int)(index % 4 == 1 ? FlickerStyle.Faulty : FlickerStyle.Hum));
                index++;
            }
        }

        private static void AddHoloGlitch(HashSet<GameObject> claimed)
        {
            foreach (var r in AllInScene<MeshRenderer>())
            {
                if (claimed.Contains(r.gameObject) || r.GetComponent<HoloGlitch>() != null) continue;
                if (NameHas(r, "PanelHolografico")) r.gameObject.AddComponent<HoloGlitch>();
            }
        }

        /// <summary>Material aditivo de doble cara con la textura holográfica (URP Unlit).</summary>
        private static Material HoloMaterial()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(HoloMaterialPath);
            var tex = WritePng($"{TexturesRoot}/T_HoloPantalla.png",
                               File.Exists($"{TexturesRoot}/T_HoloPantalla.png") ? null : ProceduralSurface.HoloPattern(TextureSize),
                               TextureSize, TextureSize, true, false);
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Transparent");
            if (mat == null) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, HoloMaterialPath); }
            else if (shader != null) mat.shader = shader;
            var col = new Color(0.5f, 1.5f, 2f, 0.85f);   // HDR: brilla con el bloom
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", col);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", col);
            if (mat.HasProperty("_Surface"))
            {
                mat.SetFloat("_Surface", 1f);   // transparente
                mat.SetFloat("_Blend", 2f);     // aditivo
                mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                mat.SetFloat("_DstBlend", (float)BlendMode.One);
                mat.SetFloat("_ZWrite", 0f);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", 0f);   // se ve por las dos caras
            mat.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        /// <summary>Pantallas holográficas flotantes sobre algunas terminales y consolas.</summary>
        private static void AddFloatingHolograms()
        {
            if (AllInScene<Transform>().Any(t => t.name == "FX_HoloFlotante")) return;
            var anchors = AllInScene<MeshRenderer>()
                .Where(r => NameHas(r, "Terminal", "Consola", "MesaMonitorizacion", "PantallaMando") && !IsOnRobotOrPlayer(r))
                .OrderBy(r => r.transform.position.z).ToList();
            var picked = VisualPlacement.Spread(anchors, MaxFloatingHolos);
            if (picked.Count == 0) return;
            var mat = HoloMaterial();
            foreach (var r in picked)
            {
                var b = r.bounds;
                var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.name = "FX_HoloFlotante";
                Object.DestroyImmediate(quad.GetComponent<Collider>());
                quad.transform.SetParent(r.transform.parent, true);
                quad.transform.position = new Vector3(b.center.x, b.max.y + 0.55f, b.center.z);
                quad.transform.rotation = Quaternion.LookRotation(r.transform.forward, Vector3.up);
                quad.transform.localScale = Vector3.one;
                quad.transform.localScale = new Vector3(0.9f / Mathf.Max(0.01f, quad.transform.lossyScale.x),
                                                        0.55f / Mathf.Max(0.01f, quad.transform.lossyScale.y), 1f);
                var mr = quad.GetComponent<MeshRenderer>();
                mr.sharedMaterial = mat;
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = false;
                quad.AddComponent<HoloGlitch>();
            }
        }

        // =====================================================================
        //  4. PARTÍCULAS
        // =====================================================================

        private static bool HasFx(string name) => AllInScene<Transform>().Any(t => t.name == name);

        private static GameObject FxObject(string name, Transform parent, Vector3 worldPos, Quaternion worldRot)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, true);
            go.transform.SetPositionAndRotation(worldPos, worldRot);
            // En el editor el sistema queda parado y sin emitir; AmbientFx lo configura al empezar la partida
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main; main.playOnAwake = false;
            var em = ps.emission; em.enabled = false;
            return go;
        }

        /// <summary>
        /// Material guardado como asset para las partículas (así la variante transparente del shader entra en la build).
        /// FxMaterials.Setup respeta el material que ya tenga el renderer.
        /// </summary>
        private static Material ParticleMaterial(bool additive)
        {
            string path = DataRoot + (additive ? "/FX_Aditivo.mat" : "/FX_Transparente.mat");
            string texPath = TexturesRoot + "/T_Particula.png";
            const int n = 64;
            Color[] px = null;
            if (!File.Exists(texPath))
            {
                px = new Color[n * n];
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                        px[y * n + x] = new Color(1f, 1f, 1f, Nexus7.UI.ProceduralSprites.GlowAlpha((x + 0.5f) / n, (y + 0.5f) / n));
            }
            var tex = WritePng(texPath, px, n, n, true, false, TextureWrapMode.Clamp);

            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) return null;
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, path); }
            else mat.shader = shader;
            mat.SetTexture("_BaseMap", tex);
            mat.mainTexture = tex;
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", additive ? 2f : 0f);
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
            mat.SetFloat("_ZWrite", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void AssignParticleMaterial(GameObject go, bool additive)
        {
            var r = go.GetComponent<ParticleSystemRenderer>();
            var mat = ParticleMaterial(additive);
            if (r == null || mat == null) return;
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        private static void AddSparks()
        {
            if (HasFx("FX_Chispas")) return;
            var candidates = AllInScene<MeshRenderer>()
                .Where(r => NameHas(r, "CablePelado", "Cable", "Panel", "Transformador", "Carcasa", "Tablero")
                            && !NameHas(r, "Holo", "Puerta") && !IsOnRobotOrPlayer(r))
                .OrderBy(r => r.transform.position.z).ToList();
            foreach (var r in VisualPlacement.Spread(candidates, MaxSparks))
            {
                var b = r.bounds;
                var dir = (Vector3.down * 0.4f + (r.transform.right.sqrMagnitude > 0 ? r.transform.right : Vector3.right)).normalized;
                var go = FxObject("FX_Chispas", r.transform.parent, new Vector3(b.center.x, b.center.y + b.extents.y * 0.5f, b.center.z), Quaternion.LookRotation(dir));
                var flash = new GameObject("Destello").AddComponent<Light>();
                flash.transform.SetParent(go.transform, false);
                flash.type = LightType.Point;
                flash.color = new Color(1f, 0.65f, 0.3f);
                flash.range = 3f;
                flash.intensity = 0f;
                flash.shadows = LightShadows.None;
                AssignParticleMaterial(go, true);
                var sparks = go.AddComponent<DamagedPanelSparks>();
                SalaInicialBuilder.SetRef(sparks, "flash", flash);
            }
        }

        private static void AddSteam()
        {
            if (HasFx("FX_Vapor")) return;
            var candidates = AllInScene<MeshRenderer>()
                .Where(r => NameHas(r, "Conducto", "Tuberia", "Ventilacion") && !IsOnRobotOrPlayer(r))
                .OrderBy(r => r.transform.position.z).ToList();
            foreach (var r in VisualPlacement.Spread(candidates, MaxSteam))
            {
                var b = r.bounds;
                bool high = b.center.y > 2f;
                var pos = high ? new Vector3(b.center.x, b.min.y, b.center.z) : new Vector3(b.center.x, b.max.y, b.center.z);
                var go = FxObject("FX_Vapor", r.transform.parent, pos, Quaternion.LookRotation(high ? Vector3.down : Vector3.up, Vector3.forward));
                AssignParticleMaterial(go, false);
                go.AddComponent<SteamVent>();
            }
        }

        private static void AddDust()
        {
            if (HasFx("FX_Polvo")) return;
            var fixtures = LampFixtures();
            var lamps = AllInScene<Light>()
                .Where(l => l.type == LightType.Point && l.transform.position.y > 2.3f && !IsOnRobotOrPlayer(l) && FixtureFor(l, fixtures) != null)
                .OrderBy(l => l.transform.position.z).ThenBy(l => l.transform.position.x).ToList();
            foreach (var l in VisualPlacement.Spread(lamps, MaxDust))
            {
                var p = l.transform.position;
                var go = FxObject("FX_Polvo", l.transform.parent, new Vector3(p.x, Mathf.Max(1.3f, p.y - 1.4f), p.z), Quaternion.identity);
                AssignParticleMaterial(go, true);
                go.AddComponent<DustMotes>();
            }
        }

        // =====================================================================
        //  5. SOMBRAS SUAVES
        // =====================================================================

        /// <summary>
        /// Sombras suaves: las luces que ya tenían sombra la mantienen (pasa a suave). Si queda presupuesto
        /// (MaxShadowTiles: una luz puntual cuesta 6 caras y un foco 1, contando también los focos de los robots),
        /// se añaden las lámparas del techo con más estanterías, cajas y mesas cerca (ShadowLightPicker),
        /// para que proyecten sombras reales donde el jugador pueda esconderse. Las demás siguen sin sombra.
        /// </summary>
        private static void AssignShadows()
        {
            var all = AllInScene<Light>()
                .Where(l => (l.type == LightType.Point || l.type == LightType.Spot) && !NameHas(l, "Destello"))
                .ToList();
            int used = 0;
            foreach (var l in all.Where(x => x.shadows != LightShadows.None))
            {
                used += ShadowTiles(l);
                if (!IsOnRobotOrPlayer(l)) SoftShadow(l);
            }

            int budget = MaxShadowTiles - used;
            if (budget >= 6)
            {
                // Solo luces encendidas y activas (no las que esperan a un evento o están a 0)
                var candidates = all.Where(l => l.shadows == LightShadows.None && l.type == LightType.Point && l.transform.position.y > 2.2f
                                                && l.enabled && l.gameObject.activeInHierarchy && l.intensity > 0f && !IsOnRobotOrPlayer(l))
                                    .OrderBy(l => l.transform.position.z).ThenBy(l => l.transform.position.x).ToList();
                var occluders = AllInScene<MeshRenderer>()
                    .Where(r => NameHas(r, OccluderWords))
                    .Select(r => r.bounds.center).ToList();
                foreach (int i in ShadowLightPicker.Pick(candidates.Select(l => l.transform.position).ToList(), occluders, budget / 6, 5f))
                    SoftShadow(candidates[i]);
            }
            foreach (var r in AllInScene<MeshRenderer>())
                if (NameHas(r, OccluderWords) && r.shadowCastingMode == ShadowCastingMode.Off)
                    r.shadowCastingMode = ShadowCastingMode.On;
        }

        private static int ShadowTiles(Light l) => l.type == LightType.Point ? 6 : 1;

        private static readonly string[] OccluderWords = { "Balda", "Caja", "Estanteria", "Contenedor", "Mesa", "Bidon", "Transformador", "Carcasa", "Tabique" };

        private static void SoftShadow(Light l)
        {
            l.shadows = LightShadows.Soft;
            l.shadowStrength = 0.85f;   // el sesgo lo pone URP (ajustes del pipeline)
            EditorUtility.SetDirty(l);
        }

        // =====================================================================
        //  UTILIDADES
        // =====================================================================

        /// <summary>Todos los componentes T de la escena activa, incluidos los de objetos inactivos.</summary>
        internal static List<T> AllInScene<T>() where T : Component
        {
            var list = new List<T>();
            foreach (var root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
                list.AddRange(root.GetComponentsInChildren<T>(true));
            return list;
        }
    }
}
