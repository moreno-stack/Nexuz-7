using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Nexus7.Core;
using Nexus7.Enemies;
using Nexus7.Gameplay;
using Nexus7.Hazards;
using Nexus7.Interaction;
using Nexus7.Inventory;
using Nexus7.Player;
using Nexus7.Power;
using Nexus7.Security;
using Nexus7.Stealth;
using Nexus7.Tutorial;
using A = Nexus7.EditorTools.AlmacenCargaBuilder;   // ayudantes de la Fase 2
using B = Nexus7.EditorTools.SalaInicialBuilder;    // ayudantes de la Fase 1

namespace Nexus7.EditorTools
{
    /// <summary>
    /// Menú "Nexus-7 > Fase 3 > Construir Sala de Energía".
    ///
    /// Plano (vista cenital, Z = norte). Muros interiores de 4 m con PASARELAS encima (y = 4.2).
    ///
    ///  z=44        ┌Montacargas┐ (salida)
    ///  z=40 ═══════════════════[ M ]═══   M = compuerta principal (4/4 energía)
    ///       │ ZÓCALO B│ ══pasarela norte══ │         │
    ///       │  (G2)   │ [PLATAFORMA REACTOR]│         │   C1 = cámara sobre la plataforma
    ///       │ ALA     │  C1  núcleo  consola│ ALA     │   H2 = terminal de hackeo (en la pasarela)
    ///       │ OESTE   │   [S2] ←rampa       │ ESTE    │   S2 = acceso reactor (2/4)
    ///       │  ↑rampa │ G1 ←vigila→         │ rampa↑  │   ⚡ = cable dañado (aturde)  · INTERRUPTOR
    ///       │         │ T  ⚡arco⚡  T      │ ⚡⚡⚡⚡⚡ │
    ///       │ foso↑   │ T          T        │         │
    ///  z=10 ══[BARRERA]═foso══════ abierto ═════[S1]══   S1 = puerta secundaria (1/4) · ⚡ delante
    ///       │ H1  escotilla      ZONA SUR (llegada)     │
    ///  z=0  ════════════════[entrada]══════════════════   ZÓCALO A junto a la entrada
    ///                        vestíbulo
    ///
    /// Energía (4 bloques): Zócalo A (+1) · Interruptor ala este (+1) · Zócalo B ala oeste (+1) · Reactor (+1, requiere 3 y un circuito)
    /// Rutas al ala oeste: barrera (hackear con circuito) · foso bajo el muro (escotilla con la llave) · pasarelas (desde el ala este).
    /// </summary>
    public static class SalaEnergiaBuilder
    {
        private const string Root = "Assets/_Nexus7";
        private const string ScenePath = Root + "/Scenes/SalaEnergia.unity";
        private const string Phase2ScenePath = Root + "/Scenes/AlmacenCarga.unity";
        public const string SceneName = "SalaEnergia";

        private const float H = 10f, T = 0.3f, WallH = 4f, CatwalkY = 4.2f;
        private const int IgnoreRaycastLayer = 2;

        internal static Material panelMat, transformerMat, cableMat, coreMat, cameraMat, screenMat, sparkMat, arcMat;
        internal static readonly Color ArcBlue = new Color(0.55f, 0.8f, 1f);

