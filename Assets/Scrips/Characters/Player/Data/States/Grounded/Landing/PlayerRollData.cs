using System;
using Animancer;
using UnityEngine;

[Serializable]
public class PlayerRollData
{
    [field: Header("动画")]
    [field: SerializeField] public ClipTransition Animation { get; private set; }

    [field: SerializeField]
    [field: Range(0f, 3f)]
    public float SpeedModifier { get; private set; } = 1f;
}
