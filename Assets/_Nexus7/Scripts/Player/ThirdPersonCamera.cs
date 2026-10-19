using UnityEngine;

namespace Nexus7.Player
{
    /// <summary>
    /// Brazo de cámara en tercera persona: coloca su transform (el "CameraBoom", padre de la cámara) detrás y a la
    /// derecha del pivote (CameraRoot), sin atravesar paredes (SphereCast). Si el hueco es menor de 0,6 m
    /// (conductos), avisa para pasar temporalmente a primera persona.
    ///
    /// Colocar en: Player/CameraRoot/CameraBoom (la cámara es hija del boom; CameraShake sigue en la cámara).
    /// </summary>
    public class ThirdPersonCamera : MonoBehaviour
    {
        [SerializeField] private Transform pivot;
        [SerializeField] private float distance = 3.2f;
        [SerializeField] private float shoulderOffset = 0.5f;
        [SerializeField] private float height = 0.15f;
        [SerializeField] private float probeRadius = 0.25f;
        [SerializeField] private LayerMask obstacleMask = ~(1 << 2);
        [SerializeField] private float smoothing = 14f;

        private float currentDistance;

        /// <summary>true mientras el hueco es demasiado pequeño y conviene la primera persona.</summary>
        public bool TooCloseForThirdPerson { get; private set; }

        private void OnEnable() => currentDistance = distance;

        private void LateUpdate()
        {
            if (pivot == null) pivot = transform.parent;
            if (pivot == null) return;

            Vector3 local = new Vector3(shoulderOffset, height, -distance);
            float len = local.magnitude;
            float wanted = ProbeDistance();

            // Acercarse al instante (no atravesar paredes) y alejarse suave.
            currentDistance = wanted < currentDistance ? wanted : Mathf.Lerp(currentDistance, wanted, 1f - Mathf.Exp(-smoothing * Time.deltaTime));
            TooCloseForThirdPerson = CameraBoomMath.ShouldFallbackToFirstPerson(currentDistance);

            float k = len > 0.001f ? currentDistance / len : 0f;
            transform.localPosition = local * k;
            transform.localRotation = Quaternion.identity;
        }

        /// <summary>Distancia libre detrás del jugador (SphereCast). Funciona aunque el componente esté desactivado.</summary>
        public float ProbeDistance()
        {
            if (pivot == null) pivot = transform.parent;
            if (pivot == null) return 0f;
            Vector3 origin = pivot.position;
            Vector3 local = new Vector3(shoulderOffset, height, -distance);
            Vector3 dir = pivot.TransformPoint(local) - origin;
            float len = dir.magnitude;
            if (len < 0.001f) return 0f;
            RaycastHit hit = default(RaycastHit);
            bool blocked = Physics.SphereCast(origin, probeRadius, dir / len, out hit, len, obstacleMask, QueryTriggerInteraction.Ignore);
            return CameraBoomMath.ResolveDistance(len, blocked ? hit.distance : 0f, blocked, probeRadius);
        }

        /// <summary>Vuelve a la posición de primera persona (la usa CameraModeSwitcher).</summary>
        public void ResetToFirstPerson()
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            currentDistance = distance;
        }
    }
}
