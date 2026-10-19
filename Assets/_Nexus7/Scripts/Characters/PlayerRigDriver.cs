using UnityEngine;
using Nexus7.Player;

namespace Nexus7.Characters
{
    /// <summary>Pasa la postura del jugador (agachado, en el aire, aturdido) a su ProceduralAnimator.</summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerRigDriver : MonoBehaviour
    {
        [SerializeField] private ProceduralAnimator animator;

        private PlayerController controller;
        private PlayerHealth health;

        private void Awake()
        {
            controller = GetComponent<PlayerController>();
            health = GetComponent<PlayerHealth>();
            if (animator == null) animator = GetComponent<ProceduralAnimator>();
        }

        private void Update()
        {
            if (animator == null) return;
            animator.SetPosture(controller.IsCrouching, !controller.IsGrounded);
            animator.SetStunned(health != null && health.IsStunned);
        }
    }
}
