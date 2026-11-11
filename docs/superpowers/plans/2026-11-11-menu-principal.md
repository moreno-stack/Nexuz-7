# Menú principal, guardado, configuración y audio — Plan de implementación

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Que Play (y el juego compilado) arranque siempre en un Menú Principal con Nueva partida, Continuar, Configuración y Salir, con partida guardada por fase, ajustes de audio/calidad/pantalla y música y sonidos generados por código.

**Architecture:** Lógica pura y probada fuera de Unity (`SaveSlot`, `GameSettings` sin motor, `QualityPresets`, `ProceduralAudio`, `SceneFade`, `MenuState`) envuelta por componentes finos de Unity (`SceneLoader`, `SaveSystem`, `AudioChannel`, `MusicPlayer`, `UISounds`, `SettingsMenuUI`). La UI del menú la construye `MenuScreenBuilder` por código, como el resto del proyecto. Los constructores de fase se arreglan para que "Construir todo" llegue hasta el menú.

**Tech Stack:** Unity 6000.6.4f1, URP 17.6, C#, TextMeshPro, uGUI, `JsonUtility`, PlayerPrefs.

**Spec:** `docs/superpowers/specs/2026-11-11-menu-principal-design.md`

## Global Constraints

- Commits con autor `moreno-stack <moreno-stack@users.noreply.github.com>`, sin ninguna atribución a Claude, varios commits pequeños.
- Todo el texto visible y los comentarios en español.
- Nada de `Object.GetInstanceID()` (obsoleto con error en Unity 6.6); no usar `??` con objetos de Unity.
- Los assets de URP no se modifican en tiempo de juego.
- Pruebas: `bash Tests/run_all.sh` (mcs + Roslyn + suites). Una suite = `Tests/Suites/<Nombre>.cs` con `Main`, se ejecuta con `bash Tests/run.sh <Nombre>`. Las definiciones simuladas de Unity se amplían en `Tests/Stubs/UnityStubs.cs` (la matemática simulada es falsa: la lógica pura usa la suya).
- Archivo de guardado: `Application.persistentDataPath/nexus7_partida.json`, `version = 1`.
- PlayerPrefs: `Nexus7_VolGeneral` (0,8), `Nexus7_VolMusica` (0,7), `Nexus7_VolEfectos` (0,8), `Nexus7_Calidad` (Alta = 2), `Nexus7_PantallaCompleta` (1).
- Catálogo: `Assets/_Nexus7/Resources/Nexus7_CatalogoObjetos.asset`, cargado con `Resources.Load<ItemCatalog>("Nexus7_CatalogoObjetos")`.
- Fases (escena → título): `SalaInicial` "Fase 1 · Sala inicial", `AlmacenCarga` "Fase 2 · Almacén de carga", `SalaEnergia` "Fase 3 · Sala de energía", `Laboratorio` "Fase 4 · Laboratorio", `CentroControl` "Fase 5 · Centro de control", `Evacuacion` "Fase 6 · Evacuación".

## Review Focus

- Partida guardada de una escena que ya no está en Build Settings → Continuar desactivado con "Partida no disponible", nunca una carga que falla. (Tarea 3: prueba `Describe` + `HasSave` con escena no cargable.)
- Archivo de guardado vacío, corrupto o de otra versión → se trata como "sin partida" sin excepción. (Tarea 3: `SaveSlot.Parse` con "", "{", versión 2.)
- Pulsar Nueva partida/Continuar dos veces seguidas o durante una carga → una sola carga. (Tarea 4: `SceneFade.CanStart`.)
- Slider de volumen a 0 → silencio real (no 0,0001), y a 100 → volumen base sin saturar. (Tarea 2: `Curve(0)=0`, `Curve(1)=1`.)
- [Esc] con Configuración abierta cierra solo el panel, no el juego, y [Intro] no lanza Nueva partida mientras el panel o la confirmación están abiertos. (Tarea 6: `MenuState.AcceptsMainInput`.)

