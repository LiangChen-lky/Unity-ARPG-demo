using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

internal sealed class AnimationMotionSampler : IDisposable
{
    private readonly GameObject sampleInstance;
    private readonly Animator animator;

    public AnimationMotionSampler(GameObject samplePrefab)
    {
        sampleInstance = Object.Instantiate(samplePrefab);
        sampleInstance.name = $"{samplePrefab.name} (Animation Motion Sample)";
        sampleInstance.hideFlags = HideFlags.HideAndDontSave;

        animator = sampleInstance.GetComponentInChildren<Animator>(true);
        animator.applyRootMotion = true;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        sampleInstance.SetActive(true);
    }

    public AnimationMotionBakeResult Sample(
        AnimationClip clip,
        AnimationMotionSampleRateMode sampleRateMode)
    {
        float sampleRate = GetSampleRate(clip, sampleRateMode);
        float interval = 1f / sampleRate;
        int fullIntervalCount = Mathf.FloorToInt(clip.length * sampleRate);
        List<AnimationMotionRootSample> samples =
            new List<AnimationMotionRootSample>(fullIntervalCount + 2);

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

            Vector3 worldDelta = current.Position - previous.Position;
            Vector3 localDelta = Quaternion.Inverse(previous.Rotation) * worldDelta;
            float forwardSpeed = localDelta.z / deltaTime;
            float midpoint = previous.Time + deltaTime * 0.5f;

            speedKeys.Add(new Keyframe(midpoint, forwardSpeed));

            accumulatedYaw += CalculateDeltaYaw(previous.Rotation, current.Rotation);
            rotationCurve.AddKey(current.Time, accumulatedYaw);
        }

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
        clip.SampleAnimation(animator.gameObject, time);
        samples.Add(new AnimationMotionRootSample(
            time,
            root.position,
            root.rotation));
    }

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