        [MenuItem("Nexus-7/Fase 3/Construir Sala de Energía")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            B.CreateFolders();
            var items = A.EnsureItems();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            A.CreateMaterials();
            CreateMaterials();
            ConfigureLighting();

            var env = new GameObject("Entorno").transform;
            var gameplay = new GameObject("Gameplay").transform;
            var hazards = new GameObject("Peligros").transform;
            var security = new GameObject("Seguridad").transform;

            // ---------- 1. Red eléctrica ----------
            var gridGo = new GameObject("RedElectrica");
            var grid = gridGo.AddComponent<PowerGrid>();
            B.SetStrings(grid, "messageByPowerLevel", new[]
            {
                "",
                "IA NEXUS-7: Energía al 25 %. Puerta secundaria del ala este desbloqueada.",
                "IA NEXUS-7: Energía al 50 %. Acceso a la plataforma del reactor desbloqueado.",
                "IA NEXUS-7: Energía al 75 %. El reactor ya puede arrancarse desde su consola.",
                "IA NEXUS-7: ¡Energía restaurada al 100 %! Compuerta principal desbloqueada."
            });
            B.SetString(grid, "completedObjective", "Objetivo: Energía restaurada. Sal por la compuerta principal (ala este, muro norte).");

            // ---------- 2. Estructura ----------
            BuildShell(env);
            BuildInteriorWalls(env);
            BuildCatwalks(env);
            BuildTrench(env, gameplay, items.Wrench);
            var (reactorConsole, reactorDoor) = BuildReactor(env, gameplay, items.Circuit);
            BuildTransformers(env);

            // ---------- 3. Fuentes de energía ----------
            var socketA = Socket("ZocaloBateria_A", gameplay, new Vector3(-4f, 1.2f, 0.18f), 0f, items.Battery, "Zócalo de emergencia (entrada)");
            Socket("ZocaloBateria_B", gameplay, new Vector3(-13f, 1.2f, 39.82f), 180f, items.Battery, "Zócalo de batería (ala oeste)");
            Breaker(gameplay, new Vector3(17.8f, 1.3f, 27f));

            // ---------- 4. Puertas alimentadas ----------
            var s1 = A.SlidingDoor("PuertaSecundaria_Este", gameplay, new Vector3(13.5f, 0f, 10f), new Vector3(3f, 3f, 0.2f),
                                   new Vector3(3.1f, 0f, 0f), A.metalMat, new Vector3(-2.05f, 2.2f, -0.25f));
            PoweredDoor(s1, 1, "Puerta sin energía", "PUERTA SECUNDARIA: sin energía. Restaura al menos 1 bloque.");
            PoweredDoor(reactorDoor, 2, "Acceso al reactor bloqueado", "ACCESO RESTRINGIDO: se requieren 2 bloques de energía en la red.");
            var mainDoor = A.SlidingDoor("CompuertaPrincipal", gameplay, new Vector3(13.5f, 0f, 40f + T / 2f), new Vector3(3f, 3.5f, 0.2f),
                                         new Vector3(0f, 3.4f, 0f), A.metalMat, new Vector3(-2.05f, 2.4f, -0.3f));
            PoweredDoor(mainDoor, 4, "Compuerta principal sin energía", "COMPUERTA PRINCIPAL: requiere la energía de la sala al 100 % (4/4).");

            // ---------- 5. Trampas eléctricas ----------
            var hz1 = Hazard("CableDanado_PuertaEste", hazards, new Vector3(13.5f, 1f, 8.7f), new Vector3(4.2f, 2f, 1.4f),
                             ElectricHazard.HazardEffect.Damage, ElectricHazard.HazardPattern.Intermittent, 25f, 0.8f, 1.6f, 2.4f, 0f, 0, true);
            RepairPanelAt("PanelDerivacion_PuertaEste", hazards, new Vector3(10.6f, 1.3f, 9.74f), new Vector3(0.5f, 0.7f, 0.12f), hz1, items.Scrap);

            var hz2 = Hazard("CableDanado_AlaEste", hazards, new Vector3(13.1f, 1f, 20f), new Vector3(9.8f, 2f, 1.2f),
                             ElectricHazard.HazardEffect.Stun, ElectricHazard.HazardPattern.Intermittent, 10f, 2.2f, 2.2f, 2.0f, 1f, 0, true);
            RepairPanelAt("PanelDerivacion_AlaEste", hazards, new Vector3(17.82f, 1.3f, 17.8f), new Vector3(0.12f, 0.7f, 0.5f), hz2, items.Scrap);

            // Arco entre transformadores: solo vive cuando la red tiene energía (¡restaurarla crea peligros!)
            Hazard("ArcoTransformadores", hazards, new Vector3(0f, 1f, 20.5f), new Vector3(6.5f, 2f, 0.9f),
                   ElectricHazard.HazardEffect.Damage, ElectricHazard.HazardPattern.Constant, 20f, 0.7f, 0f, 0f, 0f, 1, false);

            // ---------- 6. Seguridad: barrera + cámara + terminales de hackeo ----------
            var barrier = Barrier(security, new Vector3(-16f, 1.5f, 10f));
            Terminal("TerminalHackeo_Barrera", security, new Vector3(-12.9f, 0f, 9.5f), 0f, barrier, items.Circuit, 25f);
            var cam = SecurityCam(security, new Vector3(-4.6f, 1f, 37f), 150f);
            Terminal("TerminalHackeo_Camara", security, new Vector3(-3f, CatwalkY, 39.3f), 0f, cam, items.Circuit, 30f);

            // ---------- 7. Luces dinámicas ----------
            BuildLights(env);
            Sparks("ChispasPanelSur", env, new Vector3(8f, 2.2f, 0.25f));
            Sparks("ChispasAlaOeste", env, new Vector3(-17.9f, 2.5f, 30f));

            // ---------- 8. Botín ----------
            var loot = new GameObject("Botin").transform;
            A.Container("Contenedor_AlaOeste", loot, new Vector3(-17.2f, 0f, 22f), -90f,
                new[] { new ItemStack(items.Battery, 1), new ItemStack(items.Scrap, 2) });
            A.Container("Contenedor_AlaEste", loot, new Vector3(16.9f, 0f, 13f), 90f,
                new[] { new ItemStack(items.Circuit, 1), new ItemStack(items.Scrap, 2) });
            foreach (var p in new[] { new Vector3(-6f, 0f, 6f), new Vector3(6.6f, 0f, 23f), new Vector3(-14.5f, 0f, 36.5f) })
                A.Pickup("Pickup_Chatarra", loot, p, items.Scrap, 2, PrimitiveType.Cube, new Vector3(0.35f, 0.1f, 0.25f), A.scrapMat, 3f, false);

            var log = B.Box("DiarioIngeniero", env, new Vector3(-2.47f, 1.4f, -3f), new Vector3(0.06f, 0.4f, 0.5f), A.warningMat);
            var logMsg = log.AddComponent<InteractableMessage>();
            B.SetString(logMsg, "promptText", "Leer diario del ingeniero jefe");
            B.SetStrings(logMsg, "messages", new[]
            {
                "DIARIO DEL INGENIERO JEFE: «Tras la brecha, el reactor entró en parada. Para rearrancarlo hacen falta 3 bloques en la red: dos baterías y el interruptor general del ala este.»",
                "«La consola del reactor quedó dañada: lleva un circuito de control de repuesto.»",
                "«Los drones de guardia no se separan de las consolas. Las pasarelas superiores son el camino más discreto: agáchate tras las barandillas.»"
            });

            // ---------- 9. Jugador ----------
            var (player, interactor) = B.BuildPlayer();
            player.transform.SetPositionAndRotation(new Vector3(0f, 0.05f, -3.2f), Quaternion.identity);
            var inventory = player.gameObject.AddComponent<InventorySystem>();
            // Equipo por defecto si se juega esta escena directamente (si vienes del Almacén, se usa tu inventario real).
            A.SetStacks(inventory, "startingItems", new[]
            {
                new ItemStack(items.Battery, 1), new ItemStack(items.Circuit, 3), new ItemStack(items.Scrap, 6), new ItemStack(items.Wrench, 1)
            });
            var stealth = player.gameObject.AddComponent<PlayerStealth>();
            player.gameObject.AddComponent<PlayerRespawn>();
            var health = player.gameObject.AddComponent<PlayerHealth>();

            // ---------- 10. Drones de guardia ----------
            var enemies = new GameObject("Enemigos").transform;
            var routeG1 = A.Route("Ruta_G1_Reactor", enemies, PatrolRoute.RouteMode.PingPong, new[]
            {
                new A.WP(new Vector3(-6f, 0f, 25f), 3f, true, 180f),   // vigila la sala hacia el sur
                new A.WP(new Vector3(6f, 0f, 25f),  3f, true, 180f),
            });
            var routeG2 = A.Route("Ruta_G2_ZocaloOeste", enemies, PatrolRoute.RouteMode.Loop, new[]
            {
                new A.WP(new Vector3(-16f, 0f, 16f),   1f, false, 0f),
                new A.WP(new Vector3(-16f, 0f, 34f),   2.5f, true, 0f),     // mira al zócalo B
                new A.WP(new Vector3(-11.5f, 0f, 34f), 1f, false, 0f),
                new A.WP(new Vector3(-11.5f, 0f, 24f), 2f, true, 180f),     // vigila rampa y salida del foso
            });
            var g1 = A.Drone("DronGuardia_Reactor", enemies, new Vector3(-6f, 0f, 25f), 90f, routeG1);
            var g2 = A.Drone("DronGuardia_AlaOeste", enemies, new Vector3(-16f, 0f, 16f), 0f, routeG2);

            var navGo = new GameObject("NavMesh");
            navGo.transform.position = new Vector3(0f, 5f, 19f);
            var baker = navGo.AddComponent<RuntimeNavMeshBaker>();
            A.SetVector3(baker, "size", new Vector3(40f, 16f, 54f));
            A.SetObjectArray(baker, "ignoreRoots", new Object[] { player.transform, g1.transform, g2.transform });

            // ---------- 11. Zonas ----------
            var zones = new GameObject("Zonas").transform;
            foreach (var z in new[]
            {
                new Bx(new Vector3(-4.5f, 1.5f, 11.7f), new Vector3(3f, 3f, 1.2f)),  new Bx(new Vector3(4.5f, 1.5f, 11.7f), new Vector3(3f, 3f, 1.2f)),
                new Bx(new Vector3(-4.5f, 1.5f, 18.7f), new Vector3(3f, 3f, 1.2f)),  new Bx(new Vector3(4.5f, 1.5f, 18.7f), new Vector3(3f, 3f, 1.2f)),
                new Bx(new Vector3(-16.8f, 1.5f, 25f), new Vector3(2.4f, 3f, 3f)),   new Bx(new Vector3(-17f, 1.5f, 37.5f), new Vector3(2f, 3f, 3f)),
                new Bx(new Vector3(-7f, 1.5f, 28f), new Vector3(1.6f, 3f, 3f)),      new Bx(new Vector3(0f, 2f, 34.5f), new Vector3(2.2f, 2f, 1f)),
            })
                A.Zone("ZonaSombra", zones, z.pos, z.size).AddComponent<ShadowZone>();

            Checkpoint("Checkpoint_Sala", zones, new Vector3(0f, 1.5f, 11f), new Vector3(15f, 3f, 1.5f));
            Checkpoint("Checkpoint_AlaEste", zones, new Vector3(13.5f, 1.5f, 12f), new Vector3(9f, 3f, 1.5f));
            Checkpoint("Checkpoint_AlaOeste", zones, new Vector3(-14f, 1.5f, 15.5f), new Vector3(9f, 3f, 1.2f));
            Checkpoint("Checkpoint_Pasarela", zones, new Vector3(0f, CatwalkY + 1f, 38.7f), new Vector3(14f, 2f, 1.8f));

            A.MessageZone("Aviso_CableEste", zones, new Vector3(13.5f, 1.5f, 6.5f), new Vector3(5f, 3f, 1.5f),
                "IA NEXUS-7: Ese cable dañado descarga a intervalos. Las chispas avisan antes de cada descarga... o aíslalo con chatarra en el panel de la pared.");
            A.MessageZone("Aviso_Barrera", zones, new Vector3(-16f, 1.5f, 7.8f), new Vector3(4f, 3f, 2f),
                "IA NEXUS-7: Barrera eléctrica. La terminal cercana puede desactivarla unos segundos a cambio de un circuito de control.");
            A.MessageZone("Aviso_Escotilla", zones, new Vector3(-12.5f, 1.5f, 4.6f), new Vector3(3f, 3f, 1.4f),
                "IA NEXUS-7: Escotilla de mantenimiento. El foso pasa por DEBAJO del muro hasta el ala oeste. Tendrás que ir agachado.");
            A.MessageZone("Aviso_Rampa", zones, new Vector3(9.8f, 1.5f, 13f), new Vector3(3f, 3f, 1.2f),
                "IA NEXUS-7: Rampa a las pasarelas superiores. Agachado, las barandillas te ocultan de los drones de abajo.");
            A.MessageZone("Aviso_Arco", zones, new Vector3(0f, 1.5f, 17.5f), new Vector3(6f, 3f, 1f),
                "IA NEXUS-7: Con energía en la red, el arco entre transformadores está vivo. Rodéalo por los laterales.");
            A.MessageZone("Aviso_Salto", zones, new Vector3(3.5f, CatwalkY + 1f, 38.7f), new Vector3(3f, 2f, 1.8f),
                "IA NEXUS-7: Hueco en la barandilla: puedes dejarte caer a la plataforma del reactor. Vigila la cámara.");

            var exit = A.Zone("Salida_Montacargas", zones, new Vector3(13.5f, 1.5f, 42.4f), new Vector3(4f, 3f, 3f));
            var exitMsg = exit.AddComponent<TutorialTrigger>();
            B.SetStrings(exitMsg, "messages", new[] { "IA NEXUS-7: Reactor estable. Montacargas hacia el puente de mando en marcha..." });
            var exitTrig = exit.AddComponent<SceneExitTrigger>();
            B.SetString(exitTrig, "sceneName", "Laboratorio");       // Fase 4
            B.SetString(exitTrig, "fallbackMessage", "— FIN DE LA FASE 3 —");

            // ---------- 12. HUD ----------
            var hud = B.BuildHUD();
            A.BuildPhase2UI(hud.transform, inventory, stealth);
            BuildPhase3UI(hud.transform, health);

            var systems = new GameObject("GameSystems");
            var intro = systems.AddComponent<LevelIntro>();
            B.SetRef(intro, "player", player);
            B.SetRef(intro, "hud", hud);
            B.SetStrings(intro, "messages", new[]
            {
                "IA NEXUS-7: Sala de Energía del Módulo C. La nave funciona con energía de emergencia.",
                "IA NEXUS-7: Para abrir la compuerta principal hay que restaurar los 4 bloques de energía de la sala.",
                "IA NEXUS-7: Los circuitos dañados hieren o aturden. Puedes aislarlos con chatarra.",
                "IA NEXUS-7: Con un circuito de control puedes hackear barreras y cámaras durante unos segundos."
            });
            B.SetString(intro, "hint", "Empieza conectando una batería en el zócalo junto a la entrada · [Tab] inventario");

            // ---------- 13. Prefabs, guardado y enlace con la Fase 2 ----------
            B.SavePrefab(socketA, "ZocaloBateria");
            B.SavePrefab(barrier.gameObject, "BarreraElectrica");
            B.SavePrefab(cam.gameObject, "CamaraSeguridad");
            B.SavePrefab(hz1.gameObject, "CableDanado");
            B.SavePrefab(player.gameObject, "Player_Fase3");
            B.SavePrefab(hud.gameObject, "HUD_Fase3");

            EditorSceneManager.SaveScene(scene, ScenePath);
            var scenes = EditorBuildSettings.scenes.ToList();
            if (!scenes.Any(s => s.path == ScenePath)) scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            ConnectPhase2();

            Selection.activeGameObject = player.gameObject;
            Debug.Log("<color=cyan>[Nexus-7]</color> Sala de Energía construida en " + ScenePath + ". Pulsa Play.");
        }

