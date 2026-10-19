# Escape Espacial: Nexus-7 — Mejoras de jugabilidad y presentación (diseño)

Fecha: 2026-10-19 · Estado: aprobado en conversación, pendiente de revisión escrita

## 1. Objetivo

Hacer que el juego se sienta más completo sin cambiar su género (sigilo):

- personajes redondeados y animados, no cajas;
- controles completos (salto, barra rápida 1–9, cámara 1ª/3ª persona);
- robots armados que golpean y quitan vida;
- traje espacial con escudo;
- tres niveles de dificultad;
- objetos al azar con un banco de trabajo en cada sala;
- cinemáticas con subtítulos en español.

Se implementa **fase por fase**. En cada etapa se aplican las skills de pruebas, revisión de código y verificación antes de entregarla.

### Decisiones del usuario

| Tema | Decisión |
|---|---|
| Tipo de juego | Sigilo con peligro: los robots golpean y tú no peleas |
| Modelos | Generados por código: primitivas redondeadas con animación procedural |
| Cámara | Se cambia entre primera y tercera persona con [V] |
| Fabricación | Objetos al azar en cada sala y un banco de trabajo por sala |
| Traje | Escudo recargable; se recoge en la Fase 1 |
| Dificultad | Fácil, Normal o Difícil; el daño base es el de Fácil y sube con la dificultad |
| Cinemáticas | Con subtítulos en español y la opción de saltarlas |

## 2. Experiencia de juego

### 2.1 Controles

| Entrada | Acción | Notas |
|---|---|---|
| WASD / stick izquierdo | Moverse | Sin cambios |
| Shift / L3 | Correr | Ruido de radio 12 |
| Ctrl o C / B | Agacharse | Agachado no se puede saltar |
| **Espacio / A del mando** | **Saltar** | Altura de unos 1,1 m. Al aterrizar hace ruido de radio 6; si se cae desde más de 2 m, el ruido es de radio 9 |
| **1–9** | **Seleccionar una casilla de la barra rápida** | La barra muestra las primeras 9 pilas del inventario, por orden de llegada |
| Rueda del ratón / LB-RB | Casilla anterior o siguiente | |
| **G o clic izquierdo** | Usar el objeto seleccionado | Solo hace algo con los dispositivos (lanzar o activar); con el resto muestra «No se puede usar» |
| **V / Select** | Cambiar entre 1ª y 3ª persona | La preferencia se guarda |
| E / X | Interactuar | En 3ª persona, el rayo sale de la cámara, pero el objetivo debe estar a 2,5 m o menos del jugador |
| Tab o I | Inventario | Sin cambios |
| Mantener Espacio (1 s) | Saltar la cinemática | Solo durante las cinemáticas |

El botón A del mando sigue sirviendo también para interactuar en los menús. En el juego, el salto pasa al botón A y la interacción al botón X.

### 2.2 Personajes

- **Jugador (`PlayerRig`).** Torso y caderas de cápsulas, cabeza esférica, brazos y piernas de cápsulas con codos y rodillas, y manos esféricas. Hay dos variantes:
  - **sin traje:** mono naranja y cabeza descubierta;
  - **con traje:** blanco, con casco de visor emisivo azul y mochila con luces.

  En 3ª persona se ve el cuerpo entero. En 1ª persona solo se ven los antebrazos y el objeto que lleva en la mano.
- **Robots (`RobotRig`).** Torso redondeado, cabeza con un ojo emisivo cuyo color depende de la detección, brazos articulados y dos piernas. Cada tipo tiene su forma, color y arma:

  | Tipo | Aspecto | Arma (`WeaponData`) | Daño base (Fácil) | Anticipación | Alcance | Recuperación |
  |---|---|---|---|---|---|---|
  | Seguridad (Fases 2–6) | Gris, piernas | Bate metálico | 25 | 0,55 s | 1,9 m | 1,2 s |
  | Guardia (Fase 3) | Amarillo, robusto | Llave inglesa gigante | 25 | 0,6 s | 2,0 m | 1,3 s |
  | Investigación (Fase 4) | Blanco, visor morado | Porra eléctrica (+ aturde 1,0 s) | 15 | 0,45 s | 1,8 m | 1,0 s |
  | Élite (Fases 5–6) | Negro y rojo, blindado | Hacha | 40 | 0,7 s | 2,2 m | 1,5 s |

