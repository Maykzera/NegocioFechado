using UnityEngine;

/// <summary>
/// Controles touch para Android: joystick virtual à esquerda, arrastar à direita para olhar,
/// e botões de Interagir, Agachar e Mochila. Também pode ser forçado no PC para testes.
/// </summary>
public class TouchControls : MonoBehaviour
{
    [SerializeField] private bool forceEnable;
    [SerializeField] private float joystickRadius = 90f;
    [SerializeField] private float lookSensitivity = 0.15f;

    private int moveFingerId = -1;
    private int lookFingerId = -1;
    private Vector2 moveOrigin;
    private Vector2 moveCurrent;
    private Vector2 lookDelta;
    private bool interactQueued;
    private bool crouchQueued;
    private bool inventoryQueued;

    public bool IsActive => forceEnable || Application.isMobilePlatform;
    public Vector2 MoveVector { get; private set; }

    public Vector2 ConsumeLookDelta() { var d = lookDelta; lookDelta = Vector2.zero; return d; }
    public bool ConsumeInteract() { bool v = interactQueued; interactQueued = false; return v; }
    public bool ConsumeCrouch() { bool v = crouchQueued; crouchQueued = false; return v; }
    public bool ConsumeInventory() { bool v = inventoryQueued; inventoryQueued = false; return v; }

    // Botões em coordenadas de tela (origem no canto inferior esquerdo, como Input.touches).
    private Rect InteractButton => new Rect(Screen.width - Scale(190), Scale(150), Scale(150), Scale(150));
    private Rect CrouchButton => new Rect(Screen.width - Scale(330), Scale(60), Scale(110), Scale(110));
    private Rect InventoryButton => new Rect(Screen.width - Scale(130), Screen.height - Scale(130), Scale(100), Scale(100));

    private static float Scale(float v) => v * Screen.height / 1080f;

    private void Update()
    {
        if (!IsActive) return;

        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch t = Input.GetTouch(i);
            if (GameUI.IsModalOpen && t.phase == TouchPhase.Began) continue;

            switch (t.phase)
            {
                case TouchPhase.Began:
                    if (InteractButton.Contains(t.position)) { interactQueued = true; break; }
                    if (CrouchButton.Contains(t.position)) { crouchQueued = true; break; }
                    if (InventoryButton.Contains(t.position)) { inventoryQueued = true; break; }

                    if (t.position.x < Screen.width * 0.4f && moveFingerId < 0)
                    {
                        moveFingerId = t.fingerId;
                        moveOrigin = moveCurrent = t.position;
                    }
                    else if (lookFingerId < 0)
                    {
                        lookFingerId = t.fingerId;
                    }
                    break;

                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    if (t.fingerId == moveFingerId) moveCurrent = t.position;
                    else if (t.fingerId == lookFingerId) lookDelta += t.deltaPosition * lookSensitivity;
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    if (t.fingerId == moveFingerId) moveFingerId = -1;
                    if (t.fingerId == lookFingerId) lookFingerId = -1;
                    break;
            }
        }

        float radius = Scale(joystickRadius);
        MoveVector = moveFingerId >= 0
            ? Vector2.ClampMagnitude((moveCurrent - moveOrigin) / radius, 1f)
            : Vector2.zero;
    }

    private void OnGUI()
    {
        if (!IsActive || GameUI.IsModalOpen) return;

        GUI.depth = 10;
        Color old = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, 0.35f);

        if (moveFingerId >= 0)
        {
            float r = Scale(joystickRadius);
            Vector2 o = ToGui(moveOrigin);
            Vector2 c = ToGui(moveOrigin + MoveVector * r);
            GUI.Box(new Rect(o.x - r, o.y - r, r * 2, r * 2), GUIContent.none);
            GUI.Box(new Rect(c.x - r * 0.4f, c.y - r * 0.4f, r * 0.8f, r * 0.8f), GUIContent.none);
        }

        GUI.color = new Color(1f, 1f, 1f, 0.6f);
        var style = new GUIStyle(GUI.skin.box) { fontSize = Mathf.RoundToInt(Scale(30)), alignment = TextAnchor.MiddleCenter };
        GUI.Box(ToGuiRect(InteractButton), "Interagir", style);
        GUI.Box(ToGuiRect(CrouchButton), "Agachar", style);
        GUI.Box(ToGuiRect(InventoryButton), "Mochila", style);
        GUI.color = old;
    }

    private static Vector2 ToGui(Vector2 screen) => new Vector2(screen.x, Screen.height - screen.y);

    private static Rect ToGuiRect(Rect r) => new Rect(r.x, Screen.height - r.y - r.height, r.width, r.height);
}
