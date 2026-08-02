using System;
using Animancer;
using UnityEngine;

[Serializable]
public class PlayerDashData
{
    [field: Header("动画")]
    // 四个方向暂时共用同一段 Dash Clip，但分别保留 Transition 以便后续独立调整。
    [field: SerializeField] public ClipTransition ForwardAnimation { get; private set; }
    [field: SerializeField] public ClipTransition BackwardAnimation { get; private set; }
    [field: SerializeField] public ClipTransition LeftAnimation { get; private set; }
    [field: SerializeField] public ClipTransition RightAnimation { get; private set; }

    [field: SerializeField]
    [field: Range(1f, 3f)]
    public float SpeedModifier { get; private set; } = 2f;

    [field: SerializeField]
    [field: Range(0f, 2f)]
    public float TimeToBeConsideredConsecutive { get; private set; } = 1f;

    [field: SerializeField]
    [field: Range(1, 10)]
    public int ConsecutiveDashesLimitAmount { get; private set; } = 2;

    [field: SerializeField]
    [field: Range(0f, 5f)]
    public float DashLimitReachedCooldown { get; private set; } = 1.75f;
    
    [field: SerializeField] public PlayerRotationData RotationData { get; private set; }
}
