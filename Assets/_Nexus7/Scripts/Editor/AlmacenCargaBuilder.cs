using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Nexus7.Core;
using Nexus7.Enemies;
using Nexus7.Environment;
using Nexus7.Gameplay;
using Nexus7.Interaction;
using Nexus7.Inventory;
using Nexus7.Stealth;
using Nexus7.Tutorial;
using B = Nexus7.EditorTools.SalaInicialBuilder; // reutilizamos los ayudantes de la Fase 1

namespace Nexus7.EditorTools
{
    /// <summary>
    /// Menú "Nexus-7 > Fase 2 > Construir Almacén de Carga".
    ///
    /// Plano (vista cenital, Z = norte):
    ///
    ///   z=35 ┌──────┐  Montacargas (salida, requiere 2 baterías)
    ///   z=30 ════════════════════════[PUERTA]═══
    ///        │ cajas   ← Dron B (vaivén) →  cajas │
    ///   z=22 │ ║   ║   ║   ║   ║   ║   ║  ▓▓▓▓▓│ ← derrumbe (ruta este bloqueada)
    ///        │ ║   ║  ╔╝ B1 ╚╗  ║   ║  ║  [Llave]
    ///        │D║   ║   ║   ║   ║   ║   ║       │
    ///   z=8  │D║   ║   ║   ║   ║   ║   ║       │   ║ = estanterías,  D = conducto (agachado)
    ///        │D ↑ Dron A (bucle por pasillos x=±6) │
    ///   z=0  ════════[entrada]═══════════════════
    ///              Vestíbulo (inicio)
    ///
    /// Crea: objetos (ScriptableObjects), escena, materiales, iluminación tenue, estanterías, cajas,
    /// conducto de ventilación (ruta alternativa), derrumbe (ruta bloqueada), botín, 2 drones con rutas,
    /// zonas de sombra, puntos de control, HUD ampliado y conecta la salida de la Fase 1 con esta escena.
    /// </summary>
    public static class AlmacenCargaBuilder
    {
        private const string Root = "Assets/_Nexus7";
        private const string ScenePath = Root + "/Scenes/AlmacenCarga.unity";
        private const string Phase1ScenePath = Root + "/Scenes/SalaInicial.unity";
        public const string SceneName = "AlmacenCarga";

        private const float W = 30f, L = 30f, H = 6f, T = 0.2f; // ancho, largo, alto, grosor de muro
        private const int IgnoreRaycastLayer = 2;

        // Materiales compartidos durante la construcción
        internal static Material floorMat, wallMat, metalMat, shelfMat, crateOrange, crateGreen, lampMat,
                                statusMat, batteryMat, scrapMat, toolMat, circuitMat, droneMat, droneEyeMat, warningMat;

