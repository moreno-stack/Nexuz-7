using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Nexus7.Core;

namespace Nexus7.UI
{
    /// <summary>
    /// Menú principal: Nueva partida · Salir, con el récord guardado y un resumen de controles.
    /// Teclado: [Intro] nueva partida (Salir solo con el botón, para no cerrar el juego por accidente).
    ///
    /// Colocar en: el Canvas de la escena "MenuPrincipal" (necesita EventSystem).
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private TextMeshProUGUI bestScoreText;
        [Header("Dificultad")]
        [SerializeField] private Button[] difficultyButtons = new Button[3];
        [SerializeField] private TextMeshProUGUI[] difficultyLabels = new TextMeshProUGUI[3];
        [SerializeField] private TextMeshProUGUI difficultyHint;

        private void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 1f;
            if (newGameButton != null) newGameButton.onClick.AddListener(GameFlow.StartNewRun);
            if (quitButton != null) quitButton.onClick.AddListener(GameFlow.Quit);
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

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.JoystickButton0)) GameFlow.StartNewRun();
            int d = (int)DifficultySettings.Current;
            if (Input.GetKeyDown(KeyCode.LeftArrow) && d > 0) { SelectDifficulty((Difficulty)(d - 1)); RefreshDifficulty(); }
            if (Input.GetKeyDown(KeyCode.RightArrow) && d < 2) { SelectDifficulty((Difficulty)(d + 1)); RefreshDifficulty(); }
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
                string label = DifficultySettings.Label((Difficulty)i);
                difficultyLabels[i].text = sel ? $"<b>« {label} »</b>" : label;
                difficultyLabels[i].color = sel ? new Color(1f, 0.85f, 0.35f) : new Color(0.75f, 0.8f, 0.85f);
            }
            if (difficultyHint != null)
                difficultyHint.text = $"Dificultad [←][→]: daño de robots y peligros ×{DifficultySettings.DamageMultiplierOf(cur):0.0#} · puntuación ×{DifficultySettings.ScoreMultiplierOf(cur):0.0#}".Replace('.', ',');
        }
    }
}
