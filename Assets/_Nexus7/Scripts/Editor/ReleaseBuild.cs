using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Nexus7.Core;

namespace Nexus7.EditorTools
{
    /// <summary>
    /// Compila el juego para Windows, Mac o Android:
    /// - desde el menú Nexus-7 → Compilar versión (deja el resultado en la carpeta Builds/ del proyecto);
    /// - desde GameCI (GitHub Actions), con -executeMethod Nexus7.EditorTools.ReleaseBuild.BuildFromCommandLine.
    /// Antes construye todas las escenas (no están en git) y aplica los ajustes de cada sistema.
    /// </summary>
    public static class ReleaseBuild
    {
        [MenuItem("Nexus-7/Compilar versión/Windows", false, 60)] private static void MenuWindows() => BuildInteractive(BuildTarget.StandaloneWindows64);
        [MenuItem("Nexus-7/Compilar versión/Mac", false, 61)]     private static void MenuMac() => BuildInteractive(BuildTarget.StandaloneOSX);
        [MenuItem("Nexus-7/Compilar versión/Android", false, 62)] private static void MenuAndroid() => BuildInteractive(BuildTarget.Android);

        private static void BuildInteractive(BuildTarget target)
        {
            var platform = ReleaseInfo.PlatformFromTarget(target.ToString()).Value;
            string path = Path.Combine(Directory.GetCurrentDirectory(), "Builds", platform.ToString(), ReleaseInfo.PlayerFileName(platform));
            string version = string.IsNullOrEmpty(PlayerSettings.bundleVersion) ? ReleaseInfo.DevVersion : PlayerSettings.bundleVersion;
            bool ok = Build(target, path, version, null);
            string msg = ok ? "Compilación terminada:\n" + path : "La compilación ha fallado. Mira la consola.";
            Debug.Log("[Nexus-7] " + msg);
            if (EditorUtility.DisplayDialog("Escape Espacial: Nexus-7", msg, "Abrir carpeta", "Cerrar") && ok)
                EditorUtility.RevealInFinder(path);
        }

        /// <summary>Punto de entrada para GameCI. Lee los argumentos que pasa unity-builder y sale con 0 (bien) o 1 (mal).</summary>
        public static void BuildFromCommandLine()
        {
            try
            {
                string targetName = Arg("-customBuildTarget") ?? Arg("-buildTarget") ?? EditorUserBuildSettings.activeBuildTarget.ToString();
                if (!Enum.TryParse(targetName, out BuildTarget target) || ReleaseInfo.PlatformFromTarget(target.ToString()) == null)
                {
                    Debug.LogError("[Nexus-7] Objetivo de compilación no soportado: " + targetName);
                    EditorApplication.Exit(1);
                    return;
                }
                var platform = ReleaseInfo.PlatformFromTarget(target.ToString()).Value;
                string path = Arg("-customBuildPath") ?? Path.Combine("build", target.ToString(), ReleaseInfo.PlayerFileName(platform));
                string version = ReleaseInfo.VersionFromTag(Arg("-buildVersion"));
                var keystore = new Keystore
                {
                    Name = Arg("-androidKeystoreName"), Pass = Arg("-androidKeystorePass"),
                    Alias = Arg("-androidKeyaliasName"), AliasPass = Arg("-androidKeyaliasPass"),
                };
                bool ok = Build(target, path, version, keystore);
                EditorApplication.Exit(ok ? 0 : 1);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        private class Keystore { public string Name, Pass, Alias, AliasPass; }

        private static bool Build(BuildTarget target, string path, string version, Keystore keystore)
        {
            Debug.Log($"[Nexus-7] Compilando {target} versión {version} en {path}");
            var failed = BuildAllScenes.BuildAll();
            if (failed.Count > 0)
            {
                Debug.LogError("[Nexus-7] No se pudieron construir: " + string.Join(", ", failed));
                return false;
            }
            Apply(target, version, keystore);

            string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled && File.Exists(s.path)).Select(s => s.path).ToArray();
            if (scenes.Length == 0) { Debug.LogError("[Nexus-7] No hay escenas en Build Settings."); return false; }

            string dir = Path.GetDirectoryName(Path.GetFullPath(path));
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = path,
                target = target,
                targetGroup = target == BuildTarget.Android ? BuildTargetGroup.Android : BuildTargetGroup.Standalone,
                options = BuildOptions.None,
            };
            UnityEditor.Build.Reporting.BuildReport report = BuildPipeline.BuildPlayer(options);
            bool ok = report != null && report.summary.result == BuildResult.Succeeded;
            if (ok) Debug.Log("[Nexus-7] Compilación correcta: " + report.summary.outputPath);
            else Debug.LogError("[Nexus-7] La compilación ha fallado" + (report != null ? $" ({report.summary.result}, {report.summary.totalErrors} errores)." : "."));
            return ok;
        }

        /// <summary>Versión, nombre y ajustes de cada sistema (Android: paquete, IL2CPP + ARM64, solo horizontal, APK).</summary>
        private static void Apply(BuildTarget target, string version, Keystore keystore)
        {
            PlayerSettings.bundleVersion = version;
            PlayerSettings.companyName = "moreno-stack";
            PlayerSettings.productName = "Escape Espacial Nexus-7";

            // Pantalla en horizontal (izquierda o derecha), nunca en vertical
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            if (target != BuildTarget.Android) return;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, ReleaseInfo.AndroidPackage);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;   // móviles de 64 bits (los actuales)
            PlayerSettings.Android.bundleVersionCode = ReleaseInfo.AndroidVersionCode(version);
            EditorUserBuildSettings.buildAppBundle = false;                              // APK para instalar directamente

            // Firma: con la clave de los secretos (GameCI la deja en la raíz del proyecto); sin ella, clave de depuración
            bool custom = keystore != null && !string.IsNullOrEmpty(keystore.Name) && File.Exists(keystore.Name);
            PlayerSettings.Android.useCustomKeystore = custom;
            if (custom)
            {
                PlayerSettings.Android.keystoreName = keystore.Name;
                PlayerSettings.Android.keystorePass = keystore.Pass;
                PlayerSettings.Android.keyaliasName = keystore.Alias;
                PlayerSettings.Android.keyaliasPass = keystore.AliasPass;
            }
            Debug.Log("[Nexus-7] Firma del APK: " + (custom ? "clave propia" : "clave de depuración"));
        }

        /// <summary>Valor del argumento '-nombre valor' de la línea de órdenes, o nulo.</summary>
        private static string Arg(string name)
        {
            string[] args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == name && !string.IsNullOrEmpty(args[i + 1]) && !args[i + 1].StartsWith("-")) return args[i + 1];
            return null;
        }
    }
}
