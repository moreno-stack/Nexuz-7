using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Nexus7.Inventory;
using Nexus7.Player;
using Nexus7.UI;

namespace Nexus7.Core
{
    /// <summary>
    /// Zona de salida de nivel: al entrar el jugador, espera, funde a negro y carga la escena indicada.
    /// Si la escena no existe o no está en Build Settings, muestra un mensaje de fin de fase; si es una fase del juego
    /// (falta registrarla), explica cómo arreglarlo.
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

        /// <summary>
        /// Si devuelve un texto, la salida está sellada (encuentro en curso, EncounterSealModel): se muestra y no se sale.
        /// Lo pone EncounterDirector.
        /// </summary>
        public static System.Func<string> SealedReason;

        private bool waitingSeal;

        private void OnTriggerEnter(Collider other)
        {
            if (used || !other.CompareTag("Player")) return;
            string seal = SealedReason?.Invoke();
            if (!string.IsNullOrEmpty(seal))
            {
                waitingSeal = true;
                if (HUDManager.Instance != null) HUDManager.Instance.QueueMessage(seal, 4f);
                return;
            }
            Use(other);
        }

        /// <summary>Si estabas dentro cuando se abrió el sello, sales sin tener que volver a entrar.</summary>
        private void OnTriggerStay(Collider other)
        {
            if (used || !waitingSeal || !other.CompareTag("Player")) return;
            if (!string.IsNullOrEmpty(SealedReason?.Invoke())) return;
            Use(other);
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player")) waitingSeal = false;
        }

        private void Use(Collider other)
        {
            used = true;

            if (!string.IsNullOrEmpty(sceneName) && Application.CanStreamedLevelBeLoaded(sceneName))
                StartCoroutine(LoadRoutine(other.GetComponent<PlayerController>()));
            else if (HUDManager.Instance != null)
            {
                // Una fase del juego que no está en la lista de escenas: decirlo en vez de dar la partida por acabada
                bool missingPhase = GameFlow.IsPhaseScene(sceneName);
                HUDManager.Instance.QueueMessage(missingPhase ? SceneListModel.MissingSceneMessage(sceneName) : fallbackMessage, missingPhase ? 8f : 5f);
                if (missingPhase) Debug.LogWarning("[Nexus-7] " + SceneListModel.MissingSceneMessage(sceneName));
            }
        }

        private IEnumerator LoadRoutine(PlayerController player)
        {
            yield return new WaitForSeconds(delay);
            if (player != null) player.InputEnabled = false;
            if (HUDManager.Instance != null) yield return HUDManager.Instance.Fade(0f, 1f, fadeTime);
            // El inventario viaja a la siguiente escena.
            var inventory = player != null ? player.GetComponent<InventorySystem>() : null;
            if (inventory != null) GameSession.SaveInventory(inventory);
            SceneLoader.Load(sceneName);   // transición y autoguardado al llegar
        }
    }
}