        [MenuItem("Nexus-7/Fase 2/Construir Almacén de Carga")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            B.CreateFolders();
            B.EnsureFolder(Root, "Data");
            B.EnsureFolder(Root + "/Data", "Items");

            // ---------- 1. Datos: tipos de objeto ----------
            var items = EnsureItems();
            var battery = items.Battery; var scrap = items.Scrap; var wrench = items.Wrench; var circuit = items.Circuit;

            // ---------- 2. Escena, materiales e iluminación ----------
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateMaterials();
            ConfigureLighting();

            var env = new GameObject("Entorno").transform;
            var props = new GameObject("Atrezo").transform;
            var gameplay = new GameObject("Gameplay").transform;

            BuildShell(env);
            BuildShelves(props);
            BuildCrates(props);
            BuildRubble(props);
            BuildDuct(env, gameplay, wrench);
            var gate = BuildGateAndLift(env, gameplay, battery);
            BuildLights(env);

            // ---------- 3. Botín ----------
            var loot = new GameObject("Botin").transform;
            var pBattery = Pickup("Pickup_Bateria", loot, new Vector3(0f, 0f, 18f), battery, 1, PrimitiveType.Cylinder, new Vector3(0.18f, 0.14f, 0.18f), batteryMat, 0f, true);
            var pScrap = Pickup("Pickup_Chatarra", loot, new Vector3(-5f, 0f, 12f), scrap, 2, PrimitiveType.Cube, new Vector3(0.35f, 0.1f, 0.25f), scrapMat, 3f, false);
            Pickup("Pickup_Chatarra", loot, new Vector3(5f, 0f, 19.5f), scrap, 1, PrimitiveType.Cube, new Vector3(0.35f, 0.1f, 0.25f), scrapMat, 3f, false);
            Pickup("Pickup_Chatarra", loot, new Vector3(-12f, 0f, 2.2f), scrap, 1, PrimitiveType.Cube, new Vector3(0.35f, 0.1f, 0.25f), scrapMat, 3f, false);
            Pickup("Pickup_Chatarra", loot, new Vector3(1.5f, 0f, 28.9f), scrap, 3, PrimitiveType.Cube, new Vector3(0.35f, 0.1f, 0.25f), scrapMat, 3f, false);

            var cWest = Container("Contenedor_Oeste", loot, new Vector3(-12.4f, 0f, 20.8f), -90f,
                new[] { new ItemStack(battery, 1), new ItemStack(scrap, 2) });
            Container("Contenedor_Este", loot, new Vector3(13.6f, 0f, 10f), 90f,
                new[] { new ItemStack(wrench, 1), new ItemStack(circuit, 1) });
            Container("Contenedor_Centro", loot, new Vector3(1.6f, 0f, 9.2f), 0f,
                new[] { new ItemStack(circuit, 2), new ItemStack(scrap, 3), new ItemStack(battery, 1) }); // batería extra → Fase 3

            // Bitácora en el vestíbulo
            var log = B.Box("RegistroSeguridad", env, new Vector3(2.47f, 1.4f, -3f), new Vector3(0.06f, 0.4f, 0.5f), warningMat);
            var logMsg = log.AddComponent<InteractableMessage>();
            B.SetString(logMsg, "promptText", "Leer registro de seguridad");
            B.SetStrings(logMsg, "messages", new[]
            {
                "REGISTRO DE SEGURIDAD — Almacén C: Drones en protocolo de contención. Patrullas automáticas activas.",
                "«Los drones solo ven lo que ilumina su foco. Las zonas sin luz detrás de las cajas son seguras si vas agachado.»",
                "«Nota de mantenimiento: la llave hidráulica quedó en el contenedor del pasillo este. Abre la rejilla del conducto oeste.»"
            });

            // ---------- 4. Jugador ----------
            var (player, interactor) = B.BuildPlayer();
            player.transform.SetPositionAndRotation(new Vector3(0f, 0.05f, -3.2f), Quaternion.identity);
            var inventory = player.gameObject.AddComponent<InventorySystem>();
            var stealth = player.gameObject.AddComponent<PlayerStealth>();
            player.gameObject.AddComponent<PlayerRespawn>();

            // ---------- 5. Enemigos y rutas ----------
            var enemies = new GameObject("Enemigos").transform;
            // WP(posición, espera en s, ¿girarse al esperar?, orientación Y)
            var routeA = Route("Ruta_A_Bucle", enemies, PatrolRoute.RouteMode.Loop, new[]
            {
                new WP(new Vector3(-6f, 0f, 4f),    1.0f, false, 0f),
                new WP(new Vector3(-6f, 0f, 23.5f), 2.0f, true,  90f),   // vigila el pasillo norte
                new WP(new Vector3(6f, 0f, 23.5f),  1.0f, false, 0f),
                new WP(new Vector3(6f, 0f, 4f),     2.0f, true,  -90f),  // vigila el pasillo sur
            });
            var routeB = Route("Ruta_B_Vaiven", enemies, PatrolRoute.RouteMode.PingPong, new[]
            {
                new WP(new Vector3(-12f, 0f, 27f),  3.0f, true, 180f),   // vigila el pasillo oeste
                new WP(new Vector3(0f, 0f, 27f),    1.0f, false, 0f),
                new WP(new Vector3(12.5f, 0f, 25f), 3.0f, true, 180f),   // vigila el pasillo este
            });
            var droneA = Drone("DronSeguridad_A", enemies, new Vector3(-6f, 0f, 4f), 0f, routeA);
            var droneB = Drone("DronSeguridad_B", enemies, new Vector3(-12f, 0f, 27f), 90f, routeB);

            // ---------- 6. NavMesh en tiempo de ejecución ----------
            var navGo = new GameObject("NavMesh");
            navGo.transform.position = new Vector3(0f, 3f, 15f);
            var baker = navGo.AddComponent<RuntimeNavMeshBaker>();
            SetVector3(baker, "size", new Vector3(40f, 12f, 46f));
            SetObjectArray(baker, "ignoreRoots", new Object[] { player.transform, droneA.transform, droneB.transform });

            // ---------- 7. Zonas: sombras, puntos de control, mensajes, salida ----------
            var zones = new GameObject("Zonas").transform;
            var shadowZones = new (Vector3 pos, Vector3 size)[]
            {
                (new Vector3(-11.4f, 1.5f, 6.6f),  new Vector3(3f, 3f, 3f)),
                (new Vector3(0f, 1.5f, 6.5f),      new Vector3(2.6f, 3f, 2.6f)),
                (new Vector3(12.2f, 1.5f, 4.9f),   new Vector3(3.2f, 3f, 3f)),
                (new Vector3(-7.3f, 1.5f, 28.7f),  new Vector3(4f, 3f, 2.4f)),
                (new Vector3(4.6f, 1.5f, 28.7f),   new Vector3(3f, 3f, 2.4f)),
                (new Vector3(-14f, 1.5f, 20.3f),   new Vector3(2f, 3f, 2.6f)),
                (new Vector3(13.5f, 1.5f, 20.6f),  new Vector3(2.6f, 3f, 2.4f)),
            };
            foreach (var z in shadowZones)
                Zone("ZonaSombra", zones, z.pos, z.size).AddComponent<ShadowZone>();

            // A la salida norte del conducto, dentro de la zona de sombra (no delante del puesto del dron B).
            var cp = Zone("Checkpoint_Conducto", zones, new Vector3(-14.1f, 1.5f, 19.8f), new Vector3(1.8f, 3f, 1f));
            cp.AddComponent<Checkpoint>();

            MessageZone("Aviso_Derrumbe", zones, new Vector3(12.3f, 1.5f, 13.5f), new Vector3(5.4f, 3f, 2f),
                "IA NEXUS-7: El pasillo este está bloqueado por un derrumbe. Tendrás que rodear por el centro.");
            MessageZone("Aviso_Conducto", zones, new Vector3(-13.3f, 1.5f, 1.4f), new Vector3(3.4f, 3f, 2f),
                "IA NEXUS-7: Un conducto de ventilación. Si retiras la rejilla podrás avanzar agachado, fuera de la vista de los drones.");
            MessageZone("Aviso_Dron", zones, new Vector3(0f, 1.5f, 0.6f), new Vector3(3f, 3f, 1f),
                "IA NEXUS-7: Dron a la vista. Su foco de luz es su campo de visión: si cambia a amarillo, te está viendo.");

            var exit = Zone("Salida_Montacargas", zones, new Vector3(10f, 1.5f, 33f), new Vector3(4f, 3f, 3f));
            var exitMsg = exit.AddComponent<TutorialTrigger>();
            B.SetStrings(exitMsg, "messages", new[] { "IA NEXUS-7: Montacargas en marcha. Subiendo al nivel de ingeniería..." });
            var exitTrig = exit.AddComponent<SceneExitTrigger>();
            B.SetString(exitTrig, "sceneName", "SalaEnergia"); // Fase 3 (si aún no existe, muestra el mensaje de fin)
            B.SetString(exitTrig, "fallbackMessage", "— FIN DE LA FASE 2 —");

            // ---------- 8. HUD ampliado ----------
            var hud = B.BuildHUD();
            BuildPhase2UI(hud.transform, inventory, stealth);

            // ---------- 9. Sistemas ----------
            var systems = new GameObject("GameSystems");
            var intro = systems.AddComponent<LevelIntro>();
            B.SetRef(intro, "player", player);
            B.SetRef(intro, "hud", hud);
            B.SetStrings(intro, "messages", new[]
            {
                "IA NEXUS-7: Has llegado al Almacén de Carga del Módulo C.",
                "IA NEXUS-7: El montacargas del muro norte no tiene energía. Necesitará dos baterías de iones.",
                "IA NEXUS-7: Advertencia: los drones de seguridad siguen activos y su protocolo los ha vuelto hostiles.",
                "IA NEXUS-7: Observa sus rutas. Agáchate en las zonas oscuras y muévete cuando te den la espalda."
            });
            B.SetString(intro, "hint", "[Tab] abre el inventario · Agachado en la sombra = OCULTO · Correr y abrir contenedores hace ruido");
            B.SetString(intro, "objective", "Objetivo: Consigue 2 baterías de iones y lleva la energía al montacargas (muro norte).");

            // ---------- 10. Prefabs ----------
            B.SavePrefab(player.gameObject, "Player_Fase2");
            B.SavePrefab(hud.gameObject, "HUD_Fase2");
            B.SavePrefab(droneA.gameObject, "DronSeguridad");
            B.SavePrefab(cWest, "ContenedorCarga");
            B.SavePrefab(pBattery, "Pickup_Bateria");
            B.SavePrefab(pScrap, "Pickup_Chatarra");
            B.SavePrefab(gate.gameObject, "PuertaMontacargas");

            // ---------- 11. Guardar y conectar con la Fase 1 ----------
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings();
            ConnectPhase1Exit();

            Selection.activeGameObject = player.gameObject;
            Debug.Log("<color=cyan>[Nexus-7]</color> Almacén de Carga construido en " + ScenePath + ". Pulsa Play.");
        }