---

### Task 1: Constructores que llegan hasta el menú y Play por el menú

**Files:**
- Modify: `Assets/_Nexus7/Scripts/Editor/AlmacenCargaBuilder.cs:235`, `SalaEnergiaBuilder.cs:292`, `LaboratorioBuilder.cs:266`, `CentroControlBuilder.cs:286`
- Modify: `Assets/_Nexus7/Scripts/Editor/BuildAllScenes.cs`
- Create: `Assets/_Nexus7/Scripts/Editor/PlayFromMenu.cs`
- Modify: `Assets/_Nexus7/Scripts/Editor/EvacuacionBuilder.cs` (`ConfigureBuildSettings` pasa a `internal static` sin cambiar su lógica; menú "Nexus-7/Ordenar Build Settings")

**Interfaces:**
- Produces: `BuildAllScenes.Build()` sigue con las demás fases si una falla; `BuildReport.Describe(string[] failed) -> string` (lógica pura en `BuildAllScenes.cs`, clase `internal static BuildReport`); `PlayFromMenu.Enabled` (bool, `EditorPrefs` "Nexus7.PlayFromMenu", por defecto true).

- [ ] **Step 1:** En los 4 constructores, sustituir `Selection.activeGameObject = player.gameObject;` por una búsqueda nueva del jugador (`GameObject.FindGameObjectWithTag("Player")`) con comprobación de nulo, con el comentario de por qué (el `Connect…()` anterior abre otra escena y destruye `player`).
- [ ] **Step 2: Write the failing test** `Tests/Suites/BuildReportTest.cs`: `BuildReport.Describe(new string[0]) == "Juego construido: 6 fases y menú principal."`; `Describe(new[]{"Fase 3: Sala de energía"})` contiene `"Fase 3: Sala de energía"` y empieza por `"Se construyó el juego con errores"`.
- [ ] **Step 3:** Run `bash Tests/run.sh BuildReportTest` → FAIL (no compila: `BuildReport` no existe).
- [ ] **Step 4:** `BuildAllScenes.Build()`: cada fase en `try/catch` (`Debug.LogException`, guarda el nombre), luego `ItemCatalogBuilder.Build()` (Tarea 3), `EvacuacionBuilder.ConfigureBuildSettings()`, abre `MenuPrincipal` si existe y muestra `EditorUtility.DisplayDialog` con `BuildReport.Describe(failed)`.
- [ ] **Step 5:** `PlayFromMenu`: `[InitializeOnLoad]`, en el constructor y al cambiar el interruptor asigna `EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(BuildAllScenes.MenuScenePath)` si `Enabled` y existe; si no, `null`. `[MenuItem("Nexus-7/Empezar Play por el menú", false, 1)]` alterna `Enabled` con `Menu.SetChecked`.
- [ ] **Step 6:** Run `bash Tests/run_all.sh` → compilación OK, Roslyn OK, 0 fallos.
- [ ] **Step 7: Commit** — "Editor: Construir todo llega hasta el menú aunque una fase falle, y Play empieza siempre por el menú" (+ un commit aparte para el arreglo de los 4 constructores: "Editor: los constructores ya no usan al jugador de una escena cerrada").

### Task 2: Configuración (`GameSettings` y `QualityPresets`)

**Files:**
- Create: `Assets/_Nexus7/Scripts/Core/GameSettings.cs`, `Assets/_Nexus7/Scripts/Core/QualityPresets.cs`
- Test: `Tests/Suites/SettingsTest.cs`

**Interfaces:**
- Produces: `enum QualityLevel { Baja, Media, Alta }`; `static class GameSettings { float Master, Music, Sfx {get;set;} (0–1, recortados); QualityLevel Quality; bool Fullscreen; event Action Changed; float Curve(float v); string NameOf(QualityLevel); void ResetToDefaults(); void ApplyAll(); }`; `struct QualityPreset { string unityLevel; bool postProcessing; bool shadows; AaMode antialiasing; bool filmGrain; }`, `enum AaMode { None, Fxaa, Smaa }`; `static QualityPresets { QualityPreset For(QualityLevel); int FindLevel(string[] names, string wanted, int fallback); void Apply(QualityLevel) }`.

