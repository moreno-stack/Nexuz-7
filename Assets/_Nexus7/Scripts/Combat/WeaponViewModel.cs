using UnityEngine;
using Nexus7.Player;

namespace Nexus7.Combat
{
    /// <summary>
    /// Modelo del arma en primera persona, hecho con primitivas (el proyecto no usa modelos importados):
    /// cuerpo, cañón y una franja emisiva del color del arma. Cuelga de la cámara, abajo a la derecha,
    /// retrocede con cada disparo y se oculta en tercera persona.
    /// </summary>
    public class WeaponViewModel : MonoBehaviour
    {
        private static readonly Vector3 RestPosition = new Vector3(0.22f, -0.22f, 0.45f);

        private Transform body, barrel, stripe;
        private Renderer[] renderers;
        private CameraModeSwitcher cameraMode;
        private float kick;

        public static WeaponViewModel Build(Transform cameraTransform, CameraModeSwitcher mode)
        {
            var root = new GameObject("ArmaEnMano");
            root.transform.SetParent(cameraTransform, false);
            root.transform.localPosition = RestPosition;
            var vm = root.AddComponent<WeaponViewModel>();
            vm.cameraMode = mode;
            vm.body = Part(root.transform, PrimitiveType.Cube, new Color(0.18f, 0.2f, 0.23f));
            vm.barrel = Part(root.transform, PrimitiveType.Cylinder, new Color(0.35f, 0.37f, 0.4f));
            vm.barrel.localRotation = Quaternion.Euler(90f, 0f, 0f);
            vm.stripe = Part(root.transform, PrimitiveType.Cube, Color.cyan);
            vm.renderers = root.GetComponentsInChildren<Renderer>(true);
            return vm;
        }

        private static Transform Part(Transform parent, PrimitiveType type, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
            go.transform.SetParent(parent, false);
            var r = go.GetComponent<Renderer>();
            if (r != null)
            {
                r.material.color = color;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            return go.transform;
        }

        /// <summary>Cambia la forma y el color según el arma (más largo el rifle, más grueso la escopeta...).</summary>
        public void Show(PlayerWeaponSpec spec)
        {
            if (spec == null) { gameObject.SetActive(false); return; }
            gameObject.SetActive(true);
            float length = spec.Id == PlayerWeaponCatalog.Rifle ? 0.5f : spec.Id == PlayerWeaponCatalog.Railgun ? 0.6f : spec.Id == PlayerWeaponCatalog.Shotgun ? 0.42f : 0.26f;
            float thick = spec.Id == PlayerWeaponCatalog.Shotgun ? 0.11f : spec.Id == PlayerWeaponCatalog.Railgun ? 0.1f : 0.08f;
            body.localScale = new Vector3(thick, thick * 1.3f, length);
            body.localPosition = new Vector3(0f, 0f, 0f);
            barrel.localScale = new Vector3(thick * 0.45f, length * 0.35f, thick * 0.45f);
            barrel.localPosition = new Vector3(0f, thick * 0.2f, length * 0.6f);
            stripe.localScale = new Vector3(thick * 1.05f, thick * 0.25f, length * 0.7f);
            stripe.localPosition = new Vector3(0f, thick * 0.55f, 0f);
            var r = stripe.GetComponent<Renderer>();
            if (r != null)
            {
                var c = new Color(spec.ColorR, spec.ColorG, spec.ColorB);
                r.material.color = c;
                r.material.EnableKeyword("_EMISSION");
                r.material.SetColor("_EmissionColor", c * 2f);
            }
        }

        /// <summary>Retroceso visual: el arma salta hacia atrás y arriba y vuelve en ~0,15 s.</summary>
        public void Kick(float recoilDegrees) => kick = Mathf.Min(1f, kick + 0.25f + recoilDegrees * 0.15f);

        private void LateUpdate()
        {
            bool visible = cameraMode == null || !cameraMode.EffectiveThirdPerson;
            foreach (var r in renderers) if (r != null) r.enabled = visible;
            kick = Mathf.MoveTowards(kick, 0f, Time.deltaTime * 7f);
            transform.localPosition = RestPosition + new Vector3(0f, kick * 0.02f, -kick * 0.08f);
            transform.localRotation = Quaternion.Euler(-kick * 8f, 0f, 0f);
        }
    }
}
