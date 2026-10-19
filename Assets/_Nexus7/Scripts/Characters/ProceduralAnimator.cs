using UnityEngine;

namespace Nexus7.Characters
{
    /// <summary>
    /// Animación procedural de los modelos redondeados (sin Animator ni clips): rota las articulaciones por nombre.
    ///   · caminar/correr: balanceo de piernas y brazos según la velocidad real (se mide sola)
    ///   · agachado, en el aire, aturdido
    ///   · golpe: PlayAttack(anticipación, golpe) mueve el brazo derecho (con el arma)
    ///
    /// Colocar en: el GameObject que contiene el "Modelo" (jugador o robot).
    /// </summary>
    public class ProceduralAnimator : MonoBehaviour
    {
        [SerializeField] private Transform modelRoot;
        [SerializeField] private float speedSmoothing = 10f;

        private Transform hips, spine, head, lUp, lFore, rUp, rFore, lThigh, lShin, rThigh, rShin;
        private readonly GaitModel gait = new GaitModel();
        private Vector3 lastPos;
        private float speed, crouch, air;
        private bool crouched, airborne, stunned;
        private float attackStart = -999f, attackWindup, attackStrike;
        private float hipsBaseY;

        public Transform ModelRoot => modelRoot;

        private void Awake() => Bind(modelRoot);

        /// <summary>Cambia de modelo (p. ej. al ponerse el traje).</summary>
        public void Bind(Transform root)
        {
            modelRoot = root;
            if (root == null) return;
            hips = CharacterRigFactory.FindDeep(root, RigJoints.Hips);
            spine = CharacterRigFactory.FindDeep(root, RigJoints.Spine);
            head = CharacterRigFactory.FindDeep(root, RigJoints.Head);
            lUp = CharacterRigFactory.FindDeep(root, RigJoints.LUpperArm);
            lFore = CharacterRigFactory.FindDeep(root, RigJoints.LForearm);
            rUp = CharacterRigFactory.FindDeep(root, RigJoints.RUpperArm);
            rFore = CharacterRigFactory.FindDeep(root, RigJoints.RForearm);
            lThigh = CharacterRigFactory.FindDeep(root, RigJoints.LThigh);
            lShin = CharacterRigFactory.FindDeep(root, RigJoints.LShin);
            rThigh = CharacterRigFactory.FindDeep(root, RigJoints.RThigh);
            rShin = CharacterRigFactory.FindDeep(root, RigJoints.RShin);
            if (hips != null) hipsBaseY = hips.localPosition.y;
            lastPos = transform.position;
        }

        /// <summary>Postura impuesta por el dueño (el jugador la actualiza cada frame).</summary>
        public void SetPosture(bool isCrouched, bool isAirborne) { crouched = isCrouched; airborne = isAirborne; }
        public void SetStunned(bool value) => stunned = value;
        public void PlayAttack(float windup, float strike) { attackStart = Time.time; attackWindup = windup; attackStrike = strike; }
        public void CancelAttack() => attackStart = -999f;

        private void LateUpdate()
        {
            if (hips == null) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            Vector3 delta = transform.position - lastPos; delta.y = 0f;
            lastPos = transform.position;
            speed = Mathf.Lerp(speed, delta.magnitude / dt, 1f - Mathf.Exp(-speedSmoothing * dt));
            if (speed < 0.05f) speed = 0f;
            crouch = Mathf.MoveTowards(crouch, crouched ? 1f : 0f, dt * 5f);
            air = Mathf.MoveTowards(air, airborne ? 1f : 0f, dt * 6f);

            gait.Advance(speed, dt);
            float s = Mathf.Sin(gait.Phase);
            float leg = gait.LegSwingDegrees(speed) * (1f - air);
            float arm = gait.ArmSwingDegrees(speed) * (1f - air);
            float bob = speed > 0.05f ? Mathf.Abs(Mathf.Cos(gait.Phase)) * 0.03f : 0f;

            // Piernas: balanceo + rodilla que se dobla hacia atrás; agachado y en el aire se recogen.
            float crouchThigh = -45f * crouch - 30f * air, crouchShin = 75f * crouch + 50f * air;
            Rot(lThigh, s * leg + crouchThigh); Rot(rThigh, -s * leg + crouchThigh);
            Rot(lShin, Mathf.Max(0f, -s) * leg * 0.9f + crouchShin); Rot(rShin, Mathf.Max(0f, s) * leg * 0.9f + crouchShin);

            Vector3 hp = hips.localPosition; hp.y = hipsBaseY - 0.35f * crouch + bob; hips.localPosition = hp;
            if (spine != null) spine.localRotation = Quaternion.Euler(10f * crouch + speed * 1.5f, 0f, 0f);

            // Brazos: contrarios a las piernas; el derecho lo controla el golpe si hay uno en curso.
            Rot(lUp, -s * arm, 0f, -6f - 10f * air); if (lFore != null) Rot(lFore, -15f - 20f * crouch);
            float t = Time.time - attackStart;
            float attack = AttackPose.RightArmAngle(t, attackWindup, attackStrike);
            bool attacking = attack != 0f;
            Rot(rUp, attacking ? attack : s * arm, 0f, 6f + 10f * air);
            if (rFore != null) Rot(rFore, attacking ? -20f : -15f - 20f * crouch);

            if (head != null)
                head.localRotation = stunned ? Quaternion.Euler(Mathf.Sin(Time.time * 9f) * 12f, Mathf.Sin(Time.time * 6f) * 20f, 0f)
                                             : Quaternion.Euler(-6f * crouch, 0f, 0f);
        }

        private static void Rot(Transform t, float x, float y = 0f, float z = 0f)
        {
            if (t != null) t.localRotation = Quaternion.Euler(x, y, z);
        }
    }
}
