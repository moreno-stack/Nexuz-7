using System.Collections.Generic;
using UnityEngine;
using Nexus7.Combat;
using Nexus7.Enemies;
using Nexus7.Gameplay;
using Nexus7.Player;
using Nexus7.Stealth;

namespace Nexus7.Director
{
    /// <summary>
    /// Análisis de la caída (DeathCauseModel): apunta quién te ve (y cómo ibas en ese momento) y el daño que recibes; al
    /// caer arma el análisis y, al reaparecer, lo enseña unos segundos en una tarjeta: quién te vio, desde dónde, qué
    /// pesaba (luz, correr, ruido, varios robots) y un consejo. La última caída se ve también en el panel F3.
    /// Se añade solo al jugador (CombatInstaller.PlayerInstalled).
    /// </summary>
    public class DeathAnalyzer : MonoBehaviour
    {
        public static DeathAnalyzer Instance { get; private set; }
        public DeathAnalysis Last { get; private set; }

        private struct Spot { public EnemyPatrol enemy; public float time; }
        private struct Hit { public float time, amount; }

        private readonly List<Spot> spots = new List<Spot>();
        private readonly List<Hit> hits = new List<Hit>();
        private DeathFacts atSpot;
        private PlayerController controller;
        private PlayerStealth stealth;
        private PlayerHealth health;
        private float showUntil = -1f;
        private GUIStyle style, titleStyle;
        private static Texture2D white;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            CombatInstaller.PlayerInstalled -= Attach;
            CombatInstaller.PlayerInstalled += Attach;
        }

        private static void Attach(GameObject player)
        {
            if (player.GetComponent<DeathAnalyzer>() == null) player.AddComponent<DeathAnalyzer>();
        }

        private void Awake()
        {
            Instance = this;
            controller = GetComponent<PlayerController>();
            stealth = GetComponent<PlayerStealth>();
            health = GetComponent<PlayerHealth>();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnEnable()
        {
            EnemyPatrol.PlayerSpotted += OnSpotted;
            PlayerRespawn.PlayerCaught += OnCaught;
            PlayerRespawn.PlayerRespawned += OnRespawned;
            if (health != null) health.Damaged += OnDamaged;
        }

        private void OnDisable()
        {
            EnemyPatrol.PlayerSpotted -= OnSpotted;
            PlayerRespawn.PlayerCaught -= OnCaught;
            PlayerRespawn.PlayerRespawned -= OnRespawned;
            if (health != null) health.Damaged -= OnDamaged;
        }

        private void OnSpotted(EnemyPatrol enemy)
        {
            if (enemy == null) return;
            spots.Add(new Spot { enemy = enemy, time = Time.time });
            atSpot = State();
            atSpot.Spotter = Describe(enemy);
            atSpot.SpotDistance = Vector3.Distance(enemy.transform.position, transform.position);
        }

        private void OnDamaged(float amount)
        {
            if (amount > 0f) hits.Add(new Hit { time = Time.time, amount = amount });
        }

        private void OnCaught()
        {
            float now = Time.time, since = now - DeathCauseModel.Memory;
            spots.RemoveAll(s => s.time < since || s.enemy == null);
            hits.RemoveAll(h => h.time < since);

            bool dead = health != null && health.Current <= 0f;
            var kind = !dead ? DeathKind.Caught : health.LastDamageFromEnemy ? DeathKind.KnockedOut : DeathKind.Hazard;
            DeathFacts f;
            if (spots.Count > 0)
            {
                f = atSpot;
                f.Spotted = true;
                f.SecondsSinceSpot = now - spots[spots.Count - 1].time;
                var distinct = new HashSet<EnemyPatrol>();
                foreach (var s in spots) distinct.Add(s.enemy);
                f.Spotters = distinct.Count;
            }
            else f = State();
            f.Kind = kind;
            foreach (var h in hits) f.DamageTaken += h.amount;
            Last = DeathCauseModel.Analyze(f);
        }

        private void OnRespawned()
        {
            spots.Clear();
            hits.Clear();
            if (Last != null) showUntil = Time.time + DeathCauseModel.CardTime;
        }

        /// <summary>Cómo va el jugador ahora mismo (luz, sombra, agachado, corriendo, ruido).</summary>
        private DeathFacts State()
        {
            var f = new DeathFacts();
            if (stealth != null) { f.Lit = stealth.IsLit; f.Shadow = stealth.InShadow; }
            if (controller != null)
            {
                f.Crouching = controller.IsCrouching;
                f.Running = controller.State == MovementState.Running;
                f.Noise = controller.CurrentNoiseRadius;
            }
            return f;
        }

        private static string Describe(EnemyPatrol e)
        {
            if (e.GetComponent<EnemyGun>() != null) return "un robot tirador";
            if (e.GetComponent<EnemyMeleeAttack>() != null) return "un robot armado";
            return "un dron de seguridad";
        }

        /// <summary>Línea para el panel F3.</summary>
        public static string Summary() => DeathCauseModel.Summary(Instance != null ? Instance.Last : null);

        private void OnGUI()
        {
            if (Last == null || Time.time > showUntil) return;
            if (white == null) { white = new Texture2D(1, 1, TextureFormat.RGBA32, false); white.SetPixel(0, 0, Color.white); white.Apply(); }
            if (style == null)
            {
                style = new GUIStyle(GUI.skin.label) { fontSize = 15, richText = true, wordWrap = true };
                titleStyle = new GUIStyle(style) { fontSize = 18, fontStyle = FontStyle.Bold };
            }
            float fade = Mathf.Clamp01((showUntil - Time.time) / 1f);
            float w = Mathf.Min(560f, Screen.width - 32f), x = (Screen.width - w) * 0.5f, inner = w - 28f;
            string factors = Last.Factors.Count > 0 ? "Lo que pesó: " + string.Join(" · ", Last.Factors) + "." : "";
            string tip = $"<color=#7cf><b>Consejo:</b></color> {Last.TipText}";
            float h = 16f + 26f + style.CalcHeight(new GUIContent(Last.Seen), inner)
                      + (factors.Length > 0 ? style.CalcHeight(new GUIContent(factors), inner) + 4f : 0f)
                      + style.CalcHeight(new GUIContent(tip), inner) + 8f;
            float y = Screen.height * 0.62f;

            GUI.color = new Color(0f, 0f, 0f, 0.75f * fade);
            GUI.DrawTexture(new Rect(x, y, w, h), white);
            GUI.color = new Color(1f, 0.45f, 0.35f, fade);
            GUI.DrawTexture(new Rect(x, y, 4f, h), white);
            GUI.color = new Color(1f, 1f, 1f, fade);
            float cy = y + 8f;
            GUI.Label(new Rect(x + 14f, cy, inner, 26f), $"ANÁLISIS DE LA IA · {Last.Title}", titleStyle);
            cy += 26f;
            cy = Block(x + 14f, cy, inner, Last.Seen);
            if (factors.Length > 0) cy = Block(x + 14f, cy, inner, $"<color=#fc9>{factors}</color>") + 4f;
            Block(x + 14f, cy, inner, tip);
            GUI.color = Color.white;
        }

        private float Block(float x, float y, float w, string text)
        {
            float h = style.CalcHeight(new GUIContent(text), w);
            GUI.Label(new Rect(x, y, w, h), text, style);
            return y + h;
        }
    }
}
