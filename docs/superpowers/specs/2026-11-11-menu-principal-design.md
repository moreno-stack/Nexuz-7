# Diseño: arranque por el Menú Principal, guardado, configuración y audio

**Fecha:** 2026-11-11
**Estado:** pendiente de revisión por Samuel

## Objetivo

Al pulsar Play en Unity (o al abrir el juego compilado) debe aparecer **siempre** un Menú Principal funcional con **Nueva partida**, **Continuar**, **Configuración** y **Salir**. Desde Configuración se ajustan el volumen general, la música, los efectos, la calidad gráfica y la pantalla completa.

## Lo que pidió Samuel

- "MenuPrincipal" en el índice 0 de Build Settings.
- Un `SceneLoader` para transiciones limpias entre el menú, la configuración y las fases.
- Botones: Nueva partida (primera fase), Continuar (partida guardada, desactivado si no hay), Configuración (submenú), Salir (cierra el juego o detiene Play en el editor).
- Sliders de Volumen General, Música y Efectos; selector de calidad Baja/Media/Alta; Pantalla completa.
- Scripts de UI limpios e instrucciones de jerarquía para montar y enlazar el menú.

Respuestas a las preguntas:
- **Audio:** el juego no tiene ningún sonido → **añadir música y sonidos generados por código** para que los tres sliders se oigan.
- **Continuar:** carga desde el **inicio de la fase** guardada.

## Causas del problema actual (comprobadas en `Logs/Editor-prev.log`)

1. "Construir todo el juego" se interrumpe en la Fase 2: `MissingReferenceException` en `AlmacenCargaBuilder.cs:235`. `ConnectPhase1Exit()` abre `SalaInicial` (modo Single) y luego `Selection.activeGameObject = player.gameObject` usa un jugador de la escena ya cerrada. El mismo patrón está en `SalaEnergiaBuilder` (`ConnectPhase2`), `LaboratorioBuilder` (`ConnectPhase3`) y `CentroControlBuilder` (`ConnectPhase4`). Resultado: solo existen `SalaInicial` y `AlmacenCarga`, **no existe `MenuPrincipal`**, y Build Settings solo tiene esas dos.
2. En el editor, **Play arranca la escena abierta**, no la de índice 0.

## Decisiones

