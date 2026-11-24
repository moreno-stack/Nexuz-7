using UnityEngine;
using Nexus7.Combat;

namespace Nexus7.UI
{
    /// <summary>
    /// HUD de combate (dibujado con IMGUI para no depender de los constructores de escena):
    ///   · Abajo a la derecha: arma en mano, munición «cargador / reserva», barra de calor y avisos RECARGANDO,
    ///     SOBRECALENTADA y SIN MUNICIÓN, y la lista de armas con la tecla para cambiar.
    ///   · Centro: mira de 4 trazos que se abre con la dispersión real del arma y se cierra al apuntar.
    ///     Marca de impacto «X» 0,15 s (amarilla en la óptica, cian en el núcleo, roja si destruye) y el rótulo
    ///     ÓPTICA / NÚCLEO ×3 bajo la mira.
    ///   · Sobre los robots heridos: barra de vida durante 3 s.
    ///   · Alrededor de la mira: arco rojo que señala desde dónde te han dado (hasta 6 a la vez, se apagan en 1,2 s).
    /// Se oculta en las cinemáticas, los finales y con el juego en pausa.
    ///
    /// Colocar en: el jugador (lo añade CombatInstaller).
    /// </summary>
    public class CombatHUD : MonoBehaviour
    {
        private const float HitMarkerTime = 0.15f;
        private const float BarTime = 3f;
        private const float IndicatorTime = 1.2f;
        private const int MaxIndicators = 6;

        private static Texture2D white;
        private PlayerWeapon weapon;
        private Camera cam;
        private float hitMarkerUntil = -1f, zoneLabelUntil = -1f;
        private string zoneLabel;
        private Color hitMarkerColor = Color.white;
        private GUIStyle big, small, center, zoneStyle;
        private readonly Vector3[] hitOrigins = new Vector3[MaxIndicators];
        private readonly float[] hitTimes = { -99f, -99f, -99f, -99f, -99f, -99f };
        private int nextIndicator;

        private void OnEnable()
        {
            CombatEvents.HitLanded += OnHit;
            CombatEvents.EnemyKilled += OnKilled;
            Nexus7.Player.PlayerHealth.PlayerHit += OnPlayerHit;
        }

        private void OnDisable()
        {
            CombatEvents.HitLanded -= OnHit;
            CombatEvents.EnemyKilled -= OnKilled;
            Nexus7.Player.PlayerHealth.PlayerHit -= OnPlayerHit;
        }

        private void OnPlayerHit(Vector3 origin)
        {
            hitOrigins[nextIndicator] = origin;
            hitTimes[nextIndicator] = Time.time;
            nextIndicator = (nextIndicator + 1) % MaxIndicators;
        }

        private void Start()
        {
            weapon = GetComponent<PlayerWeapon>();
            cam = GetComponentInChildren<Camera>();
        }

        private void OnHit(DamageInfo info, bool killed)
        {
            hitMarkerUntil = Time.time + HitMarkerTime;
            var zoneColor = info.Zone == HitZone.Core ? new Color(0.3f, 0.95f, 1f) : new Color(1f, 0.85f, 0.2f);
            hitMarkerColor = killed ? new Color(1f, 0.25f, 0.2f) : info.Critical ? zoneColor : Color.white;
            if (info.Zone == HitZone.Body) return;
            zoneLabel = info.Zone == HitZone.Core ? "NÚCLEO ×3" : "ÓPTICA ×2";
            zoneLabelUntil = Time.time + 0.5f;
        }

        private void OnKilled(EnemyHealth enemy) =>
            PickupNotificationUI.Notify("Robot destruido", new Color(1f, 0.6f, 0.3f));

        private void OnGUI()
        {
            if (weapon == null || !weapon.isActiveAndEnabled || GameHud.Hidden || Time.timeScale <= 0f) return;
            var w = weapon.Arsenal?.Current;
            if (w == null) return;
            EnsureStyles();
            float k = Screen.height / 1080f;

            DrawCrosshair(k);
            DrawEnemyBars(k);
            DrawDamageIndicators(k);
            DrawWeaponPanel(w, k);
        }

