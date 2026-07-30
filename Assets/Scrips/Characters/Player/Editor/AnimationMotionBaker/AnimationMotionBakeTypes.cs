using UnityEngine;

internal enum AnimationMotionSampleRateMode
{
    FromClip,
    Fps60,
    Fps120
}

internal sealed class AnimationMotionBakeTarget
{
    public AnimationMotionBakeTarget(
        ScriptableObject owner,
        string propertyPath,
        AnimationClip clip)
    {
        Owner = owner;
        PropertyPath = propertyPath;
        Clip = clip;
    }

    public ScriptableObject Owner { get; }
    public string PropertyPath { get; }
    public AnimationClip Clip { get; }
    public string DisplayName => $"{Owner.name} / {PropertyPath}";
}

internal readonly struct AnimationMotionRootSample
{
    public AnimationMotionRootSample(float time, Vector3 position, Quaternion rotation)
    {
        Time = time;
        Position = position;
        Rotation = rotation;
    }

    public float Time { get; }
    public Vector3 Position { get; }
    public Quaternion Rotation { get; }
}

internal sealed class AnimationMotionBakeResult
{
    public AnimationMotionBakeResult(
        AnimationCurve speedCurve,
        AnimationCurve rotationCurve,
        float bakedDuration)
    {
        SpeedCurve = speedCurve;
        RotationCurve = rotationCurve;
        BakedDuration = bakedDuration;
    }

    public AnimationCurve SpeedCurve { get; }
    public AnimationCurve RotationCurve { get; }
    public float BakedDuration { get; }
}

internal readonly struct AnimationMotionBakeEntry
{
    public AnimationMotionBakeEntry(
        AnimationMotionBakeTarget target,
        AnimationMotionBakeResult result)
    {
        Target = target;
        Result = result;
    }

    public AnimationMotionBakeTarget Target { get; }
    public AnimationMotionBakeResult Result { get; }
}
