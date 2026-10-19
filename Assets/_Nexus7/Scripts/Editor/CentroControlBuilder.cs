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
using L4 = Nexus7.EditorTools.LaboratorioBuilder;   // Fase 4

namespace Nexus7.EditorTools
{
    /// <summary>
    /// Menú "Nexus-7 > Fase 5 > Construir Centro de Control".
    ///
    /// Plano (vista cenital, Z = norte), 40 × 38 m, techo a 12 m:
    ///
    ///  z=38 ┌──────────── PUENTE: consola de mando del capitán (final) ────────────┐
    ///  z=30 ═════════════════════[COMPUERTA BLINDADA]═════ (pantallas gigantes) ════
    ///       │ ALA OESTE  ║        │láser│ cámara                    ALA ESTE     │
    ///       │ ctrl. de   ║pasarela  antesala                         banco + R3  │
    ///       │ compuertas ║(y=4.2)                                   suministros  │
    ///       │   D3       ║   E1 ⟲  [holograma]  ⟲ E2   (élite en pareja)   cámara │
    ///       │   cámara   ║      consola de seguridad central                     │
    ///  z=6  ══[láser]════╝                                                        │
    ///       │   monitores          ZONA DE ENTRADA           monitores          │
    ///  z=0  ═════════════════════════[entrada]════════════════════════════════════
    ///
    /// Sectores de seguridad: OESTE (láser + cámara), ESTE (cámara), PUENTE (láser + cámara).
    /// </summary>
    public static class CentroControlBuilder
    {
        private const string Root = "Assets/_Nexus7";
        private const string ScenePath = Root + "/Scenes/CentroControl.unity";
        private const string Phase4ScenePath = Root + "/Scenes/Laboratorio.unity";
        public const string SceneName = "CentroControl";
        private const float H = 12f, CatY = 4.2f;
        private const int IgnoreRaycastLayer = 2;

        internal static Material hallFloor, hallWall, consoleMat, screenMat, holoMat, eliteMat, starMat, starDot, lampMat, glassMat;

        /// <summary>Especificación de una acción de consola (para rellenar el Inspector desde código).</summary>
        internal struct Act
        {
            public string label; public ConsoleActionType type; public bool hack; public HackDifficulty diff;
            public SecuritySector sector; public float duration; public LockdownDoor door; public float heat;
            public string next; public string info; public bool oneShot;
        }

        [MenuItem("Nexus-7/Fase 5/Construir Centro de Control")]
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
            CreateMaterials();
            ConfigureLighting();

            var env = new GameObject("Entorno").transform;
            var props = new GameObject("Mobiliario").transform;
            var gameplay = new GameObject("Gameplay").transform;
            var security = new GameObject("Seguridad").transform;
            var loot = new GameObject("Botin").transform;

            BuildShell(env);
            BuildWestWing(env);
            BuildCatwalk(env);
            BuildScreensAndLights(env);
            BuildHologram(props);
            BuildDecor(props);

            // ---------- 1. Compuerta blindada del puente ----------
            var blast = A.SlidingDoor("CompuertaBlindada", gameplay, new Vector3(0f, 0f, 30f), new Vector3(4f, 4f, 0.4f),
                                      new Vector3(0f, 3.95f, 0f), A.metalMat, new Vector3(-2.7f, 2.6f, -0.45f));
            B.SetString(blast, "lockedPrompt", "Compuerta blindada");
            B.SetString(blast, "lockedMessage", "COMPUERTA BLINDADA: requiere autorización desde el control de compuertas (ala oeste). En Alerta Roja permanece sellada.");
            A.SetFloat(blast, "doorNoiseRadius", 10f);
            var lockdown = blast.gameObject.AddComponent<LockdownDoor>();
            B.SetString(lockdown, "doorName", "Compuerta blindada del puente");

            // ---------- 2. Seguridad por sectores ----------
            var laserW = L4.Laser("SensorLaser_Oeste", security, -17f, -14f, 6f, new[] { 0.4f, 0.85f, 1.3f }, 0.25f, 1.45f);
            var camW = S3.SecurityCam(security, new Vector3(-19.4f, 0f, 10.5f), 140f);
            var camE = S3.SecurityCam(security, new Vector3(19.5f, 0f, 6.5f), 335f);
            var laserP = L4.Laser("SensorLaser_Puente", security, -4f, 4f, 26f, new[] { 0.45f, 1.0f }, 0.3f, 1.15f);
            var camP = S3.SecurityCam(security, new Vector3(3.3f, 0f, 29.2f), 200f);
            A.SetFloat(camP, "sweepAngle", 35f);
            var sectorW = Sector("Sector_Oeste", security, "Sector Oeste", new MonoBehaviour[] { laserW, camW });
            var sectorE = Sector("Sector_Este", security, "Sector Este", new MonoBehaviour[] { camE });
            var sectorP = Sector("Sector_Puente", security, "Sector Puente", new MonoBehaviour[] { laserP, camP });

