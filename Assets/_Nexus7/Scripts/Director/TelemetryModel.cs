using System;
using System.Globalization;
using System.Text;

namespace Nexus7.Director
{
    /// <summary>
    /// Registro de partida (puro, probado en TelemetryTest): una línea CSV por fase con lo que vio el analizador, para
    /// equilibrar con datos reales. Separador «;» (lo abre bien Excel en español) y números con punto decimal; los textos
    /// con «;», comillas o saltos de línea van entre comillas.
    /// </summary>
    public static class TelemetryModel
    {
        public const string FileName = "nexus7_telemetria.csv";
        public const char Separator = ';';

        public static readonly string[] Columns =
        {
            "fecha", "fase", "dificultad", "reinicios", "duracion_s", "vidas_perdidas", "muertes", "bajas", "disparos",
            "aciertos", "precision", "detecciones", "dano_recibido", "arma_dominante", "uso_arma", "acampada_media",
            "tension_media", "tension_max", "expulsiones", "blindajes", "ayudas", "eventos_nave", "modulos", "vigilancias", "estilo",
        };

        public static string Header() => string.Join(Separator.ToString(), Columns);

        public static string Row(PhaseReportModel r, DateTime when, string difficulty, int restarts)
        {
            float share = r.DominantWeapon(out string weapon);
            var f = new object[]
            {
                when.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture), r.PhaseTitle, difficulty, restarts,
                r.Duration, r.LivesLost, r.Deaths, r.Kills, r.Shots, r.Hits, r.Accuracy, r.Detections, r.DamageTaken,
                weapon ?? "", share, r.AverageCamping, r.AverageTension, r.TensionMax, r.Flushes, r.Adaptations, r.Assists,
                r.ShipEvents, r.ModRewards, r.HeatGuards, PhaseReportModel.StyleName(r.Style),
            };
            var sb = new StringBuilder();
            for (int i = 0; i < f.Length; i++)
            {
                if (i > 0) sb.Append(Separator);
                sb.Append(Field(f[i]));
            }
            return sb.ToString();
        }

        /// <summary>Un campo: números con punto y 2 decimales como mucho, textos escapados.</summary>
        public static string Field(object v)
        {
            if (v == null) return "";
            if (v is float fl) return Math.Round(fl, 2).ToString(CultureInfo.InvariantCulture);
            if (v is double d) return Math.Round(d, 2).ToString(CultureInfo.InvariantCulture);
            if (v is int n) return n.ToString(CultureInfo.InvariantCulture);
            string s = v.ToString();
            if (s.IndexOf(Separator) >= 0 || s.IndexOf('"') >= 0 || s.IndexOf('\n') >= 0 || s.IndexOf('\r') >= 0)
                return "\"" + s.Replace("\"", "\"\"") + "\"";
            return s;
        }
    }
}
