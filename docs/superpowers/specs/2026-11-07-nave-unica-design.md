# Nexus-7 como una sola nave — diseño

**Fecha:** 2026-11-07 · **Estado:** aprobado en conversación, pendiente de revisión escrita

## 1. Objetivo

Que todo el juego ocurra en **una sola nave espacial grande**, recorrida a pie de principio a fin, sin pantallas de carga entre fases. Ahora mismo cada fase es una escena aparte (con su propio jugador, HUD y sistemas) y `SceneExitTrigger` o una consola cargan la siguiente.

**Decisiones del usuario:**
- El avance es en orden, con puertas.
- Las 6 salas de fase se unen con zonas de paso.
- Ventanales al espacio.
- Mapa de la nave en el inventario.
- Enfoque A: una sola escena con todas las salas.

**Criterios de éxito**
1. «Nueva partida» abre la escena `NaveNexus7`, y se puede ir caminando desde la cápsula criogénica hasta la cápsula B sin ninguna carga de escena.
2. Cada sala de fase conserva su contenido y su reto: robots, botín, objetivos, puertas y cinemática.
3. La nave mide unos 350 m (±20 %) y tiene 4 zonas de paso nuevas y pasillos con ventanales.
4. El mapa del inventario muestra las salas descubiertas, la posición del jugador y la sala del objetivo.
5. El juego va fluido: solo están activas las luces, los robots y los efectos de la zona actual y de las vecinas.
6. Las 492 comprobaciones actuales siguen pasando, hay pruebas nuevas para el plano, las zonas y el mapa, y las escenas sueltas de cada fase se pueden seguir construyendo.

**Fuera de alcance:** varias cubiertas o ascensores, el vuelo exterior inicial, nuevas salas con objetivo propio, carga aditiva de escenas y cambios en las mecánicas de cada fase.

## 2. Distribución

De popa a proa:

| # | Zona | Tipo | Contenido |
|---|---|---|---|
| 1 | Sala inicial (corredor C-7) | fase 1 | Igual que ahora: cápsula, taquilla del traje, terminal, tutorial |
| 2 | Pasillo de popa | pasillo | Ventanales |
| 3 | Comedor | paso | Mesas, barra, bandejas; 4–6 puntos de botín; **sin robot** |
| 4 | Almacén de carga | fase 2 | Igual que ahora |
| 5 | Dormitorios | paso | Literas, taquillas; botín; 1 robot de seguridad |
| 6 | Sala de energía | fase 3 | Igual que ahora |
| 7 | Enfermería | paso | Camillas, armario médico, monitores; botín (más kits); 1 robot de seguridad |
| 8 | Laboratorio ESPECTRO | fase 4 | Igual que ahora |
| 9 | Sala de máquinas | paso | Tuberías, bombas, vapor; botín; 1 robot de seguridad |
| 10 | Centro de control | fase 5 | Igual que ahora |
| 11 | Pasillo blindado | pasillo | Ventanales, luces de alarma |
| 12 | Hangar de evacuación | fase 6 | Igual que ahora, con la cápsula B |

- **Las zonas de paso** miden unos 14 × 18 m y tienen la misma altura que los pasillos. Cada una tiene su propio `RoomLootSpawner`, con una tabla y probabilidad de vacío propias.
- **Los pasillos** miden 4 m de ancho, 3,2 m de alto y unos 8–12 m de largo. Llevan ventanales en una o ambas paredes: un hueco con cristal translúcido y marco.
- **Un solo fondo espacial** para toda la nave (estrellas, planeta con anillo y piezas del casco) está colocado de modo que se ve desde los ventanales.
- **Unión entre salas:** la salida de cada sala (el montacargas de las fases 2, 3 y 4, el fin del corredor de la fase 1 y la puerta del puente de la fase 5) se abre hacia un pasillo recto. La siguiente sala se desplaza en X para que su entrada (el vestíbulo sur, en local (0, 0, −5,2)) quede alineada con esa salida. La nave resulta ligeramente en zigzag, pero todos los pasillos son rectos.

