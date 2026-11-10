using System;
using UnityEngine;

namespace Nexus7.Enemies
{
    /// <summary>Cómo de expuesto está el jugador a la vista de los robots.</summary>
    public enum StealthExposure
    {
        /// <summary>Agachado en una zona de sombra o con camuflaje óptico: alcance de visión −40 %.</summary>
        Concealed,
        Normal,
        /// <summary>Corriendo o bajo luz directa: alcance de visión +30 %.</summary>
        Exposed,
    }

    /// <summary>
    /// Matemática de la percepción de los robots, sin depender del motor (se prueba fuera de Unity):
    /// alcance efectivo con tope duro, cono de visión, humo que tapa los rayos, puntos del cuerpo que se comprueban,
    /// luz directa y búsqueda en la última posición vista.
    /// Usa solo x, y, z de Vector3 y su propia matemática.
    /// </summary>
    public static class PerceptionModel
    {
        public const float ConcealedRangeMultiplier = 0.6f;
        public const float ExposedRangeMultiplier = 1.3f;
        public const float MinViewAngle = 75f, MaxViewAngle = 90f;
        public const int SamplePointCount = 5;
        /// <summary>Separación de cada hombro respecto al centro del cuerpo (m).</summary>
        public const float ShoulderOffset = 0.22f;
        /// <summary>Por debajo de esta velocidad horizontal (m/s) el jugador cuenta como quieto al buscarlo.</summary>
        public const float MinFleeSpeed = 0.5f;

        // =============== SIGILO ===============

        /// <summary>Ocultarse gana a exponerse: agachado en sombra o con camuflaje → Concealed aunque haya luz.</summary>
        public static StealthExposure Exposure(bool crouching, bool inShadow, bool cloaked, bool running, bool lit)
        {
            if ((crouching && inShadow) || cloaked) return StealthExposure.Concealed;
            if (running || lit) return StealthExposure.Exposed;
            return StealthExposure.Normal;
        }

        public static float RangeMultiplier(StealthExposure e) =>
            e == StealthExposure.Concealed ? ConcealedRangeMultiplier : e == StealthExposure.Exposed ? ExposedRangeMultiplier : 1f;

        // =============== ALCANCE ===============

        /// <summary>
        /// Alcance real = base × alerta de la nave × sigilo, limitado por el tope duro (y por el de humo si hay humo).
        /// Ningún multiplicador permite pasar del tope.
        /// </summary>
        public static float EffectiveRange(float baseRange, float alertMultiplier, float stealthMultiplier, bool inSmoke, float hardMax, float smokeMax)
        {
            float range = Math.Max(0f, baseRange * alertMultiplier * stealthMultiplier);
            float cap = inSmoke ? Math.Min(smokeMax, hardMax) : hardMax;
            return Math.Min(range, cap);
        }

        public static float ClampViewAngle(float angle) => Math.Max(MinViewAngle, Math.Min(MaxViewAngle, angle));

        // =============== CONO ===============

        /// <summary>true si 'toTarget' está dentro del cono (apertura total en grados) alrededor de 'forward'.</summary>
        public static bool InCone(Vector3 forward, Vector3 toTarget, float fullAngle)
        {
            float lf = Len(forward), lt = Len(toTarget);
            if (lf < 1e-5f || lt < 1e-5f) return false;
            float cos = Dot(forward, toTarget) / (lf * lt);
            return cos >= (float)Math.Cos(fullAngle * 0.5f * Math.PI / 180.0);
        }

        /// <summary>
        /// Cono de los robots: el ángulo se mide en el plano horizontal (apertura total 'fullAngle') y, aparte,
        /// la diferencia de inclinación con la mirada no puede pasar de 'verticalHalfAngle'.
        /// Así no hay un punto ciego justo delante y debajo del ojo (jugador agachado cerca).
        /// </summary>
        public static bool InViewCone(Vector3 forward, Vector3 toTarget, float fullAngle, float verticalHalfAngle)
        {
            float fh = (float)Math.Sqrt(forward.x * forward.x + forward.z * forward.z);
            float th = (float)Math.Sqrt(toTarget.x * toTarget.x + toTarget.z * toTarget.z);
            if (Len(toTarget) < 1e-5f || fh < 1e-5f) return false;
            if (th > 0.05f && !InCone(new Vector3(forward.x, 0f, forward.z), new Vector3(toTarget.x, 0f, toTarget.z), fullAngle)) return false;
            double pitchF = Math.Atan2(forward.y, fh), pitchT = Math.Atan2(toTarget.y, th);
            return Math.Abs(pitchT - pitchF) * 180.0 / Math.PI <= verticalHalfAngle;
        }

        // =============== HUMO ===============

        /// <summary>Metros del segmento a→b que quedan dentro de la esfera (centro, radio).</summary>
        public static float SegmentSphereLength(Vector3 a, Vector3 b, Vector3 center, float radius)
        {
            float dx = b.x - a.x, dy = b.y - a.y, dz = b.z - a.z;
            float len = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
            if (len < 1e-5f || radius <= 0f) return 0f;
            float ux = dx / len, uy = dy / len, uz = dz / len;
            float ox = a.x - center.x, oy = a.y - center.y, oz = a.z - center.z;
            // |o + t·u|² = r²  →  t² + 2(o·u)t + |o|² − r² = 0
            float b2 = ox * ux + oy * uy + oz * uz;
            float c = ox * ox + oy * oy + oz * oz - radius * radius;
            float disc = b2 * b2 - c;
            if (disc <= 0f) return 0f;
            float s = (float)Math.Sqrt(disc);
            float t0 = Math.Max(0f, -b2 - s), t1 = Math.Min(len, -b2 + s);
            return Math.Max(0f, t1 - t0);
        }

