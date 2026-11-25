# Publicar el juego: Windows, Mac y Android (APK)

Cada vez que publicas una versión, GitHub compila el juego solo y crea una **Release** con tres descargas:

| Archivo | Para |
|---|---|
| `Nexus7-Windows.zip` | Windows 10/11 de 64 bits |
| `Nexus7-Mac.zip` | Mac con procesador Intel o Apple Silicon |
| `Nexus7-Android.apk` | Móviles Android 8.0 o superior (64 bits), con controles táctiles |

Lo hace el flujo `.github/workflows/release.yml` con [GameCI](https://game.ci). Compila con **Unity 6000.6.7f1**, porque GameCI no tiene imagen de la 6000.6.4f1 del proyecto; es una versión de parche compatible.

## 1. Configurarlo (una sola vez)

### Secretos de Unity (obligatorios)

GitHub necesita tu licencia de Unity para poder abrir el proyecto. La licencia Personal es gratuita y sirve.

1. Abre **Unity Hub** con tu cuenta. Si en *Preferences → Licenses* no tienes ninguna licencia, añade una **Personal**.
2. Busca el archivo de licencia:
   - Windows: `C:\ProgramData\Unity\Unity_lic.ulf`
   - Mac: `/Library/Application Support/Unity/Unity_lic.ulf`
3. Ábrelo con el Bloc de notas y copia **todo** su contenido.
4. En GitHub, ve al repositorio → **Settings → Secrets and variables → Actions → New repository secret** y crea estos tres secretos:

| Nombre | Valor |
|---|---|
| `UNITY_LICENSE` | Todo el contenido de `Unity_lic.ulf` |
| `UNITY_EMAIL` | El correo de tu cuenta de Unity |
| `UNITY_PASSWORD` | La contraseña de tu cuenta de Unity |

Notas sobre la cuenta:
- Si entras en Unity con Google, primero ponle una contraseña a tu cuenta en id.unity.com.
- Si tienes la verificación en dos pasos por aplicación, la activación en GitHub puede fallar.

### Firma del APK (opcional, recomendable)

Sin firma propia, el APK se firma con una clave de depuración distinta en cada compilación. Se instala igual, pero **para actualizar hay que desinstalar la versión anterior**. Con una clave propia, cada APK nuevo se instala encima del anterior.

1. Crea la clave una sola vez (`keytool` viene con Java o con Android Studio). Guarda el archivo y las contraseñas en un sitio seguro, **no en el repositorio**:
   ```bash
   keytool -genkeypair -v -keystore nexus7.keystore -alias nexus7 -keyalg RSA -keysize 2048 -validity 10000
   ```
2. Conviértela a texto:
   - Windows (PowerShell): `[Convert]::ToBase64String([IO.File]::ReadAllBytes("nexus7.keystore")) | Set-Clipboard`
   - Mac/Linux: `base64 -i nexus7.keystore | pbcopy`
3. Crea estos cuatro secretos:

| Nombre | Valor |
|---|---|
| `ANDROID_KEYSTORE_BASE64` | El texto del paso 2 |
| `ANDROID_KEYSTORE_PASS` | La contraseña de la clave |
| `ANDROID_KEYALIAS_NAME` | `nexus7` (el alias del paso 1) |
| `ANDROID_KEYALIAS_PASS` | La contraseña del alias |

## 2. Publicar una versión

1. Sube primero tus cambios: `git push origin main`.
2. Crea y sube la etiqueta de la versión:
   ```bash
   git tag v1.0.0
   git push origin v1.0.0
   ```
   También puedes lanzarlo sin etiqueta: **Actions → Release → Run workflow**, escribes la versión (por ejemplo `1.0.0`) y pulsas el botón.
3. En la pestaña **Actions** verás tres compilaciones en paralelo (Windows, Mac y Android).
   - La primera vez tardan entre 40 y 90 minutos, porque Unity importa todo el proyecto.
   - Las siguientes van más rápido, gracias a la caché de `Library/`.
4. Cuando terminan las tres, la release aparece en **Releases** con las descargas y las instrucciones de instalación.

Cada compilación ejecuta **Nexus-7 → Construir todo el juego** antes de compilar (las escenas no están en git). Si una fase falla al construirse, la compilación se para y la release no se crea.

Minutos de GitHub Actions: en repositorios **públicos** son gratis. En privados se descuentan de los minutos gratuitos del mes; solo se usan ejecutores Linux, los más baratos.

## 3. Instalar

- **Windows:** descomprime el zip y abre `Nexus7.exe`. Si SmartScreen avisa, pulsa «Más información» → «Ejecutar de todas formas».
- **Mac:** descomprime el zip y arrastra `Nexus7.app` a Aplicaciones.
  - La primera vez abre la app con clic derecho → **Abrir** → Abrir, porque no está firmada por Apple (eso requiere una cuenta de desarrollador de pago).
  - Si dice que «está dañada», ejecuta en la Terminal: `xattr -cr /Applications/Nexus7.app`.
- **Android:** descarga el APK en el móvil, ábrelo y permite «instalar apps desconocidas» para el navegador o el gestor de archivos.

## 4. Controles en el móvil

| Zona | Qué hace |
|---|---|
| Mitad izquierda | Joystick: aparece donde pones el dedo. A fondo, corres |
| Mitad derecha | Arrastrar para mirar |
| **FUEGO** | Disparar (mientras lo mantienes, arrastrar también apunta). **MIRA** hace zoom |
| SALTO · AGACH. · USAR · R | Saltar, agacharse, interactuar y recargar |
| Arriba a la derecha | ARMA, DISP. (dispositivo), INV. (inventario) y CÁM. (1.ª/3.ª persona) |
| ◄ ► junto a la barra rápida | Cambiar de objeto |

Los botones cambian solos según lo que estés haciendo:
- **Hackeo:** las 4 direcciones, ¡INTERF.! y SALIR.
- **Consola y banco de trabajo:** ▲, ▼, ACEPTAR y VOLVER.
- **Pantalla final:** REINTENTAR y MENÚ.
- **Cinemáticas:** SALTAR.

En el menú principal se tocan directamente sus botones.

En el móvil la calidad gráfica empieza en **Media**, para ir a 60 fps. Se puede cambiar en Configuración.

## 5. Compilar en tu PC (opcional)

**Nexus-7 → Compilar versión → Windows / Mac / Android** deja el juego en la carpeta `Builds/` del proyecto. Para Mac y Android tienes que añadir antes a tu Unity, en Unity Hub, los módulos **Mac Build Support (Mono)** y **Android Build Support** (con OpenJDK y Android SDK & NDK).

## Problemas frecuentes

| Mensaje en Actions | Solución |
|---|---|
| «Faltan los secretos: …» | Crea los secretos del paso 1 con esos nombres exactos |
| Error de licencia o activación | Vuelve a copiar `Unity_lic.ulf` entero (desde Unity Hub, con la licencia activa) y revisa el correo y la contraseña |
| No encuentra la imagen `unityci/editor` | Cambia `unityVersion` en `release.yml` por una versión 6000.6 que exista en GameCI |
| No space left on device | Ya se libera disco al empezar; vuelve a lanzar el flujo |
| «No se pudieron construir: Fase …» | Abre el proyecto en tu Unity, ejecuta Construir todo el juego y mira el error en la consola |
