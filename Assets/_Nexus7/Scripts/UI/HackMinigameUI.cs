using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Nexus7.Core;
using Nexus7.Gameplay;
using Nexus7.Security;

namespace Nexus7.UI
{
    /// <summary>
    /// Minijuego de hackeo: introduce la secuencia de códigos [W][A][S][D] (o flechas; mando: Y X A B)
    /// antes de que se agote el tiempo.
    ///   · Error   → −segundos, pitido (ruido 6 m) y +5 de tensión en la nave.
    ///   · Fallo   → la consola se bloquea, alarma sonora (ruido 10 m) y +18 de tensión.
    ///   · [Q]     → abortar (cuenta como fallo, pero sin tensión extra).
    ///   · INTERFERENCIA (HackInterference): la pantalla se distorsiona y los códigos no responden hasta pulsar
    ///     [Espacio] (mando: RB) a tiempo; si no, cuenta como un error. Más frecuentes y rápidas en Alerta Amarilla y Roja.
    ///   · Mientras hackeas, el tecleo hace ruido (3 m cada segundo).
    /// En Alerta Roja la secuencia es más larga y hay menos tiempo.
    ///
    /// Colocar en: el Canvas "HUD" (lo usa ConsoleUI).
    /// </summary>
    public class HackMinigameUI : MonoBehaviour
    {
        public static HackMinigameUI Instance { get; private set; }

        [SerializeField] private GameObject panel;
        [SerializeField] private TextMeshProUGUI title;
        [SerializeField] private TextMeshProUGUI sequenceText;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private Image timerFill;
        [SerializeField] private Image panelBackground;

        [Tooltip("Ruido del tecleo mientras se hackea (m) y cada cuántos segundos.")]
        [SerializeField] private float typingNoiseRadius = 3f;
        [SerializeField] private float typingNoiseInterval = 1f;

        private HackSequence sequence;
        private HackInterference interference;
        private float typingTimer;
        private float spaceCooldown;   // pulsar [Espacio] sin interferencia lo bloquea un momento (no vale machacarlo)
        private Action<bool> onDone;
        private Vector3 sourcePosition;
        private float flashTimer;
        private int startFrame;
        private readonly StringBuilder sb = new StringBuilder();

        public bool IsActive => sequence != null;

        private void Awake()
        {
            Instance = this;
            if (panel != null) panel.SetActive(false);
        }

        private void OnEnable() { PlayerRespawn.PlayerCaught += Abort; Nexus7.Player.PlayerHealth.PlayerHit += OnPlayerHit; }
        private void OnDisable() { PlayerRespawn.PlayerCaught -= Abort; Nexus7.Player.PlayerHealth.PlayerHit -= OnPlayerHit; }

        /// <summary>Un golpe interrumpe el hackeo: termina como fallido (sin penalización extra) y el dueño libera su bloqueo.</summary>
        private void OnPlayerHit(Vector3 attacker)
        {
            if (sequence == null) return;
            Finish(false, false);
        }
        private void OnDestroy() { if (Instance == this) Instance = null; }

        public void Begin(HackDifficulty difficulty, string label, Vector3 position, Action<bool> callback)
        {
            bool red = AlertSystem.Level == AlertLevel.Red;
            int seed = UnityEngine.Random.Range(0, int.MaxValue);
            sequence = new HackSequence(difficulty, seed, red ? 0.8f : 1f, red ? 1 : 0);
            interference = new HackInterference(AlertSystem.Level, difficulty, seed ^ 0x5f3759df);
            typingTimer = 0f;
            spaceCooldown = 0f;
            onDone = callback;
            sourcePosition = position;
            startFrame = Time.frameCount;
            panel.SetActive(true);
            string diff = difficulty == HackDifficulty.Easy ? "FÁCIL" : difficulty == HackDifficulty.Medium ? "MEDIA" : "DIFÍCIL";
            title.text = $"<b>HACKEO: {label.ToUpperInvariant()}</b>   <size=75%>dificultad {diff}{(red ? " · ALERTA ROJA" : "")}</size>";
            Render();
        }

        private void Update()
        {
            if (sequence == null) return;

            if (sequence.Tick(Time.deltaTime)) { Finish(false, true); return; }

            // Tecleo: ruido suave y continuo mientras dura el hackeo
            if ((typingTimer -= Time.deltaTime) <= 0f)
            {
                typingTimer = typingNoiseInterval;
                NoiseSystem.Emit(sourcePosition, typingNoiseRadius, null);
            }

            // Interferencias: si se acaba el margen sin pulsar [Espacio], cuenta como un error
            if (interference.Tick(Time.deltaTime) == HackInterference.Event.Missed)
            {
                if (sequence.Penalize() == HackSequence.StepResult.Failed) { Finish(false, true); return; }
                OnError();
            }

            spaceCooldown -= Time.deltaTime;
            if (Time.frameCount != startFrame)
            {
                if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.JoystickButton6)) { Finish(false, false); return; }

                bool spacePressed = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.JoystickButton5);
                if (spacePressed && !interference.Active) spaceCooldown = 0.6f;   // pulsado sin motivo: no responde en 0,6 s

