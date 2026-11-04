# Etapa 7 — Pulido de pendientes: plan de implementación

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans (método elegido por el usuario: directo + revisión por etapa). Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Resolver los «detalles menores pendientes» que anotaron las revisiones de las etapas 0 a 6, y dejar el repositorio ordenado, con las pruebas dentro, para subirlo a git.

**Architecture:** No se añaden sistemas nuevos. Cada arreglo lleva su prueba RED→GREEN con la misma técnica: lógica pura (o un método estático) en el componente y la prueba en Mono. Las pruebas y las definiciones simplificadas de Unity pasan del área temporal a la carpeta `Tests/` del repositorio, fuera de `Assets/` para que Unity no las compile.

**Tech Stack:** Unity 2022.3/6 · C# · Mono (`mcs`) + definiciones simplificadas de Unity.

**Spec:** `docs/superpowers/specs/2026-10-19-nexus7-mejoras-design.md` y los registros de decisiones de las etapas 0 a 6 (las líneas `minor (deferred)`).

## Global Constraints

- Las mismas restricciones de las etapas anteriores.
- **Commits:** solo a nombre del usuario. Ningún mensaje lleva líneas de coautoría (preferencia del usuario).
- **Repositorio:** el código en `Assets/_Nexus7/Scripts/`, las guías en `docs/guias/`, los planes en `docs/superpowers/` y las pruebas en `Tests/`. No se suben zips (`.gitignore`).
- Las suites existentes (417 comprobaciones) deben seguir pasando.

## Review Focus

1. **Cambiar de cámara con un objeto en la mano.** En 1ª persona se ven el objeto y el antebrazo. En 3ª persona, el cuerpo entero, y los brillos no proyectan sombra. Task 2: `ShadowModeFor`.
2. **Robot aturdido por un PEM.** Muestra la postura de aturdido y, al recuperarse, vuelve a la normal. Task 1.
3. **Volver a jugar una escena con la selección guardada de otra partida.** Si el objeto ya no está, se selecciona la primera casilla y no hay error. Task 3: `HotbarModel.SelectItem`.
4. **Mando en el menú:** LB/RB cambian la dificultad sin salirse de los límites. Task 3: `NextDifficulty`.
5. **Pruebas en el repositorio:** se ejecutan desde una copia limpia con un solo comando. Task 6: `Tests/run_all.sh`.

---

### Task 1: Robots

- `Characters/ProceduralAnimator.cs`: añadir `public bool Stunned => stunned;`.
- `Enemies/EnemyPatrol.cs`:
  - busca su `ProceduralAnimator` en `Awake`;
  - `Stun` llama a `SetStunned(true)`, y la recuperación y `ResetToStart` llaman a `SetStunned(false)`;
  - nuevo `private void ApplyChaseSpeed()`, que se llama en cada frame de la persecución: `agent.speed = Speed(chaseSpeed)`, para que la velocidad siga a la alerta aunque cambie a mitad de una persecución.
- Test `$D/PolishTest.cs`:
  - `Stun(2)` deja `Stunned` en true;
  - tras `ResetToStart`, queda en false;
  - con `AlertSystem.ForceLevel(Red)` y `ApplyChaseSpeed` en la persecución, `agent.speed == chaseSpeed × Profile(Red).speed`.

### Task 2: Jugador (vista y sensación)

- `Characters/GaitModel.cs`: la frecuencia pasa a `(0.8 + 0.45·v)·π` rad/s.
  - Test: andando a 3,2 m/s, entre 0,9 y 1,4 ciclos/s; corriendo a 6 m/s, entre 1,5 y 2,2.
- `Player/JumpModel.cs`:
  - `LandingNoiseRadius`: menos de 0,5 m = 0 (los escalones de 0,3 m ya no hacen ruido), hasta 2 m = 6 y más = 9;
  - `public static float AfterCeilingHit(float vy, bool hitAbove)`: si `hitAbove && vy > 0`, devuelve 0;
  - `PlayerController` lo aplica con `CollisionFlags.Above`.
- `Player/CameraModeSwitcher.cs`:
  - `public static ShadowCastingMode ShadowModeFor(bool showFull, bool firstPersonPart, ShadowCastingMode original)`:
    - si es visible (`showFull || firstPersonPart`), devuelve `original`, con lo que los brillos se quedan en Off;
    - si no, devuelve `ShadowsOnly`.
  - `showFull = tercera persona efectiva || CinematicDirector.IsPlaying`: durante una cinemática se ve el cuerpo entero.
  - Las piezas bajo el objeto en la mano (raíz «ObjetoEnMano» de `HeldItemView`) cuentan como de 1ª persona.
  - El modo original de cada renderer se guarda la primera vez que se ve.
  - Cambiar de cámara con el mando pasa a `JoystickButton9` (R3), porque View ya abre el inventario.
- `Characters/ProceduralAnimator.cs`: `SetFirstPersonArms(bool)`. En 1ª persona, el brazo derecho se adelanta (brazo −55°, antebrazo −35°) para que la mano y el objeto se vean. `PlayerRigDriver` lo actualiza con el modo efectivo del `CameraModeSwitcher`.
- Test `$D/PolishTest.cs`:
  - frecuencias de la marcha;
  - ruido de aterrizaje: 0,45 m da 0 y 1 m da 6;
  - `AfterCeilingHit`;
  - `ShadowModeFor` en 4 casos;
  - el campo `padToggle` vale `JoystickButton9` por defecto;
  - `AttackPose` y el brazo de 1ª persona: pura `GaitModel.FirstPersonRightArm()` devuelve (−55, −35).

