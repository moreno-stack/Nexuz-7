using UnityEngine;
using Nexus7.Stealth;

namespace Nexus7.Director
{
    /// <summary>
    /// Zona a oscuras de un APAGÓN (ShipEventDirector): como ShadowZone, pero temporal. Al desaparecer con el jugador
    /// dentro le devuelve la luz (ShadowZone no lo hace porque nunca se destruye en mitad de la partida).
    ///
    /// Colocar en: un objeto con BoxCollider en modo trigger (lo crea ShipEventDirector en la capa Ignore Raycast).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class BlackoutZone : MonoBehaviour
    {
        private PlayerStealth inside;

        private void OnTriggerEnter(Collider other)
        {
            var stealth = other.GetComponent<PlayerStealth>();
            if (stealth == null || stealth == inside) return;
            inside = stealth;
            stealth.EnterShadow();
        }

        private void OnTriggerExit(Collider other)
        {
            var stealth = other.GetComponent<PlayerStealth>();
            if (stealth == null || stealth != inside) return;
            inside = null;
            stealth.ExitShadow();
        }

        private void OnDisable()
        {
            if (inside != null) inside.ExitShadow();
            inside = null;
        }
    }
}
