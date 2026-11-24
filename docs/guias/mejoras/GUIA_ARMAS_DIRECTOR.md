# Escape Espacial: Nexus-7. Armas, director adaptativo y respawn dinámico

Esta guía explica el sistema de armas, la IA que analiza al jugador (director adaptativo) y la anti-repetición al caer: qué se encontró en el código, cómo está montado, cómo probar cada etapa y qué valores tocar para equilibrarlo.

No hace falta reconstruir las escenas. `CombatInstaller` añade el combate al jugador y a los robots al cargar cada fase, y el director y el respawn dinámico se enganchan solos al jugador. Basta con abrir el proyecto, esperar a que compile y pulsar **Play** en `MenuPrincipal`.

## 1. Análisis del repositorio (antes de los cambios)

| Pieza | Cómo estaba |
|---|---|
| Motor | Unity 6000.6.4f1 con URP y C#. Las 6 fases se generan por código desde `Editor/*Builder.cs`. |
| Bucle principal | No hay un game loop central. Cada sistema es un `MonoBehaviour` con su `Update` y se comunican por eventos estáticos: `PlayerRespawn.PlayerCaught/PlayerRespawned`, `PlayerHealth.PlayerDied`, `NoiseSystem`, `AlertSystem` y `EnemyPatrol.PlayerSpotted`. |
| Estado | `GameSession` (inventario, traje y semilla entre escenas), `RunStats` (estadísticas), `SaveSystem` (disco) y `DifficultySettings`. |
| Jugador | `PlayerController`, `PlayerHealth` con escudo y regeneración, `PlayerRespawn` (vuelve al último `Checkpoint`), inventario, barra rápida y dispositivos (`DeviceQuickbar`). |
| Enemigos | Robots con `EnemyPatrol` (una máquina de estados de 637 líneas), `EnemyVision` y `EnemyMeleeAttack`. **No tenían vida**: no se podían destruir. |
| Vidas | **No existen.** Al caer se reaparece sin límite. Las caídas cuentan en `RunStats.Deaths` y penalizan recursos. |
| Progresión | Seis escenas fijas en orden. La dificultad (Fácil, Normal o Difícil) solo multiplica el daño, la detección y el escudo. Es estática. |
| Spawns | Los robots se colocan en el constructor. Al caer, **todos vuelven exactamente a su posición y waypoint inicial** (`EnemyPatrol.ResetToStart`), así que cada intento es idéntico. |
| Colisiones | Física de Unity con `CharacterController` en el jugador, `CapsuleCollider` y NavMesh horneado en tiempo de ejecución en los robots, y rayos para la visión, el sonido y el cuerpo a cuerpo. |
| Armas del jugador | No había. Existían los diseños `docs/superpowers/specs/2026-11-11-combate-basico-design.md` e `ia-combate-design.md`, sin implementar. Esta implementación sigue sus valores para la pistola y el rifle, sus controles y el ruido de los disparos. |

Qué faltaba o convenía refactorizar:

- Faltaba un contrato de daño (`IDamageable`) y vida en los robots.
- Faltaba un bus de eventos de combate para que el HUD y la IA escuchen sin acoplarse a las armas.
- `ResetToStart` estaba cerrado: hacía falta un punto de enganche para variar la reaparición sin hacer crecer más `EnemyPatrol`.
- La dificultad era estática: hacía falta una capa de multiplicadores en tiempo real (`AdaptiveModifiers`) que los sistemas de siempre leyeran con valor 1 por defecto.

## 2. Arquitectura

```
                  ┌──────────── Combat/ ───────────────────────────────┐
 Entrada ──► PlayerWeapon ──► WeaponModel (puro) ──► ProjectilePool ──► PlasmaBolt ──► IDamageable
                │                 ▲  ArsenalModel, SpreadModel,               (SphereCast)   └ EnemyHealth (HealthModel)
                │                 │  PlayerWeaponCatalog (datos)
                └──── CombatEvents (ShotFired · HitLanded · EnemyKilled) ──┬──► CombatHUD
                                                                           └──► GameDirector
                  ┌──────────── Director/ ─────────────────────────────┐
 Eventos existentes (PlayerHealth, PlayerDied, PlayerSpotted) ──► GameDirector ──► PlayerPerformanceModel (puro)
                                                                    │            └► DirectorModel (puro): tensión → perillas
                                                                    └─► AdaptiveModifiers (Core) ◄── EnemyPatrol, EnemyVision, PlayerHealth
 PlayerCaught ──► RespawnVariator ──► SpawnVariationModel (puro) ──► EnemyPatrol.VariationProvider / refuerzos / cámaras
```

Principios:

- **Lógica pura y probada.** Todo lo que decide (armas, métricas, director, variación) son clases sin `UnityEngine` que se prueban fuera de Unity. Los `MonoBehaviour` son adaptadores finos.
- **Desacople por eventos.** Las armas no conocen al director, y el director no conoce las armas: los dos hablan con `CombatEvents`. Los sistemas antiguos solo leen `AdaptiveModifiers` y `EnemyPatrol.VariationProvider`, y sin director valen 1 o `null`, así que el juego se comporta como antes.
- **Sin coste de FPS.** El director actualiza cada 0,5 s con unas decenas de operaciones. Los proyectiles salen de un pool, así que no se crean ni se destruyen objetos en combate.
- **Instalación en tiempo de ejecución.** `CombatInstaller` funciona con las escenas ya construidas.

### Commits

| # | Commit | Qué contiene |
|---|---|---|
| 1 | `feat(combate): modelos puros de armas, dispersión, arsenal y vida` | `PlayerWeaponCatalog`, `WeaponModel`, `SpreadModel`, `ArsenalModel`, `HealthModel`, `DamageInfo`/`IDamageable` y `WeaponTest` |
| 2 | `feat(combate): disparo del jugador con proyectiles en pool` | `PlayerWeapon`, `ProjectilePool`, `PlasmaBolt`, `WeaponViewModel`, `CombatEvents`, sonidos, `CombatSession` y `DeviceQuickbar` sin clic izquierdo |
| 3 | `feat(combate): robots con vida, reacción a impactos y munición al destruirlos` | `EnemyHealth` y `AmmoPickup` |
| 4 | `feat(ui): HUD de combate e instalación automática del combate en cada fase` | `CombatHUD` y `CombatInstaller` |
| 5 | `feat(director): analizador de rendimiento del jugador y director adaptativo` | `PlayerPerformanceModel`, `DirectorModel` y `DirectorTest` |
| 6 | `feat(director): director adaptativo en juego con panel de métricas [F3]` | `GameDirector`, `DirectorSession`, `AdaptiveModifiers` y los enganches en `EnemyPatrol`, `EnemyVision` y `PlayerHealth` |
| 7 | `feat(director): modelo de variación de spawns con memoria anti-repetición` | `SpawnVariationModel`, `PatrolVariation` y `SpawnVariationTest` |
| 8 | `feat(director): respawn dinámico, refuerzos adaptativos y variación del entorno al caer` | `RespawnVariator` y los enganches en `EnemyPatrol`, `PatrolRoute`, `PlayerRespawn` y `SecurityCamera` |
| 9 | `fix(combate): la tecla que cierra un panel no cambia de arma ni dispara` | `PlayerController.InputReleasedThisFrame` |
| 10 | `docs: guía de armas, director adaptativo y respawn dinámico` | Esta guía y el README |

