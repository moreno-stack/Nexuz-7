using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Nexus7.Core;

namespace Nexus7.UI
{
    /// <summary>
    /// Menú principal: Nueva partida · Continuar · Configuración · Salir, con el récord guardado y un resumen de controles.
    ///   · Continuar: gris si no hay partida guardada; debajo, la fase y cuándo se guardó (SaveSystem).
    ///   · Nueva partida con una partida guardada: pide confirmación antes de sobrescribirla.
    ///   · Configuración: abre SettingsMenuUI.
    /// La dificultad se elige en 3 tarjetas: la elegida lleva marco dorado y un poco más de tamaño.
    /// Teclado: [Intro] nueva partida · [C] continuar · [O] configuración (Salir solo con el botón, para no cerrar el juego
    /// por accidente). Mando: A nueva partida · Y continuar · Start configuración.
    ///
    /// Colocar en: el Canvas de la escena "MenuPrincipal" (necesita EventSystem).
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private TextMeshProUGUI continueDetail;
        [SerializeField] private Button settingsButton;
        [SerializeField] private SettingsMenuUI settings;
        [Header("Confirmación al sobrescribir la partida")]
        [SerializeField] private GameObject confirmPanel;
        [SerializeField] private Button confirmYes;
        [SerializeField] private Button confirmNo;
        [SerializeField] private TextMeshProUGUI bestScoreText;
        [Header("Dificultad")]
        [SerializeField] private Button[] difficultyButtons = new Button[3];
        [SerializeField] private TextMeshProUGUI[] difficultyLabels = new TextMeshProUGUI[3];
        [SerializeField] private TextMeshProUGUI difficultyHint;
        [Tooltip("Marco de cada tarjeta de dificultad (dorado en la elegida).")]
        [SerializeField] private Image[] difficultyFrames = new Image[3];
        [Tooltip("Tarjeta de cada dificultad (la elegida crece un 6 %).")]
        [SerializeField] private RectTransform[] difficultyCards = new RectTransform[3];

        public static readonly Color SelectedFrame = new Color(1f, 0.85f, 0.35f, 1f);
        public static readonly Color IdleFrame = new Color(1f, 1f, 1f, 0.08f);

