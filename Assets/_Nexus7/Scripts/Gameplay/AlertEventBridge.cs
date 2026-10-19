using UnityEngine;
using Nexus7.Core;
using Nexus7.Devices;
using Nexus7.Enemies;
using Nexus7.Player;
using Nexus7.UI;

namespace Nexus7.Gameplay
{
    /// <summary>
    /// Conecta los sucesos de TODOS los sistemas del juego con el AlertSystem. Se crea solo al arrancar
    /// el juego (RuntimeInitializeOnLoadMethod) y sobrevive a los cambios de escena (DontDestroyOnLoad),
    /// así que funciona desde la Fase 1 sin colocar nada en las escenas.
    ///
    ///   AlarmSystem.AlarmRaised     → AlarmRaised  (cámaras, láseres, bancos manipulados...)
    ///   EnemyPatrol.PlayerSpotted   → PlayerSpotted (un dron empieza a perseguirte)
    ///   PlayerRespawn.PlayerCaught  → PlayerCaught
    ///   NoiseSystem (jugador corriendo, radio ≥ 10) → LoudNoise · detonación PEM → DeviceDetonation
    /// También avisa por el HUD de los cambios de nivel y hace avanzar la disipación del calor.
    /// </summary>
    public class AlertEventBridge : MonoBehaviour
    {
        private static AlertEventBridge instance;

        /// <summary>Silencia los avisos de cambio de nivel (p. ej. al forzar el nivel en el cierre de una fase).</summary>
        public static bool MuteLevelMessages;

        [Tooltip("Una persecución (o varias a la vez) cuenta como UNA detección durante estos segundos.")]
        [SerializeField] private float spottedCooldown = 6f;
        [Tooltip("Como máximo un suceso de 'ruido fuerte' por este intervalo (correr genera muchos pasos).")]
        [SerializeField] private float noiseCooldown = 0.6f;
        private float lastSpotted = -999f, lastNoise = -999f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (instance != null) return;
            var go = new GameObject("[AlertSystem]");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<AlertEventBridge>();
        }

        private void OnEnable()
        {
            AlarmSystem.AlarmRaised += OnAlarm;
            EnemyPatrol.PlayerSpotted += OnSpotted;
            PlayerRespawn.PlayerCaught += OnCaught;
            NoiseSystem.NoiseEmitted += OnNoise;
            AlertSystem.LevelChanged += OnLevelChanged;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            AlarmSystem.AlarmRaised -= OnAlarm;
            EnemyPatrol.PlayerSpotted -= OnSpotted;
            PlayerRespawn.PlayerCaught -= OnCaught;
            NoiseSystem.NoiseEmitted -= OnNoise;
            AlertSystem.LevelChanged -= OnLevelChanged;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            AlertSystem.ForgetPlayerPosition();
            lastSpotted = lastNoise = -999f;
        }

        private void Update() => AlertSystem.Tick(Time.deltaTime);

        private void OnAlarm(Vector3 position, float radius) => AlertSystem.Report(AlertEvent.AlarmRaised, position);
        private void OnSpotted(EnemyPatrol enemy)
        {
            if (Time.time - lastSpotted < spottedCooldown)
            {
                // Mismo encuentro (otro dron del escuadrón o re-adquisición): solo actualiza la posición conocida.
                AlertSystem.ReportPlayerPosition(enemy.Vision.LastSeenPosition);
                return;
            }
            lastSpotted = Time.time;
            AlertSystem.Report(AlertEvent.PlayerSpotted, enemy.Vision.LastSeenPosition);
        }
        private void OnCaught()
        {
            // PlayerCaught también se usa al reaparecer tras morir por una descarga: eso no es una captura.
            var player = GameObject.FindGameObjectWithTag("Player");
            var health = player != null ? player.GetComponent<PlayerHealth>() : null;
            if (health != null && health.Current <= 0f) return;
            AlertSystem.Report(AlertEvent.PlayerCaught);
        }

        private void OnNoise(Vector3 position, float radius, GameObject source)
        {
            if (source == null) return;
            if (radius >= 10f && source.GetComponent<PlayerController>() != null)
            {
                if (Time.time - lastNoise < noiseCooldown) return;
                lastNoise = Time.time;
                AlertSystem.Report(AlertEvent.LoudNoise);
            }
            else if (source.GetComponent<EmpDevice>() != null) AlertSystem.Report(AlertEvent.DeviceDetonation);
        }

        private void OnLevelChanged(AlertLevel from, AlertLevel to)
        {
            var hud = HUDManager.Instance;
            if (hud == null || MuteLevelMessages) return;
            switch (to)
            {
                case AlertLevel.Red:
                    hud.QueueMessage("IA NEXUS-7: ¡ALERTA ROJA! Protocolo de caza activado: los drones de élite te buscarán activamente y las compuertas blindadas se sellan.", 3f);
                    break;
                case AlertLevel.Yellow:
                    hud.QueueMessage(from == AlertLevel.Red
                        ? "IA NEXUS-7: La nave baja a alerta amarilla. Las compuertas vuelven a su estado anterior."
                        : "IA NEXUS-7: Alerta amarilla. La seguridad está más atenta: drones más rápidos y cámaras más ágiles.", 3f);
                    break;
                default:
                    hud.QueueMessage("IA NEXUS-7: La nave vuelve a la normalidad. Buen trabajo pasando desapercibido.", 2.5f);
                    break;
            }
        }
    }
}
