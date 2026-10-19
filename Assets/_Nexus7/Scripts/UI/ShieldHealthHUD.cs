using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Nexus7.Player;

namespace Nexus7.UI
{
    /// <summary>
    /// Barra azul del escudo del traje (encima de la barra de integridad de HealthUI) y destello azul al absorber un golpe.
    /// Sin traje la barra se oculta.
    ///
    /// Colocar en: el Canvas "HUD". 'Bar Fill' = Image con pivote X = 0.
    /// </summary>
    public class ShieldHealthHUD : MonoBehaviour
    {
        [SerializeField] private SuitShield shield;
        [SerializeField] private GameObject barRoot;
        [SerializeField] private Image barFill;
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private Image shieldFlash;

        private float flash;
        private bool wasEquipped;

        private void Start()
        {
            if (shield == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) shield = player.GetComponent<SuitShield>();
            }
            if (shield != null) shield.ShieldHit += OnHit;
        }

        private void OnDestroy() { if (shield != null) shield.ShieldHit -= OnHit; }

        private void OnHit(float amount) => flash = Mathf.Clamp01(flash + 0.3f + amount / 60f);

        private void Update()
        {
            bool on = shield != null && shield.Equipped && shield.Model != null;
            if (on && !wasEquipped) flash = 1f;          // destello azul al sellar el traje
            wasEquipped = on;
            if (barRoot != null && barRoot.activeSelf != on) barRoot.SetActive(on);
            if (on)
            {
                float k = shield.Model.Capacity > 0f ? shield.Model.Value / shield.Model.Capacity : 0f;
                if (barFill != null) barFill.rectTransform.localScale = new Vector3(k, 1f, 1f);
                if (label != null) label.text = $"ESCUDO  {Mathf.CeilToInt(shield.Model.Value)}";
            }
            flash = Mathf.MoveTowards(flash, 0f, 2.5f * Time.deltaTime);
            if (shieldFlash != null)
            {
                var c = shieldFlash.color; c.a = flash * 0.35f; shieldFlash.color = c;
                shieldFlash.enabled = c.a > 0.001f;
            }
        }
    }
}
