using UnityEngine;
using Nexus7.UI;

namespace Nexus7.Interaction
{
    /// <summary>
    /// Terminal de mantenimiento: al usarla desbloquea una puerta, muestra mensajes de historia
    /// y actualiza el objetivo. Se usa una sola vez.
    ///
    /// Colocar en: el cuerpo de la terminal (el objeto con collider).
    /// </summary>
    public class AccessTerminal : Interactable
    {
        [SerializeField] private DoorController targetDoor;
        [SerializeField, TextArea] private string[] messagesOnUse =
        {
            "TERMINAL C-7: Energía auxiliar redirigida a la puerta del corredor.",
            "IA NEXUS-7: Puerta desbloqueada. Procede con cautela, tripulante."
        };
        [SerializeField] private string objectiveAfterUse = "Objetivo: Cruza la puerta del corredor C-7.";

        [Header("Pantalla (opcional)")]
        [SerializeField] private Renderer screenRenderer;
        [SerializeField] private Color usedScreenColor = new Color(0.1f, 1f, 0.3f);

        private bool used;

        public override string Prompt => promptText;
        public override bool CanInteract(PlayerInteractor interactor) => !used && base.CanInteract(interactor);

        protected override void OnInteract(PlayerInteractor interactor)
        {
            used = true;
            if (targetDoor != null) targetDoor.Unlock();

            var hud = HUDManager.Instance;
            if (hud != null)
            {
                foreach (var msg in messagesOnUse) hud.QueueMessage(msg);
                if (!string.IsNullOrEmpty(objectiveAfterUse)) hud.SetObjective(objectiveAfterUse);
            }

            if (screenRenderer != null)
            {
                var mat = screenRenderer.material;
                mat.color = usedScreenColor;
                mat.SetColor("_EmissionColor", usedScreenColor * 2f);
            }
        }
    }
}
