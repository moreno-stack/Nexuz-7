# Escape Espacial: Nexus-7. Mejoras, Etapa 1 (Fase 1, Sala inicial)

En esta etapa los sistemas de la Etapa 0 entran en el juego. Desde ahora **todas las fases** usan el jugador nuevo y el HUD nuevo. La Fase 1 recibe además:

- la taquilla del traje;
- la cinemática del despertar;
- un tutorial ampliado.

## Cómo reconstruir

1. En Unity, abre el menú **Nexus-7 → Fase 1 → Construir Sala Inicial**.
2. Vuelve a construir también las demás fases con el menú **Nexus-7** de cada una. Así reciben el jugador y el HUD nuevos; sus robots, su botín y su banco llegan en sus propias etapas.
3. Pulsa **Play** en `SalaInicial`.

## Qué verás en la Fase 1

1. **Cinemática «Despertar»** (19 s, con franjas negras y subtítulos en español):
   - la cápsula criogénica;
   - el corredor hasta la puerta C-7;
   - la taquilla del traje.

   **Mantén Espacio** durante 1 s para saltarla.
2. **Tutorial paso a paso.** Cada pista espera a que hagas la acción:

   | Paso | Teclado | Mando |
   |---|---|---|
   | Moverse | W A S D | Stick izquierdo |
   | Correr | Shift | L3 |
   | Agacharse | Ctrl / C | B |
   | **Saltar** | **Espacio** | A |
   | **Cambiar de cámara** | **V** | View |
   | Interactuar (bitácora) | E | X |
   | **Ponerse el traje** | E en la taquilla | X |

   Si te pones el traje antes de que lo pida, el tutorial lo da por hecho.
3. **Taquilla junto a la cápsula.** Al ponerte el traje:
   - el modelo cambia (casco, visor y mochila);
   - aparece la **barra azul de escudo** abajo a la izquierda.
4. **Sin traje, la puerta C-7 no se abre**, aunque ya hayas usado la terminal. La IA te recuerda dónde está.
5. **Dos montones de chatarra en el corredor.** Recógelos y verás la **barra rápida** abajo en el centro. Las teclas **1–9** o la rueda del ratón eligen el objeto, que se ve en la mano.

## Cambios comunes a todas las fases

- **Jugador:**
  - modelo redondeado y animado;
  - cámara en 1ª o 3ª persona con **V**. En 3ª persona la cámara se acerca sola en espacios estrechos y no atraviesa el techo ni las paredes;
  - salto;
  - escudo y vida;
  - inventario;
  - barra rápida 1–9.
- **HUD:** barra rápida, escudo, camuflaje y subtítulos de las cinemáticas. La antigua barra de 3 dispositivos ya no se crea.

## Verificación

- Compilan todos los scripts, incluidos los 6 constructores.
- **334 comprobaciones automáticas, 0 fallos.** Las 25 nuevas son:

  | Prueba | Comprobaciones | Qué cubre |
  |---|---|---|
  | `PlayerRigSetupTest` | 4 | Componentes del jugador, sin duplicados y en orden |
  | `TutorialTest` | 7 | Pasos y teclas; empieza aunque saltes la cinemática; traje ya puesto |
  | `Phase1SceneTest` | 8 | Guion de la cinemática: duración, textos y subtítulos sin solaparse |
  | `FixE1Test` | 6 | Correcciones de la revisión |

- Un script comprueba el plano de la sala: la taquilla no tapa la terminal, la bitácora ni el punto de aparición, y los objetos no caen dentro de la cápsula.
- Revisión de código independiente: ver la sección siguiente.

## Revisión independiente

Resultado: **«lista con correcciones»**, sin fallos críticos. Se corrigieron estos 5 puntos, cada uno con su prueba:

1. **Salto en el tutorial.** Agacharse es un interruptor, así que el jugador seguía agachado y Espacio no hacía nada. La pista ahora dice que te pongas de pie primero.
2. **Traje en la taquilla:** ahora hay un fundido breve, un sonido de sellado y un **destello azul** del escudo. El sonido se oye cuando asignes un clip en el campo *Seal Sound* de la taquilla, porque el proyecto aún no trae audio.
3. **Modelo del jugador en las fases 2–6:** ya no se reconstruye sin necesidad al empezar, y las piezas nuevas no chocan con el jugador durante un frame.
4. **Volver al menú** quita el traje. Si ya lo llevas, la taquilla aparece vacía.
5. **HUD:**
   - el indicador para saltar la cinemática solo se ve durante la cinemática y está abajo a la derecha;
   - la barra de escudo y la barra rápida ya no se montan sobre la integridad.

**Detalles menores pendientes** (no impiden jugar):
- En 1ª persona casi no se ven los brazos.
- Si juegas en 1ª persona, durante la cinemática el cuerpo se ve solo como sombra.
- La barra de vida verde aparece en las fases 3 y 6. En las demás llega cuando el daño se active en su etapa, empezando por la Etapa 2.
- Usar la chatarra no muestra el aviso «no se puede usar».

## Siguiente: Etapa 2 (Fase 2, Almacén de carga)

Entran en el almacén:
- los robots de seguridad con bate;
- el botín al azar, con los objetos imprescindibles garantizados;
- el banco de trabajo;
- la cinemática del almacén;
- la vida y el escudo, ya activos.
