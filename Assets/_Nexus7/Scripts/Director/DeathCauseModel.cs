using System.Collections.Generic;

namespace Nexus7.Director
{
    /// <summary>Cómo has caído.</summary>
    public enum DeathKind { Caught, KnockedOut, Hazard }

    /// <summary>El consejo que da el analizador (uno por caída, el más útil).</summary>
    public enum DeathTip { Hazard, Running, Light, Outnumbered, UnderFire, Noise, Unseen, FarSight, Ambush, Route }

    /// <summary>
    /// Lo que pasó antes de caer. El estado del jugador (luz, sombra, agachado, corriendo, ruido) es el del momento en que
    /// le vieron; si nadie le vio, el del momento de caer.
    /// </summary>
    public struct DeathFacts
    {
        public DeathKind Kind;
        /// <summary>Algún robot te vio (empezó a perseguirte) en los últimos DeathCauseModel.Memory segundos.</summary>
        public bool Spotted;
        /// <summary>Quién te vio el último, con artículo ("un dron de seguridad").</summary>
        public string Spotter;
        public float SpotDistance, SecondsSinceSpot;
        /// <summary>Robots distintos que te vieron en la ventana.</summary>
        public int Spotters;
        public bool Lit, Shadow, Crouching, Running;
        /// <summary>Radio de ruido de tus pasos (m).</summary>
        public float Noise;
        /// <summary>Daño recibido en la ventana.</summary>
        public float DamageTaken;
    }

    /// <summary>Resultado: lo que se enseña en la tarjeta al reaparecer.</summary>
    public sealed class DeathAnalysis
    {
        public DeathKind Kind;
        public DeathTip Tip;
        public string Title, Seen, TipText;
        public readonly List<string> Factors = new List<string>();
    }

    /// <summary>
    /// Análisis de la caída (puro, probado en DeathCauseTest): al reaparecer, la IA te cuenta qué pasó y te da un consejo.
    /// El consejo se elige por este orden (el primero que se cumple):
    ///   Peligro de la nave · Corrías cuando te vieron · Estabas bajo luz directa · Te vieron 2 o más robots ·
    ///   Te dejaron fuera de combate a tiros/golpes · Nadie te vio pero hacías ruido (≥ 6 m) · Nadie te vio ·
    ///   Te vieron desde lejos (≥ 12 m) · Te vieron de cerca (≤ 5 m) · Lo demás: estudia la ruta.
    /// </summary>
    public static class DeathCauseModel
    {
        public const float Memory = 20f, FarSpot = 12f, CloseSpot = 5f, LoudNoise = 6f, CardTime = 9f;

        public static DeathAnalysis Analyze(DeathFacts f)
        {
            var a = new DeathAnalysis { Kind = f.Kind, Title = Title(f.Kind) };
            a.Seen = f.Kind == DeathKind.Hazard ? "Una descarga de la nave te ha dejado sin vida."
                   : f.Spotted ? $"Te vio {(string.IsNullOrEmpty(f.Spotter) ? "un robot" : f.Spotter)} a {f.SpotDistance:0} m, {f.SecondsSinceSpot:0} s antes de caer."
                   : "Ningún robot te persiguió antes: te encontraron de cerca.";

            if (f.Lit) a.Factors.Add("bajo luz directa (te ven un 30 % más lejos)");
            if (f.Running) a.Factors.Add("corriendo (más ruido y te ven un 30 % más lejos)");
            if (f.Shadow && f.Crouching) a.Factors.Add("agachado en la sombra (te ven un 40 % menos)");
            else if (f.Crouching) a.Factors.Add("agachado");
            if (f.Noise >= LoudNoise) a.Factors.Add($"haciendo ruido ({f.Noise:0} m)");
            if (f.Spotters >= 2) a.Factors.Add($"te vieron {f.Spotters} robots");
            if (f.DamageTaken > 0f) a.Factors.Add($"{f.DamageTaken:0} de daño recibido");

            a.Tip = ChooseTip(f);
            a.TipText = TipText(a.Tip);
            return a;
        }

        public static DeathTip ChooseTip(DeathFacts f)
        {
            if (f.Kind == DeathKind.Hazard) return DeathTip.Hazard;
            if (f.Spotted && f.Running) return DeathTip.Running;
            if (f.Spotted && f.Lit) return DeathTip.Light;
            if (f.Spotters >= 2) return DeathTip.Outnumbered;
            if (f.Kind == DeathKind.KnockedOut) return DeathTip.UnderFire;
            if (!f.Spotted) return f.Noise >= LoudNoise ? DeathTip.Noise : DeathTip.Unseen;
            if (f.SpotDistance >= FarSpot) return DeathTip.FarSight;
            if (f.SpotDistance <= CloseSpot) return DeathTip.Ambush;
            return DeathTip.Route;
        }

        public static string Title(DeathKind k)
        {
            switch (k)
            {
                case DeathKind.Caught: return "TE HAN ATRAPADO";
                case DeathKind.KnockedOut: return "FUERA DE COMBATE";
                default: return "ACCIDENTE";
            }
        }

        public static string TipText(DeathTip t)
        {
            switch (t)
            {
                case DeathTip.Hazard: return "Las descargas eléctricas chisporrotean antes de dañar: busca cómo cortarlas o espera a que se apaguen para pasar.";
                case DeathTip.Running: return "Corre solo cuando nadie mire: corriendo te oyen antes y te ven desde más lejos.";
                case DeathTip.Light: return "Evita la luz directa: cruza por las zonas oscuras y agáchate en ellas.";
                case DeathTip.Outnumbered: return "Te vieron varios a la vez: rompe la línea de visión tras una pared antes de que te rodeen.";
                case DeathTip.UnderFire: return "Te quedaste al descubierto en combate: dispara desde una cobertura y recarga detrás de ella.";
                case DeathTip.Noise: return "Te encontraron por el ruido: agáchate al acercarte y usa señuelos para apartarlos.";
                case DeathTip.Unseen: return "Te sorprendieron sin perseguirte: escucha los pasos y asómate a las esquinas antes de doblar.";
                case DeathTip.FarSight: return "Te vieron desde lejos: cruza los pasillos largos agachado y pegado a los bordes.";
                case DeathTip.Ambush: return "Te vieron de cerca: espera a que la patrulla se dé la vuelta antes de moverte.";
                default: return "Observa la ruta de la patrulla antes de cruzar: el panel [F3] dice dónde vigila la IA.";
            }
        }

        /// <summary>Línea corta para el panel F3.</summary>
        public static string Summary(DeathAnalysis a) =>
            a == null ? "Última caída: ninguna" : $"Última caída: {a.Title.ToLowerInvariant()} · consejo: {TipName(a.Tip)}";

        public static string TipName(DeathTip t)
        {
            switch (t)
            {
                case DeathTip.Hazard: return "cuidado con las descargas";
                case DeathTip.Running: return "no correr a la vista";
                case DeathTip.Light: return "evitar la luz";
                case DeathTip.Outnumbered: return "romper la línea de visión";
                case DeathTip.UnderFire: return "cubrirse";
                case DeathTip.Noise: return "menos ruido";
                case DeathTip.Unseen: return "mirar las esquinas";
                case DeathTip.FarSight: return "pasillos largos";
                case DeathTip.Ambush: return "esperar el giro";
                default: return "estudiar la ruta";
            }
        }
    }
}
