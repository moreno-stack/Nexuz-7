# Diseño: combate básico (armas, proyectiles, enemigos con vida y HUD de combate)

**Fecha:** 2026-11-11
**Estado:** pendiente de revisión por Samuel
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

## Fuera de alcance

- Enemigos que disparan, comandos y drones de asalto (subproyecto 4).
- Mejoras y modificaciones de armas (subproyecto 5).
- La nueva celda de armería, el despertar y la historia nueva (subproyectos 1 y 3).
- Daño por zonas más allá de la cabeza, munición especial y granadas.
