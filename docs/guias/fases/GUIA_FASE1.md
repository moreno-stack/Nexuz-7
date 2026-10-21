# Escape Espacial: Nexus-7 — Fase 1 (Sala inicial)

Motor: **Unity 2022.3 LTS o Unity 6**, plantilla **3D (Built-in)** o **3D (URP)**. Lenguaje: C#.

---

## 1. Estructura de carpetas

```
Assets/
└── _Nexus7/                       ← el "_" la mantiene arriba en el panel Project
    ├── Scripts/
    │   ├── Core/          NoiseSystem.cs            (bus global de ruido)
    │   ├── Player/        PlayerController.cs       (movimiento, correr, agacharse, ruido)
    │   │                  PlayerLook.cs             (cámara en 1ª persona)
    │   ├── Interaction/   IInteractable.cs          (contrato)
    │   │                  Interactable.cs           (clase base)
    │   │                  PlayerInteractor.cs       (raycast + tecla E)
    │   │                  DoorController.cs         (puerta corredera bloqueable)
    │   │                  AccessTerminal.cs         (desbloquea la puerta)
    │   │                  InteractableMessage.cs    (bitácoras / carteles)
    │   ├── Environment/   EmergencyLight.cs         (luces parpadeantes / alarma)
    │   ├── UI/            HUDManager.cs             (prompt, mensajes, pistas, objetivo, fundido)
    │   ├── Tutorial/      TutorialManager.cs        (secuencia de despertar + tutorial)
    │   │                  TutorialTrigger.cs        (zonas que lanzan mensajes)
    │   └── Editor/        SalaInicialBuilder.cs     (construye la escena automáticamente)
    ├── Prefabs/      (Player, PuertaCorredera, TerminalMantenimiento, HUD — los genera el builder)
    ├── Scenes/       (SalaInicial.unity — la genera el builder)
    ├── Materials/    (los genera el builder)
    ├── UI/           (fuentes, iconos de la interfaz)
    └── Audio/        (pasos, puerta, alarmas)
```

Cada carpeta tiene un namespace propio (`Nexus7.Player`, `Nexus7.Interaction`, …) para que en fases
futuras se pueda separar en Assembly Definitions sin reescribir código.

---

## 2. Puesta en marcha (5 minutos)

1. **Unity Hub → New project →** plantilla *3D (URP)* o *3D (Built-in)*. Créalo donde quieras.
2. Copia la carpeta `Assets/_Nexus7` de aquí dentro de la carpeta `Assets` de tu proyecto.
3. **Edit → Project Settings → Player → Other Settings → Active Input Handling** = **Both**
   (o *Input Manager (Old)*). Los scripts usan el Input Manager clásico; si está en
   *Input System Package (New)* solamente, Unity lanzará errores al pulsar Play. Unity se reiniciará.
4. **Window → TextMeshPro → Import TMP Essential Resources** (si Unity no lo pide solo).
5. Menú superior **Nexus-7 → Fase 1 → Construir Sala Inicial**.
6. Pulsa **Play**.

El builder crea la escena `Assets/_Nexus7/Scenes/SalaInicial.unity`, los materiales, los prefabs
y la añade a *Build Settings*. Puedes volver a ejecutarlo cuando quieras para regenerarla.

---

## 3. Qué contiene la escena

| Objeto | Componentes | Función |
|---|---|---|
| **Entorno** | Cubos (suelo, techo, paredes, tuberías) | Corredor de 4 × 3 × 20 m + vestíbulo final |
| `CapsulaCriogenica` | Cubo + cristal emisivo | Donde despierta el jugador |
| `LuzRoja` ×4 | Light (Point) + **EmergencyLight** | Luces rojas parpadeantes; la del centro pulsa como alarma |
| `Bitacora` | **InteractableMessage** | Nota en la pared: historia + práctica de la tecla E |
| `TerminalMantenimiento` | **AccessTerminal** | Desbloquea la puerta y actualiza el objetivo |
| `PuertaC7` | **DoorController** + AudioSource | Puerta bloqueada (indicador rojo → verde) |
| `Trigger_Terminal`, `Trigger_FinFase1` | BoxCollider (Trigger) + **TutorialTrigger** | Mensajes de la IA al pasar |
| `Player` (Tag *Player*) | CharacterController + **PlayerController** + **PlayerLook** + AudioSource | Jugador |
| └ `CameraRoot/PlayerCamera` | Camera + AudioListener + **PlayerInteractor** | Vista y raycast de interacción |
| `HUD` | Canvas + **HUDManager** | Mira, fundido, textos |
| `GameSystems` | **TutorialManager** | Secuencia de despertar y tutorial |

