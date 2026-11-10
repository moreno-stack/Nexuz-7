# Escape Espacial: Nexus-7. Mejoras, Etapa 9 (gráficos: posprocesado, partículas, materiales y luces)

Esta etapa añade una **capa visual** encima de todas las salas sin tocar la jugabilidad. La pone el script de editor `VisualUpgrade`, que se ejecuta solo al construir cada fase y que también se puede lanzar a mano. Todo se genera por código (texturas incluidas), así que el proyecto no necesita descargar nada.

## Cómo aplicarlo a las escenas que ya tienes

1. Abre el proyecto en Unity y espera a que termine de compilar.
2. Elige una de estas dos opciones:
   - **Nexus-7 → Construir todo el juego (Fases 1–6 y menú)**: reconstruye todas las escenas y aplica la capa visual a cada una (recomendado).
   - **Nexus-7 → Gráficos → Aplicar mejoras visuales a todas las escenas**: aplica la capa visual a las escenas que ya existen, sin reconstruirlas.
3. Para probar un cambio en una sola sala, abre su escena y usa **Nexus-7 → Gráficos → Aplicar mejoras visuales a la escena abierta**.
4. Pulsa **Play** en `MenuPrincipal`.

Se puede aplicar las veces que quieras: no duplica efectos (busca primero si ya están) y no toca las luces ni las pantallas que ya controla otro script del juego (las de alerta, las de emergencia, las que dependen de la energía, etc.).

## Qué crea y dónde

| Qué | Dónde |
|---|---|
| Perfil de posprocesado | `Assets/_Nexus7/Data/Visual/Nexus7_PostProceso.asset` |
| Texturas de superficie (color, normal y metal/suavidad) | `Assets/_Nexus7/Data/Visual/Texturas/T_<Tipo>_Color/Normal/MetalSuavidad.png` |
| Material de las pantallas holográficas flotantes | `Assets/_Nexus7/Data/Visual/M_HoloFlotante.mat` |
| Materiales de las partículas (aditivo y transparente) | `Assets/_Nexus7/Data/Visual/FX_Aditivo.mat` y `FX_Transparente.mat` |
| Volumen global en cada escena | objeto `VolumenPostProceso` (con `Volume` + `TensionPostFX`) |
| Partículas | objetos `FX_Chispas`, `FX_Vapor` y `FX_Polvo` |
| Hologramas flotantes | objetos `FX_HoloFlotante` |

En el menú principal solo se añaden el volumen y el posprocesado de la cámara; las partículas, los hologramas y las sombras extra son para las salas jugables.

## 1. Posprocesado (volumen global de URP)

Parámetros recomendados (son los que pone `VisualUpgrade`; vienen de `PostFxModel.Defaults`):

| Efecto | Parámetro | Valor | Por qué |
|---|---|---|---|
| Bloom | Threshold | 1 | Solo brillan las luces y pantallas emisivas (HDR), no las paredes |
| | Intensity | 0,8 | Halo visible sin lavar la imagen |
| | Scatter | 0,7 | Halo amplio, de ambiente industrial |
| | High Quality Filtering | No | Más barato y casi igual |
| Vignette | Intensity | 0,32 (sube con la tensión) | Bordes oscuros que encierran la vista |
| | Smoothness | 0,45 | Transición suave |
| Chromatic Aberration | Intensity | 0 (lo controla `TensionPostFX`) | Solo aparece con tensión |
| Tonemapping | Mode | ACES | Contraste de cine y luces que no se queman |
| White Balance | Temperature / Tint | −12 / −2 | Tono frío, azulado |
| Color Adjustments | Post Exposure | 0,1 | Compensa la viñeta |
| | Contrast | 12 | Más contraste |
| | Saturation | −10 (baja con la tensión) | Ambiente industrial apagado |
| Shadows Midtones Highlights | Shadows | (0,90; 0,97; 1,12) | Sombras azuladas |
| | Highlights | (1,06; 1,02; 0,96) | Luces algo cálidas |
| Film Grain | Type / Intensity | Thin1 / 0,12 | Textura de cámara sutil |

**Tensión (`TensionPostFX`).** Cada fotograma calcula el estado objetivo y se acerca a él poco a poco:

| Situación | Aberración cromática | Viñeta | Saturación | Tinte |
|---|---|---|---|---|
| Calma | 0 | 0,32 | −10 | ninguno |
| Alerta Amarilla | 0,15 | 0,36 | −15 | ninguno |
| Alerta Roja | 0,35 | 0,42 | −25 | rojizo (1; 0,84; 0,82) |
| Golpe recibido | +0,45 que se apaga en 0,4 s | — | — | — |
| Vida baja (< 35 %) | — | hasta +0,08 | — | rojizo, más cuanto menos vida |

Para cambiar los valores: selecciona `Nexus7_PostProceso.asset` y edítalo en el Inspector (los cambios de aberración, viñeta, saturación y filtro de color los recalcula `TensionPostFX` en juego; sus velocidades se ajustan en el componente).

**Ajustes de URP (`Assets/Settings/PC_RPAsset.asset`).**
- HDR: activado (lo necesita el bloom).
- Grading Mode: High Dynamic Range (mejor con ACES).
- Distancia de sombras: 40 m con 2 cascadas (es un interior: no hace falta más).
- Sombras suaves: calidad media.
- Atlas de sombras de luces adicionales: 4096.

## 2. Partículas ambientales

Todas se configuran por código al empezar la partida y **se detienen cuando la cámara está lejos** (más de 18–25 m), así no gastan nada en otras zonas de la nave.