        [MenuItem("Nexus-7/Fase 2/Conectar salida de la Fase 1")]
        public static void ConnectPhase1Exit()
        {
            if (!System.IO.File.Exists(Phase1ScenePath))
            {
                Debug.Log("[Nexus-7] No existe SalaInicial.unity: ejecuta antes 'Fase 1 > Construir Sala Inicial' si quieres enlazarlas.");
                return;
            }

            string current = EditorSceneManager.GetActiveScene().path;
            var phase1 = EditorSceneManager.OpenScene(Phase1ScenePath, OpenSceneMode.Single);
            var trigger = GameObject.Find("Trigger_FinFase1");
            if (trigger != null)
            {
                trigger.layer = IgnoreRaycastLayer;
                // (No usar ?? con componentes de Unity: en el editor GetComponent devuelve un "null falso".)
                var exit = trigger.GetComponent<SceneExitTrigger>();
                if (exit == null) exit = trigger.AddComponent<SceneExitTrigger>();
                B.SetString(exit, "sceneName", SceneName);
                EditorSceneManager.SaveScene(phase1);
                Debug.Log("[Nexus-7] Salida de la Fase 1 conectada con " + SceneName + ".");
            }
            else Debug.LogWarning("[Nexus-7] No se encontró 'Trigger_FinFase1' en SalaInicial.");

            if (!string.IsNullOrEmpty(current) && current != Phase1ScenePath)
                EditorSceneManager.OpenScene(current, OpenSceneMode.Single);
        }

        // =====================================================================
        //  ENTORNO
        // =====================================================================

        internal static void BuildShell(Transform env)
        {
            // Almacén principal (x: -15..15, z: 0..30). Suelo/techo en losetas para que cada luz afecte solo a lo cercano.
            Tiled("Suelo", env, new Vector3(0, -T / 2, L / 2), new Vector3(W + 2 * T, T, L + 2 * T), floorMat);
            Tiled("Techo", env, new Vector3(0, H + T / 2, L / 2), new Vector3(W + 2 * T, T, L + 2 * T), metalMat);
            Tiled("MuroOeste", env, new Vector3(-(W + T) / 2, H / 2, L / 2), new Vector3(T, H, L + 2 * T), wallMat);
            Tiled("MuroEste", env, new Vector3((W + T) / 2, H / 2, L / 2), new Vector3(T, H, L + 2 * T), wallMat);

            // Muro sur con abertura de 3 m hacia el vestíbulo
            Tiled("MuroSur_Izq", env, new Vector3(-8.35f, H / 2, -T / 2), new Vector3(13.7f, H, T), wallMat);
            Tiled("MuroSur_Der", env, new Vector3(8.35f, H / 2, -T / 2), new Vector3(13.7f, H, T), wallMat);
            B.Box("MuroSur_Dintel", env, new Vector3(0, 4.5f, -T / 2), new Vector3(3f, 3f, T), wallMat);

            // Muro norte con hueco de 3 m (x 8.5..11.5) para el montacargas
            Tiled("MuroNorte_Izq", env, new Vector3(-3.35f, H / 2, L + T / 2), new Vector3(23.7f, H, T), wallMat);
            Tiled("MuroNorte_Der", env, new Vector3(13.35f, H / 2, L + T / 2), new Vector3(3.7f, H, T), wallMat);
            B.Box("MuroNorte_Dintel", env, new Vector3(10f, 4.75f, L + T / 2), new Vector3(3f, 2.5f, T), wallMat);

            // Vestíbulo de entrada (inicio seguro, fuera de la vista de los drones)
            B.Box("Vest_Suelo", env, new Vector3(0, -T / 2, -2.5f), new Vector3(5.4f, T, 5f), floorMat);
            B.Box("Vest_Techo", env, new Vector3(0, 4f + T / 2, -2.5f), new Vector3(5.4f, T, 5f), metalMat);
            B.Box("Vest_MuroO", env, new Vector3(-2.6f, 2f, -2.5f), new Vector3(T, 4f, 5f), wallMat);
            B.Box("Vest_MuroE", env, new Vector3(2.6f, 2f, -2.5f), new Vector3(T, 4f, 5f), wallMat);
            B.Box("Vest_MuroS", env, new Vector3(0, 2f, -5.1f), new Vector3(5.4f, 4f, T), wallMat);

            // Vigas del techo (atmósfera industrial, sin colisión)
            for (float z = 3f; z < L; z += 6f)
                B.NoCollider(B.Box("Viga", env, new Vector3(0, H - 0.35f, z), new Vector3(W, 0.5f, 0.3f), metalMat));
            // Tuberías a lo largo de los muros
            B.NoCollider(B.Box("Tuberia", env, new Vector3(-14.6f, 4.8f, L / 2), new Vector3(0.3f, 0.3f, L), metalMat));
            B.NoCollider(B.Box("Tuberia", env, new Vector3(14.6f, 4.4f, L / 2), new Vector3(0.3f, 0.3f, L), metalMat));
            // Líneas de seguridad amarillas en el suelo (marcan la ruta del dron A)
            foreach (float x in new[] { -6f, 6f })
                B.NoCollider(B.Box("LineaSuelo", env, new Vector3(x, 0.005f, 13.75f), new Vector3(0.12f, 0.01f, 19.5f), warningMat));
        }

