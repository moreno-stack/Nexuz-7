using UnityEngine;
using UnityEngine.SceneManagement;
using Nexus7.Core;

namespace Nexus7.Audio
{
    public enum AudioCategory { Music, Sfx }

    /// <summary>
    /// Volumen por canal (Configuración → Música / Efectos) de un AudioSource: volumen = volumen base × curva(canal).
    /// El volumen general lo aplica GameSettings a todo el juego (AudioListener.volume).
    /// Al cargar cada escena, los AudioSource que no tengan uno reciben un AudioChannel de Efectos.
    ///
    /// Colocar en: el mismo objeto que el AudioSource (la música lo pone MusicPlayer).
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class AudioChannel : MonoBehaviour
    {
        [SerializeField] private AudioCategory category = AudioCategory.Sfx;
        [Tooltip("Volumen a canal 100 % (negativo = el que tenga el AudioSource al empezar).")]
        [SerializeField] private float baseVolume = -1f;

        private AudioSource source;

        public AudioCategory Category { get => category; set { category = value; Apply(); } }
        public float BaseVolume { get => baseVolume; set { baseVolume = Mathf.Clamp01(value); Apply(); } }

        public static float VolumeFor(AudioCategory c, float baseVolume) =>
            baseVolume * GameSettings.Curve(c == AudioCategory.Music ? GameSettings.Music : GameSettings.Sfx);

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            if (baseVolume < 0f) baseVolume = source.volume;
        }

        private void OnEnable() { GameSettings.Changed += Apply; Apply(); }
        private void OnDisable() => GameSettings.Changed -= Apply;

        public void Apply()
        {
            if (source == null) source = GetComponent<AudioSource>();
            if (source != null) source.volume = VolumeFor(category, Mathf.Max(0f, baseVolume));
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            SceneManager.sceneLoaded -= TagScene;
            SceneManager.sceneLoaded += TagScene;
            TagScene(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        /// <summary>Los AudioSource sin canal (puertas, robots, chispas...) pasan a ser Efectos.</summary>
        private static void TagScene(Scene scene, LoadSceneMode mode)
        {
            foreach (var src in FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
                if (src.GetComponent<AudioChannel>() == null) src.gameObject.AddComponent<AudioChannel>();
        }
    }
}
