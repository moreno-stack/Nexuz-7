# Diseño: combate básico y economía de combate (armas, proyectiles, enemigos con vida, HUD, crafteo rápido y botín)

**Fecha:** 2026-11-11
**Estado:** pendiente de revisión por Samuel
**Partes:** 1) combate básico · 2) economía de combate (crafteo rápido en campo y botín de enemigos), añadida a petición de Samuel. Se construyen en ese orden.
**Contexto:** subproyecto 2 del giro de sigilo a **acción, supervivencia y disparos**. Los subproyectos son:
1. Historia nueva.
2. **Combate básico** (este documento).
3. Celda de armería y despertar (sustituirá a la Fase 1).
4. Enemigos que disparan (comandos y drones de asalto).
5. Mejoras de armamento.

## Objetivo

Que el jugador pueda **disparar** con una Pistola de Plasma y un Rifle de Asalto Táctico, gestionando cargador, calor y munición de reserva. Los robots y las cámaras **reciben daño**, reaccionan y se destruyen, y el HUD muestra munición, mira y de dónde llega el daño. Todo encaja con lo que ya existe: visión, ruido, sigilo, estamina, inventario, guardado y reaparición.

## Lo que pidió Samuel

- Clase base para armas futuristas (pistola de plasma y rifle de asalto) con cadencia, daño, tamaño del cargador y consumo de munición.
- Recarga con tiempo de espera, sonido y partículas.
- Raycast o proyectiles con pool, disparados desde la perspectiva del jugador.
- Impacto en enemigos: reciben daño, parpadean y muestran barra de vida o partículas.
- HUD: munición actual / total, mira dinámica en el centro e indicador de daño en los bordes.
- Scripts e instrucciones para configurar el prefab del arma y del proyectil.

Decisiones de Samuel:

| Pregunta | Respuesta |
|---|---|
| Munición | Cargadores **y** sobrecalentamiento |
| Disparos | **Proyectiles con pool** (visibles, rápidos, barrido cada fotograma) |
| Robot destruido y luego reaparece el jugador | **Sigue destruido** |
| De dónde salen las armas (hasta que exista la armería) | **Pistola al empezar (Fase 1)**, **rifle en un armero de la Fase 4** |
| Controles | **Clic izquierdo dispara, clic derecho apunta**, [R] recargar, [Q]/rueda cambiar de arma; los dispositivos solo con [G] |
| Diseño | Aprobado tal cual |

## Valores

| | Pistola de Plasma | Rifle de Asalto Táctico |
|---|---|---|
| Daño (cabeza ×2) | 20 | 12 |
| Disparo | Semiautomático, 4/s | Automático, 9/s |
| Cargador | 12 | 30 |
| Recarga | 1,3 s | 2,0 s |
| Calor por disparo | 12 | 4 |
| Velocidad del proyectil | 60 m/s | 120 m/s |
| Alcance | 60 m | 80 m |
| Dispersión cadera / apuntando | 1,2° / 0,3° | 2,5° / 0,6° |
| Retroceso visual por disparo | 0,6° | 0,35° |
| Ruido del disparo | 10 m | 18 m |
| Munición de reserva | Célula de plasma | Munición táctica |
| Munición al conseguirla | 24 de reserva + cargador lleno | 60 de reserva + cargador lleno |

- **Calor** (las dos armas): sube con cada disparo y baja a 40/s cuando lleva 0,25 s sin disparar. Al llegar a 100, el arma queda **sobrecalentada** 1,5 s, y al terminar ese tiempo el calor vuelve a 50.
- **Recarga:** con [R] o sola al vaciar el cargador si hay reserva. Rellena el cargador con lo que haya en la reserva. Cambiar de arma la cancela.
- **Apuntar** (clic derecho): campo de visión de 60° a 45° en 0,15 s, dispersión de apuntado y movimiento ×0,6. No se puede correr mientras se apunta.
- **Dispersión:** a la de base se suma la «expansión», que crece 0,4° por disparo y se recupera a 4°/s, con un máximo de +3°.
- **Enemigos:** robot normal 60, élite 120, cámara de seguridad 25. En la cabeza o la lente el daño es ×2.

