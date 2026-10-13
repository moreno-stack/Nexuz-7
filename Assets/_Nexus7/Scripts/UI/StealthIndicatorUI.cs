using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Nexus7.Enemies;
using Nexus7.Stealth;

namespace Nexus7.UI
{
    /// <summary>
    /// Indicador de sigilo en la parte superior de la pantalla:
    ///   OCULTO (gris azulado)  → agachado en la sombra
    ///   ? SOSPECHA (amarillo)  → algún dron te está viendo; la barra muestra cuánto falta para que te detecte
    ///   ! DETECTADO (rojo)     → te persiguen
    ///
    /// Colocar en: el Canvas "HUD". "Bar Fill" = Image con pivote X = 0 (se escala horizontalmente).
    /// </summary>
    public class StealthIndicatorUI : MonoBehaviour
    {
        [SerializeField] private PlayerStealth stealth;
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private Image barFill;
        [SerializeField] private GameObject barRoot;

        [SerializeField] private Color hiddenColor = new Color(0.55f, 0.65f, 0.8f);
        [SerializeField] private Color suspiciousColor = new Color(1f, 0.8f, 0.1f);
        [SerializeField] private Color detectedColor = new Color(1f, 0.15f, 0.1f);

        private void Start()
        {
            if (stealth == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) stealth = player.GetComponent<PlayerStealth>();
            }
        }

        private void Update()
        {
            float maxAwareness = 0f;
            bool chased = false;
            foreach (var eye in EnemyVision.All)          // drones y cámaras
                maxAwareness = Mathf.Max(maxAwareness, eye.Awareness);
            foreach (var enemy in EnemyPatrol.All)
                chased |= enemy.CurrentState == EnemyPatrol.EnemyState.Chase;

            string text = "";
            Color color = Color.white;
            if (chased)                                  { text = "! DETECTADO";  color = detectedColor;  maxAwareness = 1f; }
            else if (maxAwareness > 0.02f)               { text = "? SOSPECHA";   color = suspiciousColor; }
            else if (stealth != null && stealth.IsHidden) { text = "OCULTO";      color = hiddenColor; }

            if (label != null)
            {
                label.text = text;
                label.color = color;
            }

            bool showBar = maxAwareness > 0.02f;
            if (barRoot != null && barRoot.activeSelf != showBar) barRoot.SetActive(showBar);
            if (barFill != null)
            {
                barFill.rectTransform.localScale = new Vector3(maxAwareness, 1f, 1f);
                barFill.color = color;
            }
        }
    }
}
