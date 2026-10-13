using UnityEngine;

namespace Nexus7.Hazards
{
    /// <summary>
    /// Rayo eléctrico procedural: un LineRenderer en zigzag que se regenera varias veces por segundo.
    /// Con jitter alto parece un arco descontrolado; con jitter bajo, un haz láser de una barrera.
    ///
    /// Colocar en: un GameObject con LineRenderer (con un material unlit/aditivo brillante).
    /// Los puntos son locales a este objeto.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class ElectricArc : MonoBehaviour
    {
        [SerializeField] private Vector3 startPoint = Vector3.zero;
        [SerializeField] private Vector3 endPoint = new Vector3(0f, 0f, 2f);
        [SerializeField, Min(2)] private int segments = 12;
        [SerializeField] private float jitter = 0.2f;
        [SerializeField] private float refreshInterval = 0.05f;
        [SerializeField] private float width = 0.05f;

        private LineRenderer line;
        private float timer;
        private bool visible = true;

        public bool Visible => visible;

        private void Awake()
        {
            line = GetComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = segments + 1;
            line.widthMultiplier = width;
            Regenerate();
        }

        private void Update()
        {
            if (!visible) return;
            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = refreshInterval;
            Regenerate();
        }

        private void Regenerate()
        {
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                Vector3 p = Vector3.Lerp(startPoint, endPoint, t);
                if (i > 0 && i < segments) p += Random.insideUnitSphere * jitter; // los extremos quedan fijos
                line.SetPosition(i, p);
            }
        }

        public void SetVisible(bool value)
        {
            if (visible == value) return;
            visible = value;
            if (line != null) line.enabled = value;
        }
    }
}