        private void DrawCrosshair(float k)
        {
            float cx = Screen.width * 0.5f, cy = Screen.height * 0.5f;
            float fov = cam != null ? cam.fieldOfView : 70f;
            // Grados de dispersión → píxeles en pantalla con el campo de visión actual
            float gap = 6f * k + Screen.height * 0.5f * Mathf.Tan(weapon.CurrentSpread * Mathf.Deg2Rad) / Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
            float len = 10f * k, th = Mathf.Max(2f, 2f * k);
            var c = new Color(1f, 1f, 1f, 0.85f);
            Rect(cx - th * 0.5f, cy - gap - len, th, len, c);
            Rect(cx - th * 0.5f, cy + gap, th, len, c);
            Rect(cx - gap - len, cy - th * 0.5f, len, th, c);
            Rect(cx + gap, cy - th * 0.5f, len, th, c);

            if (Time.time < zoneLabelUntil)
            {
                var old = GUI.color;
                GUI.color = hitMarkerColor;
                GUI.Label(new Rect(cx - 100f * k, cy + 34f * k, 200f * k, 24f * k), zoneLabel, zoneStyle);
                GUI.color = old;
            }

            if (Time.time < hitMarkerUntil)
            {
                // «X» de impacto: dos diagonales hechas con cuadraditos
                for (int i = -4; i <= 4; i++)
                {
                    if (i > -2 && i < 2) continue;
                    float d = i * 3f * k;
                    Rect(cx + d - th, cy + d - th, th * 2f, th * 2f, hitMarkerColor);
                    Rect(cx + d - th, cy - d - th, th * 2f, th * 2f, hitMarkerColor);
                }
            }
        }

        private void DrawEnemyBars(float k)
        {
            if (cam == null) return;
            foreach (var e in EnemyHealth.All)
            {
                if (e == null || !e.IsAlive || Time.time - e.LastHitTime > BarTime) continue;
                Vector3 p = cam.WorldToScreenPoint(e.BarPosition);
                if (p.z <= 0f) continue;
                float w = 70f * k, h = 7f * k;
                float x = p.x - w * 0.5f, y = Screen.height - p.y - h * 0.5f;   // IMGUI tiene el origen arriba
                Rect(x - 1, y - 1, w + 2, h + 2, new Color(0f, 0f, 0f, 0.6f));
                Rect(x, y, w * e.Normalized, h, Color.Lerp(new Color(1f, 0.25f, 0.2f), new Color(0.4f, 1f, 0.5f), e.Normalized));
            }
        }

