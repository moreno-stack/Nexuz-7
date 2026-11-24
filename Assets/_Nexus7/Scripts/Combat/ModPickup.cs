using UnityEngine;
using Nexus7.Visual.Models;

namespace Nexus7.Combat
{
    /// <summary>
    /// Módulo de accesorio en el suelo: un maletín morado con la forma del accesorio encima, que flota y gira. Se recoge solo al pasar a menos de 1,4 m y se
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
            var go = new GameObject("ModuloArma_" + mod);
            go.transform.position = position;
            // Maletín morado con la forma del accesorio encima (ModelLibrary.ModCase), un poco inclinado hacia el jugador
            var model = ModelFactory.Build(ModelLibrary.ModCase(mod).Scaled(1.3f), go.transform, true);
            model.localRotation = Quaternion.Euler(-20f, 0f, 0f);
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
