using System;
using System.Collections;
using UnityEngine;
using Nexus7.Interaction;
using Nexus7.Player;
using Nexus7.UI;

namespace Nexus7.Tutorial
{
    /// <summary>
    /// Secuencia de despertar + tutorial guiado:
    /// 1. Pantalla en negro y mensajes de la IA (controles bloqueados).
    /// 2. Fundido de entrada; se devuelve el control.
    /// 3. Pasos que esperan a que el jugador REALICE la acción (moverse, correr, agacharse, interactuar).
    /// 4. Se fija el primer objetivo.
    ///
    /// Colocar en: un GameObject vacío "GameSystems" de la escena.
    /// </summary>
    public class TutorialManager : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private PlayerInteractor interactor;
        [SerializeField] private HUDManager hud;

        [Header("Opciones")]
        [Tooltip("Desactívalo para saltar la intro mientras pruebas otras cosas.")]
        [SerializeField] private bool playIntro = true;
        [SerializeField] private float fadeInDuration = 4f;

        [Header("Historia al despertar")]
        [SerializeField, TextArea(2, 4)] private string[] wakeUpMessages =
        {
            "...Reinicio de emergencia completado. Soporte vital: 34 %.",
            "IA NEXUS-7: Tripulante, has despertado del criosueño antes de lo previsto.",
            "IA NEXUS-7: Se ha producido una brecha en el Módulo C. Las puertas de seguridad se han bloqueado.",
            "IA NEXUS-7: Detecto movimiento no identificado en los niveles inferiores. Evita hacer ruido.",
            "IA NEXUS-7: Debes llegar al puente de mando. Primero, sal de este corredor."
        };

        [Header("Objetivo inicial")]
        [SerializeField] private string firstObjective = "Objetivo: Encuentra la forma de abrir la puerta del corredor C-7.";

        private bool hasInteracted;

        private void OnEnable()  { if (interactor != null) interactor.Interacted += OnPlayerInteracted; }
        private void OnDisable() { if (interactor != null) interactor.Interacted -= OnPlayerInteracted; }
        private void OnPlayerInteracted(IInteractable _) => hasInteracted = true;

        private IEnumerator Start()
        {
            if (hud == null) hud = HUDManager.Instance;
            if (hud == null || player == null)
            {
                Debug.LogError("[TutorialManager] Faltan referencias (HUD o Player).", this);
                yield break;
            }

            if (!playIntro)
            {
                hud.SetFade(0f);
                hud.SetObjective(firstObjective);
                yield break;
            }

            // --- 1. Despertar en negro ---
            player.InputEnabled = false;
            hud.SetFade(1f);
            yield return new WaitForSeconds(1f);

            if (wakeUpMessages.Length > 0) hud.QueueMessage(wakeUpMessages[0]);
            yield return new WaitForSeconds(1.5f);

            // --- 2. Abrir los ojos mientras sigue hablando la IA ---
            StartCoroutine(hud.Fade(1f, 0f, fadeInDuration));
            for (int i = 1; i < wakeUpMessages.Length; i++) hud.QueueMessage(wakeUpMessages[i]);
            yield return hud.WaitForMessages();

            var respawn = player.GetComponent<Nexus7.Gameplay.PlayerRespawn>();
            if (respawn == null || !respawn.IsRespawning) player.InputEnabled = true;

            // --- 3. Pasos del tutorial: cada uno espera a que el jugador haga la acción ---
            yield return Step("Usa [W][A][S][D] o el stick izquierdo para moverte. Mueve el ratón para mirar.",
                              () => player.State != MovementState.Idle, holdSeconds: 1.2f);

            yield return Step("Mantén [Shift] (L3 en el mando) para correr. Cuidado: correr hace MUCHO ruido.",
                              () => player.State == MovementState.Running, holdSeconds: 0.8f);

            yield return Step("Pulsa [Ctrl] o [C] (B en el mando) para agacharte. Agachado avanzas lento, pero casi en silencio.",
                              () => player.IsCrouching && player.State != MovementState.Idle, holdSeconds: 1f);

            yield return Step("Apunta a un objeto cercano y pulsa [E] (A en el mando) para interactuar. Prueba con la bitácora de la pared.",
                              () => hasInteracted, holdSeconds: 0f);

            // --- 4. Primer objetivo (si el jugador ya usó la terminal, no se pisa su objetivo "cruza la puerta") ---
            if (string.IsNullOrEmpty(hud.CurrentObjective)) hud.SetObjective(firstObjective);
        }

        /// <summary>Muestra una pista y espera a que la condición se cumpla durante holdSeconds acumulados.</summary>
        private IEnumerator Step(string hint, Func<bool> condition, float holdSeconds)
        {
            hud.SetHint(hint);
            float held = 0f;
            while (true)
            {
                if (condition())
                {
                    held += Time.deltaTime;
                    if (held >= holdSeconds) break;
                }
                yield return null;
            }
            hud.ClearHint();
            yield return new WaitForSeconds(0.6f);
        }
    }
}
