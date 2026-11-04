# Escape Espacial: Nexus-7. Mejoras, Etapa 7 (pulido de pendientes)

Esta etapa resuelve los **detalles menores** que fueron dejando anotados las revisiones de las etapas 0 a 6, y deja el repositorio ordenado con las **pruebas automáticas** dentro.

## Importante: reconstruye las 6 escenas

Muchos arreglos están en los constructores de escena. En Unity, ejecuta en orden **Nexus-7 → Fase 1 … Fase 6** y después pulsa **Play** en `MenuPrincipal`.

## Qué cambia

### Robots
- Aturdidos por un PEM o una porra, **se tambalean** (postura de aturdido) y vuelven a la normal al recuperarse.
- Si la alerta sube a mitad de una persecución, **aceleran** al momento.

### Jugador
- **Pasos a un ritmo natural:** antes iban al doble de rápido.
- **Bajar un escalón ya no hace ruido.** Las caídas de verdad (desde 0,5 m) siguen sonando.
- El salto **rebota en el techo** en vez de quedarse pegado.
- **1ª persona:** se ven el antebrazo derecho y el objeto que llevas en la mano, también agachado.
- **3ª persona:** el visor y las luces del traje ya no proyectan sombra.
- **Durante las cinemáticas** se ve tu cuerpo entero aunque juegues en 1ª persona.

### Controles e interfaz
- **Mando:** la cámara pasa a **R3** (clic del stick derecho), porque View ya abría el inventario. Las escenas antiguas se corrigen solas al cargar.
- **Menú:** la dificultad también se cambia con **LB/RB**.
- La **selección de la barra rápida** se conserva al pasar de una fase a otra.
- **Al recibir un golpe** se cierra el inventario.
- La **alerta** y la **cuenta atrás** se ocultan durante las cinemáticas y en el final.
- El aviso del banco ya no promete «[E]» desde lejos.
- **Fase 1:** aparecen avisos al recoger objetos y el mensaje «no se puede usar» si intentas usar chatarra.
- **Fase 5:** si saltas la cinemática, la intro igualmente te avisa de las hachas.
- **Derrota por quedarte sin vida** con tiempo de sobra: tiene sus propias frases («Constantes vitales perdidas...») y no muestra la explosión del reactor.

### Botín y niveles
- **Ruido al recoger:** la chatarra que aparece al azar vuelve a hacer ruido (3 m).
- **Ocho puntos de botín se apartaron** de las rutas de patrulla, ahora a 2 m o más: dos en cada una de las fases 3, 4, 5 y 6.
- **Guías antiguas:** `docs/guias/fases/GUIA_FASE2–6.md` llevan una nota que indica qué ya no aplica.

### Código
- Se eliminó el código que ya no se usaba (los antiguos drones «caja») y las referencias obsoletas a la barra de 3 dispositivos.
- La dificultad se carga antes que cualquier escena.
- Un modelo sin materiales sale gris, no rosa.
- No hay fugas de materiales en el objeto de la mano.

## Pruebas dentro del repositorio

Ahora están en `Tests/`, fuera de `Assets/` para que Unity no las compile. Con Mono instalado (en Windows, dentro de WSL):

```bash
bash Tests/run_all.sh
```

Resultado: **compilación completa OK y 440 comprobaciones, 0 fallos**. Lo comprobé desde una copia limpia del repositorio. Los detalles están en `Tests/README.md`.

## Revisión independiente

Resultado: **«lista con correcciones»**, sin fallos críticos. Se corrigieron estos 7 puntos:
1. El tutorial aún decía «View» para la cámara.
2. Las escenas ya construidas guardaban el botón antiguo.
3. El HUD reaparecía sobre el fundido del final.
4. La mano flotaba al agacharte en 1ª persona.
5. La derrota por muerte mostraba la explosión del reactor.
6. Quedaba un caso de material rosa.
7. El script de pruebas podía pasar por alto una suite que se colgara.

**Detalles menores que quedan:**
- En 1ª persona, el cuerpo desaparece en el último fundido del final.
- Subir de un salto a una caja de 0,6–1 m ya no hace ruido.
- El primer clic, el que captura el ratón, puede mostrar «no se puede usar» en la Fase 1.
