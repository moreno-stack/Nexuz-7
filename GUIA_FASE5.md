# Escape Espacial: Nexus-7 — Fase 5 (Centro de Control)

Requisitos: Fases 1–4 funcionando. Sin paquetes nuevos.

---

## 1. Puesta en marcha

1. Copia `Assets/_Nexus7` encima del de tu proyecto.
2. Menú **Nexus-7 → Fase 5 → Construir Centro de Control** → **Play**.

El constructor también enlaza la salida del Laboratorio con el Centro de Control. Si juegas esta escena
directamente, empiezas con un equipo de prueba (recursos y un dispositivo de cada tipo).

> **Importante:** el sistema de alertas funciona **desde la Fase 1**, sin colocar nada en las escenas.
> Se crea solo al pulsar Play. Para que tu historial cuente, juega la partida seguida desde el principio.

---

## 2. Sistema de alertas dinámico

### Estructura

```
                    SUCESOS (de cualquier fase)                            CONSUMIDORES
 AlarmSystem.AlarmRaised ─────── +22 ─┐                          ┌─► EnemyVision   (detección, alcance)
 EnemyPatrol.PlayerSpotted ───── +15 ─┤                          ├─► EnemyPatrol   (velocidad, oído, CAZA en Roja)
 PlayerRespawn.PlayerCaught ──── +10 ─┤    AlertEventBridge      ├─► SecurityCamera (barrido, radio de alarma)
 Hackeo fallido ──────────────── +18 ─┼──► (DontDestroyOnLoad) ──┤   HackTerminal / SecuritySector (duración)
 Error de hackeo ─────────────── +5  ─┤          │               ├─► LockdownDoor  (sellado en Roja)
 Correr (por paso) ──────────── +0.35 ┤          ▼               ├─► AlertLight / AlertScreen / Holograma / Ambiente
 Detonación PEM ──────────────── +4  ─┘     AlertSystem          └─► AlertHUD, consolas (informe de incidentes)
                                      TENSIÓN 0..100 → NIVEL
```

| Script | Función |
|---|---|
| `Core/AlertSystem.cs` | Estático: tensión, nivel, perfiles, historial (`Stats`), última posición conocida del jugador |
| `Gameplay/AlertEventBridge.cs` | Se crea solo; conecta los sucesos con el sistema y hace que la tensión se disipe |

### Niveles (con histéresis para que no "parpadee")

| Nivel | Entra con | Sale con | Efectos |
|---|---|---|---|
| **Normal** | — | — | Valores base |
| **Alerta Amarilla** | tensión ≥ 35 | < 20 → Normal | Detección ×1.25 · visión ×1.15 · oído ×1.2 · velocidad ×1.15 · cámaras ×1.3 · hackeos duran ×0.8 |
| **Alerta Roja** | tensión ≥ 70 | < 50 **y** ≥ 25 s en Roja | Detección ×1.6 · visión ×1.3 · oído ×1.5 · velocidad ×1.3 · cámaras ×1.7 y alarma ×1.5 · hackeos duran ×0.5 · **drones de élite cazan** · **compuertas blindadas selladas** · hackeos más largos y con menos tiempo |

La tensión se **disipa con el tiempo** (1.2/s en Normal, 0.8/s en Amarilla, 0.5/s en Roja) y se detiene 8 s tras
cada suceso. Un ejemplo: 2 errores de hackeo (10) + 1 alarma (32) aún es Normal; si te detectan y fallas un
hackeo (65) pasa a Amarilla; otra alarma (87) la lleva a **Roja**.

Usarlo desde tus scripts:
```csharp
AlertSystem.Report(AlertEvent.AlarmRaised, posicion);  // suceso con peso predefinido
AlertSystem.ReportCustom(12f);                          // suceso con tensión a medida
AlertSystem.Level / AlertSystem.Heat / AlertSystem.Profile.speed
AlertSystem.LevelChanged += (antes, ahora) => { ... };
```

---

## 3. Terminales de acceso y hackeo avanzado

| Script | Función |
|---|---|
| `Security/ControlConsole.cs` | Consola con lista de acciones configurable en el Inspector |
| `Security/SecuritySector.cs` | Agrupa cámaras, láseres y barreras para apagarlos a la vez |
| `Security/LockdownDoor.cs` | Compuerta blindada: se autoriza con hackeo y se sella en Alerta Roja |
| `Security/HackSequence.cs` | Lógica pura del minijuego (probada por separado) |
| `UI/ConsoleUI.cs`, `UI/HackMinigameUI.cs`, `UI/ShipStatusReport.cs` | Menú, minijuego e informe |

### Acciones de consola (`ConsoleActionType`)
| Acción | Qué hace |
|---|---|
| `ShowInfo` | Informe en vivo: nivel, tensión, **historial de incidentes de toda la partida**, sectores, drones activos |
| `DisableSector` | Apaga todos los sistemas del sector (duración × perfil de alerta) |
| `UnlockDoor` | Autoriza una compuerta blindada |
| `ReduceAlert` | "Reiniciar protocolo de alerta": −40 de tensión (sale de la Roja sin esperar) |
| `CompletePhase` | Acción final: carga `nextScene` o muestra el fin de fase |

