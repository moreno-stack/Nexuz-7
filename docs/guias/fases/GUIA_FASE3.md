# Escape Espacial: Nexus-7 — Fase 3 (Sala de Energía)

Requisitos: Fases 1 y 2 funcionando. No hace falta instalar paquetes nuevos.

---

## 1. Puesta en marcha

1. Copia `Assets/_Nexus7` encima del de tu proyecto.
2. Menú **Nexus-7 → Fase 3 → Construir Sala de Energía** → **Play**.

El constructor también:
- añade `SalaEnergia` a *Build Settings*,
- **enlaza el Almacén con la Sala de Energía**: pone `SalaEnergia` como destino de la salida del montacargas y
  añade **una batería extra** al contenedor central del Almacén (para que llegues con una batería que conectar).

**El inventario ahora viaja entre escenas** (`GameSession`): las baterías, circuitos, chatarra y la llave del
Almacén llegan a la Sala de Energía. Si juegas la Sala de Energía directamente, el jugador empieza con un
equipo de prueba (1 batería, 3 circuitos, 6 de chatarra y la llave hidráulica).

---

## 2. Scripts nuevos y modificados

```
Scripts/
├── Power/      PowerGrid.cs        Red eléctrica de la escena (suma de fuentes, estado, ambiente, objetivo)
│               PowerSource.cs      Base de toda fuente de energía (interactuable)
│               BatterySocket.cs    Fuente: consume 1 batería del inventario
│               PowerConsole.cs     Fuente: interruptor / consola dañada / reactor (requisitos configurables)
│               PowerConsumer.cs    Desbloquea puertas (o dispara eventos) a partir de N bloques
│               PoweredLight.cs     Luz dinámica: emergencia (rojo) → parcial (ámbar) → restaurada (blanco)
├── Hazards/    ElectricHazard.cs   Circuito dañado: daño o aturdimiento, constante o intermitente
│               ElectricBarrier.cs  Barrera de haces: bloquea y daña; hackeable
│               RepairPanel.cs      Aísla un circuito dañado gastando chatarra
│               ElectricArc.cs      Rayo procedural (LineRenderer en zigzag)
│               SparkEmitter.cs     Chispas (partículas + destello)
├── Security/   IHackable.cs        Contrato de lo que se puede hackear
│               SecurityCamera.cs   Cámara que barre, sigue y da la ALARMA; hackeable
│               HackTerminal.cs     Gasta recursos y desactiva temporalmente cámaras/barreras
├── Player/     PlayerHealth.cs     Integridad, regeneración, aturdimiento, muerte → reaparición
├── Core/       GameSession.cs      Inventario entre escenas
│               AlarmSystem.cs      Bus de alarmas (cámaras → drones)
├── Inventory/  ItemRequirementUtil.cs  Textos de requisitos ("Circuito 1/1") y acceso al inventario del jugador
└── UI/         HealthUI.cs, PowerStatusUI.cs
```

Cambios en scripts existentes (compatibles con las fases anteriores):

| Script | Cambio |
|---|---|
| `PlayerController` | `AddInputLock / RemoveInputLock`: varios sistemas pueden bloquear el control a la vez (aturdimiento + hackeo) sin pisarse |
| `PlayerRespawn` | `Respawn(mensaje)` genérico: se usa para la captura y para la muerte por descarga |
| `InventorySystem` | Carga el inventario de `GameSession` si vienes de otra escena |
| `SceneExitTrigger` | Guarda el inventario antes de cambiar de escena |
| `EnemyVision` | Lista global `All` (el HUD tiene en cuenta también las cámaras) |
| `EnemyPatrol` | Responde a las alarmas: acude corriendo a la posición señalada |
| `AlmacenCargaBuilder` | Ayudantes reutilizables, batería extra y salida hacia `SalaEnergia` |

---

## 3. Sistema de activación eléctrica

