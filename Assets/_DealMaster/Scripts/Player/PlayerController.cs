using UnityEngine;

/// <summary>Movimentação em primeira pessoa: caminhar, correr, agachar e olhar.</summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInputReader))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private float walkSpeed = 3.2f;
    [SerializeField] private float runSpeed = 5.8f;
    [SerializeField] private float crouchSpeed = 1.6f;
    [SerializeField] private float acceleration = 12f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float standingHeight = 1.8f;
    [SerializeField] private float crouchHeight = 1.1f;
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;

    private CharacterController controller;
    private PlayerInputReader input;
    private Vector3 horizontalVelocity;
    private float verticalVelocity;
    private float pitch;
    private bool crouching;

    public bool IsCrouching => crouching;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        input = GetComponent<PlayerInputReader>();
        if (cameraPivot == null && Camera.main != null) cameraPivot = Camera.main.transform;
        controller.height = standingHeight;
        controller.center = new Vector3(0f, standingHeight / 2f, 0f);
    }

    private void Start()
    {
        if (!Application.isMobilePlatform) GameUI.LockCursor(true);
    }

    private void Update()
    {
        if (input.CrouchPressed) TryToggleCrouch();

        UpdateLook();
        UpdateMovement();
        UpdateCrouchHeight();
    }

    private void UpdateLook()
    {
        Vector2 look = input.Look;
        transform.Rotate(0f, look.x, 0f);
        pitch = Mathf.Clamp(pitch - look.y, minPitch, maxPitch);
        if (cameraPivot != null) cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void UpdateMovement()
    {
        Vector2 move = input.Move;
        float speed = crouching ? crouchSpeed : input.Run ? runSpeed : walkSpeed;
        Vector3 target = (transform.right * move.x + transform.forward * move.y) * speed;
        horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, target, acceleration * Time.deltaTime);

        if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;

        Vector3 motion = horizontalVelocity + Vector3.up * verticalVelocity;
        controller.Move(motion * Time.deltaTime);
    }

    private void TryToggleCrouch()
    {
        if (crouching)
        {
            // Só levanta se houver espaço acima da cabeça.
            Vector3 origin = transform.position + Vector3.up * crouchHeight;
            float clearance = standingHeight - crouchHeight;
            if (Physics.SphereCast(origin, controller.radius * 0.9f, Vector3.up, out _, clearance,
                    ~0, QueryTriggerInteraction.Ignore))
                return;
        }
        crouching = !crouching;
    }

    private void UpdateCrouchHeight()
    {
        float targetHeight = crouching ? crouchHeight : standingHeight;
        controller.height = Mathf.MoveTowards(controller.height, targetHeight, 4f * Time.deltaTime);
        controller.center = new Vector3(0f, controller.height / 2f, 0f);
        if (cameraPivot != null)
        {
            Vector3 p = cameraPivot.localPosition;
            p.y = controller.height - 0.12f;
            cameraPivot.localPosition = p;
        }
    }

    /// <summary>Teleporta o jogador (ao carregar um jogo salvo, por exemplo).</summary>
    public void Teleport(Vector3 position, float yaw)
    {
        controller.enabled = false;
        transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
        controller.enabled = true;
        horizontalVelocity = Vector3.zero;
        verticalVelocity = 0f;
    }
}
