using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Nexus7.Player;
using Nexus7.UI;

namespace Nexus7.Core
{
    /// <summary>
    /// Zona de salida de nivel: al entrar el jugador, espera, funde a negro y carga la escena indicada.
    /// Si la escena no existe o no está en Build Settings, muestra un mensaje de fin de fase.
    ///
    /// Colocar en: GameObject vacío con BoxCollider "Is Trigger" (capa "Ignore Raycast").
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class SceneExitTrigger : MonoBehaviour
    {
        [Tooltip("Nombre exacto de la escena (sin .unity). Debe estar en File > Build Settings.")]
        [SerializeField] private string sceneName;
        [Tooltip("Segundos antes del fundido (para que se lean los mensajes).")]
        [SerializeField] private float delay = 2.5f;
        [SerializeField] private float fadeTime = 1.2f;
        [SerializeField] private string fallbackMessage = "— FIN DE LA FASE —";

        private bool used;

        private void Reset() => GetComponent<Collider>().isTrigger = true;

        private void OnTriggerEnter(Collider other)
        {
            if (used || !other.CompareTag("Player")) return;
            used = true;

            if (!string.IsNullOrEmpty(sceneName) && Application.CanStreamedLevelBeLoaded(sceneName))
                StartCoroutine(LoadRoutine(other.GetComponent<PlayerController>()));
            else if (HUDManager.Instance != null)
                HUDManager.Instance.QueueMessage(fallbackMessage, 5f);
        }

        private IEnumerator LoadRoutine(PlayerController player)
        {
            yield return new WaitForSeconds(delay);
            if (player != null) player.InputEnabled = false;
            if (HUDManager.Instance != null) yield return HUDManager.Instance.Fade(0f, 1f, fadeTime);
            SceneManager.LoadScene(sceneName);
        }
    }
}
