using System.Collections.Generic;
using UnityEngine;

namespace Nexus7.Combat
{
    /// <summary>
    /// Proyectil visible y rápido. Cada fotograma avanza velocidad × dt y, ANTES de moverse, barre ese tramo con un
    /// SphereCast (radio 0,05 m): a 220 m/s no atraviesa paredes finas aunque el juego vaya a pocos FPS.
    ///   · Ignora a quien disparó y los activadores.
    ///   · Contra un IDamageable aplica el daño (con la caída por distancia del arma) y, si el arma atraviesa (Pierce),
    ///     sigue hacia el siguiente. Contra una pared se detiene con un destello.
    ///   · Los disparos de los robots (hostile) atraviesan a los demás robots: no hay fuego amigo.
    ///   · Desaparece al recorrer el alcance del arma o a los 2 s.
    /// Lo gestiona ProjectilePool (no se crea ni se destruye a mano).
    /// </summary>
    public class PlasmaBolt : MonoBehaviour
    {
        public const float Radius = 0.05f;
        private const float MaxLifetime = 2f;
        private static readonly RaycastHit[] hits = new RaycastHit[12];

        private ProjectilePool pool;
        private LineRenderer line;
        private PlayerWeaponSpec spec;
        private DamageInfo template;
        private Transform ignoreRoot;
        private Vector3 position, direction;
        private float traveled, age, trailLength;
        private int pierceLeft;
        private bool hostile;
        private readonly HashSet<IDamageable> alreadyHit = new HashSet<IDamageable>();
        private int mask = ~(1 << 2);   // todo menos "Ignore Raycast" (activadores de los checkpoints)

        internal void Init(ProjectilePool owner, LineRenderer renderer) { pool = owner; line = renderer; }

        /// <param name="fromEnemy">true = lo dispara un robot: solo daña al jugador.</param>
        public void Launch(Vector3 from, Vector3 dir, PlayerWeaponSpec weapon, DamageInfo info, Transform shooter, bool fromEnemy = false)
        {
            hostile = fromEnemy;
            spec = weapon;
            template = info;
            ignoreRoot = shooter;
            position = from;
            direction = dir.normalized;
            traveled = 0f;
            age = 0f;
            pierceLeft = weapon.Pierce;
            alreadyHit.Clear();
            trailLength = Mathf.Clamp(weapon.ProjectileSpeed * 0.012f, 0.4f, 2.5f);
            var c = new Color(weapon.ColorR, weapon.ColorG, weapon.ColorB, 1f);
            line.startColor = c;
            line.endColor = new Color(c.r, c.g, c.b, 0f);
            line.widthMultiplier = weapon.Pierce > 0 ? 0.09f : weapon.Pellets > 1 ? 0.03f : 0.05f;
            gameObject.SetActive(true);
            DrawTrail();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            age += dt;
            float step = spec.ProjectileSpeed * dt;
            if (traveled + step > spec.Range) step = spec.Range - traveled;

            if (step > 0f && Sweep(step)) return;

            position += direction * step;
            traveled += step;
            DrawTrail();
            if (traveled >= spec.Range - 0.001f || age >= MaxLifetime) pool.Release(this);
        }

        /// <summary>Barrido del tramo. Devuelve true si el proyectil se ha detenido.</summary>
        private bool Sweep(float step)
        {
            int n = Physics.SphereCastNonAlloc(position, Radius, direction, hits, step, mask, QueryTriggerInteraction.Ignore);
            SortByDistance(n);
            for (int i = 0; i < n; i++)
            {
                var h = hits[i];
                if (h.collider == null) continue;
                var t = h.collider.transform;
                if (ignoreRoot != null && t.IsChildOf(ignoreRoot)) continue;

                // Si el barrido empieza ya dentro de un colisionador, Unity da distancia 0 y punto (0,0,0)
                Vector3 point = h.distance > 0f ? h.point : position;
                float distance = traveled + h.distance;
                var target = h.collider.GetComponentInParent<IDamageable>();
                if (target != null)
                {
                    if (hostile && target is EnemyHealth) continue;
                    if (!hostile && target is PlayerTarget) continue;
                    if (alreadyHit.Contains(target) || !target.IsAlive) continue;
                    alreadyHit.Add(target);
                    var info = template;
                    info.Amount = spec.Damage * spec.DamageFactorAt(distance);
                    info.Point = point;
                    info.Normal = h.normal;
                    info.Direction = direction;
                    target.TakeDamage(info);
                    if (pierceLeft-- > 0) continue;   // el cañón de riel sigue hacia el siguiente
                    Stop(point, true);
                    return true;
                }
                Stop(point, false);
                return true;
            }
            return false;
        }

        private void Stop(Vector3 point, bool onTarget)
        {
            var c = new Color(spec.ColorR, spec.ColorG, spec.ColorB, 1f);
            if (!onTarget)
            {
                Nexus7.Devices.PulseRing.Spawn(point - Vector3.up * 0.1f, 0.25f, c, 0.2f);
                Nexus7.Core.NoiseSystem.Emit(point, 3f, null);   // el impacto en una pared también se oye (cerca)
            }
            pool.Release(this);
        }

        private static void SortByDistance(int n)
        {
            for (int i = 1; i < n; i++)
            {
                var k = hits[i];
                int j = i - 1;
                while (j >= 0 && hits[j].distance > k.distance) { hits[j + 1] = hits[j]; j--; }
                hits[j + 1] = k;
            }
        }

        private void DrawTrail()
        {
            float back = Mathf.Min(trailLength, traveled + 0.05f);
            line.SetPosition(0, position);
            line.SetPosition(1, position - direction * back);
        }
    }
}
