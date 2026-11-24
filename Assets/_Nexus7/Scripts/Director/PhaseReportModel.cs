using System;
using System.Collections.Generic;

namespace Nexus7.Director
{
    /// <summary>Cómo ha jugado la fase, según el analizador.</summary>
    public enum PlayStyle { Ghost, Sharpshooter, Aggressive, Entrenched, Struggling, Balanced }

    /// <summary>
    /// Informe de la IA de una fase (puro, probado). GameDirector lo va rellenando mientras juegas y al salir de la fase
    /// lo guarda en DirectorSession; la fase siguiente lo enseña unos segundos (PhaseReportPanel).
    /// Cuenta toda la fase, no solo los últimos 60 s del panel F3. El estilo se decide por este orden:
    ///   Fantasma (0 disparos y ≤ 1 detección) · En apuros (≥ 2 vidas perdidas) · Atrincherado (acampada media ≥ 50 %) ·
    ///   Tirador (≥ 3 bajas y precisión ≥ 45 %) · Agresivo (≥ 2 bajas/min) · Equilibrado.
    /// </summary>
    public sealed class PhaseReportModel
    {
        public const float MinDuration = 20f;
        public const int StruggleDeaths = 2, SharpKills = 3;
        public const float EntrenchedCamp = 0.5f, SharpAccuracy = 0.45f, AggressiveKpm = 2f, OneWeaponShare = 0.7f;

        public string PhaseTitle { get; }
        public float Duration { get; private set; }
        public int Shots { get; private set; }
        public int Hits => hitShots.Count;
        public int Kills { get; private set; }
        public int Deaths { get; private set; }
        public int Detections { get; private set; }
        public float DamageTaken { get; private set; }
        /// <summary>Vidas perdidas (capturas y caídas en combate).</summary>
        public int LivesLost { get; private set; }
        public int Flushes { get; private set; }
        public int Adaptations { get; private set; }
        public int Assists { get; private set; }
        public int ShipEvents { get; private set; }
        public int ModRewards { get; private set; }
        public int HeatGuards { get; private set; }
        public float TensionMax { get; private set; }

        private readonly HashSet<int> hitShots = new HashSet<int>();
        private readonly Dictionary<string, int> weaponShots = new Dictionary<string, int>();
        private float tensionSum, campSum;
        private int samples;

        public PhaseReportModel(string phaseTitle) { PhaseTitle = phaseTitle ?? ""; }

        public void RecordShot(string weaponId)
        {
            Shots++;
            if (string.IsNullOrEmpty(weaponId)) return;
            weaponShots.TryGetValue(weaponId, out int n);
            weaponShots[weaponId] = n + 1;
        }

        /// <summary>Un impacto (los perdigones de una escopeta comparten disparo: cuenta una vez).</summary>
        public void RecordHit(int shotId) => hitShots.Add(shotId);
        public void RecordKill() => Kills++;
        public void RecordDeath() => Deaths++;
        public void RecordLifeLost() => LivesLost++;
        public void RecordDetected() => Detections++;
        public void RecordDamage(float amount) { if (amount > 0f) DamageTaken += amount; }
        public void RecordFlush() => Flushes++;
        public void RecordAdaptation() => Adaptations++;
        public void RecordAssist() => Assists++;
        public void RecordShipEvent() => ShipEvents++;
        public void RecordModReward() => ModRewards++;
        public void RecordHeatGuard() => HeatGuards++;

        /// <summary>Muestra periódica (cada tick del director): tensión y acampada en [0, 1].</summary>
        public void Sample(float dt, float tension, float camping)
        {
            if (dt <= 0f) return;
            Duration += dt;
            tensionSum += tension;
            campSum += camping;
            samples++;
            if (tension > TensionMax) TensionMax = tension;
        }

        /// <summary>Aciertos / disparos (o −1 si no ha disparado).</summary>
        public float Accuracy => Shots > 0 ? Math.Min(1f, (float)Hits / Shots) : -1f;
        public float AverageTension => samples > 0 ? tensionSum / samples : 0f;
        public float AverageCamping => samples > 0 ? campSum / samples : 0f;
        public float KillsPerMinute => Duration > 0f ? Kills / (Duration / 60f) : 0f;
        /// <summary>Hay datos suficientes para que el informe diga algo.</summary>
        public bool Meaningful => Duration >= MinDuration;

