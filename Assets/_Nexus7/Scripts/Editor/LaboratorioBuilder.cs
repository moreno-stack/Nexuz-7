using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Nexus7.Core;
using Nexus7.Crafting;
using Nexus7.Devices;
using Nexus7.Enemies;
using Nexus7.Environment;
using Nexus7.Gameplay;
using Nexus7.Interaction;
using Nexus7.Inventory;
using Nexus7.Security;
using Nexus7.Stealth;
using Nexus7.Tutorial;
using A = Nexus7.EditorTools.AlmacenCargaBuilder;   // Fase 2
using B = Nexus7.EditorTools.SalaInicialBuilder;    // Fase 1
using S3 = Nexus7.EditorTools.SalaEnergiaBuilder;   // Fase 3

namespace Nexus7.EditorTools
{
    /// <summary>
    /// Menú "Nexus-7 > Fase 4 > Construir Laboratorio".
    ///
    /// Plano (vista cenital, Z = norte), 32 × 36 m, techo a 6 m:
    ///
    ///  z=40             [montacargas]
    ///  z=36 ═══════════════[PUERTA C]═════   PUERTA C = contención (tarjeta del director)
    ///       │DESPACHO │ R2 ← vigila →  ║láser║│   láser doble: hackear (H), PEM o camuflaje
    ///       │ tarjeta    ......         H     │
    ///       │ T          W3(inspección R1) RACK│  T = tanques de contención (cristal óptico)
    ///       │ T     mesa       mesa    cámara │  RACK = almacén químico (cámara C1)
    ///       │ T     W2(inspección R1)    RACK │
    ///  z=10 ═════════[ láser alto ]═══════════   pasar agachado
    ///       │  caja   W1 (banco seguro)       │   ZONA DE ACCESO
    ///  z=0  ═════════════[entrada]════════════
    /// </summary>
    public static class LaboratorioBuilder
    {
        private const string Root = "Assets/_Nexus7";
        private const string ScenePath = Root + "/Scenes/Laboratorio.unity";
        private const string Phase3ScenePath = Root + "/Scenes/SalaEnergia.unity";
        public const string SceneName = "Laboratorio";
        private const float H = 6f, T = 0.3f;
        private const int IgnoreRaycastLayer = 2;

        internal static Material labFloor, labWall, tableMat, liquidMat, rackMat, barrelMat, droneWhite, visorPurple, paperMat, crystalMat, labLamp;
        internal static readonly Color LabCold = new Color(0.65f, 0.82f, 1f);