- [ ] **Step 1: Write the failing test** (`SettingsTest`): valores por defecto 0,8 / 0,7 / 0,8 / Alta / true tras `ResetToDefaults()`; `Master = 1.7f` → 1, `= -2` → 0; `Curve(0)==0`, `Curve(1)==1`, `Curve(0.5f)==0.25f`; `NameOf` → "Baja", "Media", "Alta"; `Changed` se dispara una vez por asignación; `For(Baja)` = {"Mobile", sin posprocesado, sin sombras, None, sin grano}, `For(Media)` = {"PC", posprocesado, sombras, Fxaa, sin grano}, `For(Alta)` = {"PC", posprocesado, sombras, Smaa, grano}; `FindLevel(new[]{"Mobile","PC"},"PC",0)==1`, nombre inexistente → fallback.
- [ ] **Step 2:** Run `bash Tests/run.sh SettingsTest` → FAIL (no compila).
- [ ] **Step 3:** Implementar. Los valores se leen de PlayerPrefs la primera vez que se usan y se guardan al asignarse (`PlayerPrefs.Save()`). `ApplyAll()`: `AudioListener.volume = Curve(Master)`; `Screen.fullScreenMode = Fullscreen ? FullScreenWindow : Windowed` (fuera del editor); `QualityPresets.Apply(Quality)`. `QualityPresets.Apply`: `QualitySettings.SetQualityLevel(FindLevel(QualitySettings.names, …), true)`; para cada `Camera` activa: `GetUniversalAdditionalCameraData()` → `renderPostProcessing`, `renderShadows`, `antialiasing` (None / FastApproximateAntialiasing / SubpixelMorphologicalAntiAliasing con `antialiasingQuality = High`); para cada `Volume`: `profile.TryGet<FilmGrain>` → `active = filmGrain`. `[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]` + `SceneManager.sceneLoaded` → `ApplyAll()`.
- [ ] **Step 4:** Run `bash Tests/run.sh SettingsTest` → TODAS LAS PRUEBAS PASAN; `bash Tests/run_all.sh` → 0 fallos.
- [ ] **Step 5: Commit** — "Configuración: volúmenes, calidad Baja/Media/Alta y pantalla completa guardados entre partidas".

### Task 3: Partida guardada (`SaveSlot`, `ItemCatalog`, `SaveSystem`)

**Files:**
- Create: `Assets/_Nexus7/Scripts/Core/SaveData.cs` (datos + `SaveSlot` puro), `Assets/_Nexus7/Scripts/Core/SaveSystem.cs`, `Assets/_Nexus7/Scripts/Inventory/ItemCatalog.cs`, `Assets/_Nexus7/Scripts/Editor/ItemCatalogBuilder.cs`
- Modify: `Core/GameSession.cs` (añadir `Restore`), `Core/RunStats.cs` (añadir `Snapshot`/`Restore`), `Core/GameFlow.cs` (lista de fases)
- Test: `Tests/Suites/SaveTest.cs`