        /// <summary>El arma más usada y su parte de los disparos (0 si no ha disparado).</summary>
        public float DominantWeapon(out string weaponId)
        {
            weaponId = null;
            int best = 0;
            foreach (var kv in weaponShots)
                if (kv.Value > best || (kv.Value == best && string.CompareOrdinal(kv.Key, weaponId) < 0)) { best = kv.Value; weaponId = kv.Key; }
            return Shots > 0 ? (float)best / Shots : 0f;
        }

        public PlayStyle Style
        {
            get
            {
                if (Shots == 0 && Detections <= 1) return PlayStyle.Ghost;
                if (LivesLost >= StruggleDeaths) return PlayStyle.Struggling;
                if (AverageCamping >= EntrenchedCamp) return PlayStyle.Entrenched;
                if (Kills >= SharpKills && Accuracy >= SharpAccuracy) return PlayStyle.Sharpshooter;
                if (KillsPerMinute >= AggressiveKpm) return PlayStyle.Aggressive;
                return PlayStyle.Balanced;
            }
        }

        public static string StyleName(PlayStyle s)
        {
            switch (s)
            {
                case PlayStyle.Ghost: return "Fantasma";
                case PlayStyle.Sharpshooter: return "Tirador";
                case PlayStyle.Aggressive: return "Agresivo";
                case PlayStyle.Entrenched: return "Atrincherado";
                case PlayStyle.Struggling: return "En apuros";
                default: return "Equilibrado";
            }
        }

        /// <summary>Lo que hará la IA en la fase siguiente con lo que ha visto (frase para el jugador).</summary>
        public string Verdict()
        {
            float share = DominantWeapon(out _);
            string weapon = Shots >= 10 && share >= OneWeaponShare ? " Usas casi siempre la misma arma: el blindaje adaptativo lo notará." : "";
            switch (Style)
            {
                case PlayStyle.Ghost: return "Has pasado sin disparar. Las patrullas seguirán su ritmo: el sigilo no se castiga." + weapon;
                case PlayStyle.Struggling: return "Te ha costado. La siguiente fase empieza con menos presión y más ayuda." + weapon;
                case PlayStyle.Entrenched: return "Te atrincheras mucho. Si te quedas quieto, mandaremos unidades a sacarte." + weapon;
                case PlayStyle.Sharpshooter: return "Precisión alta. Los robots afinarán la puntería y dispararán más a la vez." + weapon;
                case PlayStyle.Aggressive: return "Ritmo agresivo. Habrá más tiradores y refuerzos si mantienes el paso." + weapon;
                default: return "Rendimiento equilibrado. La presión se mantiene." + weapon;
            }
        }

        /// <summary>Líneas del panel (sin el título).</summary>
        public List<string> Lines(Func<string, string> weaponName = null)
        {
            var lines = new List<string>();
            int min = (int)(Duration / 60f), sec = (int)(Duration % 60f);
            lines.Add($"Tiempo {min}:{sec:00}   Vidas perdidas {LivesLost}   Muertes en combate {Deaths}");
            float acc = Accuracy;
            lines.Add(acc < 0f ? $"Bajas {Kills}   Sin disparos" : $"Bajas {Kills}   Precisión {acc * 100f:0}% ({Hits}/{Shots})");
            float share = DominantWeapon(out string id);
            string name = id == null ? "—" : weaponName != null ? weaponName(id) ?? id : id;
            lines.Add($"Arma dominante {name}{(id != null ? $" {share * 100f:0}%" : "")}   Daño recibido {DamageTaken:0}");
            lines.Add($"Detecciones {Detections}   Acampada media {AverageCamping * 100f:0}%");
            lines.Add($"Tensión media {AverageTension:0.00} (máx. {TensionMax:0.00})");
            lines.Add($"Acciones de la IA: {Flushes} expulsiones · {Adaptations} blindajes · {Assists} ayudas · {ShipEvents} eventos de nave · {ModRewards} módulos · {HeatGuards} vigilancias");
            lines.Add($"Estilo: {StyleName(Style)}");
            return lines;
        }
    }
}
