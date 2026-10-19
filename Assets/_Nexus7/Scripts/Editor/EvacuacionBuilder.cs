using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Nexus7.Core;
using Nexus7.Crafting;
using Nexus7.Devices;
using Nexus7.Enemies;
using Nexus7.Environment;
using Nexus7.Gameplay;
using Nexus7.Hazards;
using Nexus7.Interaction;
using Nexus7.Inventory;
using Nexus7.Player;
using Nexus7.Security;
using Nexus7.Stealth;
using Nexus7.Tutorial;
using A = Nexus7.EditorTools.AlmacenCargaBuilder;    // Fase 2
using B = Nexus7.EditorTools.SalaInicialBuilder;     // Fase 1
using S3 = Nexus7.EditorTools.SalaEnergiaBuilder;    // Fase 3
using L4 = Nexus7.EditorTools.LaboratorioBuilder;    // Fase 4
using C5 = Nexus7.EditorTools.CentroControlBuilder;  // Fase 5

namespace Nexus7.EditorTools
{
    /// <summary>
    /// Menú "Nexus-7 > Fase 6 > ...": construye la Sala de Evacuación, el Menú Principal y deja el juego cerrado
    /// (Build Settings ordenados y Centro de Control enlazado con la evacuación).
    ///
    /// Plano del hangar (vista cenital, Z = norte), 44 × 40 m, techo a 14 m:
    ///
    ///  z=40 ┌──────[cápsula A ✖]────────[cápsula B ✔]────────[cápsula C ✖]──────┐
    ///  z=37 │  ═════════════ PASARELA DE EMBARQUE (y=4.2) ══ celda ignición ═ 📷 ═ │ ← dron de seguridad (vaivén)
    ///  z=34 │       ╱rampa O (láser)                       rampa E (cables)╲       │
    ///  z=26 │                                                                      │ V
    ///       │ ┌CABINA┐     ⟲ élite ──────────────────── élite ⟲                    │ E
    ///       │ │consola│         │   LANZADERA SINIESTRADA  │                        │ N
    ///       │ │abrazad│         │   (cobertura central)    │                        │ T
    ///       │ └───────┘         └──────────────────────────┘                        │ A
    ///  z=8  │                                                                      │ N
    ///  z=0  └──────────────────────────────[entrada]─────────────────────────────────┘ A L  (x=22: espacio exterior)
    ///                                       vestíbulo
    /// Secuencia de lanzamiento de la cápsula B: 1) liberar abrazaderas (cabina oeste, hackeo medio)
    ///                                          2) cargar celda de ignición (pasarela: 1 batería o hackeo difícil)
    /// </summary>
    public static class EvacuacionBuilder
    {
        private const string Root = "Assets/_Nexus7";
        private const string ScenePath = Root + "/Scenes/Evacuacion.unity";
        private const string MenuScenePath = Root + "/Scenes/MenuPrincipal.unity";
        private const string Phase5ScenePath = Root + "/Scenes/CentroControl.unity";
        public const string SceneName = "Evacuacion";
        private const float H = 14f, CatY = 4.2f;

        private static Material hangarFloor, hangarWall, hullMat, burntMat, podMat, frameMat, planetMat, ringMat, starUnlit,
                                redGlow, orangeGlow, scorchMat;