        [MenuItem("Nexus-7/Fase 4/Construir Laboratorio")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            B.CreateFolders();
            B.EnsureFolder(Root + "/Data", "Recipes");
            var items = A.EnsureItems();
            var p4 = EnsurePhase4Data(items);
            var crystal = p4.Crystal; var keycard = p4.Keycard;
            var decoy = p4.Decoy; var emp = p4.Emp; var camo = p4.Camo; var recipes = p4.Recipes;

            // ---------- 2. Escena ----------
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            A.CreateMaterials();
            S3.CreateMaterials();
            CreateMaterials();
            ConfigureLighting();

            var env = new GameObject("Entorno").transform;
            var props = new GameObject("Mobiliario").transform;
            var gameplay = new GameObject("Gameplay").transform;
            var security = new GameObject("Seguridad").transform;
            var loot = new GameObject("Botin").transform;

            BuildShell(env);
            BuildOffice(env);

            // Bancos de trabajo (W1 seguro; W2 y W3 los revisa el dron de investigación)
            var w1 = WorkbenchAt("BancoTrabajo_Acceso", gameplay, new Vector3(8f, 0f, 5f), 0f);
            var w2 = WorkbenchAt("BancoTrabajo_Oeste", gameplay, new Vector3(-4f, 0f, 16f), 0f);
            var w3 = WorkbenchAt("BancoTrabajo_Norte", gameplay, new Vector3(4f, 0f, 24f), 0f);
            LabTable(props, new Vector3(4f, 0f, 16f));
            LabTable(props, new Vector3(-4f, 0f, 24f));
            foreach (var z in new[] { 14f, 19f, 24f }) Tank(props, new Vector3(-13.5f, 0f, z), z == 19f);
            foreach (var z in new[] { 14f, 20f, 26f }) Rack(props, new Vector3(14.6f, 0f, z));
            Clutter(props);

            // ---------- 3. Seguridad: láseres, cámara, terminal ----------
            Laser("SensorLaser_Acceso", security, -4f, 4f, 10f, new[] { 1.45f }, 1.3f, 1.6f);           // alto: pasa agachado
            var exitLaser = Laser("SensorLaser_Salida", security, 5f, 10f, 33f, new[] { 0.4f, 0.85f, 1.3f }, 0.25f, 1.45f);
            S3.Terminal("TerminalHackeo_Laser", security, new Vector3(3.2f, 0f, 33.4f), 0f, exitLaser, items.Circuit, 25f);
            var cam = S3.SecurityCam(security, new Vector3(15.5f, 0f, 30f), 210f);
            A.SetFloat(cam, "sweepAngle", 40f);

            // Puerta de contención: tarjeta del director (no se gasta)
            var exitDoor = A.SlidingDoor("PuertaContencion", gameplay, new Vector3(7.5f, 0f, 36f + T / 2f), new Vector3(3f, 3.5f, 0.2f),
                                         new Vector3(0f, 3.4f, 0f), A.metalMat, new Vector3(-2.05f, 2.4f, -0.3f));
            B.SetString(exitDoor, "lockedPrompt", "Lector de tarjeta");
            B.SetString(exitDoor, "lockedMessage", "PUERTA DE CONTENCIÓN: se requiere la tarjeta de acceso del director.");
            B.SetString(exitDoor, "unlockedWithItemsMessage", "Tarjeta aceptada. Puerta de contención abierta.");
            A.SetStacks(exitDoor, "requiredItems", new[] { new ItemStack(keycard, 1) });
            A.SetBool(exitDoor, "consumeItems", false);

            // ---------- 4. Botín ----------
            A.Container("Contenedor_Acceso", loot, new Vector3(-8f, 0f, 3f), 0f,
                new[] { new ItemStack(items.Battery, 1), new ItemStack(items.Circuit, 1) });
            A.Container("Contenedor_AlmacenQuimico", loot, new Vector3(11.6f, 0f, 17f), 90f,
                new[] { new ItemStack(items.Battery, 1), new ItemStack(items.Circuit, 2) });
            var officeBox = A.Container("Contenedor_Despacho", loot, new Vector3(-15.3f, 0f, 32.2f), -90f,
                new[] { new ItemStack(keycard, 1), new ItemStack(items.Circuit, 1) });
            foreach (var p in new[] { new Vector3(-4f, 0f, 6f), new Vector3(12f, 0f, 7.5f) })
                A.Pickup("Pickup_Chatarra", loot, p, items.Scrap, 2, PrimitiveType.Cube, new Vector3(0.35f, 0.1f, 0.25f), A.scrapMat, 3f, false);
            A.Pickup("Pickup_Chatarra", loot, new Vector3(6.3f, 0f, 22.4f), items.Scrap, 3, PrimitiveType.Cube, new Vector3(0.35f, 0.1f, 0.25f), A.scrapMat, 3f, false);
            foreach (var p in new[] { new Vector3(-11f, 0f, 16.5f), new Vector3(-11f, 0f, 26.5f) })
                A.Pickup("Pickup_CristalOptico", loot, p, crystal, 1, PrimitiveType.Capsule, new Vector3(0.12f, 0.18f, 0.12f), crystalMat, 0f, true);

            var spotGo = new GameObject("PuntoInspeccion_Despacho");
            spotGo.transform.SetParent(gameplay);
            spotGo.transform.position = new Vector3(-13.6f, 0f, 32.3f);
            var officeSpot = spotGo.AddComponent<InspectableSpot>();
            B.SetString(officeSpot, "label", "el despacho del director");
            B.SetRef(officeSpot, "watchedContainer", officeBox.GetComponent<LootContainer>());

            Note(env, "RegistroProyecto", new Vector3(9.6f, 1.05f, 5.2f), new[]
            {
                "PROYECTO ESPECTRO — Registro 14: «Los prototipos funcionan. El emisor sonoro desvía a los drones, pero los de investigación aprenden: no uses dos señuelos seguidos.»",
                "«El pulso PEM los deja fuera de servicio unos segundos. Al reiniciarse quedan muy alerta.»",
                "«El camuflaje óptico engaña a los sensores láser, pero no al oído: no corras.»"
            });
            Note(env, "DiarioDirector", new Vector3(-13f, 0.82f, 34.4f), new[]
            {
                "DIARIO DEL DIRECTOR: «He reprogramado los drones de investigación para revisar los bancos de trabajo en cada ronda. Nadie debe fabricar sin autorización.»",
                "«Guardo mi tarjeta en el armario del despacho. El dron de seguridad revisa el despacho periódicamente.»"
            });

            // ---------- 5. Jugador ----------
            var (player, interactor) = B.BuildPlayer();
            player.transform.SetPositionAndRotation(new Vector3(0f, 0.05f, -3.2f), Quaternion.identity);
            var inventory = B.GetOrAdd<InventorySystem>(player.gameObject);
            A.SetStacks(inventory, "startingItems", new[]   // solo si se juega esta escena directamente
            {
                new ItemStack(items.Scrap, 6), new ItemStack(items.Circuit, 2), new ItemStack(items.Battery, 1)
            });
            var stealth = player.gameObject.AddComponent<PlayerStealth>();
            player.gameObject.AddComponent<PlayerRespawn>();
            var crafting = player.gameObject.AddComponent<CraftingSystem>();
            A.SetObjectArray(crafting, "knownRecipes", recipes.Cast<Object>().ToArray());

            var trajGo = new GameObject("Trayectoria");
            trajGo.transform.SetParent(player.transform, false);
            var traj = trajGo.AddComponent<LineRenderer>();
            traj.useWorldSpace = true;
            traj.sharedMaterial = S3.arcMat;
            traj.widthMultiplier = 0.025f;
            traj.shadowCastingMode = ShadowCastingMode.Off;
            var quickbar = player.gameObject.AddComponent<DeviceQuickbar>();
            A.SetObjectArray(quickbar, "slots", new Object[] { decoy, emp, camo });
            B.SetRef(quickbar, "throwOrigin", interactor.transform);
            B.SetRef(quickbar, "trajectoryLine", traj);

            // ---------- 6. Drones ----------
            var enemies = new GameObject("Enemigos").transform;
            var routeR1 = A.Route("Ruta_R1_Bancos", enemies, PatrolRoute.RouteMode.Loop, new[]
            {
                new A.WP(new Vector3(-7f, 0f, 12.5f), 1f, false, 0f),
                new A.WP(new Vector3(-4f, 0f, 14.5f), 4f, true, 0f),     // inspecciona W2
                new A.WP(new Vector3(7f, 0f, 12.5f),  1f, false, 0f),
                new A.WP(new Vector3(7f, 0f, 22.5f),  1f, false, 0f),
                new A.WP(new Vector3(4f, 0f, 22.5f),  4f, true, 0f),     // inspecciona W3
                new A.WP(new Vector3(-7f, 0f, 22.5f), 1f, false, 0f),
            });
            B.SetRef(routeR1.transform.GetChild(1).GetComponent<Waypoint>(), "inspectTarget", w2);
            B.SetRef(routeR1.transform.GetChild(4).GetComponent<Waypoint>(), "inspectTarget", w3);

            var routeR2 = A.Route("Ruta_R2_Despacho", enemies, PatrolRoute.RouteMode.PingPong, new[]
            {
                new A.WP(new Vector3(7.5f, 0f, 31.2f),  3f, true, 180f),     // vigila la salida
                new A.WP(new Vector3(-6f, 0f, 33f),     1f, false, 0f),
                new A.WP(new Vector3(-13.6f, 0f, 32.3f), 4f, true, -90f),    // inspecciona el despacho
            });
            B.SetRef(routeR2.transform.GetChild(2).GetComponent<Waypoint>(), "inspectTarget", officeSpot);

            var r1 = ResearchDrone("DronInvestigacion_R1", enemies, new Vector3(-7f, 0f, 12.5f), 0f, routeR1, "Dron de investigación");
            var r2 = ResearchDrone("DronSeguridad_R2", enemies, new Vector3(7.5f, 0f, 31.2f), 180f, routeR2, "Dron de seguridad");

            var navGo = new GameObject("NavMesh");
            navGo.transform.position = new Vector3(0f, 3f, 18f);
            var baker = navGo.AddComponent<RuntimeNavMeshBaker>();
            A.SetVector3(baker, "size", new Vector3(36f, 8f, 48f));
            A.SetObjectArray(baker, "ignoreRoots", new Object[] { player.transform, r1.transform, r2.transform });

            // ---------- 7. Zonas ----------
            var zones = new GameObject("Zonas").transform;
            foreach (var z in new[]
            {
                new S3.Bx(new Vector3(-15.4f, 1.5f, 16.5f), new Vector3(1.2f, 3f, 2.4f)), new S3.Bx(new Vector3(-15.4f, 1.5f, 21.5f), new Vector3(1.2f, 3f, 2.4f)),
                new S3.Bx(new Vector3(12.4f, 1.5f, 23f), new Vector3(2.6f, 3f, 1.2f)),    new S3.Bx(new Vector3(-15f, 1.5f, 35f), new Vector3(1.8f, 3f, 1.8f)),
                new S3.Bx(new Vector3(4.2f, 1.5f, 35f), new Vector3(1.4f, 3f, 2.2f)),
            })
                A.Zone("ZonaSombra", zones, z.pos, z.size).AddComponent<ShadowZone>();

            S3.Checkpoint("Checkpoint_Laboratorio", zones, new Vector3(0f, 1.5f, 11.5f), new Vector3(8f, 3f, 1f));
            S3.Checkpoint("Checkpoint_Norte", zones, new Vector3(0f, 1.5f, 28.5f), new Vector3(14f, 3f, 1f));

            A.MessageZone("Aviso_LaserAlto", zones, new Vector3(0f, 1.5f, 8.3f), new Vector3(8f, 3f, 1.4f),
                "IA NEXUS-7: Sensor láser. Si lo cortas salta la alarma. Este haz está alto: pasa agachado [Ctrl].");
            A.MessageZone("Aviso_Banco", zones, new Vector3(8f, 1.5f, 2.6f), new Vector3(3f, 3f, 1.2f),
                "IA NEXUS-7: Un banco de trabajo. Aquí nadie lo revisa: es un buen sitio para fabricar tu primer dispositivo.");
            A.MessageZone("Aviso_Tanques", zones, new Vector3(-11f, 1.5f, 12.8f), new Vector3(3f, 3f, 1.2f),
                "IA NEXUS-7: Tanques del proyecto ESPECTRO. Junto a ellos hay cristales ópticos: necesarios para el camuflaje.");
            A.MessageZone("Aviso_Almacen", zones, new Vector3(12f, 1.5f, 11f), new Vector3(4f, 3f, 1.2f),
                "IA NEXUS-7: Almacén químico vigilado por una cámara. Un PEM la apagaría unos segundos.");
            A.MessageZone("Aviso_Salida", zones, new Vector3(7.5f, 1.5f, 29.8f), new Vector3(5f, 3f, 1.2f),
                "IA NEXUS-7: Sensores láser dobles: no se pueden esquivar. Hackéalos, apágalos con un PEM o crúzalos con el camuflaje óptico.");

            var exit = A.Zone("Salida_Montacargas", zones, new Vector3(7.5f, 1.5f, 38.3f), new Vector3(3f, 3f, 3f));
            var exitMsg = exit.AddComponent<TutorialTrigger>();
            B.SetStrings(exitMsg, "messages", new[] { "IA NEXUS-7: Has salido del laboratorio. Siguiente parada: el puente de mando." });
            var exitTrig = exit.AddComponent<SceneExitTrigger>();
            B.SetString(exitTrig, "sceneName", "CentroControl"); // Fase 5
            B.SetString(exitTrig, "fallbackMessage", "— FIN DE LA FASE 4 —");

            // ---------- 8. HUD ----------
            var hud = B.BuildHUD();
            A.BuildPhase2UI(hud.transform, inventory, stealth);
            BuildPhase4UI(hud.transform, quickbar, stealth);

            var systems = new GameObject("GameSystems");
            var intro = systems.AddComponent<LevelIntro>();
            B.SetRef(intro, "player", player);
            B.SetRef(intro, "hud", hud);
            B.SetStrings(intro, "messages", new[]
            {
                "IA NEXUS-7: Laboratorio de investigación del Módulo C. Abandonado desde la brecha.",
                "IA NEXUS-7: Aquí se desarrollaba el proyecto ESPECTRO: señuelos, pulsos PEM y camuflaje óptico. Puedes fabricarlos en los bancos de trabajo.",
                "IA NEXUS-7: Los drones de investigación revisan los bancos en su ronda. Si alguien los ha usado hace poco, darán la alarma.",
                "IA NEXUS-7: La puerta de contención exige la tarjeta del director. Su despacho está al noroeste."
            });
            B.SetString(intro, "hint", "[E] en un banco para fabricar · [1][2][3] elegir dispositivo · mantén [G] para apuntar y suelta para lanzar");
            B.SetString(intro, "objective", "Objetivo: Consigue la tarjeta del director (despacho, noroeste) y cruza la puerta de contención (norte).");

            // ---------- 9. Prefabs, guardado y enlace con la Fase 3 ----------
            B.SavePrefab(w1.gameObject, "BancoTrabajo");
            B.SavePrefab(exitLaser.gameObject, "SensorLaser");
            B.SavePrefab(r1.gameObject, "DronInvestigacion");
            B.SavePrefab(player.gameObject, "Player_Fase4");
            B.SavePrefab(hud.gameObject, "HUD_Fase4");

            EditorSceneManager.SaveScene(scene, ScenePath);
            var scenes = EditorBuildSettings.scenes.ToList();
            if (!scenes.Any(s => s.path == ScenePath)) scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            ConnectPhase3();

            Selection.activeGameObject = player.gameObject;
            Debug.Log("<color=cyan>[Nexus-7]</color> Laboratorio construido en " + ScenePath + ". Pulsa Play.");
        }