        /// <summary>
        /// Enlaza la salida del Almacén con esta escena y asegura la batería extra del Almacén
        /// (para llegar con una batería que conectar en el zócalo A).
        /// </summary>
        [MenuItem("Nexus-7/Fase 3/Conectar salida de la Fase 2")]
        public static void ConnectPhase2()
        {
            if (!System.IO.File.Exists(Phase2ScenePath))
            {
                Debug.Log("[Nexus-7] No existe AlmacenCarga.unity: ejecuta 'Fase 2 > Construir Almacén de Carga' si quieres enlazarlas.");
                return;
            }
            string current = EditorSceneManager.GetActiveScene().path;
            var phase2 = EditorSceneManager.OpenScene(Phase2ScenePath, OpenSceneMode.Single);

            var exit = GameObject.Find("Salida_Montacargas");
            if (exit != null)
            {
                var trig = exit.GetComponent<SceneExitTrigger>();
                if (trig != null) B.SetString(trig, "sceneName", SceneName);
            }

            var center = GameObject.Find("Contenedor_Centro");
            var battery = A.EnsureItems().Battery;
            if (center != null)
            {
                var loot = center.GetComponent<LootContainer>();
                if (loot != null)
                {
                    var so = new SerializedObject(loot);
                    var contents = so.FindProperty("contents");
                    bool hasBattery = false;
                    for (int i = 0; i < contents.arraySize; i++)
                        if (contents.GetArrayElementAtIndex(i).FindPropertyRelative("item").objectReferenceValue == battery) hasBattery = true;
                    if (!hasBattery)
                    {
                        contents.arraySize++;
                        var e = contents.GetArrayElementAtIndex(contents.arraySize - 1);
                        e.FindPropertyRelative("item").objectReferenceValue = battery;
                        e.FindPropertyRelative("amount").intValue = 1;
                        so.ApplyModifiedPropertiesWithoutUndo();
                    }
                }
            }
            EditorSceneManager.SaveScene(phase2);
            Debug.Log("[Nexus-7] Almacén de Carga enlazado con " + SceneName + " (y con batería extra en el contenedor central).");

            if (!string.IsNullOrEmpty(current) && current != Phase2ScenePath)
                EditorSceneManager.OpenScene(current, OpenSceneMode.Single);
        }

        // =====================================================================
        //  ESTRUCTURA
        // =====================================================================

        internal struct Bx
        {
            public Vector3 pos, size;
            public Bx(Vector3 pos, Vector3 size) { this.pos = pos; this.size = size; }
        }

