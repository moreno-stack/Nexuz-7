# Escape Espacial: Nexus-7. Mejoras, Etapa 5 (Fase 5, Centro de control)

## Cómo reconstruir

1. En Unity, abre el menú **Nexus-7 → Fase 5 → Construir Centro de Control**. Se enlaza solo con la salida del laboratorio.
2. Pulsa **Play** en `CentroControl`.

## Qué verás

1. **Cinemática «Centro de Control»** (19 s, con subtítulos). Muestra el holograma, la pareja de élite con hachas y la compuerta blindada del puente. Se salta manteniendo **Espacio** 1 s.
2. **Pareja de robots de élite** negros y rojos, con **hacha**.
   - Su golpe quita 40 en Fácil y 80 en Difícil, así que en Difícil dos golpes te dejan fuera de combate.
   - Levantan el hacha durante 0,7 s: apartarte andando basta para esquivarla. Corriendo no te alcanzan, ni siquiera en Alerta Roja.
   - Patrullan alrededor del holograma. Si uno te ve, el otro acude, y en **Alerta Roja** salen a cazarte.
   - No golpean a través de las paredes de la antesala.
3. **Robot de seguridad con bate** en el ala oeste.
4. **Robot de investigación con porra** en el ala este. Sigue revisando el banco.
5. **Objetos al azar.** Siempre hay **1 circuito** garantizado; el resto puede ser chatarra, circuitos o cristales.
6. **HUD:** se añaden las barras de **integridad** y **escudo**. Los textos de alerta, del escuadrón y del informe de la nave ya dicen «robots».

## Verificación

- **391 comprobaciones automáticas, 0 fallos.** Las 11 nuevas son:

  | Prueba | Comprobaciones | Qué cubre |
  |---|---|---|
  | `Phase5SceneTest` | 9 | Guion de la cinemática y textos «robots» |
  | `FixE5Test` | 2 | Corrección de la revisión |

- Un script comprueba que los 10 puntos de botín quedan fuera del estrado, las consolas, las mesas, la antesala, el muro y la rampa del ala, el banco, los contenedores, las cámaras y las rutas de los robots.

## Revisión independiente

Resultado: **«lista con correcciones»**, sin fallos críticos.

Se corrigió un punto importante. En Alerta Roja, al reaparecer tras quedar fuera de combate, los élite seguían cazando y corrían al punto de control. El peor caso era el de la antesala: un callejón sin salida delante de la compuerta, que está sellada en Roja. Ahora, al reaparecer:
- los robots olvidan dónde te vieron;
- la caza vuelve a esperar su intervalo;
- cualquier golpe que estuviera a medias se cancela.

Además, el punto de control de la antesala se movió delante del láser, fuera del callejón.

**Detalles menores pendientes:**
- Dos puntos de botín quedan cerca de la ruta de los élite.
- Si saltas la cinemática, no oyes el aviso de las hachas.
- Puede que la barra de alerta se dibuje sobre las franjas negras de la cinemática; hay que comprobarlo en Unity.
- La guía antigua `GUIA_FASE5.md` sigue hablando de drones.

## Siguiente: Etapa 6 (Fase 6, Evacuación, y finales)

- Élite y seguridad armados.
- Botín al azar (1 batería garantizada).
- Banco de trabajo.
- Cinemáticas de **autodestrucción**, **despegue** y **derrota**.
- **Selector de dificultad** en el menú principal y la dificultad en la pantalla de resultados.
