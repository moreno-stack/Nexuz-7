using System.Collections;
using UnityEngine;
using Nexus7.Cinematics;
using Nexus7.Player;
using Nexus7.UI;

namespace Nexus7.Tutorial
{
    /// <summary>
    /// Introducción genérica de nivel: fundido desde negro, mensajes de la IA, una pista temporal
    /// y el objetivo inicial. Reutilizable en cualquier fase.
    ///
    /// Colocar en: el GameObject "GameSystems" de la escena.
    /// </summary>
    public class LevelIntro : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private HUDManager hud;
        [SerializeField] private float fadeInDuration = 2.5f;
        [Tooltip("Bloquear el control mientras habla la IA.")]
        [SerializeField] private bool lockInputDuringMessages = false;
        [Tooltip("Espera a que termine (o se salte) la cinemática de la escena; sin fundido propio.")]
        [SerializeField] private bool waitForCinematic;

        [SerializeField, TextArea(2, 4)] private string[] messages;
        [SerializeField] private string hint;
        [SerializeField] private float hintDuration = 6f;
        [SerializeField] private string objective;

        /// <summary>¿Se pueden mostrar ya los mensajes? Sin espera, siempre; con espera, cuando la cinemática terminó o se saltó.</summary>
        public static bool ReadyToShow(bool waitForCinematic, bool cinematicPlaying, float noCinematicFor) =>
            !waitForCinematic || TutorialManager.ShouldStartSteps(cinematicPlaying, noCinematicFor);

        private IEnumerator Start()
        {
            if (hud == null) hud = HUDManager.Instance;
            if (hud == null) yield break;

            if (waitForCinematic)
            {
                hud.SetFade(0f);
                float noCinematic = 0f;
                while (!ReadyToShow(true, CinematicDirector.IsPlaying, noCinematic))
                {
                    noCinematic = CinematicDirector.IsPlaying ? 0f : noCinematic + Time.deltaTime;
                    yield return null;
                }
            }

            if (player != null && lockInputDuringMessages) player.InputEnabled = false;

            if (!waitForCinematic)
            {
                hud.SetFade(1f);
                StartCoroutine(hud.Fade(1f, 0f, fadeInDuration));
            }
            if (messages != null) foreach (var m in messages) hud.QueueMessage(m);
            if (!string.IsNullOrEmpty(objective)) hud.SetObjective(objective);

            yield return hud.WaitForMessages();
            if (player != null && lockInputDuringMessages)
            {
                var respawn = player.GetComponent<Nexus7.Gameplay.PlayerRespawn>();
                if (respawn == null || !respawn.IsRespawning) player.InputEnabled = true;   // no devolver el control a mitad de una reaparición
            }

            if (!string.IsNullOrEmpty(hint))
            {
                hud.SetHint(hint);
                yield return new WaitForSeconds(hintDuration);
                hud.ClearHint();
            }
        }
    }
}
