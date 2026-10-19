# Escape Espacial: Nexus-7 — Fase 6 (Sala de Evacuación y cierre del juego)

Requisitos: Fases 1–5 en el proyecto. No hace falta instalar paquetes nuevos (TextMeshProUGUI y uGUI ya se usaban).

---

## 1. Puesta en marcha

1. Copia `Assets/_Nexus7` encima del de tu proyecto, con los archivos de las Fases 5 y 6.
2. Si aún no lo has hecho, ejecuta **Nexus-7 → Fase 5 → Construir Centro de Control**.
3. Ejecuta **Nexus-7 → Fase 6 → Construir Sala de Evacuación (y Menú Principal)**. Este constructor:
   - crea `Scenes/Evacuacion.unity`, que es la escena final;
   - crea `Scenes/MenuPrincipal.unity` si todavía no existe;
   - ordena los **Build Settings**: MenuPrincipal → SalaInicial → AlmacenCarga → SalaEnergia → Laboratorio → CentroControl → Evacuacion;
   - enlaza la consola de mando del Centro de Control con la evacuación, de modo que «Retomar el control» carga `Evacuacion`.
4. **Play** en `Evacuacion` para probar solo el cierre. Empiezas con un equipo de prueba.
   Para jugar la partida completa, abre `MenuPrincipal` y pulsa **Play**.

Otros elementos del menú **Fase 6**:

| Elemento | Para qué sirve |
|---|---|
| Construir Menú Principal | Rehace solo el menú |
| Ordenar Build Settings | Vuelve a ordenar las escenas, por ejemplo si añades una nueva |
| Conectar salida de la Fase 5 | Vuelve a enlazar el Centro de Control con la evacuación |

---

## 2. Flujo completo del juego

```
 MenuPrincipal ──[Nueva partida]──► SalaInicial ► AlmacenCarga ► SalaEnergia ► Laboratorio ► CentroControl
      ▲                               (RunStats.EnsureStarted: arranca el cronómetro)              │
      │                                                                    consola de mando: «Retomar el control»
      │                                                                    (guarda el inventario y carga ↓)
      │                                                                                    ▼
      │                         ┌──────────────── Evacuacion: cuenta atrás de 5:00 ────────────────┐
      │                         │ liberar abrazaderas + celda de ignición → escotilla B → LANZAR   │
      │                         │            ▼ Escape()            ▼ salud 0         ▼ tiempo 0   │
      │                         │                 EndingManager → ScoreCalculator                  │
      │                         └──────────── cinemática → fundido → pantalla de resultados ───────┘
      │                                                   │                │
      └────────────────────[M] Menú principal ────────────┘   [R] Reiniciar → nueva partida desde SalaInicial
```

Todo lo que haces desde la Fase 1 cuenta para la nota final. Los datos se guardan en tres sistemas estáticos que se
mantienen al cambiar de escena:

| Sistema | Qué guarda |
|---|---|
| `AlertSystem.Stats` | Detecciones, alarmas, Alertas Rojas, capturas, ruido y hackeos fallidos |
| `RunStats` (nuevo) | Tiempo, objetos recogidos y fabricados, dispositivos usados y de qué tipos, hackeos superados, sectores apagados, objetivos de PEM, señuelos efectivos, daño y muertes |
| `GameSession` | El inventario |

`GameFlow.ResetRunState()` reinicia los tres al empezar una partida nueva o al volver al menú.

---

## 3. Sala de Evacuación (escena)

### Plano (44 × 40 m, techo a 14 m)

```
 z=40 ┌──[cápsula A ✖]──────────[cápsula B ✔]──────────[cápsula C ✖]──┐
 z=37 │ ══ humo ══════ PASARELA DE EMBARQUE (y=4.2) ═ ignición ═══ 📷 ═ │  ← dron de seguridad (vaivén)
 z=34 │      ╱rampa O                                   rampa E╲       │
      │   (sensor láser)                          (cables con descargas)│ VENTANAL
 z=26 │                 ⟲ élite ─────────────────── élite ⟲            │ PANORÁMICO
      │ ┌─CABINA─┐         │    LANZADERA SINIESTRADA   │      humo     │ (estrellas,
      │ │consola │ puerta  │    (cobertura central)     │               │  planeta,
      │ │abrazad.│◄──      │                            │               │  restos)
      │ └────────┘   humo  └────────────────────────────┘               │
 z=8  │       cajas            ⚡cables        cajas                    │
 z=0  └──────────────────────────[entrada]──────────────────────────────┘
                                vestíbulo (aparición del jugador)
```

