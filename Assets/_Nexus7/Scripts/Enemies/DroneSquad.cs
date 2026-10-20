using UnityEngine;
using Nexus7.UI;

namespace Nexus7.Enemies
{
    /// <summary>
    /// Patrulla coordinada: cuando un miembro del escuadrón empieza a perseguir al jugador,
    /// los demás acuden de inmediato a esa posición (cerrando rutas de escape).
    /// Combinado con rutas compartidas desfasadas (cada dron empieza en un extremo del bucle),
    /// cubren la sala sin dejar huecos largos.
    ///
    /// Colocar en: un GameObject vacío "Escuadron" y arrastrar los EnemyPatrol a Members.
    /// </summary>
    public class DroneSquad : MonoBehaviour
    {
        [SerializeField] private EnemyPatrol[] members;
        [SerializeField] private string squadName = "Escuadrón de élite";
        [SerializeField] private float messageCooldown = 12f;

        private float lastMessage = -999f;

        private void OnEnable() => EnemyPatrol.PlayerSpotted += OnSpotted;
        private void OnDisable() => EnemyPatrol.PlayerSpotted -= OnSpotted;

        private void OnSpotted(EnemyPatrol spotter)
        {
            if (members == null || System.Array.IndexOf(members, spotter) < 0) return;

            Vector3 target = spotter.Vision.LastSeenPosition;
            foreach (var m in members)
                if (m != null && m != spotter) m.RespondToSquad(target);

            if (Time.time - lastMessage > messageCooldown && HUDManager.Instance != null)
            {
                lastMessage = Time.time;
                HUDManager.Instance.QueueMessage($"IA NEXUS-7: {squadName}: un robot te ha visto y los demás convergen en tu posición.", 2.5f);
            }
        }
    }
}