        /// <summary>Objetos, dispositivos y recetas de la Fase 4 (crea o actualiza los assets). Reutilizado por fases posteriores.</summary>
        internal class Phase4Data
        {
            public ItemData Crystal, Keycard;
            public DeviceItemData Decoy, Emp, Camo;
            public CraftingRecipe[] Recipes;
        }

        internal static Phase4Data EnsurePhase4Data(AlmacenCargaBuilder.ItemSet items)
        {
            B.EnsureFolder(Root + "/Data", "Recipes");
            var d = new Phase4Data();
            d.Crystal = A.CreateItem("Item_CristalOptico", "component_optic_crystal", "Cristal óptico",
                "Cristal refractivo del proyecto ESPECTRO. Desvía la luz alrededor de un objeto.", ItemCategory.Component, 5, false, new Color(0.75f, 0.55f, 1f));
            d.Keycard = A.CreateItem("Item_TarjetaDirector", "key_director_card", "Tarjeta del director",
                "Tarjeta de acceso del director del laboratorio. Abre la puerta de contención.", ItemCategory.Tool, 1, true, new Color(1f, 0.85f, 0.35f));

            d.Decoy = CreateDevice("Item_SenueloSonoro", "device_sonic_decoy", "Señuelo sonoro",
                "Emisor de pulsos sonoros. Lánzalo: atrae a los drones durante unos segundos.",
                DeviceKind.SonicDecoy, DeviceUseMode.Throw, 1, 12f, 10f, 14f, 1.2f, new Color(1f, 0.6f, 0.15f));
            d.Emp = CreateDevice("Item_PulsoPEM", "device_emp", "Pulso PEM",
                "Granada electromagnética. Aturde drones y apaga cámaras, barreras y láseres cercanos.",
                DeviceKind.EmpPulse, DeviceUseMode.Throw, 2, 11f, 6f, 6f, 0f, new Color(0.3f, 0.85f, 1f));
            d.Camo = CreateDevice("Item_CamuflajeOptico", "device_optical_camo", "Camuflaje óptico",
                "Reduce drásticamente tu visibilidad durante unos segundos. Los sensores láser no te detectan.",
                DeviceKind.OpticalCamo, DeviceUseMode.SelfActivate, 3, 0f, 12f, 0f, 0.1f, new Color(0.75f, 0.55f, 1f));

            d.Recipes = new[]
            {
                CreateRecipe("Receta_SenueloSonoro", "Emisor de pulso sonoro", "Señuelo que emite pulsos de ruido durante 10 s (radio 14 m).",
                    new[] { new ItemStack(items.Scrap, 3), new ItemStack(items.Circuit, 1) }, d.Decoy, 1, 2f),
                CreateRecipe("Receta_PulsoPEM", "Pulso PEM temporal", "Aturde 6 s a los drones en 6 m y apaga cámaras, barreras y láseres.",
                    new[] { new ItemStack(items.Battery, 1), new ItemStack(items.Circuit, 1), new ItemStack(items.Scrap, 2) }, d.Emp, 1, 3f),
                CreateRecipe("Receta_CamuflajeOptico", "Camuflaje óptico temporal", "Visibilidad al 10 % durante 12 s. Invisible para los sensores láser.",
                    new[] { new ItemStack(items.Battery, 1), new ItemStack(d.Crystal, 1), new ItemStack(items.Circuit, 1) }, d.Camo, 1, 3.5f),
            };
            AssetDatabase.SaveAssets();
            return d;
        }

