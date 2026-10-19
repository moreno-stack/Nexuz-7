using System;
using System.Collections.Generic;
using UnityEngine;
using Nexus7.Interaction;
using Nexus7.UI;

namespace Nexus7.Security
{
    public enum ConsoleActionType
    {
        ShowInfo,        // consultar el estado de la nave / información del entorno
        DisableSector,   // desactivar temporalmente un sector de seguridad completo
        UnlockDoor,      // autorizar una compuerta blindada (LockdownDoor)
        ReduceAlert,     // reiniciar el protocolo de alerta (baja el calor)
        CompletePhase    // acción final: retomar el control de la nave
    }

    /// <summary>Una opción del menú de una consola. Configurable desde el Inspector.</summary>
    [Serializable]
    public class ConsoleAction
    {
        public string label = "Acción";
        public ConsoleActionType type = ConsoleActionType.ShowInfo;
        [Tooltip("Sin hackeo = Easy con 'requiresHack' desactivado.")]
        public bool requiresHack = true;
        public HackDifficulty difficulty = HackDifficulty.Medium;
        [Tooltip("DisableSector: sector afectado.")]
        public SecuritySector sector;
        [Tooltip("DisableSector: segundos (se reducen con la alerta).")]
        public float duration = 40f;
        [Tooltip("UnlockDoor: compuerta blindada.")]
        public LockdownDoor door;
        [Tooltip("ReduceAlert: calor que se resta.")]
        public float heatReduction = 40f;
        [Tooltip("CompletePhase: escena siguiente (vacío = mensaje de fin).")]
        public string nextScene;
        [TextArea(2, 6)] public string infoText;
        [Tooltip("Solo se puede usar una vez con éxito.")]
        public bool oneShot;

        [NonSerialized] public bool used;
    }

    /// <summary>
    /// Consola del Centro de Control: al interactuar abre un menú (ConsoleUI) con sus acciones.
    /// Las acciones con hackeo lanzan el minijuego (HackMinigameUI): si fallas, la consola se BLOQUEA
    /// unos segundos y la nave sube su nivel de alerta.
    ///
    /// Colocar en: la consola (objeto con collider o su raíz).
    /// </summary>
    public class ControlConsole : Interactable
    {
        [SerializeField] private string consoleName = "Consola";
        [SerializeField] private List<ConsoleAction> actions = new List<ConsoleAction>();
        [SerializeField] private float lockoutTime = 10f;
        [SerializeField] private Renderer screenRenderer;

        private float lockedUntil = -999f;

        public string ConsoleName => consoleName;
        public IReadOnlyList<ConsoleAction> Actions => actions;
        public bool IsLockedOut => Time.time < lockedUntil;
        public float LockoutRemaining => Mathf.Max(0f, lockedUntil - Time.time);

        public override string Prompt => IsLockedOut ? $"{consoleName} (bloqueada {Mathf.CeilToInt(LockoutRemaining)} s)" : $"Usar {consoleName}";
        public override bool CanInteract(PlayerInteractor interactor) => !IsLockedOut && base.CanInteract(interactor);

        protected override void OnInteract(PlayerInteractor interactor)
        {
            if (ConsoleUI.Instance != null) ConsoleUI.Instance.Open(this, interactor);
        }

        /// <summary>Tras un hackeo fallido.</summary>
        public void LockOut() => lockedUntil = Time.time + lockoutTime;

        private void Update()
        {
            if (screenRenderer == null) return;
            Color c = IsLockedOut ? (Mathf.Repeat(Time.time * 3f, 1f) < 0.5f ? Color.red : Color.black) : new Color(0.3f, 0.8f, 1f);
            screenRenderer.material.SetColor("_EmissionColor", c * 1.8f);
        }
    }
}
