using System.Collections.Generic;
using UnityEngine;

internal static class AnimationMotionBakeValidator
{
    private const float TimeTolerance = 0.0001f;

    public static List<string> ValidateConfiguration(
        IReadOnlyList<ScriptableObject> scanRoots,
        GameObject samplePrefab)
    {
        List<string> errors = new List<string>();

        if (scanRoots == null || scanRoots.Count == 0)
        {
            errors.Add("至少需要配置一个 ScriptableObject 扫描根。");
        }
        else
        {
            for (int index = 0; index < scanRoots.Count; index++)
            {
                ScriptableObject root = scanRoots[index];
                if (root == null)
                {
                    errors.Add($"扫描根列表第 {index + 1} 项不能为空。");
                    continue;
                }

                if (root is ComboList comboList)
                {
                    ValidateComboListRoot(comboList, errors);
                }
            }
        }

        if (samplePrefab == null)
        {
            errors.Add("采样模型不能为空。");
            return errors;
        }

        Animator animator = samplePrefab.GetComponentInChildren<Animator>(true);
        if (animator == null)
        {
            errors.Add($"采样模型 {samplePrefab.name} 中不存在 Animator。");
            return errors;
        }

        if (animator.avatar == null || !animator.avatar.isValid || !animator.avatar.isHuman)
        {
            errors.Add($"采样模型 {samplePrefab.name} 必须配置有效的 Humanoid Avatar。");
        }

        return errors;
    }

    private static void ValidateComboListRoot(
        ComboList comboList,
        List<string> errors)
    {
        if (comboList.ComboConfigs == null || comboList.ComboConfigs.Length == 0)
        {
            errors.Add($"扫描根 {comboList.name} 没有配置任何 ComboConfig。");
            return;
        }

        for (int index = 0; index < comboList.ComboConfigs.Length; index++)
        {
            if (comboList.ComboConfigs[index] == null)
            {
                errors.Add(
                    $"扫描根 {comboList.name} 的 ComboConfigs 第 {index + 1} 项不能为空。");
            }
        }
    }

    public static List<string> ValidateTarget(AnimationMotionBakeTarget target)
    {
        List<string> errors = new List<string>();
        AnimationClip clip = target.Clip;

        if (clip == null)
        {
            errors.Add($"{target.DisplayName}：Clip 不能为空。");
            return errors;
        }

        if (clip.isLooping)
        {
            errors.Add($"{target.DisplayName}：Clip {clip.name} 必须关闭循环播放。");
        }

        if (clip.length <= 0f)
        {
            errors.Add($"{target.DisplayName}：Clip {clip.name} 的长度必须大于 0。");
        }

        if (clip.frameRate <= 0f)
        {
            errors.Add($"{target.DisplayName}：Clip {clip.name} 的采样率必须大于 0。");
        }

        // 过渡阶段 Combo 仍由 AttackClip 播放，双来源不一致会让烘焙结果与实际动画脱节。
        if (target.Owner is ComboConfig comboConfig &&
            target.PropertyPath == "motionData" &&
            comboConfig.AttackClip != clip)
        {
            errors.Add(
                $"{target.DisplayName}：MotionData.Clip 必须与 AttackClip 引用同一个动画。");
        }

        return errors;
    }

    public static List<string> ValidateResult(
        AnimationMotionBakeTarget target,
        AnimationMotionBakeResult result)
    {
        List<string> errors = new List<string>();

        if (result == null)
        {
            errors.Add($"{target.DisplayName}：烘焙结果不能为空。");
            return errors;
        }

        ValidateCurve(target, "SpeedCurve", result.SpeedCurve, result.BakedDuration, errors);
        ValidateCurve(target, "RotationCurve", result.RotationCurve, result.BakedDuration, errors);

        if (!IsFinite(result.BakedDuration) || result.BakedDuration <= 0f)
        {
            errors.Add($"{target.DisplayName}：BakedDuration 必须是大于 0 的有限值。");
        }

        if (result.RotationCurve != null &&
            result.RotationCurve.length > 0 &&
            Mathf.Abs(result.RotationCurve.keys[0].value) > TimeTolerance)
        {
            errors.Add($"{target.DisplayName}：RotationCurve 的首值必须为 0。");
        }

        return errors;
    }

    private static void ValidateCurve(
        AnimationMotionBakeTarget target,
        string curveName,
        AnimationCurve curve,
        float bakedDuration,
        List<string> errors)
    {
        if (curve == null || curve.length == 0)
        {
            errors.Add($"{target.DisplayName}：{curveName} 不能为空。");
            return;
        }

        Keyframe[] keys = curve.keys;
        if (Mathf.Abs(keys[0].time) > TimeTolerance ||
            Mathf.Abs(keys[keys.Length - 1].time - bakedDuration) > TimeTolerance)
        {
            errors.Add(
                $"{target.DisplayName}：{curveName} 必须从 0 覆盖到 BakedDuration。");
        }

        foreach (Keyframe key in keys)
        {
            if (!IsFinite(key.time) || !IsFinite(key.value))
            {
                errors.Add($"{target.DisplayName}：{curveName} 包含非有限关键帧。");
                return;
            }
        }
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