## Decisiones técnicas

| Tema | Decisión |
|---|---|
| Datos del arma | `PlayerWeaponData` (ScriptableObject) con todos los valores de la tabla, su `ItemData` de munición, sonidos opcionales y el prefab visual. Se llama así porque `WeaponData` ya es el arma cuerpo a cuerpo de los robots. Los dos assets los crea el editor en `Assets/_Nexus7/Data/Weapons/Player/`. |
| Lógica del arma | `WeaponModel` (puro, probado): estados Listo / Recargando / Sobrecalentado, `TryFire(time)`, `StartReload(reserve)`, `Tick(dt)`, calor, cadencia y expansión. `SpreadModel` (puro): dispersión actual y dirección aleatoria dentro del cono. |
| Arsenal | `PlayerArsenal` (en el jugador): armas que tiene, la que está en mano y la munición de cada cargador. Cambiar de arma tarda 0,35 s. La reserva se lee y se consume en el `InventorySystem`. |
| Disparo | `PlayerWeapon` (en el jugador): lee la entrada, pregunta al modelo, calcula el objetivo con un rayo desde el centro de la cámara (si no toca nada, el punto a la distancia del alcance), saca un proyectil del pool en el cañón hacia ese punto con la dispersión, emite ruido (`NoiseSystem`) y anima el retroceso y el destello. |
| Proyectiles | `ProjectilePool` (uno por escena, se crea solo, 32 proyectiles, crece si hace falta). `PlasmaBolt`: avanza `velocidad × dt` y antes de moverse hace un `SphereCast` de radio 0,05 m a lo largo de ese tramo (no atraviesa paredes). Ignora al que disparó y los activadores. Vida máxima: el alcance o 2 s. |
| Impactos | `IDamageable.TakeDamage(DamageInfo)`, con `DamageInfo` = cantidad, punto, normal, dirección, origen (posición del tirador), si es crítico y quién disparó. En superficies: chispas y una marca que se desvanece en 4 s (pool de 16). |
| Vida de enemigos | `EnemyHealth : IDamageable` en robots y cámaras. Lógica pura en `HealthModel`: vida, crítico, muerte una sola vez. Al recibir daño: parpadeo blanco de 0,08 s en sus renderers (MaterialPropertyBlock), chispas y barra de vida flotante 3 s (`EnemyHealthBar`, en el mundo, siempre de cara a la cámara). Si el robot no veía al jugador, va a **investigar el origen del disparo** con el error de `SoundPropagation.EstimatePosition`. |
| Muerte | Robot: se desactivan patrulla, visión, ataque y agente, el foco se apaga, el cuerpo se inclina y cae en 0,6 s, y suelta humo y un destello. El colisionador se queda como cobertura. Cámara: se apaga y suelta chispas para siempre. Se apunta en `DestroyedRegistry` (por escena), así que al reaparecer el jugador no vuelve. `RunStats.RecordKill()`. |
| Reaparición | `EnemyPatrol.ResetToStart` y el reinicio de los robots no tocan a los destruidos. Al reaparecer, las armas se conservan y la reserva pierde el 25 % (la munición es un recurso menor). |
| Munición en el inventario | Nueva categoría `ItemCategory.Ammo`, añadida al final para no cambiar los valores guardados. Dos `ItemData`: «Célula de plasma» (montón máximo 99) y «Munición táctica» (99). Aparecen en las tablas de botín de los contenedores de todas las fases. |
| Entre fases y guardado | `GameSession` guarda las armas (Ids), el arma en mano y la munición de cada cargador, y `SaveData` lo escribe (campos nuevos; con la versión 1, un guardado antiguo sin ellos carga sin armas). La Fase 1 da la pistola si la partida no tiene armas. |
| Dónde se consiguen | Fase 1: pistola + 24 células al empezar. Fase 4 (Laboratorio): `WeaponPickup` en un armero con el rifle + 60 balas («[E] Coger Rifle de Asalto Táctico»). Más munición en contenedores. |
| Entrada | Teclado: clic izquierdo disparar (mantener con el rifle), clic derecho apuntar, [R] recargar, [Q] o rueda cambiar de arma. Mando: RT disparar, LT apuntar, cruceta abajo recargar, cruceta izquierda/derecha cambiar de arma, con ejes nuevos en el Input Manager que añade el editor (`Nexus7 Gatillo Der`, `Nexus7 Gatillo Izq`, `Nexus7 Cruceta X`, `Nexus7 Cruceta Y`). Si faltan, se leen como 0, sin error. `DeviceQuickbar` deja de usar el clic izquierdo y solo usa [G] y el botón Y del mando. |
| Cuándo no se dispara | Con la entrada bloqueada (hackeos, consolas, cinemáticas, reaparición), con el inventario o la fabricación abiertos, con el cursor libre (el primer clic solo lo bloquea) o con `EnemyPatrol.Paused`. |
| Modelos | Pistola y rifle hechos con primitivas por código (cuerpo, cañón con brillo emisivo y luz del cañón). En primera persona cuelgan de la cámara (abajo a la derecha). En tercera persona van en la mano derecha del muñeco. `HeldItemView` (el objeto de la barra rápida en la mano) se oculta mientras hay un arma en mano. |
| HUD | `AmmoHUD` (abajo a la derecha): nombre del arma, «12 / 36», barra de calor (azul → naranja → rojo) y avisos RECARGANDO y SOBRECALENTADA. `CrosshairUI` (centro): 4 trazos separados según la dispersión actual, una X de impacto 0,15 s (roja si destruye) y la mira se cierra al apuntar. `DamageIndicatorUI` (bordes): un arco rojo por golpe girado hacia el atacante (`PlayerHealth.PlayerHit`), que se desvanece en 1,2 s; hasta 6 a la vez. Todo se oculta con `GameHud.Hidden`. |
| Sonidos | Disparo de plasma, disparo de rifle, recarga, sobrecalentamiento, cambio de arma, impacto en metal y explosión de robot, generados por `ProceduralAudio` (canal Efectos). |

