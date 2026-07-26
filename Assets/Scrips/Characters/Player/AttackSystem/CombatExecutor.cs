using System.Collections.Generic;
using UnityEngine;

public sealed class CombatExecutor
{
    private readonly Transform owner;
    private readonly PlayerAttackData attackData;
    private readonly ICombatEffectSpawner effectSpawner;
    private readonly HashSet<IHitReceiver> hitReceivers = new();

    private int currentComboIndex;
    private int attackDetectionEventIndex;
    private int fxEventIndex;

    public CombatExecutor(
        Transform owner,
        PlayerAttackData attackData,
        ICombatEffectSpawner effectSpawner = null)
    {
        this.owner = owner;
        this.attackData = attackData;
        this.effectSpawner = effectSpawner ?? new CombatEffectSpawner();
    }

    public void BeginAttack()
    {
        ResetEventIndexes();
    }

    public void EndAttack()
    {
        hitReceivers.Clear();
        ResetEventIndexes();
    }

    public void BeginCombo(int comboIndex)
    {
        // 这里只重置本段攻击事件游标，不搜索目标或改变玩家朝向。
        // 普通攻击沿用当前朝向，锁定目标后的转向由玩家状态机显式负责。
        currentComboIndex = comboIndex;
        ResetEventIndexes();
    }

    public void Update(float normalizedTime)
    {
        RunAttackDetectionEvent(normalizedTime);
        RunFXEvent(normalizedTime);
    }

    private void RunAttackDetectionEvent(float normalizedTime)
    {
        ComboList comboList = attackData.CurrentComboList;

        // 同一游戏帧可能跨过多个命中帧，也允许同一动画帧配置多个碰撞盒。
        // 循环消费全部到时事件，确保它们都在本次 Update 中完成检测。
        while (true)
        {
            AttackDetectionConfig detectionConfig =
                comboList.TryGetAttackDetectionConfig(currentComboIndex, attackDetectionEventIndex);

            // 走完本段所有命中事件后正常返回，这是事件遍历的自然结束，不是配置错误。
            if (detectionConfig == null)
            {
                return;
            }

            // 配置以动画帧为准，在执行时换算为 Animator 的归一化进度。
            float startNormalizedTime = comboList.GetAttackDetectionNormalizedTime(
                currentComboIndex,
                attackDetectionEventIndex);
            if (normalizedTime <= startNormalizedTime)
            {
                return;
            }

            // 检测数据存在即说明对应索引的交互数据也通过校验，直接按同索引取得并执行。
            // 缺失数据应由 PlayerAttackState 进入攻击前的前置校验拦截，而不是在这里静默失效。
            ComboInteractionConfig interactionConfig =
                comboList.TryGetComboInteractionConfig(currentComboIndex, attackDetectionEventIndex);

            DispatchHits(detectionConfig, interactionConfig);

            attackDetectionEventIndex++;
        }
    }

    private void DispatchHits(
        AttackDetectionConfig detectionConfig,
        ComboInteractionConfig interactionConfig)
    {
        Vector3 boxPosition =
            owner.forward * detectionConfig.Position.z +
            owner.up * detectionConfig.Position.y +
            owner.right * detectionConfig.Position.x;

        Collider[] targets = Physics.OverlapBox(
            owner.position + boxPosition,
            detectionConfig.Scale,
            Quaternion.Euler(detectionConfig.Rotation + owner.eulerAngles),
            attackData.TargetLayer,
            QueryTriggerInteraction.Ignore);

        HitContext hitContext = CreateHitContext(interactionConfig);
        hitReceivers.Clear();

        foreach (Collider target in targets)
        {
            IHitReceiver hitReceiver = target.GetComponentInParent<IHitReceiver>();
            if (hitReceiver == null || !hitReceivers.Add(hitReceiver))
            {
                continue;
            }

            Component receiverComponent = hitReceiver as Component;
            if (receiverComponent != null && receiverComponent.transform.IsChildOf(owner))
            {
                continue;
            }

            hitReceiver.ReceiveHit(hitContext);
        }
    }

    private HitContext CreateHitContext(ComboInteractionConfig interactionConfig)
    {
        return new HitContext(
            owner.position,
            owner.forward,
            interactionConfig.AttackForce,
            interactionConfig.Damage);
    }

    private void RunFXEvent(float normalizedTime)
    {
        // 从下一条未执行的特效开始消费；同一帧跨过多个时机时必须全部补齐，
        // 避免后续状态切换重置游标后遗漏已到时的攻击特效。
        while (true)
        {
            FXConfig fxConfig = attackData.CurrentComboList.TryGetFXConfig(
                currentComboIndex,
                fxEventIndex);
            if (fxConfig == null || normalizedTime <= fxConfig.StartTime)
            {
                return;
            }

            effectSpawner.SpawnOneShot(
                fxConfig.FXObject,
                fxConfig.Position + owner.position,
                fxConfig.Rotation + owner.eulerAngles,
                fxConfig.Scale);

            fxEventIndex++;
        }
    }

    private void ResetEventIndexes()
    {
        attackDetectionEventIndex = 0;
        fxEventIndex = 0;
    }
}