        internal static void BuildShell(Transform env)
        {
            // Suelo con dos huecos: bocas del foso de mantenimiento.
            FloorWithHoles(env, -18.3f, 18.3f, -T, 40f + T, new[]
            {
                new Rect(-13.2f, 6f, 1.4f, 1.8f),    // boca sur (escotilla)
                new Rect(-13.2f, 12f, 1.4f, 2.5f),   // boca norte (ala oeste)
            });
            A.Tiled("Techo", env, new Vector3(0f, H + 0.1f, 20f), new Vector3(36.6f, 0.2f, 40.6f), A.metalMat);
            A.Tiled("MuroOeste", env, new Vector3(-18f - T / 2, H / 2, 20f), new Vector3(T, H, 40.6f), A.wallMat);
            A.Tiled("MuroEste", env, new Vector3(18f + T / 2, H / 2, 20f), new Vector3(T, H, 40.6f), A.wallMat);

            // Muro sur con entrada de 3 m
            A.Tiled("MuroSur_Izq", env, new Vector3(-9.9f, H / 2, -T / 2), new Vector3(16.8f, H, T), A.wallMat);
            A.Tiled("MuroSur_Der", env, new Vector3(9.9f, H / 2, -T / 2), new Vector3(16.8f, H, T), A.wallMat);
            B.Box("MuroSur_Dintel", env, new Vector3(0f, 6.5f, -T / 2), new Vector3(3f, 7f, T), A.wallMat);

            // Muro norte con hueco para la compuerta principal (x 12..15)
            A.Tiled("MuroNorte_Izq", env, new Vector3(-3.15f, H / 2, 40f + T / 2), new Vector3(30.3f, H, T), A.wallMat);
            A.Tiled("MuroNorte_Der", env, new Vector3(16.65f, H / 2, 40f + T / 2), new Vector3(3.3f, H, T), A.wallMat);
            B.Box("MuroNorte_Dintel", env, new Vector3(13.5f, 6.75f, 40f + T / 2), new Vector3(3f, 6.5f, T), A.wallMat);

            // Vestíbulo de llegada (montacargas desde el Almacén)
            B.Box("Vest_Suelo", env, new Vector3(0f, -0.1f, -2.5f), new Vector3(5.4f, 0.2f, 5f), A.floorMat);
            B.Box("Vest_Techo", env, new Vector3(0f, 4.1f, -2.5f), new Vector3(5.4f, 0.2f, 5f), A.metalMat);
            B.Box("Vest_MuroO", env, new Vector3(-2.6f, 2f, -2.5f), new Vector3(0.2f, 4f, 5f), A.wallMat);
            B.Box("Vest_MuroE", env, new Vector3(2.6f, 2f, -2.5f), new Vector3(0.2f, 4f, 5f), A.wallMat);
            B.Box("Vest_MuroS", env, new Vector3(0f, 2f, -5.1f), new Vector3(5.4f, 4f, 0.2f), A.wallMat);

            // Montacargas de salida (tras la compuerta principal)
            var lift = new GameObject("MontacargasSalida").transform;
            lift.SetParent(env);
            B.Box("Suelo", lift, new Vector3(13.5f, -0.1f, 42.4f), new Vector3(5.4f, 0.2f, 4.4f), A.floorMat);
            B.Box("Techo", lift, new Vector3(13.5f, 6.1f, 42.4f), new Vector3(5.4f, 0.2f, 4.4f), A.metalMat);
            B.Box("MuroO", lift, new Vector3(10.9f, 3f, 42.4f), new Vector3(0.2f, 6f, 4.4f), A.wallMat);
            B.Box("MuroE", lift, new Vector3(16.1f, 3f, 42.4f), new Vector3(0.2f, 6f, 4.4f), A.wallMat);
            B.Box("MuroN", lift, new Vector3(13.5f, 3f, 44.7f), new Vector3(5.4f, 6f, 0.2f), A.wallMat);

            // Vigas y conductos del techo
            for (float z = 4f; z < 40f; z += 6f)
                B.NoCollider(B.Box("Viga", env, new Vector3(0f, H - 0.5f, z), new Vector3(36f, 0.6f, 0.35f), A.metalMat));
            foreach (float x in new[] { -17.6f, 17.6f })
                B.NoCollider(B.Box("Conducto", env, new Vector3(x, 7f, 20f), new Vector3(0.5f, 0.5f, 40f), cableMat));
        }

        internal static void BuildInteriorWalls(Transform env)
        {
            var root = new GameObject("MurosInteriores").transform;
            root.SetParent(env);
            const float t = 0.4f;
            // Muro z=10 oeste: hueco de barrera (x -17.5..-14.5); el foso pasa por debajo
            B.Box("Z10_Oeste_A", root, new Vector3(-17.75f, WallH / 2, 10f), new Vector3(0.5f, WallH, t), panelMat);
            A.Tiled("Z10_Oeste_B", root, new Vector3(-11.35f, WallH / 2, 10f), new Vector3(6.3f, WallH, t), panelMat);
            B.Box("Z10_Oeste_Dintel", root, new Vector3(-16f, 3.5f, 10f), new Vector3(3f, 1f, t), panelMat);
            // Muro z=10 este: hueco para la puerta S1 (x 12..15)
            B.Box("Z10_Este_A", root, new Vector3(10.1f, WallH / 2, 10f), new Vector3(3.8f, WallH, t), panelMat);
            B.Box("Z10_Este_B", root, new Vector3(16.5f, WallH / 2, 10f), new Vector3(3f, WallH, t), panelMat);
            B.Box("Z10_Este_Dintel", root, new Vector3(13.5f, 3.5f, 10f), new Vector3(3f, 1f, t), panelMat);
            // Tabiques que separan las alas de la sala central (las pasarelas van encima)
            A.Tiled("Tabique_Oeste", root, new Vector3(-8f, WallH / 2, 25f), new Vector3(t, WallH, 30f), panelMat);
            A.Tiled("Tabique_Este", root, new Vector3(8f, WallH / 2, 25f), new Vector3(t, WallH, 30f), panelMat);
            // Franjas de alto voltaje en los tabiques
            foreach (float x in new[] { -7.79f, 7.79f, -8.21f, 8.21f })
                B.NoCollider(B.Box("FranjaAltoVoltaje", root, new Vector3(x, 1f, 25f), new Vector3(0.02f, 0.25f, 30f), A.warningMat));
        }

        internal static void BuildCatwalks(Transform env)
        {
            var root = new GameObject("Pasarelas").transform;
            root.SetParent(env);
            float y = CatwalkY - 0.1f;
            B.Box("Pasarela_Oeste", root, new Vector3(-8f, y, 25f), new Vector3(1.8f, 0.2f, 29.6f), A.metalMat);
            B.Box("Pasarela_Este", root, new Vector3(8f, y, 25f), new Vector3(1.8f, 0.2f, 29.6f), A.metalMat);
            B.Box("Pasarela_Norte", root, new Vector3(0f, y, 38.7f), new Vector3(14.2f, 0.2f, 1.8f), A.metalMat);

            // Barandillas macizas de 1 m: agachado (1.0 m) quedas oculto; de pie se te ve.
            RailZ(root, -8.85f, 10.2f, 21f); RailZ(root, -8.85f, 23f, 28f); RailZ(root, -8.85f, 29.6f, 39.8f); // huecos: rampa y caída
            RailZ(root, -7.15f, 10.2f, 37.8f);
            RailZ(root, 8.85f, 10.2f, 21f); RailZ(root, 8.85f, 23f, 39.8f);
            RailZ(root, 7.15f, 10.2f, 37.8f);
            RailX(root, 37.85f, -7.1f, 2.5f); RailX(root, 37.85f, 4.5f, 7.1f);   // hueco: caída a la plataforma del reactor
            RailX(root, 39.75f, -8.9f, 8.9f);
            RailX(root, 10.25f, -8.9f, -7.1f); RailX(root, 10.25f, 7.1f, 8.9f);

            // Rampas (sólidas por debajo para que no haya huecos donde quedarse atascado) + rellanos
            foreach (float s in new[] { -1f, 1f })
            {
                Ramp("Rampa", root, 9.8f * s, 1.6f, 14f, 0f, 21f, CatwalkY, 3f, A.metalMat);
                B.Box("Rellano", root, new Vector3(9.75f * s, CatwalkY / 2f, 22f), new Vector3(1.7f, CatwalkY, 2f), panelMat);
                RailZ(root, 10.55f * s, 21f, 23f);
                RailX(root, 22.95f, s < 0 ? -10.6f : 8.9f, s < 0 ? -8.9f : 10.6f);
            }

            // Cajas apiladas bajo el hueco de la pasarela oeste: desnivel para bajar al ala oeste
            B.Box("CajasCaida", root, new Vector3(-9.85f, 1f, 28.8f), new Vector3(1.9f, 2f, 1.9f), A.crateGreen);
        }

        internal static void RailZ(Transform p, float x, float z0, float z1) =>
            B.Box("Barandilla", p, new Vector3(x, CatwalkY + 0.5f, (z0 + z1) / 2f), new Vector3(0.1f, 1f, z1 - z0), A.metalMat);