        [MenuItem("Nexus-7/Fase 6/Construir Sala de Evacuación (y Menú Principal)")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            B.CreateFolders();
            var items = A.EnsureItems();
            var p4 = L4.EnsurePhase4Data(items);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            A.CreateMaterials();
            S3.CreateMaterials();
            L4.CreateMaterials();
            C5.CreateMaterials();
            CreateMaterials();
            ConfigureLighting();

            var env = new GameObject("Entorno").transform;
            var props = new GameObject("Mobiliario").transform;
            var gameplay = new GameObject("Gameplay").transform;
            var security = new GameObject("Seguridad").transform;
            var loot = new GameObject("Botin").transform;
            var fx = new GameObject("EfectosTension").transform;

            BuildShell(env);
            BuildWindowAndSpace(env);
            BuildBooth(env);
            BuildCatwalk(env);
            BuildPodBays(env);
            BuildWreck(props);
            BuildCover(props);
            var lights = BuildLights(env);

            // ---------- 1. Secuencia de lanzamiento de la cápsula B ----------
            var seqGo = new GameObject("SecuenciaLanzamiento_B");
            seqGo.transform.SetParent(gameplay);
            var sequence = seqGo.AddComponent<LaunchSequence>();

            var hatch = A.SlidingDoor("Escotilla_CapsulaB", gameplay, new Vector3(0f, CatY, 37.2f), new Vector3(2.4f, 2.4f, 0.2f),
                                      new Vector3(0f, 2.35f, 0f), podMat, new Vector3(-1.55f, 1.9f, -0.3f));
            B.SetString(hatch, "lockedPrompt", "Escotilla de la cápsula B");
            B.SetString(hatch, "lockedMessage", "ESCOTILLA SELLADA: completa la secuencia de lanzamiento (abrazaderas + celda de ignición).");

            var beaconBar = B.NoCollider(B.Box("IndicadorLanzamiento_B", gameplay, new Vector3(0f, 6.85f, 36.95f), new Vector3(2.6f, 0.15f, 0.05f), orangeGlow));
            var beaconLight = A.PointLight("LuzLanzamiento_B", gameplay, new Vector3(0f, 6.5f, 36.3f), new Color(1f, 0.45f, 0.05f), 7f, 2.5f, false);

            var stepClamps = LaunchTerminal("Terminal_Abrazaderas", gameplay, new Vector3(-18.5f, 0f, 23.3f), 0f,
                                            "Liberar abrazaderas", sequence, new ItemStack[0], true, HackDifficulty.Medium);
            var stepIgnition = LaunchTerminal("Terminal_CeldaIgnicion", gameplay, new Vector3(3.2f, CatY, 36.6f), 0f,
                                              "Cargar celda de ignición", sequence, new[] { new ItemStack(items.Battery, 1) }, true, HackDifficulty.Hard);

            A.SetObjectArray(sequence, "steps", new Object[] { stepClamps, stepIgnition });
            B.SetRef(sequence, "hatch", hatch);
            B.SetRef(sequence, "beacon", beaconLight);
            B.SetRef(sequence, "beaconRenderer", beaconBar.GetComponent<Renderer>());

            // Consola de lanzamiento dentro de la cápsula B (= escapar)
            var podConsole = new GameObject("ConsolaLanzamiento_B");
            podConsole.transform.SetParent(gameplay);
            podConsole.transform.SetPositionAndRotation(new Vector3(0f, CatY, 39.45f), Quaternion.Euler(0f, 180f, 0f));
            L4.Local(B.Box("Cuerpo", podConsole.transform, Vector3.zero, new Vector3(1.4f, 1.0f, 0.5f), C5.consoleMat), new Vector3(0f, 0.5f, 0f));
            var podScreen = L4.Local(B.NoCollider(B.Box("Pantalla", podConsole.transform, Vector3.zero, new Vector3(1.2f, 0.45f, 0.05f), C5.screenMat)), new Vector3(0f, 1.2f, 0.15f));
            podScreen.transform.localRotation = Quaternion.Euler(25f, 0f, 0f);
            L4.Local(B.Box("PalancaLanzamiento", podConsole.transform, Vector3.zero, new Vector3(0.12f, 0.45f, 0.12f), redGlow), new Vector3(0.45f, 1.2f, 0f));
            var escape = podConsole.AddComponent<EscapePod>();
            B.SetRef(escape, "sequence", sequence);
            B.SetString(escape, "podName", "cápsula B");
            A.PointLight("LuzInterior_B", gameplay, new Vector3(0f, 6.7f, 38.7f), new Color(1f, 0.8f, 0.55f), 4f, 1.4f, false);

            // Cápsulas A (destruida) y C (abrazaderas fundidas): escotillas que no responden
            foreach (var pod in new[] { new KeyValuePair<float, string>(-10f, "Cápsula A"), new KeyValuePair<float, string>(10f, "Cápsula C") })
            {
                var panel = B.Box("Escotilla_" + pod.Value.Replace("á", "a").Replace(" ", ""), gameplay, new Vector3(pod.Key, CatY + 1.2f, 36.92f), new Vector3(2.4f, 2.4f, 0.15f), burntMat);
                var dead = panel.AddComponent<EscapePod>();
                B.SetString(dead, "podName", pod.Value);
                B.NoCollider(B.Box("Indicador", gameplay, new Vector3(pod.Key, 6.85f, 36.95f), new Vector3(2.6f, 0.15f, 0.05f), redGlow));
            }

            // ---------- 2. Seguridad: Sector Pasarela (láser rampa oeste + cámara de la pasarela) ----------
            var laser = L4.Laser("SensorLaser_RampaOeste", security, -13f, -11f, 25.6f, new[] { 0.45f, 1.0f }, 0.3f, 1.15f);
            var cam = S3.SecurityCam(security, new Vector3(15.5f, CatY, 36.5f), 270f);
            A.SetFloat(cam, "sweepAngle", 50f);
            var sector = C5.Sector("Sector_Pasarela", security, "Sector Pasarela", new MonoBehaviour[] { laser, cam });

            // Cables dañados a pie de la rampa este (ruta alternativa: sin láser, pero con descargas intermitentes)
            var hazards = new GameObject("Trampas").transform;
            S3.Hazard("CablesDanados_RampaEste", hazards, new Vector3(12f, 1f, 25.2f), new Vector3(2.6f, 2f, 1.0f),
                      ElectricHazard.HazardEffect.Damage, ElectricHazard.HazardPattern.Intermittent, 22f, 0.8f, 1.6f, 2.4f, 0f, 0, true);
            S3.Hazard("CablesDanados_Lanzadera", hazards, new Vector3(-4.5f, 1f, 9.2f), new Vector3(3.2f, 2f, 1.0f),
                      ElectricHazard.HazardEffect.Damage, ElectricHazard.HazardPattern.Intermittent, 15f, 0.8f, 2.0f, 3.0f, 1.2f, 0, true);

            // ---------- 3. Consola del hangar (cabina oeste) ----------
            C5.ConsoleAt("ConsolaHangar", gameplay, new Vector3(-21f, 0f, 19f), 270f, "Consola de evacuación", new[]
            {
                new C5.Act { label = "Estado de las cápsulas", type = ConsoleActionType.ShowInfo,
                             info = "Cápsula A: destruida. Cápsula C: abrazaderas fundidas. Cápsula B: OPERATIVA.\n" +
                                    "Secuencia: 1) liberar abrazaderas (terminal de esta cabina)  2) celda de ignición (pasarela, junto a la B)." },
                new C5.Act { label = "Desactivar Sector Pasarela (láser + cámara)", type = ConsoleActionType.DisableSector, hack = true, diff = HackDifficulty.Medium,
                             sector = sector, duration = 45f, info = "Apaga el láser de la rampa oeste y la cámara de la pasarela de embarque." },
                new C5.Act { label = "Reiniciar protocolo de alerta", type = ConsoleActionType.ReduceAlert, hack = true, diff = HackDifficulty.Hard, heat = 40f,
                             info = "Reduce la tensión de la nave en 40 puntos." },
            });

            // ---------- 4. Botín ----------
            A.Container("Contenedor_Cabina", loot, new Vector3(-21.2f, 0f, 15.4f), 270f, new[] { new ItemStack(items.Battery, 1), new ItemStack(items.Scrap, 2) });
            A.Container("Contenedor_Ventanal", loot, new Vector3(20.6f, 0f, 5f), 90f, new[] { new ItemStack(items.Circuit, 1), new ItemStack(p4.Crystal, 1) });
            A.Pickup("Pickup_BateriaPasarela", loot, new Vector3(8f, 0f, 35.5f), items.Battery, 1, PrimitiveType.Cylinder, new Vector3(0.18f, 0.14f, 0.18f), A.batteryMat, 0f, true);
            A.Pickup("Pickup_Chatarra", loot, new Vector3(-6.5f, 0f, 4f), items.Scrap, 2, PrimitiveType.Cube, new Vector3(0.35f, 0.1f, 0.25f), A.scrapMat, 3f, false);

            // ---------- 5. Jugador ----------
            var (player, interactor) = B.BuildPlayer();
            player.transform.SetPositionAndRotation(new Vector3(0f, 0.05f, -3.2f), Quaternion.identity);
            var inventory = player.gameObject.AddComponent<InventorySystem>();
            // Equipo de prueba si se juega esta escena directamente (si vienes del Centro de Control, se usa tu inventario real).
            A.SetStacks(inventory, "startingItems", new[]
            {
                new ItemStack(items.Scrap, 4), new ItemStack(items.Circuit, 2), new ItemStack(p4.Decoy, 1), new ItemStack(p4.Emp, 1), new ItemStack(p4.Camo, 1)
            });
            var stealth = player.gameObject.AddComponent<PlayerStealth>();
            player.gameObject.AddComponent<PlayerRespawn>();
            var health = player.gameObject.AddComponent<PlayerHealth>();
            A.SetBool(health, "respawnOnDeath", false);   // aquí morir = final crítico
            var crafting = player.gameObject.AddComponent<CraftingSystem>();
            A.SetObjectArray(crafting, "knownRecipes", p4.Recipes.Cast<Object>().ToArray());
            var trajGo = new GameObject("Trayectoria");
            trajGo.transform.SetParent(player.transform, false);
            var traj = trajGo.AddComponent<LineRenderer>();
            traj.useWorldSpace = true;
            traj.sharedMaterial = S3.arcMat;
            traj.widthMultiplier = 0.025f;
            traj.shadowCastingMode = ShadowCastingMode.Off;
            var quickbar = player.gameObject.AddComponent<DeviceQuickbar>();
            A.SetObjectArray(quickbar, "slots", new Object[] { p4.Decoy, p4.Emp, p4.Camo });
            B.SetRef(quickbar, "throwOrigin", interactor.transform);
            B.SetRef(quickbar, "trajectoryLine", traj);

            var shake = interactor.gameObject.AddComponent<CameraShake>();       // en PlayerCamera
            var cam3d = interactor.GetComponent<Camera>();
            if (cam3d != null) cam3d.farClipPlane = 900f;                           // para ver el planeta y las estrellas

            // ---------- 6. Drones ----------
            var enemies = new GameObject("Enemigos").transform;
            var eliteRoute = A.Route("Ruta_Elite_Lanzadera", enemies, PatrolRoute.RouteMode.Loop, new[]
            {
                new A.WP(new Vector3(-9f, 0f, 8f),  1.5f, true, 0f),
                new A.WP(new Vector3(-9f, 0f, 28f), 1.5f, true, 90f),
                new A.WP(new Vector3(9f, 0f, 28f),  1.5f, true, 180f),
                new A.WP(new Vector3(9f, 0f, 8f),   1.5f, true, 270f),
            });
            var e1 = C5.EliteDrone("DronElite_1", enemies, new Vector3(-9f, 0f, 8f), 0f, eliteRoute, 0);
            var e2 = C5.EliteDrone("DronElite_2", enemies, new Vector3(9f, 0f, 28f), 180f, eliteRoute, 2);
            var squad = new GameObject("Escuadron_Elite").AddComponent<DroneSquad>();
            squad.transform.SetParent(enemies);
            A.SetObjectArray(squad, "members", new Object[] { e1, e2 });

            var catRoute = A.Route("Ruta_Pasarela", enemies, PatrolRoute.RouteMode.PingPong, new[]
            {
                new A.WP(new Vector3(-14f, CatY, 35.5f), 3f, true, 180f),   // vigila la rampa oeste
                new A.WP(new Vector3(14f, CatY, 35.5f),  3f, true, 180f),   // vigila la rampa este
            });
            var dCat = A.Drone("DronSeguridad_Pasarela", enemies, new Vector3(-14f, CatY, 35.5f), 90f, catRoute);

            var navGo = new GameObject("NavMesh");
            navGo.transform.position = new Vector3(0f, 7f, 18f);
            var baker = navGo.AddComponent<RuntimeNavMeshBaker>();
            A.SetVector3(baker, "size", new Vector3(48f, 16f, 50f));
            A.SetObjectArray(baker, "ignoreRoots", new Object[] { player.transform, e1.transform, e2.transform, dCat.transform });

            // ---------- 7. Humo (cobertura), zonas, puntos de control y avisos ----------
            var zones = new GameObject("Zonas").transform;
            var smokeWarning = new GameObject("HumoExtra_FaseAviso").transform;
            smokeWarning.SetParent(fx);
            foreach (var s in new[]
            {
                new S3.Bx(new Vector3(-6f, 0.2f, 31f), new Vector3(3f, 3f, 3f)), new S3.Bx(new Vector3(6f, 0.2f, 31f), new Vector3(3f, 3f, 3f)),
                new S3.Bx(new Vector3(-13f, 0.2f, 10f), new Vector3(3f, 3f, 3f)), new S3.Bx(new Vector3(14.5f, 0.2f, 12f), new Vector3(3f, 3f, 3f)),
                new S3.Bx(new Vector3(-10f, CatY + 0.2f, 35.5f), new Vector3(2.6f, 3f, 3f)),      // junto a la cápsula A averiada
            })
            {
                Smoke("HumoEmergencia", fx, s.pos, 1f, 6f);
                A.Zone("ZonaHumo", zones, s.pos + new Vector3(0f, 1.3f, 0f), s.size).AddComponent<ShadowZone>();
            }
            Smoke("HumoMotor", fx, new Vector3(0.4f, 2.6f, 25f), 0.6f, 8f);                    // motor de la lanzadera
            Smoke("HumoExtra", smokeWarning, new Vector3(-18f, 0.2f, 6f), 1.4f, 8f);
            Smoke("HumoExtra", smokeWarning, new Vector3(18f, 0.2f, 30f), 1.4f, 8f);
            Smoke("HumoExtra", smokeWarning, new Vector3(0f, 0.2f, 33f), 1.4f, 8f);

            foreach (var p in new[] { new Vector3(-1.5f, 2.6f, 12f), new Vector3(2f, 1.2f, 22f), new Vector3(-10f, 6.5f, 36.7f), new Vector3(-5f, 0.3f, 9.2f) })
                S3.Sparks("Chispas", fx, p);

            S3.Checkpoint("Checkpoint_Entrada", zones, new Vector3(0f, 1.5f, 2.5f), new Vector3(8f, 3f, 1f));
            S3.Checkpoint("Checkpoint_Cabina", zones, new Vector3(-16f, 1.5f, 19f), new Vector3(1f, 3f, 2f));
            S3.Checkpoint("Checkpoint_PasarelaO", zones, new Vector3(-12f, CatY + 1.5f, 33.2f), new Vector3(2.4f, 3f, 1f));
            S3.Checkpoint("Checkpoint_PasarelaE", zones, new Vector3(12f, CatY + 1.5f, 33.2f), new Vector3(2.4f, 3f, 1f));

            A.MessageZone("Aviso_Llegada", zones, new Vector3(0f, 1.5f, 1.2f), new Vector3(6f, 3f, 1f),
                "IA NEXUS-7: Cápsula A: destruida. Cápsula C: abrazaderas fundidas. La cápsula B, en el centro de la pasarela, es tu única salida.");
            A.MessageZone("Aviso_Cabina", zones, new Vector3(-13f, 1.5f, 19f), new Vector3(1f, 3f, 4f),
                "IA NEXUS-7: Cabina de control: terminal de abrazaderas y consola de evacuación (puede apagar el Sector Pasarela).");
            A.MessageZone("Aviso_RampaOeste", zones, new Vector3(-12f, 1.5f, 23.8f), new Vector3(2.4f, 3f, 1f),
                "IA NEXUS-7: Rampa oeste: sensor láser del Sector Pasarela. Desactívalo desde la cabina... o asume la alarma.");
            A.MessageZone("Aviso_RampaEste", zones, new Vector3(12f, 1.5f, 23.4f), new Vector3(2.4f, 3f, 0.8f),
                "IA NEXUS-7: Rampa este: cables dañados con descargas intermitentes. Cruza cuando se apaguen.");
            A.MessageZone("Aviso_Pasarela", zones, new Vector3(0f, CatY + 1.5f, 35.5f), new Vector3(5f, 3f, 3f),
                "IA NEXUS-7: Terminal de ignición junto a la cápsula B: inserta una batería o fuerza el hackeo (difícil).");

            // ---------- 8. HUD ----------
            var hud = B.BuildHUD();
            A.BuildPhase2UI(hud.transform, inventory, stealth);
            S3.BuildPhase3UI(hud.transform, health);
            L4.BuildPhase4UI(hud.transform, quickbar, stealth);
            C5.BuildPhase5UI(hud.transform);

            // ---------- 9. Sistemas de la fase final ----------
            var systems = new GameObject("GameSystems");
            systems.AddComponent<AlertAmbience>();
            var countdown = systems.AddComponent<EvacuationCountdown>();
            A.SetFloat(countdown, "duration", 300f);
            A.SetFloat(countdown, "warningAt", 120f);
            A.SetFloat(countdown, "criticalAt", 60f);
            B.SetRef(countdown, "cameraShake", shake);
            A.SetObjectArray(countdown, "enableOnWarning", new Object[] { smokeWarning.gameObject });
            A.SetObjectArray(countdown, "enableOnCritical", new Object[] { lights.criticalStrobes });

            var ending = systems.AddComponent<EndingManager>();
            B.SetRef(ending, "countdown", countdown);
            B.SetRef(ending, "cameraShake", shake);

            var tremor = systems.AddComponent<ShipTremor>();
            B.SetRef(tremor, "countdown", countdown);
            B.SetRef(tremor, "cameraShake", shake);
            B.SetRef(tremor, "flashLight", lights.explosionFlash);

            BuildPhase6UI(hud.transform, countdown, sequence);
            CreateEventSystem();

            var intro = systems.AddComponent<LevelIntro>();
            B.SetRef(intro, "player", player);
            B.SetRef(intro, "hud", hud);
            B.SetStrings(intro, "messages", new[]
            {
                "IA NEXUS-7: Hangar de evacuación. La autodestrucción está en marcha: tienes cinco minutos.",
                "IA NEXUS-7: Solo la cápsula B funciona. Libera sus abrazaderas desde la cabina oeste y carga una celda de ignición en la pasarela.",
                "IA NEXUS-7: Los drones de élite siguen activos. El humo te oculta. Todo lo que has hecho en la nave contará en tu evaluación final."
            });
            B.SetString(intro, "hint", "[E] terminales y consolas · Secuencia: 2 pasos · Arriba a la derecha: cuenta atrás y estado de la cápsula B");
            B.SetString(intro, "objective", "Objetivo: Completa la secuencia de lanzamiento y embarca en la cápsula B antes de la autodestrucción.");

            // ---------- 10. Guardado, menú, Build Settings y enlace ----------
            B.SavePrefab(player.gameObject, "Player_Fase6");
            B.SavePrefab(hud.gameObject, "HUD_Fase6");
            EditorSceneManager.SaveScene(scene, ScenePath);

            if (!System.IO.File.Exists(MenuScenePath)) BuildMainMenuScene();
            ConfigureBuildSettings();
            ConnectPhase5();
            if (EditorSceneManager.GetActiveScene().path != ScenePath) EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            Debug.Log("<color=cyan>[Nexus-7]</color> Sala de Evacuación construida en " + ScenePath + ". Pulsa Play.");
        }

