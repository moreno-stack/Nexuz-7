using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Nexus7.Core
{
    /// <summary>
    /// Genera el NavMesh (la "malla" por la que caminan los enemigos) al cargar la escena,
    /// usando la API integrada de Unity — no necesita instalar el paquete AI Navigation.
    /// Recoge los colliders dentro de una caja centrada en este objeto.
    ///
    /// Se ejecuta antes que cualquier otro script (DefaultExecutionOrder -1000), así cuando
    /// los enemigos activan su NavMeshAgent en Start() el NavMesh ya existe.
    ///
    /// Si prefieres hornearlo en el editor: instala "AI Navigation" (Package Manager),
    /// añade NavMeshSurface al suelo, pulsa Bake y elimina este componente.
    ///
    /// Colocar en: un GameObject vacío "NavMesh" en el centro del nivel.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class RuntimeNavMeshBaker : MonoBehaviour
    {
        [Tooltip("Tamaño de la zona a hornear, centrada en este objeto.")]
        [SerializeField] private Vector3 size = new Vector3(40f, 12f, 40f);
        [Tooltip("Capas incluidas. Por defecto todo menos 'Ignore Raycast' (los triggers).")]
        [SerializeField] private LayerMask includedLayers = ~(1 << 2);
        [Tooltip("Objetos dinámicos que NO deben agujerear el NavMesh (jugador, enemigos).")]
        [SerializeField] private Transform[] ignoreRoots;
        [Tooltip("0 = Humanoid (radio 0.5, altura 2).")]
        [SerializeField] private int agentTypeId = 0;

        private NavMeshDataInstance instance;

        private void Awake() => Bake();

        public void Bake()
        {
            if (instance.valid) instance.Remove();

            var markups = new List<NavMeshBuildMarkup>();
            if (ignoreRoots != null)
                foreach (var root in ignoreRoots)
                    if (root != null) markups.Add(new NavMeshBuildMarkup { root = root, ignoreFromBuild = true });

            var sources = new List<NavMeshBuildSource>();
            var worldBounds = new Bounds(transform.position, size);
            NavMeshBuilder.CollectSources(worldBounds, includedLayers, NavMeshCollectGeometry.PhysicsColliders, 0, markups, sources);

            var settings = NavMesh.GetSettingsByID(agentTypeId);
            var localBounds = new Bounds(Vector3.zero, size);
            NavMeshData data = NavMeshBuilder.BuildNavMeshData(settings, sources, localBounds, transform.position, Quaternion.identity);
            instance = NavMesh.AddNavMeshData(data);
        }

        private void OnDestroy()
        {
            if (instance.valid) instance.Remove();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.4f);
            Gizmos.DrawWireCube(transform.position, size);
        }
    }
}
