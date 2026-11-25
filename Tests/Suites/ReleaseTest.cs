using System; using System.IO;
using Nexus7.Core;
static class T {
  static int fails=0; static void Check(bool c,string m){ Console.WriteLine((c?"OK   ":"FAIL ")+m); if(!c) fails++; }
  static void Main(){
    // ---------- Tarea 5: versión y nombres ----------
    Check(ReleaseInfo.VersionFromTag("v1.2.3") == "1.2.3", "Etiqueta v1.2.3 → versión 1.2.3");
    Check(ReleaseInfo.VersionFromTag("refs/tags/v2.0.0") == "2.0.0", "Admite la referencia completa de GitHub");
    Check(ReleaseInfo.VersionFromTag("1.0.0-beta.2") == "1.0.0-beta.2", "Admite versiones previas sin la v");
    Check(ReleaseInfo.VersionFromTag("main") == ReleaseInfo.DevVersion && ReleaseInfo.VersionFromTag(null) == ReleaseInfo.DevVersion && ReleaseInfo.VersionFromTag("v1.2") == ReleaseInfo.DevVersion, "Etiquetas que no son versión → " + ReleaseInfo.DevVersion);
    Check(ReleaseInfo.AndroidVersionCode("1.2.3") == 10203 && ReleaseInfo.AndroidVersionCode("0.0.0-dev") == 1 && ReleaseInfo.AndroidVersionCode("2.10.0") == 21000, "Código de versión de Android creciente (1.2.3 → 10203; mínimo 1)");
    Check(ReleaseInfo.ArtifactName(ReleasePlatform.Windows) == "Nexus7-Windows.zip" && ReleaseInfo.ArtifactName(ReleasePlatform.Mac) == "Nexus7-Mac.zip" && ReleaseInfo.ArtifactName(ReleasePlatform.Android) == "Nexus7-Android.apk", "Nombres de las 3 descargas");
    Check(ReleaseInfo.PlayerFileName(ReleasePlatform.Windows) == "Nexus7.exe" && ReleaseInfo.PlayerFileName(ReleasePlatform.Mac) == "Nexus7.app" && ReleaseInfo.PlayerFileName(ReleasePlatform.Android) == "Nexus7-Android.apk", "Nombre del ejecutable en cada sistema");
    Check(ReleaseInfo.AndroidPackage == "com.morenostack.nexus7", "Paquete de Android");
    Check(ReleaseInfo.PlatformFromTarget("StandaloneWindows64") == ReleasePlatform.Windows && ReleaseInfo.PlatformFromTarget("StandaloneOSX") == ReleasePlatform.Mac && ReleaseInfo.PlatformFromTarget("Android") == ReleasePlatform.Android && ReleaseInfo.PlatformFromTarget("PS5") == null, "Objetivo de Unity → plataforma (desconocido → nulo)");

    Check(ReleaseInfo.RequiredScenes.Length == 7 && ReleaseInfo.RequiredScenes[0].EndsWith("MenuPrincipal.unity"), "La release necesita el menú y las 6 fases");
    var some = new System.Collections.Generic.List<string>(ReleaseInfo.RequiredScenes); some.RemoveAt(3);
    Check(ReleaseInfo.MissingScenes(some).Length == 1 && ReleaseInfo.MissingScenes(ReleaseInfo.RequiredScenes).Length == 0, "Detecta la escena que falta (una fase sin construir)");
    string rb = File.ReadAllText("Assets/_Nexus7/Scripts/Editor/ReleaseBuild.cs");
    Check(rb.Contains("public static void BuildFromCommandLine()") && rb.Contains("EditorApplication.Exit(ok ? 0 : 1)") && rb.Contains("EditorApplication.Exit(1)"), "Desde GameCI: sale con código 0 si compila y 1 si falla");
    Check(rb.Contains("BuildAllScenes.BuildAll()") && rb.Contains("BuildPipeline.BuildPlayer(") && rb.Contains("ReleaseInfo.MissingScenes("), "Construye todas las escenas y no compila si falta alguna");
    Check(rb.Contains("ScriptingImplementation.IL2CPP") && rb.Contains("AndroidArchitecture.ARM64") && rb.Contains("ReleaseInfo.AndroidPackage") && rb.Contains("allowedAutorotateToPortrait = false") && rb.Contains("buildAppBundle = false"), "Android: IL2CPP, ARM64, paquete, solo horizontal y APK (no AAB)");
    Check(rb.Contains("-androidKeystoreName") && rb.Contains("useCustomKeystore"), "Firma del APK con la clave de los secretos si GameCI la pasa");
    Check(rb.Contains("\"Nexus-7/Compilar versión/Windows\"") && rb.Contains("\"Nexus-7/Compilar versión/Mac\"") && rb.Contains("\"Nexus-7/Compilar versión/Android\""), "Menú para compilar cada sistema desde Unity");
    string bas = File.ReadAllText("Assets/_Nexus7/Scripts/Editor/BuildAllScenes.cs");
    Check(bas.Contains("public static List<string> BuildAll()"), "Construir todo sin diálogos devuelve las fases que fallaron");
    string tc = File.ReadAllText("Assets/_Nexus7/Scripts/UI/TouchControlsUI.cs");
    Check(tc.Contains("Screen.sleepTimeout = SleepTimeout.NeverSleep") && tc.Contains("Application.targetFrameRate = 60"), "En el móvil: la pantalla no se apaga y va a 60 fps");
    string gi = File.ReadAllText(".gitignore");
    Check(gi.Contains("[Bb]uilds/") && gi.Contains("[Bb]uild/"), "Las compilaciones no se suben a git");

    // ---------- Tarea 6: workflow ----------
    string wf = File.Exists(".github/workflows/release.yml") ? File.ReadAllText(".github/workflows/release.yml") : "";
    Check(wf.Contains("tags:") && wf.Contains("'v*'") && wf.Contains("workflow_dispatch:"), "Se lanza con una etiqueta v* o a mano");
    Check(wf.Contains("StandaloneWindows64") && wf.Contains("StandaloneOSX") && wf.Contains("Android"), "Compila Windows, Mac y Android");
    Check(wf.Contains("game-ci/unity-builder@v4") && wf.Contains("unityVersion: 6000.6.7f1") && wf.Contains("buildMethod: Nexus7.EditorTools.ReleaseBuild.BuildFromCommandLine"), "GameCI con Unity 6000.6.7f1 y nuestro método de compilación");
    Check(wf.Contains("secrets.UNITY_LICENSE") && wf.Contains("secrets.UNITY_EMAIL") && wf.Contains("secrets.UNITY_PASSWORD"), "Usa los 3 secretos de Unity");
    Check(wf.Contains("secrets.ANDROID_KEYSTORE_BASE64") && wf.Contains("secrets.ANDROID_KEYALIAS_PASS"), "Firma opcional del APK con secretos");
    Check(wf.Contains("free-disk-space") && wf.Contains("actions/cache@v4") && wf.Contains("Library-"), "Libera disco y guarda Library en caché");
    Check(wf.Contains("softprops/action-gh-release@v2") && wf.Contains("Nexus7-Windows.zip") && wf.Contains("Nexus7-Mac.zip") && wf.Contains("Nexus7-Android.apk") && wf.Contains("fail_on_unmatched_files: true"), "Crea la release con las 3 descargas (y falla si falta alguna)");
    Check(wf.Contains("contents: write"), "Tiene permiso para crear la release");
    Check(File.Exists("docs/guias/RELEASE.md") && File.ReadAllText("docs/guias/RELEASE.md").Contains("Unity_lic.ulf"), "Guía de la release con los secretos");
    Console.WriteLine(fails==0?"\nTODAS LAS PRUEBAS PASAN":"\nFALLOS: "+fails);
  }
}
