using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Nexus7.Combat
{
    /// <summary>
    /// Coberturas de la fase, generadas al cargarla (sin reconstruir escenas): 4 puntos a 0,8 m de cada cara de los objetos
    /// llamados Caja, Estanteria, Contenedor, Consola, Mesa, Tabique, Bidon o Columna que tengan colisionador sólido,
    /// solo si caen sobre el NavMesh. Cobertura baja si el objeto mide menos de 1,2 m.
    /// También guarda las reservas (un punto por robot). Lo prepara CombatInstaller en cada fase.
    /// </summary>
    public static class CoverField
    {
        public const float Offset = 0.8f, LowHeight = 1.2f, MinSpacing = 0.9f;
        private static readonly string[] Names = { "caja", "estanteria", "contenedor", "consola", "mesa", "tabique", "bidon", "columna" };

        public static readonly List<CoverSpot> Spots = new List<CoverSpot>();
        public static readonly CoverReservations Reservations = new CoverReservations();

        public static Vector3 Position(CoverSpot s) => new Vector3(s.X, s.Y, s.Z);

        public static void Build()
        {
            Spots.Clear();
            Reservations.Clear();
            foreach (var col in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
            {
                if (col == null || col.isTrigger || !col.enabled || !IsCoverObject(col.transform)) continue;
                var b = col.bounds;
                if (b.size.y < 0.6f || b.size.y > 4f) continue;
                float wide = Mathf.Max(b.size.x, b.size.z);
                if (wide < 0.4f || wide > 8f) continue;
                bool low = b.size.y < LowHeight;
                TryAdd(new Vector3(b.max.x + Offset, b.min.y, b.center.z), 1f, 0f, low);
                TryAdd(new Vector3(b.min.x - Offset, b.min.y, b.center.z), -1f, 0f, low);
                TryAdd(new Vector3(b.center.x, b.min.y, b.max.z + Offset), 0f, 1f, low);
                TryAdd(new Vector3(b.center.x, b.min.y, b.min.z - Offset), 0f, -1f, low);
            }
            if (Spots.Count > 0) Debug.Log($"[Nexus-7] Coberturas de combate: {Spots.Count} puntos.");
        }

        private static bool IsCoverObject(Transform t)
        {
            for (int depth = 0; t != null && depth < 2; depth++, t = t.parent)
            {
                string n = t.name.ToLowerInvariant();
                foreach (var key in Names) if (n.StartsWith(key)) return true;
            }
            return false;
        }

        private static void TryAdd(Vector3 p, float nx, float nz, bool low)
        {
            if (!NavMesh.SamplePosition(p, out NavMeshHit hit, 1f, NavMesh.AllAreas)) return;
            Vector3 q = hit.position;
            foreach (var s in Spots)
                if ((new Vector3(s.X, s.Y, s.Z) - q).sqrMagnitude < MinSpacing * MinSpacing) return;
            Spots.Add(new CoverSpot { Id = Spots.Count + 1, X = q.x, Y = q.y, Z = q.z, NormalX = nx, NormalZ = nz, Low = low });
        }
    }
}
