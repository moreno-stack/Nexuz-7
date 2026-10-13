using UnityEngine;
using Nexus7.Core;

namespace Nexus7.Player
{
    public enum MovementState { Idle, Walking, Running }

    /// <summary>
    /// Controlador de movimiento en primera persona basado en CharacterController.
    /// - Caminar (WASD / stick izquierdo), correr (Shift / L3) y agacharse (Ctrl o C / botón B).
    /// - Agacharse reduce la altura de la cápsula, la velocidad y el radio de ruido (sigilo).
    /// - Cada paso emite ruido a través de NoiseSystem para que futuros enemigos lo escuchen.
    ///
    /// Colocar en: el GameObject raíz "Player" (junto a CharacterController y PlayerLook).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Velocidades (m/s)")]
        [SerializeField] private float walkSpeed = 3.2f;
        [SerializeField] private float runSpeed = 6f;
        [SerializeField] private float crouchSpeed = 1.5f;
        [Tooltip("Aceleración en m/s² hacia la velocidad objetivo. Más alto = respuesta más brusca.")]
        [SerializeField] private float acceleration = 20f;

        [Header("Gravedad")]
        [SerializeField] private float gravity = -20f;

        [Header("Agacharse")]
        [Tooltip("Objeto hijo que contiene la cámara; baja al agacharse.")]
        [SerializeField] private Transform cameraRoot;
        [SerializeField] private float standingHeight = 1.8f;
        [SerializeField] private float crouchHeight = 1.0f;
        [SerializeField] private float standingCameraY = 1.6f;
        [SerializeField] private float crouchCameraY = 0.85f;
        [Tooltip("Metros de altura por segundo durante la transición de pie <-> agachado.")]
        [SerializeField] private float crouchTransitionSpeed = 5f;
        [Tooltip("true = pulsar para alternar; false = mantener pulsado.")]
        [SerializeField] private bool toggleCrouch = true;
        [Tooltip("Capas que impiden ponerse de pie (techos, tuberías bajas...).")]
        [SerializeField] private LayerMask ceilingMask = ~0;

        [Header("Ruido y pasos")]
        [Tooltip("Radio (m) en el que se oyen los pasos al caminar.")]
        [SerializeField] private float walkNoiseRadius = 6f;
        [SerializeField] private float runNoiseRadius = 12f;
        [SerializeField] private float crouchNoiseRadius = 1.5f;
        [SerializeField] private float walkStepInterval = 0.5f;
        [SerializeField] private float runStepInterval = 0.32f;
        [SerializeField] private float crouchStepInterval = 0.75f;
        [Tooltip("Opcional: AudioSource para los pasos.")]
        [SerializeField] private AudioSource footstepSource;
        [Tooltip("Opcional: sonidos de pasos (se elige uno al azar).")]
        [SerializeField] private AudioClip[] footstepClips;

        [Header("Controles")]
        [SerializeField] private KeyCode runKey = KeyCode.LeftShift;
        [SerializeField] private KeyCode crouchKey = KeyCode.LeftControl;
        [SerializeField] private KeyCode crouchKeyAlt = KeyCode.C;
        [Tooltip("JoystickButton8 = clic del stick izquierdo (L3) en mando Xbox.")]
        [SerializeField] private KeyCode runPadButton = KeyCode.JoystickButton8;
        [Tooltip("JoystickButton1 = botón B en mando Xbox.")]
        [SerializeField] private KeyCode crouchPadButton = KeyCode.JoystickButton1;

        // ---------- Estado público (lo leen el tutorial, la UI y futuros enemigos) ----------
        public MovementState State { get; private set; } = MovementState.Idle;
        public bool IsCrouching { get; private set; }
        /// <summary>Radio de ruido actual en metros (0 si está quieto).</summary>
        public float CurrentNoiseRadius { get; private set; }
        /// <summary>Si es false, el jugador no responde a la entrada (cinemáticas, tutorial).</summary>
        public bool InputEnabled { get; set; } = true;