### Jerarquía de la escena

```
Evacuacion
├── Entorno
│   ├── (suelo, techo, muros; el muro este tiene un hueco en z 8..32 para el ventanal)
│   ├── VentanalPanoramico   montantes, travesaño, CristalBlindado (collider invisible), PantallaVisualizacion ×2
│   ├── EspacioExterior      ~260 estrellas, Planeta + AnilloPlanetario, Restos ×12 (DebrisDrift)
│   ├── CabinaControlOeste   muros, techo, ventanas, luz
│   ├── PasarelaEmbarque     tablero (y=4.2), pilares, barandillas, RampaOeste / RampaEste (27,7°)
│   ├── BahiasCapsulas       muro de bahías, CapsulaB_Interior (suelo, paredes, asientos), marcos
│   └── Iluminacion          LuzEstelar (direccional, entra por el ventanal), lámparas AlertLight ×9,
│                            BalizaGiratoria ×5, Estroboscopios_Criticos (6, solo en fase crítica), DestelloExplosiones
├── Mobiliario               LanzaderaSiniestrada, contenedores y depósitos (cobertura)
├── Gameplay                 SecuenciaLanzamiento_B, Escotilla_CapsulaB, Terminal_Abrazaderas, Terminal_CeldaIgnicion,
│                            ConsolaLanzamiento_B (EscapePod), Escotilla_CapsulaA/C (fuera de servicio), ConsolaHangar
├── Seguridad                SensorLaser_RampaOeste, CamaraSeguridad (pasarela), Sector_Pasarela
├── Trampas                  CablesDanados_RampaEste, CablesDanados_Lanzadera
├── Botin                    Contenedor_Cabina (batería), Contenedor_Ventanal, batería bajo la pasarela, chatarra
├── EfectosTension           HumoEmergencia ×5 (con ShadowZone), HumoMotor, HumoExtra_FaseAviso, Chispas ×4
├── Enemigos                 DronElite_1/2 + Escuadron_Elite (anillo alrededor de la lanzadera), DronSeguridad_Pasarela
├── NavMesh                  RuntimeNavMeshBaker (incluye pasarela y rampas)
├── Zonas                    checkpoints ×4, avisos de la IA ×5, zonas de humo (ShadowZone)
├── Player                   + PlayerHealth (respawnOnDeath = false) · PlayerCamera + CameraShake (farClip 900)
├── HUD                      UI de las Fases 1–5 + CuentaAtras + EstadoLanzamiento + PanelResultados
├── GameSystems              AlertAmbience, EvacuationCountdown, EndingManager, ShipTremor, LevelIntro
└── EventSystem              EventSystem + StandaloneInputModule (necesario para pulsar los botones)
```

### Cómo se escapa

| Paso | Dónde | Cómo |
|---|---|---|
| 1. Liberar abrazaderas | Cabina oeste (`Terminal_Abrazaderas`) | Hackeo medio. Si fallas, el terminal se bloquea 8 s |
| 2. Cargar celda de ignición | Pasarela, junto a la B (`Terminal_CeldaIgnicion`) | Gasta **1 batería**; si no tienes, hackeo difícil |
| 3. Embarcar | La escotilla B se abre sola al completar los dos pasos | **[E]** en la consola de lanzamiento de la cápsula |

**Rutas alternativas a la pasarela**

- **Rampa oeste**: tiene un sensor láser que salta la alarma. Puedes apagarlo junto con la cámara de la pasarela
  desde la *Consola de evacuación* de la cabina (acción «Desactivar Sector Pasarela», hackeo medio, 45 s).
- **Rampa este**: no tiene láser, pero sí cables con descargas intermitentes (1,6 s activos y 2,4 s apagados).
- **Humo**: oculta al jugador (ShadowZone). Hay una columna de humo en la pasarela, junto a la cápsula A averiada,
  donde puedes esconderte del dron de seguridad.

