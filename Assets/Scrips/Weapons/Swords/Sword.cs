using UnityEngine;

/// <summary>
/// 剑的表现控制器：根据玩家战斗状态在收刀点和攻击点之间切换。
/// 输入和攻击判定由 Player 状态机及 CombatExecutor 管理，武器本身不直接监听输入。
/// </summary>
public class Sword : MonoBehaviour, IWeaponController
{
    // 武器处于待机状态时跟随的挂点。
    [field: SerializeField] public Transform WeaponIdlingPosition { get; private set; }

    // 武器处于攻击状态时跟随的挂点。
    [field: SerializeField] public Transform WeaponAttackingPosition { get; private set; }

    // 武器表现参数，例如收刀时的平滑跟随时间。
    [field: SerializeField] public WeaponSO Data { get; private set; }

    // 只描述武器当前的表现状态，不负责决定玩家是否可以进入攻击状态。
    private WeaponState currentState = WeaponState.Idle;

    // 预留给需要根据朝向调整武器表现的逻辑；当前攻击点由场景挂点直接决定。
    private Vector3 facingDirection = Vector3.forward;

    // SmoothDamp 使用的速度缓存，必须在武器禁用时清零，避免重新启用后带入旧速度。
    private Vector3 idleSmoothVelocity;


    // Player 状态机用它读取武器表现是否仍处于攻击阶段。
    public bool IsAttacking => currentState == WeaponState.Attacking;

    #region Mono Methods
    private void Start()
    {
        // 场景初始化时先将武器放到收刀点，避免使用默认 Transform 位置闪现一帧。
        EnterIdle();
    }

    private void Update()
    {
        // 武器表现跟随使用帧更新，保持与普通 Transform 动画同步。
        Update(Time.deltaTime);
    }

    private void FixedUpdate()
    {
        // 保留物理更新入口，供未来需要物理驱动武器时使用。
        PhysicsUpdate(Time.fixedDeltaTime);
    }

    private void OnDisable()
    {
        // 禁用期间不保留攻击状态或平滑速度，重新启用后从稳定的待机状态开始。
        currentState = WeaponState.Idle;
        idleSmoothVelocity = Vector3.zero;
    }

    #endregion
    
    #region IWeapon Methods
    public bool CanStartAttack()
    {
        // 武器只校验自身是否空闲；玩家能否攻击仍由玩家状态机决定。
        return currentState == WeaponState.Idle;
    }

    public void StartAttack()
    {
        // 攻击进入由 PlayerAttackState 转发到这里，武器只切换到攻击挂点。
        if (!CanStartAttack())
        {
            return;
        }

        currentState = WeaponState.Attacking;
        ApplyAttackPose();
    }

    public void CancelAttack()
    {
        // 攻击结束、状态取消或对象禁用时，统一回到收刀表现。
        EnterIdle();
    }

    public void Update(float deltaTime)
    {
        // 根据武器表现状态选择对应的挂点跟随方式。
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
        // 当前武器逻辑不需要物理更新，保留接口以便未来扩展。
    }

    public void SetFacingDirection(Vector3 worldDirection, float minimum)
    {
        // 只记录有效的水平朝向，避免垂直方向或近似零向量污染武器表现。
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
        // 状态和 Transform 必须一起切换，保证外部看到的状态与武器姿态一致。
        currentState = WeaponState.Idle;
        ApplyIdlePose();
    }

    private void ApplyIdlePose()
    {
        // 进入待机的瞬间直接对齐，后续帧再使用平滑跟随。
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
        // 进入攻击的瞬间直接对齐攻击挂点，避免攻击开始时出现滞后。
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
        // 收刀状态使用平滑位置跟随，降低攻击结束时的突兀跳变。
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
        // 攻击状态直接跟随攻击挂点，确保武器与角色攻击姿态保持一致。
        if (WeaponAttackingPosition == null)
        {
            return;
        }

        transform.position = WeaponAttackingPosition.position;
        transform.rotation = WeaponAttackingPosition.rotation;
    }
}