- **Animación procedural (`ProceduralAnimator`).** Ciclo de caminar y correr (oscilación de piernas y brazos según la velocidad), postura agachada, salto (piernas recogidas), balanceo del torso y golpe (levantar el arma → golpear → volver). Todo se hace rotando articulaciones por nombre; no hace falta ningún Animator ni assets.

### 2.3 Combate (solo los robots atacan)

- `EnemyPatrol` deja de hacer «captura instantánea». Al llegar al alcance del arma mientras persigue, pasa al estado **Ataque**:
  1. **Anticipación:** el robot se detiene, levanta el arma y su ojo parpadea en rojo.
  2. **Golpe:** se comprueba un arco de 70° y el alcance del arma. Si el jugador sigue dentro, recibe el daño y un empujón de 3 m/s.
  3. **Recuperación:** después vuelve a perseguir.
- Si el jugador se aleja durante la anticipación, **esquiva** el golpe.
- Los dispositivos siguen funcionando igual: el PEM aturde y cancela el ataque, y el señuelo distrae.
- Con la vida a 0, el jugador queda **fuera de combate**:
  - se muestra el mensaje «Fuera de combate», se funde a negro, se reaparece en el último punto de control y se rellenan la vida y el escudo;
  - cuenta como una captura en `AlertSystem.Stats.captures` y como una muerte en `RunStats`;
  - en la Fase 6 (`respawnOnDeath = false`) supone el final crítico, como hasta ahora.
- Los peligros eléctricos ya hacían daño. Ahora se aplica primero el escudo y luego la vida.

### 2.4 Traje espacial y escudo

- **`SuitPickup`** está en la taquilla de la sala inicial (Fase 1). Al interactuar se reproduce una animación corta (fundido y sonido), el modelo cambia a la versión con traje y se activa el escudo.
- La puerta de la Fase 1 no se abre hasta tener el traje y haber usado la terminal. Si lo intentas sin traje, aparece el mensaje: «IA NEXUS-7: Sin traje no sobrevivirás fuera de esta sala».
- **Escudo (`SuitShield`).**
  - Capacidad de 50.
  - Absorbe el daño antes que la vida; el daño sobrante pasa a la vida.
  - Se recarga a 8/s después de 6 s sin recibir daño; la velocidad de recarga depende de la dificultad.
  - Al recibir un golpe, el traje muestra un destello azul (emisión).
  - El HUD muestra una barra azul de escudo encima de la barra verde de vida.
- El traje se conserva entre escenas: `GameSession.HasSuit`. Si se juega una escena suelta (no la 1), se empieza con el traje puesto.

### 2.5 Dificultad

| Dificultad | Daño de los robots y peligros | Recarga del escudo | Detección | Multiplicador de puntuación |
|---|---|---|---|---|
| Fácil | ×1,0 | ×1,0 | ×1,0 | ×1,0 |
| Normal | ×1,5 | ×0,8 | ×1,15 | ×1,15 |
| Difícil | ×2,0 | ×0,6 | ×1,3 | ×1,3 |

- Se elige en el menú principal con un selector de 3 botones y el valor por defecto es Fácil. Se guarda en `PlayerPrefs`.
- Se muestra en la pantalla de resultados («Dificultad: Normal ×1,15»).
- La detección se multiplica dentro de `EnemyVision`, junto con el perfil de alerta.
- La puntuación se multiplica en `ScoreCalculator` (campo nuevo `difficultyMultiplier`). Los rangos se calculan sobre el total ya multiplicado.

### 2.6 Objetos al azar y bancos de trabajo