```
 FUENTES (PowerSource)                     PowerGrid                    CONSUMIDORES
 ┌──────────────────────┐   Activate()   ┌────────────────┐  PowerChanged  ┌───────────────────────┐
 │ BatterySocket   (+1) │ ─────────────► │ CurrentPower   │ ─────────────► │ PowerConsumer (puertas)│
 │ PowerConsole    (+1) │                │ 0..MaxPower    │                │ PoweredLight (luces)   │
 │  · interruptor       │                │ Offline/Partial│                │ ElectricHazard (trampas│
 │  · reactor (req. 3)  │                │ /Restored      │                │   con requisito)       │
 └──────────────────────┘                └────────────────┘                │ PowerStatusUI, objetivo│
                                                                            └───────────────────────┘
```

| Fuente | Dónde | Requisito | Aporta |
|---|---|---|---|
| Zócalo A | Junto a la entrada | 1 batería (la que traes del Almacén) | +1 |
| Interruptor general | Ala este (tras S1) | Cruzar el cable dañado que aturde | +1 |
| Zócalo B | Ala oeste (vigilado por el dron G2) | 1 batería (contenedor del ala oeste) | +1 |
| Consola del reactor | Plataforma norte (dron G1 + cámara) | **3 bloques en la red** + 1 Circuito de control | +1 |

| Puerta | Energía necesaria |
|---|---|
| S1 — Secundaria, ala este | 1 bloque |
| S2 — Acceso a la plataforma del reactor | 2 bloques |
| M — Compuerta principal (salida) | 4 bloques (100 %) |

**Iluminación dinámica:** todas las lámparas llevan `PoweredLight`. Sin energía se ven rojas, tenues y
parpadeantes. Con energía parcial pasan a ámbar y son más intensas cuantos más bloques haya. Al 100 % se ven
blancas. Cada vez que sube el nivel hacen un breve parpadeo de arranque. `PowerGrid` interpola también la luz
ambiental y la niebla, y el núcleo del reactor cambia de color con la red.

El **objetivo en pantalla se genera solo** con las fuentes que faltan, por ejemplo:
«Restaura la energía (2/4): Zócalo de batería (ala oeste) · Consola del reactor (plataforma norte)».

---

## 4. Obstáculos y rutas alternativas

### Circuitos dañados (`ElectricHazard`)
| Trampa | Efecto | Patrón | Cómo superarla |
|---|---|---|---|
| Cable ante la puerta S1 | 25 de daño | Intermitente (1.6 s activo / 2.4 s apagado) | Cruzar cuando se apaga (las chispas avisan 0.8 s antes) o **aislarlo con 2 de chatarra** |
| Cable del ala este | Aturde 1.5 s + 10 de daño | Intermitente | Igual: cronometrar o aislar con chatarra |
| Arco entre transformadores | 20 de daño | Constante, **solo con energía ≥ 1** | Rodearlo por los pasillos laterales |

Cada descarga **hace ruido (6 m)**, así que puede atraer a los drones. La integridad se regenera a los
5 segundos sin recibir daño. Si llega a 0, reapareces en el último punto de control y conservas el inventario
y la energía restaurada.

### Tres rutas al ala oeste
1. **Barrera eléctrica**: hackearla gastando 1 circuito (25 s). Antes de reactivarse parpadea, y si estás dentro espera a que salgas.
2. **Foso de mantenimiento** (desnivel bajo el suelo): una escotilla atornillada que se abre con la **llave
   hidráulica** del Almacén. Bajas 1.4 m y cruzas por debajo del muro, agachado.
3. **Pasarelas superiores** (y = 4.2 m, encima de los tabiques): se sube por la rampa del ala este (tras S1) y
   se baja por la rampa del ala oeste o **dejándote caer** por el hueco de la barandilla sobre unas cajas.

