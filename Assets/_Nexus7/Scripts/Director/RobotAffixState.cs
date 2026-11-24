using UnityEngine;
using Nexus7.Combat;
using Nexus7.Enemies;

namespace Nexus7.Director
{
    /// <summary>
    /// Rasgo de un robot en este intento: aplica sus efectos (vida, velocidad, detección, puntería) y muestra una esfera
    /// de color sobre la cabeza (dorada = blindado, verde = rápido, magenta = ojo de águila). Al despertar se pone sin rasgo,
    /// así las copias (refuerzos y oleadas) no heredan el del robot original.
    /// Lo añade RobotAffixDirector a cada robot.
    /// </summary>
    public class RobotAffixState : MonoBehaviour
    {
        public RobotAffix Affix { get; private set; }
        private GameObject marker;

        private void Awake()
        {
            var old = transform.Find("RasgoRobot");
            if (old != null) marker = old.gameObject;
            Apply(RobotAffix.None);
        }

        public void Apply(RobotAffix affix)
        {
            Affix = affix;
            var health = GetComponent<EnemyHealth>();
            if (health != null) health.SetAffixHealth(RobotAffixModel.Health(affix));
            var patrol = GetComponent<EnemyPatrol>();
            if (patrol != null) patrol.AffixSpeed = RobotAffixModel.Speed(affix);
            var vision = GetComponent<EnemyVision>();
            if (vision != null) vision.AffixDetection = RobotAffixModel.Detection(affix);
            var gun = GetComponent<EnemyGun>();
            if (gun != null) gun.AffixSpread = RobotAffixModel.Spread(affix);
            ShowMarker(affix);
        }

        public static Color ColorOf(RobotAffix a) =>
            a == RobotAffix.Armored ? new Color(1f, 0.8f, 0.2f) : a == RobotAffix.Swift ? new Color(0.35f, 1f, 0.45f)
            : a == RobotAffix.Hawkeye ? new Color(1f, 0.3f, 0.95f) : Color.white;

        private void ShowMarker(RobotAffix affix)
        {
            if (affix == RobotAffix.None) { if (marker != null) marker.SetActive(false); return; }
            if (marker == null)
            {
                marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                marker.name = "RasgoRobot";
                var col = marker.GetComponent<Collider>();
                if (col != null) Destroy(col);
                marker.transform.SetParent(transform, false);
                marker.transform.localPosition = new Vector3(0f, 2.25f, 0f);
                marker.transform.localScale = Vector3.one * 0.16f;
                var lightGo = new GameObject("Brillo");
                lightGo.transform.SetParent(marker.transform, false);
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Point;
                light.range = 2f;
                light.intensity = 1.2f;
            }
            marker.SetActive(true);
            var c = ColorOf(affix);
            var r = marker.GetComponent<Renderer>();
            if (r != null)
            {
                r.material.color = c;
                r.material.EnableKeyword("_EMISSION");
                r.material.SetColor("_EmissionColor", c * 3f);
            }
            var l = marker.GetComponentInChildren<Light>();
            if (l != null) l.color = c;
        }
    }
}
