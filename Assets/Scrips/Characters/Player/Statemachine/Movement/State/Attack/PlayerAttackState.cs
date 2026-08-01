using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 玩家攻击状态：按当前攻击动画进度统一驱动命中、线性连击、冲刺取消与攻击结束。
/// 世界命中检测仍由 CombatExecutor 负责，状态本身不操作目标或受击表现。
/// </summary>
public class PlayerAttackState : PlayerGroundedState
{
    private readonly PlayerAttackData attackData;
    private readonly CombatExecutor combatExecutor;
    private readonly MotionDriver motionDriver;
    private readonly PlayerActionBuffer actionBuffer;

    // 连击段切换使用固定秒数过渡，避免散落的匿名数值难以追溯。
    private const float ComboTransitionDuration = 0.1555f;

    // 当前正在播放的线性连招段；下一段固定为数组中的后一项。
    private int currentComboIndex;
    // 运动采样同时支持 Animator 过渡期，因此缓存完整状态路径 Hash 匹配下一状态。
    private int currentComboStateHash;

    public PlayerAttackState(PlayerMovementStateMachine stateMachine) : base(stateMachine)
    {
        attackData = stateMachine.Player.Data.AttackData;
        combatExecutor = stateMachine.CombatExecutor;
        motionDriver = new MotionDriver(stateMachine.Player.Rigidbody);
        actionBuffer = stateMachine.ActionBuffer;
    }

    #region IState Methods

    public override void Enter()
    {
        // 配置错误必须在注册输入和启动攻击表现前暴露，避免留下半初始化攻击状态。
        ValidateConfiguration();
        base.Enter();

        stateMachine.ReusableData.MovementSpeedModifier = 0f;
        ResetVelocity();

        stateMachine.Player.WeaponController?.StartAttack();
        combatExecutor.BeginAttack();
        ExecuteCombo(0);
    }

    public override void Exit()
    {
        motionDriver.Stop();
        // 冲刺取消、移动取消与自然结束都经由此处，统一清除尚未消费的攻击输入。
        actionBuffer.Clear(PlayerActionType.Attack);
        currentComboIndex = 0;
        currentComboStateHash = 0;
        combatExecutor.EndAttack();
        stateMachine.Player.WeaponController?.CancelAttack();
        base.Exit();
    }