**Interfaces:**
- Consumes: `ItemStack(ItemData item, int amount)`, `ItemData.Id`, `InventorySystem.GetSummary()`, `DifficultySettings.Current`.
- Produces:
  - `[Serializable] class SaveData { int version; string scene; string phaseTitle; long savedAtUtcTicks; List<SavedItem> items; bool hasSuit; int runSeed; string hotbarId; int difficulty; RunStatsSnapshot stats; }`, `[Serializable] class SavedItem { string id; int amount; }`, `[Serializable] class RunStatsSnapshot { float elapsed; int itemsCollected, itemsCrafted, devicesUsed, hacksSucceeded, empTargetsHit, decoysTriggered, deaths; float damageTaken; int[] deviceKinds; string[] sectors; }`.
  - `static class SaveSlot { const int Version = 1; string RelativeTime(TimeSpan ago); string Describe(SaveData d, DateTime nowUtc, bool sceneAvailable); bool IsCompatible(SaveData d); List<SavedItem> ToSaved(IEnumerable<ItemStack>); List<ItemStack> FromSaved(List<SavedItem>, Func<string, ItemData> find, out int unknown); }`.
  - `GameFlow.Phases` (`(string scene, string title)[]` con los valores de Global Constraints), `GameFlow.IsPhaseScene(string)`, `GameFlow.PhaseTitle(string)`.
  - `GameSession.Restore(IEnumerable<ItemStack> inventory, bool hasSuit, int runSeed, string hotbarId)`; `RunStats.Snapshot() -> RunStatsSnapshot`, `RunStats.Restore(RunStatsSnapshot)`.
  - `ItemCatalog : ScriptableObject { ItemData Find(string id); void SetItems(IEnumerable<ItemData>); static ItemCatalog Load(); }`; `ItemCatalogBuilder.Build()` (editor, menú "Nexus-7/Actualizar catálogo de objetos").
  - `static class SaveSystem { string FilePath; SaveData Peek(); bool HasSave; string Describe(); void SaveCurrent(string scene, InventorySystem inventory); bool LoadAndApply(out string scene); void Delete(); }`; `SaveSlot.Parse(string json) -> SaveData` (null si vacío/corrupto/incompatible).

- [ ] **Step 1: Write the failing test** (`SaveTest`): `RelativeTime` → <1 min "hace un momento", 5 min "hace 5 min", 2 h 10 min "hace 2 h", 1 día "hace 1 día", 3 días "hace 3 días"; `Describe(d con scene "SalaEnergia", ahora−2 h, true) == "Fase 3 · Sala de energía — hace 2 h"`; con `sceneAvailable=false` → "Partida no disponible"; `IsCompatible` false con versión 2 o escena vacía; `ToSaved` de 2 objetos → 2 `SavedItem` con id y cantidad; `FromSaved` con un id desconocido → lo omite y `unknown == 1`; `GameFlow.IsPhaseScene("MenuPrincipal") == false`, `("Laboratorio") == true`; `RunStats.Restore(Snapshot())` conserva `Deaths` y `ItemsCrafted`.
- [ ] **Step 2:** Run `bash Tests/run.sh SaveTest` → FAIL (no compila).
- [ ] **Step 3:** Implementar los tipos puros, `GameSession.Restore`, `RunStats.Snapshot/Restore` (`StartTime = Time.realtimeSinceStartup − elapsed`) y `GameFlow.Phases`.
- [ ] **Step 4:** Implementar `SaveSystem` con `File`/`JsonUtility`: escribir en `FilePath + ".tmp"` y mover (no deja un archivo a medias); `Peek()` cachea hasta el siguiente `SaveCurrent`/`Delete`; `HasSave = Peek() != null && Application.CanStreamedLevelBeLoaded(scene)`; `LoadAndApply`: `GameFlow.ResetRunState()`, `DifficultySettings.Current`, `GameSession.Restore`, `RunStats.Restore`. Cualquier excepción de E/S → `Debug.LogWarning` y comportarse como "sin partida".
- [ ] **Step 5:** `ItemCatalog` + `ItemCatalogBuilder` (busca `t:ItemData` en `Assets/_Nexus7/Data`, crea la carpeta `Resources` si falta).
- [ ] **Step 6: Write the failing test** de Review Focus: `SaveSlot.Parse("")`, `Parse("{")` y un JSON con `"version":2` devuelven null sin excepción. Run → FAIL hasta que `Parse` capture la excepción de `JsonUtility` (en las pruebas, la versión simulada de `JsonUtility.FromJson` lanza con texto no válido).
- [ ] **Step 7:** Run `bash Tests/run.sh SaveTest` → pasa; `bash Tests/run_all.sh` → 0 fallos.
- [ ] **Step 8: Commit** — dos commits: "Guardado: partida guardada por fase con inventario, traje, dificultad y estadísticas" y "Inventario: catálogo de objetos para guardar y cargar por Id".

