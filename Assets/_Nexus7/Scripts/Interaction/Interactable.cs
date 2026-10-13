using UnityEngine;
using UnityEngine.Events;

namespace Nexus7.Interaction
{
    /// <summary>
    /// Clase base para interactuables. Hereda de aquí para crear objetos nuevos
    /// sobrescribiendo OnInteract(). El UnityEvent permite además encadenar acciones
    /// desde el Inspector sin escribir código (p. ej. encender una luz).
    /// </summary>
    public abstract class Interactable : MonoBehaviour, IInteractable
    {
        [Tooltip("Texto que se muestra al apuntar al objeto.")]
        [SerializeField] protected string promptText = "Interactuar";
        [Tooltip("Acciones extra que se ejecutan al interactuar (configurables en el Inspector).")]
        [SerializeField] private UnityEvent onInteract;

        public virtual string Prompt => promptText;

        public virtual bool CanInteract(PlayerInteractor interactor) => isActiveAndEnabled;

        public void Interact(PlayerInteractor interactor)
        {
            OnInteract(interactor);
            onInteract?.Invoke();
        }

        protected abstract void OnInteract(PlayerInteractor interactor);
    }
}
