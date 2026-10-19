using UnityEngine;

namespace Nexus7.Inventory
{
    /// <summary>
    /// Punto donde puede aparecer un objeto aleatorio (en el suelo, en una estantería, sobre una mesa...).
    /// Colocar en: un GameObject vacío a la altura del soporte; lo usa RoomLootSpawner.
    /// </summary>
    public class LootSpawnPoint : MonoBehaviour
    {
        [Tooltip("Probabilidad (0–1) de que este punto quede vacío.")]
        [Range(0f, 1f)] public float emptyChance = 0.35f;

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.8f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.2f, 0.2f);
        }
    }
}
