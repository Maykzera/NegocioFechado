/// <summary>Qualquer objeto do mundo com que o jogador pode interagir (porta, balcão, bancada, NPC...).</summary>
public interface IInteractable
{
    /// <summary>Texto exibido quando o jogador mira o objeto, ex.: "Abrir porta".</summary>
    string InteractionPrompt { get; }

    bool CanInteract(InteractionController interactor);

    void Interact(InteractionController interactor);
}
