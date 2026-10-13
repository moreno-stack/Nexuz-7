using UnityEngine;
using UnityEngine.Events;
using Nexus7.UI;

namespace Nexus7.Tutorial
{
    /// <summary>
    /// Zona invisible que muestra mensajes / cambia el objetivo cuando el jugador entra.
    /// Requiere que el jugador tenga la etiqueta (Tag) "Player".
    ///
    /// Colocar en: un GameObject vacío con BoxCollider marcado como "Is Trigger".
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class TutorialTrigger : MonoBehaviour
    {
        [SerializeField, TextArea(2, 4)] private string[] messages;
        [Tooltip("Dejar vacío para no cambiar el objetivo.")]
        [SerializeField] private string newObjective;
        [SerializeField] private bool triggerOnce = true;
        [SerializeField] private UnityEvent onTriggered;

        private bool triggered;

        // Reset se llama al añadir el componente en el editor: marca el collider como trigger automáticamente.
        private void Reset() => GetComponent<Collider>().isTrigger = true;

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            if (triggerOnce && triggered) return;
            triggered = true;

            var hud = HUDManager.Instance;
            if (hud != null)
            {
                if (messages != null) foreach (var m in messages) hud.QueueMessage(m);
                if (!string.IsNullOrEmpty(newObjective)) hud.SetObjective(newObjective);
            }
            onTriggered?.Invoke();
        }

        private void OnDrawGizmos()
        {
            var box = GetComponent<BoxCollider>();
            if (box == null) return;
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.25f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
        }
    }
}
