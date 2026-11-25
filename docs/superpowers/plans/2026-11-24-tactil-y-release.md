# Controles táctiles y release descargable — Plan de implementación

> **Para agentes:** SUB-SKILL: superpowers:executing-plans (ejecución nativa elegida por Samuel). Pasos con casillas `- [ ]`.

**Objetivo:** jugar con los dedos en Android y publicar Windows, Mac y APK en Releases automáticamente.

**Arquitectura:** lógica pura de toques (`TouchInput`, `TouchRouter`, `TouchLayout`) probada fuera de Unity; `GameInput` combina `Input` y lo táctil; `TouchControlsUI` lee los toques y los dibuja; `ReleaseBuild` y un workflow de GameCI compilan y publican.

**Tecnología:** Unity 6000.6 (6000.6.7f1 en CI), C#, GitHub Actions, GameCI v4.

**Spec:** `docs/superpowers/specs/2026-11-24-tactil-y-release-design.md`

## Restricciones globales

- En PC el comportamiento no cambia: cada tecla, eje y botón del mando hace lo mismo.
- La lógica pura no usa tipos de Unity salvo `KeyCode` (enum).
- Textos en español; commits pequeños como `moreno-stack`, sin atribuciones.
- Unity de CI: `6000.6.7f1`; secretos con los nombres exactos del spec.

## Revisión (lo que las pruebas no cubren)

1. Toques convertidos en ratón (`simulateMouseWithTouches`): un toque no debe disparar ni girar la cámara dos veces.
2. Teclas pegadas al cambiar de contexto o al perder el foco de la app (`OnApplicationPause`).
3. Botones que tapan el HUD (barra rápida, munición, estamina) o la zona segura (notch).
4. La compilación en CI sin diálogos (modo batch) y con las escenas generadas.
5. El tamaño del disco de los ejecutores de GitHub (imágenes de Unity de ~15 GB).

---

### Tarea 1: `TouchInput` (teclas y ejes imitados)

**Archivos:** crear `Core/Input/TouchInput.cs`; prueba `Tests/Suites/TouchTest.cs`.

**Produce:** `TouchInput.Enabled`, `BeginFrame(int frame)`, `Press(KeyCode, int source)`, `Release(KeyCode, int source)`, `ReleaseAll()`, `GetKey/GetKeyDown/GetKeyUp(KeyCode)`, `AddAxis(string axis, float v)`, `Axis(string) → float` (acotado −1…1 para Horizontal/Vertical; sin límite para Mouse X/Y).

- [ ] Prueba: Down solo en el fotograma de pulsar; Up al soltar; dos fuentes en la misma tecla; ejes que se ponen a cero cada fotograma; ReleaseAll da Up. Ver que falla. Implementar. Pasa. Commit.

### Tarea 2: `TouchLayout` y `TouchRouter`

**Archivos:** crear `Core/Input/TouchLayout.cs`, `Core/Input/TouchRouter.cs`; prueba en `TouchTest.cs`.

**Produce:** `enum TouchContext { None, Gameplay, Inventory, Menu, Hack, Ending, Cinematic }`, `TouchButton { Id, Label, Key, Center(x,y en px de referencia), Radius, LooksWhileHeld, Hold }`, `TouchLayout.For(TouchContext, float w, float h) → TouchButton[]`, `TouchLayout.HasStick/HasLook(ctx)`; `TouchRouter.Update(ctx, touches[], screenW, screenH)` con `RawTouch { id, x, y, phase }`, `StickVector`, `StickOrigin`, `StickActive`, `PressedButtons`.

- [ ] Pruebas: el dedo se queda con su control; joystick (zona muerta 12 %, radio 110, correr > 90 %); mirar escalado (×1080/alto); Disparar mantenido + mirar; cambio de contexto suelta teclas; más de 10 dedos; botones dentro de pantalla y sin solaparse en 16:9, 20:9 y 4:3; prioridad de contextos (`TouchLayout.Pick(cinematic, ending, hack, menu, inventory, hasPlayer)`).
- [ ] Implementar; pasa; commit.

### Tarea 3: `GameInput` y migración de los 18 scripts

**Archivos:** crear `Core/Input/GameInput.cs`; modificar los 18 scripts (`Input.X(` → `GameInput.X(`); `PlayerLook` (sin cursor en móvil); prueba en `TouchTest.cs` (combinación) y comprobación de código.

- [ ] Prueba: `GameInput.GetKeyDown(Space)` es verdad si lo pulsa lo táctil; `GetAxisRaw("Horizontal")` suma el joystick y se acota; ningún script fuera de `Core/Input` y `Editor` llama a `Input.GetKey`, `GetAxis` o `GetMouseButton`. Ver que falla.
- [ ] Implementar y migrar (un commit por grupo: jugador, combate y objetos, interfaces); pasa la suite completa.

### Tarea 4: `TouchControlsUI` y contextos

**Archivos:** crear `UI/TouchControlsUI.cs`; añadir `IsOpen` a `HackMinigameUI` y `EndingUI`; `GameSettings` (Media por defecto en móvil).

- [ ] Prueba (código y lógica): crea su Canvas por encima del HUD; desactiva `simulateMouseWithTouches`; suelta todo en `OnApplicationPause`; elige el contexto con `TouchLayout.Pick`; `GameSettings.DefaultQualityFor(mobile: true) == Media`. Ver que falla. Implementar; pasa; commit.

### Tarea 5: `ReleaseBuild` (editor)

**Archivos:** crear `Editor/ReleaseBuild.cs`; lógica pura `Editor`-libre en `Core/ReleaseInfo.cs` (nombres de archivo y versión a partir de la etiqueta).

- [ ] Prueba: `ReleaseInfo.VersionFromTag("v1.2.3") == "1.2.3"`, etiquetas raras → "0.0.0-dev"; nombres `Nexus7-Windows.zip`, `Nexus7-Mac.zip`, `Nexus7-Android.apk`; código: `BuildFromCommandLine` llama a construir todo, aplica ajustes de Android (paquete, horizontal, ARM64, IL2CPP) y `EditorApplication.Exit(1)` al fallar. Ver que falla. Implementar; pasa; commit.

### Tarea 6: Workflow y guía

**Archivos:** `.github/workflows/release.yml`, `docs/guias/RELEASE.md`, enlace en la guía principal.

- [ ] Prueba (código): el workflow se lanza con `v*` y a mano, compila los 3 objetivos con 6000.6.7f1 y `buildMethod: Nexus7.EditorTools.ReleaseBuild.BuildFromCommandLine`, usa los 3 secretos, libera disco, guarda `Library/` en caché y sube los 3 archivos con `softprops/action-gh-release`. Ver que falla. Escribir; pasa; commit.

### Tarea 7: Verificación y entrega

- [ ] `bash Tests/run_all.sh` verde, revisión independiente, arreglos, bundle a la carpeta de Samuel.
