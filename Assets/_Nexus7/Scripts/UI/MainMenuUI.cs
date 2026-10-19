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

        private void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 1f;
            if (newGameButton != null) newGameButton.onClick.AddListener(GameFlow.StartNewRun);
            if (quitButton != null) quitButton.onClick.AddListener(GameFlow.Quit);
            if (bestScoreText != null)
                bestScoreText.text = GameFlow.BestScore > 0 ? $"Récord: {GameFlow.BestScore} puntos (rango {GameFlow.BestGrade})" : "Sin récord todavía";
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.JoystickButton0)) GameFlow.StartNewRun();
        }
    }
}