        /// <summary>El humo denso tapa el rayo si este recorre al menos 'blockLength' metros de humo en total.</summary>
        public static bool SmokeBlocks(Vector3 from, Vector3 to, Vector3[] centers, float[] radii, int count, float blockLength)
        {
            float total = 0f;
            for (int i = 0; i < count; i++)
            {
                total += SegmentSphereLength(from, to, centers[i], radii[i]);
                if (total >= blockLength) return true;
            }
            return false;
        }

        public static bool InsideAnySmoke(Vector3 p, Vector3[] centers, float[] radii, int count)
        {
            for (int i = 0; i < count; i++)
            {
                float dx = p.x - centers[i].x, dy = p.y - centers[i].y, dz = p.z - centers[i].z;
                if (dx * dx + dy * dy + dz * dz <= radii[i] * radii[i]) return true;
            }
            return false;
        }

        // =============== PUNTOS DEL CUERPO ===============

        /// <summary>
        /// Rellena 'output' con los 5 puntos que se comprueban: cabeza (90 % de la altura), pecho (60 %), cadera (35 %)
        /// y los dos hombros (75 %), separados a los lados respecto a la línea de mirada del robot.
        /// Devuelve cuántos puntos escribió.
        /// </summary>
        public static int SamplePoints(Vector3 feet, float height, Vector3 eye, Vector3[] output)
        {
            float hx = feet.x - eye.x, hz = feet.z - eye.z;
            float hl = (float)Math.Sqrt(hx * hx + hz * hz);
            if (hl < 1e-4f) { hx = 0f; hz = 1f; } else { hx /= hl; hz /= hl; }
            // Derecha respecto a la mirada: up × dir = (dir.z, 0, −dir.x)
            float rx = hz * ShoulderOffset, rz = -hx * ShoulderOffset;
            output[0] = new Vector3(feet.x, feet.y + height * 0.9f, feet.z);
            output[1] = new Vector3(feet.x, feet.y + height * 0.6f, feet.z);
            output[2] = new Vector3(feet.x, feet.y + height * 0.35f, feet.z);
            output[3] = new Vector3(feet.x - rx, feet.y + height * 0.75f, feet.z - rz);
            output[4] = new Vector3(feet.x + rx, feet.y + height * 0.75f, feet.z + rz);
            return SamplePointCount;
        }

        /// <summary>
        /// Velocidad de detección según cuántos rayos llegan al jugador: 0 si ninguno; si alguno llega,
        /// entre 0,4 (un solo hombro asomado) y 1 (todo el cuerpo a la vista).
        /// </summary>
        public static float DetectionFactor(int clearRays, int totalRays)
        {
            if (totalRays <= 0 || clearRays <= 0) return 0f;
            return 0.4f + 0.6f * Math.Min(1f, (float)clearRays / totalRays);
        }

        // =============== LUZ DIRECTA ===============

        /// <summary>
        /// ¿Ilumina esta luz directamente el punto? Debe estar encendida (intensidad ≥ mínimo) y:
        ///   · lámpara (puntual): el punto está en su «charco de luz», a menos de max(1,5 m; 25 % del alcance) en horizontal
        ///     y a menos del 80 % del alcance en total (sirve igual para lámparas bajas y altas);
        ///   · foco: dentro de su haz y a menos del 60 % de su alcance.
        /// Las paredes se comprueban aparte con un rayo.
        /// </summary>
        public static bool IsLitBy(Vector3 lightPos, float range, float intensity, bool isSpot, Vector3 spotForward, float spotAngle,
                                   Vector3 target, float minIntensity)
        {
            if (intensity < minIntensity || range <= 0f) return false;
            var to = new Vector3(target.x - lightPos.x, target.y - lightPos.y, target.z - lightPos.z);
            float d = Len(to);
            if (isSpot) return d <= range * 0.6f && InCone(spotForward, to, spotAngle);
            float horizontal = (float)Math.Sqrt(to.x * to.x + to.z * to.z);
            return d <= range * 0.8f && horizontal <= Math.Max(1.5f, range * 0.25f);
        }

        // =============== MEMORIA Y BÚSQUEDA ===============

        /// <summary>
        /// Punto donde buscar: 'distance' metros más allá de la última posición vista en la dirección (horizontal) en que huía.
        /// Si estaba casi quieto, la propia última posición.
        /// </summary>
        public static Vector3 SearchProbe(Vector3 lastSeen, Vector3 lastVelocity, float distance)
        {
            float vx = lastVelocity.x, vz = lastVelocity.z;
            float speed = (float)Math.Sqrt(vx * vx + vz * vz);
            if (speed < MinFleeSpeed) return new Vector3(lastSeen.x, lastSeen.y, lastSeen.z);
            return new Vector3(lastSeen.x + vx / speed * distance, lastSeen.y, lastSeen.z + vz / speed * distance);
        }

        /// <summary>Tras perderte de vista: deja de perseguir (y busca) al llegar a la última posición o si tarda demasiado.</summary>
        public static bool ShouldEndPursuit(bool canSee, bool arrived, float timeSinceLost, float maxPursuitTime) =>
            !canSee && (arrived || timeSinceLost >= maxPursuitTime);

        private static float Dot(Vector3 a, Vector3 b) => a.x * b.x + a.y * b.y + a.z * b.z;
        private static float Len(Vector3 v) => (float)Math.Sqrt(v.x * v.x + v.y * v.y + v.z * v.z);
    }
}