| Tema | Decisión |
|---|---|
| Arreglo de constructores | Tras los `Connect…()`, seleccionar al jugador buscándolo de nuevo (`GameObject.FindGameObjectWithTag("Player")`) y comprobar nulos. |
| Construir todo | Cada fase en su propio `try/catch`: si una falla se registra el error y se sigue; al final, diálogo con las fases que fallaron. Después: catálogo de objetos, Build Settings ordenados y menú abierto. |
| Play en el editor | `EditorSceneManager.playModeStartScene = MenuPrincipal` al cargar el editor. Interruptor **Nexus-7 → Empezar Play por el menú** (guardado en `EditorPrefs`, activado por defecto) para poder probar una sala suelta. |
| Build Settings | `EvacuacionBuilder.ConfigureBuildSettings()` (ya existe: menú, Fases 1–6) se ejecuta también al final de "Construir todo" y desde **Nexus-7 → Ordenar Build Settings**. |
| SceneLoader | Singleton que se crea solo (`RuntimeInitializeOnLoadMethod`) y sobrevive a los cambios de escena. `SceneLoader.Load(nombre)`: fundido a negro (0,35 s, `unscaledTime`) → `LoadSceneAsync` con barra de progreso → fundido de entrada. Ignora peticiones mientras ya carga. Si la escena no está en Build Settings, avisa y no hace nada. Lo usan `GameFlow`, `SceneExitTrigger` y `ConsoleUI`. |
| Partida guardada | `SaveSystem` guarda un JSON (`JsonUtility`) en `Application.persistentDataPath/nexus7_partida.json`. Se guarda **automáticamente al empezar cada fase** (un fotograma después de cargarla, cuando el inventario ya está listo). Contenido: versión, escena, nombre de la fase, fecha (UTC), inventario (Id + cantidad), traje, semilla, objeto de la barra rápida, dificultad y estadísticas de `RunStats`. Se **borra** al terminar la partida (victoria o derrota final). |
| Continuar | Restaura `GameSession` (inventario, traje, semilla, barra rápida), la dificultad y `RunStats`, y carga la escena guardada con `SceneLoader`. El botón está desactivado (gris) si no hay partida; si la hay, muestra "Fase 3 · Sala de energía — hace 2 h". |
| Nueva partida con partida guardada | Panel de confirmación: "Se sobrescribirá la partida guardada. ¿Empezar de nuevo?" (Sí / No). |
| Objetos por Id | `ItemCatalog` (ScriptableObject) en `Assets/_Nexus7/Resources/Nexus7_CatalogoObjetos.asset` con todos los `ItemData`; lo genera el editor al construir. Un Id que ya no exista se ignora con un aviso. |
| Configuración | `GameSettings` (estático, PlayerPrefs): `Master`, `Music`, `Sfx` (0–1), `Quality` (Baja/Media/Alta), `Fullscreen`. Cada cambio se aplica al momento y avisa con un evento. Volumen real = valor² (curva perceptual). |
| Volúmenes | General → `AudioListener.volume`. Música y Efectos → componente `AudioChannel` (categoría + volumen base) en cada `AudioSource`. Al cargar cada escena, `AudioChannelManager` añade `AudioChannel` (Efectos) a los `AudioSource` que no lo tengan. |
| Calidad | Sin tocar los assets de URP (en el editor los cambios en Play se quedarían guardados): **Baja** = nivel de calidad "Mobile", sin posprocesado ni sombras en la cámara, sin antialiasing. **Media** = nivel "PC", FXAA, posprocesado sin grano. **Alta** = nivel "PC", SMAA alto, posprocesado completo. Se aplica a todas las cámaras al cargar cada escena y al cambiarlo. |
| Pantalla completa | `Screen.fullScreenMode` (ventana completa sin bordes / ventana). En el editor solo se guarda. |
| Música | `MusicPlayer` (sobrevive entre escenas) con dos pistas generadas por código (`ProceduralAudio`): **menú** (pad ambiental de 16 s en bucle) y **nave** (zumbido grave y tenso, más bajo). Cambia de pista con un fundido al pasar del menú a las fases. Canal Música. |
| Sonidos de la UI | Clips generados: pasar por encima, pulsar, volver. `UISounds` los engancha a todos los botones y sliders de un Canvas. Canal Efectos. Al mover el slider de Efectos suena un pitido de prueba. |
| Menú | Botones en columna: NUEVA PARTIDA [Intro] · CONTINUAR · CONFIGURACIÓN · SALIR. Se mantienen las tarjetas de dificultad y el récord. Panel de Configuración centrado con 3 sliders con su %, 3 botones de calidad (el elegido con marco dorado), un interruptor de pantalla completa y VOLVER [Esc]. Teclado/mando: el primer botón queda seleccionado para navegar con flechas o cruceta. |

## Componentes