### Task 3: Interfaz y controles

- `UI/InventoryUI.cs`: se cierra con `PlayerHealth.PlayerHit`.
- `UI/WorkbenchIndicatorUI.cs`: el texto pasa a «BANCO DE TRABAJO cerca».
- `UI/MainMenuUI.cs`:
  - `public static Difficulty NextDifficulty(Difficulty d, int dir)` (con límites);
  - LB/RB (`JoystickButton4`/`5`) cambian la dificultad igual que ←/→.
- **Barra rápida guardada entre escenas:**
  - `Core/GameSession.cs` añade `HotbarSelectedId` (string, se borra con `NewRun` y `Clear`);
  - `Inventory/HotbarModel.cs` añade `public bool SelectItemById(string id)`;
  - `Hotbar` guarda la selección al cambiar y la restaura en `Start`.
- **HUD durante las cinemáticas:** `AlertHUD` y `CountdownHUD` ocultan sus textos y su barra mientras `CinematicDirector.IsPlaying`.
- **Derrota por muerte:** `CinematicScripts.DerrotaMuerte()` (7 s, mismo plano). IA: «Constantes vitales perdidas...» (0,3; 2,4); Tú: «Lo siento... no lo he conseguido.» (3,0; 2,4). `EndingManager` gana el campo `deathCinematic`, que se usa cuando no es por tiempo.
- **Fase 1:** se añade `DeviceQuickbar` al jugador («no se puede usar» con la chatarra) y las notificaciones de recogida. Para eso se extrae `A.BuildPickupNotifications` de `BuildPhase2UI`.
- **Fase 5:** la intro añade el mensaje «IA NEXUS-7: Los robots de élite llevan hachas. En Alerta Roja te cazarán: rompe su línea de visión.»
- Test `$D/PolishTest.cs`:
  - `NextDifficulty` en los límites;
  - `SelectItemById`: si existe, se selecciona; si no existe, devuelve false y la selección no cambia;
  - `NewRun` borra `HotbarSelectedId`;
  - textos exactos de `DerrotaMuerte`, que cabe en 7 s;
  - el texto del indicador del banco.

### Task 4: Higiene del código

- **Constructores:** se quitan las llamadas `SetObjectArray(quickbar, "slots", …)` de las fases 2, 4, 5 y 6.
- **Código sin uso:** se quitan `C5.EliteDrone`, `L4.ResearchDrone` y `A.Drone`, y los comentarios que aún dicen «drones» en los constructores.
- `DeviceQuickbar`: el kit cura `device.EffectValue` (40 si es 0).
- `DifficultySettings.Load` se ejecuta en `BeforeSceneLoad`.
- `RigMaterials.Get`: si no hay material, usa uno gris por defecto creado una sola vez, en lugar del rosa de material nulo.
- **Botín y objeto en la mano:**
  - `RoomLootSpawner`: la chatarra hace ruido al recogerla (3 m); los demás objetos, 0. Su collider visual se desactiva antes de destruirlo.
  - `HeldItemView`: destruye sus materiales al reconstruirse.
- `AlmacenCargaBuilder`: `AssetDatabase.SaveAssets()` tras crear la tabla.
- Test `$D/PolishTest.cs`:
  - el kit cura el valor del objeto (asset con 25 → +25);
  - `RoomLootSpawner.PickupNoiseFor(Part) == 3` y `(Battery) == 0`;
  - `RigMaterials.Get` sin materiales no devuelve null.

### Task 5: Nivel y guías

- **Puntos de botín** que pisaban rutas de patrulla:
  - Fase 3: (−6, 24) → (−4, 28) y (6, 27) → (4, 28);
  - Fase 4: (2, 13) → (1, 11.2) y (6.3, 22.4) → (5, 19.5);
  - Fase 5: (−5, 7) → (−5, 5.6) y (5, 6.5) → (5, 5.6);
  - Fase 6: (±5, 30) → (±5, 31.5).
- **Guías antiguas** (`docs/guias/fases/GUIA_FASE2–6.md`): una nota al principio: «Nota: desde las mejoras (etapas 0–7) los enemigos son robots armados y el botín es aleatorio; consulta `docs/guias/mejoras/`».
- **Script de plano:** cada punto movido queda a 1,8 m o más de su ruta y fuera de los obstáculos ya comprobados en su etapa.

### Task 6: Pruebas dentro del repositorio

- `Tests/Stubs/` contiene `UnityStubs.cs` y `Phase6Stubs.cs`; `Tests/Suites/` contiene los 31 archivos `*Test.cs`.
- `Tests/run.sh <Suite>` y `Tests/run_all.sh` usan rutas relativas a la raíz del repositorio (sin `/tmp` fijo; la salida va a `Tests/bin/`, que se ignora en git).
- `Tests/README.md` explica cómo ejecutarlas (Mono `mcs`/`mono`, o WSL en Windows).
- Las pruebas que leían archivos con ruta absoluta pasan a usar una ruta relativa a la raíz.
- El README principal enlaza `Tests/`.
- Verificación: copiar el repositorio a un directorio limpio y ejecutar `Tests/run_all.sh` → 0 FAIL.

### Task 7: Cierre

Revisión independiente (opus), correcciones con TDD, verificación, copia ordenada a la carpeta del usuario (`Assets/`, `docs/` y `Tests/`, sin zips en el repositorio) y `docs/guias/mejoras/GUIA_MEJORAS_ETAPA7.md`.