## Componentes

| Archivo | Responsabilidad |
|---|---|
| `Combat/PlayerWeaponData.cs` | Datos del arma (ScriptableObject) |
| `Combat/WeaponModel.cs` | Cargador, recarga, calor, cadencia y expansión (puro) |
| `Combat/SpreadModel.cs` | Dispersión y dirección dentro del cono (puro) |
| `Combat/DamageInfo.cs`, `Combat/IDamageable.cs`, `Combat/HealthModel.cs` | Daño y vida (puro) |
| `Combat/PlayerArsenal.cs` | Armas que se tienen, arma en mano, cargadores |
| `Combat/PlayerWeapon.cs` | Entrada, disparo, apuntar, retroceso |
| `Combat/CombatInput.cs` | Entrada de combate con ratón, teclado y mando, ejes opcionales (lógica de combinación pura) |
| `Combat/ProjectilePool.cs`, `Combat/PlasmaBolt.cs` | Proyectiles reutilizables con barrido |
| `Combat/ImpactFx.cs` | Chispas y marcas de impacto (pool) |
| `Combat/EnemyHealth.cs`, `Combat/EnemyHealthBar.cs`, `Combat/DestroyedRegistry.cs` | Vida de robots y cámaras, barra, destruidos por escena |
| `Combat/WeaponPickup.cs` | Armero / recogida de arma |
| `UI/AmmoHUD.cs`, `UI/CrosshairUI.cs`, `UI/DamageIndicatorUI.cs` | HUD de combate |
| `Editor/WeaponBuilder.cs` | Assets de armas y munición, modelos, ejes del Input Manager, HUD de combate y armero de la Fase 4 |
| Cambios | `ItemData` (Ammo), `DeviceQuickbar` (sin clic izquierdo), `EnemyPatrol` (destruidos, investigar disparos), `SecurityCamera` (destruida), `GameSession` y `SaveData`/`SaveSystem` (armas), `RunStats` (destruidos), `ProceduralAudio` (sonidos), constructores de las fases (jugador con armas, HUD, botín de munición) |

