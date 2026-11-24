# Escape Espacial: Nexus-7 — Resumen del juego completo

Shooter de sigilo en primera persona (Unity 6, URP). Despiertas en una nave tomada por la IA NEXUS-7 y tienes que llegar a la cápsula de evacuación a través de 6 fases. Puedes pasar sin que te vean o abrirte paso a tiros. La IA observa cómo juegas y se adapta.

## Las 6 fases

| Fase | Escena | Qué hay que hacer | Encuentro por oleadas |
|---|---|---|---|
| 1 | Sala inicial | Ponerte el traje y salir del corredor | No (no hay robots) |
| 2 | Almacén de carga | 2 baterías para el montacargas (salen 3) | 2 oleadas al cruzar el punto seguro |
| 3 | Sala de energía | Restaurar los 4 bloques de energía | 2 oleadas con el primer hackeo o consola |
| 4 | Laboratorio | Conseguir la tarjeta del director | 3 oleadas al hackear el terminal del láser |
| 5 | Centro de control | Autorizar la compuerta y llegar al puente | 3 oleadas al usar la consola de mando |
| 6 | Evacuación | Lanzar la cápsula B antes de 5 minutos | 2 oleadas con el primer paso del lanzamiento |

Cada fase tiene su guía en [`fases/`](fases/), con una sección «Estado actual» al principio.

## Qué se ha añadido (PR #1 a #14)

| Sistema | Qué hace | Etapa de la guía |
|---|---|---|
| Armas modulares | Pistola, rifle, escopeta y cañón de riel con cargador, recarga, calor y dispersión; se guardan con la partida y funcionan con mando | 1, 4, 5 |
| Accesorios | Supresor, cargador ampliado, estabilizador y disipador (2 huecos por arma) | 15 |
| Robots que disparan | Láser de aviso antes de cada ráfaga, turnos de ataque, coberturas, avance y flanqueo | 6, 7 |
| Puntos débiles | Óptica (deslumbra), núcleo trasero de la élite (×3) y brazo del arma (desarma) | 8, 9 |
| IA analizadora y director adaptativo | Mide precisión, bajas, caídas, detecciones y acampada; sube o baja la tensión y con ella 11 perillas; detecta patrones (acampar, abusar de un arma, caer mucho) | 2 |
| Respawn dinámico | Al caer, los robots no vuelven al mismo sitio, nunca a la vista ni encima de ti | 3 |
| Encuentros por oleadas | Se activan con el objetivo de la fase, sellan la salida mientras duran y dan una recompensa según lo que te falte | 10, 21, 22 |
| Vidas | 5 / 3 / 2 por fase; sin vidas la fase se reinicia con otra colocación | 11 |
| Variantes de robot | Blindado, rápido u ojo de águila según la tensión | 12 |
| Eventos de nave | Apagón, fuga de vapor o barrido de seguridad en tu camino | 13 |
| Informe de la IA | Al empezar cada fase, tu estilo en la anterior y lo que hará la IA | 14 |
| Recompensas del director | Accesorio según cómo juegas al despejar encuentros o destruir élites | 16 |
| La IA aprende tus rutas | Mapa de calor; en cada intento una unidad vigila donde más te ha visto | 17 |
| Registro de partida | Una fila por fase en `nexus7_telemetria.csv`, para equilibrar con datos | 18 |
| Panel de pruebas F4 | Objetos imprescindibles, munición, vidas, tensión, eventos y salto de fase (solo desarrollo) | 19 |
| Análisis de la caída | Tarjeta al reaparecer: quién te vio, qué pesó y un consejo | 20 |

Todas las etapas, con sus pasos de prueba y su tabla de balance: [`mejoras/GUIA_ARMAS_DIRECTOR.md`](mejoras/GUIA_ARMAS_DIRECTOR.md).

## Prueba rápida del juego entero (unos 15 minutos)

1. **Nexus-7 → Construir todo el juego (Fases 1–6 y menú)** y Play. Nueva partida en Normal.
2. Fase 1: ponte el traje y sal. Comprueba que se carga la Fase 2.
3. Fase 2 con **F3** abierto:
   - Deja que un robot te atrape. Sale la tarjeta de la caída, F3 cuenta la caída y pierdes una vida.
   - Busca las baterías (haz de luz y aviso del traje).
   - Cruza el punto seguro: empieza el encuentro y la salida se sella.
   - Despéjalo: llegan la recompensa, quizá un accesorio, y la salida se abre.
4. Sal por el montacargas. Al empezar la Fase 3 se ve el informe de la IA sobre la Fase 2.
5. Con **F4** y luego **F12**, salta fase a fase hasta la 6 y comprueba que cada una carga con tu inventario.
6. Abre `nexus7_telemetria.csv`: hay una fila por fase jugada.

## Pruebas automáticas

`bash Tests/run_all.sh` compila con mcs y con Roslyn, como Unity, y ejecuta 1204 comprobaciones sin abrir Unity. `BalanceTest` vigila las reglas que unen sistemas, por ejemplo:

- el sello de la salida dura más que cualquier encuentro;
- con más tensión los robots nunca son más flojos;
- Fácil y la última vida nunca sellan la salida.

Si ajustas un parámetro y una de estas reglas falla, el juego se ha vuelto incoherente.

## Estado del plan

El plan de mejoras está **completo** (Etapas 1 a 24). Lo que queda abierto está en «Límites conocidos» de la guía de armas y director. Lo más importante:

- todo se ha comprobado con pruebas automáticas y con la compilación de Roslyn, y se ha ido probando en Unity a lo largo de las entregas;
- el mando puede necesitar otros ejes fuera de Windows;
- los accesorios no se ven en el modelo del arma.
