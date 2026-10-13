using UnityEngine;
using Nexus7.Core;

namespace Nexus7.Devices
{
    /// <summary>
    /// Emisor de pulso sonoro (señuelo). Se lanza; cuando se detiene en el suelo empieza a emitir
    /// pulsos de ruido que atraen a los drones (implementa IDistraction, así que los drones saben que es un señuelo:
    /// van a inspeccionarlo y lo destruyen, y si les lanzas otro poco después buscan desde dónde lo lanzaste).
    ///
    /// Lo crea DeviceFactory en tiempo de ejecución.
    /// </summary>
    public class DecoyDevice : MonoBehaviour, IDistraction
    {
        private DeviceItemData data;
        private Vector3 origin;
        private Light beacon;
        private Renderer body;
        private float spawnTime, stillTime, pulseTimer, endTime;
        private Vector3 lastPos;
        private bool landed, active, neutralized;

        public bool IsActive => active;
        public Vector3 Position => transform.position;
        public Vector3 Origin => origin;

        public void Setup(DeviceItemData device, Vector3 throwOrigin, Light light)
        {
            data = device;
            origin = throwOrigin;
            beacon = light;
            body = GetComponent<Renderer>();
            spawnTime = Time.time;
            lastPos = transform.position;
        }

        private void Update()
        {
            if (data == null) return;

            if (!landed)
            {
                // Se considera "aterrizado" cuando lleva 0.25 s casi quieto (o a los 3 s, por si acaso).
                stillTime = (transform.position - lastPos).sqrMagnitude < 0.0001f ? stillTime + Time.deltaTime : 0f;
                lastPos = transform.position;
                if (stillTime > 0.25f || Time.time - spawnTime > 3f) Land();
                return;
            }

            if (!active) return;

            pulseTimer -= Time.deltaTime;
            if (pulseTimer <= 0f)
            {
                pulseTimer = Mathf.Max(0.3f, data.EffectValue);
                NoiseSystem.Emit(transform.position, data.Radius, gameObject);
                PulseRing.Spawn(transform.position, data.Radius * 0.35f, data.DeviceColor, 0.5f);
                if (beacon != null) beacon.intensity = 3f;
            }
            if (beacon != null) beacon.intensity = Mathf.MoveTowards(beacon.intensity, 0.4f, 6f * Time.deltaTime);

            if (Time.time >= endTime) Shutdown(5f);
        }

        private void Land()
        {
            landed = true;
            active = true;
            endTime = Time.time + data.Duration;
            pulseTimer = 0f;
        }

        /// <summary>Un dron lo ha inspeccionado y destruido.</summary>
        public void Neutralize()
        {
            if (neutralized) return;
            neutralized = true;
            PulseRing.Spawn(transform.position, 1.5f, Color.red, 0.4f);
            Shutdown(1.5f);
        }

        private void Shutdown(float destroyDelay)
        {
            active = false;
            if (beacon != null) { beacon.color = Color.red; beacon.intensity = 0.3f; }
            if (body != null) body.material.color = Color.gray;
            Destroy(gameObject, destroyDelay);
        }
    }
}
