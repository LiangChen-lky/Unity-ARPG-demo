using System;
using System.Collections;
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

    // 当前是否已经过了本段攻击的连击输入冷却时间。
    private bool canExecuteCombo;

    // 当前正在播放的连击索引，以及下一次连击要使用的索引。
    private int currentComboIndex;
    private int nextComboIndex;

    // 连击输入冷却和连击序列重置分别由两个协程管理。
    private Coroutine executeComboColdCoroutine;
    private Coroutine stopComboCoroutine;


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
        canExecuteCombo = true;

        ExecuteCombo();
    }

    public override void Exit()
    {
        // 无论攻击自然结束还是被其他状态打断，都要清理所有攻击运行时数据。
        StopAttackCoroutines();
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

    public override void OnAnimationExitEnvent()
    {
        // 如果攻击动画配置了退出事件，则由动画事件通知攻击状态结束。
        HandleAttackFinished();
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
    /// 播放指定连击动画，并重新开启本段连击的两个时间窗口。
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

        // 在冷却结束前再次按攻击键不会推进连击。
        canExecuteCombo = false;

        // 冷却结束后允许输入下一段连击。
        float comboColdTime = attackData.CurrentComboList.TryGetComboColdTime(currentComboIndex);
        if (executeComboColdCoroutine != null)
        {
            stateMachine.Player.StopCoroutine(executeComboColdCoroutine);
        }

        executeComboColdCoroutine = stateMachine.Player.StartCoroutine(ExecuteComboCold(comboColdTime));

        // 冷却时间的两倍内没有继续攻击，则下一次攻击从第一段重新开始。
        if (stopComboCoroutine != null)
        {
            stateMachine.Player.StopCoroutine(stopComboCoroutine);
        }

        stopComboCoroutine = stateMachine.Player.StartCoroutine(StopCombo(comboColdTime));
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

    private IEnumerator ExecuteComboCold(float coldTime)
    {
        // 逐帧等待输入冷却结束，避免额外创建计时器对象。
        while (coldTime > 0f)
        {
            yield return null;
            coldTime -= Time.deltaTime;
        }

        canExecuteCombo = true;
        executeComboColdCoroutine = null;
    }

    private IEnumerator StopCombo(float coldTime)
    {
        // 连击窗口关闭后重置索引，但不主动结束当前攻击动画。
        float time = coldTime * 2f;
        while (time > 0f)
        {
            yield return null;
            time -= Time.deltaTime;
        }

        nextComboIndex = 0;
        stopComboCoroutine = null;
    }

    private void StopAttackCoroutines()
    {
        // 状态退出时停止所有攻击协程，避免旧协程在下一次攻击中继续修改状态。
        if (executeComboColdCoroutine != null)
        {
            stateMachine.Player.StopCoroutine(executeComboColdCoroutine);
            executeComboColdCoroutine = null;
        }

        if (stopComboCoroutine != null)
        {
            stateMachine.Player.StopCoroutine(stopComboCoroutine);
            stopComboCoroutine = null;
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
        // 只有当前连击冷却结束后，攻击输入才会推进到下一段连击。
        if (canExecuteCombo)
        {
            ExecuteCombo();
        }
    }

    #endregion
}
