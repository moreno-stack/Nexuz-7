using UnityEngine;
using Nexus7.Gameplay;
using Nexus7.Hazards;
using Nexus7.UI;

namespace Nexus7.Environment
{
    /// <summary>
    /// Explosiones internas de la nave a intervalos aleatorios, cada vez más frecuentes según avanza la cuenta atrás:
    /// sacudida fuerte de cámara, ráfagas de chispas en todo el hangar, destello de luz y algún aviso de la IA.
    ///
    /// Colocar en: GameSystems de la escena final.
    /// </summary>
    public class ShipTremor : MonoBehaviour
    {
        [SerializeField] private EvacuationCountdown countdown;
        [SerializeField] private CameraShake cameraShake;
        [SerializeField] private Vector2 normalInterval = new Vector2(22f, 35f);
        [SerializeField] private Vector2 criticalInterval = new Vector2(6f, 12f);
        [SerializeField] private Light flashLight;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip[] explosionClips;

        private SparkEmitter[] sparks;
        private float timer, flash;
        private static readonly string[] messages =
        {
            "IA NEXUS-7: Explosión en la cubierta inferior. La integridad del casco cae.",
            "IA NEXUS-7: Fallo estructural en el anillo de energía.",
            "IA NEXUS-7: Fuga en los conductos de refrigeración. ¡Date prisa!"
        };
        private int messageIndex;

        private void Start()
        {
            sparks = FindObjectsByType<SparkEmitter>(FindObjectsSortMode.None);
            timer = Random.Range(10f, 18f);
        }

        private void Update()
        {
            if (flashLight != null)
            {
                flash = Mathf.MoveTowards(flash, 0f, 6f * Time.deltaTime);
                flashLight.intensity = flash;
            }
            if (countdown != null && !countdown.Running) return;
            if (Nexus7.Cinematics.CinematicDirector.IsPlaying) return;   // sin temblores ni mensajes durante una cinemática

            timer -= Time.deltaTime;
            if (timer > 0f) return;

            float k = countdown != null ? 1f - countdown.Remaining / countdown.Duration : 0f;
            timer = Mathf.Lerp(Random.Range(normalInterval.x, normalInterval.y), Random.Range(criticalInterval.x, criticalInterval.y), k);
            Tremor(Mathf.Lerp(0.45f, 0.85f, k));
        }

        public void Tremor(float strength)
        {
            if (cameraShake != null) cameraShake.AddTrauma(strength);
            flash = 4f * strength;
            if (sparks != null) foreach (var s in sparks) if (s != null && Random.value < 0.6f) s.Burst();
            if (audioSource != null && explosionClips != null && explosionClips.Length > 0)
                audioSource.PlayOneShot(explosionClips[Random.Range(0, explosionClips.Length)]);
            if (Random.value < 0.5f && HUDManager.Instance != null)
                HUDManager.Instance.QueueMessage(messages[messageIndex++ % messages.Length], 2f);
        }
    }
}
