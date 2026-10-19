using UnityEngine;

namespace Nexus7.Environment
{
    /// <summary>
    /// Humo de emergencia: configura un ParticleSystem como columna de humo suave.
    /// Genera en tiempo de ejecución una textura circular difuminada (no necesita assets) y un material
    /// con transparencia ("Sprites/Default"). Combínalo con una ShadowZone: el humo te oculta de los drones.
    ///
    /// Colocar en: un GameObject con ParticleSystem.
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public class SmokeEmitter : MonoBehaviour
    {
        [SerializeField] private Color color = new Color(0.55f, 0.55f, 0.58f, 0.35f);
        [SerializeField] private float rate = 6f;
        [SerializeField] private float size = 3f;
        [SerializeField] private float lifetime = 7f;
        [SerializeField] private float riseSpeed = 0.6f;
        [SerializeField] private float radius = 1f;

        private static Material sharedMaterial;

        private void Awake()
        {
            var ps = GetComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.startLifetime = lifetime;
            main.startSpeed = riseSpeed;
            main.startSize = size;
            main.startColor = color;
            main.maxParticles = 200;
            main.gravityModifier = -0.02f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission;
            em.rateOverTime = rate;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = radius;

            var mat = GetMaterial();
            if (mat != null) GetComponent<ParticleSystemRenderer>().sharedMaterial = mat;
            if (!ps.isPlaying) ps.Play();
        }

        private static Material GetMaterial()
        {
            if (sharedMaterial != null) return sharedMaterial;
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) return null;

            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x - n / 2f) / (n / 2f), dy = (y - n / 2f) / (n / 2f);
                    float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));   // borde muy suave
                }
            tex.Apply();
            sharedMaterial = new Material(shader) { mainTexture = tex };
            return sharedMaterial;
        }
    }
}
