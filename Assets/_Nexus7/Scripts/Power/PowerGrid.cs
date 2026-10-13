using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Nexus7.UI;

namespace Nexus7.Power
{
    public enum PowerState { Offline, Partial, Restored }

    /// <summary>
    /// Red eléctrica de la zona (una por escena). Suma la salida de todas las fuentes activas
    /// (PowerSource: zócalos de batería, interruptores, reactor) y publica el nivel:
    ///
    ///   CurrentPower / MaxPower  →  Offline (0) · Partial (1..max-1) · Restored (max)
    ///
    /// Quién escucha:
    ///   PowerConsumer  → desbloquea puertas a partir de cierto nivel
    ///   PoweredLight   → color/intensidad de cada luz según el estado
    ///   ElectricHazard → trampas que solo se activan con energía
    ///   UI             → indicador de energía y objetivo dinámico
    /// Además interpola la luz ambiental y la niebla de la escena según el estado.
    ///
    /// Colocar en: un GameObject "PowerGrid" (o "GameSystems") de la escena.
    /// </summary>
    [DefaultExecutionOrder(-500)]
    public class PowerGrid : MonoBehaviour
    {
        public static PowerGrid Instance { get; private set; }

        [SerializeField, Min(1)] private int maxPower = 4;

        [Header("Ambiente según el estado")]
        [SerializeField] private bool controlAmbient = true;
        [SerializeField] private Color offlineAmbient = new Color(0.06f, 0.012f, 0.01f);
        [SerializeField] private Color partialAmbient = new Color(0.06f, 0.045f, 0.03f);
        [SerializeField] private Color restoredAmbient = new Color(0.1f, 0.11f, 0.13f);
        [SerializeField] private Color offlineFog = new Color(0.03f, 0.005f, 0.005f);
        [SerializeField] private Color partialFog = new Color(0.035f, 0.025f, 0.015f);
        [SerializeField] private Color restoredFog = new Color(0.03f, 0.04f, 0.05f);
        [SerializeField] private float ambientTransitionSpeed = 0.8f;

        [Header("Mensajes y objetivo")]
        [Tooltip("Mensaje de la IA al alcanzar cada nivel (índice = nivel de energía).")]
        [SerializeField, TextArea(1, 3)] private string[] messageByPowerLevel;
        [Tooltip("Generar el texto del objetivo con las fuentes que faltan por activar.")]
        [SerializeField] private bool updateObjective = true;
        [SerializeField] private string objectivePrefix = "Objetivo: Restaura la energía";
        [SerializeField] private string completedObjective = "Objetivo: Energía restaurada. Sal por la compuerta principal.";

        private readonly List<PowerSource> sources = new List<PowerSource>();

        public int CurrentPower { get; private set; }
        public int MaxPower => maxPower;
        public float Level => (float)CurrentPower / maxPower;
        public PowerState State => CurrentPower <= 0 ? PowerState.Offline
                                 : CurrentPower >= maxPower ? PowerState.Restored
                                 : PowerState.Partial;

        /// <summary>(energía actual, energía máxima)</summary>
        public event Action<int, int> PowerChanged;

        /// <summary>Atajo seguro: 0 si no hay red en la escena.</summary>
        public static int Power => Instance != null ? Instance.CurrentPower : 0;
        public static PowerState CurrentState => Instance != null ? Instance.State : PowerState.Offline;

        private void Awake()
        {
            Instance = this;
            if (controlAmbient)
            {
                RenderSettings.ambientLight = offlineAmbient;
                RenderSettings.fogColor = offlineFog;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start() => UpdateObjective();

        public void Register(PowerSource source)
        {
            if (source == null || sources.Contains(source)) return;
            sources.Add(source);
            Recalculate();
            UpdateObjective();
        }

        public void Unregister(PowerSource source)
        {
            if (sources.Remove(source)) Recalculate();
        }

        /// <summary>Lo llaman las fuentes al activarse/desactivarse.</summary>
        public void Recalculate()
        {
            int newPower = Mathf.Min(maxPower, sources.Where(s => s != null && s.IsActive).Sum(s => s.Output));
            if (newPower == CurrentPower) return;

            int old = CurrentPower;
            CurrentPower = newPower;
            PowerChanged?.Invoke(CurrentPower, maxPower);

            var hud = HUDManager.Instance;
            if (hud != null && newPower > old && messageByPowerLevel != null && newPower < messageByPowerLevel.Length
                && !string.IsNullOrEmpty(messageByPowerLevel[newPower]))
            {
                hud.QueueMessage(messageByPowerLevel[newPower]);
            }
            UpdateObjective();
        }

        private void UpdateObjective()
        {
            var hud = HUDManager.Instance;
            if (!updateObjective || hud == null) return;

            if (CurrentPower >= maxPower)
            {
                hud.SetObjective(completedObjective);
                return;
            }
            var pending = sources.Where(s => s != null && !s.IsActive && !string.IsNullOrEmpty(s.ObjectiveLabel))
                                 .Select(s => s.ObjectiveLabel);
            hud.SetObjective($"{objectivePrefix} ({CurrentPower}/{maxPower}): {string.Join(" · ", pending)}");
        }

        private void Update()
        {
            if (!controlAmbient) return;
            Color ambient, fog;
            switch (State)
            {
                case PowerState.Restored: ambient = restoredAmbient; fog = restoredFog; break;
                case PowerState.Partial:  ambient = Color.Lerp(partialAmbient, restoredAmbient, Level * 0.5f); fog = partialFog; break;
                default:                  ambient = offlineAmbient;  fog = offlineFog; break;
            }
            float k = ambientTransitionSpeed * Time.deltaTime;
            RenderSettings.ambientLight = Color.Lerp(RenderSettings.ambientLight, ambient, k);
            RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, fog, k);
        }
    }
}
