using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttackState : PlayerGroundedState
{
    protected PlayerAttackData attackData;
    
    protected bool canExecuteCombo;
    protected Transform currentTarget;
 
    private int currentComboIndex;
    private int nextComboIndex;
    
    private Coroutine stopComboCoroutine;
    private Coroutine executeMoveOffsetCoroutine;
    private RunningEventIndex runningEventIndex;
    private bool hasHandledAttackExit;
    
    public PlayerAttackState(PlayerMovementStateMachine stateMachine) : base(stateMachine)
    {
        attackData = stateMachine.Player.Data.AttackData;
    }
    
    #region IState Methods
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
        
        RunEvent();
        TryHandleAttackAnimationExit();
    }

    #endregion

    #region Main Methods

    private void RunEvent()
    {
        // 攻击检测
        AttackDetectionConfig attackDetectionConfig =
            attackData.CurrentComboList.TryGetAttackDetectionConfig(currentComboIndex, runningEventIndex.AttackDetectionIndex);
        if (attackDetectionConfig != null)
        {
            if (stateMachine.Player.Animator.GetCurrentAnimatorStateInfo(0).normalizedTime > attackDetectionConfig.StartTime)
            {
                Vector3 boxPosition = stateMachine.Player.transform.forward * attackDetectionConfig.Position.z +
                                      stateMachine.Player.transform.up * attackDetectionConfig.Position.y +
                                      stateMachine.Player.transform.right * attackDetectionConfig.Position.x;
                // 执行攻击检测
                // OverlapBox后续可以优化
                // var targetList = Physics.OverlapBox(stateMachine.Player.transform.position + boxPosition,
                //     attackDetectionConfig.Scale, Quaternion.identity, attackData.TargetLayer);
                // foreach (var target in targetList)
                // {
                //     // 执行受击
                //     target.GetComponent<CombatControllerBase>().CharacterBeHit(
                //         attackData.CurrentComboList.TryGetComboInteractionConfig(currentComboIndex,
                //             runningEventIndex.AttackDetectionIndex), stateMachine.Player.transform,
                //         attackData.CurrentComboList.TryGetTargetMoveOffsetConfig(currentComboIndex, runningEventIndex.AttackDetectionIndex));
                // }
                // 执行一次事件后
                runningEventIndex.AttackDetectionIndex++;
            }
        }
        
        // 生成特效
        FXConfig fxConfig =
            attackData.CurrentComboList.TryGetFXConfig(currentComboIndex, runningEventIndex.FXIndex);
        if (fxConfig != null)
        {
            if (stateMachine.Player.Animator.GetCurrentAnimatorStateInfo(0).normalizedTime > fxConfig.StartTime)
            {
                ToolManager.Instance.PlayOneFX(fxConfig.FXObject, fxConfig.Position + stateMachine.Player.transform.position,
                    fxConfig.Rotation + stateMachine.Player.transform.eulerAngles, fxConfig.Scale);
                // 执行一次事件后
                runningEventIndex.FXIndex++;
            }
        }
        
        // 生成音效
    }

    private void ExecuteCombo()
    {
        FindTarget();
        LookTarget();
        
        runningEventIndex.Reset();
        
        currentComboIndex = nextComboIndex;
        
        stateMachine.Player.Animator.CrossFadeInFixedTime(attackData.CurrentComboList.TryGetComboName(currentComboIndex), 0.1555f, 0, 0);
        
        UpdateComboIndex();
        
        canExecuteCombo = false;
        hasHandledAttackExit = false;
        // 冷却时间结束后才可以开始下一combo
        stateMachine.Player.StartCoroutine(ExecuteComboCold(attackData.CurrentComboList.TryGetComboColdTime(currentComboIndex)));
        
        // 如果在冷却时间内没有继续攻击输入，则重置combo
        if (stopComboCoroutine != null)
        {
            stateMachine.Player.StopCoroutine(stopComboCoroutine);
        }
        stopComboCoroutine = stateMachine.Player.StartCoroutine(StopCombo(attackData.CurrentComboList.TryGetComboColdTime(currentComboIndex)));
    }

    private void ExecuteMoveOffset(MoveOffsetConfig moveOffsetConfig, Transform user)
    {
        if (executeMoveOffsetCoroutine != null)
        {
            stateMachine.Player.StopCoroutine(executeMoveOffsetCoroutine);
        }
        Vector3 direction = user.GetMoveOffsetDirection(moveOffsetConfig.MoveOffsetDirection);
        executeMoveOffsetCoroutine = stateMachine.Player.StartCoroutine(ExecuteMoveOffsetIEnumerator(moveOffsetConfig, direction));
    }
    
    private void UpdateComboIndex()
    {
        nextComboIndex++;
        if (nextComboIndex >= attackData.CurrentComboList.TryGetComboConfigsCount())
        {
            nextComboIndex = 0;
        }
    }

    // private void CharacterBeHit(ComboInteractionConfig interactionConfig, Transform attacker, MoveOffsetConfig moveOffsetConfig)
    // {
    //     stateMachine.Player.transform.forward = -attacker.forward;
    //     // 播放受击动画
    //     stateMachine.Player.Animator.Play(interactionConfig.HitName);
    //     
    //     // 播放受击特效
    //     var fxObject = attackData.HitFXList[(int)interactionConfig.AttackForce].TryGetOneFXObject();
    //     ToolManager.Instance.PlayOneFX(fxObject, attackData.FXPositionList[0].position, Vector3.zero, Vector3.one);
    //     
    //     // 位移补偿
    //     ExecuteMoveOffset(moveOffsetConfig, attacker);
    // }

    private void FindTarget()
    {
        if (currentTarget != null)
        {
            return;
        }
        
        // 硬编码，待优化
        var targetList = Physics.OverlapBox(stateMachine.Player.transform.position, new Vector3(4, 4, 4),
            Quaternion.identity, attackData.TargetLayer);
        float minDistance = float.MaxValue;
        // 待优化
        foreach (var target in targetList)
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
        direction.y = 0;
        stateMachine.Player.transform.forward = direction;
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

    private IEnumerator ExecuteMoveOffsetIEnumerator(MoveOffsetConfig moveOffsetConfig, Vector3 direction)
    {
        while (stateMachine.Player.Animator.GetCurrentAnimatorStateInfo(0).normalizedTime < moveOffsetConfig.Duration)
        {
            yield return null;
            float value = moveOffsetConfig.MoveCurve.Evaluate(stateMachine.Player.Animator.GetCurrentAnimatorStateInfo(0).normalizedTime);
        }
        // 移动逻辑
        
        executeMoveOffsetCoroutine = null;
    }
    
    private IEnumerator StopCombo(float coldTime)
    {
        float time = coldTime * 1.2f;
        while (time > 0f)
        {
            yield return null;
            time -= Time.deltaTime;
        }
        
        nextComboIndex = 0;
    }
    #endregion

    #region Input Methods

    protected override void OnAttackStarted(InputAction.CallbackContext context)
    {
        Debug.Log("Attack input received");
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

    #endregion
}

public class RunningEventIndex
{
    public int AttackDetectionIndex { get; set; } = 0;
    public int FXIndex { get; set; } = 0;
    public int AttackFeedbackIndex { get; set; } = 0;
    
    public void Reset()
    {
        AttackDetectionIndex = 0;
        FXIndex = 0;
        AttackFeedbackIndex = 0;
    }
}