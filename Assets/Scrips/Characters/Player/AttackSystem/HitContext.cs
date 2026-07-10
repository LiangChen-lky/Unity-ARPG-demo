using UnityEngine;

public readonly struct HitMovement
{
    public HitMovement(
        AnimationCurve curve,
        Vector3 direction,
        float startTime,
        float duration,
        float scale)
    {
        Curve = curve;
        Direction = direction.sqrMagnitude > Mathf.Epsilon ? direction.normalized : Vector3.zero;
        StartTime = startTime;
        Duration = duration;
        Scale = scale;
    }

    public AnimationCurve Curve { get; }
    public Vector3 Direction { get; }
    public float StartTime { get; }
    public float Duration { get; }
    public float Scale { get; }

    public bool IsValid =>
        Curve != null &&
        Direction.sqrMagnitude > Mathf.Epsilon &&
        !Mathf.Approximately(Scale, 0f) &&
        Duration > StartTime;

    public float Evaluate(float normalizedTime)
    {
        return Curve.Evaluate(normalizedTime) * Scale;
    }
}

public readonly struct HitContext
{
    public HitContext(
        Vector3 sourcePosition,
        Vector3 sourceForward,
        string hitAnimationName,
        Weapon weapon,
        AttackForce force,
        float damage,
        HitMovement movement)
    {
        SourcePosition = sourcePosition;
        SourceForward = sourceForward.sqrMagnitude > Mathf.Epsilon
            ? sourceForward.normalized
            : Vector3.forward;
        HitAnimationName = hitAnimationName;
        Weapon = weapon;
        Force = force;
        Damage = damage;
        Movement = movement;
    }

    public Vector3 SourcePosition { get; }
    public Vector3 SourceForward { get; }
    public string HitAnimationName { get; }
    public Weapon Weapon { get; }
    public AttackForce Force { get; }
    public float Damage { get; }
    public HitMovement Movement { get; }
}
