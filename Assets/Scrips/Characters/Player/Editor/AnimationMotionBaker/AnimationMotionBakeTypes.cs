using UnityEngine;

/// <summary>
/// 采样率来源。FromClip 沿用片段自身的 frameRate 逐帧采样；Fps60/Fps120 强制固定采样率，
/// 用于给帧率偏低的片段补密曲线。
/// </summary>
internal enum AnimationMotionSampleRateMode
{
    FromClip,
    Fps60,
    Fps120
}

/// <summary>
/// 流水线第一环：扫描阶段产出的待烘焙条目，定位「哪个资产的哪个字段要写什么动画」。
/// Owner + PropertyPath 是写回时重新定位 SerializedProperty 的唯一依据，因此必须与扫描时保持一致。
/// </summary>
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

    // 承载 AnimationMotionData 的配置资产。
    public ScriptableObject Owner { get; }
    // AnimationMotionData 字段在 Owner 内的序列化路径，支持数组元素等嵌套形式。
    public string PropertyPath { get; }
    // 待采样动画，可能为空，由校验阶段报错而非在扫描阶段丢弃。
    public AnimationClip Clip { get; }
    // 校验与进度提示统一使用的可读标识。
    public string DisplayName => $"{Owner.name} / {PropertyPath}";
}

/// <summary>
/// 流水线第二环：采样阶段的单帧原始记录，保存根节点世界位姿，尚未转换成速度或角度。
/// </summary>
internal readonly struct AnimationMotionRootSample
{
    public AnimationMotionRootSample(float time, Vector3 position, Quaternion rotation)
    {
        Time = time;
        Position = position;
        Rotation = rotation;
    }

    // 片段本地采样时间（秒）。
    public float Time { get; }
    // 根节点世界位置，起始帧已归零。
    public Vector3 Position { get; }
    // 根节点世界旋转，用于把世界位移换算回本地方向。
    public Quaternion Rotation { get; }
}

/// <summary>
/// 流水线第三环：由原始采样换算出的曲线结果，字段与 AnimationMotionData 一一对应，是校验的输入。
/// </summary>
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

    // 本地 Z 轴有符号前向速度曲线（米/秒）。
    public AnimationCurve SpeedCurve { get; }
    // 相对起始朝向的累计偏航曲线（度）。
    public AnimationCurve RotationCurve { get; }
    // 曲线覆盖的总时长（秒）。
    public float BakedDuration { get; }
}

/// <summary>
/// 流水线第四环：通过校验的目标与结果配对，攒够一整批后再交给写回阶段，避免中途失败留下半批资产。
/// </summary>
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