### Task 4: `SceneLoader`, autoguardado y Continuar

**Files:**
- Create: `Assets/_Nexus7/Scripts/Core/SceneLoader.cs`
- Modify: `Core/GameFlow.cs`, `Core/SceneExitTrigger.cs:49`, `UI/ConsoleUI.cs:188`, `Gameplay/EndingManager.cs` (`Escape`, `Fail`)
- Test: `Tests/Suites/SceneLoaderTest.cs`

**Interfaces:**
- Consumes: `SaveSystem.SaveCurrent/LoadAndApply/Delete`, `GameFlow.IsPhaseScene`.
- Produces: `static class SceneFade { float Alpha(float elapsed, float duration, bool fadingIn); bool CanStart(bool loading, bool sceneInBuild); }`; `class SceneLoader : MonoBehaviour { static bool IsLoading; static bool Load(string scene); }` (se crea solo con `RuntimeInitializeOnLoadMethod(BeforeSceneLoad)`, `DontDestroyOnLoad`, Canvas propio con orden 1000: negro + barra de progreso); `GameFlow.ContinueRun()`, `GameFlow.OnRunFinished()`.

- [ ] **Step 1: Write the failing test**: `Alpha(0,0.35f,false)==0`, `Alpha(0.35f,0.35f,false)==1`, `Alpha(0.175f,0.35f,true)==0.5f`, duración 0 → 1 (fundido a negro) / 0 (entrada); `CanStart(true,true)==false`, `CanStart(false,false)==false`, `CanStart(false,true)==true`.
- [ ] **Step 2:** Run `bash Tests/run.sh SceneLoaderTest` → FAIL.
- [ ] **Step 3:** Implementar `SceneFade` y `SceneLoader` (corrutina con `Time.unscaledDeltaTime`: fundido 0,35 s → `LoadSceneAsync` → progreso `Mathf.Clamp01(op.progress / 0.9f)` → 1 fotograma → fundido de entrada 0,35 s). En `SceneManager.sceneLoaded`, si `GameFlow.IsPhaseScene`, esperar 1 fotograma y llamar `SaveSystem.SaveCurrent(escena, inventario del objeto con etiqueta "Player")`.
- [ ] **Step 4:** `GameFlow.Load` usa `SceneLoader.Load`; `ContinueRun()`: si `SaveSystem.LoadAndApply(out scene)` → `SceneLoader.Load(scene)`; `OnRunFinished()` → `SaveSystem.Delete()`, llamado desde `EndingManager.Escape` y `EndingManager.Fail`. `SceneExitTrigger` y `ConsoleUI` llaman a `SceneLoader.Load` en lugar de `SceneManager.LoadScene`.
- [ ] **Step 5:** Run `bash Tests/run.sh SceneLoaderTest` → pasa; `bash Tests/run_all.sh` → 0 fallos.
- [ ] **Step 6: Commit** — "Escenas: SceneLoader con fundido y barra de carga, autoguardado al entrar en cada fase y Continuar".

### Task 5: Audio por canales, música y sonidos de la UI

**Files:**
- Create: `Assets/_Nexus7/Scripts/Audio/ProceduralAudio.cs`, `AudioChannel.cs`, `MusicPlayer.cs`, `UISounds.cs`
- Test: `Tests/Suites/AudioTest.cs`

**Interfaces:**
- Consumes: `GameSettings.Music/Sfx/Curve/Changed`, `GameFlow.IsPhaseScene`, `GameFlow.MenuScene`.
- Produces: namespace `Nexus7.Audio`; `enum AudioCategory { Music, Sfx }`; `static ProceduralAudio { const int SampleRate = 22050; float[] MenuPad(float seconds); float[] ShipDrone(float seconds); float[] Ui(UiSound kind); }`, `enum UiSound { Hover, Click, Back, Test }`; `AudioChannel : MonoBehaviour { AudioCategory Category; static float VolumeFor(AudioCategory c, float baseVolume); }`; `MusicPlayer` (singleton automático, fundido 1,5 s entre pistas); `UISounds : MonoBehaviour` (en un Canvas: engancha `onClick` y `PointerEnter` de todos sus `Button`) y `static UISounds.Play(UiSound)`.

