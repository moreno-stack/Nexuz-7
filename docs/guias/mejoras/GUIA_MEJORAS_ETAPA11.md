# Escape Espacial: Nexus-7. Mejoras, Etapa 11 (sonido, hackeo con interferencias, estamina y checkpoints)

Cuatro mejoras transversales para que cada huida, cada hackeo y cada caída tengan peso: los robots **oyen** de verdad, el hackeo **reacciona** a la tensión, correr **cansa** y caer **cuesta algo** sin hacerte perder lo importante.

## Cómo aplicarlo

Los scripts funcionan solos al abrir el proyecto (la estamina se añade sola al jugador). Para tener también las barras de **ESTAMINA** y **RUIDO** en el HUD, reconstruye con **Nexus-7 → Construir todo el juego**.

## 1. Sonido que se propaga

Todo lo que haces emite una **onda de sonido invisible** (`NoiseSystem`) con un radio. Un robot solo reacciona si la onda le llega:

| Acción | Radio |
|---|---|
| Quieto | 0 (silencio) |
| Caminar agachado | **1 m** (casi silencio) |
| Caminar | **6 m** |
| Correr | **12 m** |
| Aterrizar de un salto | según la altura (agachado, la mitad) |
| Jadear agotado | 3 m cada 0,8 s |
| Abrir un contenedor | 5 m |
| Hackear (tecleo, en terminales y consolas) | **3 m cada segundo** mientras dura |
| Error al hackear | 6 m |
| Hackeo fallido | 10 m (alarma) |

**Paredes y cajas amortiguan.** Entre el sonido y el «oído» del robot se lanza un rayo: cada pared, caja o estantería que cruza **reduce el radio a la mitad** (se cuentan como mucho 2). Correr al otro lado de un muro se oye a 6 m, no a 12. No amortiguan los robots, el jugador ni el objeto que hizo el ruido, y varias cajas de colisión pegadas (una pared hecha de piezas, una barandilla sobre la pasarela) cuentan como un solo obstáculo.

**El robot no sabe el punto exacto.** Va a investigar a una **posición estimada**, con un error de hasta el **25 % de la distancia**: si te oye a 10 m, puede ir a buscar a 2,5 m de donde estabas. El error se mantiene unos 3 s para la misma fuente (no se corrige paso a paso) y no cambia de ruta si el nuevo sonido cae a menos de 1,5 m de donde ya va. Durante las cinemáticas no oye nada. Los señuelos de la Fase 4 son la excepción: son balizas hechas para atraerlo y se localizan bien.

El oído de cada robot se multiplica por su `Hearing Multiplier` (los de élite, ×1,4) y por la alerta de la nave (×1,2 en Amarilla, ×1,5 en Roja).

**Medidor de RUIDO.** Abajo a la izquierda, encima del escudo, una barra muestra los metros de tus pasos (`RUIDO 6 m`). Aparece al moverte e incluye también el jadeo y los aterrizajes.

## 2. Interferencias en el hackeo

Mientras introduces la secuencia puede aparecer **«¡INTERFERENCIA!»**: los códigos se convierten en símbolos basura y **no responden** hasta que pulses **[Espacio]** (mando: **RB**). Una barra morada muestra el margen que queda.

- **A tiempo:** sigues donde ibas. Ojo: si pulsas [Espacio] **sin** interferencia, no responde durante 0,6 s (no vale machacarlo).
- Durante la interferencia se sigue viendo el tiempo y los errores; el fondo parpadea despacio (1,5 veces por segundo).
- **Tarde:** cuenta como un **error** (−segundos, pitido de 6 m y +5 de tensión en la nave). Con demasiados errores, el hackeo falla.

| Alerta de la nave | Frecuencia | Margen para reaccionar |
|---|---|---|
| Normal | ninguna en fácil · **1** en media y difícil | 1,4 s |
| Amarilla | cada **5–7 s** | 1,1 s |
| **Roja** | cada **3–4,5 s** | **0,8 s** |

La primera nunca aparece antes de 1,5 s. La alerta que cuenta es la del momento en que empiezas a hackear.

## 3. Estamina

| Parámetro (`PlayerStamina`) | Valor |
|---|---|
| Estamina | 100 |
| Gasto al correr | **17/s** (unos **6 s** de carrera) × dificultad: fácil 0,75 · normal 1 · difícil 1,25 |
| Salto | 8 |
| Espera antes de recuperar | 1 s sin correr |
| Recuperación | **14/s** caminando · **22/s** quieto o agachado |
| Agotado | al llegar a 0; no puedes correr hasta tener **35** |

**Por qué importa:** tu carrera es de 6 m/s y los robots de élite en Alerta Roja persiguen a 5,98 m/s. Antes podías huir sin fin; ahora, a los 6 s caminas (3,2 m/s) y te alcanzan. Rompe la línea de visión, escóndete agachado o usa un señuelo o un PEM antes de quedarte sin aliento. Además, agotado **jadeas** y eso se oye.

La barra de **ESTAMINA** aparece solo cuando no está llena; se vuelve roja con **AGOTADO**. Al reaparecer se rellena. Mantener [Mayús] agachado solo te pone de pie si de verdad vas a correr (hacia delante y con estamina): agotado detrás de una caja, sigues agachado.

## 4. Checkpoints inteligentes

Al caer (capturado, fuera de combate o por una descarga):

| Qué | Qué pasa |
|---|---|
| Dispositivos fabricados (señuelo, PEM, camuflaje) | **Se conservan** |
| Herramientas y objetos clave (tarjetas, piezas de misión) | **Se conservan** |
| Recursos menores (chatarra, baterías, componentes) | Se pierde el **25 %** de cada montón, redondeando hacia abajo (8 → −2, 5 → −1, 3 → 0) |
| Cuenta atrás de la Fase 6 | **−15 s**, pero nunca te deja por debajo de **60 s** |
| Estamina | Se rellena |

Al volver, una notificación te dice qué has perdido. Ningún objetivo del juego necesita más de 2 unidades de un recurso, y un montón de n ≥ 2 siempre conserva al menos min(n, 3), así que caer nunca te deja atascado.

**No se guarda en plena huida.** Si cruzas un punto de control mientras un robot te persigue, aparece «Punto seguro bloqueado: te están persiguiendo» y se guarda **en cuanto te pierdan**. Si te atrapan antes, no cuenta. Si cruzas varios durante la misma persecución, se guarda el último. Los robots congelados o fuera del NavMesh no bloquean los checkpoints.

## Pruebas

`bash Tests/run_all.sh` compila todo (también con Roslyn) y ejecuta `TacticsTest`, que comprueba:
- la propagación del sonido (paredes, oído fino, posición estimada) y los radios de los pasos;
- la estamina (6 s de carrera, espera, recuperación, agotado y umbral del 35 %, salto, dificultad, relleno);
- las interferencias (frecuencia y margen por alerta, primera tras 1,5 s, fallo por no reaccionar, errores del hackeo);
- y las penalizaciones al caer (recursos menores, objetos protegidos, cuenta atrás con su suelo de 60 s y checkpoints durante una persecución).
