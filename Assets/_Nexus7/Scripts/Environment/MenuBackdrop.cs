using UnityEngine;
using Nexus7.UI;

namespace Nexus7.Environment
{
    /// <summary>Vaivén suave de la cámara del menú principal (ScreenFx.MenuCameraOffset / MenuCameraYaw).</summary>
    public class MenuCameraDrift : MonoBehaviour
    {
        private Vector3 basePos;
        private Quaternion baseRot;
        private float start;

        private void Start() { basePos = transform.position; baseRot = transform.rotation; start = Time.unscaledTime; }

        private void LateUpdate()
        {
            float t = Time.unscaledTime - start;
            transform.position = basePos + baseRot * ScreenFx.MenuCameraOffset(t);
            transform.rotation = Quaternion.Euler(0f, ScreenFx.MenuCameraYaw(t), 0f) * baseRot;
        }
    }

    /// <summary>Luz de navegación de la nave del menú: se enciende 0,2 s de cada segundo (con desfase).</summary>
    public class NavBlinkLight : MonoBehaviour
    {
        [SerializeField] private float phase = 0f;
        [SerializeField] private Renderer bulb;
        [SerializeField] private Light glow;

        private void Update()
        {
            bool on = ScreenFx.NavBlink(Time.unscaledTime + phase) > 0.5f;
            if (bulb != null && bulb.enabled != on) bulb.enabled = on;
            if (glow != null && glow.enabled != on) glow.enabled = on;
        }
    }
}