                if (interference.Active)
                {
                    // Con la pantalla distorsionada los códigos no responden: solo sirve [Espacio] / RB
                    if (spacePressed && spaceCooldown <= 0f) interference.Respond();
                    flashTimer -= Time.deltaTime;
                    Render();
                    return;
                }

                HackSymbol? pressed = null;
                if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.JoystickButton3)) pressed = HackSymbol.Up;
                else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.JoystickButton2)) pressed = HackSymbol.Left;
                else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.JoystickButton0)) pressed = HackSymbol.Down;
                else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.JoystickButton1)) pressed = HackSymbol.Right;

                if (pressed.HasValue)
                {
                    switch (sequence.Input(pressed.Value))
                    {
                        case HackSequence.StepResult.Wrong: OnError(); break;
                        case HackSequence.StepResult.Failed: Finish(false, true); return;
                        case HackSequence.StepResult.Completed: Finish(true, false); return;
                    }
                }
            }

            flashTimer -= Time.deltaTime;
            Render();
        }

        /// <summary>Un error (código equivocado o interferencia no atendida): destello, aviso a la seguridad y pitido de 6 m.</summary>
        private void OnError()
        {
            flashTimer = 0.25f;
            AlertSystem.Report(AlertEvent.HackError, sourcePosition);
            NoiseSystem.Emit(sourcePosition, 6f, null);
        }

        private static readonly string[] GlitchGlyphs = { "#", "%", "&", "?", "¤", "§", "@", "/" };

        private void Render()
        {
            sb.Clear();
            if (interference != null && interference.Active)
            {
                // Pantalla distorsionada: símbolos basura y el aviso con el margen que queda
                for (int i = 0; i < sequence.Symbols.Length; i++)
                    sb.Append("<color=#B455FF>").Append(GlitchGlyphs[UnityEngine.Random.Range(0, GlitchGlyphs.Length)]).Append("</color>  ");
                sequenceText.text = sb.ToString();
                statusText.text = $"<color=#FF4AD2><b>¡INTERFERENCIA!</b></color>  pulsa <b>[ESPACIO]</b> (mando: RB)   " +
                                  $"<color=#FFE14A>{interference.WindowLeft:0.0} s</color>     " +
                                  $"<size=80%>Tiempo: {sequence.TimeLeft:0.0} s   Errores: {sequence.Errors}/{sequence.MaxErrors}</size>";
                if (timerFill != null)
                {
                    timerFill.rectTransform.localScale = new Vector3(Mathf.Clamp01(interference.WindowLeft / interference.Window), 1f, 1f);
                    timerFill.color = new Color(0.85f, 0.3f, 1f);
                }
                if (panelBackground != null)
                    // Parpadeo lento (1,5 ciclos/s, por debajo del límite de fotosensibilidad de 3 destellos/s)
                    panelBackground.color = (Time.unscaledTime * 1.5f) % 1f < 0.5f ? new Color(0.18f, 0.02f, 0.25f, 0.95f) : new Color(0.08f, 0.01f, 0.13f, 0.95f);
                return;
            }
            for (int i = 0; i < sequence.Symbols.Length; i++)
            {
                string k = HackSequence.Key(sequence.Symbols[i]);
                if (i < sequence.Index) sb.Append($"<color=#55FF99>{k}</color>  ");
                else if (i == sequence.Index) sb.Append($"<color=#FFE14A><b><size=130%>[{k}]</size></b></color>  ");
                else sb.Append($"<color=#5A6A78>{k}</color>  ");
            }
            sequenceText.text = sb.ToString();
            statusText.text = $"Tiempo: {sequence.TimeLeft:0.0} s     Errores: {sequence.Errors}/{sequence.MaxErrors}     " +
                              "<size=75%><color=#888888>[W][A][S][D] o flechas · [Q] abortar</color></size>";

            float t = sequence.TimeLimit > 0f ? sequence.TimeLeft / sequence.TimeLimit : 0f;
            if (timerFill != null)
            {
                timerFill.rectTransform.localScale = new Vector3(Mathf.Clamp01(t), 1f, 1f);
                timerFill.color = Color.Lerp(new Color(1f, 0.25f, 0.2f), new Color(0.3f, 0.9f, 1f), t);
            }
            if (panelBackground != null)
                panelBackground.color = flashTimer > 0f ? new Color(0.35f, 0.02f, 0.02f, 0.95f) : new Color(0.01f, 0.04f, 0.07f, 0.95f);
        }

        private void Finish(bool success, bool penalize)
        {
            if (success) RunStats.RecordHackSuccess();
            if (!success && penalize)
            {
                AlertSystem.Report(AlertEvent.HackFailed, sourcePosition);
                NoiseSystem.Emit(sourcePosition, 10f, null);
            }
            var callback = onDone;
            sequence = null;
            interference = null;
            onDone = null;
            panel.SetActive(false);
            callback?.Invoke(success);
        }

        /// <summary>Cancela el hackeo en curso sin resultado (capturas, final de partida).</summary>
        public void Abort()
        {
            if (sequence == null) return;
            sequence = null;
            interference = null;
            onDone = null;
            panel.SetActive(false);
        }
    }
}
