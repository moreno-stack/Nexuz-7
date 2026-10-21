# Escape Espacial: Nexus-7. Mejoras, Etapa 4 (Fase 4, Laboratorio)

## Cómo reconstruir

1. En Unity, abre el menú **Nexus-7 → Fase 4 → Construir Laboratorio**. Se enlaza solo con la salida de la Sala de energía.
2. Pulsa **Play** en `Laboratorio`.

## Qué verás

1. **Cinemática «Laboratorio»** (19 s, con subtítulos). Muestra los tanques del proyecto ESPECTRO, el robot de investigación y el banco seguro. Se salta manteniendo **Espacio** 1 s.
2. **Robot de investigación** blanco, con visor morado y **porra eléctrica**.
   - Su golpe quita 15 en Fácil y además **te aturde 1 s**, aunque el escudo absorba todo el daño.
   - La anticipación es corta (0,45 s): apártate en cuanto veas parpadear su ojo.
   - Sigue revisando los bancos W2 y W3 con su foco morado. Si fabricaste allí hace poco, da la alarma.
3. **Robot de seguridad con bate** junto a la salida. Sustituye al antiguo dron R2 y revisa el despacho del director. Sin el foco morado ni el oído fino del antiguo dron, es un poco menos sensible.
4. **Objetos al azar.** Siempre hay **1 cristal óptico y 3 de chatarra**, que brillan. El resto puede ser chatarra, cristales o circuitos. La tarjeta del director sigue en el armario del despacho.
5. **Tres bancos de trabajo** con su cartel y cuatro recetas: señuelo, PEM, camuflaje y **kit de reparación**.
6. **HUD:** se añaden las barras de **integridad** y **escudo**.

## Verificación

- **380 comprobaciones automáticas, 0 fallos.** La nueva es `Phase4SceneTest` (7 comprobaciones del guion de la cinemática).
- Un script comprueba que los 10 puntos de botín quedan fuera del despacho, los tanques, los racks, las mesas, los bancos y sus puntos de inspección, los contenedores, la terminal, la cámara y las líneas láser.

## Revisión independiente

Resultado: **«lista con correcciones»**, sin fallos críticos. Comprobó además que la porra cierra los paneles sin dejar bloqueos, que el camuflaje siempre se puede fabricar y que las fases 5 y 6 no se rompen.

Se corrigieron estos textos:
- **Cristales:** decían que estaban junto a los tanques, pero ahora aparecen al azar por la sala.
- **Fabricación:** el panel de fabricación decía «drones»; ahora dice «robots».
- **Cámara:** la alarma de la cámara decía «drones»; ahora dice «robots».

**Detalles menores pendientes:**
- Dos puntos de botín están en la ruta del robot de investigación.
- Los robots no tienen una postura propia de aturdidos.
- La guía antigua `GUIA_FASE4.md` sigue describiendo cristales fijos.

## Siguiente: Etapa 5 (Fase 5, Centro de control)

- **Robots de élite** negros y rojos, con **hacha** (40 de daño).
- Botín al azar (1 circuito garantizado).
- Banco de trabajo.
- Cinemática del centro de control.