        private CharacterController controller;
        private Vector3 horizontalVelocity;
        private float verticalVelocity;
        private bool wantsCrouch;
        private float stepTimer;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            controller.height = standingHeight;
            controller.center = new Vector3(0f, standingHeight * 0.5f, 0f);
        }

        private void Update()
        {
            // 1) Leer entrada. "Horizontal"/"Vertical" del Input Manager ya incluyen WASD, flechas y stick izquierdo.
            Vector2 input = Vector2.zero;
            bool runHeld = false;
            if (InputEnabled)
            {
                input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
                runHeld = Input.GetKey(runKey) || Input.GetKey(runPadButton);
                HandleCrouchInput();
            }
            input = Vector2.ClampMagnitude(input, 1f); // evita que la diagonal sea más rápida

            // Correr estando agachado = intentar ponerse de pie.
            if (runHeld && wantsCrouch && toggleCrouch) wantsCrouch = false;

            // 2) Altura de la cápsula y de la cámara.
            UpdateCrouch();

            // 3) Velocidad horizontal suavizada.
            bool running = runHeld && !IsCrouching && input.y > 0.1f; // solo se corre hacia delante
            float targetSpeed = IsCrouching ? crouchSpeed : (running ? runSpeed : walkSpeed);
            Vector3 desired = (transform.right * input.x + transform.forward * input.y) * targetSpeed;
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, desired, acceleration * Time.deltaTime);

            // 4) Gravedad.
            if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f; // mantiene pegado al suelo
            verticalVelocity += gravity * Time.deltaTime;

            controller.Move((horizontalVelocity + Vector3.up * verticalVelocity) * Time.deltaTime);

            // 5) Estado y ruido según la velocidad REAL (si choca con una pared, cuenta como quieto).
            Vector3 planar = controller.velocity;
            planar.y = 0f;
            UpdateStateAndNoise(planar.magnitude, running);
        }

        private void HandleCrouchInput()
        {
            if (toggleCrouch)
            {
                if (Input.GetKeyDown(crouchKey) || Input.GetKeyDown(crouchKeyAlt) || Input.GetKeyDown(crouchPadButton))
                    wantsCrouch = !wantsCrouch;
            }
            else
            {
                wantsCrouch = Input.GetKey(crouchKey) || Input.GetKey(crouchKeyAlt) || Input.GetKey(crouchPadButton);
            }
        }

        private void UpdateCrouch()
        {
            // Si estamos por debajo de la altura de pie y hay algo encima, seguimos agachados a la fuerza.
            bool blockedAbove = controller.height < standingHeight - 0.01f && !CanStandUp();
            IsCrouching = wantsCrouch || blockedAbove;

            float targetHeight = IsCrouching ? crouchHeight : standingHeight;
            controller.height = Mathf.MoveTowards(controller.height, targetHeight, crouchTransitionSpeed * Time.deltaTime);
            controller.center = new Vector3(0f, controller.height * 0.5f, 0f); // los pies se quedan en el suelo

            if (cameraRoot != null)
            {
                float t = Mathf.InverseLerp(crouchHeight, standingHeight, controller.height);
                Vector3 p = cameraRoot.localPosition;
                p.y = Mathf.Lerp(crouchCameraY, standingCameraY, t);
                cameraRoot.localPosition = p;
            }
        }

        /// <summary>Comprueba con un SphereCast hacia arriba si hay espacio para ponerse de pie.</summary>
        private bool CanStandUp()
        {
            float radius = controller.radius * 0.9f;
            Vector3 origin = transform.position + Vector3.up * (controller.height - radius);
            float distance = standingHeight - controller.height;
            return !Physics.SphereCast(origin, radius, Vector3.up, out _, distance, ceilingMask, QueryTriggerInteraction.Ignore);
        }

        private void UpdateStateAndNoise(float speed, bool running)
        {
            if (speed < 0.2f)
            {
                State = MovementState.Idle;
                CurrentNoiseRadius = 0f;
                stepTimer = 0f;
                return;
            }

            State = running ? MovementState.Running : MovementState.Walking;

            float interval;
            if (IsCrouching)                          { CurrentNoiseRadius = crouchNoiseRadius; interval = crouchStepInterval; }
            else if (State == MovementState.Running)  { CurrentNoiseRadius = runNoiseRadius;    interval = runStepInterval; }
            else                                      { CurrentNoiseRadius = walkNoiseRadius;   interval = walkStepInterval; }

            stepTimer -= Time.deltaTime;
            if (stepTimer <= 0f && controller.isGrounded)
            {
                stepTimer = interval;
                EmitFootstep();
            }
        }

        private void EmitFootstep()
        {
            NoiseSystem.Emit(transform.position, CurrentNoiseRadius, gameObject);

            if (footstepSource != null && footstepClips != null && footstepClips.Length > 0)
            {
                float volume = IsCrouching ? 0.15f : (State == MovementState.Running ? 1f : 0.5f);
                footstepSource.pitch = Random.Range(0.9f, 1.1f);
                footstepSource.PlayOneShot(footstepClips[Random.Range(0, footstepClips.Length)], volume);
            }
        }

        // Dibuja el radio de ruido en la vista Scene al seleccionar al jugador (útil para depurar el sigilo).
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
            float r = Application.isPlaying ? CurrentNoiseRadius : walkNoiseRadius;
            Gizmos.DrawWireSphere(transform.position, r);
        }
    }
}
