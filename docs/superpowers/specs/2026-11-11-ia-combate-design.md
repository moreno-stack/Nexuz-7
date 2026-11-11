# Diseño: IA de combate, reacción al daño y oleadas

**Fecha:** 2026-11-11
**Estado:** pendiente de revisión por Samuel
**Depende de:** `2026-11-11-combate-basico-design.md`: proyectiles con pool, `IDamageable`/`DamageInfo`, `EnemyHealth`, `DestroyedRegistry`, indicador de daño, minas y munición. Se implementa **después** de ese documento (combate básico + economía).
**Contexto:** subproyecto 4 del giro a acción («Enemigos que disparan»).

## Objetivo

Que los robots, al detectar al jugador, dejen de perseguirle a ciegas y **combatan**: buscan cobertura, se asoman a disparar proyectiles de energía, avanzan y flanquean. Además deben reaccionar a los impactos y tener puntos débiles. Cada fase tendrá un **encuentro por oleadas** en su momento clave, que obligue a usar armas, minas y cobertura. El sigilo sigue siendo válido: los robots siguen ciegos fuera de su cono y se puede evitar el combate.

## Lo que pidió Samuel

- Al detectar al jugador, pasar de patrulla a «Ataque Activo»: buscar cobertura cercana, avanzar flanqueando y disparar proyectiles energéticos.
- Reacción al recibir disparos: retroceso, interrupción del disparo y destrucción de componentes en puntos débiles.
- Puntos de activación en las salas (hackear una consola, abrir una puerta blindada) que desaten pequeñas oleadas.
- Scripts de la IA y pautas para equilibrar la dificultad.

Decisiones de Samuel:

| Pregunta | Respuesta |
|---|---|
| Documento | **Aparte**, implementado después del combate básico y la economía |
| Quién dispara | **Mezcla**: Seguridad y Guardia disparan desde cobertura; Investigación sigue cuerpo a cuerpo y avisa; Élite dispara ráfagas y flanquea |
| Oleadas | **Una por fase** en su momento clave |
| Dificultad Normal | **Exigente pero justa** (aviso antes de disparar, turnos de ataque, cobertura que funciona) |
| Diseño | Aprobado tal cual |

## Arquitectura

`EnemyPatrol` (637 líneas) sigue haciendo patrulla, investigación, búsqueda y aturdimiento, y **no crece**. Se añade `EnemyCombatBrain`, que toma el control en combate:

```
EnemyPatrol: Patrullar / Investigar / Buscar ──(alerta = 1 y lo ve)──► ¿tiene EnemyCombatBrain con arma?
        ├─ no (Investigación o desarmado) → Perseguir cuerpo a cuerpo (como hoy)
        └─ sí → EnemyCombatBrain.Engage()
               Cubrirse → Asomarse y disparar ráfaga → (Avanzar | Flanquear | Cambiar de cobertura) → …
               Lo pierde de vista → Fuego de cobertura 1,5 s hacia la última posición → devuelve el control → Buscar (memoria de EnemyPatrol)
```

- `EnemyPatrol` expone un punto de enganche: `bool TryHandOffToCombat()`, que se llama donde hoy empieza la persecución, y `ResumeFromCombat(Vector3 lastSeen)`.
- El cerebro usa el `NavMeshAgent`, `EnemyVision` (cono, rayos, memoria) y `AlertSystem` que ya existen.
- El aturdimiento por PEM y la ceguera mandan sobre el cerebro: lo pausan.

## Componentes

| Archivo | Responsabilidad |
|---|---|
| `Enemies/Combat/EnemyCombatBrain.cs` | Máquina de estados de combate |
| `Enemies/Combat/CombatStateModel.cs` | Transiciones entre estados (puro, probado) |
| `Enemies/Combat/EnemyGun.cs` + `EnemyGunData.cs` | Arma del robot: aviso láser, ráfaga, cadencia, proyectiles enemigos (pool del combate básico, bando «enemigo») |
| `Enemies/Combat/AimModel.cs` | Dispersión según distancia, movimiento y postura del jugador y ceguera (puro) |
| `Enemies/Combat/CoverPoint.cs` + `CoverRegistry.cs` | Puntos de cobertura y reservas |
| `Enemies/Combat/CoverScoring.cs` | Puntuación de coberturas (puro) |
| `Enemies/Combat/AttackTokens.cs` | Cuántos pueden disparar a la vez (puro + registro por escena) |
| `Enemies/Combat/HitReaction.cs` + `StaggerModel.cs` | Retroceso, tambaleo e interrupción (puro el modelo) |
| `Enemies/Combat/WeakPoint.cs` | Óptica, brazo del arma y núcleo trasero (colisionadores hijos con su multiplicador) |
| `Encounters/EncounterZone.cs` + `WaveModel.cs` | Encuentro por oleadas (puro el modelo) |
| `Encounters/SpawnPoint.cs`, `Encounters/EncounterTrigger.cs` | Dónde aparecen y qué lo activa |
| `Editor/CoverBuilder.cs`, `Editor/EncounterBuilder.cs` | Generar coberturas y colocar encuentros en las fases |
| Cambios | `EnemyPatrol` (enganche), `RobotBuilder` (arma, puntos débiles, cerebro según el estilo), `ProceduralAnimator` (pose de impacto), `PlayerHealth` (recibir `DamageInfo` de proyectiles), `DoorController` / `HackTerminal` / `ConsoleUI` (eventos para los encuentros), constructores de las 6 fases |

