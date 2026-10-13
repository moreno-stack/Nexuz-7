using UnityEngine;

namespace Nexus7.Enemies
{
    /// <summary>
    /// Ruta de patrulla: sus HIJOS (en orden) son los waypoints.
    /// Añade el componente Waypoint a un hijo para darle su propio tiempo de espera u orientación.
    ///   Loop     : 0→1→2→3→0→1...
    ///   PingPong : 0→1→2→1→0→1...
    ///
    /// Colocar en: un GameObject vacío "Ruta_X" con hijos vacíos "WP_0", "WP_1"...
    /// Se dibuja en la vista Scene para ajustarla fácilmente.
    /// </summary>
    public class PatrolRoute : MonoBehaviour
    {
        public enum RouteMode { Loop, PingPong }

        [SerializeField] private RouteMode mode = RouteMode.Loop;
        [Tooltip("Espera por defecto en cada punto (si el punto no tiene componente Waypoint).")]
        [SerializeField] private float defaultWaitTime = 1.5f;
        [SerializeField] private Color gizmoColor = new Color(1f, 0.6f, 0.1f);

        public int Count => transform.childCount;

        public Vector3 GetPosition(int index) => transform.GetChild(index).position;

        public float GetWaitTime(int index)
        {
            var wp = transform.GetChild(index).GetComponent<Waypoint>();
            return wp != null ? wp.WaitTime : defaultWaitTime;
        }

        /// <summary>Si el waypoint pide mirar en una dirección durante la espera, la devuelve.</summary>
        public bool TryGetLookRotation(int index, out Quaternion rotation)
        {
            var child = transform.GetChild(index);
            var wp = child.GetComponent<Waypoint>();
            rotation = child.rotation;
            return wp != null && wp.FaceForward;
        }

        /// <summary>Índice siguiente. 'direction' (+1/-1) se actualiza en modo PingPong.</summary>
        public int GetNextIndex(int current, ref int direction)
        {
            if (Count <= 1) return 0;
            if (mode == RouteMode.Loop) return (current + 1) % Count;

            int next = current + direction;
            if (next < 0 || next >= Count)
            {
                direction = -direction;
                next = current + direction;
            }
            return next;
        }

        public int GetClosestIndex(Vector3 position)
        {
            int best = 0;
            float bestDist = float.MaxValue;
            for (int i = 0; i < Count; i++)
            {
                float d = (GetPosition(i) - position).sqrMagnitude;
                if (d < bestDist) { bestDist = d; best = i; }
            }
            return best;
        }

        private void OnDrawGizmos()
        {
            if (Count == 0) return;
            Gizmos.color = gizmoColor;
            for (int i = 0; i < Count; i++)
            {
                Vector3 p = GetPosition(i);
                Gizmos.DrawSphere(p + Vector3.up * 0.1f, 0.25f);
                bool hasNext = i + 1 < Count || mode == RouteMode.Loop;
                if (hasNext) Gizmos.DrawLine(p + Vector3.up * 0.1f, GetPosition((i + 1) % Count) + Vector3.up * 0.1f);
            }
        }
    }
}
