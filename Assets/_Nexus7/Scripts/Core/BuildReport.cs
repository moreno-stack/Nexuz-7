namespace Nexus7.Core
{
    /// <summary>Texto del resumen de "Nexus-7 → Construir todo el juego" (lógica pura, se prueba fuera de Unity).</summary>
    public static class BuildReport
    {
        public static string Describe(string[] failedPhases)
        {
            if (failedPhases == null || failedPhases.Length == 0) return "Juego construido: 6 fases y menú principal.";
            return "Se construyó el juego con errores en:\n\n· " + string.Join("\n· ", failedPhases) +
                   "\n\nEl resto de fases sí se construyó. Mira el error en la ventana Console (Window → General → Console).";
        }
    }
}