        internal static void BuildShelves(Transform parent)
        {
            var rng = new System.Random(7); // semilla fija: siempre el mismo almacén
            Shelf(parent, -9f, 8f, 22f, rng);
            Shelf(parent, -3f, 8f, 14.4f, rng);  // estanterías centrales partidas: hueco de paso en z≈15
            Shelf(parent, -3f, 15.6f, 22f, rng);
            Shelf(parent, 3f, 8f, 14.4f, rng);
            Shelf(parent, 3f, 15.6f, 22f, rng);
            Shelf(parent, 9f, 8f, 22f, rng);
        }

        internal static void Shelf(Transform parent, float x, float z0, float z1, System.Random rng)
        {
            float len = z1 - z0, cz = (z0 + z1) / 2f;
            var root = new GameObject("Estanteria").transform;
            root.SetParent(parent);

            B.Box("Nucleo", root, new Vector3(x, 1.8f, cz), new Vector3(0.9f, 3.6f, len), shelfMat); // bloquea la visión
            foreach (float h in new[] { 0.05f, 1.2f, 2.4f, 3.55f })
                B.Box("Balda", root, new Vector3(x, h, cz), new Vector3(1.3f, 0.08f, len), metalMat);

            // Cajas pequeñas sobre las baldas (silueta irregular)
            for (float z = z0 + 0.8f; z < z1 - 0.6f; z += 1.6f)
            {
                if (rng.NextDouble() < 0.35) continue;
                float level = rng.NextDouble() < 0.5 ? 1.24f : 2.44f;
                float s = 0.45f + (float)rng.NextDouble() * 0.35f;
                float side = rng.NextDouble() < 0.5 ? -0.3f : 0.3f;
                var c = B.NoCollider(B.Box("CajaPequena", root, new Vector3(x + side, level + s / 2f, z), Vector3.one * s,
                                            rng.NextDouble() < 0.5 ? crateOrange : crateGreen));
                c.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 30f - 15f, 0f);
            }
        }

        internal static void BuildCrates(Transform parent)
        {
            var root = new GameObject("CajasCarga").transform;
            root.SetParent(parent);
            // (x, z, tamaño, rotación Y, altura base)
            var list = new (float x, float z, float s, float r, float y)[]
            {
                (-11.5f, 6.0f, 1.5f, 10f, 0f), (-10.4f, 7.4f, 1.0f, -15f, 0f),     // junto al conducto
                (0f, 6.5f, 1.5f, 5f, 0f),                                          // centro sur
                (12.5f, 4.5f, 1.5f, -10f, 0f), (11.2f, 5.6f, 1.0f, 20f, 0f),       // este sur
                (-8f, 28.6f, 1.5f, 0f, 0f), (-8f, 28.6f, 0.9f, 25f, 1.5f), (-6.5f, 28.8f, 1.0f, 15f, 0f), // norte oeste
                (4f, 28.6f, 1.5f, -8f, 0f), (5.3f, 28.9f, 1.0f, 12f, 0f),         // norte centro
                (-11f, 24.5f, 1.2f, 30f, 0f),                                       // zona oeste
                (13.5f, 21f, 1.5f, 0f, 0f), (13.5f, 21f, 0.8f, -20f, 1.5f),        // este norte
            };
            int i = 0;
            foreach (var c in list)
            {
                var box = B.Box("Caja", root, new Vector3(c.x, c.y + c.s / 2f, c.z), Vector3.one * c.s, (i++ % 2 == 0) ? crateOrange : crateGreen);
                box.transform.rotation = Quaternion.Euler(0f, c.r, 0f);
            }
        }

        internal static void BuildRubble(Transform parent)
        {
            // Derrumbe que bloquea por completo el pasillo este (x 9.6..15) a la altura de z≈17.
            var root = new GameObject("Derrumbe_RutaBloqueada").transform;
            root.SetParent(parent);
            B.Box("Base", root, new Vector3(12.3f, 0.8f, 17f), new Vector3(5.4f, 1.6f, 1.6f), crateGreen);
            Rot(B.Box("Caja", root, new Vector3(10.8f, 1.9f, 16.8f), Vector3.one * 1.2f, crateOrange), 15f, 20f, 10f);
            Rot(B.Box("Caja", root, new Vector3(13.4f, 2.0f, 17.3f), Vector3.one * 1.3f, crateGreen), -10f, 40f, 5f);
            Rot(B.Box("Caja", root, new Vector3(12.2f, 2.6f, 17f), Vector3.one * 1.0f, crateOrange), 25f, 10f, 30f);
            Rot(B.Box("VigaCaida", root, new Vector3(12.3f, 2.4f, 17f), new Vector3(0.3f, 0.3f, 6f), metalMat), 0f, 70f, 25f);
        }