    public override void Update()
    {
        base.Update();

        UpdateMotionTime();

        if (!TryGetCurrentComboNormalizedTime(out float normalizedTime))
        {
            return;
        }

        // 仅确认当前 Animator 已切到本段后，才允许执行本段命中和攻击 FX。
        combatExecutor.Update(normalizedTime);

        // 攻击衔接优先于同帧的移动取消：缓存有效时即使按住移动也继续连招。
        if (TryContinueCombo(normalizedTime))
        {
            return;
        }

        if (TryCancelToMovement(normalizedTime))
        {
            return;
        }

        if (normalizedTime >= 1f)
        {
            HandleAttackFinished();
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        motionDriver.PhysicsUpdate();
    }

    #endregion

    #region Main Methods

    /// <summary>
    /// 校验攻击状态的必要配置。具体招式字段由 ComboList 与 ComboConfig 在数据层校验。
    /// </summary>
    public void ValidateConfiguration()
    {
        // 缓冲时长为 0 会让预输入永远失效，属于配置错误，不在运行时替换为默认值。
        if (attackData.AttackBufferDuration <= 0f)
        {
            throw new InvalidOperationException(
                $"PlayerAttackState 的 AttackBufferDuration（{attackData.AttackBufferDuration}）必须大于 0，" +
                "请在 Player 配置资产中填写攻击预输入的有效时长。");
        }

        ComboList comboList = attackData.CurrentComboList;
        if (comboList == null)
        {
            throw new InvalidOperationException(
                "PlayerAttackState requires a configured ComboList.");
        }

        comboList.ValidateConfiguration();

        // 数据层校验通过后，再确认每段 ComboName 对应的 Animator 状态确实存在。
        // 缺失属于配置错误，必须在 Enter 阶段直接失败，运行中不做超时或回退 Idle 等兜底。
        Animator animator = stateMachine.Player.Animator;
        for (int comboIndex = 0; comboIndex < comboList.ComboCount; comboIndex++)
        {
            string statePath = comboList.GetComboName(comboIndex);
            if (!animator.HasState(0, Animator.StringToHash(statePath)))
            {
                throw new InvalidOperationException(
                    $"PlayerAttackState 第 {comboIndex + 1} 段招式的 ComboName（{statePath}）" +
                    "在 Animator 第 0 层不存在，请确认配置的是完整状态路径（如 Base Layer.Attack.AM_Attack01）。");
            }
        }
    }

    // 过渡期间 GetCurrentAnimatorStateInfo 仍可能指向旧段，不能把旧进度用于新段事件。
    private bool TryGetCurrentComboNormalizedTime(out float normalizedTime)
    {
        Animator animator = stateMachine.Player.Animator;
        if (animator.IsInTransition(0))
        {
            normalizedTime = 0f;
            return false;
        }

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        // 保留状态名比对：这是 CrossFade 期间的时序守卫，不是防御性配置校验。
        string currentComboName = attackData.CurrentComboList.GetComboName(currentComboIndex);
        if (!stateInfo.IsName(currentComboName))
        {
            normalizedTime = 0f;
            return false;
        }

        normalizedTime = stateInfo.normalizedTime;
        return true;
    }

    /// <summary>
    /// 运动曲线需要覆盖 CrossFade 开头，因此过渡期读取下一状态；命中与取消仍沿用原来的严格时序。
    /// </summary>
    private void UpdateMotionTime()
    {
        Animator animator = stateMachine.Player.Animator;
        AnimatorStateInfo stateInfo = animator.IsInTransition(0)
            ? animator.GetNextAnimatorStateInfo(0)
            : animator.GetCurrentAnimatorStateInfo(0);

        if (stateInfo.fullPathHash == currentComboStateHash)
        {
            motionDriver.SetNormalizedTime(stateInfo.normalizedTime);
        }
    }

    private void ExecuteCombo(int comboIndex)
    {
        currentComboIndex = comboIndex;
        combatExecutor.BeginCombo(currentComboIndex);

        ComboConfig comboConfig =
            attackData.CurrentComboList.ComboConfigs[currentComboIndex];
        currentComboStateHash = Animator.StringToHash(comboConfig.ComboName);

        stateMachine.Player.Animator.CrossFadeInFixedTime(
            comboConfig.ComboName,
            ComboTransitionDuration,
            0,
            0);

        // 每段 Combo 锁定开始时的角色前向，运行过程中不根据实时 WASD 改变轨迹。
        Vector3 motionDirection =
            stateMachine.Player.Rigidbody.rotation * Vector3.forward;
        motionDriver.Begin(comboConfig.MotionData, motionDirection);
    }

    private void HandleAttackFinished()
    {
        // 后摇由动画本身承担；动画结束后直接根据当前移动输入回到地面移动状态。
        if (stateMachine.ReusableData.MovementInput == Vector2.zero)
        {
            stateMachine.ChangeState(stateMachine.IdlingState);
            return;
        }

        OnMove();
    }

    /// <summary>
    /// 后摇窗口开启后消费预输入并衔接下一段；窗口未开启时不提前消费，让输入留在缓冲中继续等待。
    /// </summary>
    private bool TryContinueCombo(float normalizedTime)
    {
        int comboCount = attackData.CurrentComboList.ComboCount;
        if (currentComboIndex >= comboCount - 1)
        {
            return false;
        }

        float chainStart =
            attackData.CurrentComboList.GetRecoveryStartNormalizedTime(currentComboIndex);
        if (normalizedTime < chainStart)
        {
            return false;
        }

        if (!actionBuffer.TryConsume(
                PlayerActionType.Attack,
                Time.time,
                attackData.AttackBufferDuration))
        {
            return false;
        }

        ExecuteCombo(currentComboIndex + 1);
        return true;
    }

    // 后摇开始后，持续按住或新按下移动都可立即离开攻击，避免无效帧锁住角色。
    private bool TryCancelToMovement(float normalizedTime)
    {
        float recoveryStart = attackData.CurrentComboList.GetRecoveryStartNormalizedTime(currentComboIndex);
        if (normalizedTime < recoveryStart || stateMachine.ReusableData.MovementInput == Vector2.zero)
        {
            return false;
        }

        OnMove();
        return true;
    }

    #endregion

    #region Input Methods

    protected override void OnAttackStarted(InputAction.CallbackContext context)
    {
        int comboCount = attackData.CurrentComboList.ComboCount;
        // 末段不再记录，避免最后一段反复按攻击后循环回第一段。
        if (currentComboIndex >= comboCount - 1)
        {
            return;
        }

        // 攻击回调只记录玩家意图，实际衔接由 Update 在后摇窗口开启后决定。
        actionBuffer.Record(PlayerActionType.Attack, Time.time);
    }

    protected override void OnDashStarted(InputAction.CallbackContext context)
    {
        ComboConfig comboConfig = attackData.CurrentComboList.ComboConfigs[currentComboIndex];
        // 开启后，本段攻击从进入状态起即可被冲刺打断，包括动画过渡阶段。
        if (!comboConfig.CanDashCancel)
        {
            return;
        }

        stateMachine.ChangeState(stateMachine.DashingState);
    }

    protected override void OnJumpStarted(InputAction.CallbackContext context)
    {
        // 本轮不实现跳跃取消，攻击期间显式拦截跳跃输入。
    }

    #endregion
}
