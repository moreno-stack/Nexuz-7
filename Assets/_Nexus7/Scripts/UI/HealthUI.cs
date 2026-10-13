using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Nexus7.Player;

namespace Nexus7.UI
{
    /// <summary>
    /// Barra de integridad (abajo a la izquierda), destello rojo a pantalla completa al recibir daño
    /// y aviso "ATURDIDO" mientras el control está bloqueado por una descarga.
    ///
    /// Colocar en: el Canvas "HUD". "Bar Fill" = Image con pivote X = 0 (se escala en horizontal).
    /// </summary>
    public class HealthUI : MonoBehaviour
    {
        [SerializeField] private PlayerHealth health;
        [SerializeField] private Image barFill;
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private Image damageFlash;
        [SerializeField] private TextMeshProUGUI stunLabel;
        [SerializeField] private float flashFadeSpeed = 2.5f;
        [SerializeField] private Color healthyColor = new Color(0.3f, 0.9f, 1f);
        [SerializeField] private Color criticalColor = new Color(1f, 0.2f, 0.15f);

        private float flashAlpha;
        private float shownValue = 1f;

        private void Start()
        {
            if (health == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) health = player.GetComponent<PlayerHealth>();
            }
            if (health != null) health.Damaged += OnDamaged;
            SetFlash(0f);
        }

        private void OnDestroy()
        {
            if (health != null) health.Damaged -= OnDamaged;
        }

        private void OnDamaged(float amount) => flashAlpha = Mathf.Clamp01(flashAlpha + 0.25f + amount / 100f);

        private void Update()
        {
            if (health == null) return;

            shownValue = Mathf.MoveTowards(shownValue, health.Normalized, Time.deltaTime);
            Color c = Color.Lerp(criticalColor, healthyColor, Mathf.InverseLerp(0.25f, 0.6f, shownValue));
            if (barFill != null)
            {
                barFill.rectTransform.localScale = new Vector3(shownValue, 1f, 1f);
                barFill.color = c;
            }
            if (label != null) label.text = $"INTEGRIDAD  {Mathf.CeilToInt(health.Current)}";

            flashAlpha = Mathf.MoveTowards(flashAlpha, 0f, flashFadeSpeed * Time.deltaTime);
            SetFlash(flashAlpha * 0.45f);

            if (stunLabel != null)
            {
                stunLabel.text = health.IsStunned ? "¡ATURDIDO!" : "";
                stunLabel.color = new Color(0.5f, 0.85f, 1f, 0.6f + 0.4f * Mathf.PingPong(Time.time * 4f, 1f));
            }
        }

        private void SetFlash(float alpha)
        {
            if (damageFlash == null) return;
            var c = damageFlash.color;
            c.a = alpha;
            damageFlash.color = c;
        }
    }
}
