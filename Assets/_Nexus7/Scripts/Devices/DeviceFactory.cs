using UnityEngine;
using Nexus7.Visual.Models;

namespace Nexus7.Devices
{
    /// <summary>
    /// Crea en tiempo de ejecución el objeto físico de un dispositivo lanzado (modelo + esfera de choque + Rigidbody +
    /// luz + comportamiento). Así no hace falta mantener prefabs: el aspecto sale de ModelLibrary.Device y del color
    /// del DeviceItemData.
    /// </summary>
    public static class DeviceFactory
    {
        /// <summary>Radio de la esfera de choque del dispositivo lanzado (la esfera de antes medía 0,22 m).</summary>
        public const float ThrownRadius = 0.11f;

        public static GameObject SpawnThrown(DeviceItemData device, Vector3 position, Vector3 velocity,
                                             Vector3 throwerPosition, Collider ignoreCollider)
        {
            var go = new GameObject("Dispositivo_" + device.DisplayName);
            go.transform.position = position;
            // Modelo del dispositivo (señuelo con antena, PEM con anillos...) y una esfera de choque en la raíz
            ModelFactory.Build(ModelLibrary.Device(device.Type, ModelFactory.ToModel(device.DeviceColor)), go.transform, true);
            var col = go.AddComponent<SphereCollider>();
            col.radius = ThrownRadius;
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
