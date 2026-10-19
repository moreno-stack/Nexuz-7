using UnityEngine;

namespace Nexus7.Environment
{
    /// <summary>
    /// Vibración de cámara por "trauma" (técnica estándar): el temblor es trauma², con ruido Perlin suave.
    ///   AddTrauma(x)      → sacudida puntual (explosión, lanzamiento) que se disipa
    ///   SetBaseTrauma(x)  → vibración continua mínima (la nave desintegrándose)
    ///
    /// Colocar en: la cámara del jugador (Player/CameraRoot/PlayerCamera). Desplaza y rota su transform LOCAL,
    /// así que no interfiere con PlayerLook (que rota CameraRoot).
    /// </summary>
    public class CameraShake : MonoBehaviour
    {
        [SerializeField] private float maxOffset = 0.12f;
        [SerializeField] private float maxRoll = 3f;
        [SerializeField] private float frequency = 18f;
        [SerializeField] private float decayPerSecond = 0.9f;

        private float trauma, baseTrauma, seed;
        private Vector3 restPosition;
        private Quaternion restRotation;

        public float Trauma => Mathf.Max(trauma, baseTrauma);

        private void Awake()
        {
            seed = Random.value * 100f;
            restPosition = transform.localPosition;
            restRotation = transform.localRotation;
        }

        public void AddTrauma(float amount) => trauma = Mathf.Clamp01(trauma + amount);
        public void SetBaseTrauma(float amount) => baseTrauma = Mathf.Clamp01(amount);

        private void LateUpdate()
        {
            trauma = Mathf.Max(0f, trauma - decayPerSecond * Time.deltaTime);
            float shake = Trauma * Trauma;
            float t = Time.time * frequency;
            Vector3 offset = new Vector3(
                (Mathf.PerlinNoise(seed, t) - 0.5f) * 2f,
                (Mathf.PerlinNoise(seed + 1f, t) - 0.5f) * 2f,
                0f) * (maxOffset * shake);
            float roll = (Mathf.PerlinNoise(seed + 2f, t) - 0.5f) * 2f * maxRoll * shake;
            transform.localPosition = restPosition + offset;
            transform.localRotation = restRotation * Quaternion.Euler(0f, 0f, roll);
        }
    }
}