- [ ] **Step 1: Write the failing test**: `MenuPad(16)` tiene `16 × 22050` muestras, todas en [−1, 1], y `|m[0] − m[último]| < 0,02` (bucle sin chasquido: frecuencias con un número entero de ciclos en 16 s); `ShipDrone(12)` igual con 12 s y nivel RMS menor que `MenuPad`; cada `Ui(kind)` dura entre 0,03 y 0,4 s, empieza y termina en |x| < 0,01 y no está en silencio (pico > 0,1); `VolumeFor(Music, 0.5f)` con `GameSettings.Music = 1` → 0,5 y con `Music = 0` → 0.
- [ ] **Step 2:** Run `bash Tests/run.sh AudioTest` → FAIL.
- [ ] **Step 3:** Implementar `ProceduralAudio`. Menú: acorde de pad (La menor 9: 110, 165, 220, 261,63→ redondear cada frecuencia a un múltiplo de 1/16 Hz) con trémolo lento y una nota de campana cada 4 s con caída exponencial; nave: 55 Hz + 82,5 Hz con ruido filtrado suave y pulso lento. UI: Hover = 1 200 Hz 0,04 s; Click = 660→880 Hz 0,09 s; Back = 520→330 Hz 0,12 s; Test = 880 Hz 0,2 s; envolvente con ataque y caída a 0.
- [ ] **Step 4:** `AudioChannel` (lee `AudioSource.volume` en `Awake` como volumen base, aplica `VolumeFor` al empezar y en `GameSettings.Changed`); en `SceneManager.sceneLoaded` añade `AudioChannel` (Sfx) a todos los `AudioSource` sin uno. `MusicPlayer` crea los `AudioClip` con `AudioClip.Create(nombre, n, 1, 22050, false)` + `SetData`, elige pista por escena (menú → MenuPad; fase → ShipDrone; otra → nada) y cruza con fundido. `UISounds` usa un `AudioSource` 2D propio con `AudioChannel` Sfx.
- [ ] **Step 5:** Run `bash Tests/run.sh AudioTest` → pasa; `bash Tests/run_all.sh` → 0 fallos.
- [ ] **Step 6: Commit** — "Audio: canales de música y efectos, música del menú y de la nave y sonidos de botones generados por código".

### Task 6: Interfaz del menú y panel de Configuración

**Files:**
- Modify: `Assets/_Nexus7/Scripts/UI/MainMenuUI.cs`, `Assets/_Nexus7/Scripts/Editor/MenuScreenBuilder.cs` (`BuildMenuUI` y botones), `Assets/_Nexus7/Scripts/Editor/EvacuacionBuilder.cs` (`BuildMainMenuScene` llama a `ItemCatalogBuilder.Build()`)
- Create: `Assets/_Nexus7/Scripts/UI/SettingsMenuUI.cs`, `Assets/_Nexus7/Scripts/UI/MenuState.cs`
- Test: `Tests/Suites/MenuStateTest.cs`

**Interfaces:**
- Consumes: `GameFlow.StartNewRun/ContinueRun/Quit`, `SaveSystem.HasSave/Describe`, `GameSettings.*`, `UISounds.Play`.
- Produces: `static MenuState { bool ContinueEnabled(bool hasSave); bool NeedsConfirm(bool hasSave); bool AcceptsMainInput(bool settingsOpen, bool confirmOpen); int ToPercent(float v); float FromPercent(float p); }`; `SettingsMenuUI : MonoBehaviour { bool IsOpen; void Open(); void Close(); }` con campos `panel, masterSlider, musicSlider, sfxSlider, masterValue, musicValue, sfxValue, qualityButtons[3], qualityFrames[3], fullscreenButton, fullscreenLabel, backButton, firstSelected`; `MainMenuUI` nuevos campos `continueButton, continueDetail, settingsButton, settings, confirmPanel, confirmYes, confirmNo`.