        internal static void BuildDuct(Transform env, Transform gameplay, ItemData wrench)
        {
            // Conducto bajo (1.2 m) por el lateral oeste, de z=3 a z=19. Solo se puede recorrer agachado
            // y los drones (NavMesh de 2 m de alto) no pueden entrar: ruta alternativa segura.
            var root = new GameObject("ConductoVentilacion_RutaAlternativa").transform;
            root.SetParent(env);
            B.Box("ParedInterior", root, new Vector3(-13.3f, 0.7f, 11f), new Vector3(0.2f, 1.4f, 16f), metalMat);
            B.Box("TechoConducto", root, new Vector3(-14.1f, 1.3f, 11f), new Vector3(1.8f, 0.2f, 16f), metalMat);

            var grate = SlidingDoor("RejillaConducto", gameplay, new Vector3(-14.2f, 0f, 3f), new Vector3(1.6f, 1.2f, 0.08f),
                new Vector3(0f, -1.25f, 0f), metalMat, new Vector3(0.95f, 1.0f, -0.15f));
            B.SetString(grate, "lockedPrompt", "Rejilla atornillada");
            B.SetString(grate, "lockedMessage", "La rejilla de ventilación está atornillada. Necesitas una herramienta.");
            B.SetString(grate, "unlockedWithItemsMessage", "Has retirado la rejilla con la llave hidráulica. Entra agachado [Ctrl].");
            SetStacks(grate, "requiredItems", new[] { new ItemStack(wrench, 1) });
            SetBool(grate, "consumeItems", false);        // la herramienta no se gasta
            SetFloat(grate, "doorNoiseRadius", 3f);
        }

        internal static DoorController BuildGateAndLift(Transform env, Transform gameplay, ItemData battery)
        {
            // Cabina del montacargas tras el muro norte
            var lift = new GameObject("Montacargas").transform;
            lift.SetParent(env);
            B.Box("Suelo", lift, new Vector3(10f, -T / 2, 32.7f), new Vector3(5.4f, T, 5f), floorMat);
            B.Box("Techo", lift, new Vector3(10f, H + T / 2, 32.7f), new Vector3(5.4f, T, 5f), metalMat);
            B.Box("MuroO", lift, new Vector3(7.4f, H / 2, 32.7f), new Vector3(T, H, 5f), wallMat);
            B.Box("MuroE", lift, new Vector3(12.6f, H / 2, 32.7f), new Vector3(T, H, 5f), wallMat);
            B.Box("MuroN", lift, new Vector3(10f, H / 2, 35.3f), new Vector3(5.4f, H, T), wallMat);
            B.NoCollider(B.Box("Franja", lift, new Vector3(10f, 0.01f, 30.6f), new Vector3(3f, 0.02f, 0.25f), warningMat));

            var gate = SlidingDoor("PuertaMontacargas", gameplay, new Vector3(10f, 0f, L + T / 2), new Vector3(3f, 3.5f, 0.18f),
                new Vector3(0f, 3.4f, 0f), metalMat, new Vector3(-2.1f, 1.6f, -0.15f));
            B.SetString(gate, "lockedPrompt", "Montacargas sin energía");
            B.SetString(gate, "lockedMessage", "MONTACARGAS: Energía insuficiente. Inserte 2 baterías de iones.");
            B.SetString(gate, "unlockedWithItemsMessage", "IA NEXUS-7: Baterías conectadas. Montacargas operativo. Entra en la cabina.");
            SetStacks(gate, "requiredItems", new[] { new ItemStack(battery, 2) });
            SetBool(gate, "consumeItems", true);
            return gate;
        }

