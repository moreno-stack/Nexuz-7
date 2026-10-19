using System.Linq;
using UnityEngine;
using Nexus7.Interaction;
using Nexus7.UI;

namespace Nexus7.Gameplay
{
    /// <summary>
    /// Secuencia de lanzamiento de una cápsula de salvamento: una lista de pasos (LaunchStepTerminal).
    /// Cuando TODOS están completados: abre la escotilla de la cápsula, pone su baliza en verde y avisa.
    ///
    /// Colocar en: la cápsula funcional (junto a EscapePod).
    /// </summary>
    public class LaunchSequence : MonoBehaviour
    {
        [SerializeField] private LaunchStepTerminal[] steps;
        [SerializeField] private DoorController hatch;
        [SerializeField] private Light beacon;
        [SerializeField] private Renderer beaconRenderer;
        [SerializeField] private Color pendingColor = new Color(1f, 0.45f, 0.05f);
        [SerializeField] private Color readyColor = new Color(0.2f, 1f, 0.4f);
        [SerializeField, TextArea] private string readyMessage = "IA NEXUS-7: Cápsula B preparada. Escotilla abierta. ¡Embarca y lanza!";

        public int Completed => steps == null ? 0 : steps.Count(s => s != null && s.IsCompleted);
        public int Total => steps == null ? 0 : steps.Length;
        public bool IsReady => Total > 0 && Completed == Total;

        private bool announced;

        private void Start() => UpdateBeacon();

        /// <summary>Lo llaman los pasos al completarse.</summary>
        public void NotifyStepCompleted(LaunchStepTerminal step)
        {
            if (HUDManager.Instance != null)
                HUDManager.Instance.QueueMessage($"Secuencia de lanzamiento: {step.StepName} completado ({Completed}/{Total}).", 2.5f);
            UpdateBeacon();
            if (!IsReady || announced) return;

            announced = true;
            if (hatch != null) { hatch.Unlock(); hatch.SetOpen(true); }
            if (HUDManager.Instance != null) HUDManager.Instance.QueueMessage(readyMessage, 3f);
        }

        private void UpdateBeacon()
        {
            Color c = IsReady ? readyColor : pendingColor;
            if (beacon != null) beacon.color = c;
            if (beaconRenderer != null)
            {
                var m = beaconRenderer.material;
                m.color = c;
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", c * 3f);
            }
        }
    }
}
