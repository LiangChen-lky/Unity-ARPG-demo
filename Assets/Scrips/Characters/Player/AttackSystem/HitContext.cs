using UnityEngine;

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
