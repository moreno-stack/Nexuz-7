# Escape Espacial: Nexus-7 — Fase 2 (Almacén de Carga)

Requisitos: el proyecto de la Fase 1 funcionando (Active Input Handling = *Both*, TMP Essentials importado).
No hace falta instalar ningún paquete nuevo: el NavMesh se genera con la API integrada de Unity.

---

## 1. Puesta en marcha

1. Copia la carpeta `Assets/_Nexus7` actualizada encima de la de tu proyecto (sustituye los archivos).
   Cambian dos archivos de la Fase 1: `DoorController.cs` (ahora admite requisitos de objetos) y
   `SalaInicialBuilder.cs` (sus ayudantes pasan a `internal` para reutilizarlos). La Fase 1 funciona igual.
2. Menú **Nexus-7 → Fase 2 → Construir Almacén de Carga**.
3. Pulsa **Play** (se abre `AlmacenCarga.unity`).

El constructor además:
- crea los 4 tipos de objeto en `Assets/_Nexus7/Data/Items/`,
- añade `AlmacenCarga` a *Build Settings*,
- **conecta la Fase 1**: añade un `SceneExitTrigger` a `Trigger_FinFase1`, así que al terminar la
  Sala Inicial se carga el almacén. (Si construyes la Fase 1 de nuevo, usa
  *Nexus-7 → Fase 2 → Conectar salida de la Fase 1* para volver a enlazarlas).

---

## 2. Nuevos scripts y dónde van

```
Scripts/
├── Inventory/   ItemData.cs            ScriptableObject: definición de un tipo de objeto
│                ItemStack.cs           struct ItemStack (objeto+cantidad) y class InventorySlot
│                InventorySystem.cs     → Player
│                PickupItem.cs          → objeto suelto con collider
│                LootContainer.cs       → raíz de un contenedor
├── Enemies/     PatrolRoute.cs         → GameObject vacío; sus hijos son los waypoints
│                Waypoint.cs            → (opcional) en cada hijo: espera y orientación
│                EnemyVision.cs         → raíz del dron
│                EnemyPatrol.cs         → raíz del dron (con NavMeshAgent desactivado)
├── Stealth/     PlayerStealth.cs       → Player
│                ShadowZone.cs          → trigger en zonas oscuras
├── Gameplay/    PlayerRespawn.cs       → Player
│                Checkpoint.cs          → trigger
├── Core/        RuntimeNavMeshBaker.cs → GameObject vacío "NavMesh"
│                SceneExitTrigger.cs    → trigger de salida de nivel
├── UI/          PickupNotificationUI.cs, InventoryUI.cs, StealthIndicatorUI.cs → Canvas HUD
├── Tutorial/    LevelIntro.cs          → GameSystems
└── Editor/      AlmacenCargaBuilder.cs (constructor de la escena)
```

**Regla para los triggers:** ponlos en la capa **Ignore Raycast**. Así no tapan la visión de los drones,
no bloquean el raycast de interacción y no se meten en el NavMesh.

---

## 3. Sistema de inventario

### Estructuras de datos

| Tipo | Qué es |
|---|---|
| `ItemData` (ScriptableObject) | El **tipo** de objeto: id, nombre, descripción, categoría, icono, `MaxStack`, `IsKeyItem`, color de UI. Hay un asset por tipo; todas las baterías apuntan a `Item_Bateria`. |
| `ItemCategory` (enum) | `Part` (piezas/chatarra), `Battery`, `Component`, `Tool`. |
| `ItemStack` (struct) | (objeto, cantidad). Se usa en contenidos de contenedores y requisitos de puertas. |
| `InventorySlot` (class) | Un espacio: un tipo de objeto y su cantidad (≤ `MaxStack`). |
| `InventorySystem` | `List<InventorySlot>` con `capacity` espacios (12 por defecto). |

Objetos creados:

| Asset | Nombre | Categoría | MaxStack |
|---|---|---|---|
| Item_Bateria | Batería de iones | Battery | 5 |
| Item_Chatarra | Chatarra metálica | Part | 20 |
| Item_LlaveHidraulica | Llave hidráulica | Tool (objeto clave) | 1 |
| Item_Circuito | Circuito de control | Component | 10 |