### Minijuego de hackeo
Teclea la secuencia con **[W][A][S][D]** o con las flechas (mando: Y · X · A · B) antes de que se acabe el tiempo.

| Dificultad | Códigos | Tiempo | Errores permitidos | Penalización por error |
|---|---|---|---|---|
| Fácil | 5 | 10 s | 3 | −2 s |
| Media | 7 | 12 s | 3 | −3 s |
| Difícil | 9 | 14 s | 2 | −4 s |

- **Cada error** resta tiempo, suena un pitido (ruido de 6 m) y suma **+5 de tensión**.
- **Si fallas**, la consola se **bloquea 10 s**, salta una alarma sonora (ruido de 10 m) y suma **+18 de tensión**.
- En **Alerta Roja** la secuencia tiene 1 código más y un 20 % menos de tiempo.
- **[Q] aborta** sin sumar tensión (la consola se bloquea igualmente).
- El juego **no se pausa**: los drones siguen moviéndose mientras hackeas.

---

## 4. Drones de élite y patrullas reforzadas

| Dron | Comportamiento |
|---|---|
| **Élite ×2** (negros con blindaje) | Visión de 18 m y 100°, detección 1.5, velocidad de persecución 5.2 (casi como tu carrera), oído ×1.4. Patrullan **en pareja desfasados** (empiezan en lados opuestos del anillo). Con `aggressiveOnRed`: en **Alerta Roja** acuden cada 6 s a tu última posición conocida |
| **Escuadrón** (`DroneSquad`) | Si un miembro te persigue, **el otro acude** a tu posición |
| Dron de seguridad (ala oeste) | Vigila la entrada del ala y el control de compuertas |
| Dron de investigación (ala este) | Revisa el banco de trabajo (si fabricaste hace poco, alarma) |

**Información compartida:** cualquier dron o cámara que te vea actualiza `AlertSystem.LastKnownPlayerPosition`.
Los élite usan esa posición para cazarte en Alerta Roja.

---

## 5. El nivel

| Zona | Contenido |
|---|---|
| Entrada | Mesas de monitorización con pantallas de alerta |
| Sala central | **Holograma** de la nave (cambia de color y velocidad con la alerta), **consola de seguridad central** (desactivar sectores, reiniciar alerta), terminal de información. Patrulla de élite |
| Ala oeste | Entrada con **láser triple y cámara (Sector Oeste)**, dron de seguridad, **control de compuertas** (autorizar la compuerta: hackeo difícil). Ruta alternativa: **pasarela** sobre el muro y caída sobre unas cajas |
| Ala este | **Banco de trabajo** (puedes seguir fabricando), suministros (circuitos, chatarra, batería, cristal), cámara (Sector Este), dron de investigación |
| Antesala del puente | Láser doble y cámara (Sector Puente) ante la **compuerta blindada** |
| Puente | **Consola de mando del capitán** → «Retomar el control de la nave» (hackeo difícil) → **FIN DE LA FASE 5** |

**Iluminación dramática:** la sala está casi a oscuras y la iluminan sobre todo las **pantallas**: cinco gigantes en
el muro del puente, tres laterales, los monitores y las consolas, más el holograma. Todo sigue el nivel de alerta:
azul y estable en Normal, ámbar pulsando en Amarilla, rojo destellando con interferencias en Roja. Las
lámparas, la luz ambiental y la niebla cambian a la vez.

**Varias formas de llegar a la compuerta:**
1. Hackear la consola central para **apagar el Sector Oeste**, entrar al ala y autorizar la compuerta.
2. Subir por la **pasarela** y caer dentro del ala sin pasar por el láser.
3. Usar los **dispositivos de la Fase 4**: un PEM apaga láser y cámara, el camuflaje engaña al láser y un señuelo distrae al dron.

En la antesala del puente vuelve a pasar lo mismo con el Sector Puente.

---

## 6. Ajustes rápidos

| Parámetro | Dónde |
|---|---|
| Umbrales, salidas, pesos y disipación | Constantes y `Weight()` en `AlertSystem.cs` |
| Multiplicadores por nivel | Array `Profiles` en `AlertSystem.cs` |
| Dificultad de cada acción, duración y tensión que resta | Inspector de cada `ControlConsole` → *Actions* |
| Longitud, tiempo y errores del minijuego | `HackSequence.GetParams` |
| Caza: intervalo y memoria | `EnemyPatrol.huntInterval` / `huntMemory` |
| Bloqueo de la consola tras un fallo | `ControlConsole.lockoutTime` |

## 7. Pendiente para la Fase 6
- Escena final / secuencia de huida (pon su nombre en `nextScene` de la acción final de la consola de mando).
- Guardado en disco (inventario + `AlertSystem.Stats`).
