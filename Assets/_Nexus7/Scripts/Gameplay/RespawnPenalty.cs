using System;
using Nexus7.Inventory;

namespace Nexus7.Gameplay
{
    /// <summary>
    /// Castigo justo al caer (lógica pura):
    ///   · se conservan SIEMPRE los dispositivos fabricados, las herramientas y los objetos clave;
    ///   · de los recursos menores (piezas, baterías, componentes) se pierde el 25 % de cada montón, redondeando hacia abajo;
    ///   · en la Fase 6, −15 s de cuenta atrás, sin dejarla nunca por debajo de 60 s;
    ///   · los checkpoints no se guardan mientras te persiguen.
    /// </summary>
    public static class RespawnPenalty
    {
        public const float LossFraction = 0.25f;
        public const float CountdownSeconds = 15f;
        public const float CountdownFloor = 60f;

        public static bool IsProtected(ItemCategory category, bool isKeyItem) =>
            isKeyItem || category == ItemCategory.Device || category == ItemCategory.Tool;

        public static int LostAmount(int quantity, ItemCategory category, bool isKeyItem)
        {
            if (quantity < 2 || IsProtected(category, isKeyItem)) return 0;
            return (int)Math.Floor(quantity * LossFraction);
        }

        /// <summary>Segundos que se restan a la cuenta atrás.</summary>
        public static float CountdownPenalty(float remaining) =>
            Math.Max(0f, Math.Min(CountdownSeconds, remaining - CountdownFloor));

        public static bool CanActivateCheckpoint(bool beingChased) => !beingChased;
    }
}
