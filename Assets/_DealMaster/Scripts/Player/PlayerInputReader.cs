using UnityEngine;

/// <summary>
/// Lê os comandos do jogador em PC (teclado e mouse) e Android (controles touch).
/// Usa o Input Manager clássico: em Project Settings > Player, deixe
/// "Active Input Handling" em "Both" (ou "Input Manager (Old)").
/// </summary>
[DefaultExecutionOrder(-100)]
public class PlayerInputReader : MonoBehaviour
{
    [SerializeField] private float mouseSensitivity = 2.0f;
    [SerializeField] private TouchControls touchControls;

    public Vector2 Move { get; private set; }
    public Vector2 Look { get; private set; }
    public bool Run { get; private set; }
    public bool CrouchPressed { get; private set; }
    public bool InteractPressed { get; private set; }
    public bool InventoryPressed { get; private set; }

    /// <summary>Quando uma tela (loja, negociação, embalagem) está aberta, o personagem não se move.</summary>
    public bool Blocked => GameUI.IsModalOpen;

    private void Awake()
    {
        if (touchControls == null) touchControls = FindObjectOfType<TouchControls>();
    }

    private void Update()
    {
        Move = Vector2.zero;
        Look = Vector2.zero;
        Run = false;
        CrouchPressed = false;
        InteractPressed = false;
        InventoryPressed = Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.I);

        if (touchControls != null && touchControls.IsActive)
            InventoryPressed |= touchControls.ConsumeInventory();

        if (Blocked)
        {
            if (touchControls != null) touchControls.ConsumeInteract();
            return;
        }

        // Teclado e mouse
        Vector2 keys = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        bool cursorLocked = Cursor.lockState == CursorLockMode.Locked;
        Vector2 mouse = cursorLocked
            ? new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * mouseSensitivity
            : Vector2.zero;

        Move = Vector2.ClampMagnitude(keys, 1f);
        Look = mouse;
        Run = Input.GetKey(KeyCode.LeftShift);
        CrouchPressed = Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.LeftControl);
        InteractPressed = Input.GetKeyDown(KeyCode.E) || (cursorLocked && Input.GetMouseButtonDown(0));

        // Touch
        if (touchControls != null && touchControls.IsActive)
        {
            if (touchControls.MoveVector.sqrMagnitude > Move.sqrMagnitude) Move = touchControls.MoveVector;
            Look += touchControls.ConsumeLookDelta();
            Run |= touchControls.MoveVector.magnitude > 0.95f;
            InteractPressed |= touchControls.ConsumeInteract();
            CrouchPressed |= touchControls.ConsumeCrouch();
        }
    }
}
