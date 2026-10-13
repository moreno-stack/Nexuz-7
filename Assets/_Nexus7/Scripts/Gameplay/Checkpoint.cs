using UnityEngine;
using Nexus7.UI;

namespace Nexus7.Gameplay
{
    /// <summary>
    /// Punto de control: al cruzarlo, el jugador reaparecerá aquí si lo capturan.
    /// La orientación (eje Y) del objeto define hacia dónde mirará al reaparecer.
    ///
    /// Colocar en: GameObject vacío con BoxCollider "Is Trigger" (capa "Ignore Raycast").
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Checkpoint : MonoBehaviour
    {
        [SerializeField] private string notification = "Punto seguro registrado";
        private bool activated;

        private void Reset() => GetComponent<Collider>().isTrigger = true;

        private void OnTriggerEnter(Collider other)
        {
            if (activated) return;
            var respawn = other.GetComponent<PlayerRespawn>();
            if (respawn == null) return;

            activated = true;
            respawn.SetCheckpoint(transform);
            if (!string.IsNullOrEmpty(notification)) PickupNotificationUI.Notify(notification, new Color(0.5f, 0.9f, 1f));
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.6f);
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.6f);
            Gizmos.DrawRay(transform.position, transform.forward);
        }
    }
}
