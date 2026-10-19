using UnityEngine;
using Nexus7.Core;

namespace Nexus7.Environment
{
    /// <summary>
    /// Holograma central de la nave: núcleo emisivo flotante + anillos (LineRenderer) que giran.
    /// Color y velocidad según la alerta (azul tranquilo → ámbar → rojo frenético).
    ///
    /// Colocar en: la raíz del holograma. "Rings" = transforms hijos con LineRenderer circular.
    /// </summary>
    public class HologramDisplay : MonoBehaviour
    {
        [SerializeField] private Transform core;
        [SerializeField] private Renderer coreRenderer;
        [SerializeField] private Transform[] rings;
        [SerializeField] private LineRenderer[] ringLines;
        [SerializeField] private Light glow;
        [SerializeField] private Color normalColor = new Color(0.3f, 0.8f, 1f);
        [SerializeField] private Color yellowColor = new Color(1f, 0.7f, 0.2f);
        [SerializeField] private Color redColor = new Color(1f, 0.1f, 0.06f);
        [SerializeField] private float baseSpeed = 25f;

        private MaterialPropertyBlock block;
        private Color current;
        private Vector3 coreBase;
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        private void Awake()
        {
            block = new MaterialPropertyBlock();
            current = normalColor;
            if (core != null) coreBase = core.localPosition;
        }

        private void Update()
        {
            var level = AlertSystem.Level;
            Color goal = level == AlertLevel.Red ? redColor : level == AlertLevel.Yellow ? yellowColor : normalColor;
            float speedMult = level == AlertLevel.Red ? 3.5f : level == AlertLevel.Yellow ? 1.8f : 1f;
            current = Color.Lerp(current, goal, 2f * Time.deltaTime);

            if (rings != null)
                for (int i = 0; i < rings.Length; i++)
                    if (rings[i] != null)
                        rings[i].Rotate((i % 2 == 0 ? Vector3.up : new Vector3(0.3f, 1f, 0.2f)), baseSpeed * speedMult * (i + 1) * Time.deltaTime, Space.Self);

            if (ringLines != null)
                foreach (var l in ringLines) if (l != null) l.startColor = l.endColor = current;

            if (core != null)
            {
                core.localPosition = coreBase + Vector3.up * (Mathf.Sin(Time.time * 1.5f) * 0.15f);
                core.Rotate(Vector3.up, 40f * speedMult * Time.deltaTime, Space.World);
            }
            if (coreRenderer != null)
            {
                coreRenderer.GetPropertyBlock(block);
                block.SetColor(EmissionId, current * (2.5f + Mathf.Sin(Time.time * 3f * speedMult)));
                coreRenderer.SetPropertyBlock(block);
            }
            if (glow != null) glow.color = current;
        }
    }
}