        internal static void RailX(Transform p, float z, float x0, float x1) =>
            B.Box("Barandilla", p, new Vector3((x0 + x1) / 2f, CatwalkY + 0.5f, z), new Vector3(x1 - x0, 1f, 0.1f), A.metalMat);

        internal static void BuildTrench(Transform env, Transform gameplay, ItemData wrench)
        {
            // Foso de 1.4 m de profundidad (x -13.2..-11.8, z 6..14.5). Bajo el suelo hay 1.2 m libres → solo agachado.
            var root = new GameObject("FosoMantenimiento_RutaAlternativa").transform;
            root.SetParent(env);
            B.Box("Fondo", root, new Vector3(-12.5f, -1.5f, 10.25f), new Vector3(1.4f, 0.2f, 8.5f), A.metalMat);
            B.Box("ParedO", root, new Vector3(-13.3f, -0.7f, 10.25f), new Vector3(0.2f, 1.4f, 8.5f), A.metalMat);
            B.Box("ParedE", root, new Vector3(-11.7f, -0.7f, 10.25f), new Vector3(0.2f, 1.4f, 8.5f), A.metalMat);
            B.Box("ParedS", root, new Vector3(-12.5f, -0.7f, 5.95f), new Vector3(1.8f, 1.4f, 0.1f), A.metalMat);
            B.Box("ParedN", root, new Vector3(-12.5f, -0.7f, 14.55f), new Vector3(1.8f, 1.4f, 0.1f), A.metalMat);
            Ramp("RampaSur", root, -12.5f, 1.4f, 6f, 0f, 7.8f, -1.4f, 0.3f, A.metalMat);
            Ramp("RampaNorte", root, -12.5f, 1.4f, 12.2f, -1.4f, 14.5f, 0f, 0.3f, A.metalMat);
            A.PointLight("LuzFoso", root, new Vector3(-12.5f, -0.6f, 10f), new Color(1f, 0.5f, 0.2f), 4f, 0.6f, false);

            // Escotilla atornillada sobre la boca sur: necesita la llave hidráulica del Almacén (no se gasta)
            var hatch = A.SlidingDoor("EscotillaMantenimiento", gameplay, new Vector3(-12.5f, 0f, 6.9f), new Vector3(1.4f, 0.08f, 1.8f),
                                      new Vector3(1.6f, 0f, 0f), A.metalMat, new Vector3(1.0f, 0.15f, -1.1f));
            B.SetString(hatch, "lockedPrompt", "Escotilla atornillada");
            B.SetString(hatch, "lockedMessage", "La escotilla de mantenimiento está atornillada. Necesitas una herramienta.");
            B.SetString(hatch, "unlockedWithItemsMessage", "Escotilla abierta. Baja al foso y avanza agachado [Ctrl] bajo el muro.");
            A.SetStacks(hatch, "requiredItems", new[] { new ItemStack(wrench, 1) });
            A.SetBool(hatch, "consumeItems", false);
            A.SetFloat(hatch, "doorNoiseRadius", 3f);
        }

        internal static (PowerConsole, DoorController) BuildReactor(Transform env, Transform gameplay, ItemData circuit)
        {
            var root = new GameObject("PlataformaReactor").transform;
            root.SetParent(env);
            B.Box("Plataforma", root, new Vector3(0f, 0.5f, 34.85f), new Vector3(10f, 1f, 9.7f), panelMat);
            Ramp("RampaReactor", root, 0f, 3f, 26.5f, 0f, 30f, 1f, 1.5f, A.metalMat);
            B.Box("Frente_Izq", root, new Vector3(-3.25f, 2.3f, 30.2f), new Vector3(3.5f, 2.6f, 0.3f), panelMat);
            B.Box("Frente_Der", root, new Vector3(3.25f, 2.3f, 30.2f), new Vector3(3.5f, 2.6f, 0.3f), panelMat);
            B.NoCollider(B.Box("FranjaFrente", root, new Vector3(0f, 3.55f, 30.03f), new Vector3(10f, 0.12f, 0.02f), A.warningMat));

            var door = A.SlidingDoor("AccesoReactor", gameplay, new Vector3(0f, 1f, 30.2f), new Vector3(3f, 2.6f, 0.2f),
                                     new Vector3(3.2f, 0f, 0f), A.metalMat, new Vector3(-2.05f, 2.2f, -0.25f));

            // Núcleo del reactor: su color sigue al estado de la red (PoweredLight con emisivo)
            var core = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            core.name = "NucleoReactor";
            core.transform.SetParent(root);
            core.transform.localPosition = new Vector3(0f, 2.75f, 36.3f);
            core.transform.localScale = new Vector3(2.2f, 1.75f, 2.2f);
            core.GetComponent<Renderer>().sharedMaterial = coreMat;
            var coreLight = A.PointLight("LuzNucleo", root, new Vector3(0f, 3f, 34.6f), new Color(1f, 0.1f, 0.05f), 10f, 1f, false);
            var pl = coreLight.gameObject.AddComponent<PoweredLight>();
            B.SetRef(pl, "emissiveRenderer", core.GetComponent<Renderer>());
            A.SetFloat(pl, "emissionStrength", 4f);

            // Consola dañada: necesita 3 bloques en la red + 1 circuito de control
            var body = B.Box("ConsolaReactor", gameplay, new Vector3(0f, 1.6f, 33.6f), new Vector3(1.8f, 1.2f, 0.7f), A.metalMat);
            var screen = B.NoCollider(B.Box("Pantalla", body.transform.parent, new Vector3(0f, 1.95f, 33.24f), new Vector3(1.4f, 0.5f, 0.05f), screenMat));
            var (ind, indLight) = Indicator(gameplay, new Vector3(0.75f, 2.35f, 33.24f));
            var console = body.AddComponent<PowerConsole>();
            B.SetString(console, "actionLabel", "Reparar y arrancar el reactor");
            A.SetInt(console, "requiredGridPower", 3);
            A.SetStacks(console, "requiredItems", new[] { new ItemStack(circuit, 1) });
            B.SetString(console, "objectiveLabel", "Consola del reactor (plataforma norte)");
            B.SetString(console, "activatedMessage", "IA NEXUS-7: Circuito instalado. Secuencia de arranque del reactor... ¡completada!");
            A.SetFloat(console, "activationNoiseRadius", 12f);   // el reactor al arrancar se oye en toda la sala
            B.SetRef(console, "glowRenderer", screen.GetComponent<Renderer>());
            B.SetRef(console, "statusRenderer", ind);
            B.SetRef(console, "statusLight", indLight);
            return (console, door);
        }

        internal static void BuildTransformers(Transform env)
        {
            var root = new GameObject("Transformadores").transform;
            root.SetParent(env);
            foreach (var p in new[] { new Vector3(-4.5f, 0f, 13.5f), new Vector3(4.5f, 0f, 13.5f), new Vector3(-4.5f, 0f, 20.5f), new Vector3(4.5f, 0f, 20.5f) })
            {
                B.Box("Transformador", root, p + new Vector3(0f, 2f, 0f), new Vector3(2.5f, 4f, 2.5f), transformerMat);
                B.NoCollider(B.Box("Franja", root, p + new Vector3(0f, 1.2f, 0f), new Vector3(2.52f, 0.3f, 2.52f), A.warningMat));
                for (int i = -1; i <= 1; i++)
                {
                    var ins = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    ins.name = "Aislador";
                    Object.DestroyImmediate(ins.GetComponent<Collider>());
                    ins.transform.SetParent(root);
                    ins.transform.localPosition = p + new Vector3(i * 0.7f, 4.35f, 0f);
                    ins.transform.localScale = new Vector3(0.25f, 0.35f, 0.25f);
                    ins.GetComponent<Renderer>().sharedMaterial = cableMat;
                }
                Sparks("ChispasTransformador", root, p + new Vector3(0f, 4.6f, 0f));
            }
        }