La atmósfera se logra con: luz ambiental casi negra, sin skybox, niebla exponencial oscura y solo luces
rojas (con sombras suaves) como fuente principal.

---

## 4. Montaje manual (si prefieres no usar el builder)

1. **Jugador:** GameObject vacío `Player` (Tag *Player*) en el suelo → añade `CharacterController`
   (Height 1.8, Radius 0.35, Center Y 0.9), `PlayerController`, `PlayerLook`, `AudioSource`.
   Hijo `CameraRoot` en (0, 1.6, 0) → hijo `PlayerCamera` (Tag *MainCamera*) con `Camera`,
   `AudioListener` y `PlayerInteractor`. Arrastra `CameraRoot` al campo *Camera Root* de
   PlayerController y PlayerLook, y `Player` al campo *Player* de PlayerLook y PlayerInteractor.
2. **HUD:** Canvas (Screen Space – Overlay, Canvas Scaler 1920×1080). Hijos: Image negra a pantalla
   completa (fundido) y 4 textos TextMeshPro (interacción, mensajes, tutorial, objetivo). Añade
   `HUDManager` al Canvas y arrastra cada elemento a su campo.
3. **Puerta:** GameObject `Puerta` con `DoorController`; hijo con collider = panel móvil → campo
   *Door Panel*. Marca *Start Locked*.
4. **Terminal:** objeto con collider + `AccessTerminal`; arrastra la puerta a *Target Door*.
5. **Luces:** Point Light roja + `EmergencyLight`.
6. **Tutorial:** GameObject vacío con `TutorialManager`; arrastra Player, PlayerCamera (interactor) y HUD.

---

## 5. Controles

| Acción | Teclado / ratón | Mando (Xbox) |
|---|---|---|
| Moverse | W A S D | Stick izquierdo |
| Mirar | Ratón | Stick derecho* |
| Correr | Shift izquierdo (mantener) | L3 |
| Agacharse (alternar) | Ctrl izquierdo o C | B |
| Interactuar | E | A |
| Liberar cursor | Esc | — |

\* Para el stick derecho: crea en *Project Settings → Input Manager* los ejes `LookX` (Joystick Axis
4ª) y `LookY` (5ª, invertido) y activa *Use Gamepad Look* en PlayerLook.

---

## 6. Sigilo: cómo funciona el ruido

`PlayerController` emite un evento de ruido en **cada paso** mediante `NoiseSystem.Emit(posición, radio)`:

| Estado | Velocidad | Radio de ruido | Intervalo de pasos |
|---|---|---|---|
| Agachado | 1.5 m/s | 1.5 m | 0.75 s |
| Caminando | 3.2 m/s | 6 m | 0.5 s |
| Corriendo | 6 m/s | 12 m | 0.32 s |
| Quieto | 0 | 0 | — |

Al seleccionar al jugador en la vista *Scene* durante Play verás una esfera naranja con el radio actual.
En la Fase 2 los enemigos solo tendrán que suscribirse:

```csharp
NoiseSystem.NoiseEmitted += (pos, radio, fuente) =>
{
    if (Vector3.Distance(transform.position, pos) <= radio) Investigar(pos);
};
```

Agacharse además reduce la altura de la cápsula (se puede pasar bajo obstáculos) y comprueba con un
`SphereCast` que haya espacio antes de dejarte levantar.

---

## 7. Flujo de la Fase 1

1. Pantalla negra → la IA NEXUS-7 explica la situación (controles bloqueados).
2. Fundido de entrada; se devuelve el control.
3. Tutorial guiado: moverse → correr → agacharse → interactuar (cada paso espera a que lo hagas).
4. Objetivo: abrir la puerta. Si la usas: «ACCESO DENEGADO».
5. La bitácora da la pista; al pasar junto a la terminal, la IA la señala.
6. Usas la terminal → la puerta pasa a verde → la abres → vestíbulo → «FIN DE LA FASE 1».

---

## 8. Ampliar

- **Nuevo objeto interactuable:** hereda de `Interactable` e implementa `OnInteract()`.
- **Sonidos:** arrastra clips a *Footstep Clips* (PlayerController) y a *Open/Denied/Unlock Clip* (DoorController).
- **Probar rápido sin intro:** desmarca *Play Intro* en `GameSystems → TutorialManager`.
- **Textos:** todos los mensajes son editables desde el Inspector (no hace falta tocar código).
