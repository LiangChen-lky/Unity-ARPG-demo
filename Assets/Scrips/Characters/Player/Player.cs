using UnityEngine;

[RequireComponent(typeof(PlayerInput))]
public class Player : MonoBehaviour
{
    [field: Header("References")]
    [field: SerializeField] public PlayerSO Data { get; private set; }
    
    [field: Header("Collisions")]
    [field: SerializeField] public PlayerCapsuleColliderUtility ColliderUtility { get; private set; }
    [field: SerializeField] public PlayerLayerData LayerData { get; private set; }

    [field: Header("Cameras")]
    [field: SerializeField] public PlayerCameraUtility CameraUtility { get; private set; }

    [Header("Combat")]
    // 场景中实际挂载的武器控制器；Player 通过接口驱动武器，不依赖具体 Sword 类型。
    [SerializeField] private MonoBehaviour weaponControllerSource;
    
    public Animator Animator { get; private set; }
    public PlayerInput Input { get; private set; }
    // 玩家状态机唯一使用的武器入口，避免状态机和场景对象耦合到具体武器类。
    public IWeaponController WeaponController => weaponControllerSource as IWeaponController;
    public Rigidbody Rigidbody { get; private set; }
    public Transform MainCameraTransform { get; private set; }
    
    private PlayerMovementStateMachine movementStateMachine;

    private void Awake()
    {
        Animator = GetComponentInChildren<Animator>();
        Input = GetComponent<PlayerInput>();
        Rigidbody = GetComponent<Rigidbody>();
        
        ColliderUtility.Initialize(gameObject);
        ColliderUtility.CalculateCapsuleColliderDimensions();

        CameraUtility?.Initialize();
        
        if (Camera.main != null) MainCameraTransform = Camera.main.transform;

        movementStateMachine = new PlayerMovementStateMachine(this);
    }

    private void OnValidate()
    {
        ColliderUtility.Initialize(gameObject);
        ColliderUtility.CalculateCapsuleColliderDimensions();

        if (weaponControllerSource != null && !(weaponControllerSource is IWeaponController))
        {
            Debug.LogError($"{weaponControllerSource.name} must implement {nameof(IWeaponController)}.", this);
        }
    }

    private void Start()
    {
        movementStateMachine.ChangeState(movementStateMachine.IdlingState);
    }

    private void Update()
    {
        movementStateMachine.HandleInput();
        movementStateMachine.Update();
    }

    private void FixedUpdate()
    {
        movementStateMachine.PhysicsUpdate();
    }

    private void OnTriggerEnter(Collider collider)
    {
        movementStateMachine.OnTriggerEnter(collider);
    }
    
    private void OnTriggerExit(Collider collider)
    {
        movementStateMachine.OnTriggerExit(collider);
    }

    public void OnMovementStateAnimationEnterEvent()
    {
        movementStateMachine.OnAnimationEnterEvent();
    }
    
    public void OnMovementStateAnimationExitEvent()
    {
        movementStateMachine.OnAnimationExitEvent();
    }
    
    public void OnMovementStateAnimationTransitionEvent()
    {
        movementStateMachine.OnAnimationTransitionEvent();
    }
}