## 3. Juego

- **Inicio:** `GameFlow` lleva de «Nueva partida» a `NaveNexus7`.
- **Puertas de sección:** se mantienen las condiciones actuales de cada salida:
  - terminal C-7 (fase 1);
  - 2 baterías en el montacargas (fase 2);
  - 4 bloques de energía (fase 3);
  - tarjeta del director (fase 4);
  - autorización del puente (fase 5).

  En la nave, en lugar de cargar otra escena, la salida da paso al pasillo siguiente. La consola final de la fase 5, que antes cargaba la escena 6, ahora abre la puerta del pasillo blindado. Se puede volver a salas ya visitadas.
- **Primera entrada en una sala de fase:**
  1. se reproduce su cinemática (los robots se pausan, como ahora);
  2. su `LevelIntro` muestra el mensaje, la pista y el objetivo;
  3. se guarda un punto de control en la entrada;
  4. se muestran las piezas del HUD propias de la sala.

  Al volver a entrar no se repite nada.
- **Zonas de paso y pasillos:** al entrar sale un comentario breve de la IA (una vez por zona), y el objetivo pasa a «Sigue hacia ‹siguiente sala de fase›» hasta entrar en ella.
- **HUD por sala:**
  - La energía (`PowerStatusUI`) solo se ve en la sala de energía.
  - La cuenta atrás y el estado de la cápsula solo aparecen en el hangar. `EvacuationCountdown` empieza cuando termina la cinemática de llegada al hangar, no al cargar la escena.
- **Global, como ahora en una partida:** nivel de alerta, inventario, traje, barra rápida, dificultad, estadísticas y puntuación. `PlayerRespawn` usa el último punto de control.
- **Final:** sin cambios (`EndingManager`, cinemáticas y pantalla de resultados de la etapa 8).

## 4. Construcción (editor)

- **Menú nuevo:** `Nexus-7 → Nave completa → Construir nave`. Crea `Assets/_Nexus7/Scenes/NaveNexus7.unity` y la pone en los Build Settings después de `MenuPrincipal`.
- **`RoomContext`** (editor) contiene:
  - la raíz de la sala;
  - el modo (`Standalone` o `Ship`);
  - el jugador, el HUD y el objeto de sistemas compartidos;
  - las opciones de la sala: abrir la entrada sur y no crear `SceneExitTrigger`.
- **Cada constructor de fase** se divide en `BuildRoom(RoomContext)` (solo la sala) y su `Build()` actual (menú). `Build()` crea la escena, llama a `BuildRoom` con `Standalone` y añade lo común, así que las escenas sueltas siguen igual.
  - En modo `Ship`, la parte común (jugador, HUD, EventSystem, NavMesh, fondo espacial) se crea una sola vez.
  - Las piezas de HUD que añade cada sala (salud, energía, alerta, cuenta atrás) son idempotentes: se buscan por nombre y solo se crean si faltan.
- **Reubicación:** todo lo que crea `BuildRoom` cuelga de la raíz de la sala, y la raíz se coloca en su sitio. Todo lo que usa `Transform` se mueve con ella: rutas de patrulla (hijos), puntos de botín, puntos de control y puertas.
  - Lo que usa coordenadas absolutas se corrige. `CinematicDirector` gana un campo `space` (`Transform`); si está asignado, los planos se interpretan en el espacio local de esa sala. Si es nulo, se mantiene el comportamiento actual.
- **`ShipLayout`** (lógica pura, en `Assets/_Nexus7/Scripts/Ship/`):
  - Recibe la lista de zonas con su longitud, ancho, anclaje de entrada y anclaje de salida (en local).
  - Calcula la posición de cada raíz y los tramos de pasillo.
  - Comprobaciones: los rectángulos XZ de las zonas no se solapan, cada salida coincide en X con la entrada siguiente y la longitud total está entre 280 y 420 m.
- **Constructores nuevos:**
  - `TransitRoomBuilder` (comedor, dormitorios, enfermería, sala de máquinas);
  - `CorridorBuilder` (pasillos con ventanales);
  - `ShipBuilder` (orquesta todo).