Las **barandillas son macizas y miden 1 m**: agachado (1 m de altura) los drones de abajo no te ven, de pie sí.
Desde la pasarela norte hay otro hueco para **caer directamente en la plataforma del reactor** sin pasar por S2
ni por delante del dron G1.

---

## 5. Drones de guardia y seguridad

| Elemento | Posición | Comportamiento |
|---|---|---|
| Dron G1 | Delante de la rampa del reactor | Va y viene, y se para 3 s mirando hacia el sur de la sala |
| Dron G2 | Ala oeste | Recorre un bucle; se para a vigilar el zócalo B y la rampa/salida del foso |
| Cámara C1 | Poste en la plataforma del reactor | Barre ±50° sobre la consola. Si te detecta: **ALARMA** → los drones en 30 m acuden corriendo |
| Terminal H1 | Junto a la barrera (sur) | 1 circuito → barrera desactivada 25 s |
| Terminal H2 | Pasarela norte | 1 circuito → cámara desactivada 30 s (baja la cabeza y apaga el foco) |

**Hackear no es instantáneo:** durante 2.5 s te quedas inmóvil con una barra de progreso. Si te capturan a
mitad, el hackeo se cancela y no gastas el circuito. El HUD (arriba a la izquierda) muestra la cuenta atrás de
cada sistema desactivado y la pone en rojo cuando le quedan menos de 5 s.

**Balance de recursos:** traes 3 circuitos del Almacén y encuentras 1 más en el ala este, 4 en total. Necesitas
1 para la consola del reactor y te quedan hasta 3 para hackear. La chatarra (12 del Almacén + 10 aquí) da
para aislar los dos cables.

---

## 6. Montaje manual de cada pieza

- **Red**: GameObject `RedElectrica` + `PowerGrid` (*Max Power* = número de bloques).
- **Zócalo**: objeto con collider + `BatterySocket` (*Battery Item* = `Item_Bateria`, *Battery Visual* = modelo oculto).
- **Consola / interruptor**: objeto con collider + `PowerConsole` (*Required Grid Power*, *Required Items*).
- **Puerta con energía**: `DoorController` (*Start Locked*) + `PowerConsumer` en el mismo objeto (*Required Power*, *Doors* = la propia puerta).
- **Luz dinámica**: Light + `PoweredLight` (opcional *Emissive Renderer* = la lámpara).
- **Circuito dañado**: GameObject en capa *Ignore Raycast* con BoxCollider *Is Trigger* + `ElectricHazard`.
  Como hijos, uno o varios `ElectricArc` (LineRenderer con material unlit), `SparkEmitter` y una Light.
- **Barrera**: raíz con BoxCollider trigger (≈1 m de grosor) + `ElectricBarrier`. Hijo "Bloqueo" con BoxCollider sólido → *Blocking Collider*.
- **Cámara**: raíz + `EnemyVision` + `SecurityCamera` → hijo "Pivote" (inclinado en X) → "Lente" (*Eye*) → Spot Light.
- **Terminal de hackeo**: objeto con collider + `HackTerminal`. En *Targets* arrastra el componente `ElectricBarrier` o `SecurityCamera`.
- **Jugador**: además de lo anterior, `PlayerHealth`.

---

## 7. Ajustes rápidos (Inspector)

| Más fácil | Más difícil |
|---|---|
| `ElectricHazard.damage` 15 | 35 |
| `ElectricHazard.offTime` 3.5 | 1.5 |
| `HackTerminal.disableDuration` 40 | 15 |
| `HackTerminal.hackTime` 1.5 | 4 |
| `SecurityCamera.sweepSpeed` 15 | 35 |
| `PlayerHealth.regenDelay` 3 | 8 |

## 8. Pendiente para la Fase 4
- Puente de mando (la salida muestra «FIN DE LA FASE 3»; basta con poner el nombre de la escena en `SceneExitTrigger`).
- Guardado en disco a partir de `GameSession` (ids de `ItemData` + estado de la red).