## Estilos de robot

| Estilo | Vida | Arma | Ráfaga | Cadencia | Daño por impacto (Normal) | Táctica |
|---|---|---|---|---|---|---|
| Seguridad | 60 | Pistola de energía | 3 | 5/s | 4 | Cubrirse y asomarse |
| Guardia | 90 | Carabina de energía | 4 | 6/s | 4 | Avanzar de cobertura en cobertura hacia el jugador |
| Investigación | 50 | — (cuerpo a cuerpo de hoy) | — | — | — | Perseguir y avisar al escuadrón (`DroneSquad`) |
| Élite | 120 | Rifle de pulsos | 5 | 7/s | 5 | Flanquear cada 4–6 s si el jugador está cubierto |

- El daño se multiplica por `DifficultySettings` como el de hoy.
- **Proyectil enemigo:** 35 m/s, alcance 40 m, rojo anaranjado. Lo para la cobertura.

## Disparo justo

- **Aviso:** un láser rojo fino del cañón al objetivo durante **0,4 s** antes de cada ráfaga, con un pitido corto.
- **Turnos de ataque** (`AttackTokens`): solo disparan a la vez **1 en Fácil, 2 en Normal y 3 en Difícil**. Los demás se cubren o se recolocan. Un turno se suelta al acabar la ráfaga o al perder la línea de visión.
- **Puntería** (`AimModel`): dispersión base 1,5°, +0,15° por metro a partir de 8 m, +2° si el jugador corre, +1° si camina, −0,5° si el robot está quieto, ×0,6 en Difícil y ×1,5 en Fácil. Cegado: +6°. Apunta a la última posición vista con una anticipación del 50 % de la velocidad del jugador.
- **Entre ráfagas:** 1,2–2,0 s (Élite 0,9–1,5 s).
- **Se asoma** solo si tiene línea de visión desde el punto de asomarse. Si no, se recoloca.

## Coberturas

- `CoverBuilder` genera `CoverPoint` alrededor de cada objeto llamado Caja, Estanteria, Contenedor, Consola, Mesa, Tabique o Bidon (los nombres que ya usan la capa visual y la iluminación):
  - 4 puntos a 0,8 m de cada cara.
  - Solo si están sobre el NavMesh, a menos de 1 m.
  - Cada punto guarda la normal de la cara y si la cobertura es baja (menos de 1,2 m: agachado) o alta.
- **Puntuación** (`CoverScoring`), entre los puntos a menos de 15 m del robot:
  - +50 si un rayo del pecho del jugador al punto choca (protege).
  - +30 si la distancia al jugador está entre 6 y 14 m, y menos según se aleje de ese rango.
  - −1 por cada metro que tiene que recorrer.
  - +25 si el ángulo respecto a la línea actual robot–jugador es de 60° o más y el robot flanquea.
  - −100 si está reservado por otro robot o a menos de 3 m del jugador.
- **Reserva:** `CoverRegistry` da un punto a un solo robot y lo libera al irse o al morir.
- Si no hay ningún punto válido: se mantiene a 8–12 m del jugador moviéndose de lado y dispara al descubierto.

## Reacción al daño y puntos débiles

- **Retroceso:** cada impacto echa el torso hacia atrás 0,25 s (pose nueva en `ProceduralAnimator`).
- **Tambaleo** (`StaggerModel`): si suma 15 de daño o más en 0,3 s, cancela la ráfaga o el golpe que estaba preparando, suelta su turno de ataque y no vuelve a disparar en 0,6 s.
- **Puntos débiles** (`WeakPoint`, colisionadores hijos):

| Punto | Quién | Efecto |
|---|---|---|
| Óptica (ojo) | Todos | Daño ×2 y cegado 2 s (+6° de dispersión y no puede iniciar ráfagas en 0,8 s) |
| Brazo del arma | Seguridad, Guardia, Élite | Vida propia de 30. Al destruirlo: chispas, el arma cae y el robot pasa a **cuerpo a cuerpo** (persecución de hoy) |
| Núcleo trasero | Élite | Daño ×3. Solo se alcanza por detrás: premia flanquear |

- El brazo recibe el daño del impacto entero, y ese daño también se resta de la vida del robot.

## Oleadas (`EncounterZone`)

