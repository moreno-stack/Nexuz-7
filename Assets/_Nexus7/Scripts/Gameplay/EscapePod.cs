using UnityEngine;
using Nexus7.Interaction;
using Nexus7.UI;

namespace Nexus7.Gameplay
{
    /// <summary>
    /// Cápsula de salvamento. Con la secuencia lista, interactuar = embarcar y lanzar → EndingManager.Escape().
    /// Sin secuencia (o en cápsulas averiadas) solo informa.
    ///
    /// Colocar en: el asiento/panel interior de la cápsula (objeto con collider).
    /// </summary>
    public class EscapePod : Interactable
    {
        [SerializeField] private LaunchSequence sequence;
        [SerializeField] private string podName = "Cápsula B";

        public override string Prompt =>
            sequence == null ? $"{podName}: fuera de servicio"
            : sequence.IsReady ? $"Embarcar en la {podName} y LANZAR"
            : $"{podName}: lanzamiento no preparado ({sequence.Completed}/{sequence.Total})";

        protected override void OnInteract(PlayerInteractor interactor)
        {
            if (sequence == null || !sequence.IsReady)
            {
                if (HUDManager.Instance != null)
                    HUDManager.Instance.QueueMessage(sequence == null
                        ? $"{podName}: sistemas destruidos. No responde."
                        : "Completa la secuencia de lanzamiento: libera las abrazaderas y carga la celda de ignición.", 2.5f);
                return;
            }
            if (EndingManager.Instance != null) EndingManager.Instance.Escape();
        }
    }
}
