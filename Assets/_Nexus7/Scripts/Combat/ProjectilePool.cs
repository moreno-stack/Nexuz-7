using System.Collections.Generic;
using UnityEngine;
using Nexus7.Gameplay;

namespace Nexus7.Combat
{
    /// <summary>
    /// Proyectiles reutilizables (uno por escena, se crea solo la primera vez que se dispara).
    /// Empieza con 32 y crece si hace falta: disparar no crea ni destruye objetos durante el combate.
    /// Al reaparecer el jugador se apagan los proyectiles en vuelo.
    /// </summary>
    public class ProjectilePool : MonoBehaviour
    {
        private const int InitialSize = 32;
        private static ProjectilePool instance;
        private static Material material;

        private readonly Stack<PlasmaBolt> free = new Stack<PlasmaBolt>();
        private readonly List<PlasmaBolt> all = new List<PlasmaBolt>();

        public static ProjectilePool Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("PoolProyectiles");
                    instance = go.AddComponent<ProjectilePool>();
                    for (int i = 0; i < InitialSize; i++) instance.Release(instance.Create());
                }
                return instance;
            }
        }

        private void OnEnable() => PlayerRespawn.PlayerCaught += ReleaseAll;
        private void OnDisable() => PlayerRespawn.PlayerCaught -= ReleaseAll;
        private void OnDestroy() { if (instance == this) instance = null; }

        public PlasmaBolt Get() => free.Count > 0 ? free.Pop() : Create();

        public void Release(PlasmaBolt bolt)
        {
            if (bolt == null || free.Contains(bolt)) return;
            bolt.gameObject.SetActive(false);
            free.Push(bolt);
        }

        public void ReleaseAll()
        {
            foreach (var b in all) if (b != null && b.gameObject.activeSelf) Release(b);
        }

        private PlasmaBolt Create()
        {
            if (material == null)
            {
                var shader = Shader.Find("Sprites/Default");   // sin iluminación: el rayo brilla igual en las zonas oscuras
                if (shader != null) material = new Material(shader);
            }
            var go = new GameObject("Proyectil");
            go.transform.SetParent(transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.widthMultiplier = 0.05f;
            if (material != null) line.sharedMaterial = material;
            var bolt = go.AddComponent<PlasmaBolt>();
            bolt.Init(this, line);
            all.Add(bolt);
            go.SetActive(false);
            return bolt;
        }
    }
}
