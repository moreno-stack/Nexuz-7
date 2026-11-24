using System.Collections.Generic;
using UnityEngine;
using Nexus7.Core;

namespace Nexus7.Inventory
{
    /// <summary>
    /// Botín aleatorio de una sala: en cada PARTIDA cambian los objetos y su sitio (semilla de GameSession),
    /// pero los objetos imprescindibles ('Guaranteed') aparecen siempre. Al reaparecer tras una captura no se
    /// vuelve a tirar (la escena no se recarga), así que lo recogido no reaparece.
    /// Los imprescindibles brillan más y llevan un haz de luz vertical de 3 m para verlos desde lejos.
    ///
    /// Colocar en: un objeto "Botin" de la sala, con LootSpawnPoint como hijos (o asignados en 'Points').
    /// </summary>
    public class RoomLootSpawner : MonoBehaviour
    {
        [SerializeField] private LootTable table;
        [SerializeField] private List<ItemStack> guaranteed = new List<ItemStack>();
        [SerializeField] private LootSpawnPoint[] points;
        [Header("Pruebas")]
        [SerializeField] private bool useFixedSeed;
        [SerializeField] private int fixedSeed = 1234;

        private static Material pickupMaterial, beamMaterial;
        /// <summary>Brillo y haz de los objetos imprescindibles (para encontrarlos en salas oscuras).</summary>
        public const float BeaconLightRange = 4f, BeaconLightIntensity = 2f, BeaconHeight = 3f, BeaconWidth = 0.06f;

        private void Start()
        {
            if (points == null || points.Length == 0) points = GetComponentsInChildren<LootSpawnPoint>();
            points = System.Array.FindAll(points, p => p != null);   // un punto borrado no puede llevarse un garantizado
            if (points.Length < guaranteed.Count)
                Debug.LogError($"[RoomLootSpawner] {name}: hay {points.Length} puntos para {guaranteed.Count} objetos garantizados; los sobrantes compartirán punto.", this);

            int seed = useFixedSeed ? fixedSeed : GameSession.RunSeed ^ StableHash(gameObject.scene.name + "/" + name);
            var empty = new float[points.Length];
            for (int i = 0; i < points.Length; i++) empty[i] = points[i] != null ? points[i].emptyChance : 1f;

            var placements = LootRoller.Roll(seed, points.Length, empty, guaranteed, table != null ? table.entries : null);
            var perPoint = new Dictionary<int, int>();
            foreach (var p in placements)
            {
                int n; perPoint.TryGetValue(p.point, out n); perPoint[p.point] = n + 1;
                Spawn(p.point >= 0 ? points[p.point].transform : transform, p, n);
            }
        }

        private void Spawn(Transform at, LootPlacement p, int stackIndex)
        {
            var root = new GameObject("Botin_" + p.item.DisplayName);
            root.transform.SetParent(at, false);
            root.transform.localPosition = new Vector3(0.35f * stackIndex, 0f, 0f);
            var col = root.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.25f, 0f);
            col.size = new Vector3(0.5f, 0.5f, 0.5f);

            PrimitiveType shape = p.item.Category == ItemCategory.Battery || p.item.Category == ItemCategory.Device ? PrimitiveType.Cylinder
                                : p.item.Category == ItemCategory.Tool ? PrimitiveType.Capsule : PrimitiveType.Cube;
            var visual = GameObject.CreatePrimitive(shape);
            visual.name = "Visual";
            var vc = visual.GetComponent<Collider>();
            if (vc != null) { vc.enabled = false; Destroy(vc); }   // Destroy es diferido: que no choque ese frame
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = new Vector3(0f, 0.3f, 0f);
            visual.transform.localScale = shape == PrimitiveType.Cube ? new Vector3(0.3f, 0.12f, 0.24f) : new Vector3(0.18f, 0.14f, 0.18f);
            var r = visual.GetComponent<Renderer>();
            if (r != null)
            {
                if (pickupMaterial == null) pickupMaterial = new Material(r.sharedMaterial);
                var m = new Material(pickupMaterial) { color = p.item.UiColor };
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", p.item.UiColor * (p.guaranteed ? 0.9f : 0.4f));
                r.sharedMaterial = m;
            }
            if (p.guaranteed)
            {
                var light = new GameObject("Brillo").AddComponent<Light>();
                light.transform.SetParent(root.transform, false);
                light.transform.localPosition = new Vector3(0f, 0.6f, 0f);
                light.type = LightType.Point;
                light.color = p.item.UiColor;
                light.range = BeaconLightRange;
                light.intensity = BeaconLightIntensity;
                AddBeacon(root.transform, p.item.UiColor);
            }
            var pickup = root.AddComponent<PickupItem>();
            pickup.Setup(p.item, p.amount);
            pickup.SetNoise(PickupNoiseFor(p.item.Category));
            pickup.SetVisual(visual.transform);
        }

        /// <summary>
        /// Haz de luz vertical sobre un objeto imprescindible (baterías, llaves...): en las salas oscuras se ve desde lejos.
        /// Sin colisionador; desaparece con el objeto al recogerlo.
        /// </summary>
        private static void AddBeacon(Transform parent, Color color)
        {
            var beam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            beam.name = "Haz";
            var c = beam.GetComponent<Collider>();
            if (c != null) { c.enabled = false; Destroy(c); }
            beam.transform.SetParent(parent, false);
            beam.transform.localPosition = new Vector3(0f, 0.3f + BeaconHeight * 0.5f, 0f);
            beam.transform.localScale = new Vector3(BeaconWidth, BeaconHeight * 0.5f, BeaconWidth);   // el cilindro mide 2 de alto
            var r = beam.GetComponent<Renderer>();
            if (r == null) return;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (beamMaterial == null)
            {
                var shader = Shader.Find("Sprites/Default");
                if (shader == null) { r.enabled = false; return; }
                beamMaterial = new Material(shader);
            }
            r.sharedMaterial = new Material(beamMaterial) { color = new Color(color.r, color.g, color.b, 0.35f) };
        }

        /// <summary>Ruido al recoger: la chatarra suena (3 m), lo demás no.</summary>
        public static float PickupNoiseFor(ItemCategory category) => category == ItemCategory.Part ? 3f : 0f;

        private static int StableHash(string s)
        {
            unchecked
            {
                int h = 23;
                foreach (char c in s) h = h * 31 + c;
                return h;
            }
        }
    }
}
