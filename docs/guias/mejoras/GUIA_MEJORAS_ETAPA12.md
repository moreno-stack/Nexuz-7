# Escape Espacial: Nexus-7. Mejoras, Etapa 12 (menú principal, partida guardada, configuración y audio)

Ahora el juego **siempre empieza por el Menú Principal**: al pulsar Play en Unity y al abrir el juego compilado. El menú tiene **Nueva partida**, **Continuar**, **Configuración** y **Salir**.

## Por qué antes empezaba directamente la partida

1. **"Construir todo el juego" se rompía en la Fase 2.** Al enlazar la Fase 1, el constructor abría `SalaInicial` y luego intentaba seleccionar al jugador de la escena que acababa de cerrar (`MissingReferenceException` en `AlmacenCargaBuilder.cs:235`). Las Fases 3, 4 y 5 tenían el mismo fallo. Por eso nunca se creaba la escena `MenuPrincipal`.
2. **En el editor, Play arranca la escena que tengas abierta**, no la del índice 0 de Build Settings.

Ahora los cuatro constructores buscan de nuevo al jugador. Además, "Construir todo" sigue con las demás fases si una falla, ordena Build Settings con el menú primero y te muestra un resumen al terminar. Y Play arranca siempre el menú.

## Cómo probarlo

1. En Unity: **Nexus-7 → Construir todo el juego**. Al terminar sale un aviso: "Juego construido: 6 fases y menú principal." Si alguna fase falla, el aviso dice cuál y el error está en la ventana Console.
2. Pulsa **Play**, tengas abierta la escena que tengas.
3. Para probar una sala suelta, desmarca **Nexus-7 → Empezar Play por el menú** y abre la escena de esa sala.

Otras opciones del menú Nexus-7:
- **Ordenar Build Settings (menú primero):** `MenuPrincipal` en el índice 0 y luego las Fases 1 a 6.
- **Actualizar catálogo de objetos:** vuelve a generar la lista de objetos que usa la partida guardada (se hace sola al construir).

## El menú

| Botón | Atajo | Qué hace |
|---|---|---|
| NUEVA PARTIDA | [Intro] · mando A | Empieza en la Fase 1. Si hay una partida guardada, pregunta antes: "Se sobrescribirá la partida guardada. ¿Empezar de nuevo?" (SÍ / NO; por defecto NO, [Esc] cancela) |
| CONTINUAR | [C] · mando Y | Carga la partida guardada desde el inicio de su fase. A la derecha muestra, por ejemplo, "Fase 3 · Sala de energía — hace 2 h". Está gris si no hay partida guardada |
| CONFIGURACIÓN | [O] · mando Start | Abre el panel de configuración |
| SALIR | (solo con el botón) | En el editor detiene Play; en el juego compilado lo cierra |

Las tarjetas de dificultad y el récord siguen igual: [←][→] o LB/RB cambian la dificultad. Con un panel abierto, los atajos del menú de fondo no responden.

## Configuración

| Opción | Valores | Por defecto | Qué cambia |
|---|---|---|---|
| Volumen general | 0–100 % | 80 % | Todo el sonido (`AudioListener.volume`) |
| Música | 0–100 % | 70 % | Música del menú y zumbido de la nave |
| Efectos | 0–100 % | 80 % | Sonidos de botones y de todo lo que haga ruido en las fases (al moverlo suena un pitido de prueba) |
| Calidad gráfica | Baja / Media / Alta | Alta | Ver la tabla siguiente |
| Pantalla completa | Sí / No | Sí | Pantalla completa sin bordes o ventana (en el editor solo se guarda) |

El volumen real sigue una curva (valor²), que el oído percibe como lineal: al 0 % hay silencio de verdad. Todo se guarda en PlayerPrefs (`Nexus7_VolGeneral`, `Nexus7_VolMusica`, `Nexus7_VolEfectos`, `Nexus7_Calidad`, `Nexus7_PantallaCompleta`) y se aplica en cada escena. [Esc], mando B o VOLVER cierran el panel. Con teclado o mando: ↑ ↓ para elegir y ← → para mover un slider.

| Calidad | Nivel de Unity | Posprocesado | Sombras | Antialiasing | Grano |
|---|---|---|---|---|---|
| Baja | Mobile | No | No | No | No |
| Media | PC | Sí | Sí | FXAA | No |
| Alta | PC | Sí | Sí | SMAA alto | Sí |

No se modifican los archivos de URP: en el editor, lo que se cambia durante Play se quedaría guardado en ellos. Solo cambian el nivel de calidad de Unity y los ajustes de cada cámara.

## Partida guardada

- **Dónde:** `nexus7_partida.json`, en la carpeta de datos del juego (`Application.persistentDataPath`; en Windows, `%USERPROFILE%\AppData\LocalLow\<compañía>\<producto>`).
- **Cuándo se guarda:** automáticamente **al entrar en cada fase**, un fotograma después de cargarla.
- **Qué guarda:** la escena, el inventario con el que entraste, si llevas el traje, la dificultad, la semilla de la partida (los mismos objetos aleatorios), el objeto de la barra rápida y las estadísticas (tiempo, muertes, objetos...).
- **Continuar:** vuelve al **inicio de esa fase** con ese inventario.
- **Cuándo se borra:** al terminar la partida, tanto al escapar como en la derrota final.
- **Si algo falla:** un archivo vacío, dañado o de otra versión cuenta como "sin partida". Si la escena guardada ya no está en Build Settings, Continuar se desactiva y muestra "Partida no disponible". Si un objeto guardado ya no existe, se omite con un aviso en Console.

