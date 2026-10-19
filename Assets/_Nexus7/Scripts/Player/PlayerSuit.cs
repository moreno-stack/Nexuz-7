using UnityEngine;
using Nexus7.Characters;
using Nexus7.Core;
using Nexus7.UI;

namespace Nexus7.Player
{
    /// <summary>
    /// Traje espacial del jugador: al ponérselo se activa el escudo y el modelo cambia (casco, visor y mochila).
    /// En la Fase 1 se pone en la taquilla (SuitPickup); en las demás fases se empieza con él ('Start With Suit').
    ///
    /// Colocar en: el GameObject raíz "Player".
    /// </summary>
    public class PlayerSuit : MonoBehaviour
    {
        [SerializeField] private bool startWithSuit = true;
        [SerializeField] private SuitShield shield;
        [Tooltip("Donde cuelga el 'Modelo' del jugador (normalmente el propio Player).")]
        [SerializeField] private Transform modelParent;
        [SerializeField] private RigMaterials materials;
        [SerializeField] private ProceduralAnimator animator;
        [SerializeField] private CameraModeSwitcher cameraSwitcher;
        [SerializeField] private HeldItemView heldItem;
        [Tooltip("El 'Modelo' actual ya es el del traje (lo marca el constructor): no hace falta reconstruirlo.")]
        [SerializeField] private bool modelHasSuit;

        public bool Wearing { get; private set; }

        /// <summary>Hay que cambiar el modelo solo si aún no lleva el traje puesto ni el modelo es ya el del traje.</summary>
        public static bool NeedsRebuild(bool wearing, bool modelHasSuit) => !wearing && !modelHasSuit;

        private void Awake() { if (shield == null) shield = GetComponent<SuitShield>(); }

        private void Start()
        {
            if (GameSession.HasSuit || startWithSuit) PutOn(false);
        }

        /// <summary>Se pone el traje: escudo activo y modelo con traje. 'announce' = mensaje de la IA.</summary>
        public void PutOn(bool announce)
        {
            GameSession.HasSuit = true;
            if (shield != null) shield.Equip();
            if (NeedsRebuild(Wearing, modelHasSuit)) RebuildModel();
            Wearing = true;
            if (announce && HUDManager.Instance != null)
                HUDManager.Instance.QueueMessage("IA NEXUS-7: Traje espacial sellado. Escudo de energía activo: absorbe los golpes y se recarga si te mantienes a salvo.", 3.5f);
        }

        private void RebuildModel()
        {
            if (modelParent == null) return;
            var old = modelParent.Find("Modelo");
            if (old != null) Destroy(old.gameObject);
            var root = CharacterRigFactory.BuildPlayer(modelParent, true, materials);
            if (animator != null) animator.Bind(root);
            if (cameraSwitcher != null) cameraSwitcher.SetBody(root, CharacterRigFactory.FirstPersonRenderers(root, CharacterRigSpec.Player(true)));
            if (heldItem != null) heldItem.HandSocket = CharacterRigFactory.FindDeep(root, RigJoints.HandSocket);
            modelHasSuit = true;
        }
    }
}
