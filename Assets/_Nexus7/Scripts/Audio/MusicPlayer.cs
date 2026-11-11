using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Nexus7.Core;

namespace Nexus7.Audio
{
    /// <summary>
    /// Música de fondo (canal Música), generada por código con ProceduralAudio:
    ///   · MenuPrincipal → pad ambiental del menú.
    ///   · Fases → zumbido grave y tenso de la nave.
    /// Al cambiar de escena cruza las pistas con un fundido de 1,5 s. Se crea solo y sobrevive a los cambios de escena.
    /// </summary>
    public class MusicPlayer : MonoBehaviour
    {
        public const float CrossfadeSeconds = 1.5f;
        public const float MenuVolume = 0.8f, ShipVolume = 0.6f;

        private static MusicPlayer instance;
        private AudioClip menuClip, shipClip;
        private AudioSource[] sources;
        private AudioChannel[] channels;
        private int active;
        private Coroutine fade;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (instance != null) return;
            var go = new GameObject("Musica");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<MusicPlayer>();
            instance.Setup();
            SceneManager.sceneLoaded += instance.OnSceneLoaded;
            instance.OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        private void Setup()
        {
            sources = new AudioSource[2];
            channels = new AudioChannel[2];
            for (int i = 0; i < 2; i++)
            {
                var child = new GameObject("Pista" + (i + 1));
                child.transform.SetParent(transform, false);
                var src = child.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.loop = true;
                src.spatialBlend = 0f;
                src.volume = 0f;
                sources[i] = src;
                var ch = child.AddComponent<AudioChannel>();
                ch.Category = AudioCategory.Music;
                ch.BaseVolume = 0f;
                channels[i] = ch;
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            AudioClip clip = null;
            float volume = 0f;
            if (scene.name == GameFlow.MenuScene) { clip = MenuClip(); volume = MenuVolume; }
            else if (GameFlow.IsPhaseScene(scene.name)) { clip = ShipClip(); volume = ShipVolume; }
            PlayTrack(clip, volume);
        }

        private void PlayTrack(AudioClip clip, float volume)
        {
            if (clip != null && sources[active].clip == clip && sources[active].isPlaying) return;   // ya suena
            if (fade != null) StopCoroutine(fade);
            int next = 1 - active;
            if (clip != null)
            {
                sources[next].clip = clip;
                sources[next].Play();
            }
            fade = StartCoroutine(Crossfade(active, next, clip != null ? volume : 0f));
            active = next;
        }

        private IEnumerator Crossfade(int from, int to, float targetVolume)
        {
            float startFrom = channels[from].BaseVolume, startTo = channels[to].BaseVolume;
            for (float t = 0f; t < CrossfadeSeconds; t += Time.unscaledDeltaTime)
            {
                float k = t / CrossfadeSeconds;
                channels[from].BaseVolume = Mathf.Lerp(startFrom, 0f, k);
                channels[to].BaseVolume = Mathf.Lerp(startTo, targetVolume, k);
                yield return null;
            }
            channels[from].BaseVolume = 0f;
            channels[to].BaseVolume = targetVolume;
            sources[from].Stop();
            if (targetVolume <= 0f) sources[to].Stop();
            fade = null;
        }

        private AudioClip MenuClip() => menuClip != null ? menuClip : (menuClip = MakeClip("MusicaMenu", ProceduralAudio.MenuPad(16f)));
        private AudioClip ShipClip() => shipClip != null ? shipClip : (shipClip = MakeClip("ZumbidoNave", ProceduralAudio.ShipDrone(12f)));

        internal static AudioClip MakeClip(string name, float[] samples)
        {
            var clip = AudioClip.Create(name, samples.Length, 1, ProceduralAudio.SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
