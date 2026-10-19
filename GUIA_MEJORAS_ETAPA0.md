# Escape Espacial: Nexus-7 — Mejoras, Etapa 0 (sistemas base)

Esta etapa añade **los sistemas** de las mejoras acordadas, todos con pruebas automáticas.
**Las escenas todavía no cambian.** Cada sala se actualiza en su etapa (de la 1 a la 6). Las escenas que ya tenías
siguen funcionando igual.

## Qué se añadió

| Sistema | Archivos | Para qué sirve |
|---|---|---|
| Dificultad | `Core/DifficultySettings.cs` | Fácil, Normal o Difícil. Multiplica el daño de robots y trampas (×1 / ×1,5 / ×2), la recarga del escudo, la detección y la puntuación final. Se guarda entre partidas |
| Escudo del traje | `Player/ShieldModel.cs`, `Player/SuitShield.cs`, `Player/PlayerSuit.cs` | Escudo de 50 que absorbe los golpes antes que la vida, se recarga tras 6 s sin daño y se rellena al reaparecer |
| Daño unificado | `Player/PlayerHealth.cs` | Orden: dificultad → escudo → vida → empujón → alerta. Si un robot te deja fuera de combate cuenta como captura |
| Salto | `Player/JumpModel.cs`, `Player/PlayerController.cs` | **Espacio** salta (1,1 m), con un pequeño margen tras dejar un borde. Al aterrizar hace ruido. Incluye el empujón de los golpes |
| Barra rápida 1–9 | `Inventory/HotbarModel.cs`, `Inventory/Hotbar.cs`, `Player/HeldItemView.cs`, `UI/HotbarUI.cs` | **1–9** eligen cualquier objeto recogido y la rueda del ratón cambia de casilla. El objeto elegido se ve en la mano; **G o clic** usa los dispositivos |
| Cámara 1ª / 3ª persona | `Player/ThirdPersonCamera.cs`, `Player/CameraModeSwitcher.cs`, `Player/CameraBoomMath.cs` | **V** cambia de cámara. En 3ª persona la cámara no atraviesa paredes y en los conductos pasa sola a 1ª persona |
| Personajes redondeados | `Characters/*` | Jugador (con o sin traje) y 4 tipos de robot hechos con cápsulas, esferas y cilindros, sin ninguna caja. Se animan por código: caminar, correr, agacharse, saltar y golpear |
| Armas de los robots | `Enemies/WeaponData.cs`, `Enemies/MeleeAttackModel.cs`, `Enemies/EnemyMeleeAttack.cs` | Bate, llave inglesa, porra eléctrica (aturde) y hacha. El robot levanta el arma, y si te apartas a tiempo **esquivas** el golpe. Un PEM corta el ataque |
| Objetos al azar | `Inventory/LootTable.cs`, `Inventory/LootRoller.cs`, `Inventory/LootSpawnPoint.cs`, `Inventory/RoomLootSpawner.cs` | Objetos distintos en cada partida, con los imprescindibles siempre garantizados (brillan) |
| Cinemáticas | `Cinematics/*`, `UI/SubtitleUI.cs` | Planos de cámara, subtítulos en español y franjas negras. **Mantén Espacio** para saltarlas. Mientras duran, los robots se quedan en pausa |
| Traje y kit | `Interaction/SuitPickup.cs`, `UI/ShieldHealthHUD.cs` | Taquilla del traje, kit de reparación (+40 de vida) y barra azul de escudo. Las puertas pueden exigir el traje |

## Cambios que ya se notan en las escenas actuales

- **Espacio** salta y **1–9** eligen objetos (antes solo había 3 dispositivos). El dispositivo se usa con **G o clic**.
- Con mando:

  | Botón | Acción |
  |---|---|
  | A | Saltar |
  | X | Interactuar |
  | Y | Usar el dispositivo |
  | LB / RB | Cambiar de casilla |

- La dificultad multiplica el daño de las trampas. Hasta que llegue el selector del menú (Etapa 6), es Fácil por defecto.

## Verificación

- Compilan todos los scripts, incluidos los 6 constructores de escena.
- **309 comprobaciones automáticas, 0 fallos.** 103 son de las fases anteriores y 206 nuevas, repartidas así:

  | Prueba | Comprobaciones |
  |---|---|
  | Daño y escudo | 23 |
  | Movimiento y cámara | 12 |
  | Barra rápida | 11 |
  | Modelos | 118 |
  | Armas | 12 |
  | Botín | 10 |
  | Cinemáticas | 14 |
  | Correcciones de la revisión | 6 |

- **Revisión de código independiente: «listo con correcciones», sin fallos críticos.** Se corrigieron estos 7 puntos:
  1. Un golpe no letal ahora cierra el hackeo, la fabricación o la consola sin dejar el control bloqueado.
  2. El escudo se rellena al reaparecer.
  3. Si la escena cambia a mitad de una cinemática, los robots ya no se quedan en pausa.
  4. Los objetos imprescindibles nunca se pierden, aunque falten puntos de botín.
  5. En 3ª persona los objetos se lanzan desde el pecho.
  6. Los robots ya no se mueven mientras están congelados o aturdidos.
  7. Los botones del mando ya no chocan entre sí en las escenas ya construidas.

- **Detalles menores pendientes** (no impiden jugar; se irán puliendo en las etapas siguientes):
  - El ciclo de pasos va algo rápido.
  - El salto no rebota en el techo.
  - El objeto en la mano a veces no se ve en 1ª persona.
  - Falta el destello azul del traje.
  - El aturdimiento de los robots no tiene animación.

## Siguiente: Etapa 1 (Fase 1, Sala inicial)

En la sala inicial entran:
- el jugador con su modelo, el salto, la cámara con V y la barra rápida;
- la **taquilla del traje** (no se sale sin él);
- la **cinemática del despertar** con subtítulos;
- el HUD nuevo (escudo, vida, barra rápida y subtítulos).
