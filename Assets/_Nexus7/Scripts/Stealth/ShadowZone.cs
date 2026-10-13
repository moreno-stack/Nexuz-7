using UnityEngine;

namespace Nexus7.Stealth
{
    /// <summary>
    /// Zona oscura (detrás de cajas, rincones sin luz). Mientras el jugador esté dentro,
    /// es menos visible para los enemigos.
    ///
    /// Colocar en: GameObject vacío con BoxCollider "Is Trigger", en la capa "Ignore Raycast"
    /// (para que no bloquee raycasts ni el NavMesh).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ShadowZone : MonoBehaviour
    {
        private void Reset() => GetComponent<Collider>().isTrigger = true;

        private void OnTriggerEnter(Collider other)
        {
            var stealth = other.GetComponent<PlayerStealth>();
            if (stealth != null) stealth.EnterShadow();
        }

        private void OnTriggerExit(Collider other)
        {
            var stealth = other.GetComponent<PlayerStealth>();
            if (stealth != null) stealth.ExitShadow();
        }

        private void OnDrawGizmos()
        {
            var box = GetComponent<BoxCollider>();
            if (box == null) return;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(0.1f, 0.1f, 0.3f, 0.35f);
            Gizmos.DrawCube(box.center, box.size);
        }
    }
}
