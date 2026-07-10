using System;
using UnityEngine;

[Serializable]
public class PlayerWalkData
{
    [field: SerializeField]
    [field: Range(0f, 1f)]
    public float SpeedModifier { get; private set; } = 0.225f;

    [field: SerializeField]
    [field: Range(0.1f, 1f)]
    public float AnimationSpeedModifier { get; private set; } = 0.55f;
}
