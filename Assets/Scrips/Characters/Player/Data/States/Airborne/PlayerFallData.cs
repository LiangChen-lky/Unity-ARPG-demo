using System;
using Animancer;
using UnityEngine;

[Serializable]
public class PlayerFallData
{
    [field: Header("动画")]
    [field: SerializeField] public ClipTransition Animation { get; private set; }

    [field: SerializeField]
    [field: Range(1f, 15f)]
    public float FallSpeedLimit { get; private set; } = 15f;

    [field: SerializeField]
    [field: Range(0f, 10f)]
    public float MinimumDistanceToBeConsideredHardFall { get; private set; } = 3f;
}