        internal static void BuildLights(Transform env)
        {
            var root = new GameObject("IluminacionDinamica").transform;
            root.SetParent(env);
            var spots = new[]
            {
                new Vector3(-12f, 8f, 5f), new Vector3(0f, 8f, 5f), new Vector3(12f, 8f, 5f),
                new Vector3(-4f, 8f, 15f), new Vector3(4f, 8f, 15f), new Vector3(-4f, 8f, 26f), new Vector3(4f, 8f, 26f),
                new Vector3(-13f, 8f, 18f), new Vector3(-13f, 8f, 33f), new Vector3(13f, 8f, 18f), new Vector3(13f, 8f, 33f),
            };
            for (int i = 0; i < spots.Length; i++)
            {
                var fixture = B.NoCollider(B.Box("Lampara", root, spots[i] + Vector3.up * 0.3f, new Vector3(1.4f, 0.12f, 0.4f), A.lampMat));
                B.NoCollider(B.Box("Cable", root, spots[i] + Vector3.up * 1.2f, new Vector3(0.04f, 1.6f, 0.04f), cableMat));
                var l = A.PointLight("Luz", root, spots[i], Color.red, 13f, 1f, i == 1 || i == 3 || i == 6);
                var pl = l.gameObject.AddComponent<PoweredLight>();
                B.SetRef(pl, "emissiveRenderer", fixture.GetComponent<Renderer>());
            }
            foreach (var p in new[] { new Vector3(0f, 3.7f, -2.5f), new Vector3(13.5f, 5.5f, 42.4f) })
            {
                var l = A.PointLight("LuzAuxiliar", root, p, Color.red, 6f, 0.6f, false);
                l.gameObject.AddComponent<PoweredLight>();
            }
        }

