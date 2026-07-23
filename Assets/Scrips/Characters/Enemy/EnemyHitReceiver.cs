using UnityEngine;

// 将通用命中契约转换为 Enemy 的属性变化，再复用通用受击表现。
public class EnemyHitReceiver : HitReceiverBase
{
    private EnemyStats stats;

    public void Initialize(EnemyStats enemyStats)
    {
        // 依赖由 Enemy 组合根注入，受击组件不依赖 EnemySO 或攻击执行器。
        stats = enemyStats;
    }

    public override void ReceiveHit(HitContext context)
    {
        if (stats.IsDead)
        {
            return;
        }

        // 当前命中先改变运行时生命值，再由基类处理该次受击的朝向和 FX。
        stats.TakeDamage(context.Damage);
        base.ReceiveHit(context);
    }
}
