using System;
using UnityEngine;

[Serializable]
public sealed class AnimationMotionData
{
    [SerializeField] private AnimationClip clip;
    [SerializeField] private AnimationCurve speedCurve = new AnimationCurve();
    [SerializeField] private AnimationCurve rotationCurve = new AnimationCurve();
    [SerializeField, Min(0f)] private float bakedDuration;

    public AnimationClip Clip => clip;
    public AnimationCurve SpeedCurve => speedCurve;
    public AnimationCurve RotationCurve => rotationCurve;
    public float BakedDuration => bakedDuration;
}
