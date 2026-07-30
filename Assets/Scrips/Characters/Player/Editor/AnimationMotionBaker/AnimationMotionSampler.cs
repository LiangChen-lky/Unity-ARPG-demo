using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// 在编辑器中离线提取 Root Motion：实例化一个临时 Humanoid 模型，逐帧 SampleAnimation 记录根节点位姿，
/// 再换算成本地 Z 有符号速度与累计偏航两条标量曲线。
/// 采样只发生在这个临时实例上，不触碰场景中的玩家对象与运行时配置；实例由 Dispose 负责销毁，故务必配合 using 使用。
/// </summary>
internal sealed class AnimationMotionSampler : IDisposable
{
    private readonly GameObject sampleInstance;
    private readonly Animator animator;

    public AnimationMotionSampler(GameObject samplePrefab)
    {
        // HideAndDontSave 让临时实例不出现在层级面板、也不会被存入场景，避免污染用户的编辑状态。
        sampleInstance = Object.Instantiate(samplePrefab);
        sampleInstance.name = $"{samplePrefab.name} (Animation Motion Sample)";
        sampleInstance.hideFlags = HideFlags.HideAndDontSave;

        animator = sampleInstance.GetComponentInChildren<Animator>(true);
        // 只有开启 Root Motion 才会把 RootT/RootQ 写到根节点 Transform 上；关闭剔除以保证离屏也照常求值。
        animator.applyRootMotion = true;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        sampleInstance.SetActive(true);
    }

    /// <summary>
    /// 按采样率逐帧采样整段动画，并保证最后一个采样点精确落在 Clip.length 上。
    /// </summary>
    public AnimationMotionBakeResult Sample(
        AnimationClip clip,
        AnimationMotionSampleRateMode sampleRateMode)
    {
        float sampleRate = GetSampleRate(clip, sampleRateMode);
        float interval = 1f / sampleRate;
        int fullIntervalCount = Mathf.FloorToInt(clip.length * sampleRate);
        List<AnimationMotionRootSample> samples =
            new List<AnimationMotionRootSample>(fullIntervalCount + 2);

        // 采样前把根节点归零，使记录到的位姿就是相对动画起点的位移与朝向。
        Transform root = animator.transform;
        root.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

        // 过滤与末帧仅相差浮点误差的采样点，最后只补一次精确 Clip.length。
        float endTimeTolerance = interval * 0.001f;
        for (int index = 0; index <= fullIntervalCount; index++)
        {
            float time = index * interval;
            if (clip.length - time <= endTimeTolerance)
            {
                break;
            }

            CaptureSample(clip, time, root, samples);
        }

        CaptureSample(clip, clip.length, root, samples);
        return BuildResult(samples);
    }

    public void Dispose()
    {
        Object.DestroyImmediate(sampleInstance);
    }

