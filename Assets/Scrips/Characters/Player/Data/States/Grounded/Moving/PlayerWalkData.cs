using System;
using Animancer;
using UnityEngine;

[Serializable]
public class PlayerWalkData
{
    [field: SerializeField]
    [field: Range(0f, 1f)]
    public float SpeedModifier { get; private set; } = 0.225f;

    // 状态自身持有动画和淡入配置，避免继续依赖集中式状态名称。
    [field: SerializeField] public ClipTransition Animation { get; private set; }
}