## Flujo de un disparo

```
Entrada (clic / RT) → PlayerWeapon → WeaponModel.TryFire(t)
   ├─ no: sin munición → recarga automática si hay reserva · sobrecalentada → aviso
   └─ sí: rayo desde la cámara → punto objetivo → SpreadModel → ProjectilePool.Spawn(cañón → dirección)
          NoiseSystem.Emit(10/18 m) · destello y retroceso · sonido
PlasmaBolt.Update: SphereCast del tramo → impacto
   ├─ IDamageable → EnemyHealth.TakeDamage → parpadeo, barra, chispas; ¿no veía? → investigar origen; vida 0 → Destroyed
   └─ superficie → ImpactFx (chispas + marca)
HUD: AmmoHUD lee PlayerArsenal/WeaponModel · CrosshairUI lee la dispersión y los impactos · DamageIndicatorUI escucha PlayerHealth.PlayerHit
```

## Errores y casos límite

- Disparar sin cargador ni reserva → «clic» de vacío y el aviso SIN MUNICIÓN, sin excepción.
- Recargar con el cargador lleno o sin reserva → no hace nada.
- Cambiar de arma durante la recarga → la recarga se cancela y la munición no se pierde.
- Un proyectil que nace dentro de una pared (cañón pegado a ella) → el disparo se resuelve desde la cámara: si el rayo de la cámara al cañón choca, el impacto se aplica en ese punto.
- Un enemigo destruido no recibe más daño ni vuelve a morir.
- Una partida guardada sin datos de armas → se carga sin armas, y la Fase 1 da la pistola.
- Si faltan los ejes del mando en el Input Manager → solo funcionan ratón y teclado, sin errores.

## Pruebas (TDD, fuera de Unity)

- `WeaponModel`: cadencia (no dispara antes de 1/cadencia), semiautomático (requiere soltar), cargador y vacío, recarga (tiempo, consume reserva, parcial, cancelación), calor (sube, se enfría tras 0,25 s, sobrecalienta a 100, bloqueo de 1,5 s, vuelve a 50) y expansión (crece, máximo, se recupera).
- `SpreadModel`: dispersión cadera / apuntando + expansión; la dirección siempre cae dentro del cono.
- `HealthModel`: daño, crítico ×2, muerte una sola vez, sin daño tras morir, normalizado para la barra.
- `PlayerArsenal` (lógica pura): añadir arma sin duplicar, cambiar con [Q] / rueda en ciclo, cargadores por arma, munición recogida al conseguirla.
- `CombatInput`: combinación de ratón, teclado y ejes del mando (umbral de gatillo 0,5; cruceta con flanco).
- Barrido del proyectil: no atraviesa una pared fina a 120 m/s (prueba de la lógica de tramo).
- `DamageIndicator`: ángulo en pantalla del atacante respecto a la mirada (delante, detrás, izquierda, derecha).
- Guardado: armas, arma en mano y cargadores ida y vuelta; un guardado antiguo sin armas se carga.
- `RespawnPenalty`: la munición (Ammo) pierde el 25 %; las armas no.
- Compilación mcs + Roslyn (como Unity 6.6) y todas las suites existentes.

# Parte 2: economía de combate (crafteo rápido y botín)

## Lo que pidió Samuel

