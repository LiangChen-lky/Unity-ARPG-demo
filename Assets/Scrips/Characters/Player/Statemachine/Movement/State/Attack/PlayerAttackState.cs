using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 玩家攻击状态，负责串联攻击动画、连击输入和攻击生命周期。
/// 世界交互与命中检测由 CombatExecutor 负责，状态本身只负责时序和状态转换。
/// </summary>
public class PlayerAttackState : PlayerGroundedState
{
    // 攻击状态配置，包含当前使用的连击表和目标层级。
    private readonly PlayerAttackData attackData;

    // 攻击执行器，负责命中检测、特效和攻击事件的执行。
    private readonly CombatExecutor combatExecutor;

    // 当前是否已经进入本段攻击的连击输入窗口。
    private bool canExecuteCombo;

    // 当前正在播放的连击索引，以及下一次连击要使用的索引。
    private int currentComboIndex;
    private int nextComboIndex;

    public PlayerAttackState(PlayerMovementStateMachine stateMachine) : base(stateMachine)
    {
        attackData = stateMachine.Player.Data.AttackData;
        combatExecutor = stateMachine.CombatExecutor;
    }

    #region IState Methods

    public override void Enter()
    {
        // 在注册输入、改变速度和驱动武器之前失败，避免攻击状态只完成了一半初始化。
        ValidateConfiguration();
        base.Enter();

        // 攻击期间暂时停止移动，并清除进入攻击前残留的刚体速度。
        stateMachine.ReusableData.MovementSpeedModifier = 0f;
        ResetVelocity();

        // 玩家状态进入攻击后，只向武器发送表现指令；命中检测仍由 CombatExecutor 执行。
        stateMachine.Player.WeaponController?.StartAttack();
        combatExecutor.BeginAttack();

        // 首次进入攻击状态时，从连击序列的当前起点开始执行。
        canExecuteCombo = false;

        ExecuteCombo();
    }

    public override void Exit()
    {
        // 无论攻击自然结束还是被其他状态打断，都要清理所有攻击运行时数据。
        canExecuteCombo = false;
        currentComboIndex = 0;
        nextComboIndex = 0;

        combatExecutor.EndAttack();

        // 无论攻击自然结束还是被其他状态打断，都要让武器回到收刀表现。
        stateMachine.Player.WeaponController?.CancelAttack();

        base.Exit();
    }

    public override void Update()
    {
        base.Update();

        // 使用当前攻击动画的 normalizedTime 驱动 CombatExecutor 中配置的攻击事件。
        RunCombatEvents();
    }

    public override void OnAnimationExitEnvent(AnimationEvent animationEvent)
    {
        AnimationClip sourceClip = animationEvent?.animatorClipInfo.clip;
        string currentComboName = attackData.CurrentComboList.TryGetComboName(currentComboIndex);

        // CrossFade 期间，旧 Combo 的 Exit 事件仍可能触发，不能用它结束新 Combo。
        if (sourceClip == null || sourceClip.name != currentComboName)
        {
            return;
        }

        // 只有当前连击动画的退出事件才能结束攻击状态。
        HandleAttackFinished();
    }

    public override void OnAnimationTransitionEvent()
    {
        // 动画进入后摇衔接区后，开放下一段连击输入。
        canExecuteCombo = true;
    }

    #endregion

    #region Main Methods

    /// <summary>
    /// 校验攻击状态的必要配置。
    /// 缺少连击数据属于开发配置错误，不能被当作一次正常的攻击结束处理。
    /// </summary>
    public void ValidateConfiguration()
    {
        if (combatExecutor.HasComboData)
        {
            return;
        }

        throw new InvalidOperationException(
            "PlayerAttackState requires a valid ComboList with at least one ComboConfig.");
    }

    private void RunCombatEvents()
    {
        if (!combatExecutor.HasComboData)
        {
            return;
        }

        // CombatExecutor 使用动画归一化时间判断命中框、特效等事件的触发时机。
        AnimatorStateInfo animatorStateInfo = stateMachine.Player.Animator.GetCurrentAnimatorStateInfo(0);
        combatExecutor.Update(animatorStateInfo.normalizedTime);
    }

    /// <summary>
    /// 播放指定连击动画，并等待该动画事件开放下一段连击。
    /// </summary>
    private void ExecuteCombo()
    {
        // 将待执行索引提交为当前连击，并让执行器重置本段攻击的事件游标。
        currentComboIndex = nextComboIndex;
        combatExecutor.BeginCombo(currentComboIndex);

        // 状态机负责动画表现，CombatExecutor 不直接操作 Animator。
        stateMachine.Player.Animator.CrossFadeInFixedTime(
            attackData.CurrentComboList.TryGetComboName(currentComboIndex),
            0.1555f,
            0,
            0);

        UpdateComboIndex();

        // 播放新一段动画后，必须等待该动画自己的 Transition 事件。
        canExecuteCombo = false;
    }

    private void UpdateComboIndex()
    {
        // 预先推进下一段索引；达到连击表末尾后回到第一段。
        nextComboIndex++;
        if (nextComboIndex >= attackData.CurrentComboList.TryGetComboConfigsCount())
        {
            nextComboIndex = 0;
        }
    }

    private void HandleAttackFinished()
    {
        // 攻击动画退出事件是攻击状态结束的唯一入口。
        stateMachine.ChangeState(stateMachine.AttackRecoveryState);
    }

    #endregion

    #region Input Methods

    protected override void OnAttackStarted(InputAction.CallbackContext context)
    {
        // 只有动画 Transition 事件开放窗口后，攻击输入才会推进到下一段连击。
        if (canExecuteCombo)
        {
            ExecuteCombo();
        }
    }

    #endregion
}
