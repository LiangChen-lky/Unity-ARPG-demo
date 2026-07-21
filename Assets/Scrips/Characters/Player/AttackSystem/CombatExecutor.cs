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

    public bool HasComboData =>
        attackData != null &&
        attackData.CurrentComboList != null &&
        attackData.CurrentComboList.ComboConfigs != null &&
        attackData.CurrentComboList.ComboConfigs.Length > 0;

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
        if (!HasComboData)
        {
            return;
        }

        RunAttackDetectionEvent(normalizedTime);
        RunFXEvent(normalizedTime);
    }

    private void RunAttackDetectionEvent(float normalizedTime)
    {
        ComboList comboList = attackData.CurrentComboList;
        AttackDetectionConfig detectionConfig =
            comboList.TryGetAttackDetectionConfig(currentComboIndex, attackDetectionEventIndex);

        if (detectionConfig == null || normalizedTime <= detectionConfig.StartTime)
        {
            return;
        }

        ComboInteractionConfig interactionConfig =
            comboList.TryGetComboInteractionConfig(currentComboIndex, attackDetectionEventIndex);
        MoveOffsetConfig moveOffsetConfig =
            comboList.TryGetTargetMoveOffsetConfig(currentComboIndex, attackDetectionEventIndex);

        if (interactionConfig != null)
        {
            DispatchHits(detectionConfig, interactionConfig, moveOffsetConfig);
        }

        attackDetectionEventIndex++;
    }

    private void DispatchHits(
        AttackDetectionConfig detectionConfig,
        ComboInteractionConfig interactionConfig,
        MoveOffsetConfig moveOffsetConfig)
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

        HitContext hitContext = CreateHitContext(interactionConfig, moveOffsetConfig);
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

    private HitContext CreateHitContext(
        ComboInteractionConfig interactionConfig,
        MoveOffsetConfig moveOffsetConfig)
    {
        HitMovement movement = default;
        if (moveOffsetConfig != null)
        {
            movement = new HitMovement(
                moveOffsetConfig.MoveCurve,
                owner.GetMoveOffsetDirection(moveOffsetConfig.MoveOffsetDirection),
                moveOffsetConfig.StartTime,
                moveOffsetConfig.Duration,
                moveOffsetConfig.Scale);
        }

        return new HitContext(
            owner.position,
            owner.forward,
            interactionConfig.HitName,
            interactionConfig.Weapon,
            interactionConfig.AttackForce,
            interactionConfig.Damage,
            movement);
    }

    private void RunFXEvent(float normalizedTime)
    {
        FXConfig fxConfig = attackData.CurrentComboList.TryGetFXConfig(currentComboIndex, fxEventIndex);
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

    private void ResetEventIndexes()
    {
        attackDetectionEventIndex = 0;
        fxEventIndex = 0;
    }
}