- [ ] **Step 1: Write the failing test**: `ContinueEnabled(false)==false`; `NeedsConfirm(true)==true`, `(false)==false`; `AcceptsMainInput(false,false)==true`, `(true,false)==false`, `(false,true)==false`; `ToPercent(0.8f)==80`, `ToPercent(1.2f)==100`, `FromPercent(35)==0.35f`.
- [ ] **Step 2:** Run `bash Tests/run.sh MenuStateTest` → FAIL.
- [ ] **Step 3:** `MenuState` + `SettingsMenuUI` (sliders 0–100 enteros con su "80 %"; mover Efectos toca `UiSound.Test` como mucho cada 0,15 s; calidad con marco dorado `MainMenuUI.SelectedFrame` en la elegida; pantalla completa "PANTALLA COMPLETA: SÍ/NO"; [Esc] o VOLVER cierran y devuelven la selección a CONFIGURACIÓN).
- [ ] **Step 4:** `MainMenuUI`: Continuar `interactable = MenuState.ContinueEnabled(SaveSystem.HasSave)`, detalle `SaveSystem.Describe()` o "Sin partida guardada"; Nueva partida → si `NeedsConfirm` abre `confirmPanel` ("Se sobrescribirá la partida guardada. ¿Empezar de nuevo?") si no `StartNewRun`; [Intro], ←/→ y LB/RB solo si `AcceptsMainInput`; [Esc] cierra la confirmación; al empezar selecciona NUEVA PARTIDA en el `EventSystem`.
- [ ] **Step 5:** `MenuScreenBuilder`: columna en x = −670: NUEVA PARTIDA (y −50, 420×74), CONTINUAR (y −134, 420×64) con detalle debajo (24 px, gris), CONFIGURACIÓN (y −218), SALIR (y −302); panel de Configuración centrado 900×620 con título, 3 filas de slider (`Slider` construido por código: Background, Fill Area/Fill, Handle Slide Area/Handle), 3 botones de calidad, botón de pantalla completa y VOLVER [Esc]; panel de confirmación 760×260 con SÍ / NO; `UISounds` en el Canvas; navegación explícita arriba/abajo entre los botones.
- [ ] **Step 6:** Run `bash Tests/run.sh MenuStateTest` → pasa; `bash Tests/run_all.sh` → 0 fallos.
- [ ] **Step 7: Commit** — dos commits: "Menú: Continuar, Configuración y confirmación al sobrescribir la partida" y "Menú: panel de configuración con volúmenes, calidad y pantalla completa".

### Task 7: Guía, revisión independiente y entrega

**Files:**
- Create: `docs/guias/mejoras/GUIA_MEJORAS_ETAPA12.md`

- [ ] **Step 1:** Guía: cómo construir y probar; **jerarquía del menú** (Canvas → Fondo, Botones/…, PanelConfiguracion/…, PanelConfirmacion/…, EventSystem; qué componente va en cada objeto y qué campo enlaza con qué); parámetros (volúmenes por defecto, qué cambia cada calidad, dónde está el archivo de guardado y cuándo se guarda/borra); cómo desactivar "Empezar Play por el menú".
- [ ] **Step 2:** Revisión independiente de todo el cambio; aplicar los arreglos con sus pruebas.
- [ ] **Step 3:** Run `bash Tests/run_all.sh` en una copia limpia del repositorio después de cada commit → compilación OK, Roslyn OK, 0 fallos.
- [ ] **Step 4: Commit** — "Docs: guía de la Etapa 12 con el menú principal, la jerarquía y la configuración"; entregar en la carpeta de Samuel y recordar `git push`.