            // ---------- 3. Consolas ----------
            ConsoleAt("ConsolaSeguridadCentral", gameplay, new Vector3(0f, 0f, 11.6f), 0f, "Consola de seguridad central", new[]
            {
                new Act { label = "Consultar estado de la nave", type = ConsoleActionType.ShowInfo,
                          info = "La compuerta blindada se autoriza desde el control de compuertas del ala oeste." },
                new Act { label = "Desactivar Sector Oeste (láser + cámara)", type = ConsoleActionType.DisableSector, hack = true, diff = HackDifficulty.Medium, sector = sectorW, duration = 40f,
                          info = "Apaga el láser y la cámara de la entrada al ala oeste." },
                new Act { label = "Desactivar Sector Este (cámara)", type = ConsoleActionType.DisableSector, hack = true, diff = HackDifficulty.Easy, sector = sectorE, duration = 45f,
                          info = "Apaga la cámara que vigila el banco de trabajo y los suministros del ala este." },
                new Act { label = "Desactivar Sector Puente (láser + cámara)", type = ConsoleActionType.DisableSector, hack = true, diff = HackDifficulty.Hard, sector = sectorP, duration = 35f,
                          info = "Apaga los sistemas de la antesala de la compuerta blindada." },
                new Act { label = "Reiniciar protocolo de alerta", type = ConsoleActionType.ReduceAlert, hack = true, diff = HackDifficulty.Hard, heat = 40f,
                          info = "Reduce la tensión de la nave en 40 puntos. Útil para salir de la Alerta Roja." },
            });
            ConsoleAt("TerminalInformacion", gameplay, new Vector3(-4.4f, 0f, 16f), 90f, "Terminal de información", new[]
            {
                new Act { label = "Estado de la nave e incidentes", type = ConsoleActionType.ShowInfo,
                          info = "Drones de élite: patrullan en pareja alrededor del holograma. En ALERTA ROJA cazan activamente.\n" +
                                 "Puente: tras la compuerta blindada. Antesala protegida por el Sector Puente." },
            });
            ConsoleAt("ControlCompuertas", gameplay, new Vector3(-18.5f, 0f, 27.6f), 0f, "Control de compuertas", new[]
            {
                new Act { label = "Planos del Centro de Control", type = ConsoleActionType.ShowInfo,
                          info = "Ala oeste: control de compuertas. Pasarela superior sobre el muro del ala (acceso por rampa desde la sala)." },
                new Act { label = "Autorizar compuerta blindada del puente", type = ConsoleActionType.UnlockDoor, hack = true, diff = HackDifficulty.Hard, door = lockdown, oneShot = true,
                          info = "Desbloquea la compuerta del puente. Si la nave entra en Alerta Roja, se sellará hasta que baje." },
            });
            ConsoleAt("ConsolaMando", gameplay, new Vector3(0f, 0f, 35f), 0f, "Consola de mando del capitán", new[]
            {
                new Act { label = "Registro de la nave", type = ConsoleActionType.ShowInfo },
                new Act { label = "Retomar el control de la nave", type = ConsoleActionType.CompletePhase, hack = true, diff = HackDifficulty.Hard, oneShot = true, next = "Evacuacion",
                          info = "IA NEXUS-7: Control de mando transferido... ¡Aviso! La brecha ha desestabilizado el reactor. Autodestrucción en curso. Dirígete al hangar de evacuación." },
            });

            // ---------- 4. Banco de trabajo y suministros (ala este) ----------
            var bench = L4.WorkbenchAt("BancoTrabajo_Este", gameplay, new Vector3(16.5f, 0f, 10f), 90f);
            A.Container("Contenedor_Este1", loot, new Vector3(18.8f, 0f, 18f), 90f, new[] { new ItemStack(items.Circuit, 2), new ItemStack(items.Scrap, 3) });
            A.Container("Contenedor_Este2", loot, new Vector3(18.8f, 0f, 23f), 90f, new[] { new ItemStack(items.Battery, 1), new ItemStack(p4.Crystal, 1) });
            A.Pickup("Pickup_Chatarra", loot, new Vector3(-5f, 0f, 7f), items.Scrap, 2, PrimitiveType.Cube, new Vector3(0.35f, 0.1f, 0.25f), A.scrapMat, 3f, false);
            A.Pickup("Pickup_CristalOptico", loot, new Vector3(-19f, 0f, 20f), p4.Crystal, 1, PrimitiveType.Capsule, new Vector3(0.12f, 0.18f, 0.12f), L4.crystalMat, 0f, true);

            // ---------- 5. Jugador ----------
            var (player, interactor) = B.BuildPlayer();
            player.transform.SetPositionAndRotation(new Vector3(0f, 0.05f, -3.2f), Quaternion.identity);
            var inventory = B.GetOrAdd<InventorySystem>(player.gameObject);
            A.SetStacks(inventory, "startingItems", new[]
            {
                new ItemStack(items.Scrap, 6), new ItemStack(items.Circuit, 3), new ItemStack(items.Battery, 1),
                new ItemStack(p4.Decoy, 1), new ItemStack(p4.Emp, 1), new ItemStack(p4.Camo, 1)
            });
            var stealth = player.gameObject.AddComponent<PlayerStealth>();
            player.gameObject.AddComponent<PlayerRespawn>();
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

            // ---------- 6. Drones ----------
            var enemies = new GameObject("Enemigos").transform;
            var eliteRoute = A.Route("Ruta_Elite_Anillo", enemies, PatrolRoute.RouteMode.Loop, new[]
            {
                new A.WP(new Vector3(-8f, 0f, 8f),  1.5f, true, 90f),
                new A.WP(new Vector3(-8f, 0f, 24f), 1.5f, true, 90f),
                new A.WP(new Vector3(8f, 0f, 24f),  1.5f, true, -90f),
                new A.WP(new Vector3(8f, 0f, 8f),   1.5f, true, -90f),
            });
            var e1 = EliteDrone("DronElite_1", enemies, new Vector3(-8f, 0f, 8f), 0f, eliteRoute, 0);
            var e2 = EliteDrone("DronElite_2", enemies, new Vector3(8f, 0f, 24f), 180f, eliteRoute, 2); // desfasado: lado opuesto
            var squad = new GameObject("Escuadron_Elite").AddComponent<DroneSquad>();
            squad.transform.SetParent(enemies);
            A.SetObjectArray(squad, "members", new Object[] { e1, e2 });

