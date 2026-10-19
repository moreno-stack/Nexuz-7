using UnityEngine;
using Nexus7.Core;

namespace Nexus7.Environment
{
    /// <summary>
    /// Luz ambiental, niebla y sirena de la escena según el nivel de alerta.
    ///
    /// Colocar en: un GameObject de la escena (uno por escena). Opcional: AudioSource en bucle para la sirena.
    /// </summary>
    public class AlertAmbience : MonoBehaviour
    {
        [SerializeField] private Color normalAmbient = new Color(0.03f, 0.045f, 0.07f);
        [SerializeField] private Color yellowAmbient = new Color(0.07f, 0.05f, 0.025f);
        [SerializeField] private Color redAmbient = new Color(0.09f, 0.015f, 0.01f);
        [SerializeField] private Color normalFog = new Color(0.02f, 0.03f, 0.05f);
        [SerializeField] private Color yellowFog = new Color(0.04f, 0.03f, 0.015f);
        [SerializeField] private Color redFog = new Color(0.05f, 0.008f, 0.006f);
        [SerializeField] private float speed = 1.5f;
        [SerializeField] private AudioSource siren;

        private void Update()
        {
            var level = AlertSystem.Level;
            Color a = level == AlertLevel.Red ? redAmbient : level == AlertLevel.Yellow ? yellowAmbient : normalAmbient;
            Color f = level == AlertLevel.Red ? redFog : level == AlertLevel.Yellow ? yellowFog : normalFog;
            float k = speed * Time.deltaTime;
            RenderSettings.ambientLight = Color.Lerp(RenderSettings.ambientLight, a, k);
            RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, f, k);

            if (siren == null) return;
            bool on = level == AlertLevel.Red;
            if (on && !siren.isPlaying) siren.Play();
            else if (!on && siren.isPlaying) siren.Stop();
        }
    }
}
