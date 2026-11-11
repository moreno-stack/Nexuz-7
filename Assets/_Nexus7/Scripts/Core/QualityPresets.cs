using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Nexus7.Core
{
    public enum AaMode { None, Fxaa, Smaa }

    /// <summary>Qué cambia cada nivel de calidad.</summary>
    public struct QualityPreset
    {
        /// <summary>Nivel de calidad de Unity (Project Settings → Quality): "Mobile" o "PC".</summary>
        public string unityLevel;
        public bool postProcessing;
        public bool shadows;
        public AaMode antialiasing;
        public bool filmGrain;
    }

    /// <summary>
    /// Calidad gráfica Baja / Media / Alta sin modificar los assets de URP (en el editor, lo que se cambia en Play
    /// se quedaría guardado): solo cambia el nivel de calidad de Unity y los ajustes de cada cámara y del volumen.
    ///   Baja  = nivel "Mobile", sin posprocesado, sin sombras, sin antialiasing.
    ///   Media = nivel "PC", posprocesado sin grano, sombras, FXAA.
    ///   Alta  = nivel "PC", todo el posprocesado, sombras, SMAA alto.
    /// Se aplica al cambiarlo y cada vez que se carga una escena.
    /// </summary>
    public static class QualityPresets
    {
        public static QualityPreset For(QualityLevel level)
        {
            switch (level)
            {
                case QualityLevel.Baja:
                    return new QualityPreset { unityLevel = "Mobile", postProcessing = false, shadows = false, antialiasing = AaMode.None, filmGrain = false };
                case QualityLevel.Media:
                    return new QualityPreset { unityLevel = "PC", postProcessing = true, shadows = true, antialiasing = AaMode.Fxaa, filmGrain = false };
                default:
                    return new QualityPreset { unityLevel = "PC", postProcessing = true, shadows = true, antialiasing = AaMode.Smaa, filmGrain = true };
            }
        }

        /// <summary>Índice del nivel llamado 'wanted' (sin distinguir mayúsculas); si no existe, 'fallback'.</summary>
        public static int FindLevel(string[] names, string wanted, int fallback)
        {
            if (names == null) return fallback;
            for (int i = 0; i < names.Length; i++)
                if (string.Equals(names[i], wanted, System.StringComparison.OrdinalIgnoreCase)) return i;
            return fallback;
        }

        public static void Apply(QualityLevel level)
        {
            var p = For(level);
            int index = FindLevel(QualitySettings.names, p.unityLevel, QualitySettings.GetQualityLevel());
            if (index != QualitySettings.GetQualityLevel()) QualitySettings.SetQualityLevel(index, true);

            foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                var data = cam.GetUniversalAdditionalCameraData();
                if (data == null) continue;
                data.renderPostProcessing = p.postProcessing;
                data.renderShadows = p.shadows;
                data.antialiasing = p.antialiasing == AaMode.Smaa ? AntialiasingMode.SubpixelMorphologicalAntiAliasing
                                  : p.antialiasing == AaMode.Fxaa ? AntialiasingMode.FastApproximateAntialiasing
                                  : AntialiasingMode.None;
                data.antialiasingQuality = AntialiasingQuality.High;
            }
            foreach (var volume in Object.FindObjectsByType<Volume>(FindObjectsSortMode.None))
            {
                if (volume.sharedProfile == null) continue;
                // volume.profile es la copia en juego (no toca el asset)
                if (volume.profile.TryGet(out FilmGrain grain)) grain.active = p.filmGrain;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            GameSettings.ApplyAll();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => GameSettings.ApplyAll();
    }
}
