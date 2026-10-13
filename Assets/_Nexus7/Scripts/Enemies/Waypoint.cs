using UnityEngine;
using Nexus7.Core;

namespace Nexus7.Enemies
{
    /// <summary>
    /// Datos opcionales de un punto de patrulla: cuánto espera el enemigo y si debe
    /// girarse hacia el eje Z (flecha azul) del punto mientras espera — útil para que
    /// "vigile" un pasillo concreto.
    ///
    /// Colocar en: un hijo de PatrolRoute.
    /// </summary>
    public class Waypoint : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float waitTime = 1.5f;
        [SerializeField] private bool faceForward = false;

        [Tooltip("(Fase 4) Algo que el dron revisa al llegar aquí: un Workbench o un InspectableSpot.")]
        [SerializeField] private MonoBehaviour inspectTarget;

        public IInspectable InspectTarget => inspectTarget as IInspectable;
        public float WaitTime => waitTime;
        public bool FaceForward => faceForward;

        private void OnDrawGizmos()
        {
            if (!faceForward) return;
            Gizmos.color = Color.cyan;
            Vector3 p = transform.position + Vector3.up * 0.1f;
            Gizmos.DrawLine(p, p + transform.forward * 1.5f);
        }
    }
}
