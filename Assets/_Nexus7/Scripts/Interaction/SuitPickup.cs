using System.Collections;
using UnityEngine;
using Nexus7.Core;
using Nexus7.Player;
using Nexus7.UI;

namespace Nexus7.Interaction
{
    /// <summary>
    /// Taquilla con el traje espacial (Fase 1). Al usarla: fundido breve, sonido de sellado, el jugador se pone el traje
    /// (destello azul del escudo en el HUD) y la taquilla queda vacía. Si ya llevas el traje (p. ej. al reaparecer), aparece vacía.
    /// Colocar en: la taquilla (con collider). 'Player Suit' = PlayerSuit del jugador · 'Suit Visual' = el traje colgado.
    /// 'Seal Sound' (opcional) = sonido de sellado, se reproduce en un AudioSource de la taquilla.
    /// </summary>
    public class SuitPickup : Interactable
    {
        [SerializeField] private PlayerSuit playerSuit;
        [SerializeField] private GameObject suitVisual;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip sealSound;
        [SerializeField] private float fadeOut = 0.35f, fadeIn = 0.6f;

        private bool taken;
        private PlayerController lockedPlayer;

        public override string Prompt => "Ponerse el traje espacial";
        public override bool CanInteract(PlayerInteractor interactor) => !taken && base.CanInteract(interactor);

        private void Start()
        {
            if (!GameSession.HasSuit) return;
            taken = true;
            if (suitVisual != null) suitVisual.SetActive(false);
        }

        private void OnDisable() => ReleaseLock();

        protected override void OnInteract(PlayerInteractor interactor)
        {
            if (taken) return;
            if (playerSuit == null && interactor != null) playerSuit = interactor.GetComponentInParent<PlayerSuit>();
            if (playerSuit == null) return;
            taken = true;
            var hud = HUDManager.Instance;
            if (hud == null || !isActiveAndEnabled) { Equip(); return; }
            StartCoroutine(Sequence(hud));
        }

        private IEnumerator Sequence(HUDManager hud)
        {
            lockedPlayer = playerSuit.GetComponent<PlayerController>();
            if (lockedPlayer != null) lockedPlayer.AddInputLock(this);
            yield return hud.Fade(0f, 0.85f, fadeOut);
            if (audioSource != null && sealSound != null) audioSource.PlayOneShot(sealSound);
            Equip();
            yield return hud.Fade(0.85f, 0f, fadeIn);
            ReleaseLock();
        }

        private void Equip()
        {
            playerSuit.PutOn(true);
            if (suitVisual != null) suitVisual.SetActive(false);
        }

        private void ReleaseLock()
        {
            if (lockedPlayer != null) lockedPlayer.RemoveInputLock(this);
            lockedPlayer = null;
        }
    }
}
