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

    // 连击段切换使用固定秒数过渡，避免散落的匿名数值难以追溯。
    private const float ComboTransitionDuration = 0.1555f;

    // 当前正在播放的线性连招段；下一段固定为数组中的后一项。
    private int currentComboIndex;

    public PlayerAttackState(PlayerMovementStateMachine stateMachine) : base(stateMachine)
    {
        attackData = stateMachine.Player.Data.AttackData;
        combatExecutor = stateMachine.CombatExecutor;
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
        currentComboIndex = 0;
        combatExecutor.EndAttack();
        stateMachine.Player.WeaponController?.CancelAttack();
        base.Exit();
    }

    public override void Update()
    {
        base.Update();

        if (!TryGetCurrentComboNormalizedTime(out float normalizedTime))
        {
            return;
        }

        // 仅确认当前 Animator 已切到本段后，才允许执行本段命中和攻击 FX。
        combatExecutor.Update(normalizedTime);

        if (TryCancelToMovement(normalizedTime))
        {
            return;
        }

        if (normalizedTime >= 1f)
        {
            HandleAttackFinished();
        }
    }

    #endregion

    #region Main Methods

    /// <summary>
    /// 校验攻击状态的必要配置。具体招式字段由 ComboList 与 ComboConfig 在数据层校验。
    /// </summary>
    public void ValidateConfiguration()
    {
        ComboList comboList = attackData.CurrentComboList;
        if (comboList == null)
        {
            throw new InvalidOperationException(
                "PlayerAttackState requires a configured ComboList.");
        }

        comboList.ValidateConfiguration();
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

    private void ExecuteCombo(int comboIndex)
    {
        currentComboIndex = comboIndex;
        combatExecutor.BeginCombo(currentComboIndex);

        stateMachine.Player.Animator.CrossFadeInFixedTime(
            attackData.CurrentComboList.GetComboName(currentComboIndex),
            ComboTransitionDuration,
            0,
            0);
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
        if (currentComboIndex >= comboCount - 1 ||
            !TryGetCurrentComboNormalizedTime(out float normalizedTime))
        {
            return;
        }

        // 本轮不缓存输入：只有进入后摇起始帧后按下攻击，才立刻衔接下一段。
        float chainStart = attackData.CurrentComboList.GetRecoveryStartNormalizedTime(currentComboIndex);
        if (normalizedTime >= chainStart)
        {
            ExecuteCombo(currentComboIndex + 1);
        }
    }

    protected override void OnDashStarted(InputAction.CallbackContext context)
    {
        if (!TryGetCurrentComboNormalizedTime(out float normalizedTime))
        {
            return;
        }

        ComboConfig comboConfig = attackData.CurrentComboList.ComboConfigs[currentComboIndex];
        float recoveryStart = attackData.CurrentComboList.GetRecoveryStartNormalizedTime(currentComboIndex);
        if (!comboConfig.CanDashCancel || normalizedTime < recoveryStart)
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
