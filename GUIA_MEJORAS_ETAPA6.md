# Escape Espacial: Nexus-7. Mejoras, Etapa 6 (Fase 6, Evacuación, finales y dificultad)

## Cómo reconstruir

1. En Unity, abre el menú **Nexus-7 → Fase 6 → Construir Sala de Evacuación (y Menú Principal)**. El menú se reconstruye siempre, así que recibe el selector de dificultad.
2. Pulsa **Play** en `MenuPrincipal` para jugar la partida completa.

> **Para jugar de principio a fin**, reconstruye las 6 fases en orden: Fase 1 → 2 → 3 → 4 → 5 → 6. Cada constructor enlaza su escena con la anterior, y la Fase 6 ordena los Build Settings.

## Qué verás

1. **Menú principal con selector de dificultad.**
   - Hay tres botones: **Fácil ×1,0 · Normal ×1,5 · Difícil ×2,0** (multiplicadores de daño). También se cambia con **[←][→]**.
   - Debajo se indica el multiplicador de puntuación.
   - La elección se guarda entre sesiones y afecta al daño de robots y peligros en todas las fases.
2. **Cinemática «Autodestrucción»** al llegar al hangar (19 s): la lanzadera, la pareja de élite y las cápsulas. **La cuenta atrás de 5 minutos no empieza hasta que termina** (o la saltas con Espacio), y lo mismo pasa con la alerta amarilla y los temblores.
3. **Pareja de élite con hacha** y un **robot de seguridad con bate** en la pasarela.
4. **Objetos al azar.** Siempre hay **1 batería** para la celda de ignición, y la cabina guarda otra.
5. **Banco de trabajo** junto a la entrada, con las 4 recetas.
6. **Finales:**
   - **«Despegue»**: vista exterior; la cápsula B se separa y el reactor estalla.
   - **«Derrota»**: el hangar se desintegra.

   Ambos se pueden saltar. Al terminar, la imagen funde a negro y aparecen los resultados.
7. **Pantalla de resultados** con la línea **«Dificultad: Normal ×1,15»** (multiplicador de puntuación).

## Verificación

- **417 comprobaciones automáticas, 0 fallos.** Las 26 nuevas son:

  | Prueba | Comprobaciones | Qué cubre |
  |---|---|---|
  | `Phase6SceneTest` | 19 | Guiones de las 3 cinemáticas, cuenta atrás en pausa, explosión, textos de dificultad y guardado de la elección |
  | `FixE6Test` | 7 | Correcciones de la revisión |

- Un script comprueba que los 10 puntos de botín del hangar quedan fuera de la lanzadera, las cajas, la cabina, las rampas, los cables, el banco, el anillo de los élite y la ruta de la pasarela.

## Revisión independiente

Resultado: **«lista con correcciones»**, sin fallos críticos. Se corrigieron estos 7 puntos:

1. **Explosión que tapaba la cámara.** La esfera de la explosión envolvía la cámara, así que desaparecía o llenaba la pantalla. Ahora tiene un tamaño calculado para que la cámara quede fuera, y en la derrota es una explosión propia.
2. **Cámara de vuelta al hangar.** Al terminar el despegue, la cámara volvía al hangar ya destruido. Ahora se queda en el plano final hasta el fundido.
3. **Menú que se cerraba.** Pulsar Intro después de elegir dificultad podía activar «Salir» y cerrar el juego. Los botones del menú ya no tienen navegación.
4. **Alerta amarilla.** Se consumía durante la cinemática; ahora empieza con la cuenta atrás.
5. **Botones de dificultad.** El texto ya no se parte en dos líneas.
6. **Temblores.** Ya no saltan en mitad de la cinemática de llegada.
7. **Textos.** Las historias de los finales ya dicen «robots».

**Detalles menores pendientes:**
- Si mueres con tiempo de sobra, la derrota igualmente habla de un «fallo de contención».
- El HUD de la cuenta atrás sigue visible durante los finales.
- Con mando no se puede cambiar la dificultad, solo con [←][→].

---

## Resumen de las 6 etapas de mejoras

| Etapa | Fase | Qué entró |
|---|---|---|
| 0 | Base | Dificultad, escudo, salto, barra 1–9, cámara V, modelos redondeados, armas, botín al azar y cinemáticas, todo con pruebas |
| 1 | Sala inicial | Jugador nuevo, taquilla del traje, cinemática «Despertar» y tutorial ampliado |
| 2 | Almacén | Robots de seguridad con bate, botín, banco con señuelo y cinemática |
| 3 | Sala de energía | Guardias con llave inglesa, botín resoluble siempre, kit de reparación, módulo del reactor y cinemática |
| 4 | Laboratorio | Robot de investigación con porra eléctrica, botín y cinemática |
| 5 | Centro de control | Élite con hacha, reaparición segura en Alerta Roja y cinemática |
| 6 | Evacuación | Élite y seguridad armados, botín, banco, cinemáticas de llegada y finales, y dificultad en el menú y en los resultados |

En total hay **417 comprobaciones automáticas** y cada etapa pasó una **revisión de código independiente** con sus correcciones.