- **`LootTable` (ScriptableObject):** entradas con `item`, `weight`, `minAmount` y `maxAmount`.
- **`RoomLootSpawner`:** tiene una lista de `LootSpawnPoint` (posición, tipo de soporte y una probabilidad de quedar vacío) y una lista de **objetos garantizados**.
  - Al iniciar la escena, coloca primero los garantizados en puntos al azar y rellena el resto de puntos según la tabla.
  - La semilla cambia en cada partida (`GameSession.RunSeed`), pero se mantiene al reaparecer, así que los objetos recogidos no vuelven.
  - Tiene un modo `fixedSeed` para las pruebas.
- **Garantizados por sala**, para que la progresión no se rompa nunca:

  | Fase | Objetos garantizados |
  |---|---|
  | 2 | 3 baterías y la llave hidráulica |
  | 3 | 2 baterías y 2 circuitos |
  | 4 | 1 cristal óptico y chatarra suficiente para un señuelo |
  | 5 | 1 circuito |
  | 6 | 1 batería |

- Los contenedores (`LootContainer`) existentes se quedan. Los garantizados pueden caer en un contenedor o en el suelo.
- **Banco de trabajo por sala (Fases 2–6).** Lleva un cartel luminoso «BANCO DE TRABAJO» y un icono en el HUD cuando está cerca. Las recetas se desbloquean por fase:

  | Fase | Recetas |
  |---|---|
  | 2 | Señuelo |
  | 3 | Señuelo y kit de reparación (nuevo: +40 de vida) |
  | 4 en adelante | Señuelo, PEM, camuflaje y kit de reparación |

### 2.7 Cinemáticas

- **`CinematicDirector`** reproduce una lista de **planos**. Cada plano tiene posición y objetivo de la cámara (inicial y final, con interpolación suave), duración, subtítulos (texto, quién habla y momento) y, opcionalmente, acciones (activar objetos, vibración, sonido).
- Durante la cinemática:
  - el jugador queda bloqueado, con un `AddInputLock` propio;
  - la IA de los robots se pausa y no hay detección;
  - aparecen franjas negras arriba y abajo;
  - los subtítulos se muestran con el nombre de quien habla en color.
- Para saltarla hay que **mantener Espacio** 1 s; un indicador muestra el progreso.
- Al terminar, la cámara vuelve suavemente al jugador.
- **`SubtitleUI`** muestra los subtítulos centrados abajo, con fondo semitransparente, a 30 caracteres por segundo.

| Cinemática | Escena | Contenido |
|---|---|---|
| Despertar | Fase 1 | Cápsula de hibernación, alarma, la IA explica el motín de los robots y la cámara muestra la taquilla del traje |
| Almacén | Fase 2 | Recorrido aéreo, un robot de seguridad con bate y el montacargas al fondo |
| Energía | Fase 3 | El reactor apagado, un guardia con llave inglesa y las barreras |
| Laboratorio | Fase 4 | Tanques, un dron de investigación con porra y el banco de trabajo |
| Centro de Control | Fase 5 | El holograma, la pareja de élite con hachas y la compuerta blindada |
| Autodestrucción | Fase 6 | El hangar, la cuenta atrás que se activa, las cápsulas A, B y C |
| Despegue | Final | Exterior: la cápsula se separa y la nave explota |
| Derrota | Final | La nave se desintegra contigo dentro |

## 3. Arquitectura

### 3.1 Componentes nuevos

