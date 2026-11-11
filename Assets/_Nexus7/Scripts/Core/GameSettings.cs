using System;
using UnityEngine;

namespace Nexus7.Core
{
    public enum QualityLevel { Baja, Media, Alta }

    /// <summary>
    /// Configuración del jugador (menú Configuración), guardada en PlayerPrefs y aplicada en todas las escenas:
    ///   · Volumen general → AudioListener.volume · Música y Efectos → AudioChannel en cada AudioSource.
    ///   · Calidad Baja / Media / Alta (QualityPresets) · Pantalla completa.
    /// Los volúmenes van de 0 a 1; el volumen real sigue una curva (valor²), que el oído percibe como lineal.
    /// </summary>
    public static class GameSettings
    {
        public const string MasterKey = "Nexus7_VolGeneral", MusicKey = "Nexus7_VolMusica", SfxKey = "Nexus7_VolEfectos";
        public const string QualityKey = "Nexus7_Calidad", FullscreenKey = "Nexus7_PantallaCompleta";
        public const float DefaultMaster = 0.8f, DefaultMusic = 0.7f, DefaultSfx = 0.8f;
        public const QualityLevel DefaultQuality = QualityLevel.Alta;
        public const bool DefaultFullscreen = true;

        /// <summary>Cualquier ajuste ha cambiado (los componentes vuelven a aplicar sus volúmenes).</summary>
        public static event Action Changed;

        private static bool loaded;
        private static float master, music, sfx;
        private static QualityLevel quality;
        private static bool fullscreen;

        public static float Master { get { Load(); return master; } set { Load(); master = Clamp01(value); PlayerPrefs.SetFloat(MasterKey, master); Save(); } }
        public static float Music  { get { Load(); return music; }  set { Load(); music = Clamp01(value);  PlayerPrefs.SetFloat(MusicKey, music);   Save(); } }
        public static float Sfx    { get { Load(); return sfx; }    set { Load(); sfx = Clamp01(value);    PlayerPrefs.SetFloat(SfxKey, sfx);       Save(); } }

        public static QualityLevel Quality
        {
            get { Load(); return quality; }
            set { Load(); quality = (QualityLevel)Math.Max(0, Math.Min(2, (int)value)); PlayerPrefs.SetInt(QualityKey, (int)quality); Save(); }
        }

        public static bool Fullscreen
        {
            get { Load(); return fullscreen; }
            set { Load(); fullscreen = value; PlayerPrefs.SetInt(FullscreenKey, value ? 1 : 0); Save(); }
        }

        /// <summary>Volumen real a partir del valor del slider (0–1): valor², recortado.</summary>
        public static float Curve(float v) { float c = Clamp01(v); return c * c; }

        public static string NameOf(QualityLevel q) => q == QualityLevel.Baja ? "Baja" : q == QualityLevel.Media ? "Media" : "Alta";

        public static void ResetToDefaults()
        {
            loaded = true;
            master = DefaultMaster; music = DefaultMusic; sfx = DefaultSfx;
            quality = DefaultQuality; fullscreen = DefaultFullscreen;
            PlayerPrefs.SetFloat(MasterKey, master); PlayerPrefs.SetFloat(MusicKey, music); PlayerPrefs.SetFloat(SfxKey, sfx);
            PlayerPrefs.SetInt(QualityKey, (int)quality); PlayerPrefs.SetInt(FullscreenKey, 1);
            Save();
        }

        /// <summary>Solo el volumen general (al mover un slider: no toca la pantalla ni la calidad).</summary>
        public static void ApplyAudio() => AudioListener.volume = Curve(Master);

        /// <summary>Solo la pantalla completa (al cambiarla), para no reasignar el modo de pantalla en cada ajuste.</summary>
        public static void ApplyDisplay()
        {
#if !UNITY_EDITOR
            var mode = Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            if (Screen.fullScreenMode != mode) Screen.fullScreenMode = mode;
#endif
        }

        /// <summary>Aplica todo: volumen general, pantalla completa y calidad (al arrancar y al cargar cada escena).</summary>
        public static void ApplyAll()
        {
            ApplyAudio();
            ApplyDisplay();
            QualityPresets.Apply(Quality);
        }

        private static void Load()
        {
            if (loaded) return;
            loaded = true;
            master = Clamp01(PlayerPrefs.GetFloat(MasterKey, DefaultMaster));
            music = Clamp01(PlayerPrefs.GetFloat(MusicKey, DefaultMusic));
            sfx = Clamp01(PlayerPrefs.GetFloat(SfxKey, DefaultSfx));
            quality = (QualityLevel)Math.Max(0, Math.Min(2, PlayerPrefs.GetInt(QualityKey, (int)DefaultQuality)));
            fullscreen = PlayerPrefs.GetInt(FullscreenKey, DefaultFullscreen ? 1 : 0) == 1;
        }

        private static void Save()
        {
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;

        // Si se desactiva la recarga del dominio al entrar en Play, se vuelve a leer de PlayerPrefs
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { loaded = false; Changed = null; }
    }
}
