using UnityEngine;

namespace Nexus7.Combat
{
    /// <summary>
    /// Módulo de accesorio en el suelo: un rombo morado que flota y gira. Se recoge solo al pasar a menos de 1,4 m y se
    /// monta en el arma de la mano si le sirve (PlayerWeapon.GiveMod). Si ninguna arma lo admite, se queda en el suelo.
    /// Lo suelta ModRewardDirector al despejar un encuentro o destruir un élite.
    /// </summary>
    public class ModPickup : MonoBehaviour
    {
        public const float PickupRadius = 1.4f;
        public static readonly Color Purple = new Color(0.75f, 0.45f, 1f);

        public WeaponMod Mod { get; private set; }
        private Vector3 basePosition;
        private float phase;
        private Transform player;

        public static ModPickup Spawn(Vector3 position, WeaponMod mod)
        {
            if (mod == WeaponMod.None) return null;
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "ModuloArma_" + mod;
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(45f, 0f, 45f);   // de canto: se ve como un rombo
            go.transform.localScale = Vector3.one * 0.26f;
            var r = go.GetComponent<Renderer>();
            if (r != null)
            {
                r.material.color = Purple;
                r.material.EnableKeyword("_EMISSION");
                r.material.SetColor("_EmissionColor", Purple * 3f);
            }
            var lightGo = new GameObject("Brillo");
            lightGo.transform.SetParent(go.transform, false);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = Purple;
            light.range = 3f;
            light.intensity = 2f;
            var p = go.AddComponent<ModPickup>();
            p.Mod = mod;
            p.basePosition = position;
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
            transform.position = basePosition + Vector3.up * (Mathf.Sin(phase * 2f) * 0.1f);
            transform.Rotate(new Vector3(0f, 90f * Time.deltaTime, 0f), Space.World);
            if (player == null || PlayerWeapon.Instance == null) return;
            if ((player.position + Vector3.up * 0.9f - transform.position).sqrMagnitude > PickupRadius * PickupRadius) return;
            if (PlayerWeapon.Instance.GiveMod(Mod)) Destroy(gameObject);
        }
    }
}
