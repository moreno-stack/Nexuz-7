using System;

namespace Nexus7.Inventory
{
    /// <summary>
    /// Red de seguridad de los objetos imprescindibles (pura, probada en KeyItemRescueTest). Cuando una puerta te pide
    /// objetos que no tienes:
    ///   · si en la sala no queda ninguno al que se pueda llegar (recogidos, perdidos, bajo el suelo o dentro de algo),
    ///     el traje fabrica el repuesto que falta y la puerta se abre: nunca te quedas atascado;
    ///   · si quedan, te dice a qué distancia y hacia dónde está el más cercano.
    /// La consola deja una línea «[Botín] Puerta …» con lo que había en la escena, para saber qué pasó.
    /// </summary>
    public static class KeyItemRescue
    {
        /// <summary>Cuántos hay que fabricar: lo que falta después de contar lo que llevas y lo que queda alcanzable.</summary>
        public static int Shortfall(int needed, int have, int reachableInScene) =>
            Math.Max(0, needed - Math.Max(0, have) - Math.Max(0, reachableInScene));

        public static string RescueMessage(string itemName, int amount) =>
            $"TRAJE: no queda ninguna unidad de {itemName.ToLowerInvariant()} a tu alcance. Repuesto de emergencia fabricado (+{amount}).";

        /// <summary>Pista hacia el más cercano (o null si está a menos de 3 m).</summary>
        public static string HintMessage(string itemName, float dx, float dz)
        {
            float d = (float)Math.Sqrt(dx * dx + dz * dz);
            if (d < KeyItemSignal.NearEnough) return $"TRAJE: señal de {itemName.ToLowerInvariant()} a menos de 3 m. Busca su brillo.";
            return $"TRAJE: señal de {itemName.ToLowerInvariant()} más cercana a {d:0} m al {KeyItemSignal.Direction(dx, dz)}. Sigue su haz de luz.";
        }
    }
}