En la cabina también puedes usar «Reiniciar protocolo de alerta» (hackeo difícil, −40 de tensión) para salir de la Alerta Roja.

### Efectos de tensión

| Fase de la cuenta atrás | Efectos |
|---|---|
| Inicio (5:00) | La nave pasa a **Alerta Amarilla** (si estaba en Normal). Vibración de cámara suave y en aumento. Balizas giratorias naranjas, humo y chispas |
| Aviso (≤ 2:00) | Contador naranja, más vibración y se activa `HumoExtra_FaseAviso` |
| Crítica (≤ 1:00) | Contador rojo que parpadea, estroboscopios rojos y vibración fuerte. En los últimos 10 s el número «late» |
| Todo el tiempo | `ShipTremor`: explosiones cada vez más seguidas, con sacudida, destello naranja, ráfagas de chispas y avisos de la IA. La IA anuncia los minutos que quedan (4, 3, 2, 1:30, 1:00, 0:30 y 0:10) |

---

## 4. Sistema de puntuación (`ScoreCalculator`, lógica pura y probada)

| Categoría | Máx. | Fórmula |
|---|---|---|
| **Sigilo** | 1500 | `1200 − 120·detecciones − 80·alarmas − 300·Alertas Rojas − 150·capturas − min(300, 10·ruido)` (entre 0 y 1200), **+300 si nunca hubo Alerta Roja** |
| **Recursos** | 1000 | Sobrantes (hasta 600) + eficiencia `400 × usados/fabricados` (200 si no fabricaste nada). Valores: chatarra 5 · componente 25 · batería 40 · dispositivo 60 |
| **Tiempo** | 1000 | 800 si la partida dura ≤ 25 min y baja de forma lineal hasta 0 a los 75 min; + 1 punto por segundo sobrante de la cuenta atrás (máx. 200) |
| **Creatividad** | 1000 | `150·tipos de dispositivo + min(300, 40·hackeos) + 60·sectores distintos + min(150, 25·PEM) + min(150, 30·señuelos) − 20·hackeos fallidos` |
| Penalización | — | −200 por muerte |
| Sin escape | — | Total × 0,5 |

**Rangos:** S ≥ 3800 · A ≥ 3000 · B ≥ 2200 · C ≥ 1400 · D.

**Logros destacados** que aparecen en la pantalla de resultados:

- Fantasma: nunca saltó la Alerta Roja.
- Ingeniero eficiente: usaste todo lo que fabricaste.
- Relámpago: escapaste en menos de 25 minutos.
- Arsenal completo: usaste los 3 tipos de dispositivo.

El **récord** (puntuación y rango) se guarda en `PlayerPrefs` y se muestra en el menú y en los resultados.

## 5. Finales alternativos (`EndingManager`)

| Final | Condición | Pantalla |
|---|---|---|
| **Óptimo** («El fantasma del Nexus-7») | Escapas, **0 Alertas Rojas**, recursos sobrantes ≥ 100 y como mucho 1 captura | VICTORIA en verde |
| **Táctico** («Escape a la fuerza») | Escapas, pero has activado la Alerta Roja, te has quedado sin recursos o te capturaron más veces | VICTORIA en ámbar. Con 4 o más hackeos y dispositivos usados, la narración es «escape a la fuerza»; con menos, «escape a la carrera» |
| **Crítico** («La nave te arrastra consigo») | La salud llega a 0 en el hangar **o** la cuenta atrás llega a 0 | DERROTA en rojo (narración distinta según la causa) |

Los textos de los finales se pueden editar en el Inspector de `GameSystems → EndingManager`.

La secuencia final es la siguiente:

1. Se detiene la cuenta atrás.
2. Se calcula la nota en ese momento.
3. Se congelan el jugador y los drones.
4. Cinemática: lanzamiento con sacudida, o fallo del reactor.
5. Fundido a negro.
6. Se guarda el récord.
7. Se libera el cursor y aparece la pantalla de resultados.

## 6. Pantalla de resultados y menú

- **`EndingUI`** muestra:
  - el título del final y la narración;
  - 4 barras animadas, una por categoría;
  - los logros destacados y las penalizaciones;
  - 18 estadísticas de la partida;
  - la puntuación, que cuenta hacia arriba, y el rango.

  Botones: **[R] Reiniciar partida** y **[M] Menú principal**. También se pueden pulsar con el ratón o con los
  botones A y B del mando.
