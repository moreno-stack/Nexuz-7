# Escape Espacial: Nexus-7

Juego de **sigilo en primera o tercera persona** hecho en **Unity (C#)**. Despiertas en una nave cuyos robots se han rebelado y tienes que llegar a la cápsula de escape antes de la autodestrucción, sin pelear: escondiéndote, fabricando dispositivos y hackeando sistemas.

> Proyecto académico de Samuel (Ingeniería de Software, Universidad Cooperativa de Colombia, sede Pasto).

## Dónde está cada cosa

```
Assets/_Nexus7/Scripts/   ← todo el código del juego (C#)
docs/guias/RESUMEN_JUEGO.md ← resumen del juego completo y prueba rápida de 15 minutos
docs/guias/fases/         ← guías de las 6 fases (con su estado actual al principio)
Tests/                    ← pruebas automáticas (1196 comprobaciones, sin Unity)
docs/guias/mejoras/       ← guías de las mejoras (etapas 0 a 8)
docs/superpowers/         ← diseño (specs) y planes de implementación
docs/media/               ← vídeo de la maqueta y capturas
```

### Código (`Assets/_Nexus7/Scripts`)

| Carpeta | Qué contiene |
|---|---|
| `Player` | Movimiento, salto, cámara 1ª/3ª persona (V), salud, escudo del traje |
| `Characters` | Modelos redondeados generados por código y animación procedural |
| `Enemies` | Patrulla, visión, ataque cuerpo a cuerpo y armas de los robots |
| `Inventory` | Inventario, barra rápida 1–9, botín aleatorio con objetos garantizados |
| `Crafting` | Bancos de trabajo y recetas |
| `Devices` | Señuelo, pulso PEM, camuflaje óptico y kit de reparación |
| `Stealth` | Sombras, ruido y visibilidad del jugador |
| `Security` | Láseres, cámaras, hackeo |
| `Power` | Red eléctrica de la sala de energía |
| `Hazards` | Cables y trampas eléctricas |
| `Interaction` | Puertas, terminales, taquilla del traje, objetos interactivos |
| `Cinematics` | Cinemáticas con subtítulos en español |
| `Combat` | Armas del jugador, proyectiles, vida de los robots, munición y HUD de combate |
| `Director` | IA que analiza al jugador, dificultad adaptativa y reaparición sin repeticiones |
| `Gameplay` | Flujo de la partida, alertas, reaparición, finales y puntuación |
| `Core` | Dificultad, sesión de juego y estadísticas |
| `Environment` | Luces, efectos y ambientación |
| `Tutorial` | Tutorial de la fase 1 e intros de nivel |
| `UI` | HUD, menú principal, resultados y paneles |
| `Editor` | Constructores que generan las 6 escenas desde el menú **Nexus-7** de Unity |

## Cómo abrirlo en Unity

El repositorio es un proyecto de Unity completo (**Unity 6000.6.4f1**, URP), con `ProjectSettings/` y `Packages/` incluidos.

1. En **Unity Hub → Proyectos → Añadir → Añadir proyecto desde repositorio**, elige GitHub, el repositorio `moreno-stack/Nexuz-7` y la rama `main`. También puedes clonarlo con git y usar **Añadir proyecto desde disco**.
2. Ábrelo con Unity 6000.6.4f1. La primera vez tarda unos minutos porque Unity crea la carpeta `Library/`, que no se sube a git.
3. Construye las escenas: al abrir el proyecto por primera vez, Unity pregunta si quieres construirlas (pulsa **Construir**). También puedes usar el menú **Nexus-7 → Construir todo el juego (Fases 1–6 y menú)**. Tarda uno o dos minutos y deja abierta la escena `MenuPrincipal`.
4. Pulsa **Play**.

Los objetos imprescindibles de cada fase (por ejemplo, las baterías de la Fase 2) salen en sitios al azar en cada partida: búscalos por su brillo y el haz de luz vertical de su color (6 m de alto). Se recogen con **[E]**. Si tardas, el traje avisa cada minuto de la distancia y la dirección al más cercano, y la consola de Unity dice dónde salió cada uno («[Botín] … imprescindibles → …»).

Si al salir de una fase aparece «Falta … en la lista de escenas» o «FIN DE LA FASE» en lugar de cargar la siguiente, la fase no está en *File → Build Profiles → Scene List*. Unity las registra solo al abrir el proyecto, y también puedes forzarlo con **Nexus-7 → Registrar las fases en Build Settings**. Si la fase siguiente aún no está construida, usa **Nexus-7 → Construir todo el juego**.

El proyecto ya trae TextMeshPro, los ajustes de URP y *Active Input Handling* en **Both** (teclado, ratón y mando).

## Fases

| Fase | Escena | Enemigos | Objetivo |
|---|---|---|---|
| 1 | Sala inicial | — | Ponerse el traje espacial y salir del corredor |
| 2 | Almacén de carga | Seguridad (bate) | Conseguir 2 baterías para el montacargas |
| 3 | Sala de energía | Guardias (llave inglesa) | Restaurar los 4 bloques de energía |
| 4 | Laboratorio | Investigación (porra eléctrica) | Conseguir la tarjeta del director |
| 5 | Centro de control | Élite (hacha) | Autorizar la compuerta y llegar al puente |
| 6 | Evacuación | Élite + seguridad | Lanzar la cápsula B antes de 5 minutos |

## Controles

| Tecla | Acción |
|---|---|
| W A S D | Moverse |
| Shift | Correr (hace ruido) |
| Ctrl / C | Agacharse |
| Espacio | Saltar · mantener para saltar una cinemática |
| V | Cámara 1ª / 3ª persona |
| E | Interactuar |
| 1–9 / rueda | Elegir objeto de la barra rápida |
| Clic izquierdo | Disparar (mantener con el rifle) |
| Clic derecho | Apuntar |
| R | Recargar |
| Q | Cambiar de arma |
| G | Usar o lanzar el dispositivo |
| F3 | Panel del director adaptativo (métricas en vivo) |
| F4 | Panel de pruebas, solo en el editor y en compilaciones de desarrollo (con él abierto, F5–F12: objetos imprescindibles, munición, accesorio, vida, tensión, evento de nave, fase siguiente) |
| Mando: RT / LT | Disparar / apuntar |
| Mando: cruceta ◄ ► / ▼ | Cambiar de arma / recargar |
| Tab / I | Inventario |

Dificultad (menú principal): **Fácil ×1,0 · Normal ×1,5 · Difícil ×2,0** de daño. Durante la partida, el director adaptativo ajusta la presión según cómo juegas, y al caer los robots no reaparecen en el mismo sitio. Los robots de seguridad, guardia y élite disparan a distancia, siempre con un láser rojo de aviso antes de cada ráfaga. Se cubren tras cajas y consolas, se asoman a disparar, los guardias avanzan y la élite flanquea; apunta a su óptica para deslumbrarlos o a la espalda de la élite (núcleo cian, ×3), y dispárales al brazo del arma para desarmarlos. Cada fase tiene además un encuentro por oleadas que se activa al cumplir su objetivo: mientras dura, la salida queda sellada, y al despejarlo la recompensa depende de lo que te falte. Tienes 5/3/2 vidas por fase (Fácil/Normal/Difícil); sin vidas la fase se reinicia con otra colocación, y en cada intento el director marca a algunos robots como blindados, rápidos u ojo de águila. También lanza eventos de nave (apagón, fuga de vapor o barrido de seguridad) según cómo vas, y al empezar cada fase te enseña el informe de la IA sobre la anterior. Las armas aceptan hasta 2 accesorios (supresor, cargador ampliado, estabilizador, disipador), y el director los regala al despejar encuentros o destruir élites según cómo juegas. La IA aprende tus rutas (manda una unidad a vigilar donde más te ha visto) y cada fase deja una línea con tus cifras en `nexus7_telemetria.csv`. Al reaparecer, una tarjeta te dice quién te vio, qué pesó (luz, correr, ruido) y un consejo. Detalles en [`docs/guias/mejoras/GUIA_ARMAS_DIRECTOR.md`](docs/guias/mejoras/GUIA_ARMAS_DIRECTOR.md).

## Menú y pantalla final

![Menú principal](docs/media/etapa8_menu.jpg)

![Pantalla final](docs/media/etapa8_final_victoria.jpg)

Capturas de previsualización generadas desde el código del proyecto (en Unity la luz y los brillos se ven mejor). Detalles en [`docs/guias/mejoras/GUIA_MEJORAS_ETAPA8.md`](docs/guias/mejoras/GUIA_MEJORAS_ETAPA8.md).

## Pruebas

```bash
bash Tests/run_all.sh
```

Compila todo el código, incluidos los constructores de escena, y ejecuta las 1196 comprobaciones con Mono, sin abrir Unity. Los detalles están en [`Tests/README.md`](Tests/README.md).