- Inventario con materiales de asalto: Chatarra balística, Núcleos de energía, Pólvora espacial y Componente médico / Nanobots médicos.
- Panel de **crafteo rápido en campo** con una tecla, para fabricar al momento con lo recogido de enemigos y cofres. Recetas clave:
  - Cargador de Munición de Plasma = Chatarra balística + Núcleo de energía.
  - Nanobot de Curación = Componente médico + Chatarra.
  - Mina de Proximidad Defensiva = Núcleo + Pólvora.
- Los enemigos abatidos sueltan recursos aleatorios que se recogen pasando por encima o interactuando.

Decisiones de Samuel:

| Pregunta | Respuesta |
|---|---|
| Orden | Se añade a este diseño y se construye **después** del combate básico |
| Materiales actuales | **Renombrar y añadir**: Chatarra metálica → Chatarra balística, Batería de iones → Núcleo de energía; nuevos Pólvora espacial y Componente médico |
| Tecla | **[F]** (mando: mantener View 0,5 s, porque pulsarlo abre el inventario) |
| Velocidad | **Casi instantáneo y sin pausar**: el juego sigue, fabricar tarda 0,5–1 s y hace un poco de ruido |

## Materiales

| Material | Antes | Id (no cambia: las partidas guardadas siguen valiendo) | Categoría |
|---|---|---|---|
| Chatarra balística | Chatarra metálica | `scrap_metal` | Part |
| Núcleo de energía | Batería de iones | `battery_ion` | Battery |
| Circuito de control | (igual) | `component_circuit` | Component |
| Pólvora espacial | nuevo | `space_powder` | Part |
| Componente médico | nuevo | `medical_component` | Component |

Al renombrar, se actualizan también los textos de los objetivos, pistas y diálogos que nombran «batería(s) de iones» o «chatarra metálica» (por ejemplo, «Consigue 2 núcleos de energía»). Las puertas, zócalos y recetas que ya los usan siguen funcionando, porque solo cambia el nombre visible.

## Recetas de campo

| Receta | Ingredientes | Resultado | Tiempo |
|---|---|---|---|
| Cargador de Munición de Plasma | 1 Chatarra balística + 1 Núcleo de energía | 12 Células de plasma | 0,8 s |
| Cargador Táctico | 2 Chatarra balística + 1 Pólvora espacial | 30 Munición táctica | 0,8 s |
| Nanobot de Curación | 1 Componente médico + 1 Chatarra balística | 1 Nanobot de curación | 0,6 s |
| Mina de Proximidad Defensiva | 1 Núcleo de energía + 1 Pólvora espacial | 1 Mina de proximidad | 1,0 s |

Las recetas del banco de trabajo (señuelo, PEM, camuflaje, kit de reparación) siguen igual y solo se fabrican en el banco. Las recetas de campo también aparecen en el banco.

## Objetos nuevos

- **Nanobot de curación** (dispositivo, se usa sobre uno mismo): recupera 35 de integridad en 1,5 s. Se usa desde la barra rápida con [G] o directamente con **[H]** (usa el primero que haya). Montón máximo 5.
- **Mina de proximidad** (dispositivo, se lanza): con [G] se lanza 2 m por delante y se queda en el suelo. Se arma en 1,5 s (luz parpadeando en amarillo y luego roja fija). Estalla si un robot entra en 2,5 m: radio 4 m y 80 de daño, decreciente con la distancia y sin atravesar paredes (rayo al centro). Hace un ruido de 20 m. No daña al jugador. Montón máximo 3.

## Panel de crafteo rápido (`FieldCraftingUI`)

