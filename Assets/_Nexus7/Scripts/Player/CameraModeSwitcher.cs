using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Nexus7.Player
{
    /// <summary>
    /// [V] (o Select en el mando) alterna entre primera y tercera persona. La elección se guarda.
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
        [SerializeField] private KeyCode padToggle = KeyCode.JoystickButton6; // Select / View
        [SerializeField] private bool defaultThirdPerson = true;

        private PlayerController controller;
        private bool effectiveThird;

        /// <summary>Preferencia del jugador.</summary>
        public bool ThirdPerson { get; private set; }
        /// <summary>(tercera persona efectiva)</summary>
        public event Action<bool> ModeChanged;

        private void Awake()
        {
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
            if (bodyRoot != null)
                foreach (var r in bodyRoot.GetComponentsInChildren<Renderer>(true))
                    r.shadowCastingMode = third || IsFirstPersonPart(r) ? ShadowCastingMode.On : ShadowCastingMode.ShadowsOnly;
            ModeChanged?.Invoke(third);
        }

        private bool IsFirstPersonPart(Renderer r)
        {
            if (firstPersonVisible == null) return false;
            foreach (var p in firstPersonVisible) if (p == r) return true;
            return false;
        }
    }
}
