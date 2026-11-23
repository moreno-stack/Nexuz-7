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

## 3. Etapas: cómo probar cada una

Antes de cada etapa, ejecuta las pruebas desde la raíz con `bash Tests/run_all.sh`. El resultado esperado es `TOTAL: 852 comprobaciones, 0 fallos` y `ROSLYN: compilación OK`.

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

## 4. Pruebas finales y balance

### Pruebas automáticas

| Suite | Comprobaciones | Qué cubre |
|---|---|---|
| `WeaponTest` | 50 | Cadencia, semiautomática, cargador, recarga, calor, dispersión, caída de daño, vida, arsenal, sonidos y sesión |
| `DirectorTest` | 40 | Métricas, ventanas, olvido, acampada, abuso de arma, rendimiento, tensión, respiro y patrones |
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
| Demasiados o pocos refuerzos | Umbrales 0,6 / 0,8 de `Reinforcements` en `KnobsFor` y `maxReinforcements` (2) | `DirectorModel`, `RespawnVariator` |

Para ajustar con datos, juega con **F3** abierto: si la tensión se queda pegada a un extremo, el problema está en `Range` o en los objetivos del analizador. Si oscila, sube `DeadZone` o baja `RisePerSecond`.

## Límites conocidos

- Esta implementación se ha comprobado con las pruebas automáticas y con la compilación como la de Unity (Roslyn), **no dentro del editor de Unity**. La primera partida en Unity es la prueba de integración.
- El HUD de combate y el panel F3 usan IMGUI (`OnGUI`) para no depender de los constructores de escena. Si se quiere el estilo del resto del HUD (TextMeshPro), se pueden pasar a un constructor más adelante.
- Sin mando: las armas usan teclado y ratón, porque todos los botones del mando ya tienen uso.
- Los robots siguen atacando cuerpo a cuerpo. Los robots que disparan son el subproyecto «IA de combate» (`ia-combate-design.md`), y el director ya está preparado para sus oleadas: cuántos refuerzos y por dónde.
- Las armas y la munición pasan entre fases en memoria (`CombatSession`), pero todavía no se escriben en la partida guardada. Al pulsar Continuar se empieza con el arsenal inicial.
- No hay vidas limitadas: el director usa las caídas recientes como medida de «vidas perdidas».