| Componente | Carpeta | Responsabilidad | Depende de |
|---|---|---|---|
| `DifficultySettings` (static) | Core | Dificultad actual, multiplicadores y guardado en PlayerPrefs | — |
| `SuitShield` | Player | Escudo: absorber, recargar y evento `ShieldHit` | DifficultySettings |
| `PlayerHealth` (ampliado) | Player | `TakeDamage(float, Vector3 origen)`: pasa primero por el escudo, aplica el empujón y deja de usar la reaparición propia | SuitShield, PlayerRespawn |
| `PlayerController` (ampliado) | Player | Salto, ruido al aterrizar, empujón (`AddImpulse`) e `IsGrounded` | NoiseSystem |
| `CameraModeSwitcher` | Player | Alterna 1ª/3ª persona, mueve la cámara y oculta o muestra el cuerpo | ThirdPersonCamera |
| `ThirdPersonCamera` | Player | Brazo de cámara de 3,2 m a la altura del hombro, con SphereCast contra paredes | — |
| `Hotbar` | Inventory | Casillas 1–9 sobre `InventorySystem`, casilla seleccionada y evento `SelectionChanged` | InventorySystem |
| `HeldItemView` | Player | Muestra la forma del objeto seleccionado en el `HandSocket` | Hotbar |
| `DeviceQuickbar` (adaptado) | Devices | Usa el dispositivo de la casilla seleccionada y deja de gestionar sus propias casillas | Hotbar |
| `CharacterRigFactory` | Editor + Runtime | Crea las jerarquías `PlayerRig` y `RobotRig` con primitivas redondeadas y nombres de articulación estándar | — |
| `ProceduralAnimator` | Characters | Anima las articulaciones según la velocidad y el estado (caminar, correr, agacharse, saltar, golpear, aturdido) | — |
| `WeaponData` (ScriptableObject) | Enemies | Nombre, daño, anticipación, alcance, arco, recuperación, aturdimiento y forma del arma | — |
| `EnemyMeleeAttack` | Enemies | Máquina de estados del golpe y del esquive | WeaponData, PlayerHealth, ProceduralAnimator |
| `LootTable`, `LootSpawnPoint`, `RoomLootSpawner` | Inventory | Objetos aleatorios con los garantizados | GameSession.RunSeed, PickupItem |
| `SuitPickup` | Interaction | Interactuable que pone el traje | GameSession, CameraModeSwitcher |
| `CinematicDirector`, `CinematicShot` | Cinematics | Planos, subtítulos, franjas negras, saltar y bloqueos | HUD, PlayerController |
| `SubtitleUI` | UI | Subtítulos con nombre de quien habla y efecto de escritura | — |
| `ShieldHealthHUD` | UI | Barras de escudo y vida (sustituye a `HealthUI` desde la Fase 2) | PlayerHealth, SuitShield |
| `HotbarUI` | UI | 9 casillas con icono de texto, cantidad y selección | Hotbar |

### 3.2 Cambios en sistemas existentes

- **`EnemyPatrol`:** el estado `Chase` llama a `EnemyMeleeAttack.TryAttack()` al llegar al alcance, en lugar de `PlayerRespawn.Caught()`. Si el robot no tiene `EnemyMeleeAttack`, mantiene la captura (compatibilidad).
- **`PlayerRespawn`:** la reaparición la provoca `PlayerHealth` (fuera de combate) o una caída al vacío. Se mantienen la gracia de 2,5 s, el reinicio de sombras, etc.
- **`AlertEventBridge`:** «Fuera de combate» cuenta como captura. Un golpe recibido también es un suceso de alerta (`PlayerHit`, +6 de tensión).
- **`ScoreCalculator`:** añade el multiplicador de dificultad. La creatividad no cambia.
- **`GameSession`:** añade `HasSuit` y `RunSeed`, junto a las casillas y la selección de la barra.
- **`GameFlow.StartNewRun`:** genera una `RunSeed` nueva y quita el traje.
- **`MainMenuUI`:** añade el selector de dificultad.
- **`EndingUI`:** muestra la dificultad y el multiplicador.
- **Constructores de las Fases 1–6:**
  - jugador con `PlayerRig`;
  - robots con `RobotRig` y arma;
  - `RoomLootSpawner` con sus puntos y garantizados;
  - banco de trabajo con cartel;
  - `CinematicDirector` con los planos de la fase;
  - HUD nuevo (escudo, vida, barra rápida y subtítulos).

  Las cajas «Cuerpo» de los drones actuales se sustituyen.

### 3.3 Flujo de daño

```
EnemyMeleeAttack (impacto) ─► PlayerHealth.TakeDamage(base × Difficulty.Damage, origen)
                                ├─► SuitShield.Absorb(d) → resto
                                ├─► vida -= resto · empujón · AlertSystem(PlayerHit) · RunStats.RecordDamage
                                └─► vida ≤ 0 ─► PlayerDied ─► (respawnOnDeath ? PlayerRespawn.Respawn("Fuera de combate") : EndingManager.Fail)
```