        [MenuItem("Nexus-7/Fase 4/Conectar salida de la Fase 3")]
        public static void ConnectPhase3()
        {
            if (!System.IO.File.Exists(Phase3ScenePath))
            {
                Debug.Log("[Nexus-7] No existe SalaEnergia.unity: ejecuta 'Fase 3 > Construir Sala de Energía' si quieres enlazarlas.");
                return;
            }
            string current = EditorSceneManager.GetActiveScene().path;
            var phase3 = EditorSceneManager.OpenScene(Phase3ScenePath, OpenSceneMode.Single);
            var exit = GameObject.Find("Salida_Montacargas");
            var trig = exit != null ? exit.GetComponent<SceneExitTrigger>() : null;
            if (trig != null)
            {
                B.SetString(trig, "sceneName", SceneName);
                EditorSceneManager.SaveScene(phase3);
                Debug.Log("[Nexus-7] Sala de Energía enlazada con " + SceneName + ".");
            }
            if (!string.IsNullOrEmpty(current) && current != Phase3ScenePath)
                EditorSceneManager.OpenScene(current, OpenSceneMode.Single);
        }

        // =====================================================================
        //  DATOS: dispositivos y recetas
        // =====================================================================

        internal static DeviceItemData CreateDevice(string file, string id, string displayName, string description,
            DeviceKind kind, DeviceUseMode mode, int slot, float force, float duration, float radius, float effect, Color color)
        {
            string path = $"{Root}/Data/Items/{file}.asset";
            var item = AssetDatabase.LoadAssetAtPath<DeviceItemData>(path);
            if (item == null)
            {
                item = ScriptableObject.CreateInstance<DeviceItemData>();
                AssetDatabase.CreateAsset(item, path);
            }
            var so = new SerializedObject(item);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("description").stringValue = description;
            so.FindProperty("category").enumValueIndex = (int)ItemCategory.Device;
            so.FindProperty("maxStack").intValue = 5;
            so.FindProperty("uiColor").colorValue = color;
            so.FindProperty("deviceType").enumValueIndex = (int)kind;
            so.FindProperty("useMode").enumValueIndex = (int)mode;
            so.FindProperty("quickSlot").intValue = slot;
            so.FindProperty("throwForce").floatValue = force;
            so.FindProperty("duration").floatValue = duration;
            so.FindProperty("radius").floatValue = radius;
            so.FindProperty("effectValue").floatValue = effect;
            so.FindProperty("deviceColor").colorValue = color;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(item);
            return item;
        }

