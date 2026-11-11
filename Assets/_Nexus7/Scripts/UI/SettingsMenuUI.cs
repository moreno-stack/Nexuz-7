using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Nexus7.Audio;
using Nexus7.Core;

namespace Nexus7.UI
{
    /// <summary>
    /// Panel de Configuración: Volumen general, Música y Efectos (0–100 %), Calidad Baja / Media / Alta
    /// y Pantalla completa. Cada cambio se aplica al momento y se guarda (GameSettings).
    /// [Esc] o VOLVER lo cierran. Con teclado o mando: ↑/↓ para elegir y ←/→ para mover un slider.
    ///
    /// Colocar en: el Canvas del menú. 'Panel' = el objeto raíz del panel (empieza oculto).
    /// </summary>
    public class SettingsMenuUI : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Slider masterSlider, musicSlider, sfxSlider;
        [SerializeField] private TextMeshProUGUI masterValue, musicValue, sfxValue;
        [SerializeField] private Button[] qualityButtons = new Button[3];
        [SerializeField] private Image[] qualityFrames = new Image[3];
        [SerializeField] private Button fullscreenButton;
        [SerializeField] private TextMeshProUGUI fullscreenLabel;
        [SerializeField] private Button backButton;
        [Tooltip("Lo que queda seleccionado al abrir (para teclado y mando).")]
        [SerializeField] private Selectable firstSelected;

        public bool IsOpen => panel != null && panel.activeSelf;
        /// <summary>Se ha cerrado el panel (el menú devuelve la selección a CONFIGURACIÓN).</summary>
        public event System.Action Closed;

        private float lastTestBeep = -1f;
        private bool refreshing;

        private void Start()
        {
            Hook(masterSlider, v => GameSettings.Master = MenuState.FromPercent(v));
            Hook(musicSlider, v => GameSettings.Music = MenuState.FromPercent(v));
            Hook(sfxSlider, v =>
            {
                GameSettings.Sfx = MenuState.FromPercent(v);
                // Pitido de prueba para oír el volumen de los efectos (como mucho cada 0,15 s)
                if (Time.unscaledTime - lastTestBeep > 0.15f) { lastTestBeep = Time.unscaledTime; UISounds.Play(UiSound.Test); }
            });
            for (int i = 0; i < qualityButtons.Length; i++)
            {
                int k = i;
                if (qualityButtons[i] != null) qualityButtons[i].onClick.AddListener(() => { GameSettings.Quality = (QualityLevel)k; Refresh(); });
            }
            if (fullscreenButton != null) fullscreenButton.onClick.AddListener(() => { GameSettings.Fullscreen = !GameSettings.Fullscreen; GameSettings.ApplyAll(); Refresh(); });
            if (backButton != null) backButton.onClick.AddListener(Close);
            if (panel != null) panel.SetActive(false);
        }

        private void Hook(Slider slider, System.Action<float> onChange)
        {
            if (slider == null) return;
            slider.minValue = 0f;
            slider.maxValue = 100f;
            slider.wholeNumbers = true;
            slider.onValueChanged.AddListener(v =>
            {
                if (refreshing) return;
                onChange(v);
                GameSettings.ApplyAll();
                RefreshLabels();
            });
        }

        public void Open()
        {
            if (panel == null) return;
            panel.SetActive(true);
            Refresh();
            if (firstSelected != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(firstSelected.gameObject);
        }

        public void Close()
        {
            if (!IsOpen) return;
            panel.SetActive(false);
            Closed?.Invoke();
        }

        private void Update()
        {
            if (IsOpen && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton1)))
            {
                UISounds.Play(UiSound.Back);
                Close();
            }
        }

        private void Refresh()
        {
            refreshing = true;   // al colocar los sliders no se vuelven a guardar los valores
            if (masterSlider != null) masterSlider.value = MenuState.ToPercent(GameSettings.Master);
            if (musicSlider != null) musicSlider.value = MenuState.ToPercent(GameSettings.Music);
            if (sfxSlider != null) sfxSlider.value = MenuState.ToPercent(GameSettings.Sfx);
            refreshing = false;
            RefreshLabels();
            for (int i = 0; i < qualityFrames.Length; i++)
                if (qualityFrames[i] != null) qualityFrames[i].color = (int)GameSettings.Quality == i ? MainMenuUI.SelectedFrame : MainMenuUI.IdleFrame;
            if (fullscreenLabel != null) fullscreenLabel.text = "<b>" + MenuState.FullscreenLabel(GameSettings.Fullscreen) + "</b>";
        }

        private void RefreshLabels()
        {
            if (masterValue != null) masterValue.text = MenuState.ToPercent(GameSettings.Master) + " %";
            if (musicValue != null) musicValue.text = MenuState.ToPercent(GameSettings.Music) + " %";
            if (sfxValue != null) sfxValue.text = MenuState.ToPercent(GameSettings.Sfx) + " %";
        }
    }
}
