using UnityEngine;

namespace Nexus7.Combat
{
    /// <summary>
    /// Paquete de munición que suelta un robot destruido: un cubo brillante que flota y gira.
    /// Se recoge solo al pasar a menos de 1,4 m y va al arma que menos munición tiene (ArsenalModel.NeediestWeapon).
    /// Si todas las armas están llenas, se queda en el suelo.
    /// </summary>
    public class AmmoPickup : MonoBehaviour
    {
        public const float PickupRadius = 1.4f;
        /// <summary>Multiplicador de munición de los paquetes (el director lo sube si el jugador va justo).</summary>
        public static float DropMultiplier = 1f;

        private float packs;
        private Vector3 basePosition;
        private float phase;
        private Transform player;

        public static AmmoPickup Spawn(Vector3 position, float packs)
        {
            if (packs <= 0f) return null;
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "PaqueteMunicion";
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
            go.transform.position = position;
            go.transform.localScale = new Vector3(0.28f, 0.18f, 0.18f);
            var r = go.GetComponent<Renderer>();
            if (r != null)
            {
                var c = new Color(0.35f, 0.9f, 1f);
                r.material.color = c;
                r.material.EnableKeyword("_EMISSION");
                r.material.SetColor("_EmissionColor", c * 2.5f);
            }
            var lightGo = new GameObject("Brillo");
            lightGo.transform.SetParent(go.transform, false);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.35f, 0.9f, 1f);
            light.range = 2.5f;
            light.intensity = 1.5f;
            var p = go.AddComponent<AmmoPickup>();
            p.packs = packs;
            p.basePosition = position;
            p.phase = Random.value * 6f;
            return p;
        }

        private void Start()
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) player = go.transform;
        }

        private void Update()
        {
            phase += Time.deltaTime;
            transform.position = basePosition + Vector3.up * (Mathf.Sin(phase * 2.5f) * 0.08f);
            transform.Rotate(0f, 120f * Time.deltaTime, 0f);
            if (player == null || PlayerWeapon.Instance == null) return;
            if ((player.position + Vector3.up * 0.9f - transform.position).sqrMagnitude > PickupRadius * PickupRadius) return;
            if (PlayerWeapon.Instance.GiveAmmoPack(packs)) Destroy(gameObject);
        }
    }
}
