namespace Nexus7.Security
{
    /// <summary>
    /// Cualquier sistema de seguridad que un HackTerminal puede desactivar temporalmente
    /// (cámaras, barreras eléctricas... y en el futuro torretas, puertas de seguridad, etc.).
    /// </summary>
    public interface IHackable
    {
        /// <summary>Nombre para la UI: "Barrera eléctrica", "Cámara de seguridad"...</summary>
        string HackLabel { get; }
        bool IsDisabled { get; }
        float RemainingDisabledTime { get; }
        void Disable(float duration);
    }
}