- **La luz ambiente y la niebla** que hoy fija el `ConfigureLighting` de cada sala se guardan en el `ShipZone` de esa sala en vez de aplicarse a toda la escena.

## 5. Juego (componentes)

- **`ShipZone`** (una por sala, pasillo o zona de paso) guarda:
  - índice, nombre visible, tipo (fase, paso o pasillo) y límites XZ;
  - luz ambiente y niebla;
  - referencias a su `CinematicDirector`, su `LevelIntro` y su punto de control;
  - el mensaje de entrada y el objetivo («Sigue hacia…»);
  - las raíces de contenido que se pueden apagar (luces, robots y partículas).
- **`ShipDirector`** (uno por escena):
  - Cada pocos fotogramas calcula la zona actual con `ShipZones.IndexAt(posición)` (lógica pura).
  - Activa el contenido de las zonas `actual ± 1` y apaga el resto.
  - En la primera entrada dispara los eventos de la zona, una sola vez (`ZoneVisits`, lógica pura).
  - Mezcla la luz ambiente y la niebla hacia las de la zona actual en ~1,5 s (`ScreenFx`/`ZoneAmbience`, lógica pura).
  - Empieza a apagar zonas cuando `RuntimeNavMeshBaker` ha terminado de hornear (el NavMesh necesita todos los colisionadores) y nunca desactiva suelos ni paredes.
  - `PowerGrid` y `AlertAmbience` solo controlan la luz ambiente mientras el jugador está en su sala.
- **`ShipMap`** (lógica pura):
  - Lista de salas con su rectángulo y si están descubiertas.
  - Conversión de mundo a mapa.
  - Cuál es la sala del objetivo (la siguiente sala de fase sin completar).
- **`ShipMapUI`:** pestaña «MAPA» del inventario. Muestra los rectángulos (descubiertos en color, sin descubrir en gris oscuro), la flecha del jugador y la sala del objetivo resaltada.

## 6. Errores y casos límite

- Si el jugador sale de todas las zonas (por ejemplo, al caer), se mantiene la última zona conocida.
- Las cinemáticas solo se reproducen la primera vez, aunque se reaparezca en esa sala.
- La cuenta atrás no empieza si se salta la cinemática del hangar antes de tiempo: empieza al terminar o al saltarla, como ahora.
- En modo `Ship` no queda ningún `SceneExitTrigger` ni ninguna acción de consola que cargue escena (una prueba lo comprueba en la fuente del constructor).
- Las escenas sueltas de fase siguen funcionando igual (modo `Standalone`).

## 7. Pruebas

- **`ShipLayoutTest`:** posiciones, que no haya solapes, alineación de salidas y entradas, longitud total y orden de las 12 zonas.
- **`ShipZoneTest`:** `IndexAt` (dentro, fuera y en el borde), conjunto activo `actual ± 1`, primera visita una sola vez y mezcla de ambiente (monótona, que termina en el valor de destino).
- **`ShipMapTest`:** conversión de mundo a mapa, descubiertas y sala objetivo.
- Las suites existentes deben seguir pasando, con compilación completa incluido el editor.
- **Simulación:** construir `NaveNexus7` en el simulador del vídeo, exportarla, renderizar una vista cenital y un recorrido en vídeo, y comprobar que no se solapan objetos de salas distintas.

## 8. Entrega por etapas

- **A:** `ShipLayout`, `ShipZone`, `ShipDirector`, `ShipMap` (lógica y pruebas).
- **B:** `RoomContext` y división de los 6 constructores, comprobando que las escenas sueltas quedan iguales.
- **C:** `ShipBuilder`, pasillos con ventanales y fondo único.
- **D:** las 4 zonas de paso.
- **E:** mapa en el inventario, `GameFlow` y Build Settings.
- **F:** vídeo de recorrido y guía.

Cada etapa lleva su revisión independiente y sus commits con el perfil `moreno-stack`, sin ninguna atribución a Claude.