            var routeW = A.Route("Ruta_D3_AlaOeste", enemies, PatrolRoute.RouteMode.PingPong, new[]
            {
                new A.WP(new Vector3(-16.5f, 0f, 13f), 2f, true, 180f),   // vigila la entrada del ala
                new A.WP(new Vector3(-16.5f, 0f, 25f), 3f, true, 0f),     // vigila el control de compuertas
            });
            var d3 = A.Drone("DronSeguridad_AlaOeste", enemies, new Vector3(-16.5f, 0f, 13f), 180f, routeW);

            var routeE = A.Route("Ruta_R3_Banco", enemies, PatrolRoute.RouteMode.PingPong, new[]
            {
                new A.WP(new Vector3(15f, 0f, 10f), 4f, true, 90f),        // inspecciona el banco
                new A.WP(new Vector3(14f, 0f, 26f), 2f, true, 0f),
            });
            B.SetRef(routeE.transform.GetChild(0).GetComponent<Waypoint>(), "inspectTarget", bench);
            var r3 = L4.ResearchDrone("DronInvestigacion_AlaEste", enemies, new Vector3(15f, 0f, 10f), 90f, routeE, "Dron de investigación");

            var navGo = new GameObject("NavMesh");
            navGo.transform.position = new Vector3(0f, 4f, 19f);
            var baker = navGo.AddComponent<RuntimeNavMeshBaker>();
            A.SetVector3(baker, "size", new Vector3(44f, 10f, 44f));
            A.SetObjectArray(baker, "ignoreRoots", new Object[] { player.transform, e1.transform, e2.transform, d3.transform, r3.transform });

            // ---------- 7. Zonas ----------
            var zones = new GameObject("Zonas").transform;
            foreach (var z in new[]
            {
                new S3.Bx(new Vector3(-6f, 1.5f, 5.2f), new Vector3(3f, 3f, 1f)),   new S3.Bx(new Vector3(6f, 1.5f, 5.2f), new Vector3(3f, 3f, 1f)),
                new S3.Bx(new Vector3(-19f, 1.5f, 15f), new Vector3(1.6f, 3f, 3f)), new S3.Bx(new Vector3(19f, 1.5f, 13f), new Vector3(1.6f, 3f, 3f)),
                new S3.Bx(new Vector3(0f, 1.5f, 21.4f), new Vector3(1.8f, 3f, 1f)),
            })
                A.Zone("ZonaSombra", zones, z.pos, z.size).AddComponent<ShadowZone>();

            S3.Checkpoint("Checkpoint_Sala", zones, new Vector3(0f, 1.5f, 7f), new Vector3(12f, 3f, 1f));
            S3.Checkpoint("Checkpoint_AlaOeste", zones, new Vector3(-15.5f, 1.5f, 8f), new Vector3(5f, 3f, 1.2f));
            S3.Checkpoint("Checkpoint_Antesala", zones, new Vector3(0f, 1.5f, 28.6f), new Vector3(7.6f, 3f, 1f));
            S3.Checkpoint("Checkpoint_Puente", zones, new Vector3(0f, 1.5f, 31.5f), new Vector3(8f, 3f, 1f));

            A.MessageZone("Aviso_Consola", zones, new Vector3(0f, 1.5f, 9.4f), new Vector3(4f, 3f, 1f),
                "IA NEXUS-7: Consola de seguridad central. Hackéala para desactivar sectores enteros... si fallas, la nave sube su alerta.");
            A.MessageZone("Aviso_AlaOeste", zones, new Vector3(-15.5f, 1.5f, 4.2f), new Vector3(3f, 3f, 1.2f),
                "IA NEXUS-7: Ala oeste: control de compuertas. Láser y cámara del Sector Oeste. También hay una pasarela sobre el muro.");
            A.MessageZone("Aviso_Pasarela", zones, new Vector3(-10.1f, 1.5f, 12.8f), new Vector3(2f, 3f, 1f),
                "IA NEXUS-7: Rampa a la pasarela del ala oeste. Al fondo hay un hueco para dejarte caer dentro del ala.");
            A.MessageZone("Aviso_Antesala", zones, new Vector3(0f, 1.5f, 24.6f), new Vector3(8f, 3f, 1f),
                "IA NEXUS-7: Compuerta blindada del puente. Necesita autorización desde el ala oeste y se sella en Alerta Roja.");
            A.MessageZone("Aviso_AlaEste", zones, new Vector3(14f, 1.5f, 5.5f), new Vector3(6f, 3f, 1f),
                "IA NEXUS-7: Ala este: banco de trabajo y suministros. Un dron de investigación revisa el banco.");

            // ---------- 8. HUD y sistemas ----------
            var hud = B.BuildHUD();
            A.BuildPhase2UI(hud.transform, inventory, stealth);
            L4.BuildPhase4UI(hud.transform, quickbar, stealth);
            BuildPhase5UI(hud.transform);