Segunda entrega (PR #2, rama `feat/combate-mando-guardado-robots-tiradores`):

| # | Commit | Qué contiene |
|---|---|---|
| 11 | `feat(guardado): armas, munición y tensión del director en la partida guardada` | `SaveData` (`weapons`, `weaponIndex`, tensión), `SaveSystem`, `CombatSession.ToSaved`/`RestoreSaved` |
| 12 | `feat(mando): armas y mira con mando (gatillos, cruceta y stick derecho)` | Ejes nuevos en `InputManager.asset`, `GamepadAxes`, `CombatInput` (`TriggerLatch`, `DpadEdges`) y los enganches en `PlayerWeapon`, `PlayerLook` y `CombatHUD` |
| 13 | `feat(combate): modelos de disparo de los robots (armas, puntería, turnos, ráfagas, tambaleo)` | `EnemyGunCatalog`, `AimModel`, `AttackTokens`, `BurstModel`, `StaggerModel`, `DamageIndicatorMath` y `EnemyFireTest` |
| 14 | `feat(combate): robots de seguridad, guardia y élite disparan a distancia` | `EnemyGun`, `PlayerTarget`, `PlasmaBolt` hostil, `PlayerHealth.TakeRangedDamage`, indicador de daño, perillas nuevas del director |
| 15 | `docs: guía de mando, robots tiradores y guardado de armas` | Esta guía y el README |

Tercera entrega (PR #3, rama `feat/combate-tactico-coberturas`):

| # | Commit | Qué contiene |
|---|---|---|
| 16 | `feat(combate): modelos de coberturas y tácticas por tipo de robot` | `CoverScoring`, `CoverReservations`, `TacticsModel`, `TacticsCatalog` y `TacticalCoverTest` |
| 17 | `feat(combate): los robots tiradores se cubren, se asoman, avanzan y flanquean` | `CoverField`, `EnemyTactics`, `EnemyPatrol.ChaseDestination`, `EnemyGun.FireGate` y la perilla `TacticTempo` |
| 18 | `feat(combate): modelo de puntos débiles (óptica y núcleo trasero de la élite)` | `WeakPointModel` y `WeakPointTest` |
| 19 | `feat(combate): óptica que deslumbra y núcleo trasero de la élite en juego` | `EnemyHealth` (zonas y núcleo visible), `EnemyGun.Dazzle`, rótulos del HUD |
| 20 | `docs: guía de combate táctico y puntos débiles` | Esta guía y el README |

Cuarta entrega (PR #4, rama `feat/combate-oleadas-desarme`):

| # | Commit | Qué contiene |
|---|---|---|
| 21 | `feat(combate): modelos de retroceso al impacto y brazo del arma (desarme)` | `HitRecoilModel`, `WeaponArmModel`, zona `Arm` en `WeakPointModel` y pruebas en `WeakPointTest` |
| 22 | `feat(combate): retroceso al recibir impactos y desarme por el brazo del arma` | `ProceduralAnimator` (retroceso y brazo colgando), `EnemyHealth.Disarm`, `CombatEvents.EnemyDisarmed`, HUD |
| 23 | `feat(combate): modelos de encuentros por oleadas (catálogo, secuencia y puntos de aparición)` | `EncounterCatalog`, `EncounterTriggerRule`, `WaveModel`, `EncounterSpawnPicker` y `EncounterTest` |
| 24 | `feat(juego): aviso de objetivo cumplido en terminales, consolas y puntos seguros` | `ObjectiveEvents` y los enganches en `HackTerminal`, `AccessTerminal`, `PowerConsole`, `ConsoleUI`, `LaunchStepTerminal` y `Checkpoint` |
| 25 | `feat(combate): encuentros por oleadas activados por los objetivos de cada fase` | `EncounterDirector`, `CombatInstaller` y la línea del encuentro en F3 |
| 26 | `docs: guía de oleadas, desarme y retroceso` | Esta guía y el README |

Quinta entrega (PR #6, rama `feat/vidas-variantes-robot`):

| # | Commit | Qué contiene |
|---|---|---|
| 27 | `feat(vidas): modelo de vidas por fase y director clemente en la última vida` | `LivesModel`, `DirectorModel.LastLife` y `LivesTest` |
| 28 | `feat(vidas): vidas en juego, reinicio de fase sin repetir y vidas en el HUD` | `LivesSystem`, semillas de `RespawnVariator` y `EncounterDirector`, `GameSession`, HUD y F3 |
| 29 | `feat(director): modelo de variantes de robot (blindado, rápido, ojo de águila)` | `RobotAffixModel` y `AffixTest` |
| 30 | `feat(director): el director reparte variantes de robot en cada intento` | `RobotAffixDirector`, `RobotAffixState` y los enganches en `EnemyHealth`, `EnemyPatrol`, `EnemyVision` y `EnemyGun` |
| 31 | `docs: guía de vidas y variantes de robot` | Esta guía y el README |

Sexta entrega (PR #7, rama `feat/eventos-nave-informe-ia`):

| # | Commit | Qué contiene |
|---|---|---|
| 32 | `feat(director): modelo de eventos de nave (apagón, fuga de vapor, barrido de seguridad)` | `ShipEventModel` y `ShipEventTest` |
| 33 | `feat(director): eventos de nave en cada intento (apagón, vapor y barrido) con línea en F3` | `ShipEventDirector`, `BlackoutZone` y la línea en F3 |
| 34 | `feat(director): modelo del informe de la IA por fase (estadísticas, estilo y veredicto)` | `PhaseReportModel` y `PhaseReportTest` |
| 35 | `feat(director): informe de la IA al entrar en la fase siguiente (o tras reiniciarla)` | `PhaseReportPanel`, `DirectorSession` (informe pendiente), `GameFlow.PhaseIndex` y los registros en `GameDirector` |
| 36 | `feat(director): el panel F3 cuenta las luces atenuadas por el apagón` | `ShipEventDirector.Summary` |
| 37 | `docs: guía de eventos de nave e informe de la IA` | Esta guía y el README |

Séptima entrega (PR #8, rama `feat/accesorios-recompensas`):

| # | Commit | Qué contiene |
|---|---|---|
| 38 | `feat(armas): accesorios modulares (supresor, cargador ampliado, estabilizador, disipador)` | `WeaponModModel`, `PlayerWeaponSpec.Clone`, accesorios en `WeaponModel` y `WeaponModTest` |
| 39 | `feat(armas): accesorios en el arsenal, la partida guardada y el HUD` | `ArsenalModel.ModTarget`, `PlayerWeapon.GiveMod`, `CombatSession`, `SavedWeapon.mods` y `CombatHUD` |
| 40 | `feat(director): modelo de recompensas, el accesorio según cómo juegas` | `ModRewardModel` y `ModRewardTest` |
| 41 | `feat(director): módulos de recompensa al despejar encuentros o destruir élites` | `ModRewardDirector`, `ModPickup`, `EncounterDirector.Cleared`, F3 y el informe de la IA |
| 42 | `docs: guía de accesorios y recompensas` | Esta guía y el README |

## 3. Etapas: cómo probar cada una

Antes de cada etapa, ejecuta las pruebas desde la raíz con `bash Tests/run_all.sh`. El resultado esperado es `TOTAL: 1093 comprobaciones, 0 fallos` y `ROSLYN: compilación OK`.

### Etapa 1: sistema de armas (commits 1 a 4)

**Archivos nuevos:** `Scripts/Combat/*` y `Scripts/UI/CombatHUD.cs`. **Modificados:** `DeviceQuickbar` (el clic izquierdo pasa a las armas) y `GameSession` (una nueva partida reinicia el arsenal).

| Arma | Tipo | Daño | Cadencia | Cargador / reserva | Recarga | Particularidad |
|---|---|---|---|---|---|---|
| Pistola de plasma | Estándar, semiautomática | 20 | 4/s | 12 / 36 | 1,3 s | Precisa y se calienta (12 por disparo) |
| Rifle de asalto | Rápida, automática | 12 | 9/s | 30 / 90 | 2,0 s | Pierde daño a partir de 40 m |
| Escopeta de pulsos | Dispersión | 9 × 7 perdigones | 1,2/s | 6 / 18 | 2,4 s | Devastadora de cerca, 25 % del daño a 22 m |
| Cañón de riel | Especial | 95 | 0,7/s | 3 / 3 | 2,8 s | Atraviesa 2 robots, munición escasa y mucho ruido (26 m) |

**Controles:** clic izquierdo dispara (mantener con el rifle), clic derecho apunta, **R** recarga, **Q** cambia de arma y **G** usa los dispositivos.

**Cómo probarlo:**

1. Pulsa **Play** en `MenuPrincipal`, elige **Nueva partida** y ve a la Fase 2 (Almacén de carga), que tiene robots.
2. Abajo a la derecha debe aparecer el panel con «PISTOLA DE PLASMA 12 / 36», la barra de calor y la lista de armas. En el centro debe verse la mira de cuatro trazos y, en primera persona, el arma abajo a la derecha.
3. Dispara a una pared. Debe salir un rayo azul, un anillo pequeño en el impacto, el sonido del disparo y un retroceso del arma. La mira se abre al disparar seguido y se cierra al apuntar con clic derecho, que además hace zoom.
4. Dispara a un robot. Debe dar un respingo, verse una barra de vida sobre su cabeza durante 3 s y una «X» blanca en la mira (amarilla si das en la cabeza). Si no te había visto, va corriendo a investigar de dónde vino el disparo.
5. Con la pistola un robot de seguridad cae con **3 disparos al cuerpo o 2 a la cabeza**. Debe caer hacia atrás, apagar su foco, sonar una explosión, salir el aviso «Robot destruido» y una X roja, y soltar un **paquete de munición** azul que gira. Al pasar por encima aparece «+12 munición · …» en el arma que menos munición tiene.
6. Pulsa **Q** varias veces. Cambia el arma en mano (forma y color) y el panel. La escopeta debe soltar 7 rayos verdes en abanico y el cañón de riel uno morado que atraviesa.
7. Vacía un cargador. Se recarga solo y la barra muestra RECARGANDO. Con la pistola dispara 9 veces seguidas: aparece SOBRECALENTADA y no dispara durante 1,5 s.
8. Deja que un robot te deje fuera de combate. Al reaparecer verás «Munición perdida al caer: N», y **el robot destruido sigue destruido**.
9. Abre el inventario, un hackeo o una consola. No debe dispararse nada, y cerrar con **Q** no debe cambiar el arma.

**Sin errores:** la consola de Unity no debe mostrar excepciones. Si aparece «[PlayerWeapon] No hay cámara del jugador», la escena no tiene la cámara del jugador y conviene reconstruirla.

### Etapa 2: IA analizadora y director adaptativo (commits 5 y 6)

**Archivos nuevos:** `Scripts/Director/PlayerPerformanceModel.cs`, `DirectorModel.cs`, `GameDirector.cs`, `DirectorSession.cs` y `Scripts/Core/AdaptiveModifiers.cs`. **Modificados:** `EnemyPatrol` (velocidad y espera), `EnemyVision` (detección) y `PlayerHealth` (daño de robots), cada uno con un multiplicador que vale 1 si no hay director.

**Qué mide:** precisión por disparo (los perdigones cuentan una vez), bajas y daño recibido por minuto en una ventana de 60 s, caídas recientes (se olvidan a la mitad cada 90 s), tiempo sobrevivido, detecciones, vida actual, **acampada** (parte de los últimos 30 s en la misma celda de 4 × 4 m) y **abuso de un arma** (parte de los disparos con la misma arma).

**Qué decide:** una **tensión** de 0 a 1 que sigue al rendimiento: sube despacio (0,008/s), baja rápido (0,03/s), tiene una zona muerta de ±0,05 y da un respiro de 20 s tras cada caída. Con tensión 0,5 todo está en los valores de diseño. Por encima, los robots son más rápidos, ven antes, pegan más, aguantan más, sueltan menos munición y patrullan sin pararse tanto. Por debajo, al revés. La tensión **pasa de una fase a la siguiente**, así que la progresión deja de ser lineal.

**Cómo probarlo:**

1. Pulsa **F3** durante una fase. Arriba a la izquierda aparece el panel **DIRECTOR ADAPTATIVO** con la tensión (barra de azul a rojo), el rendimiento, las métricas y las perillas.
2. **Jugar bien:** destruye robots acertando y sin recibir daño. En uno o dos minutos el rendimiento pasa a positivo y la tensión sube poco a poco. «Velocidad robots ×1,0X» y «Detección ×1,XX» crecen, y los paquetes de munición dan menos.
3. **Jugar mal:** deja que te golpeen y cae dos veces seguidas. Al caer aparece «RESPIRO» y la tensión baja 0,15 de golpe. Con dos caídas seguidas sale «TRAJE: suministro de emergencia» con un paquete de munición delante de ti, y las perillas bajan por debajo de ×1,00.
4. **Acampar:** quédate unos 25 s en la misma zona. Sale «IA NEXUS-7: patrón detectado…» y el robot libre más cercano viene a investigar ese punto.
5. **Abusar de un arma:** dispara solo con el rifle (10 disparos o más, casi todos con él). Sale «IA NEXUS-7: los robots han analizado tu rifle de asalto…». El panel muestra «Blindaje adaptado: rifle_asalto», el rifle hace un 20 % menos de daño y las demás armas un 15 % más. Cambia de arma unos cuantos disparos y sale «blindaje adaptativo desactivado».
6. Pasa a la fase siguiente con la tensión alta. El panel debe empezar con esa tensión, no con la base de la dificultad.

### Etapa 3: respawn dinámico y anti-repetición (commits 7 y 8)

**Archivos nuevos:** `Scripts/Director/SpawnVariationModel.cs`, `RespawnVariator.cs` y `Scripts/Enemies/PatrolVariation.cs`. **Modificados:** `EnemyPatrol` (`VariationProvider`, `ApplyVariation`, `AssignRoute`), `PatrolRoute` (las rutas en bucle también se recorren al revés), `PlayerRespawn` (expone el punto de reaparición) y `SecurityCamera` (`VarySweep`).

**Qué hace al caer:**

- Cada robot reaparece **en otro waypoint de su ruta**, mirando al siguiente, en **otro sentido** y con su propio ritmo (espera ×0,7 a ×1,3 y velocidad ×0,92 a ×1,08). Nunca repite la combinación (waypoint, sentido) del intento anterior y penaliza los waypoints de los dos últimos intentos.
- Es **justo**: nunca aparece a menos de 8 m del punto de reaparición ni en el campo de visión inicial del jugador (35° y 18 m) si la ruta ofrece otra opción.
- Las **cámaras de seguridad** empiezan el barrido en otro punto y sentido.
- **Refuerzos:** con tensión ≥ 0,6 entra 1 robot extra y con ≥ 0,8 entran 2. Son copias de los tipos de robot de la fase, en rutas lejos del jugador, y su tipo y su ruta cambian respecto al intento anterior. Al volver a caer se retiran y se vuelve a decidir. Si entras en una fase con la tensión alta, la fase ya empieza con ellos (a los 2 s).
- Todo sale de la semilla de la partida, el número de intento y el robot: con la misma semilla las decisiones se repiten, lo que sirve para depurar.

**Cómo probarlo:**

1. En la Fase 2, fíjate en dónde patrulla cada robot al cruzar el checkpoint y abre el panel con **F3**.
2. Deja que te atrapen. Al reaparecer verás «IA NEXUS-7: rutas de patrulla recalculadas.», y en el panel la línea **Respawn dinámico, intento 1** con el plan («RobotSeguridad_A WP2← …»). Los robots **no están donde empezaron**: aparecen en otro punto de su ruta y la recorren en otro sentido.
3. Cae 3 o 4 veces seguidas. Cada intento debe colocarlos distinto, y nunca a tu lado ni delante de ti al recuperar el control.
4. En la Fase 4 o la 5, con cámaras, comprueba que cada intento empiezan su barrido en otra posición.
5. Para ver refuerzos, sube la tensión jugando bien (o pon «Difícil», con base 0,62). Al caer con tensión ≥ 0,6 el mensaje dice «Refuerzos desplegados: 1» y aparece un anillo rojo donde entra el robot extra. El panel muestra «refuerzos vivos 1». En el intento siguiente el refuerzo cambia de tipo o de ruta.

### Etapa 4: armas y munición en la partida guardada (commit 11)

**Modificados:** `Core/SaveData.cs` (lista `weapons` con `id`, `ammo` y `reserve`, `weaponIndex`, `hasDirectorTension` y `directorTension`), `Core/SaveSystem.cs`, `Combat/CombatSession.cs` y `Core/GameSession.cs`.

**Qué hace:** al entrar en cada fase (autoguardado) se escriben en disco las armas que llevas, la munición del cargador y de la reserva, el arma en mano y la tensión del director. Al pulsar **Continuar** vuelves con ese mismo arsenal y el director retoma la presión donde la dejaste. **Nueva partida** borra las dos cosas. Una partida guardada antes de este cambio se carga igual, con el arsenal inicial.

**Cómo probarlo:**

1. Empieza una partida, gasta munición del rifle en la Fase 1 y quédate con la escopeta en mano. Cruza a la Fase 2 (se autoguarda).
2. Abre el panel **F3** y apunta la tensión. Sal al menú y pulsa **Continuar**.
3. Debes ver en el HUD **la escopeta en mano** y los mismos números «cargador / reserva» de cada arma. En F3, la tensión es la que tenías (±0,01).
4. Pulsa **Nueva partida**: vuelves a la pistola con la munición inicial y la tensión de base.

### Etapa 5: armas con mando (commit 12)

**Archivos nuevos:** `Core/GamepadAxes.cs` y `Combat/CombatInput.cs`. **Modificados:** `ProjectSettings/InputManager.asset` (ejes nuevos), `PlayerWeapon`, `PlayerLook` y `CombatHUD`.

Todos los botones del mando ya tenían uso, así que las armas van en los **ejes** que estaban libres:

| Mando (Xbox) | Acción |
|---|---|
| Gatillo derecho (RT) | Disparar (mantener con el rifle) |
| Gatillo izquierdo (LT) | Apuntar |
| Stick derecho | Mirar |
| Cruceta abajo | Recargar |
| Cruceta izquierda / derecha | Arma anterior / siguiente |

Los gatillos se activan al pasar del 50 % y se sueltan por debajo del 30 % (no hay disparos fantasma con el gatillo a medias). Si un eje no existe en el proyecto, el juego no falla: lo ignora y avisa una sola vez en la consola.

**Cómo probarlo:**

1. Conecta un mando de Xbox (o compatible XInput) **antes** de pulsar Play.
2. En el HUD de combate aparecen las pistas «[RT] disparar · [LT] apuntar · cruceta abajo recargar · izq./der. arma».
3. Con el stick derecho mueves la cámara. Con RT disparas y con LT la mira se cierra. Cruceta izquierda/derecha cambia de arma y abajo recarga.
4. Desconecta el mando: en un segundo vuelven las pistas de teclado y ratón.

> **Aviso de plataforma:** los ejes usados (en el Input Manager: stick derecho «4th/5th axis», gatillo izquierdo «9th axis», gatillo derecho «10th axis», cruceta «6th/7th axis») son los de XInput en **Windows**. En macOS y Linux los mandos usan otros números: si allí no responde, cambia los ejes `Nexus7 Gatillo Der/Izq` y `Nexus7 Cruceta X/Y` en *Project Settings → Input Manager*.

### Etapa 6: robots que disparan (commits 13 y 14)

**Archivos nuevos:** `Scripts/Combat/Enemy/EnemyGunCatalog.cs`, `EnemyFireModels.cs`, `EnemyGun.cs` y `Scripts/Combat/PlayerTarget.cs`. **Modificados:** `PlasmaBolt` (proyectiles hostiles, sin fuego amigo), `EnemyHealth` (tambaleo y apagar el arma al morir), `PlayerHealth` (`TakeRangedDamage`), `CombatHUD` (indicador de daño), `CombatInstaller`, `CombatSoundSynth`, `DirectorModel` y `GameDirector`.

**Qué hace:**

- Los robots de **seguridad** (pistola, ráfagas de 3), **guardia** (carabina, ráfagas de 4) y **élite** (rifle de pulsos, ráfagas de 5 con pausas más cortas) disparan cuando te persiguen, te ven y estás **entre 3,5 y 24 m**. Más cerca siguen con el cuerpo a cuerpo. Los de **investigación** no llevan arma.
- **Siempre avisan:** antes de cada ráfaga sale un **láser rojo** de su mano hacia ti durante 0,4 s con un pitido. Mientras disparan se quedan quietos y se giran hacia ti.
- **Turnos de ataque:** como mucho disparan a la vez **1 robot en Fácil, 2 en Normal y 3 en Difícil** (+1 si el director aprieta). Los demás siguen persiguiéndote.
- **Puntería justa:** los proyectiles van a 35 m/s y se pueden esquivar. Fallan más de lejos, si corres o caminas, y si el robot está cegado; apuntan un poco por delante de hacia donde vas (50 %).
- **Tambaleo:** si le metes 15 de daño en 0,3 s, el robot corta la ráfaga y no vuelve a disparar en 0,6 s. Concentrar el fuego compensa.
- **Línea de tiro:** no disparan a través de paredes ni de otros robots. Sus disparos atraviesan a los demás robots (no hay fuego amigo).
- Cada impacto que recibes muestra un **arco rojo alrededor de la mira** que señala de dónde vino (arriba = delante, abajo = detrás) y te empuja un poco (×0,25 del golpe cuerpo a cuerpo).
- El panel **F3** muestra las perillas nuevas: «Tiradores extra», «Dispersión robots» y los turnos en uso.

**Cómo probarlo:**

1. En la Fase 2 (la Fase 1 no tiene robots) deja que un robot de seguridad te vea desde unos 10 m. Verás el **láser rojo**, oirás el pitido y te llegará una ráfaga de 3 proyectiles naranjas.
2. Corre en lateral: la mayoría deben fallar. Quédate quieto: aciertan casi todos.
3. Ponte detrás de una columna: deja de disparar (no hay línea de tiro). Acércate a menos de 3,5 m: pasa al cuerpo a cuerpo.
4. Con el rifle, dispárale una ráfaga larga mientras ve el láser: el aviso se corta (tambaleo).
5. Deja que te den desde un lado y desde atrás: el arco rojo señala la dirección correcta y se apaga en algo más de un segundo.
6. En la Fase 5 o 6, con varios robots persiguiéndote en Normal, abre **F3**: «Turnos» nunca pasa de 2/2 (3/3 con tensión ≥ 0,75).
7. Los robots de **investigación** (Fase 4) solo atacan cuerpo a cuerpo.

### Etapa 7: combate táctico con coberturas (commits 16 y 17)

**Archivos nuevos:** `Scripts/Combat/Enemy/CoverModels.cs`, `CoverField.cs` y `EnemyTactics.cs`. **Modificados:** `EnemyPatrol` (`ChaseDestination`: destino en persecución), `EnemyGun` (`FireGate`, `BurstsCompleted`, `TokenAvailable`), `CombatInstaller`, `EnemyHealth`, `DirectorModel` y `GameDirector`.

**Qué hace:**

- Al cargar cada fase se generan **puntos de cobertura** a 0,8 m de cada cara de las cajas, estanterías, contenedores, consolas, mesas, tabiques, bidones y columnas con colisionador, solo si caen sobre el NavMesh. La consola muestra «Coberturas de combate: N puntos».
- Cuando un robot tirador te ve, **corre a la mejor cobertura** que lo tape de ti, a 6–14 m y con poco recorrido. Cada punto es de un solo robot. Mientras corre **no dispara**.
- A cubierto espera 0,6–1,2 s y, si hay turno de ataque libre, **se asoma** 0,9 m por el lado desde el que te ve, dispara una ráfaga (con el láser de aviso de siempre) y **vuelve a cubrirse**.
- Cada tipo pelea distinto:
  - **Seguridad** aguanta en su cobertura. Si se asoma 2 veces sin tiro, cambia de cobertura.
  - **Guardia** avanza cada 5 s a una cobertura más cercana a ti.
  - **Élite** flanquea cada 4–6 s, o en cuanto se asoma sin tiro: busca un punto que cambie el ángulo 60° o más.
- Si lo rodeas y su cobertura ya no lo tapa, se recoloca. Sin coberturas útiles pelea **al descubierto a 8–12 m**. Mientras se mueve siempre te mira, así que no te pierde de vista al recolocarse.
- Si te pierde de vista más de 1,5 s, suelta la cobertura y vuelve a la búsqueda normal.
- El **director** acelera o frena todos estos tiempos con la perilla «Tempo táctico»: ×1,4 con tensión 0 (más pausados) y ×0,7 con tensión 1 (avanzan y flanquean antes). El panel **F3** muestra cuántos robots están cubiertos, asomados, moviéndose o al descubierto.

**Cómo probarlo:**

1. En la Fase 2 (almacén, con cajas y contenedores), deja que te vea un robot de seguridad a unos 10 m. Debe correr a una caja, quedarse detrás, asomarse por un lado con el láser rojo, disparar y volver a esconderse.
2. Con **F3** abierto verás la línea «Tácticas: cubiertos 1 · asomados 0 …» cambiar a «asomados 1» en cada ráfaga.
3. Rodéalo hasta verlo por el lado descubierto: en menos de un segundo debe ir a otra cobertura.
4. En la Fase 3 (guardias) quédate quieto tras una columna. El guardia debe acercarse de cobertura en cobertura cada ~5 s.
5. En la Fase 5 o 6 (élite), escóndete tras una caja grande. La élite debe moverse hacia un lado para dispararte desde otro ángulo.
6. Juega bien un rato hasta que la tensión pase de 0,7: «Tempo táctico» baja de 1 y los avances y flanqueos se notan más frecuentes.

### Etapa 8: puntos débiles (commits 18 y 19)

**Archivo nuevo:** `Scripts/Combat/Enemy/WeakPointModel.cs`. **Modificados:** `DamageInfo` (`Zone`), `EnemyHealth`, `EnemyGun` (`Dazzle`) y `CombatHUD`.

**Qué hace:**

- **Óptica** (la cabeza de todos los robots): daño **×2**, como el crítico de antes, y además **deslumbra**. Corta el láser de aviso si estaba apuntando, la puntería empeora 2 s (+6°) y no puede empezar ráfagas en 0,8 s.
- **Núcleo trasero** (solo la élite): una esfera cian brillante en su espalda. Los disparos que entran **por detrás** (a menos de 60° de hacia donde mira) a la altura del torso hacen **×3**. Flanquear a la élite compensa.
- En el HUD la «X» de impacto es **amarilla en la óptica y cian en el núcleo**, con el rótulo «ÓPTICA ×2» o «NÚCLEO ×3» bajo la mira.

**Cómo probarlo:**

1. Dispara a la cabeza de un robot mientras ves su láser de aviso: el láser se apaga, sale «ÓPTICA ×2» y tarda en volver a disparar.
2. En la Fase 5, rodea a una élite (o espera a que flanquee y te dé la espalda) y dispárale a la esfera cian: sale «NÚCLEO ×3» en cian y cae mucho antes.
3. Dispárale al pecho de frente: «X» blanca, sin rótulo.

### Etapa 9: retroceso y desarme (commits 21 y 22)

**Archivos nuevos:** `Scripts/Characters/HitRecoilModel.cs` y `Scripts/Combat/Enemy/WeaponArmModel.cs`. **Modificados:** `WeakPointModel` (zona `Arm`), `ProceduralAnimator`, `EnemyHealth`, `CombatEvents` y `CombatHUD`.

**Qué hace:**

- **Retroceso:** con cada impacto el torso del robot se echa hacia atrás (hasta 12° en 0,06 s y vuelve en 0,25 s). Un impacto fuerte, como la escopeta de cerca o el cañón de riel, lo echa más (×0,4 a ×1,5 según el daño).
- **Brazo del arma:** en los robots tiradores, los impactos a menos de 25 cm del antebrazo o la mano derechos dan en el **brazo**. Hacen el daño normal al robot y además restan de los **30 de vida del brazo**. Al llegar a 0:
  - salen chispas y el brazo queda colgando;
  - el robot deja de disparar y de usar coberturas, y pasa al **cuerpo a cuerpo**, como antes de las armas;
  - el HUD avisa «Robot desarmado: solo cuerpo a cuerpo».
- La «X» de impacto es **naranja** en el brazo, con el rótulo «BRAZO DEL ARMA». Orden de prioridad: óptica, brazo, núcleo y cuerpo.
- Un robot desarmado sigue desarmado aunque caigas y reaparezcas.

**Cómo probarlo:**

1. Dispara al pecho de un robot: se nota el respingo del torso. Con la escopeta de cerca el retroceso es mayor.
2. Apunta (clic derecho) a la mano derecha de un robot de seguridad mientras se asoma y dispárale con la pistola. Tras 2 o 3 impactos con el rótulo naranja «BRAZO DEL ARMA», salen chispas, el brazo cae y aparece «Robot desarmado».
3. Ese robot ya no hace el láser de aviso: corre hacia ti y ataca cuerpo a cuerpo.

### Etapa 10: encuentros por oleadas (commits 23 a 25)

**Archivos nuevos:** `Scripts/Combat/Encounters/WaveModels.cs`, `EncounterDirector.cs` y `Scripts/Core/ObjectiveEvents.cs`. **Modificados:** `HackTerminal`, `AccessTerminal`, `PowerConsole`, `ConsoleUI`, `LaunchStepTerminal` y `Checkpoint` (avisan del objetivo cumplido), `CombatInstaller` y `GameDirector`.

**Qué hace:**

- Cada fase con robots (de la 2 a la 6) tiene **un encuentro** que se activa una vez:
  - al cumplir un objetivo: hackeo con éxito, terminal de acceso, consola de energía, desactivar un sector o desbloquear una compuerta desde la consola de mando, o un paso de la secuencia de lanzamiento;
  - en las fases sin esos objetivos, con el primer **punto seguro**;
  - en las fases sin ninguno de los dos (Laboratorio), al **alejarte 20 m** de donde empezaste.
- Al activarse sale «IA NEXUS-7: intrusión confirmada…». A los 3 s llega la **oleada 1**: copias de los robots de la fase, con un anillo rojo donde aparece cada uno, que van directos a por ti con la detección reforzada.
- Cuando de la oleada quedan **1 o 0 robots**, o a los **25 s**, sale «¡REFUERZOS! Oleada 2/3 en 3 s». Tras despejar la última: «Zona despejada» y un paquete doble de munición a tus pies.
- **Dónde aparecen:** en waypoints de patrulla o puntos de cobertura a 8–35 m de ti (mejor a 12–20 m), fuera de tu vista si hay otra opción y, en cada intento, en puntos distintos del intento anterior.
- **Cuántos:** las oleadas del diseño (ver la tabla), ×0,7 en Fácil y ×1,3 en Difícil (mínimo 1). El director añade 1 robot a la última oleada con tensión ≥ 0,7 y quita 1 de la mayor con tensión ≤ 0,3.
- **Si caes a mitad:** los robots de las oleadas se retiran y el encuentro **vuelve a empezar 4 s después de reaparecer**, con otros puntos de aparición. Los robots de la fase destruidos siguen destruidos.
- En **F3**, la línea «Encuentro: …» muestra el estado (en espera, cuenta atrás, oleada n/N, despejado) y qué lo activó.

| Fase | Se activa con | Oleadas en Normal |
|---|---|---|
| 1 · Sala inicial | — (no tiene robots) | — |
| 2 · Almacén de carga | Primer punto seguro | 2 seguridad · 1 guardia + 1 seguridad |
| 3 · Sala de energía | Hackeo o consola de energía (lo primero que hagas) | 2 seguridad + 1 investigación · 2 guardias |
| 4 · Laboratorio | Alejarse 20 m del inicio | 2 seguridad · 2 guardias · 1 élite + 1 seguridad |
| 5 · Centro de control | Acción de la consola de mando | 3 seguridad · 2 guardias + 1 investigación · 1 élite + 2 guardias |
| 6 · Evacuación | Acción de la consola o paso de la secuencia de lanzamiento | 2 guardias + 1 seguridad · 2 élites |

Si en una fase no hay robots de un tipo, se usa otro: seguridad en lugar del que falte, y guardia en lugar de la élite.

**Cómo probarlo:**

1. En la Fase 2 cruza el primer punto seguro con **F3** abierto. «Encuentro» pasa a «cuenta atrás» y, a los 3 s, aparecen 2 robots de seguridad con un anillo rojo, lejos de ti y fuera de tu vista.
2. Destruye uno: con 1 vivo sale «¡REFUERZOS! Oleada 2/2 en 3 s» y llegan 2 más (la Fase 2 no tiene guardias, así que el guardia de la oleada es otro robot de seguridad). Destruye a todos los de la última oleada: «Zona despejada» y munición a tus pies.
3. En la Fase 3, hackea la terminal o activa la consola de energía. Deja vivo solo a uno de la primera oleada: aparece «¡REFUERZOS! Oleada 2/2 en 3 s».
4. Deja que te atrapen a mitad: los robots de la oleada desaparecen y, 4 s después de reaparecer, el encuentro vuelve a empezar. Los anillos rojos salen en otros sitios.
5. En Difícil la primera oleada de la Fase 5 trae 4 robots en vez de 3.

### Etapa 11: vidas (commits 27 y 28)

**Archivos nuevos:** `Scripts/Gameplay/LivesModel.cs` y `LivesSystem.cs`. **Modificados:** `DirectorModel` (`LastLife`), `GameDirector`, `RespawnVariator`, `EncounterDirector`, `GameSession`, `CombatInstaller` y `CombatHUD`.

**Qué hace:**

- Tienes **5 vidas en Fácil, 3 en Normal y 2 en Difícil**. Se rellenan al entrar en cada fase nueva.
- Cada vez que te atrapan o caes (vida a 0, descarga, robot que te alcanza) **pierdes una vida**. El aviso dice «Vidas: 2/3», y con la última «¡ÚLTIMA VIDA!» en rojo.
- **Despejar un encuentro de oleadas** devuelve una vida («+1 vida»), sin pasar del máximo.
- **Sin vidas:** «SIN VIDAS. Reiniciando la fase…», y la fase se recarga desde su inicio con las vidas llenas y el inventario con el que entraste. El reinicio cambia la semilla de las patrullas, los refuerzos, las oleadas y las variantes: la fase no se juega igual que antes.
- **Director clemente:** con la última vida, la tensión objetivo no pasa de la base − 0,1. Aunque vayas bien, no aprieta hasta que recuperes una vida.
- En el HUD, encima del panel del arma, hay un cuadrado por vida: cian, o rojos con la última. En **F3** aparecen las vidas y los reinicios de la fase.

**Cómo probarlo:**

1. En Normal, entra en la Fase 2: encima del panel del arma ves «VIDAS» con 3 cuadrados cian.
2. Deja que te atrape un robot: «Vidas: 2/3» y un cuadrado vacío. Otra vez: «¡ÚLTIMA VIDA!» y el cuadrado que queda en rojo. En F3 sale «ÚLTIMA: director clemente» y la tensión deja de subir.
3. Despeja el encuentro de oleadas de la fase: «+1 vida (2/3)».
4. Pierde todas: «SIN VIDAS» y la fase vuelve a empezar desde su entrada con 3 vidas. En F3, «Reinicios de la fase 1». Fíjate en que los robots no patrullan donde la primera vez.
5. Al pasar a la Fase 3 las vidas vuelven a estar llenas.

### Etapa 12: variantes de robot (commits 29 y 30)

**Archivos nuevos:** `Scripts/Director/RobotAffixModel.cs`, `RobotAffixState.cs` y `RobotAffixDirector.cs`. **Modificados:** `EnemyHealth` (`SetAffixHealth`), `EnemyPatrol` (`AffixSpeed`), `EnemyVision` (`AffixDetection`), `EnemyGun` (`AffixSpread`) y `GameDirector`.

**Qué hace:**

- Al empezar cada fase y **en cada reaparición**, el director marca a algunos robots vivos con un **rasgo**, visible como una esfera brillante sobre la cabeza:

| Rasgo | Color | Efecto |
|---|---|---|
| Blindado | Dorado | Vida ×1,4 |
| Rápido | Verde | Velocidad de patrulla y persecución ×1,2 |
| Ojo de águila | Magenta | Te detecta ×1,3 más rápido y apunta mejor (dispersión ×0,8) |

- **Cuántos** depende de la tensión: ninguno por debajo de 0,4, 1 hasta 0,6, 2 hasta 0,8 y 3 desde 0,8. Nunca más de la mitad de los robots de la fase.
- **Anti-repetición:** en cada intento prefiere robots que no tenían rasgo; si tiene que repetir robot, le cambia el rasgo. Al reaparecer, el mensaje dice qué unidades se han mejorado.
- Los refuerzos y los robots de las oleadas aparecen sin rasgo.
- En **F3**, la línea «Variantes:» dice qué robot tiene cada rasgo.

**Cómo probarlo:**

1. En Difícil (tensión de base 0,62 → 2 variantes), entra en la Fase 3: como solo hay 2 guardias, a los 0,6 s uno de ellos lleva una esfera de color (como mucho la mitad). **F3** dice cuál («RobotGuardia_Reactor Blindado», por ejemplo).
2. Si es el blindado, dispárale: su barra de vida baja más despacio que la del otro guardia.
3. Deja que te atrapen: sale el mensaje «IA NEXUS-7: unidades mejoradas en este intento: …» y la esfera pasa al **otro** guardia.
4. En Fácil (base 0,38) no deberías ver ninguna esfera al empezar, salvo que juegues muy bien y la tensión pase de 0,4.

### Etapa 13: eventos de nave (commits 32, 33 y 36)

**Archivos nuevos:** `Scripts/Director/ShipEventModel.cs`, `ShipEventDirector.cs` y `BlackoutZone.cs`. **Modificado:** `GameDirector` (línea en F3).

**Qué hace:**

- Al empezar cada fase (a 1 s) y **en cada reaparición**, el director puede lanzar un **evento de nave** en un punto de tu camino: una cobertura o un waypoint de patrulla a 6–25 m, mejor si está por delante de ti (60° a cada lado).

| Evento | Cuándo | Qué pasa | Mensaje |
|---|---|---|---|
| Apagón | Vas mal: tensión < 0,45 o alguna caída reciente | Las lámparas a menos de 7 m bajan al 20 % y una zona de 14 × 14 m cuenta como sombra (te detectan más despacio) | «NAVE: fallo eléctrico en un sector cercano…» |
| Fuga de vapor | Vas mal (alterna con el apagón) | 2 nubes de vapor separadas 2,6 m que tapan la vista de los robots | «NAVE: fuga de vapor en un conducto cercano…» |
| Barrido de seguridad | Vas sobrado: tensión ≥ 0,65 | A los 15 s, y luego cada 45 s, un aviso de 25 m manda a los robots cercanos a revisar ese punto | «IA NEXUS-7: barrido de seguridad programado en 15 s…» y «…barrido en curso» |

- Con la tensión en medio (0,45–0,65), la mitad de los intentos no hay evento y la otra mitad sale cualquiera.
- **Anti-repetición:** nunca repite el evento ni el sitio del intento anterior si hay otra opción.
- El evento dura hasta tu siguiente caída: entonces las luces vuelven a su intensidad y el vapor desaparece.
- En **F3**, la línea «Evento de nave:» dice cuál está activo, a cuántos metros y cuántas luces ha atenuado o cuánto falta para el siguiente barrido.

**Cómo probarlo:**

1. En Fácil (tensión de base 0,38), entra en la Fase 2. Al segundo sale un mensaje «NAVE: …» y en F3 ves «Evento de nave: Apagón a 14 m» o «Fuga de vapor a 14 m».
2. Si es la fuga, mira hacia delante: hay dos columnas de vapor gris. Ponte detrás de ellas respecto a un robot y comprueba que no te detecta aunque estés a la vista.
3. Si es el apagón, acércate al punto: si había lámparas, la zona se ve más oscura (F3 dice cuántas luces). Dentro cuentas como en sombra: los robots tardan más en detectarte.
4. Deja que te atrapen: el evento anterior desaparece y, si sigues yendo mal, sale **el otro** evento de ayuda en otro sitio.
5. En Difícil (base 0,62), juega bien un rato hasta que la tensión pase de 0,65 y cae una vez: al reaparecer sale «barrido de seguridad programado en 15 s». En F3 ves la cuenta atrás; al llegar a 0, los robots cercanos dejan la patrulla y van a investigar ese punto.

### Etapa 14: informe de la IA (commits 34 y 35)

**Archivos nuevos:** `Scripts/Director/PhaseReportModel.cs` y `PhaseReportPanel.cs`. **Modificados:** `GameDirector` (rellena el informe), `DirectorSession` (lo guarda entre escenas), `ShipEventDirector` y `GameFlow` (`PhaseIndex`).

**Qué hace:**

- Mientras juegas, el analizador apunta **toda la fase** (no solo los últimos 60 s del F3): disparos y aciertos, bajas, vidas perdidas, muertes en combate, daño recibido, detecciones, arma más usada, acampada media, tensión media y máxima, y lo que hizo la IA (expulsiones por acampar, blindajes adaptados, ayudas y eventos de nave).
- Al entrar en la **fase siguiente**, a los 1,5 s aparece arriba a la derecha durante 9 s el panel «INFORME DE LA IA · fase anterior», con el estilo de juego que ha visto y lo que hará con eso:

| Estilo | Cuándo | Veredicto |
|---|---|---|
| Fantasma | 0 disparos y como mucho 1 detección | El sigilo no se castiga |
| En apuros | 2 vidas perdidas o más | La siguiente fase empieza con menos presión |
| Atrincherado | Acampada media ≥ 50 % | Mandará unidades a sacarte |
| Tirador | 3 bajas o más con precisión ≥ 45 % | Afinarán la puntería y dispararán más a la vez |
| Agresivo | 2 bajas por minuto o más | Más tiradores y refuerzos |
| Equilibrado | Lo demás | La presión se mantiene |

- Si has disparado 10 veces o más y el 70 % fue con la misma arma, el veredicto añade que el blindaje adaptativo lo notará.
- Si la fase se **reinicia por quedarte sin vidas**, el panel dice «intento anterior» y resume ese intento.
- Solo sale si la fase duró 20 s o más, y solo en la fase siguiente o al reiniciar la misma (una partida nueva no enseña un informe viejo).

**Cómo probarlo:**

1. Juega la Fase 2 entera: dispara con la pistola, derriba algún robot y deja que te atrapen una vez.
2. Al entrar en la Fase 3, a los 1,5 s aparece el panel con «Fase 2 · Almacén de carga», tu precisión (por ejemplo «Precisión 42% (8/19)»), «Vidas perdidas 1», «Arma dominante Pistola 100%», las acciones de la IA y el estilo. Abajo, en amarillo, el veredicto entre comillas.
3. El panel se desvanece a los 9 s.
4. Pasa la Fase 3 sin disparar ni ser visto: en la Fase 4 el estilo es «Fantasma».
5. Pierde todas las vidas en una fase: al reiniciarse, el panel dice «intento anterior» y el estilo «En apuros».

### Etapa 15: accesorios de arma (commits 38 y 39)

**Archivos nuevos:** `Scripts/Combat/WeaponModModel.cs`. **Modificados:** `PlayerWeaponCatalog` (`Clone`), `WeaponModel` (`BaseSpec`, `Mods`, `InstallMod`), `ArsenalModel` (`ModTarget`), `PlayerWeapon` (`GiveMod`), `CombatSession`, `SaveData` y `CombatHUD`.

**Qué hace:**

- El arsenal pasa a ser modular: **cada arma lleva hasta 2 accesorios**. El arma con accesorios es una copia del arma del catálogo con los valores cambiados, así que el disparo, el ruido, el HUD y el director usan los valores nuevos sin tocar nada más.

| Accesorio | Etiqueta | Efecto | Dónde sirve |
|---|---|---|---|
| Supresor | SUP | Ruido ×0,45, daño ×0,9 | Todas |
| Cargador ampliado | CAR | Cargador ×1,5 (pistola 12 → 18), recarga ×1,15 | Todas |
| Estabilizador | EST | Dispersión ×0,7, apertura por disparo y retroceso ×0,6 | Todas |
| Disipador | DIS | Calor por disparo ×0,6 | Pistola, rifle y cañón (las que se calientan) |

- Un módulo recogido va al **arma de la mano** si le sirve y no lo lleva; si no, a otra con hueco libre. Con los 2 huecos llenos, el nuevo **sustituye al más antiguo**, y el aviso lo dice. Si quitas un cargador ampliado, las balas que sobran vuelven a la reserva.
- En el HUD, a la derecha de la munición, salen en morado las etiquetas de los accesorios del arma de la mano («SUP · EST»).
- Los accesorios pasan de fase y se guardan con la partida (Continuar).

**Cómo probarlo** (los módulos los da la Etapa 16, así que se prueban juntas):

1. Recoge un módulo: sale el aviso morado «Supresor en Pistola de plasma: menos ruido, algo menos de daño» y en el HUD aparece «SUP».
2. Con el supresor en la pistola, dispara a unos 6 m de un robot de espaldas: sin supresor (10 m de ruido) suele oírte y girarse; con él (4,5 m), no.
3. Con el cargador ampliado en la pistola, el contador muestra «18 / …» tras recargar.
4. Pasa de fase o guarda, sal al menú y pulsa Continuar: el arma conserva sus etiquetas.

### Etapa 16: recompensas del director (commits 40 y 41)

**Archivos nuevos:** `Scripts/Director/ModRewardModel.cs`, `ModRewardDirector.cs` y `Scripts/Combat/ModPickup.cs`. **Modificados:** `EncounterDirector` (evento `Cleared`), `GameDirector` (línea en F3) y `PhaseReportModel` (cuenta los módulos).

**Qué hace:**

- Al **despejar un encuentro de oleadas** y al **destruir un élite** de la fase (no los de refuerzo ni los de las oleadas), cae un **módulo de arma**: un rombo morado brillante que flota. Se recoge pasando por encima.
- El director elige el accesorio según lo que el analizador ve en los últimos 60 s:

| Si… | Da |
|---|---|
| Te ven poco (≤ 0,5 detecciones/min) o disparas desde el mismo sitio (acampada ≥ 50 %) | Supresor |
| Fallas mucho (precisión < 30 % con 8 disparos o más) | Estabilizador |
| Llevas un ritmo agresivo (≥ 1,5 bajas/min) | Cargador ampliado |
| Usas sobre todo un arma que se calienta (≥ 60 % de los disparos) | Disipador |

- Solo elige accesorios que alguna de tus armas pueda montar. Si nada destaca, sale cualquiera, pero no el de la última vez. Si todas las armas ya llevan todo lo posible, no cae nada.
- El mensaje dice por qué: «TRAJE: zona despejada. Módulo de arma recuperado: estabilizador (fallas muchos disparos). Recógelo.»
- En **F3**, la línea «Recompensas:» cuenta los módulos de la fase y el último. El informe de la IA de la Etapa 14 también los cuenta.

**Cómo probarlo:**

1. En la Fase 2, cumple el objetivo para activar el encuentro de oleadas y dispara mucho fallando a propósito (más de 8 disparos, menos del 30 % de aciertos).
2. Al despejarlo, junto a la munición de recompensa cae un rombo morado y el mensaje dice «estabilizador (fallas muchos disparos)». Recógelo: en el HUD sale «EST».
3. Repite en la Fase 3 sin que te vean antes del encuentro: lo normal es el supresor.
4. En la Fase 5 o la 6, destruye un élite (núcleo cian en la espalda): cae otro módulo, distinto del anterior si nada destaca.
5. Abre **F3** para ver «Recompensas: 1 en la fase · última …».

## 4. Pruebas finales y balance

### Pruebas automáticas

| Suite | Comprobaciones | Qué cubre |
|---|---|---|
| `WeaponTest` | 63 | Cadencia, semiautomática, cargador, recarga, calor, dispersión, caída de daño, vida, arsenal, sonidos, sesión, guardado en disco y mando (gatillos y cruceta) |
| `EnemyFireTest` | 29 | Armas por tipo de robot, dispersión y anticipación, turnos de ataque, ráfagas con aviso, tambaleo e indicador de daño |
| `TacticalCoverTest` | 37 | Estilos, puntuación de coberturas, mejor punto, reservas, ciclo cubrirse/asomarse, asomadas fallidas, relojes de avance y flanqueo, tempo del director |
| `WeakPointTest` | 26 | Óptica, núcleo trasero, brazo del arma y desarme, prioridades, multiplicadores y curva del retroceso |
| `EncounterTest` | 29 | Oleadas por fase, escalado por dificultad y director, activadores, secuencia, reinicio y puntos de aparición justos y variados |
| `LivesTest` | 10 | Vidas por dificultad, perder y ganar, máximo, reinicio de fase y relleno al cambiar de fase |
| `AffixTest` | 10 | Cuántas variantes según la tensión, efectos de cada rasgo y reparto sin repetir |
| `ShipEventTest` | 8 | Qué evento según la tensión y las caídas, sin repetir, y elección del sitio (distancia, por delante, sin repetir) |
| `WeaponModTest` | 29 | Efectos de cada accesorio, huecos y sustitución, munición al montar y quitar, guardado y a qué arma va |
| `ModRewardTest` | 13 | Qué accesorio según cómo juegas, variedad sin repetir, solo los que caben y el motivo |
| `PhaseReportTest` | 23 | Recuento de la fase, precisión por disparo, arma dominante, estilos de juego, veredicto y líneas del panel |
| `DirectorTest` | 42 | Métricas, ventanas, olvido, acampada, abuso de arma, rendimiento, tensión, respiro, patrones y perillas nuevas |
| `SpawnVariationTest` | 16 | Anti-repetición, justicia, ritmo, refuerzos, cámaras y reproducibilidad |

Fuera de Unity solo se comprueban la lógica y la compilación (mcs y Roslyn). El comportamiento en Unity hay que verlo con los pasos de arriba.

### Qué tocar según la sensación

| Sensación | Valor | Dónde |
|---|---|---|
| Los robots mueren demasiado rápido o lento | `Damage` de cada arma, o la vida base | `PlayerWeaponCatalog.cs`, `HealthModel.BaseHealthFor` |
| Un arma no compensa | `FireRate`, `MagazineSize`, `ReloadTime`, `HeatPerShot`, `FalloffStart`/`MinDamageFactor` | `PlayerWeaponCatalog.cs` |
| Falta o sobra munición | `StartReserve`, `MaxReserve`, `AmmoPerPickup` y la perilla `AmmoDrop` | `PlayerWeaponCatalog.cs`, `DirectorModel.KnobsFor` |
| Disparar alerta demasiado a los robots | `NoiseRadius` de cada arma | `PlayerWeaponCatalog.cs` |
| La dificultad cambia demasiado rápido o se nota a saltos | `RisePerSecond` (0,008), `FallPerSecond` (0,03) y `DeadZone` (0,05) | `DirectorModel` |
| El director reacciona poco o demasiado | `Range` (0,35): cuánto se aleja la tensión de la base | `DirectorModel` |
| Demasiado fácil o difícil en general | `BaselineFor` (0,38 / 0,5 / 0,62) | `DirectorModel` |
| Los extremos son demasiado duros o blandos | Los dos valores de cada `Knob(t, con tensión 0, con tensión 1)` | `DirectorModel.KnobsFor` |
| Frustra después de caer | `ReliefDuration` (20 s), `ReliefDrop` (0,15), `AssistDeaths` (1,6) y `respawnAmmoLoss` (25 %) | `DirectorModel`, `PlayerWeapon` |
| Qué cuenta como jugar bien | Objetivos `TargetAccuracy` (35 %), `TargetKillsPerMinute` (1,5) y `TargetDamagePerMinute` (40), y los pesos de `Performance` | `PlayerPerformanceModel` |
| El aviso de acampada salta demasiado | `CampThreshold` (0,7), `CampCooldown` (45 s) y `CellSize` (4 m) | `DirectorModel`, `PlayerPerformanceModel` |
| El blindaje adaptativo es injusto | `DominanceThreshold` (0,75), `DominanceRelease` (0,55) y los factores 0,8 / 1,15 de `WeaponDamageFactor` | `DirectorModel` |
| Los robots aparecen demasiado cerca | `MinSafeDistance` (8 m), `ViewDistance` (18 m) y `ViewHalfAngle` (35°) | `SpawnVariationModel` |
| Los intentos se parecen demasiado | `HistoryLength` (2), `RandomWeight` (25) y los rangos `MinWait`/`MaxWait` y `MinSpeed`/`MaxSpeed` | `SpawnVariationModel` |
| Los robots hacen demasiado daño a distancia | `Damage` (4 / 4 / 5 por impacto en Normal) y la perilla `EnemyDamage` | `EnemyGunCatalog` |
| Te disparan demasiados a la vez | `CapacityFor` (1 / 2 / 3) y el umbral 0,75 de `ExtraShooters` | `AttackTokens`, `DirectorModel.KnobsFor` |
| Aciertan demasiado o muy poco | `Base` (1,5°), `PerMeter` (0,15°/m), `Running` (+2°), `Walking` (+1°), multiplicadores de dificultad ×1,5 / ×0,6 y la perilla `EnemySpread` (1,35 → 0,75) | `AimModel`, `DirectorModel.KnobsFor` |
| No da tiempo a reaccionar | `WarnTime` (0,4 s), la velocidad del proyectil (35 m/s) y `LeadFactor` (0,5) | `BurstModel`, `EnemyGunCatalog`, `AimModel` |
| Disparan sin parar o casi nunca | `BurstCount`, `BurstRate`, `PauseMin`/`PauseMax` (1,2–2 s; élite 0,9–1,5 s) | `EnemyGunCatalog` |
| Disparan desde muy lejos o muy cerca | `MinRange` (3,5 m) y `MaxRange` (24 m) | `EnemyGunCatalog` |
| Cuesta interrumpirlos | `Threshold` (15), `Window` (0,3 s) y `Duration` (0,6 s) | `StaggerModel` |
| Los robots se esconden demasiado o casi nunca disparan | `WaitMin`/`WaitMax` (0,6–1,2 s), `PeekMax` (2,5 s) y la capacidad de los turnos | `TacticsModel`, `AttackTokens` |
| El guardia te come terreno muy deprisa | `AdvanceEvery` (5 s), `AdvanceBonus` (20) y `AdvanceGain` (2 m) | `TacticsModel`, `CoverScoring` |
| La élite flanquea demasiado | `FlankMin`/`FlankMax` (4–6 s), `FlankBonus` (25) y `FlankAngle` (60°) | `TacticsModel`, `CoverScoring` |
| Eligen coberturas absurdas | `Protects` (50), rango ideal `IdealMin`/`IdealMax` (6–14 m), `TravelPerMeter` (1) y `SearchRadius` (15 m) | `CoverScoring` |
| Faltan o sobran coberturas en una fase | Nombres de objetos en `Names`, `Offset` (0,8 m) y `MinSpacing` (0,9 m) | `CoverField` |
| El director cambia poco el ritmo táctico | Los extremos de `TacticTempo` (1,4 / 0,7) | `DirectorModel.KnobsFor` |
| Los puntos débiles deciden demasiado | `OpticMultiplier` (×2), `CoreMultiplier` (×3), `CoreMinDot` (0,5 = 60°) y `DazzleSeconds`/`DazzleNoBurstSeconds` (2 / 0,8 s) | `WeakPointModel` |
| Desarmar es demasiado fácil o difícil | `MaxHealth` del brazo (30) y `ArmRadius` (0,25 m) | `WeaponArmModel`, `WeakPointModel` |
| El retroceso se nota poco o demasiado | `MaxDegrees` (12°), `Duration` (0,25 s) y `DamageForFull` (20) | `HitRecoilModel` |
| Las oleadas abruman | Tamaños de cada fase, `DifficultyFactor` (0,7 / 1 / 1,3) y los umbrales 0,7 / 0,3 de tensión | `EncounterCatalog` |
| Las oleadas llegan demasiado seguidas | `AdvanceWhenAlive` (1), `MaxWaveTime` (25 s) y `CountdownTime` (3 s) | `WaveModel` |
| Aparecen demasiado cerca o a la vista | `MinDistance` (8 m), `ViewDistance` (18 m) y `ViewHalfAngle` (35°) | `EncounterSpawnPicker` |
| El encuentro tarda en volver tras caer | `RestartDelay` (4 s) | `EncounterDirector` |
| Las vidas se acaban muy rápido o no importan | `MaxFor` (5 / 3 / 2) y la vida extra al despejar un encuentro | `LivesModel`, `EncounterDirector` |
| La última vida es demasiado fácil o igual de dura | `LastLifeMargin` (0,1) | `DirectorModel` |
| Demasiadas o pocas variantes | Umbrales 0,4 / 0,6 / 0,8 de `CountFor` y el tope de la mitad de los robots | `RobotAffixModel` |
| Una variante es injusta | `ArmoredHealth` (1,4), `SwiftSpeed` (1,2), `HawkeyeDetection` (1,3) y `HawkeyeSpread` (0,8) | `RobotAffixModel` |
| Los gatillos del mando se disparan solos | Umbrales 0,5 / 0,3 de `TriggerLatch` y `dead` (0,1) de los ejes | `CombatInput`, `InputManager.asset` |
| Los eventos de nave ayudan demasiado o casi nunca salen | `HelpBelow` (0,45), `HurtFrom` (0,65) y el 50 % sin evento en la zona media | `ShipEventModel.Choose` |
| El apagón no se nota | `BlackoutRadius` (7 m) y `BlackoutLightFactor` (0,2) | `ShipEventModel` |
| El vapor tapa demasiado | `SteamClouds` (2), la separación (±1,3 m) y el radio de `VisionSmoke` (1,8 m) | `ShipEventModel`, `ShipEventDirector` |
| El barrido agobia | `SweepDelay` (15 s), `SweepRepeat` (45 s) y `SweepNoiseRadius` (25 m) | `ShipEventModel` |
| Los eventos salen muy lejos o a la espalda | `MinDistance`/`MaxDistance` (6–25 m) y `AheadAngle` (60°) | `ShipEventModel` |
| El informe se equivoca de estilo | `StruggleDeaths` (2), `EntrenchedCamp` (0,5), `SharpKills` (3), `SharpAccuracy` (0,45), `AggressiveKpm` (2) y `OneWeaponShare` (0,7) | `PhaseReportModel` |
| El informe dura poco o estorba | `ShowDelay` (1,5 s), `ShowTime` (9 s) y `MinDuration` (20 s) | `PhaseReportPanel`, `PhaseReportModel` |
| Un accesorio es obligatorio o inútil | `SuppressorNoise`/`SuppressorDamage` (0,45 / 0,9), `MagFactor`/`MagReload` (1,5 / 1,15), `StabSpread`/`StabBloom`/`StabRecoil` (0,7 / 0,6 / 0,6) y `HeatsinkHeat` (0,6) | `WeaponModModel` |
| Se acumulan demasiadas mejoras | `MaxSlots` (2) | `WeaponModModel` |
| El director acierta poco con el accesorio | `Fit` (40), `RepeatPenalty` (25), `RandomWeight` (15) y los umbrales 0,5 / 0,5 / 30 % / 1,5 | `ModRewardModel` |
| Salen demasiados módulos | Quitar el módulo del élite (`OnKilled`) o del encuentro (`OnCleared`) | `ModRewardDirector` |
| Demasiados o pocos refuerzos | Umbrales 0,6 / 0,8 de `Reinforcements` en `KnobsFor` y `maxReinforcements` (2) | `DirectorModel`, `RespawnVariator` |

Para ajustar con datos, juega con **F3** abierto: si la tensión se queda pegada a un extremo, el problema está en `Range` o en los objetivos del analizador. Si oscila, sube `DeadZone` o baja `RisePerSecond`.

## Límites conocidos

- Esta implementación se ha comprobado con las pruebas automáticas y con la compilación como la de Unity (Roslyn), **no dentro del editor de Unity**. La primera partida en Unity es la prueba de integración.
- El HUD de combate y el panel F3 usan IMGUI (`OnGUI`) para no depender de los constructores de escena. Si se quiere el estilo del resto del HUD (TextMeshPro), se pueden pasar a un constructor más adelante.
- El mando usa ejes de XInput en Windows (ver la Etapa 5). En otras plataformas puede hacer falta cambiar los números de eje.
- Las coberturas se generan por el nombre de los objetos de la escena. Un objeto con otro nombre no sirve de cobertura hasta que se añada a `CoverField.Names`.
- Diferencias con el diseño «IA de combate» (`ia-combate-design.md`) en los encuentros: no se bloquean puertas durante el encuentro, no hay caja de recompensa con material (solo munición) y los activadores son los objetivos que ya tiene cada fase, no objetos colocados a mano. Para cambiar el activador de una fase basta con lanzar `ObjectiveEvents.Raise` desde otro objeto.
- Un robot desarmado no recupera el arma al reaparecer el jugador, igual que los robots destruidos siguen destruidos.
- El láser de aviso usa el material `Sprites/Default`. Si una compilación lo elimina del build, el aviso se queda en el pitido: añádelo a *Always Included Shaders*.
- Las vidas viven en memoria durante la partida: al pulsar Continuar empiezas la fase guardada con las vidas llenas.
- Al reiniciar una fase sin vidas se vuelve a cargar la escena, así que su introducción (si la tiene) se vuelve a ver.
- El apagón solo atenúa lámparas puntuales y focos (no la luz direccional). Si cerca del punto no hay lámparas, la zona sigue contando como sombra pero no se ve más oscura.
- Los accesorios no se ven en el modelo del arma (solo en el HUD), y no se pueden quitar a mano: solo se sustituyen al recoger otro.
- El informe de la IA vive en memoria: si vuelves al menú y pulsas Continuar en la misma fase, verás el informe del intento que dejaste.
