using UnityEngine;
using Nexus7.UI;

namespace Nexus7.Interaction
{
    /// <summary>
    /// Objeto que solo muestra texto al usarlo: bitácoras, carteles, pantallas rotas...
    /// Sirve para contar historia ambiental.
    ///
    /// Colocar en: cualquier objeto con collider.
    /// </summary>
    public class InteractableMessage : Interactable
    {
        [SerializeField, TextArea(2, 5)] private string[] messages;
        [Tooltip("Si es true, solo puede leerse una vez.")]
        [SerializeField] private bool oneShot = false;

        private bool used;

        public override bool CanInteract(PlayerInteractor interactor) => !(oneShot && used) && base.CanInteract(interactor);

        protected override void OnInteract(PlayerInteractor interactor)
        {
            var hud = HUDManager.Instance;
            if (hud == null || hud.IsShowingMessages) return; // no encolar repeticiones mientras se lee
            used = true;
            foreach (var msg in messages) hud.QueueMessage(msg);
        }
    }
}
