using UnityEngine;

namespace Nexus7.Environment
{
    /// <summary>
    /// Restos flotando en el espacio (vistos por el ventanal): giran lentamente y derivan.
    /// </summary>
    public class DebrisDrift : MonoBehaviour
    {
        [SerializeField] private Vector3 velocity = new Vector3(0f, 0.1f, -0.3f);
        [SerializeField] private Vector3 spin = new Vector3(8f, 15f, 4f);

        private void Update()
        {
            transform.position += velocity * Time.deltaTime;
            transform.Rotate(spin * Time.deltaTime, Space.Self);
        }
    }
}