            var systems = new GameObject("GameSystems");
            systems.AddComponent<AlertAmbience>();
            var intro = systems.AddComponent<LevelIntro>();
            B.SetRef(intro, "player", player);
            B.SetRef(intro, "hud", hud);
            B.SetStrings(intro, "messages", new[]
            {
                "IA NEXUS-7: Centro de Control. Desde aquí se gobierna toda la nave.",
                "IA NEXUS-7: La seguridad responde al NIVEL DE ALERTA de la nave, y todo lo que has hecho hasta ahora cuenta. Mantén la tensión baja.",
                "IA NEXUS-7: Las consolas permiten consultar el estado de la nave, desactivar sectores de seguridad y autorizar la compuerta del puente... si superas el hackeo.",
                "IA NEXUS-7: Los drones de élite patrullan en pareja. En Alerta Roja te cazarán."
            });
            B.SetString(intro, "hint", "[E] consolas · Hackeo: teclea la secuencia con [W][A][S][D] antes de que acabe el tiempo · Arriba: nivel de alerta de la nave");
            B.SetString(intro, "objective", "Objetivo: Autoriza la compuerta blindada (control de compuertas, ala oeste) y llega a la consola de mando del puente.");

            // ---------- 9. Prefabs, guardado y enlace ----------
            B.SavePrefab(e1.gameObject, "DronElite");
            B.SavePrefab(blast.gameObject, "CompuertaBlindada");
            B.SavePrefab(player.gameObject, "Player_Fase5");
            B.SavePrefab(hud.gameObject, "HUD_Fase5");

            EditorSceneManager.SaveScene(scene, ScenePath);
            var scenes = EditorBuildSettings.scenes.ToList();
            if (!scenes.Any(s => s.path == ScenePath)) scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            ConnectPhase4();

            Selection.activeGameObject = player.gameObject;
            Debug.Log("<color=cyan>[Nexus-7]</color> Centro de Control construido en " + ScenePath + ". Pulsa Play.");
        }

        [MenuItem("Nexus-7/Fase 5/Conectar salida de la Fase 4")]
        public static void ConnectPhase4()
        {
            if (!System.IO.File.Exists(Phase4ScenePath))
            {
                Debug.Log("[Nexus-7] No existe Laboratorio.unity: ejecuta 'Fase 4 > Construir Laboratorio' si quieres enlazarlas.");
                return;
            }
            string current = EditorSceneManager.GetActiveScene().path;
            var phase4 = EditorSceneManager.OpenScene(Phase4ScenePath, OpenSceneMode.Single);
            var exit = GameObject.Find("Salida_Montacargas");
            var trig = exit != null ? exit.GetComponent<SceneExitTrigger>() : null;
            if (trig != null)
            {
                B.SetString(trig, "sceneName", SceneName);
                EditorSceneManager.SaveScene(phase4);
                Debug.Log("[Nexus-7] Laboratorio enlazado con " + SceneName + ".");
            }
            if (!string.IsNullOrEmpty(current) && current != Phase4ScenePath)
                EditorSceneManager.OpenScene(current, OpenSceneMode.Single);
        }

        // =====================================================================
        //  ESTRUCTURA
        // =====================================================================

        private static void BuildShell(Transform env)
        {
            A.Tiled("Suelo", env, new Vector3(0f, -0.1f, 19f), new Vector3(40.6f, 0.2f, 38.6f), hallFloor);
            A.Tiled("Techo", env, new Vector3(0f, H + 0.1f, 19f), new Vector3(40.6f, 0.2f, 38.6f), hallWall);
            A.Tiled("MuroOeste", env, new Vector3(-20.15f, H / 2, 19f), new Vector3(0.3f, H, 38.6f), hallWall);
            A.Tiled("MuroEste", env, new Vector3(20.15f, H / 2, 19f), new Vector3(0.3f, H, 38.6f), hallWall);
            A.Tiled("MuroSur_Izq", env, new Vector3(-10.9f, H / 2, -0.15f), new Vector3(18.8f, H, 0.3f), hallWall);
            A.Tiled("MuroSur_Der", env, new Vector3(10.9f, H / 2, -0.15f), new Vector3(18.8f, H, 0.3f), hallWall);
            B.Box("MuroSur_Dintel", env, new Vector3(0f, 7.5f, -0.15f), new Vector3(3f, 9f, 0.3f), hallWall);
            A.Tiled("MuroNorte", env, new Vector3(0f, H / 2, 38.15f), new Vector3(40.6f, H, 0.3f), hallWall);

            // Muro del puente (z=30) con la compuerta blindada (x -2..2, 4 m de alto)
            A.Tiled("MuroPuente_Izq", env, new Vector3(-11f, H / 2, 30f), new Vector3(18f, H, 0.6f), hallWall);
            A.Tiled("MuroPuente_Der", env, new Vector3(11f, H / 2, 30f), new Vector3(18f, H, 0.6f), hallWall);
            B.Box("MuroPuente_Dintel", env, new Vector3(0f, 8f, 30f), new Vector3(4f, 8f, 0.6f), hallWall);
            B.NoCollider(B.Box("FranjaCompuerta", env, new Vector3(0f, 4.15f, 29.68f), new Vector3(4.4f, 0.2f, 0.02f), A.warningMat));

            // Antesala de la compuerta: paredes bajas que obligan a cruzar el láser del Sector Puente
            B.Box("Antesala_O", env, new Vector3(-4f, 1.5f, 27.85f), new Vector3(0.2f, 3f, 3.7f), hallWall);
            B.Box("Antesala_E", env, new Vector3(4f, 1.5f, 27.85f), new Vector3(0.2f, 3f, 3.7f), hallWall);

            // Vestíbulo de llegada
            B.Box("Vest_Suelo", env, new Vector3(0f, -0.1f, -2.5f), new Vector3(5.4f, 0.2f, 5f), hallFloor);
            B.Box("Vest_Techo", env, new Vector3(0f, 4.1f, -2.5f), new Vector3(5.4f, 0.2f, 5f), hallWall);
            B.Box("Vest_MuroO", env, new Vector3(-2.6f, 2f, -2.5f), new Vector3(0.2f, 4f, 5f), hallWall);
            B.Box("Vest_MuroE", env, new Vector3(2.6f, 2f, -2.5f), new Vector3(0.2f, 4f, 5f), hallWall);
            B.Box("Vest_MuroS", env, new Vector3(0f, 2f, -5.1f), new Vector3(5.4f, 4f, 0.2f), hallWall);
        }

