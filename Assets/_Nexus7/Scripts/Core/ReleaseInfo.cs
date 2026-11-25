using System.Text.RegularExpressions;

namespace Nexus7.Core
{
    public enum ReleasePlatform { Windows, Mac, Android }

    /// <summary>Versión y nombres de archivo de la release (lógica pura: la usan ReleaseBuild y las pruebas).</summary>
    public static class ReleaseInfo
    {
        public const string AndroidPackage = "com.morenostack.nexus7";
        public const string DevVersion = "0.0.0-dev";
        private static readonly Regex Semver = new Regex(@"^(\d+)\.(\d+)\.(\d+)([-+][0-9A-Za-z.\-]+)?$");

        /// <summary>«v1.2.3», «refs/tags/v1.2.3» o «1.2.3-beta» → versión; cualquier otra cosa → 0.0.0-dev.</summary>
        public static string VersionFromTag(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return DevVersion;
            string t = tag.Trim();
            if (t.StartsWith("refs/tags/")) t = t.Substring("refs/tags/".Length);
            if (t.StartsWith("v") || t.StartsWith("V")) t = t.Substring(1);
            return Semver.IsMatch(t) ? t : DevVersion;
        }

        /// <summary>Código de versión de Android (debe crecer): mayor·10000 + menor·100 + parche, mínimo 1.</summary>
        public static int AndroidVersionCode(string version)
        {
            var m = Semver.Match(version ?? "");
            if (!m.Success) return 1;
            int code = int.Parse(m.Groups[1].Value) * 10000 + int.Parse(m.Groups[2].Value) * 100 + int.Parse(m.Groups[3].Value);
            return code < 1 ? 1 : code;
        }

        public static string ArtifactName(ReleasePlatform p)
            => p == ReleasePlatform.Windows ? "Nexus7-Windows.zip" : p == ReleasePlatform.Mac ? "Nexus7-Mac.zip" : "Nexus7-Android.apk";

        public static string PlayerFileName(ReleasePlatform p)
            => p == ReleasePlatform.Windows ? "Nexus7.exe" : p == ReleasePlatform.Mac ? "Nexus7.app" : "Nexus7-Android.apk";

        /// <summary>Nombre del objetivo de Unity (BuildTarget) → plataforma de la release, o nulo si no la publicamos.</summary>
        public static ReleasePlatform? PlatformFromTarget(string target)
        {
            switch (target)
            {
                case "StandaloneWindows64": return ReleasePlatform.Windows;
                case "StandaloneOSX":       return ReleasePlatform.Mac;
                case "Android":             return ReleasePlatform.Android;
                default:                    return null;
            }
        }
    }
}
