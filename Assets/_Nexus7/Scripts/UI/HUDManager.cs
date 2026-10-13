using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus7.UI
{
    /// <summary>
    /// Punto único de acceso a la interfaz en pantalla (singleton de escena):
    /// - Prompt de interacción ("[E] Abrir puerta")
    /// - Mensajes de historia en cola con efecto máquina de escribir (subtítulos de la IA)
    /// - Pista de tutorial persistente
    /// - Objetivo actual
    /// - Fundido a/desde negro (despertar)
    ///
    /// Colocar en: el Canvas "HUD".
    /// </summary>
    public class HUDManager : MonoBehaviour
    {
        public static HUDManager Instance { get; private set; }

        [Header("Textos (TextMeshPro)")]
        [SerializeField] private TextMeshProUGUI promptText;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private TextMeshProUGUI hintText;
        [SerializeField] private TextMeshProUGUI objectiveText;

        [Header("Fundido")]
        [SerializeField] private Image fadeImage;

        [Header("Mensajes")]
        [SerializeField] private float charactersPerSecond = 45f;
        [SerializeField] private float defaultMessageDuration = 3f;

        private readonly Queue<(string text, float duration)> messageQueue = new Queue<(string, float)>();
        private Coroutine messageRoutine;

        /// <summary>true mientras haya mensajes en pantalla o en cola.</summary>
        public bool IsShowingMessages => messageRoutine != null;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            SetText(promptText, "");
            SetText(messageText, "");
            SetText(hintText, "");
            SetText(objectiveText, "");
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ---------------- Prompt de interacción ----------------
        public void ShowPrompt(string text) => SetText(promptText, text);
        public void HidePrompt() => SetText(promptText, "");

        // ---------------- Mensajes de historia ----------------
        /// <summary>Añade un mensaje a la cola. duration = segundos que permanece tras escribirse.</summary>
        public void QueueMessage(string text, float duration = -1f)
        {
            messageQueue.Enqueue((text, duration > 0f ? duration : defaultMessageDuration));
            if (messageRoutine == null) messageRoutine = StartCoroutine(ProcessQueue());
        }

        /// <summary>Para usar en corrutinas: yield return hud.WaitForMessages();</summary>
        public IEnumerator WaitForMessages()
        {
            while (messageRoutine != null) yield return null;
        }

        private IEnumerator ProcessQueue()
        {
            while (messageQueue.Count > 0)
            {
                var (text, duration) = messageQueue.Dequeue();
                yield return Typewrite(messageText, text);
                yield return new WaitForSeconds(duration);
            }
            SetText(messageText, "");
            messageRoutine = null;
        }

        private IEnumerator Typewrite(TextMeshProUGUI target, string text)
        {
            if (target == null) yield break;
            target.text = text;
            target.maxVisibleCharacters = 0;
            target.ForceMeshUpdate();
            int total = target.textInfo.characterCount;

            float shown = 0f;
            while (shown < total)
            {
                shown += charactersPerSecond * Time.deltaTime;
                target.maxVisibleCharacters = Mathf.Min(total, Mathf.FloorToInt(shown));
                yield return null;
            }
            target.maxVisibleCharacters = 99999;
        }

        // ---------------- Pista de tutorial y objetivo ----------------
        public void SetHint(string text) => SetText(hintText, text);
        public void ClearHint() => SetText(hintText, "");
        public void SetObjective(string text) => SetText(objectiveText, text);

        // ---------------- Fundido ----------------
        public void SetFade(float alpha)
        {
            if (fadeImage == null) return;
            var c = fadeImage.color;
            c.a = alpha;
            fadeImage.color = c;
        }

        public IEnumerator Fade(float from, float to, float duration)
        {
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                // SmoothStep da una entrada/salida suave, como abrir los ojos.
                SetFade(Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / duration)));
                yield return null;
            }
            SetFade(to);
        }

        private static void SetText(TextMeshProUGUI target, string text)
        {
            if (target == null) return;
            target.text = text;
            target.maxVisibleCharacters = 99999;
        }
    }
}
