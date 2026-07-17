using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttackState : PlayerGroundedState
{
    private readonly PlayerAttackData attackData;
    private readonly CombatExecutor combatExecutor;

    private bool canExecuteCombo;
    private int currentComboIndex;
    private int nextComboIndex;
    private Coroutine executeComboColdCoroutine;
    private Coroutine stopComboCoroutine;
    private bool hasHandledAttackExit;

    public PlayerAttackState(PlayerMovementStateMachine stateMachine) : base(stateMachine)
    {
        attackData = stateMachine.Player.Data.AttackData;
        combatExecutor = stateMachine.CombatExecutor;
    }

    public override void Enter()
    {
        base.Enter();

        stateMachine.ReusableData.MovementSpeedModifier = 0f;
        ResetVelocity();
        // 玩家状态进入攻击后，只向武器发送表现指令；命中检测仍由 CombatExecutor 执行。
        stateMachine.Player.WeaponController?.StartAttack();
        combatExecutor.BeginAttack();

        canExecuteCombo = true;
        hasHandledAttackExit = false;

        ExecuteCombo();
    }

    public override void Exit()
    {
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

        RunCombatEvents();
        TryHandleAttackAnimationExit();
    }

    private void RunCombatEvents()
    {
        if (!combatExecutor.HasComboData)
        {
            return;
        }

        AnimatorStateInfo animatorStateInfo = stateMachine.Player.Animator.GetCurrentAnimatorStateInfo(0);
        combatExecutor.Update(animatorStateInfo.normalizedTime);
    }

    private void ExecuteCombo()
    {
        if (!combatExecutor.HasComboData)
        {
            HandleAttackFinished();
            return;
        }

        currentComboIndex = nextComboIndex;
        combatExecutor.BeginCombo(currentComboIndex);

        stateMachine.Player.Animator.CrossFadeInFixedTime(
            attackData.CurrentComboList.TryGetComboName(currentComboIndex),
            0.1555f,
            0,
            0);

        UpdateComboIndex();

        canExecuteCombo = false;
        hasHandledAttackExit = false;

        float comboColdTime = attackData.CurrentComboList.TryGetComboColdTime(currentComboIndex);
        if (executeComboColdCoroutine != null)
        {
            stateMachine.Player.StopCoroutine(executeComboColdCoroutine);
        }

        executeComboColdCoroutine = stateMachine.Player.StartCoroutine(ExecuteComboCold(comboColdTime));

        if (stopComboCoroutine != null)
        {
            stateMachine.Player.StopCoroutine(stopComboCoroutine);
        }

        stopComboCoroutine = stateMachine.Player.StartCoroutine(StopCombo(comboColdTime));
    }

    private void UpdateComboIndex()
    {
        nextComboIndex++;
        if (nextComboIndex >= attackData.CurrentComboList.TryGetComboConfigsCount())
        {
            nextComboIndex = 0;
        }
    }

    private IEnumerator ExecuteComboCold(float coldTime)
    {
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

    protected override void OnAttackStarted(InputAction.CallbackContext context)
    {
        if (canExecuteCombo)
        {
            ExecuteCombo();
        }
    }

    public override void OnAnimationExitEnvent()
    {
        HandleAttackFinished();
    }

    private void TryHandleAttackAnimationExit()
    {
        if (hasHandledAttackExit)
        {
            return;
        }

        AnimatorStateInfo animatorStateInfo = stateMachine.Player.Animator.GetCurrentAnimatorStateInfo(0);
        if (!stateMachine.Player.Animator.IsInTransition(0) && animatorStateInfo.normalizedTime >= 1f)
        {
            HandleAttackFinished();
        }
    }

    private void HandleAttackFinished()
    {
        if (hasHandledAttackExit)
        {
            return;
        }

        hasHandledAttackExit = true;
        stateMachine.ChangeState(stateMachine.AttackRecoveryState);
    }
}
