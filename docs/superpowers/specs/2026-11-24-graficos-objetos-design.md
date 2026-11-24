# Diseño: gráficos de armas, objetos, dispositivos e iconos

**Fecha:** 2026-11-24
**Estado:** aprobado por Samuel en el chat (Modelos hechos por código; armas en la mano, objetos en el suelo, dispositivos e iconos del inventario)

## Objetivo

Hoy todo lo que se recoge o se lleva en la mano son primitivas de un color: el arma es una caja con un cilindro, las baterías son cilindros, las herramientas cápsulas, el resto cubos, y los dispositivos lanzados esferas. El objetivo es que cada arma y cada objeto tengan **una forma propia y reconocible**, hecha por código como el resto del juego (sin modelos importados), y que el inventario y la barra rápida muestren **un icono** de cada objeto.

## Decisiones

| Tema | Decisión |
|---|---|
| Cómo se describen | `ModelBlueprint`: lista de piezas (`ModelPart`) con forma (Caja, Cilindro, Esfera, Cápsula), posición, giro (Euler), tamaño real en metros y **papel del material**. Es lógica pura: se prueba fuera de Unity. |
| Papeles de material | `Body` (carcasa gris oscuro), `Metal` (acero claro y brillante), `Grip` (goma negra mate), `Accent` (color del objeto, emisivo suave), `Glow` (color del objeto, emisivo fuerte: pantallas, núcleos, bobinas). |
| Catálogo | `ModelLibrary`: `Weapon(id, mods)`, `Item(id, categoría, color)`, `Device(tipo, color)`, `Ammo()`, `Mod(accesorio)`. Un Id desconocido usa la forma genérica de su categoría (nunca devuelve nada vacío). |
| Montaje | `ModelFactory.Build(plano, padre, opciones)`: crea las primitivas, quita sus colisionadores, aplica escala (el cilindro y la cápsula de Unity miden 2 de alto) y comparte materiales por (papel, color) con `ModelMaterials`, que parte del material por defecto de las primitivas (Lit de URP). Opción `castShadows` (apagada en la mano). |
| Armas en la mano | Mirando a +Z, tamaños parecidos a los actuales. **Pistola** corta con empuñadura inclinada, corredera y mira. **Rifle** largo con cargador curvo, culata, guardamanos y mira. **Escopeta** gruesa con dos cañones, guardamanos de bombeo y culata corta. **Cañón de riel** el más largo, con dos raíles, 3 bobinas `Glow` y batería trasera. La franja `Accent` lleva el color del arma (ColorR/G/B). |
| Accesorios visibles | **Supresor**: cilindro en la boca del cañón (el arma se alarga). **Cargador ampliado**: el cargador baja más. **Estabilizador**: empuñadura vertical bajo el cañón. **Disipador**: 3 aletas `Metal` encima. Se reconstruye el modelo al cambiar de arma o al instalar un accesorio. |
| Objetos | **Batería**: celda con tapas `Metal` y 3 franjas de carga `Glow`. **Chatarra**: 3 placas dobladas y un tornillo. **Circuito**: placa verde con chips y pistas `Glow`. **Cristal óptico**: prisma alargado `Glow` sobre base. **Llave hidráulica**: mango `Grip`, cuerpo y boca abierta `Metal`. **Tarjeta del director**: tarjeta fina con banda y chip `Accent`. **Módulo del reactor**: caja con asas y núcleo `Glow`. |
| Dispositivos | **Señuelo**: cilindro con antena y altavoz `Glow`. **PEM**: esfera con 2 anillos `Glow`. **Camuflaje**: disco con lente `Glow`. **Kit de reparación**: maletín con cruz `Accent`. Los lanzados mantienen Rigidbody, luz y comportamiento; el colisionador pasa a ser una esfera de 0,11 m en la raíz. |
| Munición y accesorios sueltos | **Munición**: caja con 3 balas `Glow` cian. **Accesorio**: maletín morado con la forma del accesorio encima. Siguen flotando, girando y brillando. |
| Botín | `RoomLootSpawner`, `HeldItemView`, `AmmoPickup`, `ModPickup` y `DeviceFactory` usan `ModelLibrary` + `ModelFactory`. Los imprescindibles se escalan ×`KeyItemScale` y conservan brillo, haz de luz y marcador. |
| Tamaño | Ningún objeto del suelo mide más de 0,45 m en su lado mayor (×2,2 en imprescindibles cabe en el punto de botín); ningún arma más de 0,75 m de largo. |
| Iconos | `IconRasterizer.Render(plano, lado)`: un trazador de rayos diminuto, lógica pura, que dibuja las piezas en vista 3/4 (giro 35°, inclinación 25°), encuadradas automáticamente, con luz difusa, contorno oscuro y fondo transparente. `ItemIcons.For(item)` lo convierte en `Sprite` de 64 px y lo guarda en caché por Id. |
| Iconos en la interfaz | `HotbarUI`: una fila de iconos de 44 px encima del texto de la barra rápida; el seleccionado con marco blanco y algo mayor. `InventoryUI`: una rejilla de 4 columnas (56 px, icono + cantidad, marco naranja si es objeto clave) en la parte derecha del panel; el texto se estrecha a 400 px. Se crean por código al empezar: no hace falta tocar los constructores. |

## Componentes

| Archivo | Responsabilidad |
|---|---|
| `Visual/Models/ModelBlueprint.cs` | `ModelShape`, `MaterialRole`, `ModelPart`, `ModelBlueprint` (piezas, límites, añadir/escalar) |
| `Visual/Models/ModelLibrary.cs` | Planos de armas, accesorios, objetos, dispositivos y munición |
| `Visual/Models/ModelFactory.cs` + `ModelMaterials.cs` | Montarlos en Unity con materiales compartidos |
| `Visual/Models/IconRasterizer.cs` | Píxeles del icono (lógica pura) |
| `UI/ItemIcons.cs` | Sprite por objeto, en caché |
| Cambios | `WeaponViewModel`, `PlayerWeapon` (refrescar con accesorios), `RoomLootSpawner`, `HeldItemView`, `AmmoPickup`, `ModPickup`, `DeviceFactory`, `HotbarUI`, `InventoryUI` |

## Errores

- Plano sin piezas o Id desconocido → forma genérica de la categoría.
- Sin material base (primitiva sin renderer) → se crea con `Shader.Find("Universal Render Pipeline/Lit")`; si tampoco existe, el objeto se ve con el material por defecto.
- Objeto sin icono posible (plano vacío) → la casilla muestra solo el color del objeto.

## Pruebas (TDD, fuera de Unity)

- Planos: cada arma con piezas válidas (tamaño > 0), al menos una `Accent`/`Glow`, longitud pistola < escopeta < rifle < cañón, ≤ 0,75 m; las 4 armas distintas entre sí.
- Accesorios: cada uno añade piezas; el supresor alarga el arma por delante; el cargador ampliado baja el punto más bajo; sin accesorios = plano base.
- Objetos: los 7 Ids y los 4 dispositivos tienen plano propio (no el genérico); todos ≤ 0,45 m; Id desconocido → genérico de su categoría; el color `Accent` es el del objeto.
- Iconos: tamaño pedido, esquinas transparentes, cobertura entre el 15 % y el 85 %, contorno presente, el color del objeto aparece, iconos distintos para objetos distintos, mismo icono para el mismo plano (determinista).
- Compilación con mcs (y Roslyn si está) y todas las suites.

## Fuera de alcance

- Modelos importados, animaciones de recarga, armas en tercera persona.
- Icono del arma en el HUD de combate.
