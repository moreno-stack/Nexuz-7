using System.Linq;
using System.Text;
using UnityEngine;
using Nexus7.Core;
using Nexus7.Enemies;
using Nexus7.Security;

namespace Nexus7.UI
{
    /// <summary>
    /// Genera el informe "Estado de la nave" que muestran las consolas: nivel de alerta, historial de
    /// incidentes de TODA la partida, sectores de seguridad, compuertas y drones activos.
    /// </summary>
    public static class ShipStatusReport
    {
        public static string LevelName(AlertLevel l) =>
            l == AlertLevel.Red ? "ALERTA ROJA" : l == AlertLevel.Yellow ? "ALERTA AMARILLA" : "NORMAL";

        public static string LevelHex(AlertLevel l) =>
            l == AlertLevel.Red ? "#FF4A3A" : l == AlertLevel.Yellow ? "#FFC23A" : "#66D9FF";

        public static string Build(string extra = null)
        {
            var s = AlertSystem.Stats;
            var sb = new StringBuilder();
            sb.AppendLine($"<b>ESTADO DE LA NAVE</b>   <color={LevelHex(AlertSystem.Level)}>{LevelName(AlertSystem.Level)}</color>  " +
                          $"<size=75%>(tensión {AlertSystem.Heat:0}/100)</size>\n");

            sb.AppendLine("<color=#7FD4FF>REGISTRO DE INCIDENTES</color>");
            sb.AppendLine($"  Alarmas activadas: {s.alarms}     Detecciones: {s.spotted}     Capturas: {s.captures}");
            sb.AppendLine($"  Hackeos fallidos: {s.hackFailures} (errores: {s.hackErrors})     Detonaciones PEM: {s.deviceDetonations}");
            sb.AppendLine($"  Ruido acumulado: {s.loudNoise:0.0}     Alertas rojas: {s.redAlerts}     Pico de tensión: {s.peakHeat:0}\n");

            sb.AppendLine("<color=#7FD4FF>SECTORES DE SEGURIDAD</color>");
            foreach (var sector in SecuritySector.All)
            {
                string state = sector.IsDisabled
                    ? $"<color=#66FF99>DESACTIVADO ({Mathf.CeilToInt(sector.RemainingTime)} s)</color>"
                    : "<color=#FF8A7A>ACTIVO</color>";
                sb.AppendLine($"  {sector.SectorName}: {state}  <size=75%>({sector.SystemCount} sistemas)</size>");
            }

            int elite = EnemyPatrol.All.Count;
            int stunned = EnemyPatrol.All.Count(e => e.CurrentState == EnemyPatrol.EnemyState.Stunned);
            sb.AppendLine($"\n<color=#7FD4FF>UNIDADES</color>  Drones activos: {elite - stunned}   Fuera de servicio: {stunned}");

            if (!string.IsNullOrEmpty(extra)) sb.AppendLine($"\n<color=#7FD4FF>NOTAS</color>\n{extra}");
            return sb.ToString();
        }
    }
}