Para crear otro: *clic derecho en Project → Create → Nexus-7 → Objeto*.

### API

```csharp
int  Add(item, n)            // añade lo que quepa; devuelve cuántas unidades entraron
bool CanAdd(item, n)         // ¿caben todas?
int  Remove(item, n)         // quita hasta n; devuelve cuántas quitó
bool TryRemove(item, n)      // todo o nada
bool TryConsume(requisitos)  // consume una lista de ItemStack de forma atómica
int  GetCount(item) / GetCountById("battery_ion")
bool Has(item, n) / HasAll(requisitos)
IEnumerable<ItemStack> GetByCategory(ItemCategory.Battery)
List<ItemStack> GetSummary() // un total por objeto, ordenado por categoría
// Eventos: ItemAdded(item, n), ItemRemoved(item, n), Changed()
```

`Add` rellena primero las pilas que ya existen y después abre espacios nuevos, así que 7 baterías
(MaxStack 5) ocupan 2 espacios. La lógica se ha probado con 14 casos: apilar, inventario lleno,
consumo atómico, eventos, etc.

### Recolección
- **`PickupItem`**: objeto suelto. Al pulsar `[E]` se añade al inventario y desaparece. Si no cabe
  entero, el resto se queda en el suelo. La chatarra hace algo de ruido (radio 3 m).
- **`LootContainer`**: abre la tapa y pasa al inventario todo lo que quepa. **Abrirlo hace ruido (5 m).**
- **Notificación visual**: `PickupNotificationUI` está suscrita a los eventos del inventario, así que
  cualquier cambio muestra "+1 Batería de iones" en el lateral derecho sin que nadie la llame.
  Para avisos libres: `PickupNotificationUI.Notify("texto", color)`.
- **`[Tab]` / `[I]`**: abre el panel del inventario agrupado por categoría. No pausa el juego.

### Puertas con requisitos (rutas bloqueadas)
`DoorController` tiene ahora una lista **Required Items** y la opción **Consume Items**:
- Montacargas: necesita 2 baterías y las **consume**.
- Rejilla del conducto: necesita la llave hidráulica y **no** la consume.

El aviso en pantalla muestra el progreso: «Montacargas sin energía (Batería de iones 1/2)».

---

## 4. Enemigos: dron de patrulla

### Máquina de estados (`EnemyPatrol`)

```
PATROL ──ve al jugador (alerta ≥ 0.35) u oye ruido──► INVESTIGATE ──llega──► SEARCH (4 s girando) ──► PATROL
   └──────────────── alerta = 1 ────────────────► CHASE ── lo pierde 2.5 s ──► INVESTIGATE
                                                     └── distancia ≤ 1.3 m ──► CAPTURA
```

| Estado | Velocidad | Comportamiento |
|---|---|---|
| Patrol | 1.8 m/s | Recorre los waypoints; espera en cada uno y puede girarse a vigilar |
| Investigate | 2.6 m/s | Va al último punto donde te vio o donde oyó el ruido |
| Search | — | Gira sobre sí mismo mirando alrededor |
| Chase | 4.4 m/s | Te persigue. **Corriendo (6 m/s) puedes escapar, pero haces ruido** |

### Visión (`EnemyVision`)
- Cono de 80° y 12 m, con comprobación de línea de visión hacia tu cabeza y tu pecho
  (las estanterías y las cajas tapan).
- **Medidor de alerta 0→1**: sube según tu visibilidad × la cercanía, y baja 0.25/s cuando deja de verte.
- A menos de 1.6 m te detecta aunque estés fuera del cono.
- **El foco de luz del dron es su cono de visión**: azul = tranquilo, amarillo = sospecha, rojo = te persigue.

### Oído
Se suscribe al `NoiseSystem` de la Fase 1. Si un ruido le llega dentro de su radio, va a investigarlo:
- caminar: 6 m · correr: 12 m · agachado: 1.5 m
- abrir un contenedor: 5 m · abrir una puerta: 8 m · recoger chatarra: 3 m