        internal static CraftingRecipe CreateRecipe(string file, string displayName, string description,
            ItemStack[] ingredients, ItemData result, int amount, float time)
        {
            string path = $"{Root}/Data/Recipes/{file}.asset";
            var recipe = AssetDatabase.LoadAssetAtPath<CraftingRecipe>(path);
            if (recipe == null)
            {
                recipe = ScriptableObject.CreateInstance<CraftingRecipe>();
                AssetDatabase.CreateAsset(recipe, path);
            }
            B.SetString(recipe, "displayName", displayName);
            B.SetString(recipe, "description", description);
            A.SetStacks(recipe, "ingredients", ingredients);
            B.SetRef(recipe, "result", result);
            A.SetInt(recipe, "resultAmount", amount);
            A.SetFloat(recipe, "craftTime", time);
            EditorUtility.SetDirty(recipe);
            return recipe;
        }

        // =====================================================================
        //  ESTRUCTURA
        // =====================================================================

        internal static void BuildShell(Transform env)
        {
            A.Tiled("Suelo", env, new Vector3(0f, -0.1f, 18f), new Vector3(32.6f, 0.2f, 36.6f), labFloor);
            A.Tiled("Techo", env, new Vector3(0f, H + 0.1f, 18f), new Vector3(32.6f, 0.2f, 36.6f), labWall);
            A.Tiled("MuroOeste", env, new Vector3(-16f - T / 2, H / 2, 18f), new Vector3(T, H, 36.6f), labWall);
            A.Tiled("MuroEste", env, new Vector3(16f + T / 2, H / 2, 18f), new Vector3(T, H, 36.6f), labWall);
            A.Tiled("MuroSur_Izq", env, new Vector3(-8.9f, H / 2, -T / 2), new Vector3(14.8f, H, T), labWall);
            A.Tiled("MuroSur_Der", env, new Vector3(8.9f, H / 2, -T / 2), new Vector3(14.8f, H, T), labWall);
            B.Box("MuroSur_Dintel", env, new Vector3(0f, 4.5f, -T / 2), new Vector3(3f, 3f, T), labWall);
            A.Tiled("MuroNorte_Izq", env, new Vector3(-5.15f, H / 2, 36f + T / 2), new Vector3(22.3f, H, T), labWall);
            A.Tiled("MuroNorte_Der", env, new Vector3(12.65f, H / 2, 36f + T / 2), new Vector3(7.3f, H, T), labWall);
            B.Box("MuroNorte_Dintel", env, new Vector3(7.5f, 4.75f, 36f + T / 2), new Vector3(3f, 2.5f, T), labWall);

            // Vestíbulo de llegada y montacargas de salida
            B.Box("Vest_Suelo", env, new Vector3(0f, -0.1f, -2.5f), new Vector3(5.4f, 0.2f, 5f), labFloor);
            B.Box("Vest_Techo", env, new Vector3(0f, 4.1f, -2.5f), new Vector3(5.4f, 0.2f, 5f), labWall);
            B.Box("Vest_MuroO", env, new Vector3(-2.6f, 2f, -2.5f), new Vector3(0.2f, 4f, 5f), labWall);
            B.Box("Vest_MuroE", env, new Vector3(2.6f, 2f, -2.5f), new Vector3(0.2f, 4f, 5f), labWall);
            B.Box("Vest_MuroS", env, new Vector3(0f, 2f, -5.1f), new Vector3(5.4f, 4f, 0.2f), labWall);
            B.Box("Salida_Suelo", env, new Vector3(7.5f, -0.1f, 38.4f), new Vector3(4.4f, 0.2f, 4.4f), labFloor);
            B.Box("Salida_Techo", env, new Vector3(7.5f, 4.1f, 38.4f), new Vector3(4.4f, 0.2f, 4.4f), labWall);
            B.Box("Salida_MuroO", env, new Vector3(5.3f, 2f, 38.4f), new Vector3(0.2f, 4f, 4.4f), labWall);
            B.Box("Salida_MuroE", env, new Vector3(9.7f, 2f, 38.4f), new Vector3(0.2f, 4f, 4.4f), labWall);
            B.Box("Salida_MuroN", env, new Vector3(7.5f, 2f, 40.7f), new Vector3(4.4f, 4f, 0.2f), labWall);

            // Mampara de cuarentena (z=10) con paso central de 8 m vigilado por un láser alto
            B.Box("Mampara_Oeste", env, new Vector3(-10f, 1.75f, 10f), new Vector3(12f, 3.5f, 0.3f), labWall);
            B.Box("Mampara_Este", env, new Vector3(10f, 1.75f, 10f), new Vector3(12f, 3.5f, 0.3f), labWall);
            B.NoCollider(B.Box("FranjaCuarentena", env, new Vector3(0f, 0.01f, 9.6f), new Vector3(8f, 0.02f, 0.3f), A.warningMat));

            // Antesala de la salida (paredes bajas que obligan a cruzar los láseres)
            B.Box("Antesala_O", env, new Vector3(5f, 1.5f, 34.5f), new Vector3(0.2f, 3f, 3f), labWall);
            B.Box("Antesala_E", env, new Vector3(10f, 1.5f, 34.5f), new Vector3(0.2f, 3f, 3f), labWall);

            // Conductos y luces
            foreach (float x in new[] { -15.5f, 15.5f })
                B.NoCollider(B.Box("Conducto", env, new Vector3(x, 5.3f, 18f), new Vector3(0.4f, 0.4f, 36f), S3.cableMat));
            BuildLights(env);
        }

        internal static void BuildOffice(Transform env)
        {
            var root = new GameObject("DespachoDirector").transform;
            root.SetParent(env);
            B.Box("Pared_S", root, new Vector3(-12f, 1.5f, 30f), new Vector3(8f, 3f, 0.2f), labWall);
            B.Box("Pared_E1", root, new Vector3(-8f, 1.5f, 31f), new Vector3(0.2f, 3f, 2f), labWall);
            B.Box("Pared_E2", root, new Vector3(-8f, 1.5f, 35f), new Vector3(0.2f, 3f, 2f), labWall);
            B.Box("Escritorio", root, new Vector3(-13f, 0.4f, 34.6f), new Vector3(2.2f, 0.8f, 1f), tableMat);
            var chair = B.Box("SillaVolcada", root, new Vector3(-11.6f, 0.25f, 33.4f), new Vector3(0.5f, 0.5f, 0.5f), rackMat);
            chair.transform.rotation = Quaternion.Euler(80f, 30f, 0f);
            var screen = B.NoCollider(B.Box("Monitor", root, new Vector3(-13.4f, 1.1f, 34.95f), new Vector3(0.7f, 0.45f, 0.05f), S3.screenMat));
            screen.AddComponent<BlinkingScreen>();
        }

