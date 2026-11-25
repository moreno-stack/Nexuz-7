# Diseño: controles táctiles (Android) y release descargable (Windows, Mac, APK)

**Fecha:** 2026-11-24
**Estado:** aprobado por Samuel en el chat («Controles táctiles», «Automático en GitHub», «Sí, hazlo así»)

## Objetivo

Que cualquiera pueda descargar el juego desde **Releases** del repositorio, en tres archivos (`Nexus7-Windows.zip`, `Nexus7-Mac.zip` y `Nexus7-Android.apk`), y que en el móvil se juegue **con los dedos**.

## Situación de partida

- 18 scripts leen el teclado, el ratón y el mando con `Input.*` (Input Manager antiguo; *Active Input Handling* = Both). No hay ninguna capa común ni lectura táctil.
- En Android, Unity convierte por defecto los toques en ratón (`Input.simulateMouseWithTouches`): cualquier toque dispararía (clic izquierdo) y movería la cámara («Mouse X/Y»).
- Las escenas no están en git: hay que generarlas con «Construir todo el juego» antes de compilar.
- GameCI tiene imágenes de Unity **6000.6.7f1**, pero no de la 6000.6.4f1.
- Ajustes de Android ya presentes: IL2CPP, ARM64 y SDK mínimo 26.

## Decisiones

| Tema | Decisión |
|---|---|
| Capa de entrada | `GameInput` (estático) sustituye a `Input` en los 18 scripts: `GetKey`, `GetKeyDown`, `GetKeyUp`, `GetAxis`, `GetAxisRaw`, `GetMouseButtonDown`. Devuelve lo de `Input` **o** lo que imite `TouchInput`. En PC no cambia nada. |
| Botones táctiles | **Imitan teclas** (`KeyCode`): Saltar → Space, Agacharse → C, Correr → LeftShift, Usar → E, Disparar → Mouse0, Apuntar → Mouse1, Recargar → R, Arma → Q, Dispositivo → G, Inventario → Tab, Cámara → V, Objeto anterior/siguiente → JoystickButton4/5. |
| Ejes táctiles | El joystick suma a «Horizontal» y «Vertical» (−1…1). Arrastrar el dedo suma a «Mouse X» y «Mouse Y»: píxeles × `LookScale` (0,08) × 1080 / alto de pantalla. |
| Joystick | Aparece donde se apoya el dedo en la **mitad izquierda**. Radio de 110 px (escalado a 1080 p), zona muerta del 12 %. Correr solo arrastrando más allá del borde (1,25 × radio): a fondo dentro del círculo se camina, porque correr hace ruido y levanta al agachado (cambio tras la revisión). |
| Mirar | Mitad derecha, fuera de los botones. Disparar y Apuntar también dejan mirar mientras se mantienen. |
| Varios dedos | `TouchRouter` (lógica pura): cada dedo se asigna al empezar al control que tocó (botón, joystick o mirar) y conserva esa asignación hasta levantarse. Una tecla está pulsada mientras la mantenga al menos un dedo. `Down`/`Up` cuentan solo en el fotograma del cambio. |
| Conjuntos de botones | Según el contexto (prioridad de arriba abajo): **Cinemática** (Saltar cinemática → Space mantenido) · **Pantalla final** (sin botones táctiles: se tocan sus botones REINTENTAR y MENÚ; cambio tras la revisión) · **Hackeo** (↑ W, ← A, ↓ S, → D, Interferencia → Space, Cancelar → Q) · **Consola o banco** (↑ W, ↓ S, Aceptar → E, Volver → Q) · **Inventario abierto** (Cerrar → Tab) · **Juego** (joystick, mirar y todos los botones). En el menú principal no hay controles táctiles: se usan los botones del menú. |
| Cuándo se ven | `TouchInput.Enabled = UnityEngine.Device.Application.isMobilePlatform` (lo imita el *Device Simulator* de Unity; la zona segura sale de `UnityEngine.Device.Screen.safeArea`). Con controles táctiles: `Input.simulateMouseWithTouches = false`, no se bloquea el cursor y la tecla Atrás de Android no desbloquea la cámara. |
| Dibujo | `TouchControlsUI` se crea sola al cargar cada escena (`RuntimeInitializeOnLoadMethod`). Usa su propio Canvas por encima del HUD y debajo de los fundidos, con imágenes sin raycast: los toques se leen de `Input.touches`, así que no hace falta EventSystem. Los botones son círculos semitransparentes con su letra o símbolo y se iluminan al pulsarlos. |
| Distribución | Referencia 1920×1080, anclada a las esquinas: Disparar (150 px) abajo a la derecha; alrededor Apuntar, Saltar, Agacharse, Recargar y Usar; arriba a la derecha Arma, Dispositivo, Inventario y Cámara; flechas de objeto junto a la barra rápida. |
| Android | Lo aplica `ReleaseBuild` antes de compilar: horizontal (izquierda y derecha) sin girar a vertical, pantalla siempre encendida, 60 fps, paquete `com.morenostack.nexus7`, IL2CPP + ARM64, calidad **Media** por defecto en móvil. |
| Compilar | `ReleaseBuild` (editor): menú **Nexus-7 → Compilar versión → Windows / Mac / Android** y método `ReleaseBuild.BuildFromCommandLine` para GameCI. Construye todas las escenas, ordena Build Settings, aplica los ajustes, pone la versión (`-buildVersion`) y llama a `BuildPipeline.BuildPlayer`. Si algo falla, sale con código 1. |
| Release | `.github/workflows/release.yml`. Se lanza con una etiqueta `v*` o a mano (*workflow_dispatch* con versión). Tres trabajos en paralelo (`StandaloneWindows64`, `StandaloneOSX`, `Android`) en `ubuntu-latest` con `game-ci/unity-builder@v4` y Unity 6000.6.7f1. Antes libera disco y usa caché de `Library/` por plataforma. Al final, un trabajo crea la release con `softprops/action-gh-release@v2` y sube los 3 archivos. |
| Secretos | Obligatorios: `UNITY_LICENSE` (contenido de `Unity_lic.ulf`), `UNITY_EMAIL` y `UNITY_PASSWORD`. Opcionales, para firmar el APK con la misma clave siempre: `ANDROID_KEYSTORE_BASE64`, `ANDROID_KEYSTORE_PASS`, `ANDROID_KEYALIAS_NAME` y `ANDROID_KEYALIAS_PASS`. Sin ellos, firma de depuración. |
| Mac | `.app` sin firmar, comprimida con `zip -ry` (conserva enlaces simbólicos y permisos). Las notas de la release explican cómo abrirla (clic derecho → Abrir o `xattr -cr`). |

