# Escape Espacial: Nexus-7 — Fase 4 (Laboratorio)

Requisitos: Fases 1–3 funcionando. Sin paquetes nuevos.

---

## 1. Puesta en marcha

1. Copia `Assets/_Nexus7` encima del de tu proyecto.
2. Menú **Nexus-7 → Fase 4 → Construir Laboratorio** → **Play**.

El constructor crea los objetos, los dispositivos y las **recetas como assets**
(`Data/Items`, `Data/Recipes`), añade `Laboratorio` a *Build Settings* y **enlaza la salida de la Sala de
Energía** con el Laboratorio. Tu inventario llega desde la fase anterior. Si juegas esta escena directamente,
empiezas con 6 de chatarra, 2 circuitos y 1 batería.

---

## 2. Controles nuevos

| Acción | Teclado | Mando (Xbox) |
|---|---|---|
| Usar banco de trabajo | E (mirando al banco) | A |
| Elegir receta / fabricar / salir | W·S / E / Q | stick / A / B |
| Elegir dispositivo | 1 · 2 · 3 | LB (siguiente) |
| Lanzable: apuntar y lanzar | **mantener G** (se ve la trayectoria) → **soltar** | RB |
| Activable (camuflaje) | G | RB |

---

## 3. Sistema de fabricación

```
Crafting/  CraftingRecipe.cs    ScriptableObject: ingredientes (ItemStack[]) → resultado, tiempo
           CraftingSystem.cs    → Player. Lógica pura: HasIngredients, MaxCraftable, TryCraft (atómico)
           Workbench.cs         → banco. Abre la UI; al fabricar hace ruido y queda "manipulado"
           InspectableSpot.cs   → punto que los drones revisan (vigila un contenedor)
UI/        CraftingUI.cs        Panel de fabricación (recetas + detalle + barra de progreso)
```

`TryCraft` es **atómico**: consume los ingredientes y añade el resultado. Si el resultado no cabe en el
inventario, deshace todo y te devuelve los ingredientes.

### Recetas

| Dispositivo | Ingredientes | Tiempo | Efecto |
|---|---|---|---|
| **Emisor de pulso sonoro** (señuelo) | 3 Chatarra + 1 Circuito | 2 s | Se lanza. Emite pulsos de ruido cada 1.2 s durante 10 s (radio 14 m) |
| **Pulso PEM temporal** | 1 Batería + 1 Circuito + 2 Chatarra | 3 s | Se lanza y detona al impactar. **Aturde 6 s** a los drones en 6 m y **apaga** cámaras, barreras y láseres |
| **Camuflaje óptico temporal** | 1 Batería + 1 Cristal óptico + 1 Circuito | 3.5 s | Se activa. **Visibilidad al 10 %** durante 12 s; los sensores láser no te detectan |

**Fabricar tiene riesgo:** el juego no se pausa, estás quieto unos segundos, las herramientas hacen ruido
(5 m) y el banco queda **manipulado** durante 45 s. Si un dron de investigación lo revisa en ese tiempo, da la alarma.

Para añadir una receta: *Create → Nexus-7 → Receta* y arrástrala a *Known Recipes* del `CraftingSystem` del jugador.
Para un dispositivo nuevo: *Create → Nexus-7 → Dispositivo*.

---

## 4. Dispositivos: uso y despliegue

```
Devices/  DeviceItemData.cs   ItemData + tipo, modo de uso (Throw/SelfActivate), ranura, duración, radio...
          DeviceQuickbar.cs   → Player. Ranuras 1-3, apuntado con trayectoria, lanzamiento/activación
          DeviceFactory.cs    Crea el objeto lanzado en tiempo de ejecución (esfera + Rigidbody + luz)
          DecoyDevice.cs      Señuelo (implementa IDistraction)
          EmpDevice.cs        Detonación PEM (EmpRegistry.Pulse)
          PulseRing.cs        Efecto visual de onda expansiva
UI/       QuickbarUI.cs       Barra inferior + tinte y cuenta atrás del camuflaje
```

Los dispositivos son **objetos de inventario** (heredan de `ItemData`): se apilan, viajan entre escenas y
aparecen en `[Tab]`. La barra rápida tiene ranuras fijas (1 señuelo · 2 PEM · 3 camuflaje) que se muestran
incluso con 0 unidades, para que sepas qué te falta fabricar.

### Reacción de los enemigos (inteligencia)