## Transiciones (`SceneLoader`)

Todos los cambios de escena pasan por `SceneLoader`: fundido a negro (0,35 s), carga en segundo plano con la palabra CARGANDO y una barra de progreso, y fundido de entrada. Mientras carga, los clics se bloquean y una segunda petición se ignora: un doble clic en Nueva partida solo carga una vez. Se crea solo al arrancar; no hay que ponerlo en ninguna escena.

## Música y sonidos

El proyecto no tenía ningún archivo de audio, así que todo se genera por código (`ProceduralAudio`):
- **Menú:** pad ambiental de 16 s en bucle (La menor con novena) con una campana cada 4 s.
- **Fases:** zumbido grave y tenso de la nave, en bucle de 12 s y más bajo.
- **Botones:** al pasar por encima, al pulsar y al volver.

Al cambiar de escena, las pistas se cruzan con un fundido de 1,5 s. Cualquier `AudioSource` que añadas a una escena se pone solo en el canal Efectos (se le añade un `AudioChannel`). Para que un sonido sea Música, añádele `AudioChannel` y pon **Category = Music**.

## Jerarquía del menú (escena `MenuPrincipal`)

La construye `MenuScreenBuilder` (desde `EvacuacionBuilder.BuildMainMenuScene`). Si la montas o la retocas a mano, esta es la estructura y lo que enlaza cada campo:

```
MenuPrincipal
├─ MenuCamera                        (etiqueta MainCamera, con posprocesado)
├─ Sol
├─ Espacio                           (planeta, anillo, nave y restos: fondo 3D animado)
├─ CanvasMenu                        Canvas (Screen Space Overlay) + CanvasScaler 1920×1080 + GraphicRaycaster
│  │                                 + MainMenuUI + SettingsMenuUI + UISounds
│  ├─ Nebulosas, HaloPlaneta, Vineta, LineasEscaneo, PanelLateral, esquinas…   (decoración)
│  ├─ BloqueTitulo                   (título, subtítulo y lema)
│  ├─ BloqueBotones
│  │  ├─ BotonNuevaPartida           Button  → MainMenuUI.newGameButton
│  │  ├─ BotonContinuar              Button  → MainMenuUI.continueButton
│  │  ├─ DetalleContinuar            TMP     → MainMenuUI.continueDetail
│  │  ├─ BotonConfiguracion          Button  → MainMenuUI.settingsButton
│  │  └─ BotonSalir                  Button  → MainMenuUI.quitButton
│  ├─ BloqueDificultad               (3 tarjetas → difficultyButtons/Labels/Frames/Cards, PistaDificultad → difficultyHint)
│  ├─ BloqueRecord / Record          TMP     → MainMenuUI.bestScoreText
│  ├─ BarraControles
│  ├─ PanelConfiguracion             (empieza DESACTIVADO) → SettingsMenuUI.panel
│  │  ├─ Oscurecer                   Image a pantalla completa (bloquea los clics al menú)
│  │  └─ Panel
│  │     ├─ SliderGeneral / SliderMusica / SliderEfectos   Slider 0–100 → masterSlider / musicSlider / sfxSlider
│  │     │    └─ Fondo, ZonaRelleno/Relleno (fillRect), ZonaTirador/Tirador (handleRect)
│  │     ├─ ValorGeneral / ValorMusica / ValorEfectos        TMP → masterValue / musicValue / sfxValue
│  │     ├─ MarcoCalidad_0..2 + BotonCalidad_0..2            → qualityFrames / qualityButtons (0 Baja, 1 Media, 2 Alta)
│  │     ├─ BotonPantallaCompleta (+ su hijo Texto)          → fullscreenButton / fullscreenLabel
│  │     └─ BotonVolver                                      → backButton
│  │     (firstSelected = SliderGeneral)
│  └─ PanelConfirmacion              (empieza DESACTIVADO) → MainMenuUI.confirmPanel
│     └─ Panel / BotonSi, BotonNo    → MainMenuUI.confirmYes / confirmNo
├─ EventSystem                       EventSystem + StandaloneInputModule
└─ (en juego aparecen solos: SceneLoader, Musica, SonidosUI)
```

Notas para montarlo a mano:
- Los botones principales tienen **Navigation = None**, a propósito: así [Intro] no "pulsa" el botón seleccionado además del atajo, ni puede pulsar Salir sin querer. Dentro de los paneles la navegación es **Automatic**.
- El `Image` del fondo de cada botón y el `Tirador` de los sliders necesitan **Raycast Target** activado.
- Los nombres `BotonVolver` y `BotonNo` hacen que `UISounds` reproduzca el sonido de "volver" en lugar del de "pulsar".

## Pruebas

`bash Tests/run_all.sh` compila todo (también con Roslyn) y ejecuta las suites nuevas:
- `BuildReportTest`: el resumen de "Construir todo".
- `SettingsTest`: valores por defecto, límites, curva de volumen y qué hace cada calidad.
- `SaveTest`: tiempo relativo, descripción de Continuar, archivos dañados o de otra versión, inventario por Id y estadísticas.
- `SceneLoaderTest`: fundidos, bloqueo de cargas repetidas y progreso.
- `AudioTest`: duración, rango, bucles sin chasquido, sonidos cortos y volumen por canal.
- `MenuStateTest`: reglas de Continuar, confirmación, atajos y porcentajes.
