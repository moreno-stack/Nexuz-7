using System.Collections.Generic;
using UnityEngine;
using Nexus7.Enemies;

namespace Nexus7.Environment
{
    /// <summary>
    /// Volumen de humo denso que bloquea la vista de los robots (esfera de 'radius' metros):
    ///   · un rayo de visión que cruce 1,5 m o más de humo queda tapado;
    ///   · si el robot o el jugador están dentro, el alcance de visión baja a 6 m.
    /// No usa colisionadores: EnemyVision lo consulta con PerceptionModel (no bloquea balas, NavMesh ni al jugador).
    ///
    /// Colocar en: el objeto del humo (p. ej. junto a SmokeEmitter). Lo añade el constructor de la Fase 6.
    /// </summary>
    public class VisionSmoke : MonoBehaviour
    {
        [Tooltip("Radio del humo denso, en metros.")]
        [SerializeField] private float radius = 1.8f;
        [Tooltip("Desplazamiento del centro respecto al objeto (el humo sube desde el suelo).")]
        [SerializeField] private Vector3 centerOffset = new Vector3(0f, 1.2f, 0f);

        private static readonly List<VisionSmoke> active = new List<VisionSmoke>();
        private static Vector3[] centers = new Vector3[8];
        private static float[] radii = new float[8];
        private static int snapshotFrame = -1, snapshotCount;

        public float Radius => radius;
        public Vector3 Center => transform.position + centerOffset;
        public static int ActiveCount => active.Count;

        private void OnEnable() { active.Add(this); snapshotFrame = -1; }
        private void OnDisable() { active.Remove(this); snapshotFrame = -1; }

        /// <summary>Copia (una vez por fotograma) las esferas activas en arrays reutilizables.</summary>
        private static int Snapshot()
        {
            if (snapshotFrame == Time.frameCount) return snapshotCount;
            snapshotFrame = Time.frameCount;
            if (centers.Length < active.Count) { centers = new Vector3[active.Count * 2]; radii = new float[active.Count * 2]; }
            snapshotCount = 0;
            foreach (var s in active)
            {
                if (s == null) continue;
                centers[snapshotCount] = s.Center;
                radii[snapshotCount] = s.radius;
                snapshotCount++;
            }
            return snapshotCount;
        }

        /// <summary>¿Tapa el humo la línea entre 'from' y 'to'? (al menos 'blockLength' metros de humo).</summary>
        public static bool Blocks(Vector3 from, Vector3 to, float blockLength)
        {
            if (active.Count == 0) return false;
            int n = Snapshot();   // antes de leer los arrays: Snapshot puede ampliarlos
            return PerceptionModel.SmokeBlocks(from, to, centers, radii, n, blockLength);
        }

        /// <summary>¿Está el punto dentro de algún humo?</summary>
        public static bool Contains(Vector3 p)
        {
            if (active.Count == 0) return false;
            int n = Snapshot();
            return PerceptionModel.InsideAnySmoke(p, centers, radii, n);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.7f, 0.7f, 0.75f, 0.5f);
            Gizmos.DrawWireSphere(transform.position + centerOffset, radius);
        }
    }
}
