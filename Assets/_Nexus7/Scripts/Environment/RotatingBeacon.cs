using UnityEngine;

namespace Nexus7.Environment
{
    /// <summary>
    /// Baliza giratoria (luz de despegue / emergencia): rota en Y a velocidad constante.
    /// Pon una Spot Light como hijo inclinada hacia fuera para el clásico barrido de luz.
    /// </summary>
    public class RotatingBeacon : MonoBehaviour
    {
        [SerializeField] private float degreesPerSecond = 220f;

        private void Update() => transform.Rotate(Vector3.up, degreesPerSecond * Time.deltaTime, Space.World);
    }
}