| Efecto | Dónde lo pone | Cuántos por escena | Detalles |
|---|---|---|---|
| Chispas (`DamagedPanelSparks`) | cables pelados, cables, paneles, transformadores, carcasas, tableros | 3 | Ráfagas de 8–20 chispas cada 0,8–3,5 s, con gravedad y un destello de luz naranja de 0,1 s |
| Vapor (`SteamVent`) | conductos, tuberías y rejillas de ventilación | 3 | Soplos de 1,2–2,5 s cada 2,5–6 s; crecen y se desvanecen |
| Polvo (`DustMotes`) | debajo de las lámparas del techo | 4 | Motas que flotan despacio dentro del haz de luz |

Para poner un efecto a mano: crea un objeto vacío, colócalo y añádele `DamagedPanelSparks`, `SteamVent` o `DustMotes` (el `ParticleSystem` se añade y configura solo). Arrastra `FX_Aditivo` (chispas, polvo) o `FX_Transparente` (vapor) al **Material** de su `Particle System Renderer`: los materiales guardados como asset aseguran que la versión transparente del shader entre en la build. Las chispas iluminan si les asignas una luz puntual en el campo **Flash**. El vapor sale hacia el eje Z azul del objeto.

## 3. Materiales y pantallas

**Superficies con relieve.** `VisualUpgrade` recorre los materiales de `Assets/_Nexus7/Materials` y, según su nombre, les asigna:

| Tipo | Materiales (por su nombre) | Repeticiones |
|---|---|---|
| Suelo (placas con juntas y remaches) | «suelo» | 4 |
| Pared (paneles) | «pared», «muro», «panel» | 2 |
| Metal oxidado (óxido, arañazos) | «metal», «estanteria», «tuberia», «estructura», «puerta», «mesa», «casco», «marco»… | 1,5 |
| Cajas y contenedores | «caja», «bidon», «contenedor» | 1 |

Cada uno recibe un mapa **normal** (relieve) y un mapa de **metal/suavidad** (R = metal, A = suavidad; la rugosidad es 1 − suavidad). En el metal oxidado, el óxido es rugoso y no metálico, y el metal limpio brilla. El color propio de cada material se mantiene: la textura de color es casi gris y se multiplica por él. Las pantallas, luces y cristales no se tocan.

Las repeticiones son por cara de objeto, así que en un suelo o una pared muy grandes las placas se ven más grandes. Si quieres placas más pequeñas en un objeto concreto, duplica su material y sube **Tiling** en Base Map (el mapa normal y el de metal usan el mismo).

Para usar las texturas en otro material: en el Inspector del material (URP Lit) pon `T_Rust_Color` en **Base Map**, `T_Rust_Normal` en **Normal Map** y `T_Rust_MetalSuavidad` en **Metallic Map**, con **Smoothness** a 1 y **Source** en «Metallic Alpha».

**Paneles que parpadean (`EmissiveFlicker`).** Las pantallas, monitores, terminales y tableros emisivos que no controla otro script parpadean como equipos reales: casi siempre estables, con temblores y apagones cortos (1 de cada 4 está «averiada»). Usa `MaterialPropertyBlock`, así que no crea materiales nuevos.

**Hologramas con fallo digital (`HoloGlitch`).** Los paneles holográficos y las 3 pantallas holográficas flotantes (`FX_HoloFlotante`, sobre terminales y consolas) dan saltos laterales, se estiran, se cortan un instante y tienen líneas de escaneo que suben. El material flotante es aditivo y de doble cara, con color HDR para que brille con el bloom.

## 4. Luces y sombras

**Parpadeo (`FlickerLight`).** Las lámparas del techo que no controla otro script reciben un parpadeo: zumbido de fluorescente (casi fijo) o, en 1 de cada 5, avería (oscila y se corta a tirones). La caja de la lámpara parpadea a la vez. Las luces de emergencia y de alerta ya parpadeaban con sus propios scripts y se respetan (solo cuentan como «ocupadas» las que controla un script del juego, no los componentes de URP).

**Sombras suaves.** Las luces que ya tenían sombra pasan a sombra suave (fuerza 0,85; el sesgo lo pone URP con sus ajustes generales). Después se reparte un presupuesto de **18 «caras» de sombra** por escena: una luz puntual cuesta 6 (URP la dibuja como un cubo) y un foco 1, y cuentan también los focos de los robots. Si queda sitio, se añaden las lámparas del techo encendidas que tienen **más estanterías, cajas, mesas y contenedores cerca** (en un radio de 5 m), para que esos objetos proyecten sombras reales donde el jugador pueda esconderse. Esas estanterías y cajas se marcan para proyectar sombra. El resto de luces siguen sin sombra por rendimiento.

## Rendimiento

- Partículas: como máximo 60–90 por efecto y 10 efectos por escena, y se paran lejos de la cámara.
- Texturas: 256 × 256 que se repiten, generadas una sola vez.
- Parpadeos y hologramas: sin crear materiales (`MaterialPropertyBlock`).
- Posprocesado: bloom sin filtrado de alta calidad, una sola capa de volumen.
- Sombras: hasta 18 caras de sombra por escena (unas 3 luces puntuales) y 2 cascadas para la luz principal.

Si va lento en un equipo modesto: baja **Bloom → Intensity**, desactiva **Film Grain** en el perfil, o reduce `MaxShadowTiles` en `VisualUpgrade.cs`.

## Pruebas

`bash Tests/run_all.sh` compila todo (también con Roslyn, el compilador de Unity) y ejecuta `VisualFxTest`, que comprueba los parpadeos, el fallo de los hologramas, los temporizadores, los valores del posprocesado según la tensión, el apagado por distancia, la elección de luces con sombra, la clasificación de materiales, el reparto de efectos y las texturas generadas (que se repiten sin costura y tienen normales válidas).
