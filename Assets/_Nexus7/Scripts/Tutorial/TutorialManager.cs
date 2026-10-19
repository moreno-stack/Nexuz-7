using System;
using System.Collections;
using UnityEngine;
using Nexus7.Cinematics;
using Nexus7.Core;
using Nexus7.Interaction;
using Nexus7.Player;
using Nexus7.UI;

namespace Nexus7.Tutorial
{
    /// <summary>
    /// Secuencia de despertar + tutorial guiado:
    /// 1. Intro: o bien la cinemática «Despertar» (waitForCinematic) o la intro clásica en negro con mensajes de la IA.
    /// 2. Se devuelve el control.
    /// 3. Pasos (TutorialSteps.Phase1) que esperan a que el jugador REALICE la acción: moverse, correr, agacharse,
    ///    saltar, cambiar de cámara, interactuar y ponerse el traje. Un paso ya cumplido (p. ej. el traje) se salta.
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
        [Tooltip("Espera a que termine (o se salte) la cinemática de la escena en lugar de la intro clásica.")]
        [SerializeField] private bool waitForCinematic;
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
        private bool cameraToggled;
        private CameraModeSwitcher cameraSwitcher;

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

            cameraSwitcher = player.GetComponent<CameraModeSwitcher>();

            if (waitForCinematic)
            {
                hud.SetFade(0f);
                float noCinematic = 0f;
                while (!ShouldStartSteps(CinematicDirector.IsPlaying, noCinematic))
                {
                    noCinematic = CinematicDirector.IsPlaying ? 0f : noCinematic + Time.deltaTime;
                    yield return null;
                }
            }
            else if (!playIntro)
            {
                hud.SetFade(0f);
                hud.SetObjective(firstObjective);
                yield break;
            }
            else
            {
                // --- Intro clásica: despertar en negro ---
                player.InputEnabled = false;
                hud.SetFade(1f);
                yield return new WaitForSeconds(1f);

                if (wakeUpMessages.Length > 0) hud.QueueMessage(wakeUpMessages[0]);
                yield return new WaitForSeconds(1.5f);

                StartCoroutine(hud.Fade(1f, 0f, fadeInDuration));
                for (int i = 1; i < wakeUpMessages.Length; i++) hud.QueueMessage(wakeUpMessages[i]);
                yield return hud.WaitForMessages();

                var respawn = player.GetComponent<Nexus7.Gameplay.PlayerRespawn>();
                if (respawn == null || !respawn.IsRespawning) player.InputEnabled = true;
            }

            if (string.IsNullOrEmpty(hud.CurrentObjective)) hud.SetObjective(firstObjective);

            // --- Pasos del tutorial: cada uno espera a que el jugador haga la acción ---
            bool initialThird = cameraSwitcher != null && cameraSwitcher.ThirdPerson;
            foreach (var step in TutorialSteps.Phase1)
            {
                if (step.id == "suit" && GameSession.HasSuit) continue;   // ya se lo puso: no se pide
                var id = step.id;
                yield return Step(step.hint, () =>
                {
                    if (cameraSwitcher != null && cameraSwitcher.ThirdPerson != initialThird) cameraToggled = true;
                    return IsStepDone(id, player, hasInteracted, cameraToggled);
                }, step.hold);
            }
        }

        /// <summary>La cinemática terminó (o se saltó / no había) y lleva al menos 0,2 s sin reproducirse.</summary>
        public static bool ShouldStartSteps(bool cinematicPlaying, float noCinematicFor) =>
            !cinematicPlaying && noCinematicFor >= 0.2f;

        /// <summary>Condición de cada paso del tutorial (ver TutorialSteps). Sin jugador solo se cumplen los que no lo necesitan.</summary>
        public static bool IsStepDone(string id, PlayerController p, bool interacted, bool cameraToggled)
        {
            switch (id)
            {
                case "suit":     return GameSession.HasSuit;
                case "camera":   return cameraToggled;
                case "interact": return interacted;
            }
            if (p == null) return false;
            switch (id)
            {
                case "move":   return p.State != MovementState.Idle;
                case "run":    return p.State == MovementState.Running;
                case "crouch": return p.IsCrouching && p.State != MovementState.Idle;
                case "jump":   return p.IsJumping;
                default:       return false;
            }
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
