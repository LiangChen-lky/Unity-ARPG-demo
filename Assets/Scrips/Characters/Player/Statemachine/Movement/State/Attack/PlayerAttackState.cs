using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttackState : PlayerGroundedState
{
    private readonly PlayerAttackData attackData;

    private bool canExecuteCombo;
    private Transform currentTarget;
    private int currentComboIndex;
    private int nextComboIndex;
    private Coroutine stopComboCoroutine;
    private RunningEventIndex runningEventIndex;
    private bool hasHandledAttackExit;

    public PlayerAttackState(PlayerMovementStateMachine stateMachine) : base(stateMachine)
    {
        attackData = stateMachine.Player.Data.AttackData;
    }

    public override void Enter()
    {
        base.Enter();

        stateMachine.ReusableData.MovementSpeedModifier = 0f;
        ResetVelocity();

        runningEventIndex = new RunningEventIndex();
        canExecuteCombo = true;
        hasHandledAttackExit = false;

        ExecuteCombo();
    }

    public override void Update()
    {
        base.Update();

        RunComboEvents();
        TryHandleAttackAnimationExit();
    }

    private void RunComboEvents()
    {
        if (!HasComboData())
        {
            return;
        }

        AnimatorStateInfo animatorStateInfo = stateMachine.Player.Animator.GetCurrentAnimatorStateInfo(0);

        RunAttackDetectionEvent(animatorStateInfo.normalizedTime);
        RunFXEvent(animatorStateInfo.normalizedTime);
    }

    private void RunAttackDetectionEvent(float normalizedTime)
    {
        AttackDetectionConfig attackDetectionConfig =
            attackData.CurrentComboList.TryGetAttackDetectionConfig(currentComboIndex, runningEventIndex.AttackDetectionIndex);
        if (attackDetectionConfig == null || normalizedTime <= attackDetectionConfig.StartTime)
        {
            return;
        }

        Vector3 boxPosition = stateMachine.Player.transform.forward * attackDetectionConfig.Position.z +
                              stateMachine.Player.transform.up * attackDetectionConfig.Position.y +
                              stateMachine.Player.transform.right * attackDetectionConfig.Position.x;

        Collider[] targets = Physics.OverlapBox(
            stateMachine.Player.transform.position + boxPosition,
            attackDetectionConfig.Scale,
            Quaternion.Euler(attackDetectionConfig.Rotation + stateMachine.Player.transform.eulerAngles),
            attackData.TargetLayer,
            QueryTriggerInteraction.Ignore);

        foreach (Collider target in targets)
        {
            if (!target.TryGetComponent(out CombatControllerBase combatController))
            {
                continue;
            }

            combatController.CharacterBeHit(
                attackData.CurrentComboList.TryGetComboInteractionConfig(currentComboIndex,
                    runningEventIndex.AttackDetectionIndex),
                stateMachine.Player.transform,
                attackData.CurrentComboList.TryGetTargetMoveOffsetConfig(currentComboIndex,
                    runningEventIndex.AttackDetectionIndex));
        }

        runningEventIndex.AttackDetectionIndex++;
    }

    private void RunFXEvent(float normalizedTime)
    {
        FXConfig fxConfig = attackData.CurrentComboList.TryGetFXConfig(currentComboIndex, runningEventIndex.FXIndex);
        if (fxConfig == null || normalizedTime <= fxConfig.StartTime)
        {
            return;
        }

        if (fxConfig.FXObject != null)
        {
            ToolManager.Instance.PlayOneFX(
                fxConfig.FXObject,
                fxConfig.Position + stateMachine.Player.transform.position,
                fxConfig.Rotation + stateMachine.Player.transform.eulerAngles,
                fxConfig.Scale);
        }

        runningEventIndex.FXIndex++;
    }

    private void ExecuteCombo()
    {
        if (!HasComboData())
        {
            HandleAttackFinished();
            return;
        }

        FindTarget();
        LookTarget();

        runningEventIndex.Reset();
        currentComboIndex = nextComboIndex;

        stateMachine.Player.Animator.CrossFadeInFixedTime(
            attackData.CurrentComboList.TryGetComboName(currentComboIndex),
            0.1555f,
            0,
            0);

        UpdateComboIndex();

        canExecuteCombo = false;
        hasHandledAttackExit = false;

        float comboColdTime = attackData.CurrentComboList.TryGetComboColdTime(currentComboIndex);
        stateMachine.Player.StartCoroutine(ExecuteComboCold(comboColdTime));

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

    private void FindTarget()
    {
        if (currentTarget != null || attackData == null)
        {
            return;
        }

        Collider[] targetList = Physics.OverlapBox(
            stateMachine.Player.transform.position,
            new Vector3(4f, 4f, 4f),
            Quaternion.identity,
            attackData.TargetLayer,
            QueryTriggerInteraction.Ignore);

        float minDistance = float.MaxValue;
        foreach (Collider target in targetList)
        {
            float distance = Vector3.Distance(stateMachine.Player.transform.position, target.transform.position);
            if (distance < minDistance)
            {
                currentTarget = target.transform;
                minDistance = distance;
            }
        }
    }

    private void LookTarget()
    {
        if (currentTarget == null)
        {
            return;
        }

        Vector3 direction = (currentTarget.position - stateMachine.Player.transform.position).normalized;
        direction.y = 0f;
        stateMachine.Player.transform.forward = direction;
    }

    private bool HasComboData()
    {
        return attackData != null &&
               attackData.CurrentComboList != null &&
               attackData.CurrentComboList.ComboConfigs != null &&
               attackData.CurrentComboList.ComboConfigs.Length > 0;
    }

    private IEnumerator ExecuteComboCold(float coldTime)
    {
        while (coldTime > 0f)
        {
            yield return null;
            coldTime -= Time.deltaTime;
        }

        canExecuteCombo = true;
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

public class RunningEventIndex
{
    public int AttackDetectionIndex { get; set; }
    public int FXIndex { get; set; }
    public int AttackFeedbackIndex { get; set; }

    public void Reset()
    {
        AttackDetectionIndex = 0;
        FXIndex = 0;
        AttackFeedbackIndex = 0;
    }
}
