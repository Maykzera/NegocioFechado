using UnityEngine;

/// <summary>Porta que abre e fecha girando nas dobradiças.</summary>
public class Door : MonoBehaviour, IInteractable
{
    [SerializeField] private float openAngle = 100f;
    [SerializeField] private float speed = 220f;
    [SerializeField] private bool locked;
    [SerializeField] private string lockedMessage = "Fechado";

    private Quaternion closedRotation;
    private bool open;

    public bool IsOpen => open;
    public bool Locked { get => locked; set => locked = value; }
    public string LockedMessage { get => lockedMessage; set => lockedMessage = value; }

    public string InteractionPrompt => locked ? lockedMessage : open ? "Fechar porta" : "Abrir porta";

    private void Awake()
    {
        closedRotation = transform.localRotation;
    }

    public bool CanInteract(InteractionController interactor) => true;

    public void Interact(InteractionController interactor)
    {
        if (locked)
        {
            GameUI.Toast(lockedMessage);
            return;
        }
        open = !open;
    }

    private void Update()
    {
        Quaternion target = open ? closedRotation * Quaternion.Euler(0f, openAngle, 0f) : closedRotation;
        transform.localRotation = Quaternion.RotateTowards(transform.localRotation, target, speed * Time.deltaTime);
    }
}