        private static void BuildWestWing(Transform env)
        {
            var root = new GameObject("AlaOeste").transform;
            root.SetParent(env);
            // Muro sur del ala (z=6) con entrada x -17..-14 (vigilada por el láser del Sector Oeste)
            B.Box("MuroS_A", root, new Vector3(-18.5f, 2f, 6f), new Vector3(3f, 4f, 0.4f), hallWall);
            B.Box("MuroS_B", root, new Vector3(-13f, 2f, 6f), new Vector3(2f, 4f, 0.4f), hallWall);
            B.Box("MuroS_Dintel", root, new Vector3(-15.5f, 3.5f, 6f), new Vector3(3f, 1f, 0.4f), hallWall);
            // Muro este del ala (x=-12): sobre él va la pasarela
            A.Tiled("MuroE", root, new Vector3(-12f, 2f, 18f), new Vector3(0.4f, 4f, 24f), hallWall);
            B.NoCollider(B.Box("FranjaSeguridad", root, new Vector3(-11.79f, 1f, 18f), new Vector3(0.02f, 0.25f, 24f), A.warningMat));
        }

        private static void BuildCatwalk(Transform env)
        {
            var root = new GameObject("PasarelaOeste").transform;
            root.SetParent(env);
            B.Box("Pasarela", root, new Vector3(-12f, CatY - 0.1f, 18f), new Vector3(1.8f, 0.2f, 23.6f), A.metalMat);
            S3.RailZ(root, -11.15f, 6.2f, 21f); S3.RailZ(root, -11.15f, 23f, 29.8f);    // hueco: rellano de la rampa
            S3.RailZ(root, -12.85f, 6.2f, 24f); S3.RailZ(root, -12.85f, 25.6f, 29.8f);  // hueco: caída al ala oeste
            S3.RailX(root, 6.25f, -12.9f, -11.1f); S3.RailX(root, 29.75f, -12.9f, -11.1f);
            S3.Ramp("Rampa", root, -10.1f, 1.6f, 14f, 0f, 21f, CatY, 3f, A.metalMat);
            B.Box("Rellano", root, new Vector3(-10.2f, CatY / 2f, 22f), new Vector3(1.8f, CatY, 2f), hallWall);
            S3.RailZ(root, -9.35f, 21f, 23f);
            S3.RailX(root, 22.95f, -11.1f, -9.3f);
            B.Box("CajasCaida", root, new Vector3(-13.85f, 1f, 24.8f), new Vector3(1.9f, 2f, 1.9f), A.crateGreen);
        }

        private static void BuildScreensAndLights(Transform env)
        {
            var root = new GameObject("PantallasEIluminacion").transform;
            root.SetParent(env);

            // Pantallas gigantes del muro del puente: la luz dramática de la sala
            for (int i = -2; i <= 2; i++)
            {
                var scr = B.NoCollider(B.Box("PantallaMando", root, new Vector3(i * 6f, 7f, 29.67f), new Vector3(5f, 3f, 0.04f), screenMat));
                var al = scr.AddComponent<AlertScreen>();
                if (i % 2 == 0)
                {
                    var l = A.PointLight("LuzPantalla", root, new Vector3(i * 6f, 7f, 28.5f), new Color(0.2f, 0.65f, 1f), 14f, 1.5f, false);
                    B.SetRef(al, "screenLight", l);
                }
            }
            foreach (float z in new[] { 8f, 16f, 24f })
            {
                var scr = B.NoCollider(B.Box("PantallaLateral", root, new Vector3(19.97f, 6.5f, z), new Vector3(0.04f, 3f, 5f), screenMat));
                scr.AddComponent<AlertScreen>();
            }
            var scrW = B.NoCollider(B.Box("PantallaAlaOeste", root, new Vector3(-19.97f, 6.5f, 18f), new Vector3(0.04f, 3f, 6f), screenMat));
            scrW.AddComponent<AlertScreen>();

            // Lámparas de techo que siguen la alerta
            var lamps = new[]
            {
                new Vector3(-8f, 11.4f, 8f), new Vector3(8f, 11.4f, 8f), new Vector3(-8f, 11.4f, 22f), new Vector3(8f, 11.4f, 22f),
                new Vector3(0f, 11.4f, 15f), new Vector3(-16f, 11.4f, 18f), new Vector3(16f, 11.4f, 18f), new Vector3(0f, 11.4f, 3f),
                new Vector3(-5f, 11.4f, 34f), new Vector3(5f, 11.4f, 34f),
            };
            for (int i = 0; i < lamps.Length; i++)
            {
                var fixture = B.NoCollider(B.Box("Lampara", root, lamps[i] + Vector3.up * 0.3f, new Vector3(2.4f, 0.1f, 0.5f), lampMat));
                var l = A.PointLight("LuzAlerta", root, lamps[i] - Vector3.up * 0.3f, new Color(0.55f, 0.78f, 1f), 16f, 1.6f, i < 4);
                var al = l.gameObject.AddComponent<AlertLight>();
                B.SetRef(al, "emissiveRenderer", fixture.GetComponent<Renderer>());
            }
            A.PointLight("LuzVestibulo", root, new Vector3(0f, 3.7f, -2.5f), new Color(0.55f, 0.78f, 1f), 6f, 0.8f, false).gameObject.AddComponent<AlertLight>();
        }

