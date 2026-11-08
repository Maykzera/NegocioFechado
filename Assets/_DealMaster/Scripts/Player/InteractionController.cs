using UnityEngine;

/// <summary>Detecta o objeto mirado pela câmera e executa a interação contextual.</summary>
[RequireComponent(typeof(PlayerInputReader))]
public class InteractionController : MonoBehaviour
{
    [SerializeField] private Camera viewCamera;
    [SerializeField] private float reach = 2.6f;
    [SerializeField] private LayerMask mask = ~0;

    private PlayerInputReader input;

    public IInteractable Current { get; private set; }
    public string CurrentPrompt => Current != null && Current.CanInteract(this) ? Current.InteractionPrompt : null;
    public PlayerController Player { get; private set; }

    private void Awake()
    {
        input = GetComponent<PlayerInputReader>();
        Player = GetComponent<PlayerController>();
        if (viewCamera == null) viewCamera = Camera.main;
    }

    private void Update()
    {
        Current = null;
        if (viewCamera == null || GameUI.IsModalOpen) return;

        // Mira pelo centro da tela (no PC é a mira; no celular, o centro da visão).
        Ray ray = viewCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, reach, mask, QueryTriggerInteraction.Collide))
            Current = hit.collider.GetComponentInParent<IInteractable>();

        if (Current != null && input.InteractPressed && Current.CanInteract(this))
            Current.Interact(this);
    }
}