    /// <summary>
    /// 把原始位姿采样换算成两条标量曲线：
    /// 速度取相邻帧世界位移经上一帧朝向逆变换后的本地 Z 分量除以时间差，因此只保留前后向分量（负值为后退），
    /// 侧向与竖直位移被丢弃；转向取相邻帧前向在水平面投影的有符号夹角并逐帧累加，得到单调连续、可跨 ±180° 的偏航曲线。
    /// 独立于实例方法暴露为 internal static，便于测试直接用构造好的采样序列验证换算规则。
    /// </summary>
    internal static AnimationMotionBakeResult BuildResult(
        IReadOnlyList<AnimationMotionRootSample> samples)
    {
        if (samples == null || samples.Count < 2)
        {
            throw new InvalidOperationException("根运动曲线至少需要两个采样点。");
        }

        AnimationCurve speedCurve = new AnimationCurve();
        AnimationCurve rotationCurve = new AnimationCurve();
        List<Keyframe> speedKeys = new List<Keyframe>(samples.Count + 1);

        // 累计偏航以起始朝向为基准，首个关键帧固定为 0。
        float accumulatedYaw = 0f;
        rotationCurve.AddKey(0f, 0f);

        for (int index = 1; index < samples.Count; index++)
        {
            AnimationMotionRootSample previous = samples[index - 1];
            AnimationMotionRootSample current = samples[index];
            float deltaTime = current.Time - previous.Time;

            if (deltaTime <= 0f)
            {
                throw new InvalidOperationException("根运动采样时间必须严格递增。");
            }

            // 世界位移逆变换回上一帧的本地空间，只取 Z 分量作为有符号前向速度（米/秒）。
            Vector3 worldDelta = current.Position - previous.Position;
            Vector3 localDelta = Quaternion.Inverse(previous.Rotation) * worldDelta;
            float forwardSpeed = localDelta.z / deltaTime;
            // 速度是区间量而非瞬时量，关键帧放在区间中点，避免整条曲线相对动画整体偏移半帧。
            float midpoint = previous.Time + deltaTime * 0.5f;

            speedKeys.Add(new Keyframe(midpoint, forwardSpeed));

            accumulatedYaw += CalculateDeltaYaw(previous.Rotation, current.Rotation);
            rotationCurve.AddKey(current.Time, accumulatedYaw);
        }

        // 速度关键帧位于各区间中点，两端会各空出半帧；用首尾值补上 0 与 duration 端点，
        // 使曲线完整覆盖 [0, BakedDuration]（结果校验也要求这一点）。
        float duration = samples[samples.Count - 1].Time;
        speedCurve.AddKey(0f, speedKeys[0].value);
        foreach (Keyframe speedKey in speedKeys)
        {
            speedCurve.AddKey(speedKey);
        }
        speedCurve.AddKey(duration, speedKeys[speedKeys.Count - 1].value);

        SetLinearTangents(speedCurve);
        SetLinearTangents(rotationCurve);

        return new AnimationMotionBakeResult(
            speedCurve,
            rotationCurve,
            duration);
    }

    /// <summary>
    /// 将窗口中的采样率选项转换成实际 FPS；FromClip 直接采用动画资源自身的 frameRate。
    /// </summary>
    private static float GetSampleRate(
        AnimationClip clip,
        AnimationMotionSampleRateMode sampleRateMode)
    {
        switch (sampleRateMode)
        {
            case AnimationMotionSampleRateMode.Fps60:
                return 60f;
            case AnimationMotionSampleRateMode.Fps120:
                return 120f;
            default:
                return clip.frameRate;
        }
    }

    private void CaptureSample(
        AnimationClip clip,
        float time,
        Transform root,
        List<AnimationMotionRootSample> samples)
    {
        // SampleAnimation 直接把指定时刻的姿势写到实例上，无需推进播放状态，因此可以任意顺序、任意时间点取样。
        clip.SampleAnimation(animator.gameObject, time);
        samples.Add(new AnimationMotionRootSample(
            time,
            root.position,
            root.rotation));
    }

    /// <summary>
    /// 取相邻两帧的单帧偏航增量：先把前向投影到水平面剔除俯仰与翻滚，再求绕 Y 轴的有符号夹角（右转为正）。
    /// 只累加单帧增量，故整体累计值不受 ±180° 环绕限制。
    /// </summary>
    private static float CalculateDeltaYaw(
        Quaternion previousRotation,
        Quaternion currentRotation)
    {
        Vector3 previousForward = Vector3.ProjectOnPlane(
            previousRotation * Vector3.forward,
            Vector3.up);
        Vector3 currentForward = Vector3.ProjectOnPlane(
            currentRotation * Vector3.forward,
            Vector3.up);

        return Vector3.SignedAngle(previousForward, currentForward, Vector3.up);
    }

    /// <summary>
    /// 把曲线所有关键帧的左右切线设为线性，保证烘焙值只在相邻采样点之间做线性插值。
    /// </summary>
    private static void SetLinearTangents(AnimationCurve curve)
    {
        // 逐帧数据不需要贝塞尔平滑，线性切线可避免速度和旋转产生额外过冲。
        for (int index = 0; index < curve.length; index++)
        {
            AnimationUtility.SetKeyLeftTangentMode(
                curve,
                index,
                AnimationUtility.TangentMode.Linear);
            AnimationUtility.SetKeyRightTangentMode(
                curve,
                index,
                AnimationUtility.TangentMode.Linear);
        }
    }
}
