using UnityEngine;

public class Sword : MonoBehaviour, IWeaponController
{
    [field: SerializeField] public Transform WeaponIdlingPosition { get; private set; }
    [field: SerializeField] public Transform WeaponAttackingPosition { get; private set; }
    [field: SerializeField] public WeaponSO Data { get; private set; }

    private WeaponState currentState = WeaponState.Idle;
    private Vector3 facingDirection = Vector3.forward;
    private Vector3 idleSmoothVelocity;


    public bool IsAttacking => currentState == WeaponState.Attacking;

    #region Mono Methods
    private void Start()
    {
        EnterIdle();
    }

    private void Update()
    {
        Update(Time.deltaTime);
    }

    private void FixedUpdate()
    {
        PhysicsUpdate(Time.fixedDeltaTime);
    }

    private void OnDisable()
    {
        currentState = WeaponState.Idle;
        idleSmoothVelocity = Vector3.zero;
    }

    #endregion
    
    #region IWeapon Methods
    public bool CanStartAttack()
    {
        return currentState == WeaponState.Idle;
    }

    public void StartAttack()
    {
        if (!CanStartAttack())
        {
            return;
        }

        currentState = WeaponState.Attacking;
        ApplyAttackPose();
    }

    public void CancelAttack()
    {
        EnterIdle();
    }

    public void Update(float deltaTime)
    {
        switch (currentState)
        {
            case WeaponState.Idle:
                UpdateIdlePose();
                break;

            case WeaponState.Attacking:
                UpdateAttackPose();
                break;
        }
    }

    public void PhysicsUpdate(float fixedDeltaTime)
    {
        // 当前武器逻辑不需要物理更新，先留接口以便未来扩展
    }

    public void SetFacingDirection(Vector3 worldDirection, float minimum)
    {
        worldDirection.y = 0f;
        if (worldDirection.sqrMagnitude < minimum)
        {
            return;
        }

        facingDirection = worldDirection.normalized;
    }

    #endregion

    private void EnterIdle()
    {
        currentState = WeaponState.Idle;
        ApplyIdlePose();
    }

    private void ApplyIdlePose()
    {
        if (WeaponIdlingPosition == null)
        {
            Debug.LogError("WeaponIdlingPosition is not set");
            return;
        }

        transform.position = WeaponIdlingPosition.position;
        transform.rotation = WeaponIdlingPosition.rotation;
    }

    private void ApplyAttackPose()
    {
        if (WeaponAttackingPosition == null)
        {
            Debug.LogError("WeaponAttackingPosition is not set");
            return;
        }

        transform.position = WeaponAttackingPosition.position;
        transform.rotation = WeaponAttackingPosition.rotation;
    }

    private void UpdateIdlePose()
    {
        if (WeaponIdlingPosition == null)
        {
            Debug.LogError("WeaponIdlingPosition is not set");
            return;
        }

        transform.position = Vector3.SmoothDamp(
            transform.position,
            WeaponIdlingPosition.position,
            ref idleSmoothVelocity,
            Data != null ? Data.SmoothTime : 0.08f
        );

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            WeaponIdlingPosition.rotation,
            1f
        );
    }


    private void UpdateAttackPose()
    {
        if (WeaponAttackingPosition == null)
        {
            return;
        }

        transform.position = WeaponAttackingPosition.position;
        transform.rotation = WeaponAttackingPosition.rotation;
    }
}
