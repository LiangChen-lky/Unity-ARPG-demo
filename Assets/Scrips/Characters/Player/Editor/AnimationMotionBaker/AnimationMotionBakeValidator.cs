using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 烘焙流水线的三道校验，各自负责不同阶段，都以「返回错误列表」而非抛异常的方式汇报，便于窗口一次列出全部问题：
/// ValidateConfiguration 校验窗口配置（扫描根、采样模型）；
/// ValidateTarget 校验单个扫描出的输入目标（Clip 属性、双来源一致性）；
/// ValidateResult 校验采样产出的曲线，是写回前的最后一道闸门。
/// </summary>
internal static class AnimationMotionBakeValidator
{
    // 曲线端点时间与首值比较用的容差，吸收浮点累积误差。
    private const float TimeTolerance = 0.0001f;

    /// <summary>
    /// 配置校验：在任何采样发生之前确认扫描根非空、容器内容完整，且采样模型带有效的 Humanoid Avatar，
    /// 因为 Root Motion 采样依赖 Humanoid 的 RootT/RootQ。
    /// </summary>
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

    /// <summary>
    /// ComboList 作为扫描根时的额外检查：它自身不含 MotionData，若引用列表为空或有空洞，扫描会静默产出零目标。
    /// </summary>
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

    /// <summary>
    /// 输入目标校验：确认 Clip 可作为一次性位移数据源——必须存在、非循环（循环片段无明确终点，累计偏航无意义）、
    /// 长度与帧率为正（否则无法推导采样区间）。
    /// </summary>
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

        return errors;
    }

    /// <summary>
    /// 烘焙结果校验：写回前确认两条曲线完整覆盖 [0, BakedDuration]、不含 NaN/Inf，
    /// 且累计偏航从 0 起算——首值非 0 意味着整条曲线带了一个基准偏移，取任意区间的角度增量都会被它污染。
    /// </summary>
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

    /// <summary>
    /// 校验单条曲线的端点覆盖范围与所有关键帧数值，错误信息统一附带目标路径和曲线名称。
    /// </summary>
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
