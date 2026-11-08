using UnityEngine;

/// <summary>
/// Cliente no mundo: anda pela praça e pelas calçadas, para quando o jogador puxa conversa
/// e vai embora depois que a negociação termina.
/// </summary>
public class CustomerAgent : MonoBehaviour, IInteractable
{
    private enum Mode { Walking, Idle, Talking, Leaving }

    [SerializeField] private float walkSpeed = 1.3f;

    private Vector3[] waypoints;
    private Vector3 target;
    private Vector3 exitPoint;
    private float idleUntil;
    private Mode mode = Mode.Walking;
    private System.Random rng;
    private CustomerSpawner spawner;
    private Transform talkTarget;

    public string CustomerName { get; private set; }
    public CustomerBrain Brain { get; private set; }

    public string InteractionPrompt =>
        Brain.IsFinished ? $"{CustomerName} já está de saída" : $"Conversar com {CustomerName}";

    public void Setup(CustomerSpawner owner, string customerName, CustomerBrain brain,
        Vector3[] points, Vector3 exit, int seed)
    {
        spawner = owner;
        CustomerName = customerName;
        Brain = brain;
        waypoints = points;
        exitPoint = exit;
        rng = new System.Random(seed);
        PickTarget();
    }

    public bool CanInteract(InteractionController interactor) => Brain != null && !Brain.IsFinished && mode != Mode.Leaving;

    public void Interact(InteractionController interactor)
    {
        mode = Mode.Talking;
        talkTarget = interactor.transform;
        GameUI.Open(new NegotiationPanel(this));
    }

    /// <summary>Chamado quando a tela de negociação fecha.</summary>
    public void EndTalk()
    {
        talkTarget = null;
        if (Brain.IsFinished)
        {
            mode = Mode.Leaving;
            target = exitPoint;
        }
        else
        {
            mode = Mode.Idle;
            idleUntil = Time.time + 6f; // espera um pouco, caso o jogador volte
        }
    }

    private void Update()
    {
        switch (mode)
        {
            case Mode.Talking:
                if (talkTarget != null) Face(talkTarget.position);
                return;
            case Mode.Idle:
                if (Time.time >= idleUntil) { mode = Mode.Walking; PickTarget(); }
                return;
        }

        Vector3 to = target - transform.position;
        to.y = 0f;
        if (to.magnitude < 0.3f)
        {
            if (mode == Mode.Leaving)
            {
                if (spawner != null) spawner.Despawn(this);
                else Destroy(gameObject);
                return;
            }
            mode = Mode.Idle;
            idleUntil = Time.time + 2f + (float)rng.NextDouble() * 5f;
            return;
        }

        Vector3 step = to.normalized * walkSpeed * Time.deltaTime;
        transform.position += Vector3.ClampMagnitude(step, to.magnitude);
        Face(target);
    }

    private void PickTarget()
    {
        if (waypoints == null || waypoints.Length == 0) return;
        target = waypoints[rng.Next(waypoints.Length)];
    }

    private void Face(Vector3 point)
    {
        Vector3 dir = point - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 8f * Time.deltaTime);
    }
}
