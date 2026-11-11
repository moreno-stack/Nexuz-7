using UnityEngine;
using Nexus7.UI;

namespace Nexus7.Visual
{
    /// <summary>Materiales compartidos de las partículas ambientales (se crean una vez al cargar).</summary>
    public static class FxMaterials
    {
        private static Material additive, alpha;

        /// <summary>Partícula suave: aditiva (chispas, brillo) o con transparencia normal (vapor, polvo).</summary>
        public static Material Get(bool isAdditive)
        {
            if (isAdditive && additive != null) return additive;
            if (!isAdditive && alpha != null) return alpha;
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            bool urp = shader != null;
            if (!urp) shader = Shader.Find("Sprites/Default");
            if (shader == null) return null;
            var m = new Material(shader) { name = isAdditive ? "FX_Aditivo" : "FX_Transparente", hideFlags = HideFlags.DontSave };
            var tex = ProceduralSprites.Texture(UIShapeKind.Glow);
            m.mainTexture = tex;
            if (urp)
            {
                m.SetTexture("_BaseMap", tex);
                m.SetFloat("_Surface", 1f);                      // transparente
                m.SetFloat("_Blend", isAdditive ? 2f : 0f);      // 2 = aditivo, 0 = alfa
                m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", (float)(isAdditive ? UnityEngine.Rendering.BlendMode.One : UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));
                m.SetFloat("_ZWrite", 0f);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }
            if (isAdditive) additive = m; else alpha = m;
            return m;
        }

        internal static ParticleSystem Setup(GameObject go, bool isAdditive, int maxParticles)
        {
            var ps = go.GetComponent<ParticleSystem>();
            if (ps == null) ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.maxParticles = maxParticles;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission; em.enabled = true;   // VisualUpgrade la deja apagada en el editor
            var r = go.GetComponent<ParticleSystemRenderer>();
            // Si VisualUpgrade ya le puso un material guardado (FX_Aditivo/FX_Transparente), se respeta
            if (r != null && r.sharedMaterial == null) { var mat = Get(isAdditive); if (mat != null) r.sharedMaterial = mat; }
            if (r != null) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return ps;
        }
    }

    /// <summary>
    /// Base de los efectos ambientales: se apagan cuando la cámara está lejos (VisualCull) para ahorrar rendimiento.
    /// </summary>
    public abstract class AmbientFx : MonoBehaviour
    {
        [Tooltip("Distancia a la cámara a partir de la cual el efecto se detiene.")]
        [SerializeField] protected float cullRadius = 22f;

        protected ParticleSystem ps;
        protected bool running;
        private float checkTimer;

        protected abstract void Configure();
        protected virtual void OnRunningChanged(bool run) { if (run) ps.Play(); else ps.Stop(true, ParticleSystemStopBehavior.StopEmitting); }
        protected virtual void Tick(float dt) {}

        private void Awake() => Configure();

        private void Update()
        {
            if ((checkTimer -= Time.deltaTime) <= 0f)
            {
                checkTimer = 0.5f;
                var cam = Camera.main;
                float d = cam != null ? Vector3.Distance(cam.transform.position, transform.position) : 0f;
                bool run = VisualCull.ShouldRun(d, cullRadius, running);
                if (run != running) { running = run; OnRunningChanged(run); }
            }
            if (running) Tick(Time.deltaTime);
        }
    }

    /// <summary>Chispas eléctricas intermitentes en un panel o cable dañado, con un destello de luz anaranjada.</summary>
    public class DamagedPanelSparks : AmbientFx
    {
        [SerializeField] private Vector2 interval = new Vector2(0.8f, 3.5f);
        [SerializeField] private Vector2Int burst = new Vector2Int(8, 20);
        [SerializeField] private Light flash;
        [SerializeField] private float flashIntensity = 2.5f;

        private IntermittentTimer timer;
        private float flashTimer;

