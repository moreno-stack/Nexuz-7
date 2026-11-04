# Escape Espacial: Nexus-7. Mejoras, Etapa 8 (menú y pantalla final más gráficos)

Esta etapa rediseña las dos pantallas que enmarcan la partida: el **menú principal** y la **pantalla de resultados**. Todas las formas (círculos, anillos, estrellas, degradados, viñeta y líneas de escaneo) se generan por código, así que el proyecto no necesita imágenes nuevas.

## Importante: reconstruye el menú y la Fase 6

En Unity, ejecuta **Nexus-7 → Fase 6 → Construir Menú Principal** y **Nexus-7 → Fase 6 → Construir Sala de Evacuación**, y después pulsa **Play** en `MenuPrincipal`.

Capturas de referencia (previsualización generada fuera de Unity; en el juego la luz y los brillos se ven mejor): `docs/media/etapa8_menu.jpg`, `docs/media/etapa8_final_victoria.jpg` y `docs/media/etapa8_final_derrota.jpg`.

## Menú principal

### Fondo 3D
- **La nave Nexus-7** gira despacio a la derecha. Lleva casco, puente con ventanal, anillo habitable con radios, paneles solares, antena, motores encendidos y luces de navegación que parpadean (roja, verde y blanca).
- El planeta ahora tiene **anillo** y un **halo de atmósfera** alrededor del borde.
- Hay **escombros** que giran entre la cámara y la nave.
- La cámara tiene un **vaivén suave**: empieza quieta y, en unos 3 s, llega a moverse unos 25 cm.

### Interfaz
- **Nebulosas** de colores (morada, azul y rosa), **viñeta** oscura en los bordes y **líneas de escaneo** suaves.
- **Panel lateral** con degradado hacia la derecha, una franja cian y esquinas de pantalla de mando.
- **Título** con una sombra azul que late y una línea naranja recorrida por un destello.
- **Botones** con franja de acento y la tecla en un recuadro («Intro»). Al pasar el ratón se aclaran.
- **Dificultad en 3 tarjetas.** Cada una muestra el nombre, el «nivel de peligro» (1, 2 o 3 marcas) y el daño («daño ×1,5»). La elegida lleva **marco dorado** y es un 6 % más grande. Se sigue cambiando con ←/→ o LB/RB.
- El **récord** va en su propia tarjeta, con una estrella.
- Los **controles** van en una barra inferior, y arriba a la derecha aparece «SISTEMA NEXUS-7 · EN LÍNEA» con un punto que late.
- **Entrada animada:** el título, los botones, la dificultad y el récord aparecen uno tras otro deslizándose desde la izquierda.

## Pantalla final

- **El fondo es semitransparente**, con degradado y viñeta, y detrás se ve la última imagen de la cinemática: el fundido previo ahora es parcial (`EndingManager.ResultsBackdropFade = 0.4`). Los restos del HUD de juego (mira, objetivo, barra rápida, escudo, sigilo) se ocultan.
- **Cabecera** con una banda del color del final: verde para el ÓPTIMO, ámbar para el TÁCTICO y rojo para el CRÍTICO. Lleva una etiqueta «FINAL …».
- **Evaluación:** cada categoría tiene un icono redondo con su inicial (S, R, T, C) y una barra con brillo. Las barras **se llenan una tras otra**.
- Los **logros** («+ Fantasma…») se encogen solos para caber en la tarjeta.
- Las **estadísticas** van en su propia tarjeta.
- **Insignia del rango:** la letra (S–D) va sobre un disco, rodeada por un **anillo segmentado que gira**, con un halo y **estrellas**: S tres, A dos, B una, y C y D ninguna.
- **Puntuación:** sube mientras se llenan las barras. Si hay récord nuevo, aparece el cartel «¡NUEVO RÉCORD!», que late. Si no, se muestra el récord anterior.
- **Botones** «REINICIAR PARTIDA [R]» y «MENÚ PRINCIPAL [M]» con tinte al pasar el ratón. Mientras un bloque aún es invisible, no se puede pulsar.

## Código nuevo

| Archivo | Qué hace |
|---|---|
| `UI/ProceduralSprites.cs` | Genera las formas (lógica pura y probada) y las guarda en caché, sin guardarlas dentro de la escena. |
| `UI/UIShape.cs` | Pone la forma a una Image o RawImage al cargar. |
| `UI/ScreenFx.cs` | Matemáticas de las animaciones, los colores del final y del rango, las estrellas y el vaivén de la cámara. |
| `UI/UIAnimators.cs` | `UIIntro` (entrada), `UISpin` (giro), `UIPulse` (latido) y `UISweep` (destello). |
| `Environment/MenuBackdrop.cs` | Vaivén de la cámara del menú y luces de navegación. |
| `Editor/MenuScreenBuilder.cs` | Construye la nave, los escombros y toda la interfaz del menú. |
| `Editor/EndingScreenBuilder.cs` | Construye la pantalla de resultados. Antes estaba dentro de `EvacuacionBuilder`. |

Cambios en código existente:
- `MainMenuUI` gana `CardTitle`, los marcos y las tarjetas.
- `EndingUI` gana la banda, la etiqueta, el anillo, las estrellas, el cartel de récord, `StarLit`, `ScoreLine` y `HideOnResults`.
- `DifficultySettings` gana `DamageText`.
- `ShieldHealthHUD` y `StealthIndicatorUI` se ocultan durante las cinemáticas y en los resultados.

## Pruebas

```bash
bash Tests/run_all.sh
```

Resultado: **compilación completa OK y 492 comprobaciones, 0 fallos**. La suite nueva `ScreenFxTest` prueba:
- las formas: el disco, el aro, la estrella entre puntas, la viñeta, el halo, los degradados y el tamaño de las texturas;
- las animaciones: la entrada sin retroceder, las barras escalonadas, el pulso y el destello que se apaga en los extremos;
- las estrellas y los colores de cada rango y final;
- las tarjetas de dificultad: marcas, escala, títulos y «daño ×…»;
- el vaivén de la cámara y el parpadeo de las luces;
- en los resultados, qué se oculta y la línea de puntuación.

## Revisión independiente

Resultado: **«lista con correcciones»**, sin fallos críticos. Se corrigieron estos puntos:
1. Los logros podían salirse de la tarjeta de evaluación (ahora se encogen solos).
2. Las líneas de escaneo se veían como un óvalo en el editor justo después de construir.
3. Las texturas generadas se habrían guardado dentro de la escena (ahora son `DontSave`).
4. El HUD de juego se transparentaba tras el fundido parcial.
5. El halo del planeta tapaba el centro en vez de rodear el borde.
6. El destello del título se salía de la línea y saltaba.
7. «¡NUEVO RÉCORD!» salía dos veces.
8. Los botones invisibles se podían pulsar durante la entrada.
9. El latido de los textos reconstruía la malla en cada fotograma.
10. El cambio de color al pasar el ratón casi no se notaba.
11. Las marcas del anillo sobresalían.
12. La estrella se calculaba de forma costosa.
13. Los escombros se alejaban del encuadre.
14. Había código duplicado.

**Detalles menores que quedan:**
- A resoluciones distintas de 1080p, las líneas de escaneo pueden verse algo irregulares.
- El halo del planeta es de la interfaz: con el vaivén de la cámara se desplaza unos píxeles respecto al planeta.