        private static void BuildHologram(Transform parent)
        {
            var root = new GameObject("Holograma").transform;
            root.SetParent(parent);
            root.localPosition = new Vector3(0f, 0f, 16f);

            var dais = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            dais.name = "Estrado";
            dais.transform.SetParent(root, false);
            dais.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            dais.transform.localScale = new Vector3(6f, 0.2f, 6f);
            dais.GetComponent<Renderer>().sharedMaterial = consoleMat;
            var emitter = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            emitter.name = "Proyector";
            Object.DestroyImmediate(emitter.GetComponent<Collider>());
            emitter.transform.SetParent(root, false);
            emitter.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            emitter.transform.localScale = new Vector3(1.4f, 0.05f, 1.4f);
            emitter.GetComponent<Renderer>().sharedMaterial = holoMat;

            var core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            core.name = "NucleoHolografico";
            Object.DestroyImmediate(core.GetComponent<Collider>());
            core.transform.SetParent(root, false);
            core.transform.localPosition = new Vector3(0f, 2.6f, 0f);
            core.transform.localScale = Vector3.one * 1.1f;
            core.GetComponent<Renderer>().sharedMaterial = holoMat;

            var rings = new List<Transform>();
            var lines = new List<Object>();
            float[] radii = { 1.6f, 2.1f, 2.6f };
            for (int i = 0; i < radii.Length; i++)
            {
                var ring = new GameObject("Anillo" + i);
                ring.transform.SetParent(root, false);
                ring.transform.localPosition = new Vector3(0f, 2.6f, 0f);
                ring.transform.localRotation = Quaternion.Euler(20f * i, 30f * i, 10f * i);
                var lr = ring.AddComponent<LineRenderer>();
                lr.useWorldSpace = false;
                lr.loop = true;
                lr.sharedMaterial = S3.arcMat;
                lr.widthMultiplier = 0.04f;
                lr.shadowCastingMode = ShadowCastingMode.Off;
                const int seg = 64;
                lr.positionCount = seg;
                for (int k = 0; k < seg; k++)
                {
                    float a = k * Mathf.PI * 2f / seg;
                    lr.SetPosition(k, new Vector3(Mathf.Cos(a) * radii[i], 0f, Mathf.Sin(a) * radii[i]));
                }
                rings.Add(ring.transform);
                lines.Add(lr);
            }
            var glow = A.PointLight("LuzHolograma", root, new Vector3(0f, 2.6f, 0f), new Color(0.3f, 0.8f, 1f), 12f, 2.5f, false);

            var holo = root.gameObject.AddComponent<HologramDisplay>();
            B.SetRef(holo, "core", core.transform);
            B.SetRef(holo, "coreRenderer", core.GetComponent<Renderer>());
            A.SetObjectArray(holo, "rings", rings.Cast<Object>().ToArray());
            A.SetObjectArray(holo, "ringLines", lines.ToArray());
            B.SetRef(holo, "glow", glow);
        }

        private static void BuildDecor(Transform parent)
        {
            // Consolas decorativas del anillo central y mesas de monitorización
            DecorConsole(parent, new Vector3(4.4f, 0f, 16f), -90f);
            DecorConsole(parent, new Vector3(0f, 0f, 20.4f), 180f);
            foreach (float x in new[] { -6f, 6f })
            {
                L4.Local(B.Box("MesaMonitorizacion", parent, Vector3.zero, new Vector3(3f, 0.9f, 1f), consoleMat), new Vector3(x, 0.45f, 4f));
                for (int i = -1; i <= 1; i += 2)
                {
                    var scr = B.NoCollider(B.Box("Monitor", parent, new Vector3(x + i * 0.7f, 1.3f, 4.4f), new Vector3(1.1f, 0.6f, 0.05f), screenMat));
                    scr.AddComponent<AlertScreen>();
                }
            }

            // Puente: sillón del capitán, consolas laterales y ventanal estelar
            B.Box("SillonCapitan", parent, new Vector3(0f, 0.5f, 33.6f), new Vector3(0.9f, 1f, 0.9f), consoleMat);
            DecorConsole(parent, new Vector3(-5f, 0f, 35f), 0f);
            DecorConsole(parent, new Vector3(5f, 0f, 35f), 0f);
            B.NoCollider(B.Box("Ventanal", parent, new Vector3(0f, 5f, 37.97f), new Vector3(16f, 6f, 0.04f), starMat));
            var rng = new System.Random(9);
            for (int i = 0; i < 60; i++)
            {
                float x = (float)rng.NextDouble() * 15f - 7.5f, y = 2.2f + (float)rng.NextDouble() * 5.6f;
                float s = 0.03f + (float)rng.NextDouble() * 0.05f;
                B.NoCollider(B.Box("Estrella", parent, new Vector3(x, y, 37.94f), new Vector3(s, s, 0.01f), starDot));
            }
        }

