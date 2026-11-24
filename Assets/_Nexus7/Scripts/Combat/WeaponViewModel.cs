using System.Collections.Generic;
using UnityEngine;
using Nexus7.Player;
using Nexus7.Visual.Models;

namespace Nexus7.Combat
{
    /// <summary>
    /// Modelo del arma en primera persona, hecho por código (ModelLibrary.Weapon): cada arma tiene su forma
    /// (pistola, escopeta, rifle, cañón de riel), una franja del color del arma y los accesorios instalados
    /// a la vista. Cuelga de la cámara, abajo a la derecha, retrocede con cada disparo y se oculta en tercera persona.
    /// </summary>
    public class WeaponViewModel : MonoBehaviour
    {
        private static readonly Vector3 RestPosition = new Vector3(0.22f, -0.22f, 0.45f);

        private Transform model;
        private string shownKey;
        private Renderer[] renderers = new Renderer[0];
        private CameraModeSwitcher cameraMode;
        private float kick;

        public static WeaponViewModel Build(Transform cameraTransform, CameraModeSwitcher mode)
        {
            var root = new GameObject("ArmaEnMano");
            root.transform.SetParent(cameraTransform, false);
            root.transform.localPosition = RestPosition;
            var vm = root.AddComponent<WeaponViewModel>();
            vm.cameraMode = mode;
            return vm;
        }

        /// <summary>Enseña el arma con sus accesorios (solo se vuelve a montar si ha cambiado algo).</summary>
        public void Show(PlayerWeaponSpec spec, IReadOnlyList<WeaponMod> mods = null)
        {
            if (spec == null) { gameObject.SetActive(false); return; }
            gameObject.SetActive(true);
            var list = new List<WeaponMod>();
            if (mods != null) list.AddRange(mods);
            string key = spec.Id + "|" + string.Join(",", list);
            if (key == shownKey && model != null) return;
            shownKey = key;
            if (model != null) Destroy(model.gameObject);
            var bp = ModelLibrary.Weapon(spec.Id, new MColor(spec.ColorR, spec.ColorG, spec.ColorB), list);
            model = ModelFactory.Build(bp, transform, false);
            renderers = model.GetComponentsInChildren<Renderer>(true);
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