## 4. Errores y casos límite

- **Golpe durante una cinemática, un hackeo o una fabricación:** durante las cinemáticas los robots están pausados. Durante un hackeo o una fabricación sí puedes recibir golpes: el golpe cancela la interfaz abierta (reutiliza `PlayerCaught → Abort`).
- **Salto:**
  - no se puede saltar agachado, con un bloqueo de entrada activo o sin estar en el suelo;
  - hay un margen de 0,1 s tras dejar el borde (coyote time) para que el salto no falle al bajar una rampa.
- **Cámara en 3ª persona dentro de los conductos (techo de 1,2 m):** el SphereCast acerca la cámara. Si queda a menos de 0,6 m, se pasa temporalmente a 1ª persona.
- **Inventario con más de 9 pilas:** la barra muestra las 9 primeras; el resto se ve en el inventario (Tab).
- **Botín:** si una sala tiene menos puntos que objetos garantizados, el `RoomLootSpawner` avisa con un error en el editor y coloca los garantizados restantes en el primer punto.
- **Escenas sueltas:** si se juega una escena directamente, empiezas con el traje y el equipo de prueba de esa escena.

## 5. Pruebas

Con las mismas definiciones simplificadas de Unity de ahora (Mono).

| Archivo | Qué comprueba |
|---|---|
| `DamageTest` | Escudo absorbe → resto → vida; recarga tras 6 s; multiplicadores de dificultad; fuera de combate |
| `HotbarTest` | 1–9 sobre el inventario; la selección sigue a la pila cuando otras se vacían; rueda del ratón |
| `LootTest` | Con semilla fija sale siempre lo mismo; los garantizados se colocan siempre; los pesos se respetan en 10 000 tiradas; no se coloca más de una cosa en el mismo punto |
| `MeleeTest` | Anticipación → golpe; esquivar si el jugador sale del arco o del alcance; un PEM cancela; recuperación |
| `CinematicTest` | Tiempos de los planos, subtítulos en su momento, saltar a 1 s y bloqueos liberados al terminar |
| `ScoreTest` | Multiplicador de dificultad aplicado y rangos sobre el total multiplicado |

Las 103 comprobaciones actuales deben seguir pasando. Al final de cada etapa, un revisor independiente revisa la fase y se verifica antes de copiarla a la carpeta del usuario.

## 6. Etapas (fase por fase)

| Etapa | Contenido | Se entrega |
|---|---|---|
| **0. Base** | Todos los sistemas comunes de la sección 3.1 con sus pruebas (sin tocar todavía las escenas) | Scripts + pruebas |
| **1. Fase 1** | Jugador con modelo, salto, cámara V, barra rápida, taquilla del traje, cinemática del despertar, HUD nuevo | Constructor 1 actualizado |
| **2. Fase 2** | Robots de seguridad con bate, botín al azar, banco de trabajo, cinemática del almacén, vida y escudo activos | Constructor 2 |
| **3. Fase 3** | Guardias con llave inglesa, botín, banco con kit de reparación, cinemática | Constructor 3 |
| **4. Fase 4** | Drones de investigación con porra eléctrica, botín, banco, cinemática | Constructor 4 |
| **5. Fase 5** | Élite con hacha, botín, banco, cinemática | Constructor 5 |
| **6. Fase 6** | Élite y seguridad armados, botín, banco, cinemáticas de autodestrucción y de los finales, dificultad en menú y resultados | Constructor 6 + menú |

Cada etapa sigue este ciclo:

1. Pruebas primero.
2. Implementación.
3. Compilación y todas las pruebas.
4. Revisión de código independiente.
5. Correcciones.
6. Verificación.
7. Copia a `Nexuz-7 Juego` y actualización de su guía (`GUIA_MEJORAS_FASEN.md`).

## 7. Fuera de alcance

- Combate del jugador (no ataca).
- Modelos externos o del Asset Store.
- Voces grabadas: las cinemáticas tienen un campo opcional de audio, pero no se incluyen clips.
- Multijugador.
- Guardado a mitad de partida.
