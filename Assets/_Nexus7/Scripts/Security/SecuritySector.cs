using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Nexus7.Core;

namespace Nexus7.Security
{
    /// <summary>
    /// Agrupa los sistemas de seguridad de una zona (cámaras, láseres, barreras: cualquier IHackable)
    /// para poder desactivarlos TODOS a la vez desde una consola central.
    /// La duración se reduce con el nivel de alerta de la nave.
    ///
    /// Colocar en: un GameObject vacío "Sector_X". En Systems, arrastra los componentes
    /// SecurityCamera / LaserTripwire / ElectricBarrier de esa zona.
    /// </summary>
    public class SecuritySector : MonoBehaviour
    {
        public static readonly List<SecuritySector> All = new List<SecuritySector>();

        [SerializeField] private string sectorName = "Sector";
        [SerializeField] private MonoBehaviour[] systems;

        public string SectorName => sectorName;
        public IEnumerable<IHackable> Systems => systems == null ? Enumerable.Empty<IHackable>() : systems.OfType<IHackable>();
        public int SystemCount => Systems.Count();
        public bool IsDisabled => Systems.Any() && Systems.All(s => s.IsDisabled);
        public float RemainingTime => Systems.Select(s => s.RemainingDisabledTime).DefaultIfEmpty(0f).Min();

        private void OnEnable() => All.Add(this);
        private void OnDisable() => All.Remove(this);

        /// <summary>Desactiva todo el sector. Devuelve la duración real aplicada.</summary>
        public float DisableAll(float baseDuration)
        {
            float duration = baseDuration * AlertSystem.Profile.hackDuration;
            foreach (var s in Systems) s.Disable(duration);
            RunStats.RecordSectorDisabled(gameObject.scene.name + "/" + sectorName);
            return duration;
        }
    }
}