- **`MainMenuUI`** ofrece **[Intro] Nueva partida** y el botón **Salir** (sin atajo de teclado, para no cerrar el juego por accidente), muestra el récord y los controles, y tiene un
  planeta girando de fondo.

---

## 7. Cómo probar el cierre completo

| Prueba | Cómo hacerla rápido | Resultado esperado |
|---|---|---|
| Final óptimo | Play en `Evacuacion`. Usa el humo, hackea las abrazaderas, recoge la batería de la cabina, cárgala y embarca sin que salte la Roja | VICTORIA verde, «Fantasma», rango alto |
| Final táctico | Cruza el láser de la rampa oeste varias veces o déjate ver por los élites hasta la Alerta Roja, y luego escapa | VICTORIA ámbar |
| Derrota por tiempo | En `GameSystems → EvacuationCountdown` pon `duration = 20` | A 0 s: fallo del reactor y DERROTA |
| Derrota por salud | Quédate encima de los cables de la rampa este | Salud 0, sin reaparecer, y DERROTA con el texto de «constantes vitales» |
| Botones | En la pantalla de resultados pulsa R y M (o haz clic) | R: SalaInicial con todo a cero. M: menú (con el récord actualizado) |
| Partida completa | Play en `MenuPrincipal` → Nueva partida → … → consola de mando | El Centro de Control carga `Evacuacion` y el inventario se conserva |
| Récord | Termina dos veces con notas distintas | Solo la más alta queda como récord («¡NUEVO RÉCORD!») |

**Pruebas automáticas.** La lógica de puntuación, los finales, el valor de los recursos, `RunStats` y el récord
tienen 26 comprobaciones nuevas. Junto con las 72 de las fases anteriores, todas pasan, y los 6 constructores y todos
los scripts compilan sin errores.

### Si algo falla

| Síntoma | Solución |
|---|---|
| Los botones de resultados no responden | Falta el `EventSystem` en la escena (el constructor lo crea) o falta *Active Input Handling = Both* |
| «La escena X no está en Build Settings» | Ejecuta **Fase 6 → Ordenar Build Settings** |
| La consola de mando no lleva a la evacuación | Ejecuta **Fase 6 → Conectar salida de la Fase 5** |
| El planeta o las estrellas no se ven | Comprueba el *Far Clip Plane* de `PlayerCamera` (900) |
| El humo se ve como cuadrados rosas | En URP, cambia el material de las partículas por uno de *Particles/Unlit* (el script usa `Sprites/Default`, que funciona en Built-in y en URP) |
| En el hangar el tiempo de la partida sale casi 0 | Has jugado `Evacuacion` directamente: el cronómetro arranca en la primera escena que cargas. Es lo esperado |

---

## 8. Scripts de la Fase 6

| Script | Carpeta | Función |
|---|---|---|
| `RunStats` | Core | Estadísticas de toda la partida (estático) |
| `GameFlow` | Core | Nueva partida, reiniciar, menú, salir y récord |
| `ScoreCalculator` | Gameplay | Fórmulas, rango y elección del final (sin dependencias de Unity) |
| `EndingManager` | Gameplay | Detecta victoria y derrota, cinemática y resultados |
| `EvacuationCountdown` | Gameplay | Cuenta atrás con 3 fases de tensión y avisos de la IA |
| `LaunchSequence` / `LaunchStepTerminal` | Gameplay | Pasos de lanzamiento (objeto o hackeo), escotilla y baliza |
| `EscapePod` | Gameplay | Consola de lanzamiento (`Escape()`); sin secuencia queda «fuera de servicio» |
| `CameraShake` | Environment | Vibración de cámara por «trauma» |
| `ShipTremor` | Environment | Explosiones periódicas cada vez más seguidas |
| `SmokeEmitter` | Environment | Columnas de humo sin assets (textura generada por código) |
| `RotatingBeacon` / `DebrisDrift` | Environment | Balizas giratorias y restos a la deriva |
| `CountdownHUD` | UI | Contador «AUTODESTRUCCIÓN mm:ss» y estado de la cápsula B |
| `EndingUI` | UI | Pantalla de victoria o derrota con resultados |
| `MainMenuUI` | UI | Menú principal |
| `EvacuacionBuilder` | Editor | Construye la escena final y el menú, y deja el juego enlazado |

