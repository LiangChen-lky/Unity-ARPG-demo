using System;
using System.Collections;
using UnityEngine;

public class CombatControllerBase : MonoBehaviour
{
    [field: SerializeField] public ComboList CurrentComboList { get; private set; }
    [field: SerializeField] public LayerMask TargetLayer { get; private set; }
    [field: SerializeField] public HitFXConfig[] HitFXList { get; private set; }
    [field: SerializeField] public Transform[] FXPositionList { get; private set; }
    
    public PlayerInput Input { get; private set; }

    private int currentComboIndex;
    private int nextComboIndex;
    
    protected bool canExecuteCombo;
    protected Transform currentTarget;

    private Animator animator;
    private Coroutine stopComboCoroutine;
    private Coroutine executeMoveOffsetCoroutine;
    private RunningEventIndex runningEventIndex;

    protected virtual void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        Input = GetComponent<PlayerInput>();
        
        runningEventIndex = new RunningEventIndex();
        
        canExecuteCombo = true;
    }

    private void Update()
    {
        RunEvent();
    }


    private void RunEvent()
    {
        // 只有执行招式时调用（如果和分层状态机结合，只在攻击状态的Update调用，则不需要此判断）
        if (!animator.GetCurrentAnimatorStateInfo(0).IsName(CurrentComboList.TryGetComboName(currentComboIndex)) ||
            animator.IsInTransition(0))
        {
            return;
        }
        
        // 攻击检测
        AttackDetectionConfig attackDetectionConfig =
            CurrentComboList.TryGetAttackDetectionConfig(currentComboIndex, runningEventIndex.AttackDetectionIndex);
        if (attackDetectionConfig != null)
        {
            if (animator.GetCurrentAnimatorStateInfo(0).normalizedTime > attackDetectionConfig.StartTime)
            {
                Vector3 boxPosition = transform.forward * attackDetectionConfig.Position.z +
                                      transform.up * attackDetectionConfig.Position.y +
                                      transform.right * attackDetectionConfig.Position.x;
                // 执行攻击检测
                // OverlapBox后续可以优化
                var targetList = Physics.OverlapBox(transform.position + boxPosition,
                    attackDetectionConfig.Scale, Quaternion.identity, TargetLayer);
                foreach (var target in targetList)
                {
                    // 执行受击
                    target.GetComponent<CombatControllerBase>().CharacterBeHit(
                        CurrentComboList.TryGetComboInteractionConfig(currentComboIndex,
                            runningEventIndex.AttackDetectionIndex), transform,
                        CurrentComboList.TryGetTargetMoveOffsetConfig(currentComboIndex, runningEventIndex.AttackDetectionIndex));
                }
                // 执行一次事件后
                runningEventIndex.AttackDetectionIndex++;
            }
        }
        
        // 生成特效
        FXConfig fxConfig =
            CurrentComboList.TryGetFXConfig(currentComboIndex, runningEventIndex.FXIndex);
        if (fxConfig != null)
        {
            if (animator.GetCurrentAnimatorStateInfo(0).normalizedTime > fxConfig.StartTime)
            {
                ToolManager.Instance.PlayOneFX(fxConfig.FXObject, fxConfig.Position + transform.position,
                    fxConfig.Rotation + transform.eulerAngles, fxConfig.Scale);
                // 执行一次事件后
                runningEventIndex.FXIndex++;
            }
        }
        
        // 生成音效
    }
    
    protected void ExecuteCombo()
    {
        FindTarget();
        LookTarget();
        
        runningEventIndex.Reset();
        
        currentComboIndex = nextComboIndex;
        
        animator.CrossFadeInFixedTime(CurrentComboList.TryGetComboName(currentComboIndex), 0.1555f, 0, 0);
        
        UpdateComboIndex();
        
        canExecuteCombo = false;
        // 冷却时间结束后才可以开始下一combo
        StartCoroutine(ExecuteComboCold(CurrentComboList.TryGetComboColdTime(currentComboIndex)));
        
        // 如果在冷却时间内没有继续攻击输入，则重置combo
        if (stopComboCoroutine != null)
        {
            StopCoroutine(stopComboCoroutine);
        }
        stopComboCoroutine = StartCoroutine(StopCombo(CurrentComboList.TryGetComboColdTime(currentComboIndex)));
    }

    protected void ExecuteMoveOffset(MoveOffsetConfig moveOffsetConfig, Transform user)
    {
        if (executeMoveOffsetCoroutine != null)
        {
            StopCoroutine(executeMoveOffsetCoroutine);
        }
        Vector3 direction = user.GetMoveOffsetDirection(moveOffsetConfig.MoveOffsetDirection);
        executeMoveOffsetCoroutine = StartCoroutine(ExecuteMoveOffsetIEnumerator(moveOffsetConfig, direction));
    }
    
    private void UpdateComboIndex()
    {
        nextComboIndex++;
        if (nextComboIndex >= CurrentComboList.TryGetComboConfigsCount())
        {
            nextComboIndex = 0;
        }
    }

    private void CharacterBeHit(ComboInteractionConfig interactionConfig, Transform attacker, MoveOffsetConfig moveOffsetConfig)
    {
        transform.forward = -attacker.forward;
        // 播放受击动画
        animator.Play(interactionConfig.HitName);
        
        // 播放受击特效
        var fxObject = HitFXList[(int)interactionConfig.AttackForce].TryGetOneFXObject();
        ToolManager.Instance.PlayOneFX(fxObject, FXPositionList[0].position, Vector3.zero, Vector3.one);
        
        // 位移补偿
        ExecuteMoveOffset(moveOffsetConfig, attacker);
    }

    private void FindTarget()
    {
        if (currentTarget != null)
        {
            return;
        }
        
        // 硬编码，待优化
        var targetList = Physics.OverlapBox(transform.position, new Vector3(4, 4, 4), Quaternion.identity, TargetLayer);
        float minDistance = float.MaxValue;
        // 待优化
        foreach (var target in targetList)
        {
            float distance = Vector3.Distance(transform.position, target.transform.position);
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
        Vector3 direction = (currentTarget.position - transform.position).normalized;
        direction.y = 0;
        transform.forward = direction;
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
        while (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < moveOffsetConfig.Duration)
        {
            yield return null;
            float value = moveOffsetConfig.MoveCurve.Evaluate(animator.GetCurrentAnimatorStateInfo(0).normalizedTime);
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
}
