# Gráficos de armas, objetos, dispositivos e iconos — Plan de implementación

> **Para agentes:** SUB-SKILL: superpowers:executing-plans (Samuel eligió ejecución nativa). Pasos con casillas `- [ ]`.

**Objetivo:** sustituir las primitivas de un color por modelos propios hechos por código y añadir iconos al inventario y a la barra rápida.

**Arquitectura:** planos de piezas en lógica pura (`ModelBlueprint`, `ModelLibrary`, `IconRasterizer`) probados fuera de Unity; un fabricante (`ModelFactory`) los monta en Unity; los componentes existentes piden el plano y lo montan.

**Tecnología:** Unity 6000.6, URP 17.6, C#; pruebas con mcs + stubs (`bash Tests/run_all.sh`).

**Spec:** `docs/superpowers/specs/2026-11-24-graficos-objetos-design.md`

## Restricciones globales

- Sin modelos importados ni assets nuevos: todo por código.
- La lógica pura no usa tipos de Unity (los stubs de `Vector3` y `Color` no calculan): usa `MVec` y `MColor` propios.
- Objetos del suelo ≤ 0,45 m de lado mayor; armas ≤ 0,75 m de largo.
- Textos en español; commits pequeños como `moreno-stack`, sin atribuciones.

## Revisión (lo que las pruebas no cubren)

1. Escala de las primitivas de Unity (cilindro y cápsula miden 2 de alto): un fallo deja piezas el doble de largas.
2. Materiales compartidos que se destruyen con un objeto (no destruir los de caché).
3. El colisionador de los dispositivos lanzados (que choquen y reboten igual que antes).
4. Imprescindibles: siguen recibiendo brillo, haz, marcador y `MoveToFreeFloor`.
5. Iconos creados cada fotograma (deben salir de la caché).

---

### Tarea 1: Planos y armas

**Archivos:** crear `Visual/Models/ModelBlueprint.cs`, `Visual/Models/ModelLibrary.cs`; prueba `Tests/Suites/ModelTest.cs`.

**Produce:** `MVec(x,y,z)`, `MColor(r,g,b)`, `enum ModelShape { Box, Cylinder, Sphere, Capsule }`, `enum MaterialRole { Body, Metal, Grip, Accent, Glow }`, `ModelPart { Shape, Position, Euler, Size, Role }`, `ModelBlueprint { Key, Accent, Parts, Add(...), MinCorner(), MaxCorner(), Scaled(f) }` (límites con las esquinas de la caja girada de cada pieza), `ModelLibrary.Weapon(string id, MColor accent, IList<WeaponMod> mods)`.

- [ ] Prueba: piezas válidas, `Accent`/`Glow` presente, orden de longitudes pistola < escopeta < rifle < cañón, ≤ 0,75 m, 4 armas distintas; supresor alarga por delante, cargador ampliado baja el mínimo Y, estabilizador y disipador añaden piezas, sin accesorios = plano base. Ver que falla.
- [ ] Implementar; ver que pasa; commit.

### Tarea 2: Objetos, dispositivos, munición y accesorios sueltos

**Archivos:** `ModelLibrary.cs`; prueba en `ModelTest.cs`.

**Produce:** `ModelLibrary.Item(string id, ItemCategory cat, MColor color)`, `ModelLibrary.HasOwnModel(string id)`, `ModelLibrary.Device(DeviceKind kind, MColor color)`, `ModelLibrary.Ammo()`, `ModelLibrary.ModCase(WeaponMod mod)`.

- [ ] Prueba: los 7 Ids y los 4 dispositivos tienen plano propio, ≤ 0,45 m, Id desconocido → genérico de su categoría (clave `generic_<cat>`), el `Accent` es el color dado. Ver que falla.
- [ ] Implementar; ver que pasa; commit.

### Tarea 3: Iconos (trazador de rayos)

**Archivos:** crear `Visual/Models/IconRasterizer.cs`; prueba `Tests/Suites/IconTest.cs`.

**Produce:** `IconRasterizer.Render(ModelBlueprint bp, int size) → MColor[] + alfa` como `float[] rgba` (size·size·4, fila 0 abajo).

- [ ] Prueba: longitud, esquinas transparentes, cobertura 15–85 %, contorno oscuro presente, aparece el color del objeto, iconos distintos para batería y llave, determinista. Ver que falla.
- [ ] Implementar (rayo por píxel en vista 3/4, intersección analítica caja/esfera/cilindro/cápsula en el espacio de la pieza, luz difusa + ambiente, contorno de 1 px); ver que pasa; commit.

### Tarea 4: Fabricante y arma en la mano

**Archivos:** crear `Visual/Models/ModelFactory.cs`, `Visual/Models/ModelMaterials.cs`; modificar `Combat/WeaponViewModel.cs`, `Combat/PlayerWeapon.cs`; ampliar stubs si falta algo.

**Produce:** `ModelFactory.Build(ModelBlueprint bp, Transform parent, bool castShadows) → Transform`, `ModelMaterials.For(MaterialRole role, MColor accent) → Material`, `WeaponViewModel.Show(PlayerWeaponSpec spec, IList<WeaponMod> mods)`.

- [ ] Prueba (fuente): `WeaponViewModel` usa `ModelLibrary.Weapon` y `PlayerWeapon` refresca la vista al instalar un accesorio. Ver que falla.
- [ ] Implementar; compilación completa OK; commit.

### Tarea 5: Botín, objeto en la mano, munición, accesorios y dispositivos

**Archivos:** `Inventory/RoomLootSpawner.cs`, `Player/HeldItemView.cs`, `Combat/AmmoPickup.cs`, `Combat/ModPickup.cs`, `Devices/DeviceFactory.cs`.

- [ ] Prueba (fuente): ninguno de los cinco usa `CreatePrimitive`; todos usan `ModelFactory.Build`. Ver que falla.
- [ ] Implementar, un commit por archivo o pareja; suite completa verde.

### Tarea 6: Iconos en la interfaz

**Archivos:** crear `UI/ItemIcons.cs`; modificar `UI/HotbarUI.cs`, `UI/InventoryUI.cs`.

**Produce:** `ItemIcons.For(ItemData item) → Sprite` (caché por Id, 64 px), `ItemIcons.BlueprintFor(ItemData item) → ModelBlueprint`.

- [ ] Prueba: `BlueprintFor` de un `DeviceItemData` usa su tipo y color; de un objeto normal su Id; el sprite se cachea (misma instancia dos veces). Ver que falla.
- [ ] Implementar fila de iconos de la barra rápida y rejilla del inventario; suite verde; commit.

### Tarea 7: Guía y verificación

- [ ] `docs/guias/mejoras/GUIA_GRAFICOS_OBJETOS.md`; `bash Tests/run_all.sh` verde; revisión final; entrega.
