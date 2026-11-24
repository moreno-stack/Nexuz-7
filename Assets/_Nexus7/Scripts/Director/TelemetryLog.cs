using System;
using System.IO;
using System.Text;
using UnityEngine;
using Nexus7.Core;
using Nexus7.Gameplay;

namespace Nexus7.Director
{
    /// <summary>
    /// Escribe el registro de partida (TelemetryModel): al salir de cada fase que haya durado 20 s o más, añade una línea a
    /// «nexus7_telemetria.csv» en la carpeta de datos del juego (Application.persistentDataPath). Si el archivo no existe,
    /// lo crea con la cabecera (UTF-8 con BOM, para que Excel lea bien las tildes). Si falla al escribir, solo avisa en la
    /// consola: el juego sigue.
    /// </summary>
    public static class TelemetryLog
    {
        public static string Path => System.IO.Path.Combine(Application.persistentDataPath, TelemetryModel.FileName);
        public static int RowsThisSession { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => RowsThisSession = 0;

        public static void Append(PhaseReportModel report)
        {
            if (report == null || !report.Meaningful) return;
            try
            {
                string path = Path;
                string row = TelemetryModel.Row(report, DateTime.Now, DifficultySettings.Current.ToString(), LivesSystem.RestartsHere);
                if (!File.Exists(path))
                    File.WriteAllText(path, TelemetryModel.Header() + "\n", new UTF8Encoding(true));
                File.AppendAllText(path, row + "\n", new UTF8Encoding(false));
                if (RowsThisSession++ == 0) Debug.Log($"[Nexus-7] Registro de partida: {path}");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Nexus-7] No se pudo escribir el registro de partida: {e.Message}");
            }
        }
    }
}
