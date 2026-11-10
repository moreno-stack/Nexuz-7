using UnityEngine;
using Nexus7.UI;

namespace Nexus7.Gameplay
{
    /// <summary>
    /// Punto de control: al cruzarlo, el jugador reaparecerá aquí si lo capturan.
    /// La orientación (eje Y) del objeto define hacia dónde mirará al reaparecer.
    /// Inteligente: si lo cruzas mientras un robot te persigue, no se guarda hasta que te pierdan de vista
    /// (RespawnPenalty.CanActivateCheckpoint), así no se puede «guardar» en mitad de una huida.
    ///
    /// Colocar en: GameObject vacío con BoxCollider "Is Trigger" (capa "Ignore Raycast").
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Checkpoint : MonoBehaviour
    {
        [SerializeField] private string notification = "Punto seguro registrado";
        [SerializeField] private string blockedNotification = "Punto seguro bloqueado: te están persiguiendo";
        private bool activated;
        private PlayerRespawn pending;   // lo cruzó durante una persecución: se guarda al acabar
        // Si cruza varios durante la misma persecución, solo cuenta el último
        private static Checkpoint latestPending;

        private void Reset() => GetComponent<Collider>().isTrigger = true;

        private void OnTriggerEnter(Collider other)
        {
            if (activated) return;
            var respawn = other.GetComponent<PlayerRespawn>();
            if (respawn == null) return;

            if (!RespawnPenalty.CanActivateCheckpoint(Nexus7.Enemies.EnemyPatrol.AnyChasing))
            {
                if (pending == null && !string.IsNullOrEmpty(blockedNotification))
                    PickupNotificationUI.Notify(blockedNotification, new Color(1f, 0.55f, 0.3f));
                pending = respawn;
                latestPending = this;
                return;
            }
            Activate(respawn);
        }

        // Si te atrapan durante la persecución, el punto que cruzaste no cuenta
        private void OnEnable() => PlayerRespawn.PlayerCaught += ClearPending;
        private void OnDisable() => PlayerRespawn.PlayerCaught -= ClearPending;
        private void ClearPending() { pending = null; if (latestPending == this) latestPending = null; }

        private void Update()
        {
            if (pending == null || activated) return;
            if (latestPending != this) { pending = null; return; }   // se cruzó otro después: ese es el bueno
            if (RespawnPenalty.CanActivateCheckpoint(Nexus7.Enemies.EnemyPatrol.AnyChasing) && !pending.IsRespawning) Activate(pending);
        }

        private void Activate(PlayerRespawn respawn)
        {
            activated = true;
            pending = null;
            if (latestPending == this) latestPending = null;
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
