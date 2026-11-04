using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Nexus7.Player
{
    /// <summary>
    /// [V] (o R3 en el mando) alterna entre primera y tercera persona. La elección se guarda.
    ///   · 1ª persona: el brazo de cámara se apaga y el cuerpo solo proyecta sombra (se ven los antebrazos).
    ///   · 3ª persona: se ve el modelo entero. Si la cámara no cabe (conductos), pasa sola a 1ª persona.
    ///
    /// Colocar en: el GameObject raíz "Player".
    /// </summary>
    public class CameraModeSwitcher : MonoBehaviour
    {
        public const string PrefsKey = "Nexus7_Camara3P";

        [SerializeField] private ThirdPersonCamera boom;
        [Tooltip("Raíz del modelo del jugador (PlayerRig).")]
        [SerializeField] private Transform bodyRoot;
        [Tooltip("Partes visibles también en primera persona (antebrazos y manos).")]
        [SerializeField] private Renderer[] firstPersonVisible;
        [SerializeField] private KeyCode toggleKey = KeyCode.V;
        [SerializeField] private KeyCode padToggle = KeyCode.JoystickButton9; // R3 (View/Back abre el inventario)
        [SerializeField] private bool defaultThirdPerson = true;

        private PlayerController controller;
        private bool effectiveThird;

        /// <summary>Preferencia del jugador.</summary>
        public bool ThirdPerson { get; private set; }
        /// <summary>(tercera persona efectiva)</summary>
        public event Action<bool> ModeChanged;

        private void Awake()
        {
            // Escenas construidas antes de la Etapa 7 guardaron View (que también abre el inventario): pasa a R3
            if (padToggle == KeyCode.JoystickButton6) padToggle = KeyCode.JoystickButton9;
            controller = GetComponent<PlayerController>();
            ThirdPerson = PlayerPrefs.GetInt(PrefsKey, defaultThirdPerson ? 1 : 0) == 1;
            Apply(ThirdPerson, true);
        }

        private void Update()
        {
            bool canToggle = controller == null || controller.InputEnabled;
            if (canToggle && (Input.GetKeyDown(toggleKey) || Input.GetKeyDown(padToggle)))
            {
                ThirdPerson = !ThirdPerson;
                PlayerPrefs.SetInt(PrefsKey, ThirdPerson ? 1 : 0);
            }
            // Sin sitio (conductos) → 1ª persona temporal; vuelve a 3ª cuando hay al menos 0,9 m (histéresis).
            bool wanted = ThirdPerson;
            if (wanted && boom != null)
            {
                float room = boom.ProbeDistance();
                wanted = effectiveThird ? !CameraBoomMath.ShouldFallbackToFirstPerson(room) : room >= 0.9f;
            }
            Apply(wanted, false);
        }

        /// <summary>Cambia el modelo del jugador (p. ej. al ponerse el traje) y vuelve a aplicar la visibilidad.</summary>
        public void SetBody(Transform root, Renderer[] firstPersonParts)
        {
            bodyRoot = root;
            firstPersonVisible = firstPersonParts;
            Apply(effectiveThird, true);
        }

        private void Apply(bool third, bool force)
        {
            if (!force && third == effectiveThird) return;
            effectiveThird = third;
            if (boom != null)
            {
                boom.enabled = third;
                if (!third) boom.ResetToFirstPerson();
            }
            ApplyVisibility();
            ModeChanged?.Invoke(third);
        }

        /// <summary>Tercera persona efectiva (el brazo de cámara está en uso).</summary>
        public bool EffectiveThirdPerson => effectiveThird;

        private readonly System.Collections.Generic.Dictionary<Renderer, ShadowCastingMode> originalModes =
            new System.Collections.Generic.Dictionary<Renderer, ShadowCastingMode>();
        private bool lastShowFull;

        /// <summary>Visible → su modo original (los brillos siguen sin sombra); oculto → solo sombra.</summary>
        public static ShadowCastingMode ShadowModeFor(bool showFull, bool firstPersonPart, ShadowCastingMode original) =>
            showFull || firstPersonPart ? original : ShadowCastingMode.ShadowsOnly;

        private void ApplyVisibility()
        {
            if (bodyRoot == null) return;
            bool showFull = effectiveThird || Nexus7.Cinematics.CinematicDirector.IsPlaying;   // en una cinemática se ve el cuerpo entero
            lastShowFull = showFull;
            foreach (var r in bodyRoot.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null) continue;
                ShadowCastingMode original;
                if (!originalModes.TryGetValue(r, out original))
                {
                    original = r.shadowCastingMode == ShadowCastingMode.ShadowsOnly ? ShadowCastingMode.On : r.shadowCastingMode;
                    originalModes[r] = original;
                }
                r.shadowCastingMode = ShadowModeFor(showFull, IsFirstPersonPart(r), original);
            }
        }

        private void LateUpdate()
        {
            // Entrar/salir de una cinemática o cambiar el objeto en la mano: se vuelve a aplicar
            bool showFull = effectiveThird || Nexus7.Cinematics.CinematicDirector.IsPlaying;
            if (showFull != lastShowFull || HeldChanged()) ApplyVisibility();
        }

        private Transform lastHeld;
        private bool HeldChanged()
        {
            var held = bodyRoot != null ? FindHeld(bodyRoot) : null;
            if (held == lastHeld) return false;
            lastHeld = held;
            return true;
        }

        private static Transform FindHeld(Transform root) => Nexus7.Characters.CharacterRigFactory.FindDeep(root, "ObjetoEnMano");

        private bool IsFirstPersonPart(Renderer r)
        {
            // El objeto en la mano (HeldItemView, "ObjetoEnMano") también se ve en 1ª persona
            for (var t = r.transform; t != null && t != bodyRoot; t = t.parent)
                if (t.name == "ObjetoEnMano") return true;
            if (firstPersonVisible == null) return false;
            foreach (var p in firstPersonVisible) if (p == r) return true;
            return false;
        }
    }
}