        // =====================================================================
        //  MENÚ PRINCIPAL
        // =====================================================================

        [MenuItem("Nexus-7/Fase 6/Construir Menú Principal")]
        public static void BuildMainMenuMenuItem()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            BuildMainMenuScene();
            ConfigureBuildSettings();
        }

        private static void BuildMainMenuScene()
        {
            B.CreateFolders();
            S3.CreateMaterials();
            CreateMaterials();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.skybox = null;
            RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.05f, 0.06f, 0.09f);

            var camGo = new GameObject("MenuCamera") { tag = "MainCamera" };
            camGo.transform.SetPositionAndRotation(new Vector3(0f, 0f, 0f), Quaternion.Euler(-4f, 20f, 0f));
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.farClipPlane = 900f;
            camGo.AddComponent<AudioListener>();

            var sun = new GameObject("Sol").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.8f, 0.6f);
            sun.intensity = 1.3f;
            sun.transform.rotation = Quaternion.LookRotation(new Vector3(-0.8f, -0.3f, 0.5f));

            var space = new GameObject("Espacio").transform;
            Starfield(space, Vector3.zero, new Vector3(0f, 0f, 1f), 260f, 300, 2.2f);
            var planet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            planet.name = "Planeta";
            Object.DestroyImmediate(planet.GetComponent<Collider>());
            planet.transform.SetParent(space);
            planet.transform.position = new Vector3(70f, -45f, 160f);
            planet.transform.localScale = Vector3.one * 130f;
            planet.GetComponent<Renderer>().sharedMaterial = planetMat;
            var spin = planet.AddComponent<DebrisDrift>();
            A.SetVector3(spin, "velocity", Vector3.zero);
            A.SetVector3(spin, "spin", new Vector3(0f, 1.5f, 0f));

            // Canvas del menú
            var canvasGo = new GameObject("CanvasMenu");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var c = new Vector2(0.5f, 0.5f);
            B.UIText("Titulo", canvasGo.transform, c, new Vector2(-380f, 230f), new Vector2(1000f, 140f), 96f, TextAlignmentOptions.Left, new Color(0.6f, 0.9f, 1f)).text = "<b>ESCAPE ESPACIAL</b>";
            B.UIText("Subtitulo", canvasGo.transform, c, new Vector2(-380f, 130f), new Vector2(1000f, 80f), 60f, TextAlignmentOptions.Left, new Color(1f, 0.55f, 0.2f)).text = "NEXUS-7";
            B.UIText("Lema", canvasGo.transform, c, new Vector2(-380f, 60f), new Vector2(1000f, 50f), 26f, TextAlignmentOptions.Left, new Color(0.75f, 0.8f, 0.85f)).text =
                "La nave se ha vuelto contra ti. Sigilo, ingenio... y una cápsula de escape.";
            var newGame = UIButton("BotonNuevaPartida", canvasGo.transform, new Vector2(-710f, -60f), new Vector2(340f, 70f), "[Intro]  Nueva partida", new Color(0.1f, 0.45f, 0.6f, 0.95f));
            var quit = UIButton("BotonSalir", canvasGo.transform, new Vector2(-710f, -150f), new Vector2(340f, 70f), "Salir", new Color(0.35f, 0.12f, 0.12f, 0.95f));
            var best = B.UIText("Record", canvasGo.transform, c, new Vector2(-380f, -240f), new Vector2(1000f, 40f), 26f, TextAlignmentOptions.Left, new Color(1f, 0.85f, 0.35f));
            B.UIText("Controles", canvasGo.transform, new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(1800f, 60f), 20f, TextAlignmentOptions.Center, new Color(0.6f, 0.65f, 0.7f)).text =
                "[WASD] moverse · [Shift] correr · [Ctrl/C] agacharse · [E] interactuar · [Tab/I] inventario · [1-3] elegir dispositivo · [G] usar/lanzar";

            var menu = canvasGo.AddComponent<UI.MainMenuUI>();
            B.SetRef(menu, "newGameButton", newGame);
            B.SetRef(menu, "quitButton", quit);
            B.SetRef(menu, "bestScoreText", best);
            CreateEventSystem();

            EditorSceneManager.SaveScene(scene, MenuScenePath);
            Debug.Log("<color=cyan>[Nexus-7]</color> Menú principal construido en " + MenuScenePath + ".");
        }

        /// <summary>Menú principal primero (índice 0), luego las fases en orden y la evacuación al final.</summary>
        [MenuItem("Nexus-7/Fase 6/Ordenar Build Settings")]
        public static void ConfigureBuildSettings()
        {
            string[] order =
            {
                MenuScenePath, Root + "/Scenes/SalaInicial.unity", Root + "/Scenes/AlmacenCarga.unity", Root + "/Scenes/SalaEnergia.unity",
                Root + "/Scenes/Laboratorio.unity", Phase5ScenePath, ScenePath
            };
            var result = new List<EditorBuildSettingsScene>();
            foreach (var path in order)
                if (System.IO.File.Exists(path)) result.Add(new EditorBuildSettingsScene(path, true));
            foreach (var s in EditorBuildSettings.scenes)
                if (!result.Any(r => r.path == s.path)) result.Add(s);
            EditorBuildSettings.scenes = result.ToArray();
            Debug.Log("[Nexus-7] Build Settings: " + string.Join(" → ", result.Select(r => System.IO.Path.GetFileNameWithoutExtension(r.path)).ToArray()));
        }

        /// <summary>La acción final de la consola de mando (Fase 5) carga la evacuación.</summary>
        [MenuItem("Nexus-7/Fase 6/Conectar salida de la Fase 5")]
        public static void ConnectPhase5()
        {
            if (!System.IO.File.Exists(Phase5ScenePath))
            {
                Debug.Log("[Nexus-7] No existe CentroControl.unity: ejecuta 'Fase 5 > Construir Centro de Control' si quieres enlazarlas.");
                return;
            }
            string current = EditorSceneManager.GetActiveScene().path;
            var phase5 = EditorSceneManager.OpenScene(Phase5ScenePath, OpenSceneMode.Single);
            var go = GameObject.Find("ConsolaMando");
            var console = go != null ? go.GetComponent<ControlConsole>() : null;
            if (console != null)
            {
                var so = new SerializedObject(console);
                var list = so.FindProperty("actions");
                for (int i = 0; i < list.arraySize; i++)
                {
                    var e = list.GetArrayElementAtIndex(i);
                    if (e.FindPropertyRelative("type").enumValueIndex == (int)ConsoleActionType.CompletePhase)
                        e.FindPropertyRelative("nextScene").stringValue = SceneName;
                }
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.SaveScene(phase5);
                Debug.Log("[Nexus-7] Centro de Control enlazado con " + SceneName + ".");
            }
            if (!string.IsNullOrEmpty(current) && current != Phase5ScenePath)
                EditorSceneManager.OpenScene(current, OpenSceneMode.Single);
        }

        // =====================================================================
        //  ESTRUCTURA DEL HANGAR
        // =====================================================================

        private static void BuildShell(Transform env)
        {
            A.Tiled("Suelo", env, new Vector3(0f, -0.1f, 20f), new Vector3(44.6f, 0.2f, 40.6f), hangarFloor);
            A.Tiled("Techo", env, new Vector3(0f, H + 0.1f, 20f), new Vector3(44.6f, 0.2f, 40.6f), hangarWall);
            A.Tiled("MuroOeste", env, new Vector3(-22.15f, H / 2, 20f), new Vector3(0.3f, H, 40.6f), hangarWall);
            A.Tiled("MuroNorte", env, new Vector3(0f, H / 2, 40.15f), new Vector3(44.6f, H, 0.3f), hangarWall);
            // Muro sur con la entrada (x -1.5..1.5, 4 m de alto)
            A.Tiled("MuroSur_Izq", env, new Vector3(-11.9f, H / 2, -0.15f), new Vector3(20.8f, H, 0.3f), hangarWall);
            A.Tiled("MuroSur_Der", env, new Vector3(11.9f, H / 2, -0.15f), new Vector3(20.8f, H, 0.3f), hangarWall);
            B.Box("MuroSur_Dintel", env, new Vector3(0f, 9f, -0.15f), new Vector3(3f, 10f, 0.3f), hangarWall);
            B.NoCollider(B.Box("FranjaEntrada", env, new Vector3(0f, 4.15f, 0.02f), new Vector3(3.4f, 0.2f, 0.02f), A.warningMat));

            // Muro este: ventanal panorámico z 8..32, y 2..12
            A.Tiled("MuroEste_Bajo", env, new Vector3(22.15f, 1f, 20f), new Vector3(0.3f, 2f, 40.6f), hangarWall);
            A.Tiled("MuroEste_Alto", env, new Vector3(22.15f, 13f, 20f), new Vector3(0.3f, 2f, 40.6f), hangarWall);
            B.Box("MuroEste_Sur", env, new Vector3(22.15f, 7f, 3.85f), new Vector3(0.3f, 10f, 8.3f), hangarWall);
            B.Box("MuroEste_Norte", env, new Vector3(22.15f, 7f, 36.15f), new Vector3(0.3f, 10f, 8.3f), hangarWall);

            // Marcas de suelo: pista de despegue de la lanzadera
            for (int i = 0; i < 6; i++)
                B.NoCollider(B.Box("MarcaPista", env, new Vector3(0f, 0.005f, 3f + i * 5f), new Vector3(0.4f, 0.01f, 2.5f), A.warningMat));

            // Vestíbulo de llegada
            B.Box("Vest_Suelo", env, new Vector3(0f, -0.1f, -2.5f), new Vector3(5.4f, 0.2f, 5f), hangarFloor);
            B.Box("Vest_Techo", env, new Vector3(0f, 4.1f, -2.5f), new Vector3(5.4f, 0.2f, 5f), hangarWall);
            B.Box("Vest_MuroO", env, new Vector3(-2.6f, 2f, -2.5f), new Vector3(0.2f, 4f, 5f), hangarWall);
            B.Box("Vest_MuroE", env, new Vector3(2.6f, 2f, -2.5f), new Vector3(0.2f, 4f, 5f), hangarWall);
            B.Box("Vest_MuroS", env, new Vector3(0f, 2f, -5.1f), new Vector3(5.4f, 4f, 0.2f), hangarWall);
        }

        private static void BuildWindowAndSpace(Transform env)
        {
            var root = new GameObject("VentanalPanoramico").transform;
            root.SetParent(env);
            // Marco: montantes verticales y travesaño
            for (int i = 0; i <= 5; i++)
                B.NoCollider(B.Box("Montante", root, new Vector3(22f, 7f, 8f + i * 4.8f), new Vector3(0.35f, 10f, 0.3f), frameMat));
            B.NoCollider(B.Box("Travesano", root, new Vector3(22f, 7f, 20f), new Vector3(0.3f, 0.25f, 24f), frameMat));
            B.NoCollider(B.Box("Alfeizar", root, new Vector3(21.8f, 2.05f, 20f), new Vector3(0.7f, 0.1f, 24f), frameMat));
            // Cristal: collider invisible (no se puede "salir" al espacio)
            var glass = new GameObject("CristalBlindado");
            glass.transform.SetParent(root);
            glass.transform.position = new Vector3(22f, 7f, 20f);
            glass.AddComponent<BoxCollider>().size = new Vector3(0.3f, 10f, 24f);

            // Exterior: estrellas, planeta con anillo y restos de la nave a la deriva
            var space = new GameObject("EspacioExterior").transform;
            space.SetParent(env);
            Starfield(space, new Vector3(0f, 5f, 20f), new Vector3(1f, 0f, 0f), 280f, 260, 1.6f);

            var planet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            planet.name = "Planeta";
            Object.DestroyImmediate(planet.GetComponent<Collider>());
            planet.transform.SetParent(space);
            planet.transform.position = new Vector3(160f, -40f, 70f);
            planet.transform.localScale = Vector3.one * 150f;
            planet.GetComponent<Renderer>().sharedMaterial = planetMat;
            planet.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "AnilloPlanetario";
            Object.DestroyImmediate(ring.GetComponent<Collider>());
            ring.transform.SetParent(space);
            ring.transform.SetPositionAndRotation(planet.transform.position, Quaternion.Euler(12f, 0f, 22f));
            ring.transform.localScale = new Vector3(250f, 0.05f, 250f);
            ring.GetComponent<Renderer>().sharedMaterial = ringMat;
            ring.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

            var rng = new System.Random(66);
            for (int i = 0; i < 12; i++)
            {
                float x = 30f + (float)rng.NextDouble() * 50f, y = -8f + (float)rng.NextDouble() * 26f, z = (float)rng.NextDouble() * 45f;
                float s = 0.4f + (float)rng.NextDouble() * 2.6f;
                var d = B.NoCollider(B.Box("Restos", space, new Vector3(x, y, z), new Vector3(s, s * 0.4f, s * 0.7f), i % 3 == 0 ? burntMat : A.metalMat));
                var drift = d.AddComponent<DebrisDrift>();
                A.SetVector3(drift, "velocity", new Vector3(0.15f + (float)rng.NextDouble() * 0.3f, -0.05f + (float)rng.NextDouble() * 0.1f, -0.3f + (float)rng.NextDouble() * 0.6f));
                A.SetVector3(drift, "spin", new Vector3((float)rng.NextDouble() * 20f, (float)rng.NextDouble() * 30f, (float)rng.NextDouble() * 10f));
            }

            // Pantallas de visualización (muro oeste): telemetría de la autodestrucción
            foreach (float z in new[] { 10f, 30f })
            {
                var scr = B.NoCollider(B.Box("PantallaVisualizacion", root, new Vector3(-21.97f, 8.5f, z), new Vector3(0.04f, 4f, 7f), C5.screenMat));
                scr.AddComponent<AlertScreen>();
            }
        }

        /// <summary>Puntos de luz a gran distancia, en la semiesfera que mira hacia 'facing'.</summary>
        private static void Starfield(Transform parent, Vector3 center, Vector3 facing, float radius, int count, float maxSize)
        {
            var rng = new System.Random(42);
            for (int i = 0; i < count; i++)
            {
                var d = new Vector3((float)rng.NextDouble() * 2f - 1f, (float)rng.NextDouble() * 1.6f - 0.6f, (float)rng.NextDouble() * 2f - 1f);
                d = (d + facing * 1.2f).normalized;
                float r = radius + (float)rng.NextDouble() * 40f, s = 0.4f + (float)rng.NextDouble() * maxSize;
                var star = B.NoCollider(B.Box("Estrella", parent, center + d * r, new Vector3(s, s, s), starUnlit));
                star.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        private static void BuildBooth(Transform env)
        {
            // Cabina de control oeste: x -22..-15, z 14..24, puerta abierta al este (z 18..20)
            var root = new GameObject("CabinaControlOeste").transform;
            root.SetParent(env);
            B.Box("MuroS", root, new Vector3(-18.5f, 1.6f, 14f), new Vector3(7f, 3.2f, 0.3f), C5.hallWall);
            B.Box("MuroN", root, new Vector3(-18.5f, 1.6f, 24f), new Vector3(7f, 3.2f, 0.3f), C5.hallWall);
            B.Box("MuroE_S", root, new Vector3(-15f, 1.6f, 16f), new Vector3(0.3f, 3.2f, 4f), C5.hallWall);
            B.Box("MuroE_N", root, new Vector3(-15f, 1.6f, 22f), new Vector3(0.3f, 3.2f, 4f), C5.hallWall);
            B.Box("Dintel", root, new Vector3(-15f, 2.9f, 19f), new Vector3(0.3f, 0.6f, 2f), C5.hallWall);
            B.Box("Techo", root, new Vector3(-18.5f, 3.3f, 19f), new Vector3(7.3f, 0.2f, 10.3f), C5.hallWall);
            B.NoCollider(B.Box("Ventana", root, new Vector3(-14.84f, 1.9f, 16f), new Vector3(0.02f, 0.8f, 3.4f), C5.glassMat));
            B.NoCollider(B.Box("Ventana", root, new Vector3(-14.84f, 1.9f, 22f), new Vector3(0.02f, 0.8f, 3.4f), C5.glassMat));
            A.PointLight("LuzCabina", root, new Vector3(-18.5f, 2.9f, 19f), new Color(1f, 0.35f, 0.2f), 7f, 1.6f, true);
        }

        private static void BuildCatwalk(Transform env)
        {
            var root = new GameObject("PasarelaEmbarque").transform;
            root.SetParent(env);
            B.Box("Pasarela", root, new Vector3(0f, CatY - 0.1f, 35.5f), new Vector3(32.2f, 0.2f, 3f), A.metalMat);
            foreach (float x in new[] { -15f, -6f, 6f, 15f })
                B.Box("Pilar", root, new Vector3(x, (CatY - 0.2f) / 2f, 35.5f), new Vector3(0.35f, CatY - 0.2f, 0.35f), A.metalMat);
            // Barandillas (huecos en la llegada de las rampas)
            S3.RailX(root, 34.05f, -16.1f, -13f);
            S3.RailX(root, 34.05f, -11f, 11f);
            S3.RailX(root, 34.05f, 13f, 16.1f);
            S3.RailZ(root, -16.05f, 34f, 37f);
            S3.RailZ(root, 16.05f, 34f, 37f);
            // Rampas: suben de z=26 (suelo) a z=34 (pasarela)
            S3.Ramp("RampaOeste", root, -12f, 2f, 26f, 0f, 34f, CatY, 0.3f, A.metalMat);
            S3.Ramp("RampaEste", root, 12f, 2f, 26f, 0f, 34f, CatY, 0.3f, A.metalMat);
            // Pretiles a pie de rampa: obligan a cruzar el láser (oeste) o los cables (este); no se pueden rodear
            foreach (float x in new[] { -13.15f, -10.85f, 10.85f, 13.15f })
                B.Box("PretilRampa", root, new Vector3(x, 0.8f, 26.3f), new Vector3(0.3f, 1.6f, 3.4f), A.metalMat);
            B.NoCollider(B.Box("FranjaBorde", root, new Vector3(0f, CatY + 0.005f, 34.15f), new Vector3(32f, 0.01f, 0.15f), A.warningMat));
        }

        private static void BuildPodBays(Transform env)
        {
            // Muro de bahías (z 37..37.4) con la boca de la cápsula B (x -1.2..1.2, y 4.2..6.6)
            var root = new GameObject("BahiasCapsulas").transform;
            root.SetParent(env);
            B.Box("MuroBahias_O", root, new Vector3(-11.6f, H / 2f, 37.2f), new Vector3(20.8f, H, 0.4f), hangarWall);
            B.Box("MuroBahias_E", root, new Vector3(11.6f, H / 2f, 37.2f), new Vector3(20.8f, H, 0.4f), hangarWall);
            B.Box("MuroBahias_BajoB", root, new Vector3(0f, CatY / 2f, 37.2f), new Vector3(2.4f, CatY, 0.4f), hangarWall);
            B.Box("MuroBahias_SobreB", root, new Vector3(0f, (6.6f + H) / 2f, 37.2f), new Vector3(2.4f, H - 6.6f, 0.4f), hangarWall);

            // Interior de la cápsula B (z 37.4..40)
            var pod = new GameObject("CapsulaB_Interior").transform;
            pod.SetParent(root);
            B.Box("Suelo", pod, new Vector3(0f, CatY - 0.1f, 38.7f), new Vector3(3.4f, 0.2f, 2.6f), podMat);
            B.Box("ParedO", pod, new Vector3(-1.75f, CatY + 1.5f, 38.7f), new Vector3(0.3f, 3f, 2.6f), podMat);
            B.Box("ParedE", pod, new Vector3(1.75f, CatY + 1.5f, 38.7f), new Vector3(0.3f, 3f, 2.6f), podMat);
            B.Box("Techo", pod, new Vector3(0f, CatY + 3.1f, 38.7f), new Vector3(3.8f, 0.2f, 2.6f), podMat);
            foreach (float x in new[] { -1.2f, 1.2f })
                B.Box("Asiento", pod, new Vector3(x, CatY + 0.3f, 38.7f), new Vector3(0.6f, 0.6f, 0.9f), C5.consoleMat);

            // Marcos de las tres bocas
            foreach (float x in new[] { -10f, 0f, 10f })
            {
                B.NoCollider(B.Box("MarcoO", root, new Vector3(x - 1.3f, CatY + 1.25f, 36.97f), new Vector3(0.2f, 2.6f, 0.06f), frameMat));
                B.NoCollider(B.Box("MarcoE", root, new Vector3(x + 1.3f, CatY + 1.25f, 36.97f), new Vector3(0.2f, 2.6f, 0.06f), frameMat));
                B.NoCollider(B.Box("Franja", root, new Vector3(x, CatY + 0.02f, 36.6f), new Vector3(2.4f, 0.02f, 0.6f), A.warningMat));
            }
        }

        private static void BuildWreck(Transform parent)
        {
            // Lanzadera siniestrada en el centro: cobertura y obstáculo visual. Girada 12°.
            var root = new GameObject("LanzaderaSiniestrada").transform;
            root.SetParent(parent);
            root.SetPositionAndRotation(new Vector3(0f, 0f, 18f), Quaternion.Euler(0f, 12f, 0f));
            B.Box("Fuselaje", root, new Vector3(0f, 1.5f, 0f), new Vector3(3.4f, 2.6f, 12f), hullMat);
            B.Box("Morro", root, new Vector3(0f, 1.2f, 6.8f), new Vector3(2.6f, 1.9f, 1.6f), hullMat);
            B.NoCollider(B.Box("Cabina", root, new Vector3(0f, 2.3f, 6.2f), new Vector3(2.2f, 0.6f, 1.4f), C5.glassMat));
            foreach (float x in new[] { -4.4f, 4.4f })
                B.Box("Ala", root, new Vector3(x, 0.9f, -1f), new Vector3(5.4f, 0.3f, 4f), hullMat);
            foreach (float x in new[] { -1.1f, 1.1f })
                B.Box("Motor", root, new Vector3(x, 1.5f, -6.6f), new Vector3(1.2f, 1.2f, 1.4f), burntMat);
            B.Box("Deriva", root, new Vector3(0f, 3.8f, -4.6f), new Vector3(0.3f, 2.2f, 2.4f), hullMat);
            B.NoCollider(B.Box("MarcaQuemada", root, new Vector3(0f, 0.01f, -1f), new Vector3(9f, 0.02f, 16f), scorchMat));
            A.PointLight("BrasasMotor", root, new Vector3(0f, 1.6f, -7.6f), new Color(1f, 0.35f, 0.1f), 6f, 2f, false)
                .gameObject.AddComponent<EmergencyLight>();
        }

        private static void BuildCover(Transform parent)
        {
            // Contenedores y depósitos fuera de las rutas de los drones (anillo x=±9, z 8..28)
            var crates = new[]
            {
                new S3.Bx(new Vector3(-4.5f, 1f, 4.5f), new Vector3(2f, 2f, 2f)),   new S3.Bx(new Vector3(4.5f, 0.75f, 4f), new Vector3(2.4f, 1.5f, 1.6f)),
                new S3.Bx(new Vector3(-13f, 1f, 6.5f), new Vector3(2f, 2f, 2f)),    new S3.Bx(new Vector3(15.5f, 1f, 8.5f), new Vector3(2f, 2f, 2.4f)),
                new S3.Bx(new Vector3(-15f, 0.75f, 29.5f), new Vector3(1.6f, 1.5f, 1.6f)), new S3.Bx(new Vector3(16.5f, 1f, 26f), new Vector3(2.4f, 2f, 2f)),
                new S3.Bx(new Vector3(13f, 0.6f, 18f), new Vector3(1.6f, 1.2f, 3f)),  new S3.Bx(new Vector3(-12.5f, 1f, 16f), new Vector3(2f, 2f, 2f)),
            };
            for (int i = 0; i < crates.Length; i++)
                B.Box("Contenedor", parent, crates[i].pos, crates[i].size, i % 2 == 0 ? A.crateOrange : A.crateGreen);
            foreach (var p in new[] { new Vector3(-17f, 0f, 30f), new Vector3(18f, 0f, 16f) })
                L4.Cyl("DepositoCombustible", parent, p + new Vector3(0f, 1.4f, 0f), new Vector3(1.6f, 1.4f, 1.6f), burntMat, true);
        }

        private struct HangarLights { public GameObject criticalStrobes; public Light explosionFlash; }

        private static HangarLights BuildLights(Transform env)
        {
            var root = new GameObject("Iluminacion").transform;
            root.SetParent(env);

            // Luz del sol del sistema, entrando por el ventanal (rayos dramáticos sobre el suelo)
            var sun = new GameObject("LuzEstelar").AddComponent<Light>();
            sun.transform.SetParent(root);
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.78f, 0.6f);
            sun.intensity = 0.9f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.LookRotation(new Vector3(-0.8f, -0.35f, -0.45f));

            // Lámparas de techo que siguen el nivel de alerta
            int n = 0;
            foreach (float x in new[] { -14f, 0f, 14f })
                foreach (float z in new[] { 8f, 22f, 33f })
                {
                    var pos = new Vector3(x, H - 0.6f, z);
                    var fixture = B.NoCollider(B.Box("Lampara", root, pos + Vector3.up * 0.3f, new Vector3(2.4f, 0.1f, 0.5f), C5.lampMat));
                    var l = A.PointLight("LuzAlerta", root, pos - Vector3.up * 0.3f, new Color(0.55f, 0.78f, 1f), 18f, 1.4f, n++ < 2);
                    var al = l.gameObject.AddComponent<AlertLight>();
                    B.SetRef(al, "emissiveRenderer", fixture.GetComponent<Renderer>());
                }
            A.PointLight("LuzVestibulo", root, new Vector3(0f, 3.7f, -2.5f), new Color(1f, 0.3f, 0.2f), 6f, 1f, false).gameObject.AddComponent<AlertLight>();

            // Balizas giratorias naranjas (luces de despegue)
            foreach (var p in new[] { new Vector3(-21.6f, 10f, 4f), new Vector3(21.6f, 10f, 4f), new Vector3(-21.6f, 10f, 34f), new Vector3(21.6f, 10f, 36f), new Vector3(0f, 8.5f, 36.8f) })
                Beacon(root, p, new Color(1f, 0.5f, 0.1f));

            // Estroboscopios rojos: EvacuationCountdown los activa en la fase crítica
            var strobes = new GameObject("Estroboscopios_Criticos");
            strobes.transform.SetParent(root);
            foreach (float x in new[] { -10f, 10f })
                foreach (float z in new[] { 10f, 20f, 30f })
                    Strobe(strobes.transform, new Vector3(x, H - 0.4f, z));

            // Destello de las explosiones (ShipTremor)
            var flash = A.PointLight("DestelloExplosiones", root, new Vector3(0f, 11f, 20f), new Color(1f, 0.5f, 0.2f), 45f, 0f, false);
            return new HangarLights { criticalStrobes = strobes, explosionFlash = flash };
        }

        private static void Beacon(Transform parent, Vector3 pos, Color color)
        {
            var root = new GameObject("BalizaGiratoria").transform;
            root.SetParent(parent);
            root.position = pos;
            B.NoCollider(B.Box("Carcasa", root, Vector3.zero, new Vector3(0.4f, 0.3f, 0.4f), orangeGlow)).transform.localPosition = Vector3.zero;
            var pivot = new GameObject("Pivote").transform;
            pivot.SetParent(root, false);
            pivot.gameObject.AddComponent<RotatingBeacon>();
            var spotGo = new GameObject("Haz");
            spotGo.transform.SetParent(pivot, false);
            spotGo.transform.localRotation = Quaternion.Euler(20f, 0f, 0f);   // ligeramente hacia abajo
            var spot = spotGo.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.color = color;
            spot.range = 28f;
            spot.spotAngle = 35f;
            spot.intensity = 4f;
        }

        private static void Strobe(Transform parent, Vector3 pos)
        {
            var fixture = B.NoCollider(B.Box("Estroboscopio", parent, pos, new Vector3(0.6f, 0.1f, 0.3f), redGlow));
            var lightGo = new GameObject("LuzEstroboscopica");
            lightGo.transform.SetParent(parent);
            lightGo.transform.localPosition = pos + Vector3.down * 0.3f;
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.08f, 0.05f);
            light.range = 16f;
            light.intensity = 3f;
            var em = lightGo.AddComponent<EmergencyLight>();
            B.SetEnum(em, "mode", (int)EmergencyLight.Mode.Pulse);
            A.SetFloat(em, "baseIntensity", 3.5f);
            A.SetFloat(em, "pulseFrequency", 2.2f);
            B.SetRef(em, "emissiveRenderer", fixture.GetComponent<Renderer>());
        }

        private static SmokeEmitter Smoke(string name, Transform parent, Vector3 pos, float radius, float size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = pos;
            go.AddComponent<ParticleSystem>();
            var smoke = go.AddComponent<SmokeEmitter>();
            A.SetFloat(smoke, "radius", radius);
            A.SetFloat(smoke, "size", size * 0.5f);
            A.SetFloat(smoke, "rate", 5f + size * 0.4f);
            return smoke;
        }

        /// <summary>Terminal de un paso de la secuencia de lanzamiento (orientado como las consolas: se usa desde -Z local).</summary>
        private static LaunchStepTerminal LaunchTerminal(string name, Transform parent, Vector3 pos, float rotY, string stepName,
            LaunchSequence sequence, ItemStack[] items, bool allowHack, HackDifficulty difficulty)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent);
            root.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, rotY, 0f));
            L4.Local(B.Box("Cuerpo", root.transform, Vector3.zero, new Vector3(1.1f, 1.15f, 0.6f), C5.consoleMat), new Vector3(0f, 0.575f, 0f));
            var scr = L4.Local(B.NoCollider(B.Box("Pantalla", root.transform, Vector3.zero, new Vector3(0.9f, 0.45f, 0.05f), C5.screenMat)), new Vector3(0f, 1.3f, -0.18f));
            scr.transform.localRotation = Quaternion.Euler(-25f, 0f, 0f);
            var status = L4.Local(B.NoCollider(B.Box("LuzEstado", root.transform, Vector3.zero, new Vector3(0.9f, 0.08f, 0.08f), orangeGlow)), new Vector3(0f, 1.62f, 0f));
            L4.Local(B.NoCollider(B.Box("Franja", root.transform, Vector3.zero, new Vector3(1.12f, 0.12f, 0.62f), A.warningMat)), new Vector3(0f, 0.15f, 0f));
            A.PointLight("LuzTerminal", root.transform, new Vector3(0f, 1.6f, -0.8f), new Color(1f, 0.55f, 0.2f), 3f, 1f, false);

            var term = root.AddComponent<LaunchStepTerminal>();
            B.SetString(term, "stepName", stepName);
            B.SetRef(term, "sequence", sequence);
            A.SetStacks(term, "requiredItems", items);
            A.SetBool(term, "allowHack", allowHack);
            B.SetEnum(term, "hackDifficulty", (int)difficulty);
            B.SetRef(term, "statusRenderer", status.GetComponent<Renderer>());
            return term;
        }

        // =====================================================================
        //  UI DE LA FASE 6: cuenta atrás + pantalla de resultados
        // =====================================================================

        private static void BuildPhase6UI(Transform canvas, EvacuationCountdown countdown, LaunchSequence sequence)
        {
            // Arriba a la derecha, bajo el indicador de sigilo: el centro superior lo ocupan la barra de alerta y las pistas.
            var topRight = new Vector2(1f, 1f);
            var timer = B.UIText("CuentaAtras", canvas, topRight, new Vector2(-30f, -105f), new Vector2(280f, 120f), 52f, TextAlignmentOptions.Right, Color.white);
            timer.rectTransform.pivot = new Vector2(1f, 1f);
            var launch = B.UIText("EstadoLanzamiento", canvas, topRight, new Vector2(-30f, -230f), new Vector2(300f, 60f), 20f, TextAlignmentOptions.Right, new Color(1f, 0.75f, 0.4f));
            launch.rectTransform.pivot = new Vector2(1f, 1f);
            var top = new Vector2(0.5f, 1f);
            var chud = canvas.gameObject.AddComponent<UI.CountdownHUD>();
            B.SetRef(chud, "countdown", countdown);
            B.SetRef(chud, "launch", sequence);
            B.SetRef(chud, "timerText", timer);
            B.SetRef(chud, "launchText", launch);

            // Pantalla de resultados (pantalla completa, encima de todo)
            var panel = B.UIImage("PanelResultados", canvas, new Color(0.01f, 0.02f, 0.04f, 0.96f));
            B.Anchor(panel.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var p = panel.transform;
            var c = new Vector2(0.5f, 0.5f);
            var title = B.UIText("Titulo", p, top, new Vector2(0f, -40f), new Vector2(1700f, 150f), 64f, TextAlignmentOptions.Top, Color.white);
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            var story = B.UIText("Narracion", p, top, new Vector2(0f, -195f), new Vector2(1500f, 90f), 24f, TextAlignmentOptions.Top, new Color(0.78f, 0.82f, 0.88f));
            story.rectTransform.pivot = new Vector2(0.5f, 1f);
            story.fontStyle = FontStyles.Italic;

            B.UIText("CabeceraEvaluacion", p, c, new Vector2(-430f, 230f), new Vector2(760f, 40f), 28f, TextAlignmentOptions.Left, new Color(0.6f, 0.9f, 1f)).text = "<b>EVALUACIÓN</b>";
            var labels = new TextMeshProUGUI[4];
            var bars = new Image[4];
            Color[] barColors = { new Color(0.4f, 1f, 0.7f), new Color(1f, 0.8f, 0.3f), new Color(0.4f, 0.8f, 1f), new Color(0.85f, 0.5f, 1f) };
            for (int i = 0; i < 4; i++)
            {
                float y = 170f - i * 72f;
                labels[i] = B.UIText("Categoria" + i, p, c, new Vector2(-430f, y), new Vector2(760f, 36f), 26f, TextAlignmentOptions.Left, Color.white);
                var (fill, bg) = S3.Bar("Barra" + i, p, c, new Vector2(-810f, y - 30f));
                B.Anchor(bg.rectTransform, c, c, new Vector2(-810f, y - 30f), new Vector2(760f, 12f));
                fill.color = barColors[i];
                fill.rectTransform.localScale = new Vector3(0f, 1f, 1f);
                bars[i] = fill;
            }
            var breakdown = B.UIText("Destacados", p, c, new Vector2(-430f, -180f), new Vector2(760f, 150f), 22f, TextAlignmentOptions.TopLeft, Color.white);
            var stats = B.UIText("Estadisticas", p, c, new Vector2(430f, 60f), new Vector2(800f, 400f), 22f, TextAlignmentOptions.TopLeft, Color.white);
            var total = B.UIText("PuntuacionTotal", p, c, new Vector2(150f, -250f), new Vector2(700f, 110f), 48f, TextAlignmentOptions.Center, Color.white);
            var grade = B.UIText("Rango", p, c, new Vector2(600f, -240f), new Vector2(220f, 180f), 140f, TextAlignmentOptions.Center, Color.white);
            B.UIText("EtiquetaRango", p, c, new Vector2(600f, -140f), new Vector2(220f, 30f), 20f, TextAlignmentOptions.Center, new Color(0.6f, 0.65f, 0.7f)).text = "RANGO";
            var restart = UIButton("BotonReiniciar", p, new Vector2(-220f, -440f), new Vector2(380f, 70f), "[R]  Reiniciar partida", new Color(0.1f, 0.45f, 0.6f, 0.95f));
            var menu = UIButton("BotonMenu", p, new Vector2(220f, -440f), new Vector2(380f, 70f), "[M]  Menú principal", new Color(0.25f, 0.27f, 0.32f, 0.95f));

            var ui = canvas.gameObject.AddComponent<UI.EndingUI>();
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
            panel.gameObject.SetActive(false);
        }

        private static Button UIButton(string name, Transform parent, Vector2 pos, Vector2 size, string label, Color color)
        {
            var c = new Vector2(0.5f, 0.5f);
            var img = B.UIImage(name, parent, color);
            img.raycastTarget = true;   // B.UIImage lo desactiva: sin esto el botón no recibe clics
            B.Anchor(img.rectTransform, c, c, pos, size);
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            B.UIText("Texto", img.transform, c, Vector2.zero, size, 28f, TextAlignmentOptions.Center, Color.white).text = label;
            return btn;
        }

        private static void CreateEventSystem()
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        // =====================================================================
        //  MATERIALES E ILUMINACIÓN
        // =====================================================================

        private static void ConfigureLighting()
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.05f, 0.035f, 0.035f);  // AlertAmbience lo anima en juego
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;                        // neblina en el hangar sin tapar el planeta
            RenderSettings.fogColor = new Color(0.04f, 0.025f, 0.025f);
            RenderSettings.fogStartDistance = 25f;
            RenderSettings.fogEndDistance = 600f;
        }

        private static void CreateMaterials()
        {
            hangarFloor = B.CreateMat("M6_SueloHangar", new Color(0.13f, 0.13f, 0.14f), null, 0.6f, 0.8f);
            hangarWall  = B.CreateMat("M6_PanelHangar", new Color(0.22f, 0.22f, 0.24f), null, 0.45f, 0.7f);
            hullMat     = B.CreateMat("M6_Casco", new Color(0.55f, 0.57f, 0.6f), null, 0.5f, 0.6f);
            burntMat    = B.CreateMat("M6_Quemado", new Color(0.08f, 0.07f, 0.07f), new Color(0.35f, 0.08f, 0.02f) * 0.3f, 0.2f, 0.5f);
            podMat      = B.CreateMat("M6_Capsula", new Color(0.85f, 0.86f, 0.88f), null, 0.7f, 0.4f);
            frameMat    = B.CreateMat("M6_Marco", new Color(0.1f, 0.1f, 0.12f), null, 0.7f, 0.9f);
            planetMat   = B.CreateMat("M6_Planeta", new Color(0.2f, 0.4f, 0.65f), new Color(0.03f, 0.08f, 0.16f), 0.15f, 0f);
            ringMat     = S3.UnlitMat("M6_Anillo", new Color(0.55f, 0.48f, 0.4f, 0.6f));
            starUnlit   = S3.UnlitMat("M6_Estrella", Color.white);
            redGlow     = B.CreateMat("M6_BrilloRojo", Color.black, new Color(1f, 0.08f, 0.05f) * 3f);
            orangeGlow  = B.CreateMat("M6_BrilloNaranja", Color.black, new Color(1f, 0.45f, 0.05f) * 3f);
            scorchMat   = B.CreateMat("M6_Quemadura", new Color(0.03f, 0.03f, 0.03f), null, 0.05f, 0f);
        }
    }
}