### Sigilo (`PlayerStealth`)
Visibilidad = 1 × agachado 0.5 × quieto 0.8 × corriendo 1.4 × en sombra 0.45.
Por debajo de 0.3 cuentas como **OCULTO** (agachado en una `ShadowZone`): solo te ven si están casi encima.
El indicador de la esquina superior derecha muestra OCULTO / ? SOSPECHA / ! DETECTADO junto con una barra de alerta.

### Captura
`PlayerRespawn`: fundido a negro, reapareces en el último `Checkpoint` y los drones vuelven a su
posición inicial. **El inventario se conserva.**

### Crear un enemigo a mano
1. GameObject vacío `Dron` → `CapsuleCollider` (altura 1.9) + `NavMeshAgent` (**desmarcado**) +
   `EnemyVision` + `EnemyPatrol` + `AudioSource`.
2. Hijo `Ojo` a 1.6 m de altura mirando al frente, con una **Spot Light** hija → campos *Eye* y *Vision Light*.
3. GameObject vacío `Ruta` + `PatrolRoute`; hijos vacíos `WP_0`, `WP_1`… (añade `Waypoint` para ajustar
   la espera y la orientación) → arrástralo a *Route*.
4. Asegúrate de que hay un `RuntimeNavMeshBaker` en la escena que cubra la zona, con el jugador y los
   drones en *Ignore Roots*.

---

## 5. El nivel

| Elemento | Ubicación | Diseño |
|---|---|---|
| Vestíbulo (inicio) | Sur | Zona segura para leer la situación; registro de seguridad con pistas |
| Estanterías | 6 bloques de 3.6 m de alto | Tapan la visión. Las centrales tienen un hueco de paso a media altura (z≈15) |
| **Dron A** | Bucle por los pasillos x=±6 | Gira a vigilar en las esquinas; línea amarilla en el suelo marca su ruta |
| **Dron B** | Vaivén por el pasillo norte | Se detiene 3 s mirando hacia los pasillos oeste y este |
| Batería 1 | Pasillo central (suelo) | Al descubierto; hay que cronometrar los movimientos |
| Llave hidráulica | Contenedor del pasillo este | Pasillo este sin salida por el norte (**derrumbe = ruta bloqueada**) |
| **Conducto de ventilación** | Lateral oeste, 1.2 m de alto | **Ruta alternativa**: rejilla + llave → avanzas agachado, invisible para los drones (no caben) |
| Batería 2 | Contenedor oeste, salida del conducto | Vigilado por el Dron B desde el norte; punto de control al lado |
| Montacargas | Muro norte (baliza naranja) | Requiere 2 baterías → **FIN DE LA FASE 2** |
| Zonas de sombra | Tras grupos de cajas | Agachado dentro = OCULTO |

**Iluminación:** ambiente casi negro azulado, niebla, lámparas industriales frías (algunas parpadean,
otras proyectan sombras) y la baliza naranja pulsante del montacargas como punto de referencia.
Suelos y muros van en losetas de 6 m para que cada luz ilumine bien su zona.

**Rutas posibles:**
1. *Directa y arriesgada*: cruzar los pasillos centrales cronometrando al Dron A y entrar al oeste
   esquivando al Dron B.
2. *Sigilosa*: ir al este por la llave (cuidado al abrir el contenedor), volver, abrir la rejilla y
   recorrer el conducto agachado hasta la batería 2.

---

## 6. Ajustes rápidos de dificultad (Inspector)

| Más fácil | Más difícil |
|---|---|
| `EnemyVision.detectionSpeed` 0.8 | 1.5 |
| `EnemyVision.viewDistance` 9 | 15 |
| `EnemyPatrol.chaseSpeed` 3.8 | 5.2 |
| `EnemyPatrol.hearingMultiplier` 0.6 | 1.5 |
| `PlayerStealth.shadowMultiplier` 0.3 | 0.6 |

---

## 7. Pendiente para la Fase 3
- Persistencia del inventario entre escenas (guardado por `ItemData.Id`).
- Usar la chatarra y los circuitos (fabricación o reparación de terminales).
- Animaciones y sonidos de los drones (los campos de audio ya existen, solo falta asignar clips).
