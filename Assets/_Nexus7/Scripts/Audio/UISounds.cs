using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Nexus7.Audio
{
    /// <summary>
    /// Sonidos de la interfaz (canal Efectos): al pasar por encima o seleccionar un botón con teclado/mando,
    /// al pulsarlo y al volver (botones «Volver» y «No»).
    ///
    /// Colocar en: el Canvas del menú (engancha todos sus botones al empezar). UISounds.Play(...) sirve desde cualquier script.
    /// </summary>
    public class UISounds : MonoBehaviour
    {
        private static AudioSource source;
        private static readonly Dictionary<UiSound, AudioClip> clips = new Dictionary<UiSound, AudioClip>();
        private static float lastHover = -1f;

        private void Start()
        {
            foreach (var button in GetComponentsInChildren<Button>(true))
            {
                bool back = button.name.Contains("Volver") || button.name.Contains("No");
                button.onClick.AddListener(() => Play(back ? UiSound.Back : UiSound.Click));
                var trigger = button.GetComponent<EventTrigger>();
                if (trigger == null) trigger = button.gameObject.AddComponent<EventTrigger>();
                AddEntry(trigger, EventTriggerType.PointerEnter);
                AddEntry(trigger, EventTriggerType.Select);
            }
        }

        private static void AddEntry(EventTrigger trigger, EventTriggerType type)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(_ => Hover());
            trigger.triggers.Add(entry);
        }

        /// <summary>El sonido de «pasar por encima», como mucho cada 0,05 s.</summary>
        private static void Hover()
        {
            if (Time.unscaledTime - lastHover < 0.05f) return;
            lastHover = Time.unscaledTime;
            Play(UiSound.Hover);
        }

        public static void Play(UiSound kind)
        {
            if (source == null)
            {
                var go = new GameObject("SonidosUI");
                DontDestroyOnLoad(go);
                source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                source.volume = 1f;
                go.AddComponent<AudioChannel>().Category = AudioCategory.Sfx;
            }
            if (!clips.TryGetValue(kind, out var clip) || clip == null)
            {
                clip = MusicPlayer.MakeClip("UI_" + kind, ProceduralAudio.Ui(kind));
                clips[kind] = clip;
            }
            source.PlayOneShot(clip);
        }
    }
}