        private void DrawWeaponPanel(WeaponModel w, float k)
        {
            float pw = 330f * k, ph = 132f * k;
            float x = Screen.width - pw - 24f * k, y = Screen.height - ph - 24f * k;
            Rect(x, y, pw, ph, new Color(0.03f, 0.06f, 0.09f, 0.6f));
            var accent = new Color(w.Spec.ColorR, w.Spec.ColorG, w.Spec.ColorB);
            Rect(x, y, 4f * k, ph, accent);

            small.fontSize = Mathf.RoundToInt(17 * k);
            big.fontSize = Mathf.RoundToInt(38 * k);
            GUI.color = accent;
            GUI.Label(new Rect(x + 16 * k, y + 8 * k, pw, 24 * k), w.Spec.DisplayName.ToUpperInvariant(), small);
            GUI.color = w.Ammo == 0 ? new Color(1f, 0.4f, 0.35f) : Color.white;
            GUI.Label(new Rect(x + 16 * k, y + 30 * k, pw, 46 * k), $"{w.Ammo} / {w.Reserve}", big);

            // Barra de calor (azul → naranja → rojo) o progreso de la recarga
            float barY = y + 82 * k, barW = pw - 32 * k, barH = 8 * k;
            Rect(x + 16 * k, barY, barW, barH, new Color(1f, 1f, 1f, 0.12f));
            string status = "";
            if (w.State == WeaponState.Reloading)
            {
                Rect(x + 16 * k, barY, barW * w.ReloadProgress(Time.time), barH, new Color(0.5f, 0.9f, 1f));
                status = "RECARGANDO";
            }
            else if (w.Spec.HeatPerShot > 0f)
            {
                float h = w.Heat / WeaponModel.MaxHeat;
                var hc = h < 0.5f ? Color.Lerp(new Color(0.3f, 0.7f, 1f), new Color(1f, 0.6f, 0.2f), h * 2f) : Color.Lerp(new Color(1f, 0.6f, 0.2f), new Color(1f, 0.2f, 0.15f), (h - 0.5f) * 2f);
                Rect(x + 16 * k, barY, barW * h, barH, hc);
            }
            if (w.State == WeaponState.Overheated) status = "SOBRECALENTADA";
            else if (w.IsEmpty) status = "SIN MUNICIÓN";
            if (status.Length > 0)
            {
                GUI.color = w.State == WeaponState.Reloading ? new Color(0.6f, 0.9f, 1f) : new Color(1f, 0.4f, 0.3f);
                GUI.Label(new Rect(x + 150 * k, y + 8 * k, pw - 160 * k, 24 * k), status, small);
            }

            // Lista de armas: [Q] cambia
            GUI.color = new Color(1f, 1f, 1f, 0.75f);
            small.fontSize = Mathf.RoundToInt(13 * k);
            var list = weapon.Arsenal.Weapons;
            float cell = (pw - 32 * k) / Mathf.Max(1, list.Count);
            for (int i = 0; i < list.Count; i++)
            {
                bool current = i == weapon.Arsenal.CurrentIndex;
                GUI.color = current ? Color.white : new Color(1f, 1f, 1f, 0.45f);
                string name = list[i].Spec.DisplayName.Split(' ')[0];
                GUI.Label(new Rect(x + 16 * k + i * cell, y + 98 * k, cell, 20 * k), current ? $"> {name}" : name, small);
            }
            GUI.color = new Color(1f, 1f, 1f, 0.5f);
            GUI.Label(new Rect(x, y - 22 * k, pw, 20 * k), Nexus7.Core.GamepadAxes.AnyGamepad
                ? "[RT] disparar · [LT] apuntar · cruceta abajo recargar · izq./der. arma"
                : "[Q] cambiar · [R] recargar · clic dcho. apuntar", center);
            GUI.color = Color.white;
        }

        private void EnsureStyles()
        {
            if (white == null) { white = new Texture2D(1, 1, TextureFormat.RGBA32, false); white.SetPixel(0, 0, Color.white); white.Apply(); }
            if (big != null) return;
            big = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
            small = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
            center = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleRight, fontSize = 12 };
            zoneStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 14 };
        }

        /// <summary>Arco de 7 puntos alrededor de la mira en la dirección del atacante (arriba = delante, abajo = detrás).</summary>
        private void DrawDamageIndicators(float k)
        {
            Transform view = cam != null ? cam.transform : transform;
            Vector3 f = view.forward;
            float cx = Screen.width * 0.5f, cy = Screen.height * 0.5f, radius = 150f * k, dot = 6f * k;
            for (int i = 0; i < MaxIndicators; i++)
            {
                float age = Time.time - hitTimes[i];
                if (age >= IndicatorTime) continue;
                Vector3 to = hitOrigins[i] - transform.position;
                float angle = DamageIndicatorMath.ScreenAngle(f.x, f.z, to.x, to.z);
                var c = new Color(1f, 0.2f, 0.15f, 0.85f * (1f - age / IndicatorTime));
                for (int j = -3; j <= 3; j++)
                {
                    float a = (angle + j * 4f) * Mathf.Deg2Rad;
                    Rect(cx + Mathf.Sin(a) * radius - dot * 0.5f, cy - Mathf.Cos(a) * radius - dot * 0.5f, dot, dot, c);
                }
            }
        }

        private static void Rect(float x, float y, float w, float h, Color c)
        {
            var old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(new Rect(x, y, w, h), white);
            GUI.color = old;
        }
    }
}
