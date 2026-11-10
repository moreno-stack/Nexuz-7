# Escape Espacial: Nexus-7. Mejoras, Etapa 10 (visión y percepción de los robots)

Los robots y drones ahora ven con reglas estrictas, iguales para todos: un cono y un alcance que no se pueden saltar, rayos que tapan las cajas, las paredes y el humo, un alcance que cambia con la luz y el sigilo, y una memoria que solo recuerda lo que vieron. Las cámaras de seguridad usan el mismo sistema, aunque conservan su cono más estrecho.

## Cómo aplicarlo

Los scripts funcionan solos al abrir el proyecto. Para que las escenas usen los valores nuevos (humo que tapa en la Fase 6, robots de élite a 14 m y 90°), reconstruye con **Nexus-7 → Construir todo el juego**.

## 1. Cono de visión y distancia máxima

| Parámetro (`EnemyVision`) | Valor | Qué hace |
|---|---|---|
| View Angle | 80° (robots: siempre entre 75° y 90°) | Apertura total del cono, medida en horizontal. Fuera, el robot está **totalmente ciego** |
| Vertical Half Angle | 60° | Robots: cuánto puede mirar por encima o por debajo de su mirada (así te ve si estás agachado justo delante) |
| View Distance | 12 m (élite: 14 m · cámaras: 13 m) | Alcance base en un pasillo |
| Max View Distance | **15 m** | Tope duro: ni la Alerta Roja ni el +30 % lo superan |
| Smoke View Distance | **6 m** | Tope si el robot o el jugador están dentro del humo |

**Alcance real = base × alerta de la nave × sigilo**, y después el tope:

| Situación | Cálculo | Alcance |
|---|---|---|
| Normal | 12 × 1 × 1 | 12 m |
| Agachado en sombra o con camuflaje | 12 × 1 × 0,6 | 7,2 m |
| Corriendo o bajo luz directa | 12 × 1 × 1,3 = 15,6 | **15 m** (tope) |
| Alerta Amarilla, normal | 12 × 1,15 | 13,8 m |
| Alerta Roja y corriendo | 12 × 1,3 × 1,3 = 20,3 | **15 m** (tope) |
| Dentro del humo | — | como mucho **6 m** |

Ya no existe el «sexto sentido» de 1,6 m que te detectaba por la espalda: si te acercas por detrás sin hacer ruido, no te ve, y **tampoco te puede golpear ni atrapar** si no te ve (si ya había empezado un golpe, lo termina). Lo que sí sigue funcionando es su **oído**: correr, romper cosas o lanzar un señuelo hace ruido.

## 2. Oclusión: 5 rayos al cuerpo

Desde el ojo del robot salen 5 rayos hacia tu **cabeza** (90 % de tu altura), **pecho** (60 %), **cadera** (35 %) y **los dos hombros** (75 %, separados 22 cm a cada lado de su línea de mirada). Al agacharte los puntos bajan contigo.

- Te ve si llega **al menos un rayo**. Si no llega ninguno, deja de verte en ese mismo fotograma.
- Cuantos más rayos lleguen, más rápido sube su alerta: con un solo hombro asomado, al 52 % de la velocidad; con todo el cuerpo a la vista, al 100 %.
- Tapan los rayos las paredes, cajas, estanterías y cualquier objeto con colisionador (menos los de la capa *Ignore Raycast*).
- **El humo denso** (`VisionSmoke`) también: si un rayo atraviesa **1,5 m o más** de humo, queda tapado. En la Fase 6 cada columna de humo lleva un `VisionSmoke` con un radio de 0,8 m más que el del emisor (1,8 m en las de emergencia y 2,2 m en las del aviso final).

Con el robot seleccionado y el juego en marcha, la vista *Scene* dibuja los 5 rayos (rojos si te ve, grises si no), el cono base (amarillo) y el tope (rojo).

## 3. Luz y sigilo

| Estado | Alcance de los robots | Cuándo |
|---|---|---|
| **Oculto** | ×0,6 (−40 %) | Agachado dentro de una zona de sombra (incluido el humo) o con el **camuflaje óptico** activo |
| Normal | ×1 | El resto del tiempo |
| **Expuesto** | ×1,3 (+30 %) | Corriendo o **bajo luz directa** |

Si se cumplen las dos cosas a la vez (por ejemplo, agachado en sombra bajo una lámpara), gana **oculto**.

