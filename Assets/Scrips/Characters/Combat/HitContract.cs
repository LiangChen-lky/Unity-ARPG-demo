using UnityEngine;

// 攻击方只传递力度档位，受击方自行映射为特效、硬直或击退等反应。
public enum AttackForce
{
    Easy,
    Medium,
    Hard
}

// 只描述攻击方已经确认的命中事实，受击方自行决定具体反应。
public readonly struct HitContext
{
    public HitContext(
        Vector3 sourcePosition,
        Vector3 sourceForward,
        AttackForce force,
        float damage)
    {
        SourcePosition = sourcePosition;
        SourceForward = sourceForward.sqrMagnitude > Mathf.Epsilon
            ? sourceForward.normalized
            : Vector3.forward;
        Force = force;
        Damage = damage;
    }

    public Vector3 SourcePosition { get; }
    public Vector3 SourceForward { get; }
    public AttackForce Force { get; }
    public float Damage { get; }
}

// 目标接收一次已确认命中的最小契约，不承担角色生命周期或属性系统职责。
public interface IHitReceiver
{
    void ReceiveHit(HitContext context);
}