        private void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 1f;
            if (newGameButton != null) newGameButton.onClick.AddListener(OnNewGame);
            if (quitButton != null) quitButton.onClick.AddListener(GameFlow.Quit);
            if (continueButton != null) continueButton.onClick.AddListener(OnContinue);
            if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
            if (confirmYes != null) confirmYes.onClick.AddListener(() => { CloseConfirm(); GameFlow.StartNewRun(); });
            if (confirmNo != null) confirmNo.onClick.AddListener(CloseConfirm);
            if (confirmPanel != null) confirmPanel.SetActive(false);
            if (settings != null) settings.Closed += () => { lastPanelChangeFrame = Time.frameCount; Select(null); };
            RefreshContinue();
            for (int i = 0; i < difficultyButtons.Length; i++)
            {
                int k = i;   // copia para el cierre
                if (difficultyButtons[i] != null) difficultyButtons[i].onClick.AddListener(() =>
                {
                    SelectDifficulty((Difficulty)k);
                    RefreshDifficulty();
                    // que [Intro] no envíe "Submit" al botón que quedó seleccionado
                    if (UnityEngine.EventSystems.EventSystem.current != null) UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
                });
            }
            RefreshDifficulty();
            if (bestScoreText != null)
                bestScoreText.text = GameFlow.BestScore > 0 ? $"Récord: {GameFlow.BestScore} puntos (rango {GameFlow.BestGrade})" : "Sin récord todavía";
        }

        private bool SettingsOpen => settings != null && settings.IsOpen;
        private int lastPanelChangeFrame = -1;   // fotograma en que se abrió o cerró un panel
        private bool ConfirmOpen => confirmPanel != null && confirmPanel.activeSelf;

        private void OnNewGame()
        {
            if (MenuState.NeedsConfirm(SaveSystem.HasSave) && confirmPanel != null)
            {
                confirmPanel.SetActive(true);
                lastPanelChangeFrame = Time.frameCount;
                StartCoroutine(SelectNextFrame(confirmNo));   // por defecto, no sobrescribir (un fotograma después: si no, el mismo [Intro] pulsaría NO)
            }
            else GameFlow.StartNewRun();
        }

        private void OnContinue()
        {
            if (MenuState.ContinueEnabled(SaveSystem.HasSave)) GameFlow.ContinueRun();
        }

        private void OpenSettings()
        {
            if (settings == null) return;
            settings.Open();
            lastPanelChangeFrame = Time.frameCount;
        }

        private void CloseConfirm()
        {
            if (confirmPanel != null) confirmPanel.SetActive(false);
            lastPanelChangeFrame = Time.frameCount;
            Select(null);
        }

        private System.Collections.IEnumerator SelectNextFrame(Selectable s)
        {
            yield return null;
            if (ConfirmOpen) Select(s);
        }

        private void RefreshContinue()
        {
            bool has = SaveSystem.HasSave;
            if (continueButton != null) continueButton.interactable = MenuState.ContinueEnabled(has);
            if (continueDetail != null) continueDetail.text = SaveSystem.Describe();
        }

        /// <summary>
        /// Selección para teclado/mando. Los botones principales no se seleccionan nunca: así [Intro] no los «pulsa»
        /// además del atajo (y no puede pulsar Salir por accidente).
        /// </summary>
        private static void Select(Selectable s)
        {
            if (UnityEngine.EventSystems.EventSystem.current != null)
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(s != null ? s.gameObject : null);
        }

        private void Update()
        {
            if (ConfirmOpen && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton1))) { CloseConfirm(); return; }
            if (!MenuState.AcceptsMainInput(SettingsOpen, ConfirmOpen, Time.frameCount, lastPanelChangeFrame)) return;
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.JoystickButton0)) { OnNewGame(); return; }
            if (Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.JoystickButton3)) { OnContinue(); return; }
            if (Input.GetKeyDown(KeyCode.O) || Input.GetKeyDown(KeyCode.JoystickButton7)) { OpenSettings(); return; }
            int d = (int)DifficultySettings.Current;
            int dir = Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.JoystickButton4) ? -1
                    : Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.JoystickButton5) ? 1 : 0;   // ←/→ o LB/RB
            if (dir != 0 && NextDifficulty((Difficulty)d, dir) != (Difficulty)d) { SelectDifficulty(NextDifficulty((Difficulty)d, dir)); RefreshDifficulty(); }
        }

        /// <summary>Dificultad anterior/siguiente sin salirse de Fácil–Difícil.</summary>
        public static Difficulty NextDifficulty(Difficulty d, int dir)
        {
            int v = (int)d + (dir > 0 ? 1 : dir < 0 ? -1 : 0);
            return (Difficulty)(v < 0 ? 0 : v > 2 ? 2 : v);
        }

        /// <summary>Título de la tarjeta: «« Normal »» en negrita si está elegida; si no, solo el nombre.</summary>
        public static string CardTitle(Difficulty d, bool selected)
        {
            string name = DifficultySettings.NameOf(d);
            return selected ? $"<b>« {name} »</b>" : name;
        }

        /// <summary>Elige la dificultad (se guarda en PlayerPrefs y se aplica a toda la partida).</summary>
        public static void SelectDifficulty(Difficulty d) => DifficultySettings.Current = d;

        private void RefreshDifficulty()
        {
            var cur = DifficultySettings.Current;
            for (int i = 0; i < difficultyLabels.Length; i++)
            {
                if (difficultyLabels[i] == null) continue;
                bool sel = (int)cur == i;
                difficultyLabels[i].text = CardTitle((Difficulty)i, sel);
                difficultyLabels[i].color = sel ? new Color(1f, 0.85f, 0.35f) : new Color(0.75f, 0.8f, 0.85f);
            }
            for (int i = 0; i < difficultyFrames.Length; i++)
                if (difficultyFrames[i] != null) difficultyFrames[i].color = (int)cur == i ? SelectedFrame : IdleFrame;
            for (int i = 0; i < difficultyCards.Length; i++)
                if (difficultyCards[i] != null) { float k = ScreenFx.CardScale((int)cur == i); difficultyCards[i].localScale = new Vector3(k, k, 1f); }
            if (difficultyHint != null)
                difficultyHint.text = $"Dificultad [←][→] o LB/RB: daño de robots y peligros ×{DifficultySettings.DamageMultiplierOf(cur):0.0#} · puntuación ×{DifficultySettings.ScoreMultiplierOf(cur):0.0#}".Replace('.', ',');
        }
    }
}