        internal static void BuildLights(Transform env)
        {
            var root = new GameObject("IluminacionLaboratorio").transform;
            root.SetParent(env);
            var lamps = new[]
            {
                new Vector4(-9f, 5.7f, 5f, 0), new Vector4(9f, 5.7f, 5f, 0), new Vector4(0f, 5.7f, 13f, 1),
                new Vector4(-8f, 5.7f, 19f, 0), new Vector4(8f, 5.7f, 19f, 0), new Vector4(0f, 5.7f, 25f, 0),
                new Vector4(-8f, 5.7f, 27f, 1), new Vector4(8f, 5.7f, 29f, 0), new Vector4(7.5f, 5.7f, 33.5f, 0),
            };
            for (int i = 0; i < lamps.Length; i++)
            {
                var pos = new Vector3(lamps[i].x, lamps[i].y, lamps[i].z);
                var fixture = B.NoCollider(B.Box("Fluorescente", root, pos, new Vector3(2.2f, 0.08f, 0.35f), labLamp));
                var l = A.PointLight("Luz", root, pos + Vector3.down * 0.3f, LabCold, 10f, 1.5f, i == 2 || i == 5);
                if (lamps[i].w > 0f)
                {
                    var fl = l.gameObject.AddComponent<EmergencyLight>();
                    B.SetRef(fl, "emissiveRenderer", fixture.GetComponent<Renderer>());
                    A.SetColor(fl, "emissionColor", LabCold);
                    A.SetFloat(fl, "baseIntensity", 1.5f);
                    A.SetFloat(fl, "emissionStrength", 2f);
                    A.SetFloat(fl, "flickerAmount", 0.6f);
                }
            }
            A.PointLight("LuzDespacho", root, new Vector3(-12f, 2.8f, 33f), new Color(1f, 0.8f, 0.6f), 6f, 0.7f, false);
            A.PointLight("LuzVestibulo", root, new Vector3(0f, 3.7f, -2.5f), LabCold, 6f, 0.8f, false);
            A.PointLight("LuzSalida", root, new Vector3(7.5f, 3.7f, 38.4f), LabCold, 6f, 0.8f, false);
        }

