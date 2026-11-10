using System.Collections.Generic;
using UnityEngine;
using Nexus7.Enemies;

namespace Nexus7.Stealth
{
    /// <summary>
    /// Detecta si el jugador está bajo LUZ DIRECTA: dentro del «charco de luz» de alguna lámpara o del haz de un foco
    /// encendido (PerceptionModel.IsLitBy) y sin paredes ni cajas en medio. No cuentan las luces de los robots ni de las cámaras. Se comprueba 5 veces por segundo
    /// y la lista de luces se refresca cada 3 s (hay luces que se encienden y apagan con la energía o las alertas).
    /// PlayerStealth lo usa para el +30 % de alcance de visión de los robots.
    ///
    /// Colocar en: el GameObject raíz "Player" (lo añade PlayerStealth si falta).
    /// </summary>
    public class PlayerLightExposure : MonoBehaviour
    {
        [SerializeField] private float checkInterval = 0.2f;
        [SerializeField] private float refreshInterval = 3f;
        [Tooltip("Intensidad mínima para que una luz cuente como «directa».")]
        [SerializeField] private float minIntensity = 0.5f;
        [SerializeField] private LayerMask obstacleMask = ~(1 << 2);
        [Tooltip("Segundos que se mantiene «iluminado» tras dejar de estarlo (las luces que parpadean no lo hacen saltar).")]
        [SerializeField] private float holdTime = 0.5f;

        public bool IsLit { get; private set; }

        private readonly List<Light> lights = new List<Light>();
        private CharacterController controller;
        private float checkTimer, refreshTimer, lastLitTime = -999f;

        private void Awake() => controller = GetComponent<CharacterController>();

        private void Update()
        {
            if ((refreshTimer -= Time.deltaTime) <= 0f) { refreshTimer = refreshInterval; RefreshLights(); }
            if ((checkTimer -= Time.deltaTime) > 0f) return;
            checkTimer = checkInterval;
            if (ComputeLit()) lastLitTime = Time.time;
            IsLit = Time.time - lastLitTime <= holdTime;
        }

        private void RefreshLights()
        {
            lights.Clear();
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (l.type == LightType.Directional || l.transform.IsChildOf(transform)) continue;
                // Los focos de visión, escaneo y efectos de robots y cámaras no cuentan: no es «estar bajo una lámpara»
                if (l.GetComponentInParent<EnemyVision>() != null || l.GetComponentInParent<EnemyPatrol>() != null) continue;
                lights.Add(l);
            }
        }

        private bool ComputeLit()
        {
            float height = controller != null ? controller.height : 1.8f;
            Vector3 chest = transform.position + Vector3.up * (height * 0.6f);
            foreach (var l in lights)
            {
                if (l == null || !l.isActiveAndEnabled) continue;
                Vector3 lp = l.transform.position;
                if (!PerceptionModel.IsLitBy(lp, l.range, l.intensity, l.type == LightType.Spot, l.transform.forward, l.spotAngle, chest, minIntensity))
                    continue;
                if (!Physics.Linecast(lp, chest, out RaycastHit hit, obstacleMask, QueryTriggerInteraction.Ignore)
                    || hit.transform == transform || hit.transform.IsChildOf(transform))
                    return true;
            }
            return false;
        }
    }
}