- **Activadores** (`EncounterTrigger`): hackeo con éxito de una terminal (`HackTerminal`), acción de una consola (`ConsoleUI`), puerta blindada que se abre (`DoorController`) o un volumen al que entra el jugador. Cada uno activa un encuentro una sola vez.
- **Secuencia** (`WaveModel`):
  - Las oleadas van en orden.
  - La siguiente empieza cuando queda **1 o ningún enemigo vivo** de la actual, o a los **25 s**, con una cuenta atrás de 3 s y el aviso «¡REFUERZOS!».
  - Al terminar la última y despejar la zona: «Zona despejada», se abren las puertas y aparece una **caja de recompensa** con munición y un material.
- **Puntos de aparición** (`SpawnPoint`): en puertas o rejillas, con un destello y un sonido. Nunca aparecen a menos de 8 m del jugador ni dentro de su campo de visión, si hay otro punto libre.
- **Bloqueo:** durante el encuentro, las puertas de la zona (`lockedDoors`) quedan cerradas con su luz en rojo.
- **Al caer:**
  - El encuentro se **reinicia**: los enemigos aparecidos se retiran y se vuelve al estado anterior a la activación (el activador puede volver a usarse).
  - Los robots que ya estaban en la sala y se destruyeron siguen destruidos (`DestroyedRegistry`).
  - Antes de cada encuentro hay un checkpoint.
- **Cantidades por dificultad:** ×0,7 en Fácil, ×1 en Normal y ×1,3 en Difícil, redondeando y con un mínimo de 1 por oleada.

| Fase | Activador | Oleadas (Normal) |
|---|---|---|
| 1 · Sala inicial | Abrir la salida | 1: 2 Seguridad |
| 2 · Almacén de carga | Dar energía al montacargas | 2: 2 Seguridad · 1 Guardia + 1 Seguridad |
| 3 · Sala de energía | Activar el reactor | 2: 2 Seguridad + 1 Investigación · 2 Guardia |
| 4 · Laboratorio | Hackear la terminal principal | 3: 2 Seguridad · 2 Guardia · 1 Élite + 1 Seguridad |
| 5 · Centro de control | Acción de la consola de mando | 3: 3 Seguridad · 2 Guardia + 1 Investigación · 1 Élite + 2 Guardia |
| 6 · Evacuación | Abrir la puerta de la cápsula | 2: 2 Guardia + 1 Seguridad · 2 Élite |

La Fase 1 la sustituirá la celda de armería (subproyecto 3). Su encuentro se moverá allí.

## Pautas de equilibrio (Normal)

| Medida | Valor |
|---|---|
| Disparos de pistola a robot de Seguridad (60) | 3 al cuerpo · 2 a la óptica |
| Disparos de rifle a robot de Seguridad | 5 al cuerpo · 3 a la óptica |
| Ráfagas completas para tumbar al jugador (100) al descubierto | ~8 (Seguridad) · ~4 (Élite) |
| A cubierto agachado | Solo se recibe daño si un robot flanquea |
| Robots disparando a la vez | 2 |
| Encuentro típico | 3–6 robots en total, 45–90 s |

La guía incluirá qué valor tocar para cada sensación («mueren demasiado rápido», «me matan desde lejos», «las oleadas abruman») y dónde está cada uno.

## Errores y casos límite

- Robot sin NavMesh cerca de ninguna cobertura → lucha al descubierto con movimiento lateral; no se queda quieto.
- El jugador muere a mitad de ráfaga → los proyectiles en vuelo se apagan al reaparecer.
- Un encuentro activo y una cinemática → el encuentro se pausa (`EnemyPatrol.Paused`).
- PEM durante la ráfaga → la cancela, aturde y suelta el turno.
- Robot desarmado → no vuelve a pedir turnos de disparo.
- Destruir un robot mientras reserva una cobertura → la reserva se libera.

## Pruebas (TDD, fuera de Unity)

- `CombatStateModel`: detecta → cubrirse; en cobertura y con turno → aviso → ráfaga → entre ráfagas; sin línea de visión → recolocarse; lo pierde → fuego de cobertura 1,5 s → devolver el control; aturdido → pausa; desarmado → cuerpo a cuerpo.
- `CoverScoring`: protegido gana a no protegido; rango ideal; reservado o muy cerca descartado; el flanqueo suma solo al flanquear.
- `AttackTokens`: 1/2/3 por dificultad, se liberan al terminar la ráfaga o al morir, y no se dan dos al mismo robot.
- `AimModel`: dispersión por distancia, movimiento, quietud, cegado y dificultad; anticipación del 50 %.
- `StaggerModel`: 15 en 0,3 s tambalea y 14 no; la ventana se desliza; bloqueo de 0,6 s.
- `WeakPoint`: multiplicadores; el brazo con 30 de vida desarma una sola vez.
- `WaveModel`: activar una sola vez; siguiente oleada con ≤ 1 vivo o a los 25 s; final y recompensa; reinicio al caer; cantidades por dificultad con mínimo 1.
- Compilación mcs + Roslyn y todas las suites existentes.

## Fuera de alcance

- Drones voladores de asalto (más adelante, con su propio movimiento en 3D).
- Granadas o armas de área enemigas.
- Que los enemigos recojan armas o munición.
- Diálogos o voces de los comandos.