        internal static void ConfigureLighting()
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.06f, 0.012f, 0.01f); // PowerGrid lo anima en juego
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.03f, 0.005f, 0.005f);
            RenderSettings.fogDensity = 0.03f;
        }

        // =====================================================================
        //  ENERGÍA
        // =====================================================================

        internal static GameObject Socket(string name, Transform parent, Vector3 pos, float rotY, ItemData battery, string label)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent);
            root.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, rotY, 0f));
            var housing = B.Box("Carcasa", root.transform, Vector3.zero, new Vector3(0.7f, 1.0f, 0.35f), panelMat);
            housing.transform.localPosition = Vector3.zero;
            var visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visual.name = "BateriaConectada";
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = new Vector3(0f, -0.05f, 0.2f);
            visual.transform.localScale = new Vector3(0.18f, 0.14f, 0.18f);
            visual.GetComponent<Renderer>().sharedMaterial = A.batteryMat;
            var (ind, light) = Indicator(root.transform, Vector3.zero);
            ind.transform.localPosition = new Vector3(0f, 0.6f, 0.19f);
            light.transform.localPosition = new Vector3(0f, 0.6f, 0.6f);

            var socket = housing.AddComponent<BatterySocket>();
            B.SetRef(socket, "batteryItem", battery);
            B.SetRef(socket, "batteryVisual", visual);
            B.SetString(socket, "objectiveLabel", label);
            B.SetRef(socket, "statusRenderer", ind);
            B.SetRef(socket, "statusLight", light);
            return root;
        }

        internal static void Breaker(Transform parent, Vector3 pos)
        {
            var body = B.Box("InterruptorGeneral", parent, pos, new Vector3(0.35f, 1.4f, 0.9f), panelMat);
            var lever = B.NoCollider(B.Box("Palanca", parent, pos + new Vector3(-0.3f, 0.2f, 0f), new Vector3(0.25f, 0.6f, 0.08f), A.warningMat));
            lever.transform.rotation = Quaternion.Euler(0f, 0f, -25f);
            var (ind, light) = Indicator(parent, pos + new Vector3(-0.19f, 0.55f, 0.3f));
            var console = body.AddComponent<PowerConsole>();
            B.SetString(console, "actionLabel", "Accionar el interruptor general");
            B.SetString(console, "objectiveLabel", "Interruptor general (ala este)");
            B.SetString(console, "activatedMessage", "Interruptor general accionado. Las líneas del ala este vuelven a tener tensión.");
            B.SetRef(console, "statusRenderer", ind);
            B.SetRef(console, "statusLight", light);
        }

        internal static void PoweredDoor(DoorController door, int requiredPower, string lockedPrompt, string lockedMessage)
        {
            B.SetString(door, "lockedPrompt", lockedPrompt);
            B.SetString(door, "lockedMessage", lockedMessage);
            var consumer = door.gameObject.AddComponent<PowerConsumer>();
            A.SetInt(consumer, "requiredPower", requiredPower);
            A.SetObjectArray(consumer, "doors", new Object[] { door });
        }

        internal static (Renderer, Light) Indicator(Transform parent, Vector3 pos)
        {
            var cube = B.NoCollider(B.Box("Indicador", parent, pos, new Vector3(0.14f, 0.14f, 0.05f), A.statusMat));
            var l = A.PointLight("LuzIndicador", parent, pos + new Vector3(0f, 0f, -0.3f), Color.red, 2f, 1f, false);
            return (cube.GetComponent<Renderer>(), l);
        }

        // =====================================================================
        //  PELIGROS Y SEGURIDAD
        // =====================================================================

        internal static ElectricHazard Hazard(string name, Transform parent, Vector3 center, Vector3 size,
            ElectricHazard.HazardEffect effect, ElectricHazard.HazardPattern pattern, float damage, float hitInterval,
            float onTime, float offTime, float offset, int requiredPower, bool floorCable)
        {
            var root = new GameObject(name) { layer = IgnoreRaycastLayer };
            root.transform.SetParent(parent);
            root.transform.localPosition = center;
            var box = root.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = size;

            float half = size.x / 2f - 0.1f;
            var arcs = new List<ElectricArc>();
            if (floorCable)
            {
                B.NoCollider(B.Box("CablePelado", root.transform, new Vector3(0f, -size.y / 2f + 0.03f, 0f), new Vector3(size.x, 0.06f, 0.12f), cableMat))
                    .transform.localPosition = new Vector3(0f, -size.y / 2f + 0.03f, 0f);
                arcs.Add(Arc("Arco", root.transform, new Vector3(-half, -size.y / 2f + 0.15f, 0f), new Vector3(2f * half, 0f, 0f), 0.25f, 0.05f));
                arcs.Add(Arc("Arco", root.transform, new Vector3(-half, -size.y / 2f + 0.9f, 0f), new Vector3(2f * half, 0f, 0f), 0.45f, 0.04f));
            }
            else
            {
                arcs.Add(Arc("Arco", root.transform, new Vector3(-half, 0.4f, 0f), new Vector3(2f * half, 0f, 0f), 0.35f, 0.07f));
                arcs.Add(Arc("Arco", root.transform, new Vector3(-half, 0.4f, 0f), new Vector3(2f * half, 0f, 0f), 0.5f, 0.03f));
            }
            var sparks = Sparks("Chispas", root.transform, new Vector3(0f, -size.y / 2f + 0.2f, 0f));
            A.SetVector2(sparks, "interval", Vector2.zero); // solo ráfagas controladas por la trampa
            var light = A.PointLight("LuzArco", root.transform, new Vector3(0f, 0f, 0f), ArcBlue, 5f, 0f, false);

            var hz = root.AddComponent<ElectricHazard>();
            B.SetEnum(hz, "effect", (int)effect);
            B.SetEnum(hz, "pattern", (int)pattern);
            A.SetFloat(hz, "damage", damage);
            A.SetFloat(hz, "hitInterval", hitInterval);
            if (pattern == ElectricHazard.HazardPattern.Intermittent)
            {
                A.SetFloat(hz, "onTime", onTime);
                A.SetFloat(hz, "offTime", offTime);
                A.SetFloat(hz, "timeOffset", offset);
            }
            A.SetInt(hz, "requiredGridPower", requiredPower);
            A.SetObjectArray(hz, "arcs", arcs.Cast<Object>().ToArray());
            B.SetRef(hz, "sparks", sparks);
            B.SetRef(hz, "arcLight", light);
            return hz;
        }

        internal static void RepairPanelAt(string name, Transform parent, Vector3 pos, Vector3 size, ElectricHazard target, ItemData scrap)
        {
            var body = B.Box(name, parent, pos, size, panelMat);
            // La pantalla sobresale por las dos caras del eje fino del panel (escala local relativa al panel).
            var screenScale = size.x < size.z ? new Vector3(1.3f, 0.55f, 0.7f) : new Vector3(0.7f, 0.55f, 1.3f);
            var screen = B.NoCollider(B.Box("Pantalla", body.transform, Vector3.zero, screenScale, A.warningMat));
            var rp = body.AddComponent<RepairPanel>();
            B.SetRef(rp, "target", target);
            A.SetStacks(rp, "cost", new[] { new ItemStack(scrap, 2) });
            B.SetRef(rp, "screenRenderer", screen.GetComponent<Renderer>());
        }

        internal static ElectricBarrier Barrier(Transform parent, Vector3 center)
        {
            var root = new GameObject("BarreraElectrica") { layer = IgnoreRaycastLayer };
            root.transform.SetParent(parent);
            root.transform.localPosition = center;
            var zone = root.AddComponent<BoxCollider>();
            zone.isTrigger = true;
            zone.size = new Vector3(3f, 3f, 1f);

            var blocker = new GameObject("Bloqueo");
            blocker.transform.SetParent(root.transform, false);
            var solid = blocker.AddComponent<BoxCollider>();
            solid.size = new Vector3(3f, 3f, 0.2f);

            B.Box("PosteIzq", root.transform, Vector3.zero, new Vector3(0.15f, 3f, 0.35f), A.warningMat).transform.localPosition = new Vector3(-1.55f, 0f, 0f);
            B.Box("PosteDer", root.transform, Vector3.zero, new Vector3(0.15f, 3f, 0.35f), A.warningMat).transform.localPosition = new Vector3(1.55f, 0f, 0f);
            var beams = new List<ElectricArc>();
            foreach (float y in new[] { -1.1f, -0.55f, 0f, 0.55f, 1.1f })
                beams.Add(Arc("Haz", root.transform, new Vector3(-1.45f, y, 0f), new Vector3(2.9f, 0f, 0f), 0.03f, 0.03f));
            var light = A.PointLight("LuzBarrera", root.transform, new Vector3(0f, 0.5f, -0.5f), ArcBlue, 6f, 2.5f, false);
            var hum = root.AddComponent<AudioSource>();
            hum.loop = true;
            hum.playOnAwake = false;
            hum.spatialBlend = 1f;

            var barrier = root.AddComponent<ElectricBarrier>();
            B.SetRef(barrier, "blockingCollider", solid);
            A.SetObjectArray(barrier, "beams", beams.Cast<Object>().ToArray());
            B.SetRef(barrier, "barrierLight", light);
            B.SetRef(barrier, "humSource", hum);
            return barrier;
        }

        internal static SecurityCamera SecurityCam(Transform parent, Vector3 basePos, float yaw)
        {
            B.Box("PosteCamara", parent, basePos + new Vector3(0f, 1.4f, 0f), new Vector3(0.15f, 2.8f, 0.15f), A.metalMat);
            var root = new GameObject("CamaraSeguridad");
            root.transform.SetParent(parent);
            root.transform.localPosition = basePos + new Vector3(0f, 2.9f, 0f);

            var pivot = new GameObject("Pivote").transform;
            pivot.SetParent(root.transform, false);
            pivot.localRotation = Quaternion.Euler(25f, yaw, 0f);   // inclinada hacia abajo
            B.NoCollider(B.Box("Cuerpo", pivot, Vector3.zero, new Vector3(0.3f, 0.25f, 0.5f), cameraMat)).transform.localPosition = Vector3.zero;
            var led = B.NoCollider(B.Box("Led", pivot, Vector3.zero, new Vector3(0.06f, 0.06f, 0.02f), A.statusMat));
            led.transform.localPosition = new Vector3(0.1f, 0.08f, 0.26f);
            var lens = new GameObject("Lente").transform;
            lens.SetParent(pivot, false);
            lens.localPosition = new Vector3(0f, 0f, 0.3f);
            var spotGo = new GameObject("Foco");
            spotGo.transform.SetParent(lens, false);
            var spot = spotGo.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.spotAngle = 50f;
            spot.range = 13f;
            spot.intensity = 4f;
            spot.shadows = LightShadows.Soft;

            var vision = root.AddComponent<EnemyVision>();
            B.SetRef(vision, "eye", lens);
            B.SetRef(vision, "visionLight", spot);
            A.SetFloat(vision, "viewDistance", 13f);
            A.SetFloat(vision, "viewAngle", 50f);
            A.SetFloat(vision, "detectionSpeed", 1.3f);
            A.SetFloat(vision, "proximityRadius", 0.5f);

            var audio = root.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.spatialBlend = 1f;
            var cam = root.AddComponent<SecurityCamera>();
            B.SetRef(cam, "pivot", pivot);
            B.SetRef(cam, "vision", vision);
            B.SetRef(cam, "visionLight", spot);
            B.SetRef(cam, "statusRenderer", led.GetComponent<Renderer>());
            B.SetRef(cam, "audioSource", audio);
            return cam;
        }

        internal static void Terminal(string name, Transform parent, Vector3 floorPos, float rotY, MonoBehaviour target, ItemData circuit, float duration)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent);
            root.transform.SetPositionAndRotation(floorPos, Quaternion.Euler(0f, rotY, 0f));
            var body = B.Box("Cuerpo", root.transform, Vector3.zero, new Vector3(0.6f, 1.2f, 0.4f), panelMat);
            body.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            var screen = B.NoCollider(B.Box("Pantalla", root.transform, Vector3.zero, new Vector3(0.5f, 0.35f, 0.03f), screenMat));
            screen.transform.localPosition = new Vector3(0f, 1.0f, -0.21f);
            screen.transform.localRotation = Quaternion.Euler(-15f, 0f, 0f);

            var hack = root.AddComponent<HackTerminal>();
            A.SetObjectArray(hack, "targets", new Object[] { target });
            A.SetStacks(hack, "cost", new[] { new ItemStack(circuit, 1) });
            A.SetFloat(hack, "disableDuration", duration);
            B.SetRef(hack, "screenRenderer", screen.GetComponent<Renderer>());
        }

        internal static void Checkpoint(string name, Transform parent, Vector3 pos, Vector3 size) =>
            A.Zone(name, parent, pos, size).AddComponent<Gameplay.Checkpoint>();

        // =====================================================================
        //  EFECTOS
        // =====================================================================

        internal static SparkEmitter Sparks(string name, Transform parent, Vector3 localPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.startLifetime = 0.45f;
            main.startSpeed = 4f;
            main.startSize = 0.05f;
            main.gravityModifier = 1.2f;
            main.startColor = new Color(1f, 0.85f, 0.45f);
            main.maxParticles = 300;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.05f;
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = sparkMat;

            var flash = A.PointLight("Destello", go.transform, Vector3.zero, ArcBlue, 4f, 0f, false);
            var se = go.AddComponent<SparkEmitter>();
            B.SetRef(se, "particles", ps);
            B.SetRef(se, "flashLight", flash);
            return se;
        }

        internal static ElectricArc Arc(string name, Transform parent, Vector3 localPos, Vector3 end, float jitter, float width)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var lr = go.AddComponent<LineRenderer>();
            lr.sharedMaterial = arcMat;
            lr.useWorldSpace = false;
            lr.startColor = lr.endColor = ArcBlue;
            lr.widthMultiplier = width;
            lr.shadowCastingMode = ShadowCastingMode.Off;
            var arc = go.AddComponent<ElectricArc>();
            A.SetVector3(arc, "endPoint", end);
            A.SetFloat(arc, "jitter", jitter);
            A.SetFloat(arc, "width", width);
            return arc;
        }

        /// <summary>Rampa sólida entre (z0,y0) y (z1,y1). 'thick' grande = sin huecos debajo.</summary>
        internal static void Ramp(string name, Transform parent, float x, float width, float z0, float y0, float z1, float y1, float thick, Material mat)
        {
            if (z1 < z0) { float tz = z0; z0 = z1; z1 = tz; float ty = y0; y0 = y1; y1 = ty; }
            float dz = z1 - z0, dy = y1 - y0;
            float len = Mathf.Sqrt(dz * dz + dy * dy) + 0.2f;
            float ang = Mathf.Atan2(dy, dz);                                       // + sube hacia +Z
            var normal = new Vector3(0f, Mathf.Cos(ang), -Mathf.Sin(ang));
            var mid = new Vector3(x, (y0 + y1) / 2f, (z0 + z1) / 2f);
            var go = B.Box(name, parent, mid - normal * (thick / 2f), new Vector3(width, thick, len), mat);
            go.transform.rotation = Quaternion.Euler(-ang * Mathf.Rad2Deg, 0f, 0f);
        }

        /// <summary>Suelo en losetas (≤6 m) dejando huecos rectangulares (x, z, ancho, largo).</summary>
        internal static void FloorWithHoles(Transform parent, float x0, float x1, float z0, float z1, Rect[] holes)
        {
            var root = new GameObject("Suelo").transform;
            root.SetParent(parent);
            var xs = Cuts(x0, x1, holes.SelectMany(h => new[] { h.xMin, h.xMax }));
            var zs = Cuts(z0, z1, holes.SelectMany(h => new[] { h.yMin, h.yMax }));
            for (int i = 0; i < xs.Count - 1; i++)
                for (int j = 0; j < zs.Count - 1; j++)
                {
                    float cx = (xs[i] + xs[i + 1]) / 2f, cz = (zs[j] + zs[j + 1]) / 2f;
                    if (holes.Any(h => h.Contains(new Vector2(cx, cz)))) continue;
                    B.Box("Loseta", root, new Vector3(cx, -0.1f, cz), new Vector3(xs[i + 1] - xs[i], 0.2f, zs[j + 1] - zs[j]), A.floorMat);
                }
        }

        internal static List<float> Cuts(float a, float b, IEnumerable<float> extra)
        {
            var set = new SortedSet<float> { a, b };
            for (float v = Mathf.Ceil(a / 6f) * 6f; v < b; v += 6f) set.Add(v);
            foreach (var e in extra) if (e > a && e < b) set.Add(e);
            return set.ToList();
        }

        // =====================================================================
        //  UI DE LA FASE 3
        // =====================================================================

        internal static void BuildPhase3UI(Transform canvas, PlayerHealth health)
        {
            // Destello de daño (pantalla completa)
            var flash = B.UIImage("DestelloDano", canvas, new Color(1f, 0.1f, 0.05f, 0f));
            flash.rectTransform.anchorMin = Vector2.zero;
            flash.rectTransform.anchorMax = Vector2.one;
            flash.rectTransform.offsetMin = flash.rectTransform.offsetMax = Vector2.zero;
            flash.transform.SetSiblingIndex(2); // detrás de los textos, delante de la mira

            // Integridad (abajo a la izquierda)
            var hpLabel = B.UIText("TextoIntegridad", canvas, Vector2.zero, new Vector2(40f, 66f), new Vector2(400f, 34f), 24f, TextAlignmentOptions.Left, Color.white);
            hpLabel.rectTransform.pivot = Vector2.zero;
            var (hpFill, hpBg) = Bar("BarraIntegridad", canvas, Vector2.zero, new Vector2(40f, 50f));
            var stun = B.UIText("TextoAturdido", canvas, new Vector2(0.5f, 0.5f), new Vector2(0f, 70f), new Vector2(500f, 50f), 40f, TextAlignmentOptions.Center, Color.cyan);
            var hpUi = canvas.gameObject.AddComponent<UI.HealthUI>();
            B.SetRef(hpUi, "health", health);
            B.SetRef(hpUi, "barFill", hpFill);
            B.SetRef(hpUi, "label", hpLabel);
            B.SetRef(hpUi, "damageFlash", flash);
            B.SetRef(hpUi, "stunLabel", stun);

            // Energía + sistemas hackeados (arriba a la izquierda, bajo el objetivo)
            var pLabel = B.UIText("TextoEnergia", canvas, new Vector2(0f, 1f), new Vector2(40f, -112f), new Vector2(700f, 34f), 24f, TextAlignmentOptions.Left, Color.white);
            pLabel.rectTransform.pivot = new Vector2(0f, 1f);
            var (pFill, pBg) = Bar("BarraEnergia", canvas, new Vector2(0f, 1f), new Vector2(40f, -150f));
            var hacks = B.UIText("TextoHackeos", canvas, new Vector2(0f, 1f), new Vector2(40f, -166f), new Vector2(700f, 120f), 22f, TextAlignmentOptions.TopLeft, Color.white);
            hacks.rectTransform.pivot = new Vector2(0f, 1f);
            var pUi = canvas.gameObject.AddComponent<UI.PowerStatusUI>();
            B.SetRef(pUi, "label", pLabel);
            B.SetRef(pUi, "barFill", pFill);
            B.SetRef(pUi, "hackList", hacks);
        }

        internal static (Image fill, Image bg) Bar(string name, Transform canvas, Vector2 anchor, Vector2 pos)
        {
            var bg = B.UIImage(name, canvas, new Color(0f, 0f, 0f, 0.6f));
            bg.rectTransform.pivot = new Vector2(0f, anchor.y);
            B.Anchor(bg.rectTransform, anchor, anchor, pos, new Vector2(300f, 10f));
            var fill = B.UIImage("Relleno", bg.transform, Color.white);
            fill.rectTransform.pivot = new Vector2(0f, 0.5f);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = Vector2.one;
            fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
            return (fill, bg);
        }

        // =====================================================================
        //  MATERIALES
        // =====================================================================

        internal static void CreateMaterials()
        {
            panelMat       = B.CreateMat("M3_PanelIndustrial", new Color(0.2f, 0.21f, 0.2f), null, 0.35f, 0.7f);
            transformerMat = B.CreateMat("M3_Transformador",   new Color(0.16f, 0.22f, 0.18f), null, 0.4f, 0.6f);
            cableMat       = B.CreateMat("M3_Cable",           new Color(0.05f, 0.05f, 0.05f), null, 0.2f, 0.1f);
            coreMat        = B.CreateMat("M3_NucleoReactor",   new Color(0.6f, 0.9f, 1f), new Color(1f, 0.1f, 0.05f), 0.9f, 0.1f);
            cameraMat      = B.CreateMat("M3_Camara",          new Color(0.85f, 0.85f, 0.8f), null, 0.6f, 0.3f);
            screenMat      = B.CreateMat("M3_Pantalla",        new Color(0.1f, 0.5f, 0.3f), new Color(0.1f, 0.9f, 0.4f) * 1.5f);
            sparkMat       = UnlitMat("M3_Chispa", new Color(1f, 0.9f, 0.6f));
            arcMat         = UnlitMat("M3_ArcoElectrico", Color.white);
        }

        /// <summary>Material sin iluminación para partículas y rayos (URP o Built-in).</summary>
        internal static Material UnlitMat(string name, Color color)
        {
            Shader shader = null;
            if (GraphicsSettings.currentRenderPipeline != null) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");

            string path = $"{Root}/Materials/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, path); }
            else mat.shader = shader;
            mat.color = color;
            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