- [F] lo abre y lo cierra, igual que [Esc]. **El juego no se pausa**: puedes seguir moviéndote con WASD, pero no mirar con el ratón (el cursor queda libre para hacer clic), disparar ni interactuar.
- Es un panel flotante a la derecha con las 4 recetas de campo. Cada una muestra su icono, nombre, ingredientes con lo que tienes («Núcleo de energía 1/1») y el resultado. Las que no puedes fabricar salen en gris con lo que falta en rojo.
- Para fabricar: clic en la receta o su tecla [1]–[4] mientras el panel está abierto. Aparece una barra de progreso (0,5–1 s). Los ingredientes se consumen al **terminar**. Si recibes un golpe o cierras el panel, se cancela sin perder nada. Fabricar hace un ruido de 4 m.
- Mando: mantener View 0,5 s abre el panel, la cruceta elige la receta y A fabrica.
- Reutiliza `CraftingSystem` (comprobar y consumir ingredientes y dar el resultado). La diferencia con el banco es solo la interfaz y que no pausa.

## Botín de enemigos (`EnemyLoot`)

Al destruir un robot caen de 1 a 3 objetos, elegidos con la semilla de la partida, que saltan un poco y brillan:

| Robot | Chatarra balística | Núcleo de energía | Pólvora espacial | Componente médico | Células de plasma |
|---|---|---|---|---|---|
| Normal | 70 % (1–2) | 35 % (1) | 30 % (1) | 20 % (1) | 50 % (4–8) |
| Élite | 90 % (2–3) | 60 % (1–2) | 50 % (1–2) | 35 % (1) | 70 % (8–12) |

- Si la tirada no da nada, cae al menos 1 Chatarra balística.
- **Recogida:** se recogen solos al pasar a menos de 1,2 m, atraídos hacia el jugador en los últimos 0,3 s, o con [E]. Muestran la notificación de siempre. Si el inventario está lleno, se quedan en el suelo con el aviso «Inventario lleno».
- Lo que no se recoge se queda en el suelo mientras estés en la fase.
- Los **contenedores** de todas las fases añaden a sus tablas de botín Pólvora espacial, Componente médico y munición.

## Componentes de la parte 2

| Archivo | Responsabilidad |
|---|---|
| `Crafting/FieldRecipes.cs` | Qué recetas son de campo (lista en un asset `FieldRecipeBook`) |
| `Crafting/FieldCraftModel.cs` | Fabricación con tiempo, cancelación y consumo al terminar (puro) |
| `UI/FieldCraftingUI.cs` | Panel flotante, teclas [F]/[1]–[4], mando |
| `Combat/EnemyLoot.cs` + `Combat/LootDropModel.cs` | Tabla por tipo de robot y tirada con semilla (puro) |
| `Inventory/AutoPickup.cs` | Recogida al pasar por encima con atracción |
| `Devices/NanoHealDevice.cs`, `Devices/ProximityMine.cs` | Nanobot y mina (dos valores nuevos de `DeviceKind` al final del enum) |
| Cambios | `DeviceFactory` (mina y nanobot), `PlayerController` (entrada con el panel abierto), constructores (materiales renombrados, objetos y recetas nuevos, botín de contenedores, textos), `PlayerWeapon` (no dispara con el panel abierto) |

## Pruebas de la parte 2

- `FieldCraftModel`: no empieza sin ingredientes; tarda lo indicado; consume al terminar; cancelar no consume; no se puede fabricar dos a la vez.
- `LootDropModel`: probabilidades por tipo (con semilla fija y muchas tiradas, cada porcentaje ±3 %), cantidades en rango, de 1 a 3 objetos y al menos uno.
- Mina: daño según la distancia (80 en el centro, 0 a 4 m) y armado a los 1,5 s.
- Nanobot: cura 35 en 1,5 s sin pasar del máximo.
- Autorrecogida: distancia de 1,2 m y no recoge con el inventario lleno.
- Renombrado: los Id no cambian (una partida guardada con `battery_ion` se carga como Núcleo de energía).

## Fuera de alcance

- Enemigos que disparan, comandos y drones de asalto (subproyecto 4).
- Mejoras y modificaciones de armas (subproyecto 5).
- La nueva celda de armería, el despertar y la historia nueva (subproyectos 1 y 3).
- Daño por zonas más allá de la cabeza, munición especial y granadas.
- Que los enemigos recojan objetos, el comercio y la moneda.
