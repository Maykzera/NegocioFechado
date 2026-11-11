using UnityEngine;

/// <summary>Mesa do jogador: sentar e usar o computador.</summary>
public class PlayerDesk : MonoBehaviour, IInteractable
{
    [SerializeField] private Transform seatPoint;
    [SerializeField] private Transform monitor;

    public Transform SeatPoint { get => seatPoint; set => seatPoint = value; }
    public Transform Monitor { get => monitor; set => monitor = value; }

    public string InteractionPrompt => GameServices.Game?.ActiveCall != null ? "Voltar para a ligação" : "Sentar e usar o computador";

    public bool CanInteract(InteractionController interactor) => GameServices.IsReady;

    public void Interact(InteractionController interactor)
    {
        var player = interactor.Player;
        if (player != null && seatPoint != null)
        {
            Vector3 look = (monitor != null ? monitor.position : seatPoint.position + seatPoint.forward) - seatPoint.position;
            player.Teleport(seatPoint.position, Quaternion.LookRotation(new Vector3(look.x, 0, look.z)).eulerAngles.y);
            player.SetPitch(12f);
        }
        GameUI.Open(new ComputerPanel());
    }
}
