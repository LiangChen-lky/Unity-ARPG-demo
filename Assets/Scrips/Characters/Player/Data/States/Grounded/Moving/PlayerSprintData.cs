using System;
using Animancer;
using UnityEngine;

[Serializable]
public class PlayerSprintData
{
    [field: SerializeField]
    [field: Range(1f, 3f)]
    public float SpeedModifier { get; private set; } = 1.7f;
    
    [field: SerializeField]
    [field: Range(0f, 5f)]
    public float SprintToRunTime { get; private set; } = 1f;

    [field: SerializeField]
    [field: Range(0f, 5f)]
    public float RunToWalkTime { get; private set; } = 0.5f;

    // 状态自身持有动画和淡入配置，避免继续依赖集中式状态名称。
    [field: SerializeField] public ClipTransition Animation { get; private set; }
}
