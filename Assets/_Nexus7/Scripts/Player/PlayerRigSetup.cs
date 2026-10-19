using System;
using Nexus7.Characters;
using Nexus7.Inventory;

namespace Nexus7.Player
{
    /// <summary>
    /// Componentes que lleva el jugador común (todas las fases), en el orden en que se añaden.
    /// Lo usa SalaInicialBuilder.BuildPlayer: cada dependencia de [RequireComponent] va antes que quien la necesita
    /// (InventorySystem antes que Hotbar, PlayerController antes que PlayerHealth...), así Unity no añade copias.
    /// Las referencias entre ellos se serializan en el constructor; no dependen del orden de Awake.
    /// </summary>
    public static class PlayerRigSetup
    {
        public static readonly Type[] Required =
        {
            typeof(PlayerController),
            typeof(PlayerLook),
            typeof(SuitShield),
            typeof(PlayerHealth),
            typeof(InventorySystem),
            typeof(Hotbar),
            typeof(HeldItemView),
            typeof(ProceduralAnimator),
            typeof(PlayerRigDriver),
            typeof(CameraModeSwitcher),
            typeof(PlayerSuit),
        };
    }
}