**Luz directa (`PlayerLightExposure`).** Se añade sola al jugador. Cinco veces por segundo comprueba si estás en el **charco de luz** de alguna lámpara encendida (intensidad ≥ 0,5) y si **no hay nada en medio**:
- **Lámpara:** a menos de 1,5 m en horizontal o del 25 % de su alcance, lo que sea mayor. Por ejemplo, 2,5 m bajo las lámparas del laboratorio y 4 m bajo las del centro de control. Además tienes que estar a menos del 80 % de su alcance.
- **Foco:** dentro de su haz y a menos del 60 % de su alcance.
- **No cuentan** los focos de visión de los robots ni los de las cámaras. Dentro de una zona de sombra nunca cuentas como iluminado.
- El estado se mantiene 0,5 s, así las luces que parpadean no lo hacen saltar. Las luces que se apagan por falta de energía o por una alerta dejan de contar enseguida.

Ten en cuenta el tope: con el alcance base de 12 m, el +30 % llega a 15,6 m y se queda en 15 m (+25 %). En Alerta Amarilla o Roja, el robot ya está cerca del tope, así que exponerte pesa menos que ocultarte.

El indicador de sigilo muestra **ILUMINADO** cuando estás bajo luz directa y ningún robot sospecha de ti.

La visibilidad que ya existía (agachado ×0,5, quieto ×0,8, corriendo ×1,4, en sombra ×0,45) sigue decidiendo **lo rápido** que se llena la alerta; el nuevo multiplicador decide **hasta dónde** llega su vista.

## 4. Memoria de la última posición

1. Cuando te ve con la alerta al máximo, te **persigue** y se actualiza su destino 5 veces por segundo.
2. Si te pierde (una caja, una pared o el humo te tapan) durante más de **0,25 s**, va **solo al último punto donde te vio** y recuerda hacia dónde corrías. No vuelve a apuntar a tu posición real mientras no te vea. El margen evita que un parpadeo de un fotograma corte la persecución.
3. Al llegar (o si en **8 s** no ha conseguido llegar), **busca durante 6 s**. Primero avanza **3 m** en la dirección en que huías, sin atravesar paredes (como mucho la mitad del tiempo). Después gira mirando a un lado y luego al otro. En Alerta Roja, los robots que cazan no vuelven atrás a su propio avistamiento a mitad de la búsqueda.
4. Si no te encuentra, **vuelve a su patrulla** por el punto de ruta más cercano.
5. Aunque su alerta siga alta, **solo vuelve a perseguirte si te ve otra vez**.

Lo que comparten los robots (el escuadrón que acude al aviso de un compañero y la caza en Alerta Roja) es siempre la **última posición vista** por alguno de ellos, nunca tu posición real.

| Parámetro (`EnemyPatrol`) | Valor | Qué hace |
|---|---|---|
| Search Duration | 6 s | Tiempo de búsqueda antes de volver a patrullar |
| Search Probe Distance | 3 m | Cuánto avanza hacia donde huías |
| Max Pursuit Time | 8 s | Tiempo máximo yendo a la última posición |
| Lost Sight Grace | 0,25 s | Margen antes de darte por perdido |

## Consejos para jugar

- Agáchate detrás de una caja o una estantería: si los 5 rayos quedan tapados, el robot te pierde aunque estés a 2 m.
- Rompe la línea de visión y cambia de dirección: el robot buscará hacia donde corrías.
- Evita las lámparas cuando un robot mire hacia ti: bajo luz directa te ve un 30 % más lejos.
- En la Fase 6, el humo es tu mejor amigo: dentro de él, los robots no ven a más de 6 m y no te ven a través de él.

## Pruebas

`bash Tests/run_all.sh` compila todo (también con Roslyn, el compilador de Unity) y ejecuta `PerceptionTest`, que comprueba:
- el sigilo y sus multiplicadores, el alcance con el tope duro y el de humo;
- el cono (también el horizontal de los robots, sin punto ciego bajo el ojo), el humo que tapa los rayos y los 5 puntos del cuerpo (de pie, agachado y en otras direcciones);
- la velocidad de detección según los rayos, la luz directa (lámparas y focos) y el punto de búsqueda;
- y el comportamiento del robot: no persigue sin verte, no te da por perdido por un parpadeo, al perderte va solo a la última posición y no la actualiza, busca 6 s y vuelve a patrullar, y si no llega en 8 s busca donde esté.