| Situación | Reacción del dron |
|---|---|
| Oye un **señuelo** | Va a inspeccionarlo, **lo destruye** y queda **receloso** (detecta 1.6× más rápido durante 25 s) |
| Recibe **otro señuelo** antes de 40 s | **Deduce el engaño**: ignora el señuelo y va corriendo **al punto desde donde lo lanzaste** |
| Le alcanza un **PEM** | Aturdido 6 s: quieto, ciego (foco apagado) y sordo. Luego se reinicia, **busca durante el doble de tiempo** y queda receloso |
| La detonación del PEM | Hace ruido (radio ×1.8): los drones que estaban fuera del radio acuden a investigar |
| Banco **manipulado** al inspeccionarlo | Mensaje de alerta + **alarma** de 18 m: todos los drones cercanos acuden |
| Jugador con **camuflaje** | Visibilidad × 0.1 → cuenta como oculto: solo te detectan si están casi encima (pero siguen oyéndote) |

Las interfaces que lo conectan, sin acoplar módulos entre sí (`Core/DeviceInterfaces.cs`):
- `IDistraction`: lo que distrae (señuelo).
- `IInspectable`: lo que los drones revisan (banco, despacho).
- `IEmpTarget` + `EmpRegistry`: lo que un PEM inutiliza (drones, cámaras, barreras eléctricas y sensores láser).

---

## 5. Drones de investigación y obstáculos

| Elemento | Dónde | Comportamiento |
|---|---|---|
| **R1 — Dron de investigación** (blanco, visor morado) | Sala central | Recorre un bucle y se para **4 s a escanear** (foco morado) los bancos W2 y W3. Oído más fino (×1.2) |
| **R2 — Dron de seguridad** | Norte | Vigila la salida mirando al sur e **inspecciona el despacho**: si el armario de la tarjeta está abierto, da la alarma |
| **Cámara** | Almacén químico (este) | Barrido de ±40°. Si te ve, alarma. Un PEM o un hackeo la apagan |
| **Sensor láser alto** | Mampara de cuarentena (z=10) | Haz a 1.45 m: **pasa agachado** |
| **Sensor láser doble** | Antesala de la salida | 3 haces de 0.4 a 1.3 m: no se puede esquivar → **hackear** (terminal, 1 circuito), **PEM** o **camuflaje** |

Para que un dron inspeccione algo, selecciona un waypoint de su ruta, añade (o edita) el componente `Waypoint` y
arrastra un `Workbench` o un `InspectableSpot` a **Inspect Target**. Pon *Wait Time* = duración del escaneo.

---

## 6. El nivel

| Zona | Contenido |
|---|---|
| Acceso (sur) | Banco **W1 (seguro, nadie lo revisa)**, contenedor (batería + circuito), chatarra, registro del proyecto ESPECTRO |
| Tanques de contención (oeste) | 3 tanques con líquido luminoso (uno averiado, con chispas) y **2 cristales ópticos** |
| Sala central | Bancos W2 y W3 (revisados por R1), mesas con terminales parpadeantes, probetas y papeles por el suelo |
| Almacén químico (este) | Estanterías, bidones (uno volcado), contenedor (batería + 2 circuitos), cámara |
| Despacho del director (noroeste) | Armario con la **tarjeta** (+1 circuito), diario del director. R2 lo revisa |
| Salida (norte) | Láser doble + terminal de hackeo → **puerta de contención** (tarjeta) → montacargas |

**Recursos del laboratorio:** 7 de chatarra, 4 circuitos, 2 baterías y 2 cristales. Por ejemplo, alcanzan para
1 camuflaje + 1 PEM + 1 señuelo, y te sobra 1 circuito para hackear los láseres. Lo que traigas de la Sala de
Energía te da margen extra.

**Iluminación:** fluorescentes fríos (azul claro), algunos parpadeando, ambiente y niebla azulados, el resplandor
verde agua de los tanques y la luz cálida y tenue del despacho.

---

## 7. Ajustes rápidos

| Parámetro | Dónde | Efecto |
|---|---|---|
| `decoyMemory` (40) | EnemyPatrol | Menos = los drones caen más veces en los señuelos |
| `wariness` (1.6) / `warinessDuration` (25) | EnemyPatrol | Cuánto y cuánto tiempo quedan recelosos |
| `tamperWindow` (45) | EnemyPatrol | Cuánto tiempo delata un banco usado |
| `duration` / `radius` / `effectValue` | Assets de `Data/Items` (dispositivos) | Potencia de cada dispositivo |
| Ingredientes y tiempo | Assets de `Data/Recipes` | Coste de fabricación |

## 8. Pendiente para la Fase 5
- Puente de mando (la salida muestra «FIN DE LA FASE 4»).
- Guardado en disco (inventario + recetas conocidas).
