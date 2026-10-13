using UnityEngine;

namespace Nexus7.Devices
{
    /// <summary>
    /// Crea en tiempo de ejecución el objeto físico de un dispositivo lanzado (esfera + Rigidbody + luz +
    /// comportamiento). Así no hace falta mantener prefabs: el aspecto sale del color del DeviceItemData.
    /// </summary>
    public static class DeviceFactory
    {
        public static GameObject SpawnThrown(DeviceItemData device, Vector3 position, Vector3 velocity,
                                             Vector3 throwerPosition, Collider ignoreCollider)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Dispositivo_" + device.DisplayName;
            go.transform.position = position;
            go.transform.localScale = Vector3.one * 0.22f;

            var renderer = go.GetComponent<Renderer>();
            var mat = renderer.material;              // instancia propia
            mat.color = device.DeviceColor;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", device.DeviceColor * 1.5f);

            var col = go.GetComponent<Collider>();
            if (ignoreCollider != null) Physics.IgnoreCollision(col, ignoreCollider);

            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 0.4f;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.AddForce(velocity, ForceMode.VelocityChange);

            var lightGo = new GameObject("Luz");
            lightGo.transform.SetParent(go.transform, false);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = device.DeviceColor;
            light.range = 3f;
            light.intensity = 1.5f;

            switch (device.Type)
            {
                case DeviceKind.SonicDecoy: go.AddComponent<DecoyDevice>().Setup(device, throwerPosition, light); break;
                case DeviceKind.EmpPulse:   go.AddComponent<EmpDevice>().Setup(device, light); break;
            }
            return go;
        }
    }
}
