using System;
using UnityEngine;
using Nexus7.Player;
using Nexus7.UI;

namespace Nexus7.Interaction
{
    /// <summary>
    /// Lanza un raycast desde el centro de la cámara. Si golpea un IInteractable, muestra
    /// "[E] Texto" en el HUD y, al pulsar E (o A en el mando), ejecuta la interacción.
    ///
    /// Colocar en: la cámara del jugador (Player/CameraRoot/PlayerCamera).
    /// </summary>
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private float interactDistance = 2.5f;
        [Tooltip("Capas que el raycast puede golpear. Las paredes también deben estar incluidas para que bloqueen la vista.")]
        [SerializeField] private LayerMask interactMask = ~0;
        [SerializeField] private KeyCode interactKey = KeyCode.E;
        [Tooltip("JoystickButton0 = botón A en mando Xbox.")]
        [SerializeField] private KeyCode interactPadButton = KeyCode.JoystickButton0;

        /// <summary>Se dispara tras cada interacción (el tutorial lo usa para avanzar).</summary>
        public event Action<IInteractable> Interacted;

        public IInteractable Current { get; private set; }
        private string shownPrompt;

        private void Update()
        {
            bool canAct = player == null || player.InputEnabled;
            Current = canAct ? FindTarget() : null;
            RefreshPrompt();

            if (Current != null && (Input.GetKeyDown(interactKey) || Input.GetKeyDown(interactPadButton)))
            {
                Current.Interact(this);
                Interacted?.Invoke(Current);
            }
        }

        private IInteractable FindTarget()
        {
            var ray = new Ray(transform.position, transform.forward);
            if (!Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactMask, QueryTriggerInteraction.Ignore))
                return null;

            // GetComponentInParent permite poner el script en el padre y el collider en un hijo (p. ej. el panel de la puerta).
            var target = hit.collider.GetComponentInParent<IInteractable>();
            return (target != null && target.CanInteract(this)) ? target : null;
        }

        private void RefreshPrompt()
        {
            var hud = HUDManager.Instance;
            if (hud == null) return;

            string wanted = Current != null ? $"[E] {Current.Prompt}" : null;
            if (wanted == shownPrompt) return; // solo tocamos la UI cuando cambia

            shownPrompt = wanted;
            if (wanted == null) hud.HidePrompt();
            else hud.ShowPrompt(wanted);
        }

        private void OnDisable()
        {
            if (HUDManager.Instance != null) HUDManager.Instance.HidePrompt();
            shownPrompt = null;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, transform.forward * interactDistance);
        }
    }
}