**Cambios en scripts existentes** (para registrar estadísticas):

- `CraftingSystem`, `DeviceQuickbar`, `EmpDevice`, `HackTerminal`, `HackMinigameUI`, `SecuritySector`, `PickupItem`,
  `LootContainer` y `EnemyPatrol` llaman a `RunStats.Record*`.
- `PlayerHealth` añade `respawnOnDeath` y el evento `PlayerDied`.
- `ConsoleUI` guarda el inventario antes de cambiar de escena.
- `AlertEventBridge` no cuenta las muertes como capturas.
- `CentroControlBuilder` hace `internal` sus ayudantes para que se puedan reutilizar.

---

## 9. Revisión de código de todas las fases (correcciones aplicadas)

Tres revisores independientes revisaron el código de las Fases 1–6. Estas son las correcciones aplicadas:

| Fase | Problema | Corrección |
|---|---|---|
| 2 | El checkpoint del conducto te hacía reaparecer delante del dron B | El checkpoint está ahora en la salida norte del conducto, dentro de la sombra |
| 3 | Si llegabas sin batería sobrante, la compuerta principal no podía abrirse (3/4 de energía) | El contenedor del ala este trae 1 batería y 2 circuitos |
| 3 | El checkpoint del ala oeste estaba a 2 m del dron G2 | Movido a su espalda |
| 1–6 | Capturas en cadena justo al reaparecer | Tras reaparecer, los drones no pueden verte durante 2,5 s |
| 1–6 | Si te capturaban dentro de una zona de sombra, quedabas «en sombra» para siempre | El contador de sombras se reinicia al reaparecer |
| 1–6 | Las puertas abiertas seguían bloqueando el paso de los drones | Las puertas ya no se hornean en el NavMesh: un NavMeshObstacle que se mueve con ellas les abre o cierra el paso |
| 1 | El tutorial cambiaba el objetivo «Cruza la puerta» por el antiguo | Solo pone el objetivo inicial si no había otro |
| 4–5 | Un PEM acortaba un apagado más largo hecho con un hackeo | Se queda el apagado más largo |
| 5 | Al final de la Fase 5, el panel de la consola reaparecía y se quedaba bloqueado | Corregido; además, el jugador y los drones se congelan durante el cierre de la fase |
| 1–5 | Correr sin parar llevaba la nave sola a la Alerta Roja | El ruido ya no frena la disipación de la tensión y cuenta como mucho una vez cada 0,6 s |
| 2–5 | Un mismo encuentro (o los dos drones de élite a la vez) contaba varias detecciones | Una detección por encuentro (6 s) |
| 5 | En Alerta Roja, los drones de élite corrían más que tú | Su velocidad de persecución queda justo por debajo de la tuya |
| 6 | No se podía hacer clic en los botones de resultados ni del menú | Los botones aceptan el ratón |
| 6 | Un hackeo a medias o una reaparición seguían activos tras el final | El final cierra el hackeo y la consola y desactiva los controles del jugador |
| 6 | La creatividad se podía «farmear» repitiendo el mismo sector | Solo cuentan los sectores distintos |
| 6 | Correr hundía el sigilo | La penalización por ruido tiene un tope de 300 |
| 6 | Láser y cables de las rampas se podían rodear | Pretiles a pie de cada rampa |
| 6 | La cuenta atrás tapaba las pistas del HUD | Movida arriba a la derecha; los avisos urgentes se muestran al momento |

**Verificación:**

- Compilan los 104 scripts, incluidos los 6 constructores.
- Las 103 comprobaciones automáticas pasan: inventario 14, energía 14, Fase 4 18, Fase 5 26 y Fase 6 31.

**Mejoras menores pendientes** (no impiden jugar):

- Al reaparecer, el jugador cae 1,5 m.
- Las puertas se pueden cerrar sobre el jugador.
- Un PEM sobre un sistema que ya estaba apagado cuenta como «objetivo alcanzado».
