using UnityEngine;

[RequireComponent(typeof(PlayerInput))]
public class Sword : MonoBehaviour
{
    [field: SerializeField] public Transform WeaponIdlingPosition { get; private set; }
    [field: SerializeField] public Transform WeaponAttackingPosition { get; private set; }
    [field: SerializeField] public WeaponSO Data { get; private set; }
    
    public PlayerInput PlayerInput { get; private set; }
    
    private SwordStateMachine stateMachine;

    private void Awake()
    {
        PlayerInput = GetComponent<PlayerInput>();
        
        stateMachine = new SwordStateMachine(this);
    }

    private void Start()
    {
        stateMachine.ChangeState(stateMachine.IdlingState);
    }

    private void Update()
    {
        stateMachine.HandleInput();
        stateMachine.Update();
    }

    private void FixedUpdate()
    {
        stateMachine.PhysicsUpdate();
    }
}