| Archivo | Responsabilidad | Depende de |
|---|---|---|
| `Core/SceneLoader.cs` | Transiciones con fundido y carga asíncrona | `CanvasGroup` propio |
| `Core/SaveSystem.cs` | Guardar, cargar, borrar, ¿existe?, descripción del hueco | `SaveData`, `ItemCatalog`, `GameSession`, `RunStats`, `DifficultySettings` |
| `Core/SaveData.cs` | Datos serializables + lógica pura (descripción, "hace X") | — |
| `Inventory/ItemCatalog.cs` | Lista de `ItemData` y búsqueda por Id | `ItemData` |
| `Core/GameSettings.cs` | Ajustes, PlayerPrefs, curva de volumen, presets de calidad | — |
| `Core/QualityPresets.cs` | Qué cambia cada nivel (lógica pura) y su aplicación a cámaras | URP |
| `Audio/AudioChannel.cs` + `AudioChannelManager.cs` | Volumen por canal | `GameSettings` |
| `Audio/ProceduralAudio.cs` | Síntesis pura de muestras (música y UI) | — |
| `Audio/MusicPlayer.cs`, `Audio/UISounds.cs` | Reproducir música y sonidos de UI | `ProceduralAudio`, `AudioChannel` |
| `UI/MainMenuUI.cs` | Menú: botones, Continuar, confirmación | `GameFlow`, `SaveSystem` |
| `UI/SettingsMenuUI.cs` | Panel de configuración | `GameSettings` |
| `Editor/MenuScreenBuilder.cs` | Construye la UI nueva del menú | — |
| `Editor/BuildAllScenes.cs` | Construir todo robusto, catálogo, Build Settings, Play por el menú | constructores |
| Cambios | `GameFlow` (Continuar, borrar al terminar, `SceneLoader`), `SceneExitTrigger`, `ConsoleUI`, `GameSession` (restaurar), `RunStats` (instantánea), `EndingManager` (borrar partida), 4 constructores | — |

## Flujo

```
Play ──► MenuPrincipal (playModeStartScene / índice 0)
          ├─ Nueva partida ─(¿hay guardado? confirmar)─► GameFlow.StartNewRun ─► SceneLoader.Load("SalaInicial")
          ├─ Continuar ─► SaveSystem.Load ─► restaura sesión ─► SceneLoader.Load(escena guardada)
          ├─ Configuración ─► panel (sliders, calidad, pantalla completa) ─► Volver
          └─ Salir ─► GameFlow.Quit (detiene Play / Application.Quit)
Cada fase al cargarse ─► SaveSystem.AutoSave (1 fotograma después)
Salida de fase ─► SceneExitTrigger ─► SceneLoader.Load(siguiente)
Fin de partida ─► SaveSystem.Delete ─► resultados ─► menú / reiniciar
```

## Errores

- Archivo de guardado corrupto o de otra versión → se ignora (Continuar desactivado) y se registra un aviso; no se borra por si se quiere recuperar.
- Escena guardada que ya no está en Build Settings → Continuar desactivado con "Partida no disponible".
- `ItemCatalog` ausente → el inventario guardado no se puede restaurar: se avisa y se continúa con inventario vacío.
- Sin `AudioListener` en una escena → el volumen general se aplica igualmente (es global).

## Pruebas (TDD, fuera de Unity)

- `SaveData`: descripción del hueco ("Fase 3 · Sala de energía — hace 2 h"), formato de tiempo relativo (minutos, horas, días), instantánea ↔ inventario (Id + cantidad; Ids desconocidos ignorados), versión incompatible rechazada.
- `GameSettings`: valores por defecto, límites 0–1, curva de volumen, nombres de calidad, siguiente/anterior.
- `QualityPresets`: qué activa cada nivel (nivel de Unity, posprocesado, sombras, antialiasing, grano).
- `ProceduralAudio`: longitud, rango [−1, 1], bucle sin chasquido (inicio ≈ final), silencio al principio/final de los sonidos cortos.
- `SceneLoader`: lógica pura del fundido (alfa por tiempo) y bloqueo de cargas repetidas.
- `MenuState`: Continuar habilitado solo con partida válida; confirmación solo si hay partida.
- Compilación con mcs + Roslyn (como Unity 6.6) y todas las suites existentes.

## Fuera de alcance

- Menú de pausa con Configuración dentro de las fases (se puede añadir luego reutilizando `SettingsMenuUI`).
- Varios huecos de guardado.
- Guardar el estado de puzles, robots o posición dentro de una fase.
- Sonidos de juego (pasos, puertas, robots): el canal Efectos queda listo para ellos.
