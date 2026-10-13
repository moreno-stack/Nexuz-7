using UnityEngine;
using Nexus7.Core;
using Nexus7.UI;

namespace Nexus7.Devices
{
    /// <summary>
    /// Pulso PEM temporal. Se lanza y detona al primer impacto (o a los 2 s):
    ///   · drones en el radio → aturdidos (quietos, ciegos y sordos) durante Duration s
    ///   · cámaras, barreras y sensores láser en el radio → apagados durante Duration s
    ///   · la detonación HACE RUIDO: los drones fuera del radio vienen a ver qué ha pasado.
    /// Afecta a todo lo registrado en EmpRegistry (IEmpTarget).
    /// </summary>
    public class EmpDevice : MonoBehaviour
    {
        [SerializeField] private float fuse = 2f;

        private DeviceItemData data;
        private Light glow;
        private float spawnTime;
        private bool detonated;

        public void Setup(DeviceItemData device, Light light)
        {
            data = device;
            glow = light;
            spawnTime = Time.time;
        }

        private void Update()
        {
            if (data == null || detonated) return;
            if (glow != null) glow.intensity = 1f + Mathf.PingPong(Time.time * 12f, 2f); // parpadeo de carga
            if (Time.time - spawnTime >= fuse) Detonate();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (Time.time - spawnTime > 0.05f) Detonate();
        }

        private void Detonate()
        {
            if (detonated || data == null) return;
            detonated = true;

            int hit = EmpRegistry.Pulse(transform.position, data.Radius, data.Duration);
            NoiseSystem.Emit(transform.position, data.Radius * 1.8f, gameObject);
            PulseRing.Spawn(transform.position, data.Radius, data.DeviceColor, 0.5f);
            PulseRing.Spawn(transform.position, data.Radius * 0.6f, Color.white, 0.35f);
            PickupNotificationUI.Notify(hit > 0 ? $"PEM: {hit} sistema(s) inutilizado(s) {data.Duration:0} s" : "PEM: ningún objetivo en el radio",
                                        hit > 0 ? data.DeviceColor : new Color(0.7f, 0.7f, 0.7f));

            if (glow != null) { glow.intensity = 8f; glow.range = data.Radius; }
            var r = GetComponent<Renderer>();
            if (r != null) r.enabled = false;
            Destroy(gameObject, 0.15f);
        }
    }
}