        internal static DoorController SlidingDoor(string name, Transform parent, Vector3 pos, Vector3 panelSize,
                                                  Vector3 openOffset, Material panelMat, Vector3 statusLocalPos)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent);
            root.transform.localPosition = pos;

            var panel = B.Box("Panel", root.transform, new Vector3(0f, panelSize.y / 2f, 0f), panelSize, panelMat);
            var status = B.NoCollider(B.Box("Indicador", root.transform, statusLocalPos, new Vector3(0.25f, 0.25f, 0.05f), statusMat));
            var lightGo = new GameObject("LuzIndicador");
            lightGo.transform.SetParent(root.transform);
            lightGo.transform.localPosition = statusLocalPos + new Vector3(0f, 0f, -0.4f);
            var statusLight = lightGo.AddComponent<Light>();
            statusLight.type = LightType.Point;
            statusLight.range = 2.5f;
            statusLight.intensity = 1.2f;

            var audio = root.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.spatialBlend = 1f;

            var door = root.AddComponent<DoorController>();
            B.SetRef(door, "doorPanel", panel.transform);
            B.SetRef(door, "statusRenderer", status.GetComponent<Renderer>());
            B.SetRef(door, "statusLight", statusLight);
            B.SetRef(door, "audioSource", audio);
            SetVector3(door, "openOffset", openOffset);
            return door;
        }

        internal static void BuildLights(Transform env)
        {
            var root = new GameObject("Iluminacion").transform;
            root.SetParent(env);
            // (posición XZ, parpadea, sombras)
            var lamps = new (float x, float z, bool flicker, bool shadows)[]
            {
                (-10f, 4f, false, true), (0f, 4f, true, false), (10f, 4f, false, true),
                (-6f, 15f, false, false), (6f, 15f, true, true), (12.5f, 11f, false, false),
                (-10f, 26f, true, false), (0f, 26f, false, true),
            };
            foreach (var l in lamps) IndustrialLamp(root, new Vector3(l.x, H - 0.3f, l.z), l.flicker, l.shadows);

            // Baliza naranja giratoria (pulso) sobre el montacargas
            var beacon = B.NoCollider(B.Box("Baliza", root, new Vector3(10f, 5.1f, L - 0.2f), new Vector3(0.4f, 0.2f, 0.2f), warningMat));
            var bl = PointLight("LuzBaliza", root, new Vector3(10f, 4.8f, L - 0.8f), new Color(1f, 0.55f, 0.1f), 9f, 2.5f, false);
            var pulse = bl.gameObject.AddComponent<EmergencyLight>();
            B.SetEnum(pulse, "mode", (int)EmergencyLight.Mode.Pulse);
            B.SetRef(pulse, "emissiveRenderer", beacon.GetComponent<Renderer>());
            SetColor(pulse, "emissionColor", new Color(1f, 0.55f, 0.1f));
            SetFloat(pulse, "baseIntensity", 2.5f);

            PointLight("LuzVestibulo", root, new Vector3(0f, 3.7f, -2.5f), new Color(0.8f, 0.85f, 1f), 6f, 0.9f, false);
            PointLight("LuzMontacargas", root, new Vector3(10f, 5.4f, 33f), new Color(1f, 0.75f, 0.45f), 7f, 1.2f, false);
        }

        internal static void IndustrialLamp(Transform parent, Vector3 pos, bool flicker, bool shadows)
        {
            var fixture = B.NoCollider(B.Box("Lampara", parent, pos, new Vector3(1.4f, 0.1f, 0.35f), lampMat));
            Color c = new Color(0.6f, 0.75f, 1f);
            var light = PointLight("Luz", parent, pos + Vector3.down * 0.3f, c, 11f, 1.6f, shadows);
            if (!flicker) return;

            var fl = light.gameObject.AddComponent<EmergencyLight>();
            B.SetEnum(fl, "mode", (int)EmergencyLight.Mode.Flicker);
            B.SetRef(fl, "emissiveRenderer", fixture.GetComponent<Renderer>());
            SetColor(fl, "emissionColor", c);
            SetFloat(fl, "baseIntensity", 1.6f);
            SetFloat(fl, "emissionStrength", 2f);
            SetFloat(fl, "flickerAmount", 0.5f);
        }

        internal static Light PointLight(string name, Transform parent, Vector3 pos, Color color, float range, float intensity, bool shadows)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.localPosition = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.range = range;
            l.intensity = intensity;
            l.shadows = shadows ? LightShadows.Soft : LightShadows.None;
            return l;
        }

        internal static void ConfigureLighting()
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.04f, 0.05f, 0.07f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.02f, 0.03f, 0.045f);
            RenderSettings.fogDensity = 0.035f;
        }

        // =====================================================================
        //  BOTÍN, ENEMIGOS, ZONAS
        // =====================================================================

        internal static GameObject Pickup(string name, Transform parent, Vector3 floorPos, ItemData item, int amount,
                                         PrimitiveType shape, Vector3 visualScale, Material mat, float noise, bool glow)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent);
            root.transform.localPosition = floorPos;
            var col = root.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.25f, 0f);
            col.size = new Vector3(0.5f, 0.5f, 0.5f);

            var visual = GameObject.CreatePrimitive(shape);
            visual.name = "Visual";
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = new Vector3(0f, 0.3f, 0f);
            visual.transform.localScale = visualScale;
            visual.GetComponent<Renderer>().sharedMaterial = mat;

            if (glow) PointLight("Brillo", root.transform, new Vector3(0f, 0.6f, 0f), item.UiColor, 2f, 0.8f, false);

            var pickup = root.AddComponent<PickupItem>();
            B.SetRef(pickup, "item", item);
            SetInt(pickup, "amount", amount);
            B.SetRef(pickup, "visual", visual.transform);
            SetFloat(pickup, "pickupNoiseRadius", noise);
            return root;
        }

        internal static GameObject Container(string name, Transform parent, Vector3 floorPos, float rotY, ItemStack[] contents)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent);
            root.transform.localPosition = floorPos;
            root.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);

            B.Box("Base", root.transform, new Vector3(0f, 0.35f, 0f), new Vector3(1.2f, 0.7f, 0.8f), crateGreen);
            var lidPivot = new GameObject("PivoteTapa").transform;
            lidPivot.SetParent(root.transform, false);
            lidPivot.localPosition = new Vector3(0f, 0.7f, 0.4f);             // bisagra en el borde trasero
            B.NoCollider(B.Box("Tapa", lidPivot, new Vector3(0f, 0.05f, -0.4f), new Vector3(1.25f, 0.1f, 0.85f), metalMat));
            B.NoCollider(B.Box("Etiqueta", root.transform, new Vector3(0f, 0.45f, -0.405f), new Vector3(0.5f, 0.15f, 0.01f), warningMat));

            var audio = root.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.spatialBlend = 1f;

            var c = root.AddComponent<LootContainer>();
            SetStacks(c, "contents", contents);
            B.SetRef(c, "lid", lidPivot);
            SetVector3(c, "openLidEuler", new Vector3(110f, 0f, 0f));
            B.SetRef(c, "audioSource", audio);
            return root;
        }

        internal struct WP
        {
            public Vector3 pos; public float wait; public bool face; public float rotY;
            public WP(Vector3 pos, float wait, bool face, float rotY) { this.pos = pos; this.wait = wait; this.face = face; this.rotY = rotY; }
        }

        internal static PatrolRoute Route(string name, Transform parent, PatrolRoute.RouteMode mode, WP[] points)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent);
            var route = root.AddComponent<PatrolRoute>();
            B.SetEnum(route, "mode", (int)mode);

            for (int i = 0; i < points.Length; i++)
            {
                var wp = new GameObject("WP_" + i);
                wp.transform.SetParent(root.transform);
                wp.transform.SetPositionAndRotation(points[i].pos, Quaternion.Euler(0f, points[i].rotY, 0f));
                var w = wp.AddComponent<Waypoint>();
                SetFloat(w, "waitTime", points[i].wait);
                SetBool(w, "faceForward", points[i].face);
            }
            return route;
        }

        internal static EnemyPatrol Drone(string name, Transform parent, Vector3 pos, float rotY, PatrolRoute route)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent);
            root.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, rotY, 0f));

            var col = root.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.95f, 0f);
            col.height = 1.9f;
            col.radius = 0.4f;

            var agent = root.AddComponent<NavMeshAgent>();
            agent.radius = 0.4f;
            agent.height = 1.9f;
            agent.speed = 1.8f;
            agent.angularSpeed = 300f;
            agent.acceleration = 12f;
            agent.stoppingDistance = 0.2f;
            agent.enabled = false; // se activa en Start, cuando el NavMesh ya existe

            // Cuerpo + visor
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Cuerpo";
            Object.DestroyImmediate(body.GetComponent<Collider>());
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            body.transform.localScale = new Vector3(0.8f, 0.95f, 0.8f);
            body.GetComponent<Renderer>().sharedMaterial = droneMat;
            B.NoCollider(B.Box("Visor", root.transform, new Vector3(0f, 1.6f, 0.33f), new Vector3(0.55f, 0.14f, 0.12f), droneEyeMat));
            B.NoCollider(B.Box("Antena", root.transform, new Vector3(0.2f, 2.0f, -0.1f), new Vector3(0.04f, 0.35f, 0.04f), metalMat));

            var eye = new GameObject("Ojo").transform;
            eye.SetParent(root.transform, false);
            eye.localPosition = new Vector3(0f, 1.6f, 0.42f);
            eye.localRotation = Quaternion.Euler(8f, 0f, 0f); // ligeramente hacia abajo, como un foco

            var lightGo = new GameObject("FocoVision");
            lightGo.transform.SetParent(eye, false);
            var spot = lightGo.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.range = 12f;
            spot.spotAngle = 80f;
            spot.intensity = 4f;
            spot.shadows = LightShadows.Soft;

            var audio = root.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.spatialBlend = 1f;

            var vision = root.AddComponent<EnemyVision>();
            B.SetRef(vision, "eye", eye);
            B.SetRef(vision, "visionLight", spot);

            var patrol = root.AddComponent<EnemyPatrol>();
            B.SetRef(patrol, "route", route);
            B.SetRef(patrol, "audioSource", audio);
            return patrol;
        }

        internal static GameObject Zone(string name, Transform parent, Vector3 pos, Vector3 size)
        {
            var go = new GameObject(name) { layer = IgnoreRaycastLayer };
            go.transform.SetParent(parent);
            go.transform.localPosition = pos;
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = size;
            return go;
        }

        internal static void MessageZone(string name, Transform parent, Vector3 pos, Vector3 size, string message)
        {
            var t = Zone(name, parent, pos, size).AddComponent<TutorialTrigger>();
            B.SetStrings(t, "messages", new[] { message });
        }

        // =====================================================================
        //  UI DE LA FASE 2
        // =====================================================================

        internal static void BuildPhase2UI(Transform canvas, InventorySystem inventory, PlayerStealth stealth)
        {
            // Notificaciones de recogida (lateral derecho)
            var notifGo = new GameObject("Notificaciones", typeof(RectTransform));
            notifGo.transform.SetParent(canvas, false);
            var notifRt = (RectTransform)notifGo.transform;
            notifRt.pivot = new Vector2(1f, 0.5f);
            B.Anchor(notifRt, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-40f, -40f), new Vector2(560f, 300f));
            var layout = notifGo.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.LowerRight;
            layout.spacing = 4f;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var notif = canvas.gameObject.AddComponent<UI.PickupNotificationUI>();
            B.SetRef(notif, "inventory", inventory);
            B.SetRef(notif, "container", notifRt);

            // Indicador de sigilo (esquina superior derecha)
            var label = B.UIText("TextoSigilo", canvas, new Vector2(1f, 1f), new Vector2(-40f, -40f), new Vector2(400f, 40f),
                                 28f, TextAlignmentOptions.Right, Color.white);
            label.rectTransform.pivot = new Vector2(1f, 1f);
            var barBg = B.UIImage("BarraAlerta", canvas, new Color(0f, 0f, 0f, 0.6f));
            barBg.rectTransform.pivot = new Vector2(1f, 1f);
            B.Anchor(barBg.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f, -88f), new Vector2(300f, 8f));
            var fill = B.UIImage("Relleno", barBg.transform, new Color(1f, 0.8f, 0.1f));
            fill.rectTransform.pivot = new Vector2(0f, 0.5f);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = Vector2.one;
            fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
            var stealthUi = canvas.gameObject.AddComponent<UI.StealthIndicatorUI>();
            B.SetRef(stealthUi, "stealth", stealth);
            B.SetRef(stealthUi, "label", label);
            B.SetRef(stealthUi, "barFill", fill);
            B.SetRef(stealthUi, "barRoot", barBg.gameObject);

            // Panel de inventario (centro, oculto por defecto)
            var panel = B.UIImage("PanelInventario", canvas, new Color(0.02f, 0.04f, 0.07f, 0.88f));
            B.Anchor(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720f, 520f));
            var list = B.UIText("TextoInventario", panel.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(660f, 470f),
                                26f, TextAlignmentOptions.TopLeft, Color.white);
            var invUi = canvas.gameObject.AddComponent<UI.InventoryUI>();
            B.SetRef(invUi, "inventory", inventory);
            B.SetRef(invUi, "panel", panel.gameObject);
            B.SetRef(invUi, "listText", list);
            panel.gameObject.SetActive(false);
        }

        // =====================================================================
        //  MATERIALES Y DATOS
        // =====================================================================

        internal static void CreateMaterials()
        {
            floorMat    = B.CreateMat("M2_SueloAlmacen", new Color(0.16f, 0.17f, 0.18f), null, 0.45f, 0.7f);
            wallMat     = B.CreateMat("M2_MuroAlmacen",  new Color(0.24f, 0.26f, 0.28f), null, 0.3f, 0.5f);
            metalMat    = B.CreateMat("M2_MetalOscuro",  new Color(0.12f, 0.13f, 0.14f), null, 0.6f, 0.9f);
            shelfMat    = B.CreateMat("M2_Estanteria",   new Color(0.2f, 0.22f, 0.26f), null, 0.4f, 0.8f);
            crateOrange = B.CreateMat("M2_CajaNaranja",  new Color(0.55f, 0.32f, 0.12f), null, 0.25f, 0.3f);
            crateGreen  = B.CreateMat("M2_CajaVerde",    new Color(0.22f, 0.28f, 0.22f), null, 0.25f, 0.3f);
            lampMat     = B.CreateMat("M2_Lampara",      Color.black, new Color(0.6f, 0.75f, 1f) * 2f);
            statusMat   = B.CreateMat("M2_Indicador",    Color.red, new Color(1f, 0.1f, 0.05f) * 3f);
            batteryMat  = B.CreateMat("M2_Bateria",      new Color(0.4f, 0.9f, 0.3f), new Color(0.4f, 1f, 0.3f) * 1.5f);
            scrapMat    = B.CreateMat("M2_Chatarra",     new Color(0.5f, 0.5f, 0.52f), null, 0.7f, 1f);
            toolMat     = B.CreateMat("M2_Herramienta",  new Color(1f, 0.6f, 0.15f), new Color(1f, 0.5f, 0.1f) * 0.8f);
            circuitMat  = B.CreateMat("M2_Circuito",     new Color(0.2f, 0.7f, 1f), new Color(0.2f, 0.7f, 1f) * 1.2f);
            droneMat    = B.CreateMat("M2_Dron",         new Color(0.15f, 0.15f, 0.17f), null, 0.8f, 0.9f);
            droneEyeMat = B.CreateMat("M2_VisorDron",    Color.red, new Color(1f, 0.15f, 0.05f) * 4f);
            warningMat  = B.CreateMat("M2_Aviso",        new Color(1f, 0.75f, 0.1f), new Color(1f, 0.7f, 0.1f) * 0.6f);
        }

        /// <summary>Tipos de objeto compartidos por todas las fases (crea o actualiza los assets).</summary>
        internal class ItemSet { public ItemData Battery, Scrap, Wrench, Circuit; }

        internal static ItemSet EnsureItems()
        {
            B.EnsureFolder(Root, "Data");
            B.EnsureFolder(Root + "/Data", "Items");
            var set = new ItemSet();
            set.Battery = CreateItem("Item_Bateria", "battery_ion", "Batería de iones",
                "Celda de energía de alta capacidad. Alimenta puertas y maquinaria.", ItemCategory.Battery, 5, false, new Color(0.55f, 1f, 0.35f));
            set.Scrap = CreateItem("Item_Chatarra", "scrap_metal", "Chatarra metálica",
                "Restos de placas y tornillos. Útil para fabricar y reparar.", ItemCategory.Part, 20, false, new Color(0.75f, 0.75f, 0.8f));
            set.Wrench = CreateItem("Item_LlaveHidraulica", "tool_hydraulic_wrench", "Llave hidráulica",
                "Herramienta de mantenimiento. Permite retirar rejillas y paneles atornillados.", ItemCategory.Tool, 1, true, new Color(1f, 0.65f, 0.2f));
            set.Circuit = CreateItem("Item_Circuito", "component_circuit", "Circuito de control",
                "Placa electrónica intacta. Necesaria para reparar terminales.", ItemCategory.Component, 10, false, new Color(0.3f, 0.85f, 1f));
            AssetDatabase.SaveAssets();
            return set;
        }

        internal static ItemData CreateItem(string file, string id, string displayName, string description,
                                           ItemCategory category, int maxStack, bool keyItem, Color color)
        {
            string path = $"{Root}/Data/Items/{file}.asset";
            var item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (item == null)
            {
                item = ScriptableObject.CreateInstance<ItemData>();
                AssetDatabase.CreateAsset(item, path);
            }
            var so = new SerializedObject(item);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("description").stringValue = description;
            so.FindProperty("category").enumValueIndex = (int)category;
            so.FindProperty("maxStack").intValue = maxStack;
            so.FindProperty("isKeyItem").boolValue = keyItem;
            so.FindProperty("uiColor").colorValue = color;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(item);
            return item;
        }

        internal static void AddToBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (!scenes.Any(s => s.path == ScenePath)) scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        // =====================================================================
        //  UTILIDADES
        // =====================================================================

        /// <summary>Crea una caja dividida en losetas de ≤ 6 m (mejor iluminación con muchas luces).</summary>
        internal static void Tiled(string name, Transform parent, Vector3 center, Vector3 size, Material mat, float tile = 6f)
        {
            int nx = Mathf.Max(1, Mathf.CeilToInt(size.x / tile));
            int nz = Mathf.Max(1, Mathf.CeilToInt(size.z / tile));
            var piece = new Vector3(size.x / nx, size.y, size.z / nz);
            var root = new GameObject(name).transform;
            root.SetParent(parent);
            for (int ix = 0; ix < nx; ix++)
                for (int iz = 0; iz < nz; iz++)
                {
                    var p = new Vector3(center.x - size.x / 2f + piece.x * (ix + 0.5f), center.y,
                                        center.z - size.z / 2f + piece.z * (iz + 0.5f));
                    B.Box(name + "_" + ix + "_" + iz, root, p, piece, mat);
                }
        }

        internal static GameObject Rot(GameObject go, float x, float y, float z)
        {
            go.transform.rotation = Quaternion.Euler(x, y, z);
            return go;
        }

        internal static SerializedObject So(Object target, string field, out SerializedProperty prop)
        {
            var so = new SerializedObject(target);
            prop = so.FindProperty(field);
            if (prop == null) Debug.LogWarning($"[Nexus-7] Campo '{field}' no encontrado en {target.GetType().Name}");
            return so;
        }

        internal static void SetInt(Object t, string f, int v)         { var so = So(t, f, out var p); if (p == null) return; p.intValue = v; so.ApplyModifiedPropertiesWithoutUndo(); }
        internal static void SetFloat(Object t, string f, float v)     { var so = So(t, f, out var p); if (p == null) return; p.floatValue = v; so.ApplyModifiedPropertiesWithoutUndo(); }
        internal static void SetBool(Object t, string f, bool v)       { var so = So(t, f, out var p); if (p == null) return; p.boolValue = v; so.ApplyModifiedPropertiesWithoutUndo(); }
        internal static void SetColor(Object t, string f, Color v)     { var so = So(t, f, out var p); if (p == null) return; p.colorValue = v; so.ApplyModifiedPropertiesWithoutUndo(); }
        internal static void SetVector3(Object t, string f, Vector3 v) { var so = So(t, f, out var p); if (p == null) return; p.vector3Value = v; so.ApplyModifiedPropertiesWithoutUndo(); }

        internal static void SetVector2(Object t, string f, Vector2 v) { var so = So(t, f, out var p); if (p == null) return; p.vector2Value = v; so.ApplyModifiedPropertiesWithoutUndo(); }

        internal static void SetObjectArray(Object t, string f, Object[] values)
        {
            var so = So(t, f, out var p);
            if (p == null) return;
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void SetStacks(Object t, string f, IList<ItemStack> stacks)
        {
            var so = So(t, f, out var p);
            if (p == null) return;
            p.arraySize = stacks.Count;
            for (int i = 0; i < stacks.Count; i++)
            {
                var e = p.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("item").objectReferenceValue = stacks[i].item;
                e.FindPropertyRelative("amount").intValue = stacks[i].amount;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