        protected override void Configure()
        {
            cullRadius = 20f;
            ps = FxMaterials.Setup(gameObject, true, 60);
            var main = ps.main;
            main.loop = true;   // sigue en marcha con emisión 0: las chispas salen con Emit
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.06f);
            main.startColor = new Color(1f, 0.75f, 0.35f, 1f);
            main.gravityModifier = 1.2f;
            var em = ps.emission; em.rateOverTime = 0f;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Cone; shape.angle = 35f; shape.radius = 0.02f;
            var r = GetComponent<ParticleSystemRenderer>();
            if (r != null) { r.renderMode = ParticleSystemRenderMode.Stretch; r.velocityScale = 0.06f; r.lengthScale = 1f; }
            timer = new IntermittentTimer(interval.x, interval.y, Random.Range(0, 9973));
            if (flash != null) flash.intensity = 0f;
        }

        protected override void OnRunningChanged(bool run)
        {
            base.OnRunningChanged(run);
            if (!run && flash != null) flash.intensity = 0f;
        }

        protected override void Tick(float dt)
        {
            if (timer.Tick(dt))
            {
                ps.Emit(Random.Range(burst.x, burst.y + 1));
                flashTimer = 0.1f;
            }
            if (flash != null)
            {
                flashTimer -= dt;
                flash.intensity = flashTimer > 0f ? flashIntensity * (flashTimer / 0.1f) : 0f;
            }
        }
    }

    /// <summary>Vapor que sale a ráfagas de un conducto de ventilación (sale hacia el eje Z local).</summary>
    public class SteamVent : AmbientFx
    {
        [SerializeField] private Vector2 pause = new Vector2(2.5f, 6f);
        [SerializeField] private Vector2 puff = new Vector2(1.2f, 2.5f);
        [SerializeField] private float rate = 22f;

        private IntermittentTimer pauseTimer;
        private float puffLeft;

        protected override void Configure()
        {
            cullRadius = 25f;
            ps = FxMaterials.Setup(gameObject, false, 90);
            var main = ps.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 2.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 1.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.7f);
            main.startColor = new Color(0.85f, 0.88f, 0.9f, 0.16f);
            main.gravityModifier = -0.05f;
            var em = ps.emission; em.rateOverTime = 0f;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Cone; shape.angle = 12f; shape.radius = 0.08f;
            var size = ps.sizeOverLifetime; size.enabled = true; size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 2.2f));
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(g);
            var noise = ps.noise; noise.enabled = true; noise.strength = 0.25f; noise.frequency = 0.6f;
            pauseTimer = new IntermittentTimer(pause.x, pause.y, Random.Range(0, 7919));
        }

        protected override void Tick(float dt)
        {
            var em = ps.emission;
            if (puffLeft > 0f)
            {
                puffLeft -= dt;
                if (puffLeft <= 0f) em.rateOverTime = 0f;
            }
            else if (pauseTimer.Tick(dt))
            {
                puffLeft = Random.Range(puff.x, puff.y);
                em.rateOverTime = rate;
            }
        }
    }

    /// <summary>Motas de polvo que flotan despacio en el haz de una lámpara del techo (una caja bajo la luz).</summary>
    public class DustMotes : AmbientFx
    {
        [SerializeField] private Vector3 area = new Vector3(3f, 2.6f, 3f);
        [SerializeField] private float rate = 6f;

        protected override void Configure()
        {
            cullRadius = 18f;
            ps = FxMaterials.Setup(gameObject, true, 80);
            var main = ps.main;
            main.loop = true;
            main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(8f, 12f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.08f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.015f, 0.035f);
            main.startColor = new Color(1f, 0.95f, 0.85f, 0.35f);
            main.gravityModifier = 0f;
            var em = ps.emission; em.rateOverTime = rate;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = area;
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(1f, 0.8f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(g);
            var noise = ps.noise; noise.enabled = true; noise.strength = 0.06f; noise.frequency = 0.3f;
        }
    }
}