## Componentes

| Archivo | Responsabilidad |
|---|---|
| `Controls/TouchInput.cs` | Estado de teclas y ejes imitados (lógica pura con fotograma inyectable) |
| `Controls/TouchRouter.cs` | Asignación de dedos a controles, joystick y mirar (lógica pura) |
| `Controls/TouchLayout.cs` | Controles de cada contexto y sus rectángulos (lógica pura) |
| `Controls/GameInput.cs` | Fachada `Input` + táctil |
| `UI/TouchControlsUI.cs` | Lee `Input.touches`, decide el contexto, alimenta el router y dibuja |
| `Editor/ReleaseBuild.cs` | Compilar desde el menú y desde GameCI |
| `.github/workflows/release.yml` | Compilación y release |
| `docs/guias/RELEASE.md` | Secretos, publicar una versión, instalar en cada sistema |
| Cambios | 18 scripts `Input.` → `GameInput.`; `PlayerLook` (cursor en móvil); `IsOpen` en `HackMinigameUI` y `EndingUI`; `GameSettings` (calidad Media por defecto en móvil) |

## Errores

- Un dedo que empieza en un botón oculto (cambio de contexto) se ignora hasta que se levanta.
- Si se cambia de contexto con una tecla mantenida, la tecla se suelta (`Up`) para que no se quede pegada.
- Más de 10 dedos: los sobrantes se ignoran.
- La compilación falla (código 1) si falla una fase al construir las escenas o si `BuildPlayer` no termina bien. La release no se crea si falta cualquiera de los tres archivos.
- Sin `UNITY_LICENSE`, el flujo se detiene en el primer paso con un mensaje que remite a la guía.

## Pruebas (TDD, fuera de Unity)

- `TouchInput`: `Down` solo en el fotograma de pulsar, `Up` al soltar, mantenida con dos dedos, ejes acotados a −1…1 y puestos a cero en cada fotograma.
- `TouchRouter`: el dedo se queda con su control aunque salga de él; joystick (zona muerta, radio, correr a más del 90 %, dirección); mirar (delta escalado con la resolución); Disparar mantenido + mirar; cambio de contexto que suelta teclas; dedos ignorados.
- `TouchLayout`: cada contexto tiene sus botones y teclas; ningún botón se sale de la pantalla ni se solapa con otro; prioridad de contextos.
- `GameInput`: combina las dos fuentes (con un `Input` simulado por los stubs).
- Código: ningún script de juego usa `Input.GetKey`, `GetAxis` ni `GetMouseButton` directamente (solo `GameInput`); el workflow tiene los 3 objetivos, la versión 6000.6.7f1, los secretos y la subida de los 3 archivos; `ReleaseBuild` sale con código 1 al fallar.
- Compilación con mcs y Roslyn, y todas las suites.

## Fuera de alcance

- Firma y notarización de Apple, y publicación en Google Play (necesitan cuentas de pago).
- Personalizar la posición de los botones, vibración y giroscopio.
- iOS.