        internal static void DecorConsole(Transform parent, Vector3 pos, float rotY)
        {
            var root = new GameObject("ConsolaDecorativa").transform;
            root.SetParent(parent);
            root.SetPositionAndRotation(pos, Quaternion.Euler(0f, rotY, 0f));
            L4.Local(B.Box("Cuerpo", root, Vector3.zero, new Vector3(1.6f, 1.1f, 0.7f), consoleMat), new Vector3(0f, 0.55f, 0f));
            var scr = L4.Local(B.NoCollider(B.Box("Pantalla", root, Vector3.zero, new Vector3(1.3f, 0.5f, 0.05f), screenMat)), new Vector3(0f, 1.3f, -0.2f));
            scr.transform.localRotation = Quaternion.Euler(-25f, 0f, 0f);
            scr.AddComponent<AlertScreen>();
        }

        internal static ControlConsole ConsoleAt(string name, Transform parent, Vector3 pos, float rotY, string consoleName, Act[] acts)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent);
            root.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, rotY, 0f));
            L4.Local(B.Box("Cuerpo", root.transform, Vector3.zero, new Vector3(1.6f, 1.1f, 0.7f), consoleMat), new Vector3(0f, 0.55f, 0f));
            var scr = L4.Local(B.NoCollider(B.Box("Pantalla", root.transform, Vector3.zero, new Vector3(1.3f, 0.5f, 0.05f), screenMat)), new Vector3(0f, 1.3f, -0.2f));
            scr.transform.localRotation = Quaternion.Euler(-25f, 0f, 0f);
            var holo = L4.Local(B.NoCollider(B.Box("Holo", root.transform, Vector3.zero, new Vector3(0.9f, 0.5f, 0.01f), glassMat)), new Vector3(0f, 1.9f, -0.1f));
            holo.name = "PanelHolografico";
            A.PointLight("LuzConsola", root.transform, new Vector3(0f, 1.6f, -0.8f), new Color(0.3f, 0.8f, 1f), 3f, 1f, false);

            var console = root.AddComponent<ControlConsole>();
            B.SetString(console, "consoleName", consoleName);
            B.SetRef(console, "screenRenderer", scr.GetComponent<Renderer>());

            var so = new SerializedObject(console);
            var list = so.FindProperty("actions");
            list.arraySize = acts.Length;
            for (int i = 0; i < acts.Length; i++)
            {
                var e = list.GetArrayElementAtIndex(i);
                var a = acts[i];
                e.FindPropertyRelative("label").stringValue = a.label;
                e.FindPropertyRelative("type").enumValueIndex = (int)a.type;
                e.FindPropertyRelative("requiresHack").boolValue = a.hack;
                e.FindPropertyRelative("difficulty").enumValueIndex = (int)a.diff;
                e.FindPropertyRelative("sector").objectReferenceValue = a.sector;
                e.FindPropertyRelative("duration").floatValue = a.duration > 0f ? a.duration : 40f;
                e.FindPropertyRelative("door").objectReferenceValue = a.door;
                e.FindPropertyRelative("heatReduction").floatValue = a.heat > 0f ? a.heat : 40f;
                e.FindPropertyRelative("nextScene").stringValue = a.next ?? "";
                e.FindPropertyRelative("infoText").stringValue = a.info ?? "";
                e.FindPropertyRelative("oneShot").boolValue = a.oneShot;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            return console;
        }

        internal static SecuritySector Sector(string name, Transform parent, string label, MonoBehaviour[] systems)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            var sector = go.AddComponent<SecuritySector>();
            B.SetString(sector, "sectorName", label);
            A.SetObjectArray(sector, "systems", systems.Cast<Object>().ToArray());
            return sector;
        }

        internal static EnemyPatrol EliteDrone(string name, Transform parent, Vector3 pos, float rotY, PatrolRoute route, int startWp)
        {
            var drone = A.Drone(name, parent, pos, rotY, route);
            var body = drone.transform.Find("Cuerpo");
            if (body != null) body.GetComponent<Renderer>().sharedMaterial = eliteMat;
            // Hombreras blindadas
            foreach (float x in new[] { -0.45f, 0.45f })
                L4.Local(B.NoCollider(B.Box("Blindaje", drone.transform, Vector3.zero, new Vector3(0.25f, 0.5f, 0.6f), eliteMat)), new Vector3(x, 1.25f, 0f));

            var stun = new GameObject("EfectoAturdido");
            stun.transform.SetParent(drone.transform, false);
            stun.transform.localPosition = new Vector3(0f, 2.1f, 0f);
            A.PointLight("LuzAturdido", stun.transform, Vector3.zero, new Color(0.3f, 0.8f, 1f), 3f, 2f, false);
            S3.Arc("ArcoAturdido", stun.transform, new Vector3(-0.35f, 0f, 0f), new Vector3(0.7f, 0f, 0f), 0.2f, 0.03f);

            var vision = drone.GetComponent<EnemyVision>();
            A.SetFloat(vision, "viewDistance", 18f);
            A.SetFloat(vision, "viewAngle", 100f);
            A.SetFloat(vision, "detectionSpeed", 1.5f);
            A.SetFloat(vision, "proximityRadius", 2.2f);
            A.SetColor(vision, "calmColor", new Color(1f, 0.55f, 0.45f));

            A.SetFloat(drone, "patrolSpeed", 2.2f);
            A.SetFloat(drone, "investigateSpeed", 3.2f);
            A.SetFloat(drone, "chaseSpeed", 4.6f);   // ×1.3 en Roja = 5.98: justo por debajo de tu carrera (6)
            A.SetFloat(drone, "alarmSpeed", 4.4f);
            A.SetFloat(drone, "catchDistance", 1.5f);
            A.SetFloat(drone, "hearingMultiplier", 1.4f);
            A.SetFloat(drone, "searchDuration", 6f);
            A.SetBool(drone, "aggressiveOnRed", true);
            A.SetInt(drone, "startWaypoint", startWp);
            B.SetString(drone, "displayName", "Dron de élite");
            B.SetRef(drone, "stunEffect", stun);
            return drone;
        }

        // =====================================================================
        //  UI DE LA FASE 5
        // =====================================================================

        internal static void BuildPhase5UI(Transform canvas)
        {
            // Indicador de alerta (arriba, centro)
            var label = B.UIText("TextoAlerta", canvas, new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(600f, 34f), 26f, TextAlignmentOptions.Center, Color.white);
            label.rectTransform.pivot = new Vector2(0.5f, 1f);
            var (fill, bg) = S3.Bar("BarraTension", canvas, new Vector2(0.5f, 1f), new Vector2(-150f, -50f));
            foreach (float t in new[] { AlertSystem.YellowThreshold, AlertSystem.RedThreshold })
            {
                var tick = B.UIImage("Umbral", bg.transform, new Color(1f, 1f, 1f, 0.8f));
                B.Anchor(tick.rectTransform, new Vector2(t / 100f, 0.5f), new Vector2(t / 100f, 0.5f), Vector2.zero, new Vector2(2f, 16f));
            }
            var alertHud = canvas.gameObject.AddComponent<UI.AlertHUD>();
            B.SetRef(alertHud, "label", label);
            B.SetRef(alertHud, "heatFill", fill);

            // Menú de consola
            var panel = B.UIImage("PanelConsola", canvas, new Color(0.01f, 0.04f, 0.07f, 0.93f));
            B.Anchor(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, 580f));
            var menu = B.UIText("MenuConsola", panel.transform, new Vector2(0.5f, 0.5f), new Vector2(-245f, 0f), new Vector2(460f, 540f), 24f, TextAlignmentOptions.TopLeft, Color.white);
            var info = B.UIText("InfoConsola", panel.transform, new Vector2(0.5f, 0.5f), new Vector2(245f, 0f), new Vector2(470f, 540f), 20f, TextAlignmentOptions.TopLeft, Color.white);
            var cUi = canvas.gameObject.AddComponent<UI.ConsoleUI>();
            B.SetRef(cUi, "panel", panel.gameObject);
            B.SetRef(cUi, "menuText", menu);
            B.SetRef(cUi, "infoText", info);

            // Minijuego de hackeo
            var hp = B.UIImage("PanelHackeo", canvas, new Color(0.01f, 0.04f, 0.07f, 0.95f));
            B.Anchor(hp.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(920f, 300f));
            var title = B.UIText("TituloHackeo", hp.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 105f), new Vector2(860f, 50f), 28f, TextAlignmentOptions.Center, new Color(0.6f, 0.9f, 1f));
            var seq = B.UIText("Secuencia", hp.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(880f, 90f), 46f, TextAlignmentOptions.Center, Color.white);
            var status = B.UIText("EstadoHackeo", hp.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -60f), new Vector2(880f, 40f), 24f, TextAlignmentOptions.Center, Color.white);
            var (tFill, tBg) = S3.Bar("TiempoHackeo", hp.transform, new Vector2(0.5f, 0f), new Vector2(-150f, 30f));
            var hUi = canvas.gameObject.AddComponent<UI.HackMinigameUI>();
            B.SetRef(hUi, "panel", hp.gameObject);
            B.SetRef(hUi, "title", title);
            B.SetRef(hUi, "sequenceText", seq);
            B.SetRef(hUi, "statusText", status);
            B.SetRef(hUi, "timerFill", tFill);
            B.SetRef(hUi, "panelBackground", hp);
        }

        // =====================================================================
        //  MATERIALES E ILUMINACIÓN
        // =====================================================================

        private static void ConfigureLighting()
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.03f, 0.045f, 0.07f); // AlertAmbience lo anima en juego
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.02f, 0.03f, 0.05f);
            RenderSettings.fogDensity = 0.025f;
        }

        internal static void CreateMaterials()
        {
            hallFloor  = B.CreateMat("M5_SueloMando", new Color(0.12f, 0.14f, 0.17f), null, 0.75f, 0.8f);
            hallWall   = B.CreateMat("M5_PanelMando", new Color(0.2f, 0.23f, 0.28f), null, 0.5f, 0.7f);
            consoleMat = B.CreateMat("M5_Consola", new Color(0.1f, 0.12f, 0.15f), null, 0.7f, 0.8f);
            screenMat  = B.CreateMat("M5_Pantalla", new Color(0.05f, 0.15f, 0.25f), new Color(0.2f, 0.65f, 1f) * 2f, 0.9f, 0f);
            holoMat    = B.CreateMat("M5_Holograma", new Color(0.3f, 0.8f, 1f), new Color(0.3f, 0.8f, 1f) * 2.5f, 0.9f, 0f);
            eliteMat   = B.CreateMat("M5_DronElite", new Color(0.08f, 0.06f, 0.06f), new Color(0.6f, 0.05f, 0.03f) * 0.4f, 0.85f, 0.9f);
            starMat    = B.CreateMat("M5_Espacio", new Color(0.01f, 0.01f, 0.03f), new Color(0.02f, 0.03f, 0.08f), 0.95f, 0f);
            starDot    = B.CreateMat("M5_Estrella", Color.white, Color.white * 2f);
            lampMat    = B.CreateMat("M5_Lampara", Color.black, new Color(0.55f, 0.78f, 1f) * 2f);
            glassMat   = B.CreateMat("M5_PanelHolografico", new Color(0.3f, 0.8f, 1f), new Color(0.3f, 0.8f, 1f) * 1.2f, 0.95f, 0f);
        }
    }
}
