namespace Nexus7.Interaction
{
    /// <summary>
    /// Contrato para cualquier cosa con la que el jugador pueda interactuar (puertas, terminales, notas...).
    /// PlayerInteractor busca este interfaz en el objeto que golpea su raycast (o en sus padres).
    /// </summary>
    public interface IInteractable
    {
        /// <summary>Texto que aparece en pantalla al apuntar, p. ej. "Abrir puerta".</summary>
        string Prompt { get; }

        /// <summary>Si devuelve false, el objeto se ignora (no muestra aviso ni se puede usar).</summary>
        bool CanInteract(PlayerInteractor interactor);

        void Interact(PlayerInteractor interactor);
    }
}
