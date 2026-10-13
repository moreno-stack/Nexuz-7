using UnityEngine;

namespace Nexus7.Devices
{
    /// <summary>
    /// Efecto visual: anillo que se expande y se desvanece (pulso sonoro, onda PEM).
    /// Se crea por código: PulseRing.Spawn(posición, radio, color, duración).
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class PulseRing : MonoBehaviour
    {
        private const int Segments = 48;
        private static Material sharedMaterial;

        private LineRenderer line;
        private float radius, duration, age;
        private Color color;

        public static void Spawn(Vector3 position, float radius, Color color, float duration = 0.6f)
        {
            if (sharedMaterial == null)
            {
                var shader = Shader.Find("Sprites/Default");
                if (shader == null) return; // sin material no dibujamos nada (no es crítico)
                sharedMaterial = new Material(shader);
            }
            var go = new GameObject("PulsoVisual");
            go.transform.position = position + Vector3.up * 0.1f;
            var lr = go.AddComponent<LineRenderer>();
            lr.sharedMaterial = sharedMaterial;
            lr.useWorldSpace = false;
            lr.loop = true;
            lr.positionCount = Segments;
            lr.widthMultiplier = 0.06f;
            var ring = go.AddComponent<PulseRing>();
            ring.line = lr;
            ring.radius = radius;
            ring.duration = duration;
            ring.color = color;
        }

        private void Update()
        {
            age += Time.deltaTime;
            float t = Mathf.Clamp01(age / duration);
            float r = Mathf.Lerp(0.2f, radius, t);
            for (int i = 0; i < Segments; i++)
            {
                float a = i * Mathf.PI * 2f / Segments;
                line.SetPosition(i, new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r));
            }
            var c = color;
            c.a = 1f - t;
            line.startColor = line.endColor = c;
            if (t >= 1f) Destroy(gameObject);
        }
    }
}