        internal static void ConfigureLighting()
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.035f, 0.05f, 0.07f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.02f, 0.03f, 0.045f);
            RenderSettings.fogDensity = 0.04f;
        }

        // =====================================================================
        //  MOBILIARIO
        // =====================================================================

        internal static Workbench WorkbenchAt(string name, Transform parent, Vector3 pos, float rotY)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent);
            root.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, rotY, 0f));
            Local(B.Box("Cuerpo", root.transform, Vector3.zero, new Vector3(2.4f, 0.9f, 1.1f), rackMat), new Vector3(0f, 0.45f, 0f));
            Local(B.Box("Tablero", root.transform, Vector3.zero, new Vector3(2.5f, 0.06f, 1.2f), tableMat), new Vector3(0f, 0.93f, 0f));
            var monitor = Local(B.NoCollider(B.Box("Monitor", root.transform, Vector3.zero, new Vector3(0.8f, 0.5f, 0.05f), S3.screenMat)), new Vector3(0.6f, 1.3f, 0.45f));
            Local(B.NoCollider(B.Box("Herramienta", root.transform, Vector3.zero, new Vector3(0.4f, 0.08f, 0.15f), A.warningMat)), new Vector3(-0.6f, 1f, -0.2f));
            Local(B.NoCollider(B.Box("Placa", root.transform, Vector3.zero, new Vector3(0.3f, 0.03f, 0.25f), A.circuitMat)), new Vector3(-0.1f, 0.97f, 0.1f));
            var point = new GameObject("PuntoInspeccion").transform;
            point.SetParent(root.transform, false);
            point.localPosition = new Vector3(0f, 0f, -1.5f);

            // Cartel luminoso «BANCO DE TRABAJO» (se ve de lejos en la penumbra)
            var signMat = B.CreateMat("MB_CartelBanco", new Color(1f, 0.65f, 0.15f), new Color(1f, 0.6f, 0.1f) * 1.6f);
            Local(B.NoCollider(B.Box("Cartel", root.transform, Vector3.zero, new Vector3(1.5f, 0.32f, 0.04f), signMat)), new Vector3(0f, 1.9f, 0.5f));
            var textGo = new GameObject("TextoCartel");
            textGo.transform.SetParent(root.transform, false);
            textGo.transform.localPosition = new Vector3(0f, 1.9f, 0.47f);
            // Sin girar: el texto TMP se lee desde −Z local, el lado del punto de inspección (donde está el jugador)
            var sign = textGo.AddComponent<TextMeshPro>();
            sign.text = "BANCO DE TRABAJO";
            sign.fontSize = 2.2f;
            sign.alignment = TextAlignmentOptions.Center;
            sign.color = new Color(0.08f, 0.05f, 0.02f);
            sign.rectTransform.sizeDelta = new Vector2(1.45f, 0.3f);
            A.PointLight("LuzCartel", root.transform, new Vector3(0f, 1.9f, 0.1f), new Color(1f, 0.65f, 0.2f), 3f, 1.2f, false);

            var bench = root.AddComponent<Workbench>();
            B.SetRef(bench, "inspectionPoint", point);
            B.SetRef(bench, "screenRenderer", monitor.GetComponent<Renderer>());
            return bench;
        }

        internal static void LabTable(Transform parent, Vector3 pos)
        {
            var root = new GameObject("MesaLaboratorio").transform;
            root.SetParent(parent);
            root.localPosition = pos;
            Local(B.Box("Mesa", root, Vector3.zero, new Vector3(2.4f, 0.95f, 1.1f), tableMat), new Vector3(0f, 0.475f, 0f));
            var screen = Local(B.NoCollider(B.Box("Terminal", root, Vector3.zero, new Vector3(0.6f, 0.4f, 0.05f), S3.screenMat)), new Vector3(-0.5f, 1.25f, 0.4f));
            screen.AddComponent<BlinkingScreen>();
            for (int i = 0; i < 3; i++)
            {
                var beaker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                beaker.name = "Probeta";
                Object.DestroyImmediate(beaker.GetComponent<Collider>());
                beaker.transform.SetParent(root, false);
                beaker.transform.localPosition = new Vector3(0.3f + i * 0.25f, 1.05f, -0.1f);
                beaker.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                beaker.GetComponent<Renderer>().sharedMaterial = i == 1 ? crystalMat : liquidMat;
            }
            for (int i = 0; i < 4; i++)
            {
                var paper = B.NoCollider(B.Box("Documento", root, Vector3.zero, new Vector3(0.21f, 0.005f, 0.3f), paperMat));
                paper.transform.localPosition = new Vector3(-1f + i * 0.6f, 0.003f, -0.9f - (i % 2) * 0.3f);
                paper.transform.localRotation = Quaternion.Euler(0f, i * 37f, 0f);
            }
        }

        internal static void Tank(Transform parent, Vector3 pos, bool broken)
        {
            var root = new GameObject(broken ? "TanqueContencion_Averiado" : "TanqueContencion").transform;
            root.SetParent(parent);
            root.localPosition = pos;
            Cyl("Base", root, new Vector3(0f, 0.15f, 0f), new Vector3(2.2f, 0.15f, 2.2f), rackMat, true);
            var liquid = Cyl("Liquido", root, new Vector3(0f, 1.9f, 0f), new Vector3(1.8f, 1.6f, 1.8f), liquidMat, true);
            Cyl("Tapa", root, new Vector3(0f, 3.6f, 0f), new Vector3(2.2f, 0.1f, 2.2f), rackMat, false);
            Local(B.NoCollider(B.Box("Tuberia", root, Vector3.zero, new Vector3(0.25f, 2.3f, 0.25f), S3.cableMat)), new Vector3(0f, 4.85f, 0f));

            var l = A.PointLight("LuzTanque", root, new Vector3(1.4f, 1.9f, 0f), new Color(0.3f, 1f, 0.8f), 5f, 1.5f, false);
            var fx = l.gameObject.AddComponent<EmergencyLight>();
            B.SetEnum(fx, "mode", (int)(broken ? EmergencyLight.Mode.Flicker : EmergencyLight.Mode.Pulse));
            B.SetRef(fx, "emissiveRenderer", liquid.GetComponent<Renderer>());
            A.SetColor(fx, "emissionColor", new Color(0.3f, 1f, 0.8f));
            A.SetFloat(fx, "baseIntensity", 1.5f);
            A.SetFloat(fx, "emissionStrength", 1.5f);
            A.SetFloat(fx, "pulseFrequency", 0.3f);
            if (broken) S3.Sparks("ChispasTanque", root, new Vector3(1f, 3.2f, 0f));
        }

        internal static void Rack(Transform parent, Vector3 pos)
        {
            var root = new GameObject("EstanteriaQuimica").transform;
            root.SetParent(parent);
            root.localPosition = pos;
            Local(B.Box("Estructura", root, Vector3.zero, new Vector3(1.6f, 2.6f, 4.5f), rackMat), new Vector3(0f, 1.3f, 0f));
            for (int i = 0; i < 3; i++)
            {
                var b = Cyl("Bidon", root, new Vector3(-1.8f, 0.45f, -1.4f + i * 1.4f), new Vector3(0.6f, 0.45f, 0.6f), barrelMat, true);
                if (i == 1) b.transform.localRotation = Quaternion.Euler(90f, 0f, 20f);   // bidón volcado
            }
        }

        internal static void Clutter(Transform parent)
        {
            // Atmósfera de abandono: papeles y cajas por el suelo
            var rng = new System.Random(4);
            for (int i = 0; i < 18; i++)
            {
                float x = (float)rng.NextDouble() * 20f - 10f, z = 11f + (float)rng.NextDouble() * 18f;
                var p = B.NoCollider(B.Box("PapelSuelto", parent, new Vector3(x, 0.003f, z), new Vector3(0.21f, 0.005f, 0.3f), paperMat));
                p.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
            }
            var box = B.Box("CajaMuestras", parent, new Vector3(-12f, 0.3f, 6f), new Vector3(0.8f, 0.6f, 0.6f), A.crateGreen);
            box.transform.rotation = Quaternion.Euler(0f, 25f, 0f);
        }

        internal static GameObject Cyl(string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat, bool collider)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        internal static GameObject Local(GameObject go, Vector3 localPos)
        {
            go.transform.localPosition = localPos;
            return go;
        }

        internal static void Note(Transform parent, string name, Vector3 pos, string[] messages)
        {
            var go = B.Box(name, parent, pos, new Vector3(0.3f, 0.02f, 0.4f), paperMat);
            var msg = go.AddComponent<InteractableMessage>();
            B.SetString(msg, "promptText", "Leer documento");
            B.SetStrings(msg, "messages", messages);
        }

        // =====================================================================
        //  SEGURIDAD Y DRONES
        // =====================================================================

        internal static LaserTripwire Laser(string name, Transform parent, float x0, float x1, float z, float[] heights, float zoneMin, float zoneMax)
        {
            float width = x1 - x0, cy = (zoneMin + zoneMax) / 2f;
            var root = new GameObject(name) { layer = IgnoreRaycastLayer };
            root.transform.SetParent(parent);
            root.transform.localPosition = new Vector3((x0 + x1) / 2f, cy, z);
            var zone = root.AddComponent<BoxCollider>();
            zone.isTrigger = true;
            zone.size = new Vector3(width, zoneMax - zoneMin, 0.3f);

            float postH = heights.Max() + 0.4f;
            foreach (float px in new[] { x0, x1 })
                B.Box("Poste", parent, new Vector3(px, postH / 2f, z), new Vector3(0.12f, postH, 0.12f), A.metalMat);

            var arcs = new List<Object>();
            foreach (float h in heights)
            {
                var arc = S3.Arc("Haz", root.transform, new Vector3(-width / 2f + 0.06f, h - cy, 0f), new Vector3(width - 0.12f, 0f, 0f), 0f, 0.02f);
                var lr = arc.GetComponent<LineRenderer>();
                lr.startColor = lr.endColor = new Color(1f, 0.1f, 0.1f);
                arcs.Add(arc);
            }
            var light = A.PointLight("LuzLaser", root.transform, new Vector3(0f, 0f, -0.3f), new Color(1f, 0.15f, 0.1f), 4f, 1.5f, false);

            var laser = root.AddComponent<LaserTripwire>();
            A.SetObjectArray(laser, "beams", arcs.ToArray());
            B.SetRef(laser, "beamLight", light);
            return laser;
        }

        internal static EnemyPatrol ResearchDrone(string name, Transform parent, Vector3 pos, float rotY, PatrolRoute route, string label)
        {
            var drone = A.Drone(name, parent, pos, rotY, route);
            var body = drone.transform.Find("Cuerpo");
            if (body != null) body.GetComponent<Renderer>().sharedMaterial = droneWhite;
            var visor = drone.transform.Find("Visor");
            if (visor != null) visor.GetComponent<Renderer>().sharedMaterial = visorPurple;

            // Foco de escaneo (morado, hacia abajo): se enciende al inspeccionar
            var scanGo = new GameObject("FocoEscaneo");
            scanGo.transform.SetParent(drone.transform, false);
            scanGo.transform.localPosition = new Vector3(0f, 1.3f, 0.45f);
            scanGo.transform.localRotation = Quaternion.Euler(55f, 0f, 0f);
            var scan = scanGo.AddComponent<Light>();
            scan.type = LightType.Spot;
            scan.spotAngle = 55f;
            scan.range = 4f;
            scan.intensity = 4f;
            scan.color = new Color(0.75f, 0.35f, 1f);

            // Efecto de aturdimiento (PEM): luz azul + arco eléctrico sobre la cabeza
            var stun = new GameObject("EfectoAturdido");
            stun.transform.SetParent(drone.transform, false);
            stun.transform.localPosition = new Vector3(0f, 2.1f, 0f);
            A.PointLight("LuzAturdido", stun.transform, Vector3.zero, new Color(0.3f, 0.8f, 1f), 3f, 2f, false);
            S3.Arc("ArcoAturdido", stun.transform, new Vector3(-0.35f, 0f, 0f), new Vector3(0.7f, 0f, 0f), 0.2f, 0.03f);

            B.SetRef(drone, "scanLight", scan);
            B.SetRef(drone, "stunEffect", stun);
            B.SetString(drone, "displayName", label);
            A.SetFloat(drone, "hearingMultiplier", 1.2f);   // sensores más finos
            return drone;
        }

        // =====================================================================
        //  UI DE LA FASE 4
        // =====================================================================

        internal static void BuildPhase4UI(Transform canvas, DeviceQuickbar quickbar, PlayerStealth stealth)
        {
            // La barra rápida y el camuflaje ya los muestra HotbarUI (HUD común de BuildHUD).
            // Panel de fabricación
            var panel = B.UIImage("PanelFabricacion", canvas, new Color(0.02f, 0.05f, 0.09f, 0.92f));
            B.Anchor(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(980f, 580f));
            var list = B.UIText("ListaRecetas", panel.transform, new Vector2(0.5f, 0.5f), new Vector2(-235f, 10f), new Vector2(450f, 520f), 26f, TextAlignmentOptions.TopLeft, Color.white);
            var details = B.UIText("DetalleReceta", panel.transform, new Vector2(0.5f, 0.5f), new Vector2(235f, 10f), new Vector2(450f, 520f), 23f, TextAlignmentOptions.TopLeft, Color.white);
            var (fill, bg) = S3.Bar("ProgresoFabricacion", panel.transform, new Vector2(0.5f, 0f), new Vector2(-150f, 24f));
            fill.color = new Color(0.4f, 1f, 0.6f);
            var cUi = canvas.gameObject.AddComponent<UI.CraftingUI>();
            B.SetRef(cUi, "panel", panel.gameObject);
            B.SetRef(cUi, "recipeList", list);
            B.SetRef(cUi, "details", details);
            B.SetRef(cUi, "progressRoot", bg.gameObject);
            B.SetRef(cUi, "progressFill", fill);
        }

        // =====================================================================
        //  MATERIALES
        // =====================================================================

        internal static void CreateMaterials()
        {
            labFloor    = B.CreateMat("M4_SueloLaboratorio", new Color(0.42f, 0.46f, 0.5f), null, 0.7f, 0.2f);
            labWall     = B.CreateMat("M4_ParedLaboratorio", new Color(0.62f, 0.67f, 0.72f), null, 0.5f, 0.1f);
            tableMat    = B.CreateMat("M4_MesaAcero", new Color(0.75f, 0.78f, 0.8f), null, 0.75f, 0.9f);
            liquidMat   = B.CreateMat("M4_LiquidoTanque", new Color(0.2f, 0.9f, 0.7f), new Color(0.2f, 1f, 0.75f) * 1.5f, 0.95f, 0f);
            rackMat     = B.CreateMat("M4_Estructura", new Color(0.28f, 0.32f, 0.36f), null, 0.4f, 0.7f);
            barrelMat   = B.CreateMat("M4_BidonQuimico", new Color(0.85f, 0.65f, 0.1f), null, 0.4f, 0.3f);
            droneWhite  = B.CreateMat("M4_DronInvestigacion", new Color(0.88f, 0.9f, 0.92f), null, 0.8f, 0.3f);
            visorPurple = B.CreateMat("M4_VisorMorado", new Color(0.7f, 0.3f, 1f), new Color(0.7f, 0.3f, 1f) * 3f);
            paperMat    = B.CreateMat("M4_Papel", new Color(0.9f, 0.9f, 0.85f), null, 0.1f, 0f);
            crystalMat  = B.CreateMat("M4_CristalOptico", new Color(0.75f, 0.55f, 1f), new Color(0.75f, 0.55f, 1f) * 2f, 0.95f, 0f);
            labLamp     = B.CreateMat("M4_Fluorescente", Color.black, LabCold * 2.5f);
        }
    }
}
