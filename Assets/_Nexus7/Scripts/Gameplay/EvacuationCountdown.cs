using System;
using UnityEngine;
using Nexus7.Core;
using Nexus7.Environment;
using Nexus7.UI;

namespace Nexus7.Gameplay
{
    /// <summary>
    /// Cuenta atrás de la autodestrucción. Tres fases de tensión:
    ///   Normal   (> warningAt)  · Aviso (≤ warningAt) · Crítica (≤ criticalAt)
    /// En cada fase aumenta la vibración de cámara, se activan luces/humo de emergencia y la IA avisa.
    /// A cero → EndingManager.Fail(timeOut: true) → final crítico.
    ///
    /// Colocar en: GameSystems de la escena final.
    /// </summary>
    public class EvacuationCountdown : MonoBehaviour
    {
        public enum Phase { Normal, Warning, Critical }

        [SerializeField] private float duration = 300f;
        [SerializeField] private float warningAt = 120f;
        [SerializeField] private float criticalAt = 60f;
        [SerializeField] private CameraShake cameraShake;
        [Tooltip("Objetos que se ACTIVAN al entrar en fase crítica (estroboscopios, humo extra...).")]
        [SerializeField] private GameObject[] enableOnCritical;
        [Tooltip("Objetos que se activan en fase de aviso.")]
        [SerializeField] private GameObject[] enableOnWarning;
        [Tooltip("La autodestrucción pone la nave en Alerta Amarilla al empezar (si estaba en Normal).")]
        [SerializeField] private bool raiseAlertToYellow = true;

        public float Remaining { get; private set; }
        public float Duration => duration;
        public Phase CurrentPhase { get; private set; } = Phase.Normal;
        public bool Running { get; private set; }

        public event Action<Phase> PhaseChanged;

        private readonly int[] announcements = { 240, 180, 120, 90, 60, 30, 10 };
        private int nextAnnouncement;

        private void Start()
        {
            Remaining = duration;
            Running = true;
            if (raiseAlertToYellow && AlertSystem.Level == AlertLevel.Normal) AlertSystem.ForceLevel(AlertLevel.Yellow);
            SetActive(enableOnWarning, false);
            SetActive(enableOnCritical, false);
            while (nextAnnouncement < announcements.Length && announcements[nextAnnouncement] >= duration) nextAnnouncement++;
        }

        public void Stop() => Running = false;

        private void Update()
        {
            if (!Running) return;
            Remaining = Mathf.Max(0f, Remaining - Time.deltaTime);

            // Vibración continua creciente
            if (cameraShake != null)
            {
                float k = 1f - Remaining / duration;
                cameraShake.SetBaseTrauma(CurrentPhase == Phase.Critical ? 0.35f : CurrentPhase == Phase.Warning ? 0.15f : 0.04f + 0.06f * k);
            }

            var phase = Remaining <= criticalAt ? Phase.Critical : Remaining <= warningAt ? Phase.Warning : Phase.Normal;
            if (phase != CurrentPhase)
            {
                CurrentPhase = phase;
                if (phase == Phase.Warning) SetActive(enableOnWarning, true);
                if (phase == Phase.Critical) SetActive(enableOnCritical, true);
                PhaseChanged?.Invoke(phase);
            }

            if (nextAnnouncement < announcements.Length && Remaining <= announcements[nextAnnouncement])
            {
                int s = announcements[nextAnnouncement++];
                if (HUDManager.Instance != null)
                    HUDManager.Instance.ShowMessageNow(s >= 60
                        ? $"IA NEXUS-7: Autodestrucción en {s / 60} minuto{(s >= 120 ? "s" : "")}{(s % 60 != 0 ? $" y {s % 60} segundos" : "")}."
                        : $"IA NEXUS-7: ¡{s} segundos para la autodestrucción!", 2.5f);
            }

            if (Remaining <= 0f)
            {
                Running = false;
                if (EndingManager.Instance != null) EndingManager.Instance.Fail(true);
            }
        }

        private static void SetActive(GameObject[] objects, bool value)
        {
            if (objects == null) return;
            foreach (var go in objects) if (go != null) go.SetActive(value);
        }
    }
}
